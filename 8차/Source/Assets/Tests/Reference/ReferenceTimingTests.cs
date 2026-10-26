using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class ReferenceTimingTests
{
    const string ProfileKey = "Gaeun.Ref.Profile5";
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    ReferenceGame game;
    bool hadProfile, hadUnlocked, hadSelected;
    string oldProfile;
    int oldUnlocked, oldSelected;
    InputSettings.BackgroundBehavior oldBackground;
    InputSettings.EditorInputBehaviorInPlayMode oldEditorInput;

    T Read<T>(string name) => (T)typeof(ReferenceGame).GetField(name, Private).GetValue(game);
    void Write(string name, object value) => typeof(ReferenceGame).GetField(name, Private).SetValue(game, value);
    void Invoke(string name, params object[] args) => typeof(ReferenceGame).GetMethod(name, Private).Invoke(game, args);

    [UnitySetUp]
    public IEnumerator Setup()
    {
        oldBackground = InputSystem.settings.backgroundBehavior;
        oldEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        hadProfile = PlayerPrefs.HasKey(ProfileKey); oldProfile = PlayerPrefs.GetString(ProfileKey);
        hadUnlocked = PlayerPrefs.HasKey("Gaeun.Ref.Unlocked"); oldUnlocked = PlayerPrefs.GetInt("Gaeun.Ref.Unlocked");
        hadSelected = PlayerPrefs.HasKey("Gaeun.Ref.Selected"); oldSelected = PlayerPrefs.GetInt("Gaeun.Ref.Selected");
        PlayerPrefs.DeleteKey(ProfileKey);
        yield return SceneManager.LoadSceneAsync("Main");
        yield return new WaitUntil(() => ReferenceGame.Instance != null && ReferenceGame.Instance.Ready);
        game = ReferenceGame.Instance;
        game.TestInput = true;
        game.TestDirection = Vector2.zero;
        game.LoadLevel(11); // All four source boosters are unlocked by level 12.
        var profile = Read<ReferenceProfile>("profile");
        profile.freePlay = false;
        profile.boosters = new[] { 3, 3, 3, 3 };
        game.ShowHome();
        yield return null;
    }

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        InputSystem.settings.backgroundBehavior = oldBackground;
        InputSystem.settings.editorInputBehaviorInPlayMode = oldEditorInput;
        Scene scene = SceneManager.GetSceneByName("Main");
        SceneManager.SetActiveScene(SceneManager.CreateScene("TimingCleanup"));
        if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene);
        if (hadProfile) PlayerPrefs.SetString(ProfileKey, oldProfile); else PlayerPrefs.DeleteKey(ProfileKey);
        if (hadUnlocked) PlayerPrefs.SetInt("Gaeun.Ref.Unlocked", oldUnlocked); else PlayerPrefs.DeleteKey("Gaeun.Ref.Unlocked");
        if (hadSelected) PlayerPrefs.SetInt("Gaeun.Ref.Selected", oldSelected); else PlayerPrefs.DeleteKey("Gaeun.Ref.Selected");
        PlayerPrefs.Save();
    }

    [UnityTest]
    public IEnumerator PlayWaitsForFirstJoystickGestureAndResumeDoesNotChargePauseTime()
    {
        game.StartPlay();
        float full = game.Remaining;
        yield return new WaitForSecondsRealtime(.2f);
        Assert.AreEqual(full, game.Remaining, "Play alone must not begin the original countdown.");
        Assert.IsFalse(Read<bool>("countdownStarted"));
        game.BeginStick(game.IdleJoystickPosition);
        game.ReleaseStick();
        Assert.IsTrue(Read<bool>("countdownStarted"));
        yield return new WaitForSecondsRealtime(.15f);
        Assert.Less(game.Remaining, full - .05f, "The first gesture starts counting even after release.");
        game.Pause();
        float paused = game.Remaining;
        yield return new WaitForSecondsRealtime(.2f);
        Assert.AreEqual(paused, game.Remaining);
        // Also model a suspended app whose Update did not run while paused.
        Write("countdownRealtime", Time.realtimeSinceStartupAsDouble - 20d);
        game.Resume();
        yield return null;
        Assert.Greater(game.Remaining, paused - 1f, "Resume must reset the real-time marker, not charge the paused interval.");
        game.LoadLevel(0);
        game.StartPlay();
        full = game.Remaining;
        yield return new WaitForSecondsRealtime(.1f);
        Assert.AreEqual(full, game.Remaining, "The next level waits for its own first gesture.");
    }

    [UnityTest]
    public IEnumerator CountdownAccountsForRealElapsedHitchWhileFreezeDiscardsFrozenTime()
    {
        game.StartPlay();
        game.SetCountdownForTest(60);
        // Inject a stale clock sample without blocking the editor or changing frame dt.
        Write("countdownRealtime", Time.realtimeSinceStartupAsDouble - 2d);
        Invoke("Update");
        Assert.That(game.Remaining, Is.InRange(57.8f, 58.01f),
            "Countdown must use elapsed wall time rather than the capped movement timestep.");
        Invoke("Boost", 3);
        float frozen = game.Remaining;
        Write("countdownRealtime", Time.realtimeSinceStartupAsDouble - 3d);
        Invoke("Update");
        Assert.AreEqual(frozen, game.Remaining, "Freeze discards elapsed countdown time.");
        yield return new WaitForSecondsRealtime(.1f);
        Assert.AreEqual(frozen, game.Remaining);
        game.Pause();
        float activePlayTime = Read<float>("playTime");
        float freezeEnd = Read<float>("freezeUntil");
        yield return new WaitForSecondsRealtime(.15f);
        Assert.AreEqual(activePlayTime, Read<float>("playTime"));
        Assert.AreEqual(freezeEnd, Read<float>("freezeUntil"));
        game.Resume();
        Write("playTime", freezeEnd + .01f);
        Invoke("Update");
        Assert.IsFalse(Read<bool[]>("boostUsed")[3]);
        Assert.Greater(game.Remaining, frozen - .5f, "Expired Freeze must not subtract the frozen interval retroactively.");
    }

    [UnityTest]
    public IEnumerator EveryBoosterExpiresAndCanBeBoughtFromInventoryAgainWithoutDuplicateConsumption()
    {
        game.StartPlay();
        var profile = Read<ReferenceProfile>("profile");
        var buttons = Read<Button[]>("boostButtons");
        var labels = Read<TextMeshProUGUI[]>("boostLabels");
        for (int index = 0; index < 4; index++)
        {
            buttons[index].onClick.Invoke();
            Assert.AreEqual(2, profile.boosters[index]);
            Assert.IsFalse(buttons[index].interactable);
            StringAssert.StartsWith("Active ", labels[index].text);
            buttons[index].onClick.Invoke();
            Assert.AreEqual(2, profile.boosters[index], "An extra activation while active must not consume another item.");
        }
        Assert.AreEqual(8, Read<float>("sizeUntil"), .001f);
        Assert.AreEqual(8, Read<float>("speedUntil"), .001f);
        Assert.AreEqual(12, Read<float>("finderUntil"), .001f);
        Assert.AreEqual(8, Read<float>("freezeUntil"), .001f);
        game.Pause();
        yield return new WaitForSecondsRealtime(.1f);
        foreach (bool active in Read<bool[]>("boostUsed")) Assert.IsTrue(active);
        game.Resume();
        Write("playTime", 12.01f);
        Invoke("Update");
        for (int index = 0; index < 4; index++)
        {
            Assert.IsFalse(Read<bool[]>("boostUsed")[index]);
            Assert.IsTrue(buttons[index].interactable);
            StringAssert.DoesNotContain("Used", labels[index].text);
            buttons[index].onClick.Invoke();
            Assert.AreEqual(1, profile.boosters[index], "Expiry must permit a new activation and charge inventory once.");
            buttons[index].onClick.Invoke();
            Assert.AreEqual(1, profile.boosters[index]);
        }
    }
}
