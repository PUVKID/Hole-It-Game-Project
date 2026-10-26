using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class ReferenceTouchTests {
    Touchscreen screen;
    InputSettings.BackgroundBehavior oldBackground;
    InputSettings.EditorInputBehaviorInPlayMode oldEditorInput;
    int unlocked,selected;bool hadUnlocked,hadSelected;
    void Touch(int id,UnityEngine.InputSystem.TouchPhase phase,Vector2 p){
        InputSystem.QueueStateEvent(screen,new TouchState{touchId=id,phase=phase,position=p,pressure=phase==UnityEngine.InputSystem.TouchPhase.Ended?0:1});
    }
    Vector2 ButtonPosition(string name){
        foreach(var b in Object.FindObjectsByType<Button>(FindObjectsSortMode.None))
            if(b.name==name)return RectTransformUtility.WorldToScreenPoint(null,b.transform.position);
        Assert.Fail("Visible button missing: "+name);return Vector2.zero;
    }
    IEnumerator Tap(string name,int id){var p=ButtonPosition(name);Touch(id,UnityEngine.InputSystem.TouchPhase.Began,p);yield return null;yield return null;Touch(id,UnityEngine.InputSystem.TouchPhase.Ended,p);yield return null;yield return null;}
    [UnitySetUp]public IEnumerator Setup(){
        // Batchmode has no focused Game View; route injected devices as a player would.
        oldBackground=InputSystem.settings.backgroundBehavior;oldEditorInput=InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        hadUnlocked=PlayerPrefs.HasKey("Gaeun.Ref.Unlocked");unlocked=PlayerPrefs.GetInt("Gaeun.Ref.Unlocked");hadSelected=PlayerPrefs.HasKey("Gaeun.Ref.Selected");selected=PlayerPrefs.GetInt("Gaeun.Ref.Selected");
        yield return SceneManager.LoadSceneAsync("Main");yield return new WaitUntil(()=>ReferenceGame.Instance!=null&&ReferenceGame.Instance.Ready);yield return new WaitForSecondsRealtime(.6f);
        ReferenceGame.Instance.TestInput=false;ReferenceGame.Instance.LoadLevel(0);ReferenceGame.Instance.ShowHome();
        screen=InputSystem.AddDevice<Touchscreen>();yield return null;
    }
    [UnityTearDown]public IEnumerator Cleanup(){
        if(screen!=null)InputSystem.RemoveDevice(screen);
        InputSystem.settings.backgroundBehavior=oldBackground;InputSystem.settings.editorInputBehaviorInPlayMode=oldEditorInput;
        var scene=SceneManager.GetSceneByName("Main");SceneManager.SetActiveScene(SceneManager.CreateScene("TouchCleanup"));yield return SceneManager.UnloadSceneAsync(scene);
        if(hadUnlocked)PlayerPrefs.SetInt("Gaeun.Ref.Unlocked",unlocked);else PlayerPrefs.DeleteKey("Gaeun.Ref.Unlocked");
        if(hadSelected)PlayerPrefs.SetInt("Gaeun.Ref.Selected",selected);else PlayerPrefs.DeleteKey("Gaeun.Ref.Selected");
    }
    [UnityTest]public IEnumerator TouchPlaysDragsStopsAndResumes(){
        var g=ReferenceGame.Instance;
        Assert.IsTrue(ReferenceGame.NativeInputEnabled,"Production input must not compile out");
        Assert.IsInstanceOf<InputSystemUIInputModule>(EventSystem.current.currentInputModule);
        Assert.IsFalse(g.JoystickVisible);
        yield return Tap("Play",1);
        Assert.AreEqual(ReferenceGame.ScreenState.Playing,g.State,"Native touch must operate PLAY");
        Assert.IsTrue(g.JoystickVisible,"Joystick must be visible before a finger touches it");
        var p=g.IdleJoystickPosition;var before=g.HolePosition;
        Touch(2,UnityEngine.InputSystem.TouchPhase.Began,p);yield return null;yield return null;
        Assert.IsTrue(g.Stick.Held,"Real input path must acquire the touch");
        Touch(2,UnityEngine.InputSystem.TouchPhase.Moved,p+Vector2.right*Screen.width*.6f);yield return new WaitForSeconds(.4f);
        Assert.Greater(g.HolePosition.x-before.x,.5f);Assert.Less(g.HolePosition.x-before.x,g.MaximumSpeed*.55f);
        Assert.AreEqual(1,g.Stick.Value.magnitude,.01f);
        Touch(2,UnityEngine.InputSystem.TouchPhase.Ended,p);yield return null;yield return null;
        Assert.IsFalse(g.Stick.Held);Assert.AreEqual(Vector2.zero,g.Velocity);before=g.HolePosition;
        yield return new WaitForSeconds(.15f);Assert.AreEqual(before,g.HolePosition);Assert.IsTrue(g.JoystickVisible);
        yield return Tap("Pause",3);Assert.AreEqual(ReferenceGame.ScreenState.Paused,g.State);Assert.IsFalse(g.JoystickVisible);
        yield return Tap("Resume",4);Assert.AreEqual(ReferenceGame.ScreenState.Playing,g.State);Assert.IsTrue(g.JoystickVisible);
        // The UI touch must not leak into movement when the menu closes.
        Assert.IsFalse(g.Stick.Held);Assert.AreEqual(before,g.HolePosition);
    }
    [UnityTest]public IEnumerator FloatingTouchKeepsOwnerAndCancelStops(){
        var g=ReferenceGame.Instance;yield return Tap("Play",1);
        var p=new Vector2(Screen.width*.55f,Screen.height*.42f);
        Touch(2,UnityEngine.InputSystem.TouchPhase.Began,p);yield return null;yield return null;
        Assert.IsTrue(g.Stick.Held);Assert.AreEqual(p,g.Stick.Origin);
        Touch(2,UnityEngine.InputSystem.TouchPhase.Moved,p+Vector2.up*Screen.width*.2f);yield return null;yield return null;
        Assert.Greater(g.Stick.Value.y,.9f);
        Touch(3,UnityEngine.InputSystem.TouchPhase.Began,p+Vector2.left*100);yield return null;yield return null;
        Assert.AreEqual(p,g.Stick.Origin,"A second finger must not move the origin");
        Touch(3,UnityEngine.InputSystem.TouchPhase.Ended,p);yield return null;yield return null;Assert.IsTrue(g.Stick.Held);
        Touch(2,UnityEngine.InputSystem.TouchPhase.Canceled,p);yield return null;yield return null;
        Assert.IsFalse(g.Stick.Held);Assert.AreEqual(Vector2.zero,g.Velocity);Assert.IsTrue(g.JoystickVisible);
    }
    [UnityTest]public IEnumerator FreePlayModeAndLevelPickerRespondToNativeTouch(){
        var g=ReferenceGame.Instance;yield return Tap("Settings",1);yield return new WaitForSecondsRealtime(.3f);
        if(!g.FreePlay){yield return Tap("Mode",2);}Assert.IsTrue(g.FreePlay);
        yield return Tap("Done",3);yield return new WaitForSecondsRealtime(.3f);
        yield return Tap("Levels",4);yield return new WaitForSecondsRealtime(.3f);
        yield return Tap("NextPage",5);yield return Tap("Stage19",6);Assert.AreEqual(39,g.LevelIndex);Assert.AreEqual(ReferenceGame.ScreenState.Home,g.State);
        yield return Tap("Settings",7);yield return new WaitForSecondsRealtime(.3f);yield return Tap("Mode",8);Assert.IsFalse(g.FreePlay);
        yield return Tap("Done",9);yield return new WaitForSecondsRealtime(.3f);yield return Tap("Shop",10);yield return new WaitForSecondsRealtime(.3f);
        yield return Tap("Buy0",11);yield return Tap("Done",12);Assert.AreEqual(ReferenceGame.ScreenState.Home,g.State);
    }
}
