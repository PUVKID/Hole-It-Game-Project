#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
using System.Collections;
using System.IO;
using UnityEngine;
/// <summary>Opt-in desktop capture utility; excluded from Android builds.</summary>
public class PreviewCapture:MonoBehaviour
{
    private string folder;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init(){var args=System.Environment.GetCommandLineArgs();for(int i=0;i<args.Length-1;i++)if(args[i]=="--capture"){var go=new GameObject("PreviewCapture");go.AddComponent<PreviewCapture>().folder=args[i+1];}}
    private IEnumerator Start(){
        Application.runInBackground=true;Directory.CreateDirectory(folder);yield return new WaitForSecondsRealtime(2);
        var gm=GameManager.Instance;var input=InputManager.Instance;var hs=Object.FindFirstObjectByType<HighScoreManager>();
        PlayerPrefs.SetInt("PocketPit.Relaxed",0);Object.FindFirstObjectByType<FamilyMode>().SendMessage("Refresh");
        // Captures use a separate company/product PlayerPrefs namespace supplied at build time.
        hs.UnlockLevel(30);gm.SelectLevel(0);yield return Shot("01-menu");
        gm.SendMessage("HandlePlay");yield return new WaitForSecondsRealtime(1.6f);gm.Resume();yield return Shot("02-play");
        gm.Pause();yield return Shot("03-pause");gm.SendMessage("HandleHome");
        var ui=Object.FindFirstObjectByType<UIManager>();ui.SendMessage("ShowSettings");yield return Shot("04-settings");ui.SendMessage("CloseSettings");
        gm.SelectLevel(23);gm.SendMessage("HandlePlay");yield return new WaitForSecondsRealtime(1.6f);gm.Resume();yield return Shot("05-city");
        gm.SendMessage("HandleHome");ui.SendMessage("ShowLevelSelect");yield return Shot("06-levels");
        ui.SendMessage("CloseLevelSelect");gm.SelectLevel(0);gm.SendMessage("HandlePlay");yield return new WaitForSecondsRealtime(1.6f);gm.Resume();
        input.enabled=false;var hole=Object.FindFirstObjectByType<HoleController>();
        foreach(var e in Object.FindObjectsByType<EdibleObject>(FindObjectsSortMode.None))if(e.Data.objectType==ObjectType.Bomb){e.transform.position=hole.Position+Vector3.up*.02f;break;}
        Physics.SyncTransforms();yield return new WaitForSecondsRealtime(1.5f);yield return Shot("07-bomb-failure");
        gm.SendMessage("HandleHome");gm.SelectLevel(1);gm.SendMessage("HandlePlay");yield return new WaitForSecondsRealtime(1.2f);gm.Resume();yield return Shot("08-steps");
        gm.SendMessage("HandleHome");gm.SelectLevel(2);gm.SendMessage("HandlePlay");yield return new WaitForSecondsRealtime(1.2f);gm.Resume();yield return Shot("09-rings");
        gm.SendMessage("HandleHome");gm.SelectLevel(4);gm.SendMessage("HandlePlay");yield return new WaitForSecondsRealtime(1.2f);gm.Resume();yield return Shot("10-castle");
        Application.Quit();
    }
    private IEnumerator Shot(string name){
        yield return new WaitForSecondsRealtime(.3f);var cam=Camera.main;var canvas=Object.FindFirstObjectByType<Canvas>();
        var rt=new RenderTexture(540,960,24);rt.antiAliasing=4;cam.targetTexture=rt;cam.aspect=540f/960;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=cam;canvas.planeDistance=1;
        Canvas.ForceUpdateCanvases();yield return null;yield return null;cam.Render();var prior=RenderTexture.active;RenderTexture.active=rt;
        var image=new Texture2D(540,960,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,540,960),0,0);image.Apply();File.WriteAllBytes(Path.Combine(folder,name+".png"),image.EncodeToPNG());
        RenderTexture.active=prior;cam.targetTexture=null;canvas.renderMode=RenderMode.ScreenSpaceOverlay;Object.Destroy(image);rt.Release();Object.Destroy(rt);
    }
}
#endif
