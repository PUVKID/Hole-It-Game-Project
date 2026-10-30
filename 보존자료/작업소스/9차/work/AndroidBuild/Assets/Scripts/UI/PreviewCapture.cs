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
        // Captures use a separate company/product PlayerPrefs namespace supplied at build time.
        hs.UnlockLevel(30);gm.SelectLevel(0);yield return Shot("01-menu");
        gm.SendMessage("HandlePlay");yield return new WaitForSecondsRealtime(1.6f);gm.Resume();yield return Shot("02-play");
        gm.Pause();yield return Shot("03-pause");gm.SendMessage("HandleHome");
        var ui=Object.FindFirstObjectByType<UIManager>();ui.SendMessage("ShowSettings");yield return Shot("04-settings");ui.SendMessage("CloseSettings");
        gm.SelectLevel(23);gm.SendMessage("HandlePlay");yield return new WaitForSecondsRealtime(1.6f);gm.Resume();yield return Shot("05-city");
        gm.SendMessage("HandleHome");ui.SendMessage("ShowLevelSelect");yield return Shot("06-levels");
        Application.Quit();
    }
    private IEnumerator Shot(string name){yield return new WaitForSecondsRealtime(.3f);ScreenCapture.CaptureScreenshot(Path.Combine(folder,name+".png"));yield return new WaitForSecondsRealtime(.5f);}
}
#endif
