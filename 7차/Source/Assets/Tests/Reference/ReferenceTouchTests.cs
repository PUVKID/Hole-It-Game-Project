using System.Collections;
using TMPro;
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
    [UnityTest]public IEnumerator OriginalTypographyAndCanvasScalingSurviveScreenTransitions(){
        var g=ReferenceGame.Instance;
        Button play=null;foreach(var button in Object.FindObjectsByType<Button>(FindObjectsSortMode.None))if(button.name=="Play")play=button;
        Assert.IsNotNull(play);Assert.AreEqual("Level 1",play.GetComponentInChildren<TextMeshProUGUI>().text);
        Assert.AreEqual(new Vector2(525,360),play.GetComponent<RectTransform>().sizeDelta);
        Assert.IsNull(GameObject.Find("ForGaeun"));Assert.IsNotNull(GameObject.Find("Home/Coin Display/Wallet"));
        Assert.AreEqual(-10,play.GetComponentInChildren<TextMeshProUGUI>().characterSpacing);
        bool tall=Screen.safeArea.width/Mathf.Max(1,Screen.safeArea.height)<.5624f;
        var ui=GameObject.Find("English Interface").GetComponent<CanvasScaler>();Assert.AreEqual(tall?0:1,ui.matchWidthOrHeight);
        foreach(var text in Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None)){
            Assert.AreEqual("Linik Sans",text.font.faceInfo.familyName);Assert.AreEqual("Black",text.font.faceInfo.styleName);
            foreach(char c in text.text)if(!char.IsControl(c))Assert.IsTrue(text.font.HasCharacter(c),"Unsupported glyph in "+text.name+": "+c);
        }
        yield return Tap("Play",1);Assert.AreEqual(tall?0:.5f,ui.matchWidthOrHeight);
        var timer=GameObject.Find("Time").GetComponent<TextMeshProUGUI>();Assert.AreEqual(2.8f,timer.characterSpacing,.001f);
        Assert.AreEqual(50,timer.fontSize);Assert.IsFalse(timer.enableAutoSizing);Assert.IsFalse(timer.extraPadding);
        Assert.AreEqual(new Vector2(284.5626f,147.7273f),GameObject.Find("TimerBadge").GetComponent<RectTransform>().sizeDelta);
        Assert.IsTrue(System.Text.RegularExpressions.Regex.IsMatch(timer.text,@"^\d{2}:\d{2}$"));
        var world=GameObject.Find("Original World Hole Size").GetComponent<TextMeshPro>();Assert.IsNotNull(world);Assert.AreEqual("Size 1",world.text);
        Assert.AreEqual(new Vector2(1.3808f,.2384f),world.rectTransform.sizeDelta);Assert.AreEqual(-3,world.characterSpacing);Assert.AreEqual(252f/255f,world.color.r,.0001f);Assert.AreEqual(190f/255f,world.color.g,.0001f);
        Assert.Less(Quaternion.Angle(Quaternion.Euler(90,0,0),world.transform.rotation),.001f);
        var count=GameObject.Find("Goal0/Count").GetComponent<TextMeshProUGUI>();Assert.AreEqual(-5,count.characterSpacing);Assert.IsFalse(count.enableAutoSizing);Assert.AreEqual("Outline_InCanvas",count.fontSharedMaterial.name);
        Assert.IsFalse(GameObject.Find("English Interface/Safe Area/Play").transform.Find("GrowthChip").gameObject.activeSelf);

        Assert.IsEmpty(Object.FindObjectsByType<Text>(FindObjectsSortMode.None),"The reconstructed UI must use original TMP typography throughout");
    }
    [UnityTest]public IEnumerator OriginalSettingsToggleAndFailureTypographyRemainUsable(){
        var g=ReferenceGame.Instance;
        yield return Tap("Settings",1);yield return new WaitForSecondsRealtime(.3f);
        var settingsPanel=GameObject.Find("English Interface/Safe Area/Settings");
        var title=settingsPanel.transform.Find("Title").GetComponent<TextMeshProUGUI>();Assert.AreEqual("Settings",title.text);Assert.AreEqual(100,title.fontSizeMax);Assert.AreEqual(-10,title.characterSpacing);
        var muted=settingsPanel.transform.Find("Sound Frame/Sound/Sound Icon/Muted");bool previous=muted.gameObject.activeSelf;
        yield return Tap("Sound",2);Assert.AreEqual(!previous,muted.gameObject.activeSelf);yield return Tap("Sound",3);Assert.AreEqual(previous,muted.gameObject.activeSelf);
        yield return Tap("Done",4);yield return Tap("Play",5);g.SetCountdownForTest(.01f);yield return new WaitForSecondsRealtime(.25f);
        Assert.AreEqual(ReferenceGame.ScreenState.Lost,g.State);
        var result=GameObject.Find("Result");Assert.AreEqual("Time Is Up!",result.transform.Find("Title").GetComponent<TextMeshProUGUI>().text);
        Assert.AreEqual("Level Failed!",result.transform.Find("Outcome").GetComponent<TextMeshProUGUI>().text);
        Assert.AreEqual("Try Again",result.transform.Find("Continue/Label").GetComponent<TextMeshProUGUI>().text);
        Assert.IsFalse(result.transform.Find("Bravo").gameObject.activeSelf);
        yield return Tap("Continue",6);Assert.AreEqual(ReferenceGame.ScreenState.Playing,g.State);
    }
    [UnityTest]public IEnumerator OriginalBombMessageRetainsItsLightSourcePanelAndDarkSourceText(){
        var g=ReferenceGame.Instance;g.TestInput=true;g.LoadLevel(20);g.StartPlay();g.TriggerBombForTest();
        for(int frame=0;frame<240&&g.State==ReferenceGame.ScreenState.Playing;frame++)yield return null;
        Assert.AreEqual(ReferenceGame.ScreenState.Lost,g.State);
        var panel=GameObject.Find("Result/Original Bomb Info").GetComponent<Image>();
        Assert.AreEqual("info-card-v1",panel.sprite.name);Assert.AreEqual(new Vector2(759.6f,511.02f),panel.rectTransform.sizeDelta);
        var text=panel.transform.Find("Outcome").GetComponent<TextMeshProUGUI>();
        Assert.AreEqual("You Blasted!",text.text);Assert.AreEqual(.283018887f,text.color.r,.0001f);Assert.AreEqual(0,text.color.g);Assert.AreEqual(0,text.color.b);
        Assert.AreEqual(new Vector2(338.0251f,73.518f),text.rectTransform.sizeDelta);Assert.AreEqual(new Vector2(0,-130.1f),text.rectTransform.anchoredPosition);
        Assert.AreEqual("poison-bomb",panel.transform.Find("Bomb Icon").GetComponent<Image>().sprite.name);
        Assert.IsFalse(GameObject.Find("Result").transform.Find("Detail").gameObject.activeSelf);
    }
    [UnityTest]public IEnumerator FinderMarkersStayVisibleInOverlayAndCameraCanvasAndExpireCleanly(){
        var g=ReferenceGame.Instance;g.TestInput=true;g.LoadLevel(15);g.StartPlay();
        var duration=typeof(ReferenceGame).GetField("finderUntil",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);duration.SetValue(g,100f);
        yield return null;yield return null;
        var markers=GameObject.Find("Original Finder Indicators").GetComponent<RectTransform>();Assert.Greater(markers.childCount,0);Assert.LessOrEqual(markers.childCount,g.GoalCounts.Length*5);
        foreach(var graphic in markers.GetComponentsInChildren<Graphic>(true))Assert.IsFalse(graphic.raycastTarget);
        var ui=markers.GetComponentInParent<Canvas>();
        foreach(var mode in new[]{RenderMode.ScreenSpaceOverlay,RenderMode.ScreenSpaceCamera}){
            ui.renderMode=mode;ui.worldCamera=Camera.main;ui.planeDistance=1;Canvas.ForceUpdateCanvases();yield return null;yield return null;
            Camera uiCamera=mode==RenderMode.ScreenSpaceOverlay?null:ui.worldCamera;
            foreach(RectTransform marker in markers){if(!marker.gameObject.activeSelf)continue;var position=RectTransformUtility.WorldToScreenPoint(uiCamera,marker.position);
                Assert.IsFalse(float.IsNaN(position.x)||float.IsNaN(position.y));Assert.That(position.x,Is.InRange(-100f,Screen.width+100f));Assert.That(position.y,Is.InRange(-100f,Screen.height+100f));}
        }
        ui.renderMode=RenderMode.ScreenSpaceOverlay;duration.SetValue(g,0f);yield return null;yield return null;
        Assert.IsNull(GameObject.Find("Original Finder Indicators"));
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
