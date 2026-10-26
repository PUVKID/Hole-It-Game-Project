using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class BombPlayTests
{
    private bool hadUnlock,hadRelaxed;private int oldUnlock,oldRelaxed;
    [UnitySetUp] public IEnumerator Setup(){
        hadUnlock=PlayerPrefs.HasKey("HoleIt.MaxLevel");oldUnlock=PlayerPrefs.GetInt("HoleIt.MaxLevel",1);
        hadRelaxed=PlayerPrefs.HasKey("PocketPit.Relaxed");oldRelaxed=PlayerPrefs.GetInt("PocketPit.Relaxed",0);
        PlayerPrefs.SetInt("HoleIt.MaxLevel",30);PlayerPrefs.SetInt("PocketPit.Relaxed",1);Time.timeScale=1;
        yield return SceneManager.LoadSceneAsync("Main");yield return null;
    }
    [UnityTearDown] public IEnumerator Cleanup(){
        Time.timeScale=1;var scene=SceneManager.GetSceneByName("Main");SceneManager.SetActiveScene(SceneManager.CreateScene("BombCleanup"));yield return SceneManager.UnloadSceneAsync(scene);
        if(hadUnlock)PlayerPrefs.SetInt("HoleIt.MaxLevel",oldUnlock);else PlayerPrefs.DeleteKey("HoleIt.MaxLevel");
        if(hadRelaxed)PlayerPrefs.SetInt("PocketPit.Relaxed",oldRelaxed);else PlayerPrefs.DeleteKey("PocketPit.Relaxed");
    }
    private IEnumerator Start(int level){var gm=GameManager.Instance;gm.SelectLevel(level);gm.SendMessage("HandlePlay");yield return WaitToPlay();InputManager.Instance.enabled=false;}
    private IEnumerator WaitToPlay(){float limit=Time.realtimeSinceStartup+5;while(GameManager.Instance.State==GameState.Ready&&Time.realtimeSinceStartup<limit)yield return null;Assert.AreEqual(GameState.Playing,GameManager.Instance.State);}
    private EdibleObject Bomb(){return Object.FindObjectsByType<EdibleObject>(FindObjectsSortMode.None).First(e=>e.Data.objectType==ObjectType.Bomb);}
    private void PutInside(EdibleObject bomb){bomb.transform.position=Object.FindFirstObjectByType<HoleController>().Position+Vector3.up*.02f;Physics.SyncTransforms();}
    private IEnumerator WaitForFailure(){float limit=Time.realtimeSinceStartup+3;while(GameManager.Instance.State==GameState.Playing&&Time.realtimeSinceStartup<limit)yield return null;Assert.AreEqual(GameState.Failed,GameManager.Instance.State);}
    [UnityTest] public IEnumerator BombFailsInRelaxedModeAndRetryRestoresLevel(){
        yield return Start(0);var gm=GameManager.Instance;var hole=Object.FindFirstObjectByType<HoleController>();var bomb=Bomb();int score=gm.TotalScore;float growth=hole.Growth;
        string missions=Object.FindFirstObjectByType<MissionManager>().DescribeProgress();int session=gm.SessionId;
        // Nearby bombs should not be magnetized from outside the actual opening.
        bomb.transform.position=hole.Position+new Vector3(1.35f,.02f,0);Physics.SyncTransforms();yield return new WaitForSeconds(.35f);
        Assert.AreEqual(SwallowState.Normal,bomb.State);Assert.AreEqual(GameState.Playing,gm.State);
        PutInside(bomb);yield return WaitForFailure();Assert.IsFalse(hole.ControlEnabled);Assert.AreEqual(score,gm.TotalScore);Assert.AreEqual(growth,hole.Growth);
        Assert.AreEqual(missions,Object.FindFirstObjectByType<MissionManager>().DescribeProgress());
        gm.OnEdibleConsumed(bomb);Assert.AreEqual(score,gm.TotalScore);
        Assert.IsTrue(Object.FindObjectsByType<HoleItText>(FindObjectsSortMode.None).Any(t=>t.text=="BOOM! TRY AGAIN"));
        gm.SendMessage("HandleRetry");yield return WaitToPlay();Assert.Greater(gm.SessionId,session);Assert.AreEqual(0,gm.TotalScore);Assert.AreEqual(1,gm.SelectedLevel);
        Assert.AreEqual(2,Object.FindObjectsByType<EdibleObject>(FindObjectsSortMode.None).Count(e=>e.Data.objectType==ObjectType.Bomb&&e.State==SwallowState.Normal));
    }
    [UnityTest] public IEnumerator StrictMissionCannotBlockBombAndPauseFreezesSwallow(){
        yield return Start(26);var gm=GameManager.Instance;var bomb=Bomb();Assert.IsFalse(gm.IsBlocked(bomb.Data));Assert.IsTrue(gm.ReserveSwallow(bomb.Data));PutInside(bomb);
        float deadline=Time.realtimeSinceStartup+2;while(bomb.State==SwallowState.Normal&&Time.realtimeSinceStartup<deadline)yield return null;
        Assert.AreNotEqual(SwallowState.Normal,bomb.State);gm.Pause();var pos=bomb.transform.position;yield return new WaitForSecondsRealtime(.3f);
        Assert.AreEqual(pos,bomb.transform.position);Assert.AreEqual(GameState.Paused,gm.State);gm.Resume();yield return WaitForFailure();
    }
    [UnityTest] public IEnumerator FinalMissionCannotHideAnAlreadySwallowedBomb(){
        yield return Start(0);var gm=GameManager.Instance;var bomb=Bomb();PutInside(bomb);
        while(bomb.State==SwallowState.Normal)yield return null;
        var level=Object.FindFirstObjectByType<LevelManager>().Current;var missions=Object.FindFirstObjectByType<MissionManager>();
        foreach(var m in level.missions)for(int i=0;i<m.requiredCount;i++)missions.Register(m.target);
        Assert.AreEqual(GameState.Playing,gm.State,"Clear must wait for a pending bomb");yield return WaitForFailure();
    }
}
