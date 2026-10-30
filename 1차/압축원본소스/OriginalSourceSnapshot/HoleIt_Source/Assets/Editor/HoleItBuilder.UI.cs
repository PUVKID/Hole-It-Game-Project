using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds the complete portrait UI. The look is a bright, playful black-hole puzzle style:
/// candy colours, rounded panels, clear hierarchy and a fixed bottom-centre thumb stick.
/// The generated UI is original and uses no copied Hole it! artwork.
/// </summary>
public static partial class HoleItBuilder
{
    private class UIBuild
    {
        public UIManager manager;
        public VirtualJoystick joystick;
    }

    private static Sprite roundedSprite;
    private static Sprite playGradientSprite;
    private static Sprite purpleGradientSprite;
    private static Sprite goldGradientSprite;
    private static Sprite coralGradientSprite;
    private static TMP_FontAsset resolvedDefaultFont;

    /// <summary>
    /// Resolve the package-provided TMP font even before the user imports TMP Essential Resources.
    /// This ensures all generated texts have an explicit valid font in a fresh project.
    /// </summary>
    private static TMP_FontAsset ResolveDefaultFont()
    {
        if (resolvedDefaultFont != null) return resolvedDefaultFont;

        string[] candidates =
        {
            "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset",
            "Packages/com.unity.textmeshpro/Package Resources/Fonts & Materials/LiberationSans SDF.asset",
            "Packages/com.unity.textmeshpro/Package Resources/Fonts & Materials/LiberationSans SDF - Fallback.asset"
        };
        for (int i = 0; i < candidates.Length; i++)
        {
            resolvedDefaultFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(candidates[i]);
            if (resolvedDefaultFont != null) return resolvedDefaultFont;
        }

        // Package layout can vary between TMP patch versions; search its resource folder as fallback.
        string[] guids = AssetDatabase.FindAssets("LiberationSans SDF t:TMP_FontAsset",
            new[] { "Packages/com.unity.textmeshpro", "Assets/TextMesh Pro" });
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            resolvedDefaultFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (resolvedDefaultFont != null) return resolvedDefaultFont;
        }
        return null;
    }

    private static bool EnsureDefaultFontAvailable()
    {
        return ResolveDefaultFont() != null;
    }

    private static readonly Color Gold = new Color(1f, 0.83f, 0.25f);
    private static readonly Color Mint = new Color(0.18f, 0.95f, 0.58f);
    private static readonly Color Cyan = new Color(0.28f, 0.94f, 1f);
    private static readonly Color Coral = new Color(1f, 0.36f, 0.52f);
    private static readonly Color DeepNavy = new Color(0.075f, 0.065f, 0.19f, 0.94f);
    private static readonly Color CardPurple = new Color(0.16f, 0.105f, 0.36f, 0.92f);
    private static readonly Color Dim = new Color(0.035f, 0.02f, 0.13f, 0.62f);

    // ───────────────────────── Small UI helpers ─────────────────────────

    private static RectTransform NewRect(string name, Transform parent)
    {
        GameObject g = new GameObject(name, typeof(RectTransform));
        g.transform.SetParent(parent, false);
        return (RectTransform)g.transform;
    }

    private static RectTransform Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    private static RectTransform Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return rt;
    }

    private static Image NewImage(string name, Transform parent, Color color, Sprite sprite, bool raycast)
    {
        RectTransform rt = NewRect(name, parent);
        Image img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = raycast;
        if (IsSlicedSprite(sprite)) img.type = Image.Type.Sliced;
        return img;
    }

    private static bool IsSlicedSprite(Sprite sprite)
    {
        return sprite != null && (sprite == roundedSprite || sprite == playGradientSprite ||
            sprite == purpleGradientSprite || sprite == goldGradientSprite || sprite == coralGradientSprite);
    }

    private static TextMeshProUGUI NewText(string name, Transform parent, string text, float size,
        Color color, TextAlignmentOptions align, bool bold)
    {
        RectTransform rt = NewRect(name, parent);
        TextMeshProUGUI t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        TMP_FontAsset font = ResolveDefaultFont();
        if (font != null) t.font = font;
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.raycastTarget = false;
        t.textWrappingMode = TextWrappingModes.NoWrap;
        t.overflowMode = TextOverflowModes.Ellipsis;
        t.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
        if (bold) t.fontWeight = FontWeight.Heavy;

        // Do not set TMP outlineWidth/outlineColor here. In a fresh project, TMP can
        // resolve a FontAsset before its material preset is initialized; assigning
        // outlineWidth then calls TextMeshProUGUI.SetOutlineThickness and throws
        // NullReferenceException, interrupting first-open scene generation.
        // A standard UI Shadow supplies a subtle readable edge without mutating TMP
        // material state or depending on a pre-imported TMP material preset.
        Shadow textShadow = t.gameObject.AddComponent<Shadow>();
        textShadow.effectColor = new Color(0.035f, 0.025f, 0.12f, 0.72f);
        textShadow.effectDistance = new Vector2(1.2f, -1.8f);
        textShadow.useGraphicAlpha = true;
        return t;
    }

    private static void StyleSurface(Image img, Color outlineColor, bool addShadow)
    {
        if (img == null) return;
        if (addShadow)
        {
            Shadow shadow = img.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.025f, 0.015f, 0.09f, 0.72f);
            shadow.effectDistance = new Vector2(0f, -10f);
            shadow.useGraphicAlpha = true;
        }
        if (outlineColor.a > 0.001f)
        {
            Outline outline = img.gameObject.AddComponent<Outline>();
            outline.effectColor = outlineColor;
            outline.effectDistance = new Vector2(2f, -2f);
            outline.useGraphicAlpha = true;
        }
    }

    private static RectTransform AddSurface(Transform parent, string name, Vector2 anchor, Vector2 pivot,
        Vector2 pos, Vector2 size, Color fill, Color outline, bool shadow = true)
    {
        Image img = NewImage(name, parent, fill, roundedSprite, false);
        Place(img.rectTransform, anchor, pivot, pos, size);
        StyleSurface(img, outline, shadow);
        return img.rectTransform;
    }

    private static Button NewButton(string name, Transform parent, string label, float fontSize, Color bg,
        Vector2 anchor, Vector2 pos, Vector2 size)
    {
        return NewButton(name, parent, label, fontSize, bg, anchor, pos, size, roundedSprite);
    }

    private static Button NewButton(string name, Transform parent, string label, float fontSize, Color bg,
        Vector2 anchor, Vector2 pos, Vector2 size, Sprite buttonSprite)
    {
        Sprite useSprite = buttonSprite != null ? buttonSprite : roundedSprite;
        Image img = NewImage(name, parent, bg, useSprite, true);
        Place(img.rectTransform, anchor, new Vector2(0.5f, 0.5f), pos, size);
        StyleSurface(img, new Color(1f, 1f, 1f, 0.24f), true);

        Button b = img.gameObject.AddComponent<Button>();
        b.targetGraphic = img;
        b.transition = Selectable.Transition.ColorTint;
        ColorBlock cb = b.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1f, 1f, 1f, 1f);
        cb.pressedColor = new Color(0.84f, 0.91f, 1f, 1f);
        cb.selectedColor = Color.white;
        cb.disabledColor = new Color(0.62f, 0.62f, 0.7f, 0.6f);
        cb.colorMultiplier = 1f;
        cb.fadeDuration = 0.08f;
        b.colors = cb;

        TextMeshProUGUI t = NewText("Label", img.transform, label, fontSize, Color.white, TextAlignmentOptions.Center, true);
        Stretch(t.rectTransform);
        UIPunch punch = t.gameObject.AddComponent<UIPunch>();
        b.onClick.AddListener(() => punch.Play(0.55f));
        return b;
    }

    private static TextMeshProUGUI CenterText(Transform parent, string name, string text, float size,
        Color color, float anchorY, float height, bool bold)
    {
        TextMeshProUGUI t = NewText(name, parent, text, size, color, TextAlignmentOptions.Center, bold);
        Place(t.rectTransform, new Vector2(0.5f, anchorY), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000f, height));
        return t;
    }

    private static RectTransform NewPanel(string name, Transform parent, bool dim)
    {
        RectTransform rt = NewRect(name, parent);
        Stretch(rt);
        if (dim)
        {
            Image bg = rt.gameObject.AddComponent<Image>();
            bg.sprite = whiteSprite;
            bg.color = Dim;
            bg.raycastTarget = true;
        }
        return rt;
    }

    private static void AddDecorDot(Transform parent, Sprite circle, Vector2 anchor, Vector2 pos,
        float diameter, Color color)
    {
        Image dot = NewImage("DecorDot", parent, color, circle, false);
        Place(dot.rectTransform, anchor, new Vector2(0.5f, 0.5f), pos, new Vector2(diameter, diameter));
    }

    private static void AddHoleEmblem(Transform parent, Sprite circle, Sprite ring, Vector2 anchor, Vector2 pos, float diameter)
    {
        Image outer = NewImage("HoleEmblem_Ring", parent, new Color(0.40f, 0.98f, 1f, 0.98f), ring, false);
        Place(outer.rectTransform, anchor, new Vector2(0.5f, 0.5f), pos, new Vector2(diameter, diameter));
        Image inner = NewImage("HoleEmblem_Core", parent, new Color(0.025f, 0.02f, 0.08f, 1f), circle, false);
        Place(inner.rectTransform, anchor, new Vector2(0.5f, 0.5f), pos, new Vector2(diameter * 0.68f, diameter * 0.68f));
        Image glint = NewImage("HoleEmblem_Glint", parent, new Color(1f, 1f, 1f, 0.9f), circle, false);
        Place(glint.rectTransform, anchor, new Vector2(0.5f, 0.5f), pos + new Vector2(-diameter * 0.16f, diameter * 0.15f),
            new Vector2(diameter * 0.12f, diameter * 0.12f));
    }

    // ───────────────────────── Main UI builder ─────────────────────────

    private static UIBuild BuildUI(Sprite circle, Sprite ring)
    {
        GameObject canvasGo = new GameObject("Canvas", typeof(RectTransform));
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.pixelPerfect = false;
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.45f;
        canvasGo.AddComponent<GraphicRaycaster>();
        RectTransform canvasRect = (RectTransform)canvasGo.transform;
        UIBuild result = new UIBuild();

        // Fixed bottom-centre joystick. The base stays in place; only its knob follows the finger.
        RectTransform joyRoot = NewRect("Joystick", canvasRect);
        Stretch(joyRoot);
        CanvasGroup joyGroup = joyRoot.gameObject.AddComponent<CanvasGroup>();
        joyGroup.blocksRaycasts = false;
        joyGroup.interactable = false;
        joyGroup.ignoreParentGroups = true;

        Image joyBase = NewImage("Base", joyRoot, new Color(0.035f, 0.045f, 0.14f, 0.80f), circle, false);
        Place(joyBase.rectTransform, new Vector2(0.5f, 0.145f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(280f, 280f));
        StyleSurface(joyBase, new Color(1f, 1f, 1f, 0.18f), true);

        Image joyFill = NewImage("Fill", joyBase.transform, new Color(0.10f, 0.17f, 0.37f, 0.62f), circle, false);
        Place(joyFill.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(248f, 248f));
        Image joyRing = NewImage("NeonRing", joyBase.transform, new Color(0.30f, 0.98f, 1f, 0.92f), ring, false);
        Stretch(joyRing.rectTransform);
        Image joyKnob = NewImage("Knob", joyBase.transform, new Color(0.22f, 0.96f, 0.78f, 0.98f), circle, false);
        Place(joyKnob.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(104f, 104f));
        StyleSurface(joyKnob, new Color(1f, 1f, 1f, 0.74f), true);
        Image knobHighlight = NewImage("KnobHighlight", joyKnob.transform, new Color(1f, 1f, 1f, 0.52f), circle, false);
        Place(knobHighlight.rectTransform, new Vector2(0.34f, 0.66f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(30f, 30f));

        VirtualJoystick joystick = joyRoot.gameObject.AddComponent<VirtualJoystick>();
        SetRef(joystick, "canvasRect", canvasRect);
        SetRef(joystick, "baseRect", joyBase.rectTransform);
        SetRef(joystick, "fillRect", joyFill.rectTransform);
        SetRef(joystick, "knobRect", joyKnob.rectTransform);
        SetRef(joystick, "group", joyGroup);
        joyRoot.gameObject.SetActive(false);
        result.joystick = joystick;

        TextMeshProUGUI moveHint = NewText("MoveHint", joyRoot, "MOVE", 26f,
            new Color(0.76f, 0.94f, 1f, 0.84f), TextAlignmentOptions.Center, true);
        Place(moveHint.rectTransform, new Vector2(0.5f, 0.245f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(210f, 36f));
        moveHint.characterSpacing = 4f;

        // HUD is safe-area aware; the joystick is on the full canvas so its location is consistent.
        RectTransform hud = NewRect("HUD", canvasRect);
        Stretch(hud);
        hud.gameObject.AddComponent<SafeAreaFitter>();

        AddSurface(hud, "TimerBadge", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -34f), new Vector2(220f, 76f), new Color(0.15f, 0.11f, 0.36f, 0.92f), new Color(0.43f, 0.95f, 1f, 0.48f));
        TextMeshProUGUI timer = NewText("Timer", hud, "1:00", 61f, Color.white, TextAlignmentOptions.Center, true);
        Place(timer.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -35f), new Vector2(220f, 76f));

        AddSurface(hud, "LevelBadge", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -121f), new Vector2(292f, 62f), new Color(0.97f, 0.34f, 0.55f, 0.92f), new Color(1f, 1f, 1f, 0.28f), false);
        TextMeshProUGUI levelText = NewText("LevelText", hud, "LEVEL 1", 37f, Color.white, TextAlignmentOptions.Center, true);
        Place(levelText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -121f), new Vector2(292f, 62f));

        AddSurface(hud, "ScoreBadge", new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-28f, -34f), new Vector2(190f, 76f), new Color(0.16f, 0.12f, 0.39f, 0.92f), new Color(1f, 0.82f, 0.25f, 0.42f));
        TextMeshProUGUI score = NewText("Score", hud, "0", 56f, Gold, TextAlignmentOptions.Center, true);
        Place(score.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-28f, -35f), new Vector2(190f, 76f));
        UIPunch scorePunch = score.gameObject.AddComponent<UIPunch>();

        Button pause = NewButton("PauseButton", hud, "II", 48f, new Color(0.20f, 0.14f, 0.45f, 0.96f),
            new Vector2(0f, 1f), new Vector2(30f, -35f), new Vector2(94f, 94f), circle);

        // A compact mission card: readable but does not steal the entire play area.
        RectTransform missionPanel = NewRect("MissionPanel", hud);
        Place(missionPanel, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -202f), new Vector2(640f, 314f));
        Image missionBg = missionPanel.gameObject.AddComponent<Image>();
        missionBg.sprite = roundedSprite;
        missionBg.type = Image.Type.Sliced;
        missionBg.color = new Color(0.12f, 0.09f, 0.31f, 0.88f);
        missionBg.raycastTarget = false;
        StyleSurface(missionBg, new Color(0.45f, 0.93f, 1f, 0.30f), true);

        TextMeshProUGUI missionTitle = NewText("Title", missionPanel, "YOUR MISSION", 31f, new Color(0.70f, 0.98f, 1f), TextAlignmentOptions.Center, true);
        Place(missionTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(590f, 40f));

        MissionRowUI[] rows = new MissionRowUI[3];
        for (int i = 0; i < 3; i++) rows[i] = BuildMissionRow(missionPanel, circle, i);
        MissionUI missionUI = missionPanel.gameObject.AddComponent<MissionUI>();
        SetRefs(missionUI, "rows", rows);

        // Keep growth feedback above the fixed joystick instead of overlapping the controls.
        RectTransform sizeRoot = NewRect("SizeGauge", hud);
        Place(sizeRoot, new Vector2(0.5f, 0.30f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(500f, 86f));
        TextMeshProUGUI sizeText = NewText("SizeText", sizeRoot, "SIZE 1", 34f, Color.white, TextAlignmentOptions.Center, true);
        Place(sizeText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -2f), new Vector2(480f, 42f));
        UIPunch sizePunch = sizeText.gameObject.AddComponent<UIPunch>();
        Image barBg = NewImage("BarBg", sizeRoot, new Color(0.045f, 0.035f, 0.15f, 0.84f), roundedSprite, false);
        barBg.type = Image.Type.Sliced;
        Place(barBg.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 7f), new Vector2(440f, 20f));
        StyleSurface(barBg, new Color(1f, 1f, 1f, 0.14f), false);
        Image barFill = NewImage("BarFill", barBg.transform, Mint, roundedSprite, false);
        barFill.type = Image.Type.Sliced;
        Stretch(barFill.rectTransform);
        barFill.type = Image.Type.Filled;
        barFill.fillMethod = Image.FillMethod.Horizontal;
        barFill.fillOrigin = 0;
        barFill.fillAmount = 0f;

        // Ready and mission-complete banners with contrasting candy-colour plaques.
        RectTransform readyBanner = NewRect("ReadyBanner", canvasRect);
        Stretch(readyBanner);
        AddSurface(readyBanner, "ReadyPlaque", new Vector2(0.5f, 0.62f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(650f, 152f), new Color(0.18f, 0.56f, 0.96f, 0.93f), new Color(1f, 1f, 1f, 0.35f));
        TextMeshProUGUI readyText = CenterText(readyBanner, "Text", "LEVEL 1", 102f, Color.white, 0.62f, 140f, true);

        RectTransform missionDone = NewRect("MissionCompleteBanner", canvasRect);
        Stretch(missionDone);
        AddSurface(missionDone, "MissionCompletePlaque", new Vector2(0.5f, 0.58f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(780f, 180f), new Color(1f, 0.43f, 0.20f, 0.96f), new Color(1f, 1f, 1f, 0.44f));
        CenterText(missionDone, "Text", "MISSION COMPLETE!", 77f, Color.white, 0.58f, 150f, true);
        missionDone.gameObject.AddComponent<UIPunch>();

        // Clear / result / pause use a centred card over the dimmed, still-visible world.
        RectTransform clear = NewPanel("ClearPanel", canvasRect, true);
        AddSurface(clear, "ResultCard", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
            new Vector2(840f, 1180f), CardPurple, new Color(0.43f, 0.95f, 1f, 0.43f));
        CenterText(clear, "Title", "LEVEL CLEAR!", 94f, Gold, 0.72f, 170f, true);
        TextMeshProUGUI clearScore = CenterText(clear, "Score", "SCORE 0", 70f, Color.white, 0.62f, 120f, true);
        TextMeshProUGUI breakdown = CenterText(clear, "Breakdown", "", 42f, new Color(0.89f, 0.91f, 1f), 0.50f, 290f, false);
        Button clearNext = NewButton("NextButton", clear, "NEXT LEVEL  >", 56f, Color.white,
            new Vector2(0.5f, 0.30f), Vector2.zero, new Vector2(570f, 142f), playGradientSprite);
        Button clearHome = NewButton("HomeButton", clear, "HOME", 46f, new Color(0.31f, 0.24f, 0.53f, 1f),
            new Vector2(0.5f, 0.20f), Vector2.zero, new Vector2(520f, 112f));

        RectTransform fail = NewPanel("FailPanel", canvasRect, true);
        AddSurface(fail, "ResultCard", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
            new Vector2(850f, 1450f), CardPurple, new Color(1f, 0.48f, 0.68f, 0.44f));
        TextMeshProUGUI failTitle = CenterText(fail, "Title", "TIME UP", 98f, Coral, 0.79f, 160f, true);
        TextMeshProUGUI failScore = CenterText(fail, "Score", "SCORE 0", 70f, Color.white, 0.69f, 120f, true);
        TextMeshProUGUI newBest = CenterText(fail, "NewBest", "NEW BEST!", 76f, Gold, 0.61f, 130f, true);
        newBest.gameObject.AddComponent<UIPunch>();
        CenterText(fail, "Top3Header", "TOP SCORES", 36f, new Color(0.78f, 0.96f, 1f, 0.9f), 0.53f, 66f, true);
        TextMeshProUGUI[] tops = new TextMeshProUGUI[3];
        for (int i = 0; i < 3; i++) tops[i] = CenterText(fail, "Top" + (i + 1), (i + 1) + ".  0", 48f,
            i == 0 ? Gold : Color.white, 0.47f - i * 0.055f, 70f, true);
        Button failRetry = NewButton("RetryButton", fail, "TRY AGAIN", 54f, Color.white,
            new Vector2(0.5f, 0.23f), Vector2.zero, new Vector2(560f, 132f), coralGradientSprite);
        Button failHome = NewButton("HomeButton", fail, "HOME", 45f, new Color(0.31f, 0.24f, 0.53f, 1f),
            new Vector2(0.5f, 0.145f), Vector2.zero, new Vector2(510f, 108f));

        RectTransform pausePanel = NewPanel("PausePanel", canvasRect, true);
        AddSurface(pausePanel, "PauseCard", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero,
            new Vector2(780f, 760f), CardPurple, new Color(0.43f, 0.95f, 1f, 0.43f));
        CenterText(pausePanel, "Title", "TAKE A BREATHER", 70f, Color.white, 0.68f, 130f, true);
        Button resume = NewButton("ResumeButton", pausePanel, "LET'S PLAY!", 54f, Color.white,
            new Vector2(0.5f, 0.46f), Vector2.zero, new Vector2(560f, 136f), playGradientSprite);
        Button pauseHome = NewButton("HomeButton", pausePanel, "HOME", 46f, new Color(0.31f, 0.24f, 0.53f, 1f),
            new Vector2(0.5f, 0.32f), Vector2.zero, new Vector2(500f, 112f));

        // Main menu: show the vivid world through a light tint and put controls on a polished centre card.
        RectTransform title = NewRect("TitlePanel", canvasRect);
        Stretch(title);
        Image titleBg = title.gameObject.AddComponent<Image>();
        titleBg.sprite = whiteSprite;
        titleBg.color = new Color(0.07f, 0.025f, 0.19f, 0.20f);
        titleBg.raycastTarget = true;

        AddSurface(title, "TitleCard", new Vector2(0.5f, 0.52f), new Vector2(0.5f, 0.5f), Vector2.zero,
            new Vector2(900f, 1260f), new Color(0.10f, 0.075f, 0.27f, 0.86f), new Color(0.51f, 0.93f, 1f, 0.46f));

        // Small floating colour accents around the card create a playful, candy-world frame.
        AddDecorDot(title, circle, new Vector2(0.16f, 0.80f), Vector2.zero, 52f, Coral);
        AddDecorDot(title, circle, new Vector2(0.84f, 0.77f), Vector2.zero, 42f, Gold);
        AddDecorDot(title, circle, new Vector2(0.13f, 0.62f), Vector2.zero, 26f, Cyan);
        AddDecorDot(title, circle, new Vector2(0.88f, 0.57f), Vector2.zero, 32f, Mint);
        AddDecorDot(title, circle, new Vector2(0.19f, 0.34f), Vector2.zero, 34f, Gold);
        AddDecorDot(title, circle, new Vector2(0.81f, 0.29f), Vector2.zero, 48f, Coral);
        AddDecorDot(title, circle, new Vector2(0.23f, 0.24f), Vector2.zero, 18f, Cyan);
        AddDecorDot(title, circle, new Vector2(0.78f, 0.22f), Vector2.zero, 22f, Gold);

        AddHoleEmblem(title, circle, ring, new Vector2(0.5f, 0.805f), Vector2.zero, 92f);
        CenterText(title, "Logo", "HOLE IT!", 164f, Color.white, 0.715f, 208f, true);
        TextMeshProUGUI sub = CenterText(title, "Tagline", "SWALLOW  -  GROW  -  CLEAR", 38f,
            new Color(0.72f, 0.96f, 1f), 0.633f, 65f, true);
        sub.characterSpacing = 2.0f;

        AddSurface(title, "LevelSelectPill", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(450f, 112f), new Color(0.22f, 0.15f, 0.50f, 0.95f), new Color(0.73f, 0.84f, 1f, 0.34f), false);
        Button prev = NewButton("PrevLevel", title, "<", 76f, new Color(0.24f, 0.17f, 0.52f, 0.98f),
            new Vector2(0.5f, 0.5f), new Vector2(-282f, 0f), new Vector2(106f, 106f), circle);
        Button next = NewButton("NextLevel", title, ">", 76f, new Color(0.24f, 0.17f, 0.52f, 0.98f),
            new Vector2(0.5f, 0.5f), new Vector2(282f, 0f), new Vector2(106f, 106f), circle);
        TextMeshProUGUI titleLevel = CenterText(title, "LevelText", "LEVEL 1", 62f, Gold, 0.5f, 104f, true);
        Button play = NewButton("PlayButton", title, "PLAY", 82f, Color.white,
            new Vector2(0.5f, 0.37f), Vector2.zero, new Vector2(610f, 184f), playGradientSprite);

        AddSurface(title, "BestScorePill", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -488f), new Vector2(460f, 82f), new Color(0.18f, 0.13f, 0.43f, 0.95f), new Color(1f, 0.82f, 0.25f, 0.37f), false);
        TextMeshProUGUI best = CenterText(title, "Best", "BEST SCORE  0", 43f, Color.white, 0.245f, 74f, true);
        CenterText(title, "Footer", "A LITTLE HOLE. A BIG ADVENTURE.", 28f,
            new Color(0.78f, 0.82f, 1f, 0.86f), 0.205f, 48f, true);

        // Wire up the UIManager; field names intentionally match the runtime controller.
        UIManager ui = canvasGo.AddComponent<UIManager>();
        SetRef(ui, "titlePanel", title.gameObject);
        SetRef(ui, "hudPanel", hud.gameObject);
        SetRef(ui, "readyBanner", readyBanner.gameObject);
        SetRef(ui, "missionCompleteBanner", missionDone.gameObject);
        SetRef(ui, "clearPanel", clear.gameObject);
        SetRef(ui, "failPanel", fail.gameObject);
        SetRef(ui, "pausePanel", pausePanel.gameObject);
        SetRef(ui, "playButton", play);
        SetRef(ui, "levelPrevButton", prev);
        SetRef(ui, "levelNextButton", next);
        SetRef(ui, "titleLevelText", titleLevel);
        SetRef(ui, "titleBestText", best);
        SetRef(ui, "levelText", levelText);
        SetRef(ui, "timerText", timer);
        SetRef(ui, "scoreText", score);
        SetRef(ui, "sizeText", sizeText);
        SetRef(ui, "sizeFill", barFill);
        SetRef(ui, "pauseButton", pause);
        SetRef(ui, "missionUI", missionUI);
        SetRef(ui, "sizePunch", sizePunch);
        SetRef(ui, "scorePunch", scorePunch);
        SetRef(ui, "readyLevelText", readyText);
        SetRef(ui, "clearScoreText", clearScore);
        SetRef(ui, "clearBreakdownText", breakdown);
        SetRef(ui, "clearNextButton", clearNext);
        SetRef(ui, "clearHomeButton", clearHome);
        SetRef(ui, "failTitleText", failTitle);
        SetRef(ui, "failScoreText", failScore);
        SetRefs(ui, "topScoreTexts", tops);
        SetRef(ui, "newBestObject", newBest.gameObject);
        SetRef(ui, "failRetryButton", failRetry);
        SetRef(ui, "failHomeButton", failHome);
        SetRef(ui, "resumeButton", resume);
        SetRef(ui, "pauseHomeButton", pauseHome);
        result.manager = ui;

        hud.gameObject.SetActive(false);
        readyBanner.gameObject.SetActive(false);
        missionDone.gameObject.SetActive(false);
        clear.gameObject.SetActive(false);
        fail.gameObject.SetActive(false);
        pausePanel.gameObject.SetActive(false);
        return result;
    }

    private static MissionRowUI BuildMissionRow(RectTransform parent, Sprite circle, int index)
    {
        RectTransform row = NewRect("Row" + (index + 1), parent);
        Place(row, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -54f - index * 78f), new Vector2(590f, 70f));
        Image bg = row.gameObject.AddComponent<Image>();
        bg.sprite = roundedSprite;
        bg.type = Image.Type.Sliced;
        bg.color = new Color(0.30f, 0.22f, 0.57f, 0.84f);
        bg.raycastTarget = false;

        Image icon = NewImage("Icon", row, Color.white, circle, false);
        Place(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(16f, 0f), new Vector2(56f, 56f));
        UIPunch iconPunch = icon.gameObject.AddComponent<UIPunch>();

        TextMeshProUGUI nameText = NewText("Name", row, "ITEM", 31f, Color.white, TextAlignmentOptions.Left, true);
        Place(nameText.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(91f, 0f), new Vector2(270f, 60f));

        TextMeshProUGUI count = NewText("Count", row, "0 / 0", 37f, Gold, TextAlignmentOptions.Right, true);
        Place(count.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-66f, 0f), new Vector2(180f, 60f));
        UIPunch countPunch = count.gameObject.AddComponent<UIPunch>();

        Image check = NewImage("Check", row, Mint, circle, false);
        Place(check.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-15f, 0f), new Vector2(38f, 38f));
        check.gameObject.SetActive(false);

        MissionRowUI r = row.gameObject.AddComponent<MissionRowUI>();
        SetRef(r, "icon", icon);
        SetRef(r, "nameText", nameText);
        SetRef(r, "countText", count);
        SetRef(r, "checkMark", check.gameObject);
        SetRef(r, "background", bg);
        SetRef(r, "iconPunch", iconPunch);
        SetRef(r, "countPunch", countPunch);
        return r;
    }
}
