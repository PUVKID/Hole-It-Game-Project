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

public class ReferenceExtraTouchTests
{
    const string ProfileKey = "Gaeun.Ref.Profile5";
    const int CampaignIndex = 2;
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
        PlayerPrefs.SetString(ProfileKey, JsonUtility.ToJson(new ReferenceProfile { coins = 321, freePlay = false }));
        PlayerPrefs.SetInt("Gaeun.Ref.Unlocked", 10);
        PlayerPrefs.SetInt("Gaeun.Ref.Selected", CampaignIndex);

        yield return SceneManager.LoadSceneAsync("Main");
        yield return new WaitUntil(() => ReferenceGame.Instance != null && ReferenceGame.Instance.Ready);
        yield return new WaitForSecondsRealtime(.6f);
        var game = ReferenceGame.Instance;
        game.TestInput = false;
        game.LoadLevel(CampaignIndex);
        game.ShowHome();
        touchscreen = InputSystem.AddDevice<Touchscreen>();
        yield return null;
        Assert.IsTrue(ReferenceGame.NativeInputEnabled);
        Assert.IsInstanceOf<InputSystemUIInputModule>(EventSystem.current.currentInputModule);
    }

    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        if (touchscreen != null) InputSystem.RemoveDevice(touchscreen);
        InputSystem.settings.backgroundBehavior = oldBackground;
        InputSystem.settings.editorInputBehaviorInPlayMode = oldEditorInput;
        Scene scene = SceneManager.GetSceneByName("Main");
        SceneManager.SetActiveScene(SceneManager.CreateScene("ExtraTouchCleanup"));
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

    IEnumerator Tap(string panelName, string buttonName, int id)
    {
        Button selected = null;
        foreach (var candidate in Object.FindObjectsByType<Button>(FindObjectsSortMode.None))
        {
            if (candidate.name != buttonName || candidate.transform.parent.name != panelName) continue;
            Assert.IsNull(selected, "More than one visible button at " + panelName + "/" + buttonName);
            selected = candidate;
        }
        Assert.IsNotNull(selected, "Visible button missing: " + panelName + "/" + buttonName);
        Assert.IsTrue(selected.interactable);
        Vector2 position = RectTransformUtility.WorldToScreenPoint(null, selected.transform.position);
        SendTouch(id, UnityEngine.InputSystem.TouchPhase.Began, position);
        yield return null;
        yield return null;
        SendTouch(id, UnityEngine.InputSystem.TouchPhase.Ended, position);
        yield return null;
        yield return null;
    }

    [UnityTest]
    public IEnumerator ChallengeNativeTouchStartsFreePracticeAndHomeRestoresCampaign()
    {
        var game = ReferenceGame.Instance;
        Assert.IsFalse(game.FreePlay);
        Assert.IsFalse(game.IsChallenge);
        Assert.AreEqual(CampaignIndex, game.LevelIndex);
        yield return Tap("Home", "Challenges", 1);
        yield return new WaitForSecondsRealtime(.25f);
        GameObject challengePanel = GameObject.Find("Bonus Challenges");
        Assert.IsNotNull(challengePanel);
        Assert.IsTrue(challengePanel.activeInHierarchy);
        yield return Tap("Bonus Challenges", "Challenge0", 2);
        Assert.IsTrue(game.IsChallenge);
        Assert.AreEqual(0, game.ChallengeIndex);
        Assert.IsTrue(game.FreePlay, "Challenges should be practice without campaign life or booster costs.");
        Assert.AreEqual(ReferenceGame.ScreenState.Playing, game.State);
        Assert.AreEqual("Challenge 1", game.CurrentStageLabel);
        Assert.IsFalse(challengePanel.activeSelf);
        Assert.AreEqual(CampaignIndex, PlayerPrefs.GetInt("Gaeun.Ref.Selected"), "A challenge must not overwrite the selected campaign stage.");

        yield return Tap("Play", "Pause", 3);
        Assert.AreEqual(ReferenceGame.ScreenState.Paused, game.State);
        yield return new WaitForSecondsRealtime(.25f);
        yield return Tap("Paused", "Home", 4);
        Assert.AreEqual(ReferenceGame.ScreenState.Home, game.State);
        Assert.IsFalse(game.IsChallenge);
        Assert.IsFalse(game.FreePlay, "Returning from practice must restore the player's classic campaign mode.");
        Assert.AreEqual(CampaignIndex, game.LevelIndex);
        Assert.AreEqual("Level 3", game.CurrentStageLabel);
        Assert.AreEqual(CampaignIndex, PlayerPrefs.GetInt("Gaeun.Ref.Selected"));
        Assert.AreEqual(10, PlayerPrefs.GetInt("Gaeun.Ref.Unlocked"));
        Assert.AreEqual(321, game.AttendanceCoinsForTest);
        Assert.IsFalse(game.Stick.Held);
    }

    [UnityTest]
    public IEnumerator CollectionNativeTouchRendersActualModelAndNextKeepsCampaignSelection()
    {
        var game = ReferenceGame.Instance;
        yield return Tap("Home", "Collection", 1);
        yield return new WaitForSecondsRealtime(.25f);
        GameObject panel = GameObject.Find("3D Collection");
        Assert.IsNotNull(panel);
        Assert.IsTrue(panel.activeInHierarchy);
        var image = panel.transform.Find("Model View").GetComponent<RawImage>();
        var renderTexture = image.texture as RenderTexture;
        Assert.IsNotNull(renderTexture, "Collection must display a rendered model texture.");
        Assert.IsTrue(renderTexture.IsCreated());
        Assert.GreaterOrEqual(renderTexture.width, 512);
        Assert.GreaterOrEqual(renderTexture.height, 512);
        GameObject previewStage = GameObject.Find("Collection Preview Stage");
        Assert.IsNotNull(previewStage);
        var meshes = previewStage.GetComponentsInChildren<MeshFilter>();
        Assert.IsNotEmpty(meshes, "The preview must contain actual collectible geometry.");
        foreach (var mesh in meshes) Assert.Greater(mesh.sharedMesh.vertexCount, 0);
        var title = panel.transform.Find("Title").GetComponent<TextMeshProUGUI>();
        string firstTitle = title.text;

        yield return Tap("3D Collection", "Next", 2);
        Assert.AreNotEqual(firstTitle, title.text);
        Assert.AreEqual(game.Catalog.models[1].label.ToUpperInvariant() + "\n2 / " + game.Catalog.models.Length, title.text);
        Assert.AreSame(renderTexture, image.texture, "Browsing should reuse the preview render target.");
        Assert.AreEqual(CampaignIndex, game.LevelIndex, "Collection Next must never invoke the Home Next button.");
        Assert.AreEqual(CampaignIndex, PlayerPrefs.GetInt("Gaeun.Ref.Selected"));
        yield return Tap("3D Collection", "Done", 3);
        Assert.IsFalse(panel.activeSelf);
        Assert.IsFalse(previewStage.activeSelf, "Closing the collection should stop its preview stage.");
        Assert.AreEqual(ReferenceGame.ScreenState.Home, game.State);
        Assert.AreEqual(321, game.AttendanceCoinsForTest);
        Assert.IsFalse(game.Stick.Held);
    }
}
