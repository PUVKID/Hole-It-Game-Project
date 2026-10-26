using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class ReferenceSixthContentTests {
    [UnityTest]public IEnumerator RandomPoisonDoesNotReplaceThinPlateSupports(){
        var g=ReferenceGame.Instance;g.LoadLevel(17);
        Assert.AreEqual(2,g.BombPositionsForTest().Length);
        Assert.AreEqual(0,g.RandomBombOverlapCountForTest(),"Poison must not intersect the thin plate stacks or other remaining collectibles");
        int authored=g.Catalog.levels[17].points.Length;
        Assert.That(g.CollectionTarget,Is.InRange(authored-2,authored),"Only actual replaced items may leave the collection target");
        yield return null;
    }
    readonly Dictionary<string,string> prefs=new Dictionary<string,string>();
    [UnitySetUp]public IEnumerator Setup(){
        foreach(string key in new[]{"Gaeun.Ref.Selected","Gaeun.Ref.Unlocked"})prefs[key]=PlayerPrefs.HasKey(key)?PlayerPrefs.GetInt(key).ToString():null;
        yield return SceneManager.LoadSceneAsync("Main");yield return new WaitUntil(()=>ReferenceGame.Instance!=null&&ReferenceGame.Instance.Ready);ReferenceGame.Instance.TestInput=true;
    }
    [UnityTearDown]public IEnumerator Cleanup(){
        var scene=SceneManager.GetSceneByName("Main");SceneManager.SetActiveScene(SceneManager.CreateScene("SixthContentCleanup"));yield return SceneManager.UnloadSceneAsync(scene);
        foreach(var p in prefs)if(p.Value==null)PlayerPrefs.DeleteKey(p.Key);else PlayerPrefs.SetInt(p.Key,int.Parse(p.Value));
    }
    [UnityTest,Timeout(600000)]public IEnumerator AuthoredBombsPillarsAndThemesMatchBundledData(){
        var g=ReferenceGame.Instance;Assert.AreEqual(465,g.Catalog.levels.Length);Assert.AreEqual(111,g.Catalog.challengeLevels.Length);Assert.AreEqual(1010,g.Catalog.models.Length);
        Assert.AreEqual(153,g.Catalog.levels.Sum(l=>l.obstacles.Length));Assert.AreEqual(35,g.Catalog.levels.Count(l=>l.obstacles.Length>0));
        foreach(var l in g.Catalog.levels){
            Assert.AreEqual(0,l.themeIndex);
            if(l.obstacles.Length==0)continue;
            g.LoadLevel(l.index-1);Assert.AreEqual(1,g.CurrentThemeVariant);Assert.AreEqual(.62f,g.SourceCharacterStartRadius,.0001f);
            var positions=g.BombPositionsForTest();Assert.AreEqual(l.obstacles.Length,positions.Length);
            for(int i=0;i<positions.Length;i++){var p=l.obstacles[i].position;Assert.Less(Vector3.Distance(new Vector3(p[0],p[1],p[2]),positions[i]),.0001f,"Stage "+l.index+" bomb "+i);}
            Assert.IsTrue(g.CanStandAt(g.HolePosition));yield return null;
        }
        g.LoadLevel(411);Assert.AreEqual(24,g.PillarCountForTest());Assert.IsTrue(g.Catalog.environments.Any(m=>m.id==9&&m.parts.Length>0));
    }
    [UnityTest,Timeout(600000)]public IEnumerator EveryExtraMapLoadsWithAvailableGoalsAndReachableStart(){
        var g=ReferenceGame.Instance;var ids=new HashSet<int>(g.Catalog.models.Select(m=>m.id));
        for(int i=0;i<g.Catalog.challengeLevels.Length;i++){
            g.LoadChallenge(i);var l=g.Catalog.challengeLevels[i];Assert.IsTrue(g.IsChallenge);Assert.AreEqual(i,g.ChallengeIndex);
            Assert.AreEqual(l.time,g.Remaining);Assert.IsTrue(g.CanStandAt(g.HolePosition),"Challenge "+(i+1));
            var counts=l.points.GroupBy(p=>p.id).ToDictionary(x=>x.Key,x=>x.Count());
            foreach(var goal in l.goals){Assert.IsTrue(ids.Contains(goal.id));Assert.GreaterOrEqual(counts[goal.id],goal.amount);}
            Assert.IsTrue(g.PhysicsTransformsFinite());yield return null;
        }
    }
    [UnityTest]public IEnumerator ChallengesDoNotAwardCurrencyOrChangeCampaignProgress(){
        var g=ReferenceGame.Instance;var type=typeof(ReferenceGame);var flags=BindingFlags.Instance|BindingFlags.NonPublic;
        var profile=(ReferenceProfile)type.GetField("profile",flags).GetValue(g);profile.freePlay=false;profile.coins=321;profile.hearts=4;profile.refillAt=long.MaxValue;
        type.GetField("unlocked",flags).SetValue(g,26);g.LoadLevel(5);int selected=PlayerPrefs.GetInt("Gaeun.Ref.Selected");g.LoadChallenge(0);
        Assert.IsTrue(g.FreePlay);type.GetMethod("RecordWin",flags).Invoke(g,null);type.GetMethod("RecordFailure",flags).Invoke(g,null);
        Assert.AreEqual(321,profile.coins);Assert.AreEqual(4,profile.hearts);Assert.AreEqual(26,type.GetField("unlocked",flags).GetValue(g));Assert.AreEqual(selected,PlayerPrefs.GetInt("Gaeun.Ref.Selected"));
        g.Retry();Assert.IsTrue(g.IsChallenge);Assert.AreEqual(0,g.ChallengeIndex);g.ShowHome();Assert.IsFalse(g.IsChallenge);Assert.IsFalse(g.FreePlay);Assert.AreEqual(5,g.LevelIndex);yield return null;
    }
}
