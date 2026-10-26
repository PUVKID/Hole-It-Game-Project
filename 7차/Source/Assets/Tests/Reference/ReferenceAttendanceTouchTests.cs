using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class ReferenceAttendanceTouchTests
{
    const string ProfileKey = "Gaeun.Ref.Profile5";
    Touchscreen touchscreen;
    InputSettings.BackgroundBehavior oldBackground;
    InputSettings.EditorInputBehaviorInPlayMode oldEditorInput;
    bool hadProfile, hadUnlocked, hadSelected;
    string oldProfile;
    int oldUnlocked, oldSelected;

    [UnitySetUp]
    public IEnumerator Setup()
    {
        oldBackground = InputSystem.settings.backgroundBehavior;
        oldEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        hadProfile = PlayerPrefs.HasKey(ProfileKey);
        oldProfile = PlayerPrefs.GetString(ProfileKey);
        hadUnlocked = PlayerPrefs.HasKey("Gaeun.Ref.Unlocked");
        oldUnlocked = PlayerPrefs.GetInt("Gaeun.Ref.Unlocked");
        hadSelected = PlayerPrefs.HasKey("Gaeun.Ref.Selected");
        oldSelected = PlayerPrefs.GetInt("Gaeun.Ref.Selected");
        PlayerPrefs.DeleteKey(ProfileKey);

        yield return SceneManager.LoadSceneAsync("Main");
        yield return new WaitUntil(() => ReferenceGame.Instance != null && ReferenceGame.Instance.Ready);
        yield return new WaitForSecondsRealtime(.6f);
        var game = ReferenceGame.Instance;
        game.TestInput = false;
        game.LoadLevel(0);
        game.ShowHome();
        touchscreen = InputSystem.AddDevice<Touchscreen>();
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        if (touchscreen != null) InputSystem.RemoveDevice(touchscreen);
        InputSystem.settings.backgroundBehavior = oldBackground;
        InputSystem.settings.editorInputBehaviorInPlayMode = oldEditorInput;
        Scene scene = SceneManager.GetSceneByName("Main");
        SceneManager.SetActiveScene(SceneManager.CreateScene("AttendanceTouchCleanup"));
        if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);

        if (hadProfile) PlayerPrefs.SetString(ProfileKey, oldProfile); else PlayerPrefs.DeleteKey(ProfileKey);
        if (hadUnlocked) PlayerPrefs.SetInt("Gaeun.Ref.Unlocked", oldUnlocked); else PlayerPrefs.DeleteKey("Gaeun.Ref.Unlocked");
        if (hadSelected) PlayerPrefs.SetInt("Gaeun.Ref.Selected", oldSelected); else PlayerPrefs.DeleteKey("Gaeun.Ref.Selected");
        PlayerPrefs.Save();
    }

    void SendTouch(int id, UnityEngine.InputSystem.TouchPhase phase, Vector2 position)
    {
        InputSystem.QueueStateEvent(touchscreen, new TouchState
        {
            touchId = id, phase = phase, position = position,
            pressure = phase == UnityEngine.InputSystem.TouchPhase.Ended ? 0 : 1
        });
    }

    IEnumerator Tap(string buttonName, int id)
    {
        Button button = null;
        foreach (var candidate in Object.FindObjectsByType<Button>(FindObjectsSortMode.None))
            if (candidate.name == buttonName) { button = candidate; break; }
        Assert.IsNotNull(button, "Visible button missing: " + buttonName);
        Vector2 position = RectTransformUtility.WorldToScreenPoint(null, button.transform.position);
        SendTouch(id, UnityEngine.InputSystem.TouchPhase.Began, position);
        yield return null;
        yield return null;
        SendTouch(id, UnityEngine.InputSystem.TouchPhase.Ended, position);
        yield return null;
        yield return null;
    }

    [UnityTest]
    public IEnumerator NativeTouchOpensClaimsOnceClosesAndKeepsRewardClaimed()
    {
        var game = ReferenceGame.Instance;
        Assert.IsTrue(ReferenceGame.NativeInputEnabled);
        Assert.IsInstanceOf<InputSystemUIInputModule>(EventSystem.current.currentInputModule);
        Assert.AreEqual(0, game.AttendanceCoinsForTest, "The test must use a fresh isolated profile.");
        Assert.IsFalse(game.AttendanceIsOpen);

        yield return Tap("Daily Rewards", 1);
        yield return new WaitForSecondsRealtime(.25f);
        Assert.IsTrue(game.AttendanceIsOpen, "Native touch must open the attendance panel.");
        Assert.AreEqual(31, game.AttendanceCardCount);
        var panel = GameObject.Find("Daily Attendance");
        Assert.IsNotNull(panel);
        for (int day = 1; day <= 31; day++)
        {
            Transform card = panel.transform.Find("Attendance Day " + day);
            Assert.IsNotNull(card, "Missing day " + day);
            Assert.IsTrue(card.gameObject.activeInHierarchy);
            Assert.AreEqual("+1,000", card.Find("Reward").GetComponent<TextMeshProUGUI>().text);
        }
        Assert.IsTrue(game.AttendanceClaimButtonForTest.interactable);

        yield return Tap("Claim Attendance", 2);
        Assert.AreEqual(1000, game.AttendanceCoinsForTest);
        Assert.IsFalse(game.AttendanceClaimButtonForTest.interactable);
        Assert.AreEqual("Done", panel.transform.Find("Attendance Day 1/State").GetComponent<TextMeshProUGUI>().text);
        StringAssert.Contains("1000", GameObject.Find("Home/Coin Display/Wallet").GetComponent<TextMeshProUGUI>().text);
        yield return Tap("Claim Attendance", 3);
        yield return Tap("Claim Attendance", 4);
        Assert.AreEqual(1000, game.AttendanceCoinsForTest, "Further native taps on the disabled claim must not pay again.");
        Assert.AreEqual(ReferenceGame.ScreenState.Home, game.State);
        Assert.IsFalse(game.Stick.Held, "Menu input must not reach the movement controller.");

        yield return Tap("Close Attendance", 5);
        Assert.IsFalse(game.AttendanceIsOpen);
        yield return Tap("Daily Rewards", 6);
        yield return new WaitForSecondsRealtime(.25f);
        Assert.IsTrue(game.AttendanceIsOpen);
        Assert.IsFalse(game.AttendanceClaimButtonForTest.interactable);
        Assert.AreEqual(1000, game.AttendanceCoinsForTest);
        yield return Tap("Close Attendance", 7);
        Assert.IsFalse(game.AttendanceIsOpen);
    }
}
