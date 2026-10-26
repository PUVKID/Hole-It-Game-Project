using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class ReferenceGame {
    // 원본처럼 모든 배치 물체에 충돌체와 강체를 유지합니다.
    // 초당 활성화 수 제한과 전체 강체 수 제한을 혼동하지 않습니다.
    int BodyBudget=>Mathf.Max(1,actors.Count);
    const float PhysicsStep=ReferencePhysicsSettings.FixedStep;
    class PhysicalBody {
        public GameObject root;public Rigidbody rigid;public readonly List<Collider> colliders=new List<Collider>();
        public Actor actor;public Vector3 previous,current;public Quaternion previousRotation,currentRotation;
        public int switchTier,selectedGroup=-1,materialPhase=-1;public bool initialSettling,collectorEntered;public float restAt,stableTime;
    }
    Scene physicalScene;PhysicsScene physicsScene;MeshCollider physicalFloor,physicalInnerFloor;
    PhysicsMaterial friction,lessFriction,dragFriction;float physicsAccumulator,activationClock,initialRestStarted,simulatedPhysicsTime;
    readonly Dictionary<Rigidbody,PhysicalBody> bodiesByRigid=new Dictionary<Rigidbody,PhysicalBody>();
    readonly Queue<PhysicalBody> pendingActivation=new Queue<PhysicalBody>();
    readonly HashSet<PhysicalBody> queuedActivation=new HashSet<PhysicalBody>();
    Transform activationTrigger,smoothTrigger,collectionTrigger;
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
            var floorObject=new GameObject("Source HoleCollider-Outer");floorObject.layer=20;SceneManager.MoveGameObjectToScene(floorObject,physicalScene);physicalFloor=floorObject.AddComponent<MeshCollider>();
            var innerObject=new GameObject("Source HoleCollider-Inner");innerObject.layer=20;SceneManager.MoveGameObjectToScene(innerObject,physicalScene);physicalInnerFloor=innerObject.AddComponent<MeshCollider>();physicalInnerFloor.sharedMaterial=friction;
            activationTrigger=CreateSourceTrigger("ObjectPhysicsDetection",ReferencePhysicsTrigger.TriggerKind.Activate);
            var capsule=activationTrigger.gameObject.AddComponent<CapsuleCollider>();capsule.radius=.49999997f;capsule.height=1.99999988f;capsule.isTrigger=true;
            smoothTrigger=CreateSourceTrigger("SmoothObjectDetection",ReferencePhysicsTrigger.TriggerKind.Smooth);
            var smooth=smoothTrigger.gameObject.AddComponent<BoxCollider>();smooth.size=new Vector3(1.2935724f,.40494776f,1.3469124f);smooth.center=new Vector3(.02995112f,-.20329030f,-.009540831f);smooth.isTrigger=true;
            collectionTrigger=CreateSourceTrigger("Cube-CollectionTrigger",ReferencePhysicsTrigger.TriggerKind.Collect);
            var collector=collectionTrigger.gameObject.AddComponent<BoxCollider>();collector.center=new Vector3(-.30063584f,-4.15507568f,.44971511f);collector.size=new Vector3(31.02136433f,2.96247728f,29.32730703f);collector.isTrigger=true;
        }
        foreach(var a in actors)ReleasePhysicalBody(a);
        physicsAccumulator=activationClock=simulatedPhysicsTime=0;pendingActivation.Clear();queuedActivation.Clear();widePhysicsActors.Clear();sourceFitRadii.Clear();
    }
    Transform CreateSourceTrigger(string name,ReferencePhysicsTrigger.TriggerKind kind){
        var root=new GameObject(name);root.layer=20;SceneManager.MoveGameObjectToScene(root,physicalScene);
        var trigger=root.AddComponent<ReferencePhysicsTrigger>();trigger.Owner=this;trigger.Kind=kind;return root.transform;
    }
    // 원본의 배치 순서를 유지하며 모든 물체의 정적인 지지 충돌체도 생성합니다.
    void ConfigureAuthoredPhysics(){
        if(Catalog.holeContactMesh==null)throw new System.InvalidOperationException("Original floor aperture mesh missing");
        physicalFloor.sharedMesh=Catalog.holeContactMesh;physicalInnerFloor.sharedMesh=Catalog.holeContactMesh;
        UpdateHoleContactGeometry();
        for(int i=0;i<actors.Count;i++){
            var a=actors[i];var bounds=WorldBounds(a.model.bounds,a.rotation,a.scale);
            if(Mathf.Max(bounds.extents.x,bounds.extents.z)>2.5f)widePhysicsActors.Add(i);
            if(EnsurePhysicalBody(i)==null)throw new System.InvalidOperationException("Authored body missing: "+i);
        }
    }
    void StepAuthoredRest(float dt){
        foreach(var b in physicalBodies)if(b.actor!=null&&b.initialSettling){StepPhysicalWorld(dt);break;}
    }
    void UpdateHoleContactGeometry(){
        var scale=growthAnimation.Scale;scale.x=Mathf.Max(.01f,scale.x);scale.y=Mathf.Max(.01f,scale.y);scale.z=Mathf.Max(.01f,scale.z);
        physicalFloor.transform.SetPositionAndRotation(HolePosition+Vector3.up*(-.041f+.05f*scale.y),Quaternion.identity);physicalFloor.transform.localScale=scale*.92f;
        physicalInnerFloor.transform.SetPositionAndRotation(HolePosition+Vector3.up*(-.041f+.025f*scale.y),Quaternion.identity);physicalInnerFloor.transform.localScale=scale*.915f;
        activationTrigger.SetPositionAndRotation(HolePosition+Vector3.down*.041f,Quaternion.identity);activationTrigger.localScale=Vector3.Scale(scale,new Vector3(1.55f,100,1.55f));
        smoothTrigger.SetPositionAndRotation(HolePosition+Vector3.down*.041f,Quaternion.identity);smoothTrigger.localScale=Vector3.Scale(scale,new Vector3(1,100,1));
        collectionTrigger.SetPositionAndRotation(HolePosition,Quaternion.identity);collectionTrigger.localScale=Vector3.one;
    }
    public void HandleReferenceTrigger(Collider other,ReferencePhysicsTrigger.TriggerKind kind,bool entered){
        var rigid=other.attachedRigidbody;
        if(rigid==null||!bodiesByRigid.TryGetValue(rigid,out var b)||b.actor==null)return;
        if(kind==ReferencePhysicsTrigger.TriggerKind.Collect){if(entered&&b.actor.state==0)b.collectorEntered=true;return;}
        if(kind==ReferencePhysicsTrigger.TriggerKind.Smooth){if(b.actor.state==0)SetContactMaterial(b,entered?1:2);return;}
        if(entered&&b.actor.state==0&&b.rigid.isKinematic&&queuedActivation.Add(b))pendingActivation.Enqueue(b);
    }
    public bool ReachedSourceCollectionTrigger(int index){var a=actors[index];return a.body!=null&&a.state==0&&a.body.collectorEntered;}
    void ProcessSourceActivations(){
        // 원본 HoleCapsuleCollider의 프레임당 최대 25개 활성화 규칙입니다.
        for(int count=0;count<25&&pendingActivation.Count>0;){
            var b=pendingActivation.Dequeue();queuedActivation.Remove(b);
            if(b.actor==null||b.actor.state!=0||!b.rigid.isKinematic)continue;
            MakePhysical(b);count++;
            foreach(int child in b.actor.above){var above=actors[child];if(above.body!=null)MakePhysical(above.body);}
        }
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
        var a=actors[i];ReleasePhysicalBody(a);a.pos=new Vector3(500,10,500);a.state=1;a.fallSpeed=0;var b=EnsurePhysicalBody(i);MakePhysical(b);return i;
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
        physicalBodies.Clear();spareBodies.Clear();bodiesByRigid.Clear();
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
            var root=new GameObject("Pooled Contact Body");root.layer=20;SceneManager.MoveGameObjectToScene(root,physicalScene);
            b=new PhysicalBody{root=root};b.rigid=root.AddComponent<Rigidbody>();b.rigid.isKinematic=true;
            
            b.rigid.solverIterations=ReferencePhysicsSettings.SolverIterations;b.rigid.solverVelocityIterations=ReferencePhysicsSettings.SolverVelocityIterations;b.rigid.maxAngularVelocity=ReferencePhysicsSettings.MaximumAngularSpeed;b.rigid.sleepThreshold=ReferencePhysicsSettings.CollectableSleepThreshold;b.rigid.maxDepenetrationVelocity=ReferencePhysicsSettings.MaximumDepenetrationSpeed;
            physicalBodies.Add(b);bodiesByRigid[b.rigid]=b;
        }
        b.actor=a;a.body=b;b.root.SetActive(false);b.rigid.isKinematic=false;b.rigid.linearVelocity=Vector3.zero;b.rigid.angularVelocity=Vector3.zero;b.rigid.isKinematic=true;
        b.rigid.useGravity=a.model.useGravity;b.rigid.interpolation=RigidbodyInterpolation.None;b.rigid.collisionDetectionMode=CollisionDetectionMode.Discrete;
        b.restAt=initialRestStarted+ReferencePhysicsSettings.RestDelay(a.model.id);b.initialSettling=ReferencePhysicsSettings.InitiallyDynamic(a.placement,a.model.id)&&Time.time<b.restAt;b.stableTime=0;b.collectorEntered=false;
        b.switchTier=ReferencePhysicsSettings.ColliderSwitchTier(a.placement,a.model,a.rotation,a.scale);b.selectedGroup=-1;b.materialPhase=-1;
        b.root.transform.SetPositionAndRotation(a.pos,a.rotation);b.root.transform.localScale=Vector3.one*a.scale;
        for(int c=0;c<a.model.collisionParts.Length;c++){
            var shape=a.model.collisionParts[c];System.Type type=shape.mesh.name=="Cube"?typeof(BoxCollider):shape.mesh.name=="Sphere"?typeof(SphereCollider):shape.mesh.name=="Capsule"?typeof(CapsuleCollider):typeof(MeshCollider);
            if(c==b.colliders.Count){var child=new GameObject("Authored Contact Shape");child.layer=20;child.transform.SetParent(b.root.transform,false);b.colliders.Add((Collider)child.AddComponent(type));}
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
        b.root.SetActive(true);RefreshBodyColliders(b);if(b.initialSettling)b.rigid.isKinematic=false;
        return b;
    }
    PhysicsMaterial SourceMaterial(int kind)=>kind==1?friction:kind==2?lessFriction:kind==3?dragFriction:null;
    void RefreshBodyColliders(PhysicalBody b){
        int group=ReferencePhysicsSettings.ColliderGroup(b.actor.placement,b.actor.model,b.switchTier,Mathf.Clamp(AppliedGrowthTier,0,10));
        if(group==b.selectedGroup)return;b.selectedGroup=group;
        for(int c=0;c<b.actor.model.collisionParts.Length;c++)b.colliders[c].enabled=ReferencePhysicsSettings.ColliderEnabled(b.actor.model.collisionParts[c],group);
        
    }
    void SetContactMaterial(PhysicalBody b,int phase){
        if(b.materialPhase==phase)return;b.materialPhase=phase;
        for(int c=0;c<b.actor.model.collisionParts.Length;c++)b.colliders[c].sharedMaterial=phase==3?friction:phase==1?lessFriction:phase==2?null:SourceMaterial(b.actor.model.collisionParts[c].materialKind);
    }
    void MakePhysical(PhysicalBody b){b.initialSettling=false;b.rigid.useGravity=true;b.rigid.interpolation=RigidbodyInterpolation.Interpolate;b.rigid.isKinematic=false;}
    void ReleasePhysicalBody(Actor a){
        var b=a.body;if(b==null)return;
        b.rigid.isKinematic=true;b.root.SetActive(false);b.actor=null;a.body=null;spareBodies.Push(b);
    }
    void BeginPhysicalDrop(int index){
        var b=EnsurePhysicalBody(index);if(b==null)return;
        SetContactMaterial(b,3);
    }
    void StepPhysicalWorld(float dt){
        UpdateHoleContactGeometry();Physics.SyncTransforms();ProcessSourceActivations();
        foreach(var b in physicalBodies){
            if(b.actor==null)continue;
            if(b.initialSettling&&Time.time>=b.restAt){b.initialSettling=false;b.rigid.isKinematic=true;}
            RefreshBodyColliders(b);
        }
        // 원격 물체도 충돌체를 유지합니다. 위치를 직접 내려 보내는 대체 낙하는 없습니다.
        for(int k=falling.Count-1;k>=0;k--){var a=actors[falling[k]];
            if(a.state!=0||!a.falling){a.falling=false;falling.RemoveAt(k);continue;}
            if(a.body!=null)MakePhysical(a.body);
        }
        physicsAccumulator+=dt;
        while(physicsAccumulator>=PhysicsStep){
            foreach(var b in physicalBodies){if(b.actor==null)continue;b.previous=b.current;b.previousRotation=b.currentRotation;}
            physicsScene.Simulate(PhysicsStep);physicsAccumulator-=PhysicsStep;simulatedPhysicsTime+=PhysicsStep;
            foreach(var b in physicalBodies){var a=b.actor;if(a==null)continue;
                b.current=b.rigid.position;b.currentRotation=b.rigid.rotation;
                a.pos=b.current;a.rotation=b.currentRotation;
                Bounds bounds=default;bool hasBounds=false;foreach(var col in b.colliders)if(col.gameObject.activeInHierarchy&&col.enabled){if(!hasBounds){bounds=col.bounds;hasBounds=true;}else bounds.Encapsulate(col.bounds);}if(hasBounds){a.bottomOffset=bounds.min.y-a.pos.y;a.topOffset=bounds.max.y-a.pos.y;}
                // 수집 여부는 실제 OnTriggerEnter에서 기록합니다. 경계 상자의 교차는 사용하지 않습니다.
                long key=Key(Mathf.FloorToInt(a.pos.x/CellSize),Mathf.FloorToInt(a.pos.z/CellSize));
                if(key!=a.cellKey){int id=actors.IndexOf(a);if(cells.TryGetValue(a.cellKey,out var old))old.Remove(id);AddCell(id);}

                bool stable=b.rigid.linearVelocity.sqrMagnitude<.0009f&&b.rigid.angularVelocity.sqrMagnitude<.0009f;b.stableTime=stable?b.stableTime+PhysicsStep:0;
                if(a.falling&&a.state==0&&(b.rigid.IsSleeping()||b.stableTime>=.2f))a.falling=false;
            }
        }
    }
    Vector3 RenderPosition(Actor a)=>a.body==null?a.pos:Vector3.Lerp(a.body.previous,a.body.current,physicsAccumulator/PhysicsStep);
    Quaternion RenderRotation(Actor a)=>a.body==null?a.rotation:Quaternion.Slerp(a.body.previousRotation,a.body.currentRotation,physicsAccumulator/PhysicsStep);
}
