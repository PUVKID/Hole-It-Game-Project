using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class ReferenceGame {
    // Only the local contact region needs PhysX bodies. Distant authored stacks stay asleep.
    // The supplied game's gravity and common collectable rigidbody settings are retained.
    const int BodyBudget=384;
    const float PhysicsStep=1f/60f;
    class PhysicalBody {
        public GameObject root;public Rigidbody rigid;public readonly List<Collider> colliders=new List<Collider>();
        public Actor actor;public Vector3 previous,current;public Quaternion previousRotation,currentRotation;
    }
    Scene physicalScene;PhysicsScene physicsScene;BoxCollider physicalFloor;
    PhysicsMaterial friction;float physicsAccumulator,activationClock;
    readonly List<PhysicalBody> physicalBodies=new List<PhysicalBody>();
    readonly Stack<PhysicalBody> spareBodies=new Stack<PhysicalBody>();
    readonly List<int> physicalCandidates=new List<int>();
    readonly List<GameObject> physicalBorders=new List<GameObject>();
    public int ActivePhysicsBodies=>physicalBodies.Count-spareBodies.Count;
    public float ReferenceGravity=>-5.81f;
    void ResetPhysicalWorld(){
        foreach(var border in physicalBorders){border.SetActive(false);Destroy(border);}physicalBorders.Clear();
        if(!physicalScene.IsValid()){
            physicalScene=SceneManager.CreateScene("Reference Contact World",new CreateSceneParameters(LocalPhysicsMode.Physics3D));physicsScene=physicalScene.GetPhysicsScene();
            Physics.gravity=new Vector3(0,-5.81f,0);
            friction=new PhysicsMaterial("Reference No Friction"){dynamicFriction=0,staticFriction=0,bounciness=0,frictionCombine=PhysicsMaterialCombine.Minimum,bounceCombine=PhysicsMaterialCombine.Minimum};
            var floorObject=new GameObject("Contact Floor");SceneManager.MoveGameObjectToScene(floorObject,physicalScene);physicalFloor=floorObject.AddComponent<BoxCollider>();physicalFloor.center=new Vector3(0,-1,0);physicalFloor.size=new Vector3(2000,2,2000);physicalFloor.sharedMaterial=friction;
        }
        foreach(var a in actors)ReleasePhysicalBody(a);
        physicsAccumulator=activationClock=0;
    }
    void BuildPhysicalBorders(){
        foreach(var draw in environmentDraws){int kind=draw.model.id%10;if((kind>=4&&kind!=9)||draw.model.collisionParts==null)continue;
            foreach(var shape in draw.model.collisionParts){var matrix=draw.matrix*shape.local;var root=new GameObject("Authored Arena Contact");SceneManager.MoveGameObjectToScene(root,physicalScene);root.transform.SetPositionAndRotation(matrix.GetColumn(3),matrix.rotation);root.transform.localScale=matrix.lossyScale;var col=root.AddComponent<MeshCollider>();col.sharedMesh=shape.mesh;col.sharedMaterial=friction;physicalBorders.Add(root);}
        }
    }
    public bool PhysicsTransformsFinite(){foreach(var a in actors)if(float.IsNaN(a.pos.sqrMagnitude)||float.IsInfinity(a.pos.sqrMagnitude)||a.pos.sqrMagnitude>100000000)return false;return true;}
    public int StartIsolatedFallForTest(){
        int i=0;while(i<actors.Count&&(actors[i].model.id<0||actors[i].model.collisionParts==null))i++;
        var a=actors[i];ReleasePhysicalBody(a);a.pos=new Vector3(500,10,500);a.state=1;a.fallSpeed=0;var b=EnsurePhysicalBody(i);b.rigid.isKinematic=false;IgnoreFloor(b,true);return i;
    }
    public Vector3 ActorPositionForTest(int index)=>actors[index].pos;
    void EnsurePlayableStart(){
        if(CanStandAt(HolePosition))return;
        Vector3 best=HolePosition;float bestScore=float.MaxValue;
        foreach(var tile in playground)for(int z=0;z<3;z++)for(int x=0;x<3;x++){
            var candidate=new Vector3(Mathf.Lerp(tile.xMin+Radius,tile.xMax-Radius,x*.5f),0,Mathf.Lerp(tile.yMin+Radius,tile.yMax-Radius,z*.5f));
            if(!CanStandAt(candidate))continue;
            float score=(candidate-HolePosition).sqrMagnitude;Query(candidate,Radius+1,physicalCandidates);
            foreach(int i in physicalCandidates){var a=actors[i];var delta=a.pos-candidate;delta.y=0;if(delta.sqrMagnitude<Mathf.Pow(a.radius+Radius+.3f,2))score+=100;}
            if(score<bestScore){bestScore=score;best=candidate;}
        }
        if(bestScore==float.MaxValue)throw new System.InvalidOperationException("No playable spawn in authored stage "+(LevelIndex+1));
        HolePosition=best;min=Vector2.Min(min,new Vector2(best.x-Radius,best.z-Radius));max=Vector2.Max(max,new Vector2(best.x+Radius,best.z+Radius));
    }
    void DisposePhysicalWorld(){
        foreach(var b in physicalBodies)if(b.root!=null)b.root.SetActive(false);
        physicalBodies.Clear();spareBodies.Clear();
        if(physicalScene.IsValid()&&physicalScene.isLoaded)SceneManager.UnloadSceneAsync(physicalScene);
        if(friction!=null)Destroy(friction);
    }
    PhysicalBody EnsurePhysicalBody(int index){
        var a=actors[index];if(a.body!=null)return a.body;
        if(a.model.collisionParts==null||a.model.collisionParts.Length==0)return null;
        PhysicalBody b;
        if(spareBodies.Count>0)b=spareBodies.Pop();
        else {
            if(physicalBodies.Count>=BodyBudget)return null;
            var root=new GameObject("Pooled Contact Body");SceneManager.MoveGameObjectToScene(root,physicalScene);
            b=new PhysicalBody{root=root};b.rigid=root.AddComponent<Rigidbody>();b.rigid.isKinematic=true;
            
            b.rigid.solverIterations=6;b.rigid.solverVelocityIterations=1;b.rigid.maxAngularVelocity=7;b.rigid.sleepThreshold=.005f;
            physicalBodies.Add(b);
        }
        b.actor=a;a.body=b;b.root.SetActive(false);b.rigid.isKinematic=true;
        b.root.transform.SetPositionAndRotation(a.pos,a.rotation);b.root.transform.localScale=Vector3.one*a.scale;
        for(int c=0;c<a.model.collisionParts.Length;c++){
            var shape=a.model.collisionParts[c];System.Type type=shape.mesh.name=="Cube"?typeof(BoxCollider):shape.mesh.name=="Sphere"?typeof(SphereCollider):shape.mesh.name=="Capsule"?typeof(CapsuleCollider):typeof(MeshCollider);
            if(c==b.colliders.Count){var child=new GameObject("Authored Contact Shape");child.transform.SetParent(b.root.transform,false);b.colliders.Add((Collider)child.AddComponent(type));}
            var col=b.colliders[c];if(col.GetType()!=type){var owner=col.gameObject;col.enabled=false;Destroy(col);col=(Collider)owner.AddComponent(type);b.colliders[c]=col;}
            col.enabled=true;col.sharedMaterial=friction;col.contactOffset=.01f;col.gameObject.SetActive(true);col.transform.localPosition=shape.local.GetColumn(3);col.transform.localRotation=shape.local.rotation;col.transform.localScale=shape.local.lossyScale;
            if(col is MeshCollider meshCollider){meshCollider.convex=true;meshCollider.sharedMesh=shape.mesh;}
            else if(col is BoxCollider box){box.center=shape.mesh.bounds.center;box.size=shape.mesh.bounds.size;}
            else if(col is SphereCollider sphere)sphere.radius=.5f;
            else if(col is CapsuleCollider capsule){capsule.radius=.5f;capsule.height=2;capsule.direction=1;}
        }
        for(int c=a.model.collisionParts.Length;c<b.colliders.Count;c++)b.colliders[c].gameObject.SetActive(false);
        b.rigid.mass=Mathf.Max(.01f,a.model.mass);b.rigid.linearDamping=a.model.linearDamping;b.rigid.angularDamping=a.model.angularDamping;
        b.previous=b.current=a.pos;b.previousRotation=b.currentRotation=a.rotation;
        b.root.SetActive(true);IgnoreFloor(b,a.state==1);
        return b;
    }
    void IgnoreFloor(PhysicalBody b,bool ignore){foreach(var col in b.colliders)if(col.gameObject.activeInHierarchy)Physics.IgnoreCollision(col,physicalFloor,ignore);}
    void ReleasePhysicalBody(Actor a){
        var b=a.body;if(b==null)return;
        b.rigid.isKinematic=true;b.root.SetActive(false);b.actor=null;a.body=null;spareBodies.Push(b);
    }
    void BeginPhysicalDrop(int index){
        var b=EnsurePhysicalBody(index);if(b==null)return;
        b.rigid.isKinematic=false;IgnoreFloor(b,true);b.rigid.WakeUp();
        // Off-centre support loss tips the object; there is no horizontal suction force.
        Vector3 offset=actors[index].pos-HolePosition;offset.y=0;
        b.rigid.AddTorque(Vector3.Cross(Vector3.up,offset)*.3f,ForceMode.VelocityChange);
    }
    void StepPhysicalWorld(float dt){
        activationClock-=dt;
        if(activationClock<=0){
            activationClock=.10f;
            foreach(var b in physicalBodies){if(b.actor==null)continue;var a=b.actor;
                var d=a.pos-HolePosition;d.y=0;
                if(a.state==0&&!a.falling&&d.sqrMagnitude>Mathf.Pow(Radius+6,2)&&b.rigid.IsSleeping())ReleasePhysicalBody(a);
            }
            Query(HolePosition,Radius+3,physicalCandidates);
            physicalCandidates.Sort((x,y)=>actors[x].Bottom.CompareTo(actors[y].Bottom));
            foreach(int i in physicalCandidates){var a=actors[i];if(a.state==2)continue;Vector3 d=a.pos-HolePosition;d.y=0;
                if(d.sqrMagnitude>Mathf.Pow(Radius+2.5f+a.radius,2))continue;
                var b=EnsurePhysicalBody(i);if(b==null)continue;
                if(a.falling||a.state==1||d.sqrMagnitude<Mathf.Pow(Radius+.8f+a.radius,2)){
                    b.rigid.isKinematic=false;if(a.state==1)IgnoreFloor(b,true);
                }
            }
        }
        for(int k=falling.Count-1;k>=0;k--){int i=falling[k];var a=actors[i];
            if(a.state!=0||!a.falling){a.falling=false;falling.RemoveAt(k);continue;}
            var b=EnsurePhysicalBody(i);
            if(b!=null){b.rigid.isKinematic=false;continue;}
            // Bounded fallback for remote unsupported pieces, using the same gravity.
            float surface=a.support>=0&&actors[a.support].state==0?Mathf.Max(0,actors[a.support].Top):0;
            a.fallSpeed+=5.81f*dt;a.pos.y=Mathf.Max(surface-a.bottomOffset,a.pos.y-a.fallSpeed*dt);
            if(a.Bottom<=surface+.005f){a.falling=false;falling.RemoveAt(k);}else foreach(int child in a.above)Wake(child);
        }
        physicsAccumulator+=dt;
        while(physicsAccumulator>=PhysicsStep){
            foreach(var b in physicalBodies){if(b.actor==null)continue;b.previous=b.current;b.previousRotation=b.currentRotation;}
            physicsScene.Simulate(PhysicsStep);physicsAccumulator-=PhysicsStep;
            foreach(var b in physicalBodies){var a=b.actor;if(a==null)continue;
                b.current=b.rigid.position;b.currentRotation=b.rigid.rotation;
                float oldY=a.pos.y;a.pos=b.current;a.rotation=b.currentRotation;
                var bounds=b.colliders[0].bounds;foreach(var col in b.colliders)if(col.gameObject.activeInHierarchy)bounds.Encapsulate(col.bounds);a.bottomOffset=bounds.min.y-a.pos.y;a.topOffset=bounds.max.y-a.pos.y;
                long key=Key(Mathf.FloorToInt(a.pos.x/CellSize),Mathf.FloorToInt(a.pos.z/CellSize));
                if(key!=a.cellKey){int id=actors.IndexOf(a);if(cells.TryGetValue(a.cellKey,out var old))old.Remove(id);AddCell(id);}
                if(a.pos.y<oldY-.003f)foreach(int child in a.above)Wake(child);
                if(a.falling&&a.state==0&&b.rigid.IsSleeping())a.falling=false;
            }
        }
    }
    Vector3 RenderPosition(Actor a)=>a.body==null?a.pos:Vector3.Lerp(a.body.previous,a.body.current,physicsAccumulator/PhysicsStep);
    Quaternion RenderRotation(Actor a)=>a.body==null?a.rotation:Quaternion.Slerp(a.body.previousRotation,a.body.currentRotation,physicsAccumulator/PhysicsStep);
}
