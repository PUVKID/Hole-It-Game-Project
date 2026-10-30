from pathlib import Path
import json
import struct

ROOT=Path(__file__).resolve().parent.parent
SOURCE=ROOT/'Source'
path=SOURCE/'Assets/Scripts/ReferenceGame.Physics.cs'
s=path.read_text(encoding='utf-8-sig')
s=s.replace('    // Only the local contact region needs PhysX bodies. Distant authored stacks stay asleep.\n    // Runtime parameters and collider modes are recovered from the supplied native build.\n    const int BodyBudget=384;',
'''    // 원본처럼 모든 배치 물체에 충돌체와 강체를 유지합니다.
    // 초당 활성화 수 제한과 전체 강체 수 제한을 혼동하지 않습니다.
    int BodyBudget=>Mathf.Max(1,actors.Count);''')
s=s.replace('readonly List<int> authoredPhysicsQueue=new List<int>();',
'''readonly Dictionary<Rigidbody,PhysicalBody> bodiesByRigid=new Dictionary<Rigidbody,PhysicalBody>();
    readonly Queue<PhysicalBody> pendingActivation=new Queue<PhysicalBody>();
    readonly HashSet<PhysicalBody> queuedActivation=new HashSet<PhysicalBody>();
    Transform activationTrigger,smoothTrigger,collectionTrigger;''')
s=s.replace('initialSettling,collectorEntered;', 'initialSettling,collectorEntered;')
s=s.replace('var floorObject=new GameObject("Source HoleCollider-Outer");','var floorObject=new GameObject("Source HoleCollider-Outer");floorObject.layer=20;')
s=s.replace('var innerObject=new GameObject("Source HoleCollider-Inner");','var innerObject=new GameObject("Source HoleCollider-Inner");innerObject.layer=20;')
s=s.replace('physicalInnerFloor.sharedMaterial=friction;','''physicalInnerFloor.sharedMaterial=friction;
            activationTrigger=CreateSourceTrigger("ObjectPhysicsDetection",ReferencePhysicsTrigger.TriggerKind.Activate);
            var capsule=activationTrigger.gameObject.AddComponent<CapsuleCollider>();capsule.radius=.49999997f;capsule.height=1.99999988f;capsule.isTrigger=true;
            smoothTrigger=CreateSourceTrigger("SmoothObjectDetection",ReferencePhysicsTrigger.TriggerKind.Smooth);
            var smooth=smoothTrigger.gameObject.AddComponent<BoxCollider>();smooth.size=new Vector3(1.2935724f,.40494776f,1.3469124f);smooth.center=new Vector3(.02995112f,-.20329030f,-.009540831f);smooth.isTrigger=true;
            collectionTrigger=CreateSourceTrigger("Cube-CollectionTrigger",ReferencePhysicsTrigger.TriggerKind.Collect);
            var collector=collectionTrigger.gameObject.AddComponent<BoxCollider>();collector.center=new Vector3(-.30063584f,-4.15507568f,.44971511f);collector.size=new Vector3(31.02136433f,2.96247728f,29.32730703f);collector.isTrigger=true;''')
s=s.replace('authoredPhysicsQueue.Clear();widePhysicsActors.Clear();sourceFitRadii.Clear();','pendingActivation.Clear();queuedActivation.Clear();widePhysicsActors.Clear();sourceFitRadii.Clear();')
start=s.index('    // Call after the authored actors')
end=s.index('    public Vector3 ActorAngularVelocityForTest',start)
s=s[:start]+'''    Transform CreateSourceTrigger(string name,ReferencePhysicsTrigger.TriggerKind kind){
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
''' +s[end:]
s=s.replace('physicalBodies.Clear();spareBodies.Clear();','physicalBodies.Clear();spareBodies.Clear();bodiesByRigid.Clear();')
s=s.replace('var root=new GameObject("Pooled Contact Body");','var root=new GameObject("Pooled Contact Body");root.layer=20;')
s=s.replace('physicalBodies.Add(b);','physicalBodies.Add(b);bodiesByRigid[b.rigid]=b;')
s=s.replace('var child=new GameObject("Authored Contact Shape");','var child=new GameObject("Authored Contact Shape");child.layer=20;')
s=s.replace('Mathf.Clamp(GrowthTier(GrowthPoints),0,10)','Mathf.Clamp(AppliedGrowthTier,0,10)')
s=s.replace('void MakePhysical(PhysicalBody b){b.initialSettling=false;b.rigid.interpolation=RigidbodyInterpolation.Interpolate;b.rigid.isKinematic=false;}',
'''void MakePhysical(PhysicalBody b){b.initialSettling=false;b.rigid.useGravity=true;b.rigid.interpolation=RigidbodyInterpolation.Interpolate;b.rigid.isKinematic=false;}''')
start=s.index('    void StepPhysicalWorld(float dt){')
end=s.index('        physicsAccumulator+=dt;',start)
s=s[:start]+'''    void StepPhysicalWorld(float dt){
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
''' +s[end:]
start=s.index('                // Source OnTriggerEnter is delivered')
end=s.index('                long key=Key(',start)
s=s[:start]+'''                // 수집 여부는 실제 OnTriggerEnter에서 기록합니다. 경계 상자의 교차는 사용하지 않습니다.
''' +s[end:]
s=s.replace('                if(!b.initialSettling&&a.pos.y<oldY-.003f)foreach(int child in a.above)Wake(child);','')
s=s.replace('float oldY=a.pos.y;a.pos=b.current;', 'a.pos=b.current;')
path.write_text(s,encoding='utf-8')

# 원본 레이어 충돌 행렬을 그대로 직렬화합니다.
physics=json.loads(Path(r'C:\Users\PUVKID\Desktop\Hole It\7차\Audit\OriginalPhysicsManager.json').read_text(encoding='utf-8-sig'))
matrix=''.join(struct.pack('<I',row).hex() for row in physics['m_LayerCollisionMatrix'])
path=SOURCE/'ProjectSettings/DynamicsManager.asset'
s=path.read_text(encoding='utf-8-sig')
import re
s=re.sub(r'm_LayerCollisionMatrix: [0-9a-f]+','m_LayerCollisionMatrix: '+matrix,s)
path.write_text(s,encoding='utf-8')
print('물리 수정 적용 완료')
