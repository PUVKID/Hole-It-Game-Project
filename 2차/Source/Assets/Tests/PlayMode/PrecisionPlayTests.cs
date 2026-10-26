using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class PrecisionPlayTests
{
    private int oldMode,oldRelaxed;private bool hadMode,hadRelaxed;
    private void Invoke(object target,string method,params object[] args){target.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(target,args);}
    [UnitySetUp] public IEnumerator Setup(){
        hadMode=PlayerPrefs.HasKey("HoleIt.ControlMode");oldMode=PlayerPrefs.GetInt("HoleIt.ControlMode");hadRelaxed=PlayerPrefs.HasKey("PocketPit.Relaxed");oldRelaxed=PlayerPrefs.GetInt("PocketPit.Relaxed");
        PlayerPrefs.SetInt("HoleIt.ControlMode",0);PlayerPrefs.SetInt("PocketPit.Relaxed",0);Time.timeScale=1;
        yield return SceneManager.LoadSceneAsync("Main");yield return null;var gm=GameManager.Instance;gm.SelectLevel(0);gm.SendMessage("HandlePlay");while(gm.State==GameState.Ready)yield return null;
        Assert.AreEqual(GameState.Playing,gm.State);
    }
    [UnityTearDown] public IEnumerator Cleanup(){
        Time.timeScale=1;var scene=SceneManager.GetSceneByName("Main");SceneManager.SetActiveScene(SceneManager.CreateScene("PrecisionCleanup"));yield return SceneManager.UnloadSceneAsync(scene);
        if(hadMode)PlayerPrefs.SetInt("HoleIt.ControlMode",oldMode);else PlayerPrefs.DeleteKey("HoleIt.ControlMode");if(hadRelaxed)PlayerPrefs.SetInt("PocketPit.Relaxed",oldRelaxed);else PlayerPrefs.DeleteKey("PocketPit.Relaxed");
    }
    [UnityTest] public IEnumerator DragTracksDisplacementWithoutJumpDriftOrReleaseSlide(){
        var input=InputManager.Instance;input.enabled=false;var hole=Object.FindFirstObjectByType<HoleController>();var pos=new Vector2(Screen.width*.5f,Screen.height*.45f);var before=hole.Position;
        Invoke(input,"Begin",pos,-1);yield return null;Assert.Less(Vector3.Distance(before,hole.Position),.001f,"Pointer down must not teleport the hole");
        var plane=new Plane(Vector3.up,Vector3.zero);var a=Camera.main.ScreenPointToRay(pos);var b=Camera.main.ScreenPointToRay(pos+Vector2.right*160);plane.Raycast(a,out float da);plane.Raycast(b,out float db);
        var expected=hole.ClampPosition(before+(b.GetPoint(db)-a.GetPoint(da))*.85f);
        Invoke(input,"Drag",pos+Vector2.right*160);yield return new WaitForSeconds(.5f);
        Assert.Less(Vector3.Distance(expected,hole.Position),.035f,"Ground-plane mapped target must be reached accurately");
        var stopped=hole.Position;Invoke(input,"Drag",pos+Vector2.right*160);yield return new WaitForSeconds(.3f);Assert.Less(Vector3.Distance(stopped,hole.Position),.001f,"Stationary finger must not keep moving");
        Invoke(input,"Release");yield return new WaitForSeconds(.2f);Assert.Less(Vector3.Distance(stopped,hole.Position),.001f,"Release must stop immediately");Assert.AreEqual(Vector2.zero,hole.Velocity);
    }
    [UnityTest] public IEnumerator SwallowedObjectDropsAtEntryInsteadOfFollowingMovingHole(){
        var hole=Object.FindFirstObjectByType<HoleController>();hole.SetControlEnabled(false);InputManager.Instance.enabled=false;
        var e=Object.FindObjectsByType<EdibleObject>(FindObjectsSortMode.None).First(x=>x.Data.objectType!=ObjectType.Bomb&&x.Data.sizeTier==1);
        e.transform.position=hole.Position+new Vector3(.1f,.02f,0);var scale=e.transform.localScale;
        e.BeginAttract(hole.transform);e.Tick(.05f,hole.Settings,hole.Radius);var entry=e.transform.position;
        hole.transform.position+=Vector3.right*3;e.Tick(.14f,hole.Settings,hole.Radius);
        Assert.AreEqual(entry.x,e.transform.position.x,.001f);Assert.AreEqual(entry.z,e.transform.position.z,.001f);Assert.Less(e.transform.position.y,entry.y);Assert.AreEqual(scale,e.transform.localScale);yield return null;
    }
    [UnityTest] public IEnumerator FirstFourCollectedItemsCauseVisibleGrowth(){
        var hole=Object.FindFirstObjectByType<HoleController>();hole.SetControlEnabled(false);InputManager.Instance.enabled=false;float start=hole.Radius;
        var items=Object.FindObjectsByType<EdibleObject>(FindObjectsSortMode.None).Where(e=>e.Data.objectType!=ObjectType.Bomb&&e.Data.sizeTier==1).Take(4).ToArray();
        for(int i=0;i<4;i++){var e=items[i];e.transform.position=hole.Position+Vector3.up*.02f;e.BeginAttract(hole.transform);e.Tick(.05f,hole.Settings,hole.Radius);e.Tick(.5f,hole.Settings,hole.Radius);Assert.AreEqual(i+1,hole.Growth);if(i<3)Assert.AreEqual(1,hole.Tier);}
        yield return new WaitForSeconds(.4f);Assert.AreEqual(2,hole.Tier);Assert.Greater(hole.Radius,start*1.28f);
        Assert.IsTrue(Object.FindObjectsByType<HoleItText>(FindObjectsSortMode.None).Any(t=>t.text.Contains("8 TO GROW")));
    }
    [UnityTest] public IEnumerator CountdownExpiresAndRetryRestoresShortChallenge(){
        var gm=GameManager.Instance;Assert.Greater(gm.TimeRemaining,0);Assert.LessOrEqual(gm.TimeRemaining,45);
        typeof(GameManager).GetField("timeRemaining",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(gm,.08f);
        yield return new WaitForSeconds(.2f);Assert.AreEqual(GameState.Failed,gm.State);
        Assert.IsTrue(Object.FindObjectsByType<HoleItText>(FindObjectsSortMode.None).Any(t=>t.text=="TIME UP"));
        gm.SendMessage("HandleRetry");while(gm.State==GameState.Ready)yield return null;Assert.AreEqual(GameState.Playing,gm.State);Assert.Greater(gm.TimeRemaining,20);
    }
}
