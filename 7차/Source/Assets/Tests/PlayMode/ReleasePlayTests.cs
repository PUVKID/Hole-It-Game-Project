using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
public class ReleasePlayTests
{
    private int oldUnlock,oldRelaxed;private bool hadUnlock,hadRelaxed;
    [UnitySetUp] public IEnumerator Setup(){
        hadUnlock=PlayerPrefs.HasKey("HoleIt.MaxLevel");oldUnlock=PlayerPrefs.GetInt("HoleIt.MaxLevel",1);
        hadRelaxed=PlayerPrefs.HasKey("PocketPit.Relaxed");oldRelaxed=PlayerPrefs.GetInt("PocketPit.Relaxed",0);
        PlayerPrefs.SetInt("HoleIt.MaxLevel",30);PlayerPrefs.SetInt("PocketPit.Relaxed",0);Time.timeScale=1;
        yield return SceneManager.LoadSceneAsync("Main");yield return null;
    }
    [UnityTearDown] public IEnumerator Cleanup(){
        Time.timeScale=1;
        if(hadUnlock)PlayerPrefs.SetInt("HoleIt.MaxLevel",oldUnlock);else PlayerPrefs.DeleteKey("HoleIt.MaxLevel");
        if(hadRelaxed)PlayerPrefs.SetInt("PocketPit.Relaxed",oldRelaxed);else PlayerPrefs.DeleteKey("PocketPit.Relaxed");
        var scene=SceneManager.GetSceneByName("Main");var empty=SceneManager.CreateScene("ReleaseCleanup");SceneManager.SetActiveScene(empty);yield return SceneManager.UnloadSceneAsync(scene);
    }
    private IEnumerator Start(int level){var gm=GameManager.Instance;gm.SelectLevel(level);gm.SendMessage("HandlePlay");while(gm.State==GameState.Ready)yield return null;Assert.AreEqual(GameState.Playing,gm.State);}
    [UnityTest] public IEnumerator CaptureScreensAndCheckFonts(){
        GameManager.Instance.SelectLevel(0);yield return Capture("01-menu");
        yield return Start(0);yield return new WaitForSeconds(.3f);yield return Capture("02-play");
        GameManager.Instance.Pause();yield return Capture("03-pause");GameManager.Instance.SendMessage("HandleHome");
        Object.FindFirstObjectByType<UIManager>().SendMessage("ShowSettings");yield return Capture("04-settings");
        Object.FindFirstObjectByType<UIManager>().SendMessage("CloseSettings");yield return Start(23);yield return Capture("05-city");
        foreach(var text in Object.FindObjectsByType<HoleItText>(FindObjectsInactive.Include,FindObjectsSortMode.None)){Assert.IsNotNull(text.font);Assert.AreEqual("NanumGothic-Bold",text.font.name);Assert.IsFalse(System.Text.RegularExpressions.Regex.IsMatch(text.text,"[가-힣]"),text.name+": "+text.text);}
    }
    private IEnumerator Capture(string name){
        var cam=Camera.main;var canvas=Object.FindFirstObjectByType<Canvas>();var rt=new RenderTexture(540,960,24);
        var oldTarget=cam.targetTexture;var oldMode=canvas.renderMode;var oldCamera=canvas.worldCamera;
        cam.targetTexture=rt;cam.aspect=540f/960;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=cam;canvas.planeDistance=1;
        Canvas.ForceUpdateCanvases();yield return null;yield return null;cam.Render();
        var prior=RenderTexture.active;RenderTexture.active=rt;var image=new Texture2D(540,960,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,540,960),0,0);image.Apply();
        Directory.CreateDirectory("TestResults/Screenshots");File.WriteAllBytes("TestResults/Screenshots/"+name+".png",image.EncodeToPNG());
        RenderTexture.active=prior;cam.targetTexture=oldTarget;canvas.renderMode=oldMode;canvas.worldCamera=oldCamera;Object.Destroy(image);rt.Release();Object.Destroy(rt);
    }
    [UnityTest] public IEnumerator RelaxedModeDoesNotCountDown(){PlayerPrefs.SetInt("PocketPit.Relaxed",1);yield return Start(2);float before=GameManager.Instance.TimeRemaining;yield return new WaitForSeconds(.15f);Assert.AreEqual(before,GameManager.Instance.TimeRemaining);}
    [UnityTest,Timeout(900000)] public IEnumerator AllThirtyLevelsCompleteThroughPhysicalSwallowing(){
        PlayerPrefs.SetInt("PocketPit.Relaxed",1);Time.timeScale=6;
        var hole=Object.FindFirstObjectByType<HoleController>();var gm=GameManager.Instance;var objectives=Object.FindFirstObjectByType<MissionManager>();
        var movement=typeof(InputManager).GetField("<MoveVector>k__BackingField",BindingFlags.Instance|BindingFlags.NonPublic);
        Assert.IsNotNull(movement);var report=new System.Text.StringBuilder("Automated real movement / trigger / swallow test. Untimed mode.\n");
        for(int level=0;level<30;level++){
            gm.SendMessage("HandleHome");yield return Start(level);InputManager.Instance.enabled=false;
            float deadline=Time.realtimeSinceStartup+40;float simulatedStart=Time.time;EdibleObject target=null;
            while(gm.State==GameState.Playing&&Time.realtimeSinceStartup<deadline){
                if(target==null||!target.gameObject.activeInHierarchy||target.State!=SwallowState.Normal){
                    target=null;float best=float.MaxValue;
                    foreach(var e in Object.FindObjectsByType<EdibleObject>(FindObjectsSortMode.None)){
                        if(e.Data.objectType==ObjectType.Bomb||!e.CanBeEatenBy(hole.Tier,hole.Radius,hole.Settings.maxObjectToHoleRatio)||gm.IsBlocked(e.Data))continue;
                        float distance=(e.transform.position-hole.Position).sqrMagnitude*(objectives.IsActiveTarget(e.Data)?1:2.5f);
                        if(distance<best){best=distance;target=e;}
                    }
                }
                Vector2 direction=Vector2.zero;
                if(target!=null){Vector3 delta=target.transform.position-hole.Position;direction=new Vector2(delta.x,delta.z);if(direction.magnitude>.15f)direction=direction.normalized;else direction=Vector2.zero;}
                // Keep the hole opening away from hazards while collecting nearby towers.
                foreach(var e in Object.FindObjectsByType<EdibleObject>(FindObjectsSortMode.None))if(e.Data.objectType==ObjectType.Bomb&&e.gameObject.activeInHierarchy){
                    Vector2 away=new Vector2(hole.Position.x-e.transform.position.x,hole.Position.z-e.transform.position.z);
                    float safe=hole.Radius+.1f;
                    if(away.magnitude<safe+.35f&&Vector2.Dot(direction,away)<0)direction+=away.normalized*(safe+.35f-away.magnitude)*4f;
                }
                movement.SetValue(InputManager.Instance,direction.normalized);yield return null;
            }
            movement.SetValue(InputManager.Instance,Vector2.zero);
            while(gm.State==GameState.Clearing)yield return null;
            if(gm.State!=GameState.LevelClear){var mm=Object.FindFirstObjectByType<MissionManager>();report.AppendLine("Hole "+hole.Position+" growth "+hole.Growth+" radius "+hole.Radius+" targets "+mm.DescribeProgress());if(target!=null)report.AppendLine("Target "+target.Data.objectId+" "+target.transform.position+" "+target.State+" layer "+target.gameObject.layer);yield return Capture("failure-level-"+(level+1));}
            report.AppendLine("Level "+(level+1)+": "+gm.State+"; simulated seconds="+(Time.time-simulatedStart).ToString("F1",System.Globalization.CultureInfo.InvariantCulture)+"; budget="+Object.FindFirstObjectByType<LevelManager>().Current.timeLimit);File.WriteAllText("TestResults/PhysicalCampaign.txt",report.ToString());
            Assert.AreEqual(GameState.LevelClear,gm.State,"Level "+(level+1)+" could not complete; tier="+hole.Tier);
            if(level==0)yield return Capture("06-clear");InputManager.Instance.enabled=true;
        }
    }
}
