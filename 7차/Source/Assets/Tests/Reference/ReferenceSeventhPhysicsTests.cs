using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ReferenceSeventhPhysicsTests {
    ReferenceCatalog Catalog=>Resources.Load<ReferenceCatalog>("ReferenceCatalog");

    [Test] public void NativeReleaseSettingsDifferFromSerializedAndDebugGravity(){
        ReferencePhysicsSettings.ApplyGlobalSettings();
        Assert.AreEqual(new Vector3(0,-35,0),Physics.gravity);
        Assert.AreEqual(.02f,Time.fixedDeltaTime,.000001f);Assert.AreEqual(.33333334f,Time.maximumDeltaTime,.000001f);
        Assert.AreEqual(6,Physics.defaultSolverIterations);Assert.AreEqual(1,Physics.defaultSolverVelocityIterations);
        Assert.AreEqual(.005f,Physics.sleepThreshold);Assert.AreEqual(0,ReferencePhysicsSettings.CollectableSleepThreshold);
        Assert.AreEqual(7,Physics.defaultMaxAngularSpeed);Assert.AreEqual(10,Physics.defaultMaxDepenetrationVelocity);
        Assert.AreEqual(.01f,Physics.defaultContactOffset);Assert.AreEqual(2,Physics.bounceThreshold);
        Assert.AreEqual(0,ReferencePhysicsSettings.LinearDamping);Assert.AreEqual(1,ReferencePhysicsSettings.AngularDamping);
    }
    [Test] public void ObstacleAwakeOverridesPrefabDampingAndUsesItsOwnRestDelay(){
        Assert.AreEqual(2f,ReferencePhysicsSettings.RestDelay(-1));Assert.AreEqual(1.5f,ReferencePhysicsSettings.RestDelay(42));
        var root=new GameObject("Obstacle native settings probe");
        try{var body=root.AddComponent<Rigidbody>();body.linearDamping=2.5f;body.angularDamping=.05f;ReferencePhysicsSettings.ApplyRuntimeDamping(body);Assert.AreEqual(0,body.linearDamping);Assert.AreEqual(1,body.angularDamping);}
        finally{Object.DestroyImmediate(root);}
    }
    [Test] public void NativePlacementMassSentinelsAndAuthoredGrowthAreRetained(){
        var levels=Catalog.levels;var models=Catalog.models.ToDictionary(m=>m.id);
        var positive=levels.SelectMany(l=>l.points).Where(p=>p.massOverride==50).ToArray();
        var negative=levels.SelectMany(l=>l.points).Where(p=>p.massOverride==-3.25f).ToArray();
        Assert.AreEqual(923,positive.Length);Assert.AreEqual(51,negative.Length);
        foreach(var p in positive)Assert.AreEqual(50,ReferencePhysicsSettings.Mass(p));
        foreach(var p in negative)Assert.AreEqual(1,ReferencePhysicsSettings.Mass(p));
        var inherited=levels.SelectMany(l=>l.points).Where(p=>p.points==0&&models[p.id].objectSize>0).ToArray();
        Assert.AreEqual(1148,inherited.Length,"Source mS==0 preserves prefab objectSize");
        foreach(var p in inherited)Assert.AreEqual(models[p.id].objectSize,ReferencePhysicsSettings.GrowthValue(p,models[p.id]));
        Assert.AreEqual(0,ReferencePhysicsSettings.GrowthValue(new ReferencePoint{points=-1},models[10]));
        Assert.IsFalse(ReferencePhysicsSettings.InitiallyDynamic(new ReferencePoint{physicsMode=1},59));
        Assert.IsTrue(ReferencePhysicsSettings.InitiallyDynamic(new ReferencePoint{physicsMode=1},10));
        Assert.IsFalse(ReferencePhysicsSettings.InitiallyDynamic(new ReferencePoint{physicsMode=0},10));
    }
    [Test] public void SourceCompoundAndSpecialColliderMembershipIsRecovered(){
        var models=Catalog.models.ToDictionary(m=>m.id);
        Assert.AreEqual(2535,Catalog.models.Sum(m=>m.collisionParts.Length)+Catalog.bomb.collisionParts.Length);
        Assert.AreEqual(40,Catalog.models.SelectMany(m=>m.collisionParts).Count(c=>!c.sourceEnabled));
        var p=new ReferencePoint{holeColliderActive=1,holeColliderLevel=3};var bag=models[10];
        int threshold=ReferencePhysicsSettings.ColliderSwitchTier(p,bag,Quaternion.identity,1);
        Assert.AreEqual(3,threshold);Assert.AreEqual(1,ReferencePhysicsSettings.ColliderGroup(p,bag,threshold,2));
        Assert.AreEqual(2,ReferencePhysicsSettings.ColliderGroup(p,bag,threshold,3));
        Assert.AreEqual(8,bag.collisionParts.Count(c=>(c.group&1)!=0));Assert.AreEqual(1,bag.collisionParts.Count(c=>(c.group&2)!=0));
        var basket=models[68];p.specialCollision=1;
        Assert.AreEqual(4,ReferencePhysicsSettings.ColliderGroup(p,basket,3,0));
        Assert.AreEqual(19,basket.collisionParts.Count(c=>(c.group&4)!=0));
        Assert.IsTrue(ReferencePhysicsSettings.ColliderEnabled(new ReferenceCollision{group=3},1));
        Assert.IsTrue(ReferencePhysicsSettings.ColliderEnabled(new ReferenceCollision{group=3},2));
        Assert.IsFalse(ReferencePhysicsSettings.ColliderEnabled(new ReferenceCollision{group=1},2));
    }
    [Test] public void SourceFloorMeshHasPhysicalApertureAndOriginalExtent(){
        var mesh=Catalog.holeContactMesh;Assert.IsNotNull(mesh);Assert.AreEqual(664,mesh.vertexCount);
        Assert.AreEqual(384,mesh.triangles.Length/3);Assert.AreEqual(551.2423f,mesh.bounds.size.x,.001f);
        Assert.AreEqual(-8.59572f,mesh.bounds.min.y,.0001f);
        float minimumTopRadius=mesh.vertices.Where(v=>v.y>-.001f).Min(v=>new Vector2(v.x,v.z).magnitude);
        Assert.AreEqual(.83923f,minimumTopRadius,.0001f);
    }
    [Test] public void RecoveredComplexContactsStayWithinNativeConvexCookingLimit(){
        int[] ids={14,34,115,116};string[] keys={"G-4494295972605348666","G-4952678351463023605","G8186878611941021423","G4136433066467917765"};
        for(int i=0;i<ids.Length;i++){
            var model=Catalog.models.Single(m=>m.id==ids[i]);
            var contact=model.collisionParts.Single(c=>c.mesh.name==keys[i]||c.mesh.name==keys[i]+"_BoundedHull");
            Assert.LessOrEqual(contact.mesh.vertexCount,120,model.label);
            Assert.LessOrEqual(contact.mesh.triangles.Length/3,240,model.label);
            Assert.AreEqual(236,contact.mesh.triangles.Length/3,model.label);
        }
    }
    [TestCase(.2f,0f,true)]
    [TestCase(2f,0f,false)]
    [TestCase(.2f,2f,false)]
    public void ActualSourceApertureDropsOnlyBodiesThatPhysicallyFit(float size,float offset,bool shouldFall){
        ReferencePhysicsSettings.ApplyGlobalSettings();
        var scene=SceneManager.CreateScene("SeventhApertureProbe_"+System.Guid.NewGuid().ToString("N"),new CreateSceneParameters(LocalPhysicsMode.Physics3D));
        var physics=scene.GetPhysicsScene();var roots=new System.Collections.Generic.List<GameObject>();
        try {
            foreach(var shape in new[]{new Vector2(.92f,.05f),new Vector2(.915f,.025f)}){
                var floor=new GameObject("Source floor probe");SceneManager.MoveGameObjectToScene(floor,scene);roots.Add(floor);
                floor.transform.position=Vector3.up*(-.041f+shape.y*.7f);floor.transform.localScale=Vector3.one*(shape.x*.7f);
                floor.AddComponent<MeshCollider>().sharedMesh=Catalog.holeContactMesh;
            }
            var item=new GameObject("Probe",typeof(BoxCollider),typeof(Rigidbody));SceneManager.MoveGameObjectToScene(item,scene);roots.Add(item);
            item.transform.localScale=Vector3.one*size;item.transform.position=new Vector3(offset,1+size*.5f,0);
            var body=item.GetComponent<Rigidbody>();body.mass=1;body.linearDamping=0;body.angularDamping=1;body.sleepThreshold=0;
            Physics.SyncTransforms();for(int tick=0;tick<40;tick++)physics.Simulate(.02f);
            if(shouldFall)Assert.Less(body.position.y,-2.673837f,"The genuine mesh aperture must allow gravity, without IgnoreCollision or an imposed drop");
            else {
                Assert.Greater(body.position.y,size*.4f-.04f);Assert.AreEqual(offset,body.position.x,.03f);
                if(offset>0){
                    // Moving the source floor/aperture must not drag a missed object with it.
                    roots[0].transform.position+=Vector3.right*8;roots[1].transform.position+=Vector3.right*8;
                    Physics.SyncTransforms();for(int tick=0;tick<30;tick++)physics.Simulate(.02f);
                    Assert.AreEqual(offset,body.position.x,.03f);Assert.Greater(body.position.y,.04f);
                }
            }
        } finally {foreach(var root in roots)Object.DestroyImmediate(root);SceneManager.UnloadSceneAsync(scene);}
    }
    [Test] public void ZeroDeadZoneJoystickKeepsStationaryTouchFinite(){
        var stick=new FloatingStick();Assert.AreEqual(0,stick.DeadZone);stick.Begin(new Vector2(10,20));stick.Move(new Vector2(10,20));Assert.AreEqual(Vector2.zero,stick.Value);
        stick.Move(new Vector2(11,20));Assert.Greater(stick.Value.x,0);Assert.AreEqual(0,stick.Value.y);
    }
}
