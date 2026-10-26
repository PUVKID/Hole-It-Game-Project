using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

public class ReferenceEighthPhysicsTests {
    ReferenceGame game;
    IList Actors=>(IList)typeof(ReferenceGame).GetField("actors",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(game);
    static object Field(object value,string name)=>value.GetType().GetField(name,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).GetValue(value);
    static void Set(object value,string name,object data)=>value.GetType().GetField(name,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).SetValue(value,data);
    [UnitySetUp]public IEnumerator Setup(){
        Time.timeScale=1;yield return SceneManager.LoadSceneAsync("Main");
        yield return new WaitUntil(()=>ReferenceGame.Instance!=null&&ReferenceGame.Instance.Ready);
        game=ReferenceGame.Instance;game.TestInput=true;game.LoadLevel(0);game.StartPlay();
    }
    [UnityTearDown]public IEnumerator Cleanup(){
        var main=SceneManager.GetSceneByName("Main");SceneManager.SetActiveScene(SceneManager.CreateScene("EighthCleanup"));yield return SceneManager.UnloadSceneAsync(main);Time.timeScale=1;
    }
    Rigidbody PrepareBox(int index,Vector3 position,Vector3 size,Quaternion rotation){
        var actor=Actors[index];var body=Field(actor,"body");
        var rigid=(Rigidbody)Field(body,"rigid");rigid.isKinematic=false;rigid.useGravity=false;
        rigid.position=position;rigid.rotation=rotation;rigid.transform.localScale=Vector3.one;
        var colliders=(List<Collider>)Field(body,"colliders");
        foreach(var c in colliders)c.enabled=false;
        // 실제 강체와 원본 수집 트리거를 사용하되 검사 물체의 형상만 상자로 대체합니다.
        var box=rigid.gameObject.AddComponent<BoxCollider>();box.size=size;
        Set(actor,"pos",position);Set(actor,"rotation",rotation);
        Set(body,"previous",position);Set(body,"current",position);Set(body,"previousRotation",rotation);Set(body,"currentRotation",rotation);
        Physics.SyncTransforms();return rigid;
    }
    [UnityTest]public IEnumerator AabbCornerOverlapDoesNotCountAsPhysicalCollection(){
        var center=game.HolePosition+new Vector3(-.30063584f,-4.15507568f,.44971511f);
        var collector=new Bounds(center,new Vector3(31.02136433f,2.96247728f,29.32730703f));
        var body=PrepareBox(0,new Vector3(collector.max.x+4,center.y,collector.max.z+4),new Vector3(15,.2f,.2f),Quaternion.Euler(0,45,0));
        Assert.IsTrue(collector.Intersects(body.GetComponent<BoxCollider>().bounds),"7차의 경계 상자 판정은 이 모서리를 잘못 수집합니다.");
        yield return new WaitForSeconds(.15f);
        Assert.AreEqual(0,game.Collected);Assert.IsFalse(game.ReachedSourceCollectionTrigger(0));Assert.IsTrue(body.gameObject.activeInHierarchy);
    }
    [UnityTest]public IEnumerator CollectingTallBodyKeepsItsVisiblePartAndRigidbody(){
        var body=PrepareBox(0,game.HolePosition+Vector3.up*.4f,new Vector3(.2f,8,.2f),Quaternion.identity);
        yield return new WaitForSeconds(.12f);
        Assert.AreEqual(1,game.Collected);Assert.AreEqual(3,(int)Field(Actors[0],"state"));
        Assert.IsTrue(body.gameObject.activeInHierarchy);Assert.IsNotNull(Field(Actors[0],"body"));
        Assert.Greater(body.GetComponent<BoxCollider>().bounds.max.y,0,"수집 구역에 닿아도 바닥 위의 몸체는 남아 있어야 합니다.");
        yield return new WaitForSeconds(.15f);Assert.AreEqual(1,game.Collected,"같은 물체를 중복 계산하지 않습니다.");
    }
    [UnityTest]public IEnumerator GrowthRingAndPhysicalExpansionFollowSourceDelayedCallback(){
        for(int i=0;i<9;i++){PrepareBox(i,game.HolePosition+Vector3.down*3.1f,new Vector3(.1f,.1f,.1f),Quaternion.identity);yield return new WaitForSeconds(.025f);}
        Assert.AreEqual(9,game.Collected);Assert.AreEqual(0,game.AppliedGrowthTier);Assert.AreEqual(.9f,game.GrowthProgressFraction,.0001f);
        var ring=Object.FindFirstObjectByType<ReferenceGrowthRing>();Assert.IsNotNull(ring);Assert.AreEqual(.9f,ring.Progress,.0001f);
        PrepareBox(9,game.HolePosition+Vector3.down*3.1f,new Vector3(.1f,.1f,.1f),Quaternion.identity);
        yield return new WaitForSeconds(.05f);Assert.AreEqual(10,game.Collected);Assert.AreEqual(1,game.CurrentGrowthTier);Assert.AreEqual(0,game.AppliedGrowthTier);
        yield return new WaitForSeconds(.32f);Assert.AreEqual(1,game.AppliedGrowthTier);Assert.AreEqual(.843f,game.Radius,.0001f);
    }
    [Test]public void LargePointAwardDoesNotSkipAllGrowthAnimationsInOneFrame(){
        var progress=new ReferenceGrowthProgress();progress.Reset();progress.Add(130);
        Assert.AreEqual(1,progress.Tier);Assert.AreEqual(0,progress.AppliedTier);Assert.AreEqual(120,progress.Remainder);
        Assert.IsFalse(progress.Advance(.299f));Assert.IsTrue(progress.Advance(.002f));
        Assert.AreEqual(1,progress.AppliedTier);Assert.AreEqual(2,progress.Tier);Assert.AreEqual(100,progress.Remainder);
        Assert.IsTrue(progress.Advance(.3f));Assert.AreEqual(2,progress.AppliedTier);Assert.AreEqual(3,progress.Tier);
    }
    [Test]public void DenseLayoutKeepsEveryAuthoredContactBody(){
        game.LoadLevel(15);Assert.AreEqual(2973,game.ActorCount);Assert.AreEqual(game.ActorCount,game.ActivePhysicsBodies);
        foreach(var actor in Actors)Assert.IsNotNull(Field(actor,"body"),"원격 지지 물체를 포함해 강체를 생략하지 않습니다.");
    }
    [Test]public void LayerCollisionPairsMatchSuppliedPhysicsManager(){
        Assert.IsFalse(Physics.GetIgnoreLayerCollision(20,20));Assert.IsFalse(Physics.GetIgnoreLayerCollision(20,21));
        Assert.IsTrue(Physics.GetIgnoreLayerCollision(20,14));Assert.IsTrue(Physics.GetIgnoreLayerCollision(20,17));
        Assert.IsTrue(Physics.GetIgnoreLayerCollision(21,21));
    }
}
