
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
public class CampaignPlayModeTests
{
    private bool hadUnlock;private int oldUnlock;
    [UnitySetUp] public IEnumerator Setup(){hadUnlock=PlayerPrefs.HasKey("HoleIt.MaxLevel");oldUnlock=PlayerPrefs.GetInt("HoleIt.MaxLevel",1);PlayerPrefs.SetInt("HoleIt.MaxLevel",30);Time.timeScale=1;yield return SceneManager.LoadSceneAsync("Main",LoadSceneMode.Single);yield return null;Assert.IsNotNull(GameManager.Instance);}
    [UnityTearDown] public IEnumerator Teardown(){Time.timeScale=1;var scene=SceneManager.GetSceneByName("Main");if(scene.IsValid()&&scene.isLoaded){var cleanup=SceneManager.CreateScene("HoleItTestCleanup");SceneManager.SetActiveScene(cleanup);yield return SceneManager.UnloadSceneAsync(scene);}if(hadUnlock)PlayerPrefs.SetInt("HoleIt.MaxLevel",oldUnlock);else PlayerPrefs.DeleteKey("HoleIt.MaxLevel");PlayerPrefs.Save();yield return null;}
    [UnityTest] public IEnumerator EveryLevelLoadsWithSavedPlacements(){var manager=Object.FindFirstObjectByType<LevelManager>();for(int i=0;i<30;i++){GameManager.Instance.SelectLevel(i);yield return null;Assert.IsNotNull(manager.Current);Assert.AreEqual(i+1,manager.Current.levelId);Assert.AreEqual(GameState.Title,GameManager.Instance.State);Assert.Greater(ObjectPooler.Instance.ActiveCount,0);}}
    private IEnumerator StartTimedLevel(){GameManager.Instance.SelectLevel(2);GameManager.Instance.SendMessage("HandlePlay");float deadline=Time.realtimeSinceStartup+5;while(GameManager.Instance.State==GameState.Ready&&Time.realtimeSinceStartup<deadline)yield return null;Assert.AreEqual(GameState.Playing,GameManager.Instance.State);}
    [UnityTest] public IEnumerator PauseFreezesTimerAndInputThenResumes(){yield return StartTimedLevel();var gm=GameManager.Instance;gm.Pause();float before=gm.TimeRemaining;yield return new WaitForSecondsRealtime(.12f);Assert.AreEqual(GameState.Paused,gm.State);Assert.AreEqual(before,gm.TimeRemaining);Assert.AreEqual(Vector2.zero,InputManager.Instance.MoveVector);gm.Resume();yield return new WaitForSeconds(.08f);Assert.Less(gm.TimeRemaining,before);}
    [UnityTest] public IEnumerator SwallowCompletionRewardsExactlyOnce(){
        yield return StartTimedLevel();var gm=GameManager.Instance;var hole=Object.FindFirstObjectByType<HoleController>();hole.SetControlEnabled(false);
        var all=Object.FindObjectsByType<EdibleObject>(FindObjectsInactive.Exclude,FindObjectsSortMode.None);EdibleObject edible=null;
        foreach(var e in all)if(e.Data.sizeTier==1&&e.Data.objectType!=ObjectType.Bomb){edible=e;break;}Assert.IsNotNull(edible);
        edible.transform.position=hole.Position+Vector3.right*.1f;int score=gm.TotalScore;float growth=hole.Growth;
        edible.BeginAttract(hole.transform);edible.Tick(.1f,hole.Settings,hole.Radius);edible.Tick(.6f,hole.Settings,hole.Radius);
        int after=gm.TotalScore;float afterGrowth=hole.Growth;Assert.Greater(after,score);Assert.Greater(afterGrowth,growth);
        edible.Consume();gm.OnEdibleConsumed(edible);Assert.AreEqual(after,gm.TotalScore);Assert.AreEqual(afterGrowth,hole.Growth);yield return null;
    }
    [UnityTest] public IEnumerator StrictModeBlocksFodderAndCompletedTargets(){
        var gm=GameManager.Instance;gm.SelectLevel(26);var level=Object.FindFirstObjectByType<LevelManager>().Current;var missions=Object.FindFirstObjectByType<MissionManager>();
        EdibleData candy=level.missions[0].target;EdibleData wrong=null;foreach(var p in level.placements)if(p.edible.objectType!=ObjectType.Bomb&&!missions.IsTarget(p.edible)){wrong=p.edible;break;}
        Assert.IsNotNull(wrong);Assert.IsTrue(gm.IsBlocked(wrong));Assert.IsFalse(gm.IsBlocked(candy));
        for(int i=0;i<level.missions[0].requiredCount;i++)missions.Register(candy);Assert.IsTrue(gm.IsBlocked(candy));yield return null;
    }
    [UnityTest] public IEnumerator RepeatedRestartAndHomeRetainSingleManagers(){yield return StartTimedLevel();for(int i=0;i<5;i++){GameManager.Instance.Pause();GameManager.Instance.SendMessage("HandleRetry");yield return null;GameManager.Instance.SendMessage("HandleHome");yield return null;Assert.AreEqual(GameState.Title,GameManager.Instance.State);Assert.AreEqual(1,Object.FindObjectsByType<GameManager>(FindObjectsSortMode.None).Length);GameManager.Instance.SendMessage("HandlePlay");yield return new WaitForSeconds(1.3f);}}
}
