using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
public class ReferenceFifthTests {
    int oldUnlocked,oldSelected;bool hadUnlocked,hadSelected;
    [UnitySetUp]public IEnumerator Setup(){hadUnlocked=PlayerPrefs.HasKey("Gaeun.Ref.Unlocked");oldUnlocked=PlayerPrefs.GetInt("Gaeun.Ref.Unlocked");hadSelected=PlayerPrefs.HasKey("Gaeun.Ref.Selected");oldSelected=PlayerPrefs.GetInt("Gaeun.Ref.Selected");yield return SceneManager.LoadSceneAsync("Main");yield return new WaitUntil(()=>ReferenceGame.Instance!=null&&ReferenceGame.Instance.Ready);yield return new WaitForSecondsRealtime(.6f);ReferenceGame.Instance.TestInput=true;}
    [UnityTearDown]public IEnumerator Cleanup(){var scene=SceneManager.GetSceneByName("Main");SceneManager.SetActiveScene(SceneManager.CreateScene("FifthCleanup"));yield return SceneManager.UnloadSceneAsync(scene);if(hadUnlocked)PlayerPrefs.SetInt("Gaeun.Ref.Unlocked",oldUnlocked);else PlayerPrefs.DeleteKey("Gaeun.Ref.Unlocked");if(hadSelected)PlayerPrefs.SetInt("Gaeun.Ref.Selected",oldSelected);else PlayerPrefs.DeleteKey("Gaeun.Ref.Selected");}
    [UnityTest,Timeout(600000)]public IEnumerator EveryAuthoredStageLoadsWithGoalsAndReachableStart(){
        var g=ReferenceGame.Instance;Assert.AreEqual(465,g.Catalog.levels.Length);Assert.AreEqual(1010,g.Catalog.models.Length);
        var errors=new List<string>();var report=new System.Text.StringBuilder();var ids=g.Catalog.models.ToDictionary(m=>m.id);
        foreach(var m in g.Catalog.models){Assert.IsNotEmpty(m.parts,m.label);Assert.IsNotEmpty(m.collisionParts,m.label);foreach(var shape in m.collisionParts)Assert.IsNotNull(shape.mesh,m.label);}
        for(int i=0;i<g.Catalog.levels.Length;i++){
            g.LoadLevel(i);yield return null;var l=g.Catalog.levels[i];
            if(!g.CanStandAt(g.HolePosition))errors.Add("Level "+(i+1)+" unreachable start "+g.HolePosition);
            Assert.AreEqual(l.points.Length+g.BombPositionsForTest().Length,g.ActorCount);Assert.AreEqual(l.time,g.Remaining);
            var counts=l.points.GroupBy(p=>p.id).ToDictionary(x=>x.Key,x=>x.Count());foreach(var goal in l.goals){Assert.IsTrue(ids.ContainsKey(goal.id));Assert.GreaterOrEqual(counts[goal.id],goal.amount);}
            Assert.IsTrue(g.PhysicsTransformsFinite());report.AppendLine("Level "+(i+1)+": "+g.ActorCount+" actors; "+l.time+" sec; start valid="+g.CanStandAt(g.HolePosition));
        }
        Directory.CreateDirectory("TestResults");File.WriteAllText("TestResults/FifthStageAudit.txt",report.ToString());Assert.IsEmpty(errors,string.Join("\n",errors));
    }
    [UnityTest]public IEnumerator FreeFallUsesReferenceGravityAndPauseFreezesPhysics(){
        var g=ReferenceGame.Instance;g.LoadLevel(0);g.StartPlay();int index=g.StartIsolatedFallForTest();float start=Time.time;
        yield return new WaitForSeconds(.4f);float elapsed=Time.time-start;var p=g.ActorPositionForTest(index);
        Assert.AreEqual(500,p.x,.001f);Assert.AreEqual(500,p.z,.001f);Assert.AreEqual(10-.5f*5.81f*elapsed*elapsed,p.y,.13f);Assert.AreEqual(-5.81f,Physics.gravity.y,.001f);
        g.Pause();p=g.ActorPositionForTest(index);yield return new WaitForSeconds(.2f);Assert.AreEqual(p,g.ActorPositionForTest(index));
    }
    [UnityTest,Timeout(300000)]public IEnumerator DenseAndLateStagesUseBoundedStableContacts(){
        var g=ReferenceGame.Instance;
        foreach(int level in new[]{3,15,28,90,199,300,461,464}){
            g.LoadLevel(level);g.StartPlay();for(int i=0;i<150;i++){if(i%25==0)g.MoveHoleForTest(g.NearestCollectible());yield return null;Assert.IsTrue(g.PhysicsTransformsFinite(),"Level "+(level+1));Assert.LessOrEqual(g.ActivePhysicsBodies,384);}
            Assert.Greater(g.Collected,0,"Stage "+(level+1)+" must actually collect items");
        }
    }
    [UnityTest]public IEnumerator CotonArtAndOriginalSoundsAreBundled(){
        foreach(string n in new[]{"Loading","Home"}){var t=Resources.Load<Texture2D>("CotonArt/"+n);Assert.IsNotNull(t);Assert.GreaterOrEqual(t.height,1600);Assert.GreaterOrEqual(t.width,900);}
        Assert.AreEqual(10,ReferenceGame.Instance.Catalog.sounds.Length);foreach(var clip in ReferenceGame.Instance.Catalog.sounds)Assert.IsNotNull(clip);yield return null;
    }
    [UnityTest]public IEnumerator MovementReachesAuthoredFloorBeyondInitialItemBounds(){
        var g=ReferenceGame.Instance;g.LoadLevel(17);g.StartPlay();g.MoveHoleForTest(new Vector3(21,0,-10));Assert.IsTrue(g.CanStandAt(g.HolePosition));g.TestDirection=Vector2.right;yield return new WaitForSeconds(.5f);g.TestDirection=Vector2.zero;Assert.Greater(g.HolePosition.x,22.5f,"Arena floor, not initial item extents, must bound movement");Assert.IsTrue(g.CanStandAt(g.HolePosition));
    }
}
