using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class ReferenceGame {
    // Only the local contact region needs PhysX bodies. Distant authored stacks stay asleep.
    // Runtime parameters and collider modes are recovered from the supplied native build.
    const int BodyBudget=384;
    const float PhysicsStep=ReferencePhysicsSettings.FixedStep;
    class PhysicalBody {
        public GameObject root;public Rigidbody rigid;public readonly List<Collider> colliders=new List<Collider>();
        public Actor actor;public Vector3 previous,current;public Quaternion previousRotation,currentRotation;
        public int switchTier,selectedGroup=-1,materialPhase=-1;public bool initialSettling,collectorEntered;public float restAt,stableTime;
    }
    Scene physicalScene;PhysicsScene physicsScene;MeshCollider physicalFloor,physicalInnerFloor;
    PhysicsMaterial friction,lessFriction,dragFriction;float physicsAccumulator,activationClock,initialRestStarted,simulatedPhysicsTime;
    readonly List<int> authoredPhysicsQueue=new List<int>();
    readonly List<int> widePhysicsActors=new List<int>();
    readonly Dictionary<Actor,float> sourceFitRadii=new Dictionary<Actor,float>();
    readonly List<PhysicalBody> physicalBodies=new List<PhysicalBody>();
    readonly Stack<PhysicalBody> spareBodies=new Stack<PhysicalBody>();
    readonly List<int> physicalCandidates=new List<int>();
    readonly List<GameObject> physicalBorders=new List<GameObject>();
    public int ActivePhysicsBodies=>physicalBodies.Count-spareBodies.Count;
    public float ReferenceGravity=>-ReferencePhysicsSettings.GravityMagnitude;
    public float SimulatedPhysicsTime=>simulatedPhysicsTime;
    public float SourceApertureRadius=>.83923f*.915f*VisualRadius;
    float SourceFitRadius(Actor actor){
        if(!sourceFitRadii.TryGetValue(actor,out var radius)){
            radius=ReferencePhysicsSettings.EffectiveFitRadius(actor.model,actor.rotation,actor.scale);
            // Bombs have their mesh on a child, unlike ordinary collectables. The native
            // fit-tier helper returns -1 without a root MeshFilter; bot safety cannot use it.
            if(radius<0){var bounds=WorldBounds(actor.model.bounds,actor.rotation,actor.scale);radius=new Vector2(bounds.extents.x,bounds.extents.z).magnitude+new Vector2(bounds.center.x,bounds.center.z).magnitude;}
            sourceFitRadii[actor]=radius;
        }
        return radius;
    }
    void ResetPhysicalWorld(){
        ReferencePhysicsSettings.ApplyGlobalSettings();initialRestStarted=Time.time;ResetVerificationTarget();
        foreach(var border in physicalBorders){border.SetActive(false);Destroy(border);}physicalBorders.Clear();
        if(!physicalScene.IsValid()){
            physicalScene=SceneManager.CreateScene("Reference Contact World",new CreateSceneParameters(LocalPhysicsMode.Physics3D));physicsScene=physicalScene.GetPhysicsScene();
            friction=new PhysicsMaterial("Reference NoFriction"){dynamicFriction=0,staticFriction=0,bounciness=0,frictionCombine=PhysicsMaterialCombine.Multiply,bounceCombine=PhysicsMaterialCombine.Multiply};
            lessFriction=new PhysicsMaterial("Reference LessFrictionObject"){dynamicFriction=0,staticFriction=0,bounciness=0,frictionCombine=PhysicsMaterialCombine.Average,bounceCombine=PhysicsMaterialCombine.Multiply};
            dragFriction=new PhysicsMaterial("Reference dragMat"){dynamicFriction=.1f,staticFriction=.1f,bounciness=0,frictionCombine=PhysicsMaterialCombine.Minimum,bounceCombine=PhysicsMaterialCombine.Multiply};
            var floorObject=new GameObject("Source HoleCollider-Outer");SceneManager.MoveGameObjectToScene(floorObject,physicalScene);physicalFloor=floorObject.AddComponent<MeshCollider>();
            var innerObject=new GameObject("Source HoleCollider-Inner");SceneManager.MoveGameObjectToScene(innerObject,physicalScene);physicalInnerFloor=innerObject.AddComponent<MeshCollider>();physicalInnerFloor.sharedMaterial=friction;
        }
        foreach(var a in actors)ReleasePhysicalBody(a);
        physicsAccumulator=activationClock=simulatedPhysicsTime=0;authoredPhysicsQueue.Clear();widePhysicsActors.Clear();sourceFitRadii.Clear();
    }
    // Call after the authored actors, obstacles and hole start position are loaded.
    void ConfigureAuthoredPhysics(){
        if(Catalog.holeContactMesh==null)throw new System.InvalidOperationException("Original floor aperture mesh missing");
        physicalFloor.sharedMesh=Catalog.holeContactMesh;physicalInnerFloor.sharedMesh=Catalog.holeContactMesh;
        UpdateHoleContactGeometry();
        for(int i=0;i<actors.Count;i++){
            var a=actors[i];var bounds=WorldBounds(a.model.bounds,a.rotation,a.scale);
            if(Mathf.Max(bounds.extents.x,bounds.extents.z)>2.5f)widePhysicsActors.Add(i);
            if(ReferencePhysicsSettings.InitiallyDynamic(a.placement,a.model.id))authoredPhysicsQueue.Add(i);
        }
        // Initial gravity is authored by Ph. Schedule distant initial bodies in batches if the
        // original layout exceeds the mobile body pool; they retain source values and order.
        PrimeNextAuthoredBodies();
    }
    void PrimeNextAuthoredBodies(){
        while(authoredPhysicsQueue.Count>0&&ActivePhysicsBodies<BodyBudget){
            int i=authoredPhysicsQueue[0];authoredPhysicsQueue.RemoveAt(0);if(actors[i].state!=0)continue;
            var b=EnsurePhysicalBody(i);if(b==null)continue;b.initialSettling=true;b.restAt=Time.time+ReferencePhysicsSettings.RestDelay(actors[i].model.id);b.rigid.isKinematic=false;
        }
    }
    void StepAuthoredRest(float dt){
        bool pending=authoredPhysicsQueue.Count>0;if(!pending)foreach(var b in physicalBodies)if(b.actor!=null&&b.initialSettling){pending=true;break;}
        if(pending)StepPhysicalWorld(dt);
    }
    void UpdateHoleContactGeometry(){
        float scale=Mathf.Max(.01f,VisualRadius);
        physicalFloor.transform.SetPositionAndRotation(HolePosition+Vector3.up*(-.041f+.05f*scale),Quaternion.identity);physicalFloor.transform.localScale=Vector3.one*(.92f*scale);
        physicalInnerFloor.transform.SetPositionAndRotation(HolePosition+Vector3.up*(-.041f+.025f*scale),Quaternion.identity);physicalInnerFloor.transform.localScale=Vector3.one*(.915f*scale);
    }
    bool IntersectsSourceDetection(PhysicalBody b){
        float radius=VisualRadius*1.55f*.5f;
        foreach(var col in b.colliders){if(!col.enabled||!col.gameObject.activeInHierarchy)continue;
            Vector3 probe=new Vector3(HolePosition.x,Mathf.Clamp(0,col.bounds.min.y,col.bounds.max.y),HolePosition.z);
            var delta=col.ClosestPoint(probe)-probe;delta.y=0;if(delta.sqrMagnitude<=radius*radius)return true;
        }
        return false;
    }
    bool IntersectsSmoothDetection(PhysicalBody b){
        float scale=VisualRadius;var center=HolePosition+new Vector3(.02995112f*scale,-.041f-20.32903f*scale,-.009540831f*scale);
        var box=new Bounds(center,new Vector3(1.2935724f*scale,40.494776f*scale,1.3469124f*scale));
        foreach(var col in b.colliders)if(col.enabled&&col.gameObject.activeInHierarchy&&box.Intersects(col.bounds))return true;
        return false;
    }
    public bool ReachedSourceCollectionTrigger(int index){
        var a=actors[index];if(a.body==null||a.state!=0)return false;
        return a.body.collectorEntered;
    }
    bool SourceCollectorOverlaps(PhysicalBody body){
        var center=HolePosition+new Vector3(-.30063584f,-4.15507568f,.44971511f);
        var box=new Bounds(center,new Vector3(31.02136433f,2.96247728f,29.32730703f));
        foreach(var col in body.colliders)if(col.enabled&&col.gameObject.activeInHierarchy&&box.Intersects(col.bounds))return true;
        return false;
    }
    public Vector3 ActorAngularVelocityForTest(int index)=>actors[index].body==null?Vector3.zero:actors[index].body.rigid.angularVelocity;
    void BuildPhysicalBorders(){
        foreach(var draw in environmentDraws){int kind=draw.model.id%10;if((kind>=4&&kind!=9)||draw.model.collisionParts==null)continue;
            foreach(var shape in draw.model.collisionParts){if(!shape.sourceEnabled)continue;var matrix=draw.matrix*shape.local;var root=new GameObject("Authored Arena Contact");SceneManager.MoveGameObjectToScene(root,physicalScene);root.transform.SetPositionAndRotation(matrix.GetColumn(3),matrix.rotation);root.transform.localScale=matrix.lossyScale;var col=root.AddComponent<MeshCollider>();col.sharedMesh=shape.mesh;col.sharedMaterial=SourceMaterial(shape.materialKind);physicalBorders.Add(root);}
        }
    }
    public bool PhysicsTransformsFinite(){foreach(var a in actors)if(float.IsNaN(a.pos.sqrMagnitude)||float.IsInfinity(a.pos.sqrMagnitude)||a.pos.sqrMagnitude>100000000)return false;return true;}
    public int StartIsolatedFallForTest(){
        int i=0;while(i<actors.Count&&(actors[i].model.id<0||actors[i].model.collisionParts==null))i++;
        var a=actors[i];ReleasePhysicalBody(a);a.pos=new Vector3(500,10,500);a.state=1;a.fallSpeed=0;var b=EnsurePhysicalBody(i);MakePhysical(b);IgnoreFloor(b,true);return i;
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
        if(lessFriction!=null)Destroy(lessFriction);if(dragFriction!=null)Destroy(dragFriction);
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
            
            b.rigid.solverIterations=ReferencePhysicsSettings.SolverIterations;b.rigid.solverVelocityIterations=ReferencePhysicsSettings.SolverVelocityIterations;b.rigid.maxAngularVelocity=ReferencePhysicsSettings.MaximumAngularSpeed;b.rigid.sleepThreshold=ReferencePhysicsSettings.CollectableSleepThreshold;b.rigid.maxDepenetrationVelocity=ReferencePhysicsSettings.MaximumDepenetrationSpeed;
            physicalBodies.Add(b);
        }
        b.actor=a;a.body=b;b.root.SetActive(false);b.rigid.isKinematic=false;b.rigid.linearVelocity=Vector3.zero;b.rigid.angularVelocity=Vector3.zero;b.rigid.isKinematic=true;
        b.rigid.useGravity=a.model.useGravity;b.rigid.interpolation=RigidbodyInterpolation.None;b.rigid.collisionDetectionMode=CollisionDetectionMode.Discrete;
        b.restAt=initialRestStarted+ReferencePhysicsSettings.RestDelay(a.model.id);b.initialSettling=ReferencePhysicsSettings.InitiallyDynamic(a.placement,a.model.id)&&Time.time<b.restAt;b.stableTime=0;b.collectorEntered=false;
        b.switchTier=ReferencePhysicsSettings.ColliderSwitchTier(a.placement,a.model,a.rotation,a.scale);b.selectedGroup=-1;b.materialPhase=-1;
        b.root.transform.SetPositionAndRotation(a.pos,a.rotation);b.root.transform.localScale=Vector3.one*a.scale;
        for(int c=0;c<a.model.collisionParts.Length;c++){
            var shape=a.model.collisionParts[c];System.Type type=shape.mesh.name=="Cube"?typeof(BoxCollider):shape.mesh.name=="Sphere"?typeof(SphereCollider):shape.mesh.name=="Capsule"?typeof(CapsuleCollider):typeof(MeshCollider);
            if(c==b.colliders.Count){var child=new GameObject("Authored Contact Shape");child.transform.SetParent(b.root.transform,false);b.colliders.Add((Collider)child.AddComponent(type));}
            var col=b.colliders[c];if(col.GetType()!=type){var owner=col.gameObject;col.enabled=false;Destroy(col);col=(Collider)owner.AddComponent(type);b.colliders[c]=col;}
            col.enabled=true;col.sharedMaterial=SourceMaterial(shape.materialKind);col.contactOffset=ReferencePhysicsSettings.ContactOffset;col.gameObject.SetActive(true);col.transform.localPosition=shape.local.GetColumn(3);col.transform.localRotation=shape.local.rotation;col.transform.localScale=shape.local.lossyScale;
            if(col is MeshCollider meshCollider){meshCollider.convex=true;meshCollider.sharedMesh=shape.mesh;}
            else if(col is BoxCollider box){box.center=shape.mesh.bounds.center;box.size=shape.mesh.bounds.size;}
            else if(col is SphereCollider sphere)sphere.radius=.5f;
            else if(col is CapsuleCollider capsule){capsule.radius=.5f;capsule.height=2;capsule.direction=1;}
        }
        for(int c=a.model.collisionParts.Length;c<b.colliders.Count;c++)b.colliders[c].gameObject.SetActive(false);
        b.rigid.mass=a.model.id<0?Mathf.Max(.0001f,a.model.mass):ReferencePhysicsSettings.Mass(a.placement);
        ReferencePhysicsSettings.ApplyRuntimeDamping(b.rigid);
        b.previous=b.current=a.pos;b.previousRotation=b.currentRotation=a.rotation;
        b.root.SetActive(true);RefreshBodyColliders(b);if(b.initialSettling)b.rigid.isKinematic=false;IgnoreFloor(b,a.state==1);
        return b;
    }
    PhysicsMaterial SourceMaterial(int kind)=>kind==1?friction:kind==2?lessFriction:kind==3?dragFriction:null;
    void RefreshBodyColliders(PhysicalBody b){
        int group=ReferencePhysicsSettings.ColliderGroup(b.actor.placement,b.actor.model,b.switchTier,Mathf.Clamp(GrowthTier(GrowthPoints),0,10));
        if(group==b.selectedGroup)return;b.selectedGroup=group;
        for(int c=0;c<b.actor.model.collisionParts.Length;c++)b.colliders[c].enabled=ReferencePhysicsSettings.ColliderEnabled(b.actor.model.collisionParts[c],group);
        IgnoreFloor(b,b.actor.state==1);
    }
    void SetContactMaterial(PhysicalBody b,int phase){
        if(b.materialPhase==phase)return;b.materialPhase=phase;
        for(int c=0;c<b.actor.model.collisionParts.Length;c++)b.colliders[c].sharedMaterial=phase==3?friction:phase==1?lessFriction:phase==2?null:SourceMaterial(b.actor.model.collisionParts[c].materialKind);
    }
    void MakePhysical(PhysicalBody b){b.initialSettling=false;b.rigid.interpolation=RigidbodyInterpolation.Interpolate;b.rigid.isKinematic=false;}
    // The exported concave floor has a real aperture. IgnoreCollision is reserved for the
    // isolated free-fall test; gameplay never removes floor support by a bounding-box rule.
    void IgnoreFloor(PhysicalBody b,bool ignore){if(ignore)return;foreach(var col in b.colliders)if(col.gameObject.activeInHierarchy&&col.enabled){Physics.IgnoreCollision(col,physicalFloor,false);Physics.IgnoreCollision(col,physicalInnerFloor,false);}}
    void ReleasePhysicalBody(Actor a){
        var b=a.body;if(b==null)return;
        b.rigid.isKinematic=true;b.root.SetActive(false);b.actor=null;a.body=null;spareBodies.Push(b);
    }
    void BeginPhysicalDrop(int index){
        var b=EnsurePhysicalBody(index);if(b==null)return;
        MakePhysical(b);SetContactMaterial(b,3);IgnoreFloor(b,true);b.rigid.WakeUp();
    }
    void StepPhysicalWorld(float dt){
        UpdateHoleContactGeometry();
        Physics.SyncTransforms();
        activationClock-=dt;
        if(activationClock<=0){
            activationClock=PhysicsStep;
            foreach(var b in physicalBodies){if(b.actor==null)continue;var a=b.actor;
                if(b.initialSettling&&Time.time>=b.restAt){b.initialSettling=false;b.rigid.isKinematic=true;}
                RefreshBodyColliders(b);
                var d=a.pos-HolePosition;d.y=0;
                bool touching=IntersectsSmoothDetection(b);
                SetContactMaterial(b,a.state==1?3:touching?1:b.materialPhase==1?2:b.materialPhase<0?0:b.materialPhase);
                if(a.state==0&&!a.falling&&!b.initialSettling&&!b.collectorEntered&&d.sqrMagnitude>Mathf.Pow(Radius+6,2)&&(b.rigid.isKinematic||b.rigid.IsSleeping()||(b.rigid.linearVelocity.sqrMagnitude<.0009f&&b.rigid.angularVelocity.sqrMagnitude<.0009f)))ReleasePhysicalBody(a);
            }
            PrimeNextAuthoredBodies();
            Query(HolePosition,Radius+3,physicalCandidates);
            foreach(int i in widePhysicsActors)if(actors[i].state!=2&&!physicalCandidates.Contains(i))physicalCandidates.Add(i);
            physicalCandidates.Sort((x,y)=>actors[x].Bottom.CompareTo(actors[y].Bottom));
            int activated=0;
            foreach(int i in physicalCandidates){var a=actors[i];if(a.state==2)continue;Vector3 d=a.pos-HolePosition;d.y=0;
                var bounds=WorldBounds(a.model.bounds,a.rotation,a.scale);bounds.center+=a.pos;
                float dx=Mathf.Max(0,Mathf.Abs(bounds.center.x-HolePosition.x)-bounds.extents.x),dz=Mathf.Max(0,Mathf.Abs(bounds.center.z-HolePosition.z)-bounds.extents.z);
                if(dx*dx+dz*dz>Mathf.Pow(VisualRadius*.775f+2.5f,2))continue;
                var b=EnsurePhysicalBody(i);if(b==null)continue;
                if(a.falling||a.state==1||(State==ScreenState.Playing&&IntersectsSourceDetection(b))){
                    if(b.rigid.isKinematic&&activated++>=25)continue;
                    MakePhysical(b);SetContactMaterial(b,a.state==1?3:1);if(a.state==1)IgnoreFloor(b,true);
                }
            }
        }
        for(int k=falling.Count-1;k>=0;k--){int i=falling[k];var a=actors[i];
            if(a.state!=0||!a.falling){a.falling=false;falling.RemoveAt(k);continue;}
            var b=EnsurePhysicalBody(i);
            if(b!=null){MakePhysical(b);continue;}
            // Bounded fallback for remote unsupported pieces, using the same gravity.
            float surface=a.support>=0&&actors[a.support].state==0?Mathf.Max(0,actors[a.support].Top):0;
            a.fallSpeed+=ReferencePhysicsSettings.GravityMagnitude*dt;a.pos.y=Mathf.Max(surface-a.bottomOffset,a.pos.y-a.fallSpeed*dt);
            if(a.Bottom<=surface+.005f){a.falling=false;falling.RemoveAt(k);}else foreach(int child in a.above)Wake(child);
        }
        physicsAccumulator+=dt;
        while(physicsAccumulator>=PhysicsStep){
            foreach(var b in physicalBodies){if(b.actor==null)continue;b.previous=b.current;b.previousRotation=b.currentRotation;}
            physicsScene.Simulate(PhysicsStep);physicsAccumulator-=PhysicsStep;simulatedPhysicsTime+=PhysicsStep;
            foreach(var b in physicalBodies){var a=b.actor;if(a==null)continue;
                b.current=b.rigid.position;b.currentRotation=b.rigid.rotation;
                float oldY=a.pos.y;a.pos=b.current;a.rotation=b.currentRotation;
                Bounds bounds=default;bool hasBounds=false;foreach(var col in b.colliders)if(col.gameObject.activeInHierarchy&&col.enabled){if(!hasBounds){bounds=col.bounds;hasBounds=true;}else bounds.Encapsulate(col.bounds);}if(hasBounds){a.bottomOffset=bounds.min.y-a.pos.y;a.topOffset=bounds.max.y-a.pos.y;}
                // Source OnTriggerEnter is delivered at physics cadence. Keep that event even
                // if multiple ticks or a dense collection queue move the body past the box.
                if(a.state==0&&!b.collectorEntered&&SourceCollectorOverlaps(b))b.collectorEntered=true;
                long key=Key(Mathf.FloorToInt(a.pos.x/CellSize),Mathf.FloorToInt(a.pos.z/CellSize));
                if(key!=a.cellKey){int id=actors.IndexOf(a);if(cells.TryGetValue(a.cellKey,out var old))old.Remove(id);AddCell(id);}
                if(!b.initialSettling&&a.pos.y<oldY-.003f)foreach(int child in a.above)Wake(child);
                bool stable=b.rigid.linearVelocity.sqrMagnitude<.0009f&&b.rigid.angularVelocity.sqrMagnitude<.0009f;b.stableTime=stable?b.stableTime+PhysicsStep:0;
                if(a.falling&&a.state==0&&(b.rigid.IsSleeping()||b.stableTime>=.2f))a.falling=false;
            }
        }
    }
    Vector3 RenderPosition(Actor a)=>a.body==null?a.pos:Vector3.Lerp(a.body.previous,a.body.current,physicsAccumulator/PhysicsStep);
    Quaternion RenderRotation(Actor a)=>a.body==null?a.rotation:Quaternion.Slerp(a.body.previousRotation,a.body.currentRotation,physicsAccumulator/PhysicsStep);
}
