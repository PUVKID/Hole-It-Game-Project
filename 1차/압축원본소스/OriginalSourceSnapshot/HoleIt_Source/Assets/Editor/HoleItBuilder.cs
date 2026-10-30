using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 메뉴 한 번으로 프로젝트를 구성한다: 레이어 → 머티리얼/프리팹/FX → 데이터(ScriptableObject) → 씬 → 빌드 설정.
/// 런타임 코드가 아니라 "에디터 타임"에 에셋을 만들기 때문에, 이후 Inspector에서 에셋을 자유롭게 교체할 수 있다.
/// 실행: 메뉴 HoleIt ▸ 1. 프로젝트 전체 구성   (CLI: -executeMethod HoleItBuilder.BuildAllCli)
/// </summary>
public static partial class HoleItBuilder
{
    private const string Root = "Assets/HoleIt";
    private const string MatDir = Root + "/Materials";
    private const string MeshDir = Root + "/Meshes";
    private const string PrefabDir = Root + "/Prefabs";
    private const string DataDir = Root + "/Data";
    private const string ResDir = Root + "/Resources";   // GameSettings / HoleSizeTable (Resources.Load 대비 복구용)
    private const string LevelDir = ResDir + "/Data/Levels";
    private const string EdibleDir = Root + "/Data/Edibles";
    private const string TexDir = Root + "/Textures";
    private const string AudioDir = Root + "/Audio";
    private const string SceneDir = "Assets/Scenes";
    private const string ScenePath = SceneDir + "/Main.unity";

    /// <summary>현재 에디터가 레거시 Input을 못 쓰는 상태(New 전용)라서 재시작이 필요한지.</summary>
    private static bool NeedsEditorRestart;

    [MenuItem("HoleIt/1. 프로젝트 전체 구성")]
    public static void BuildAllMenu()
    {
        NeedsEditorRestart = false;
        if (!BuildAll(true)) return;
        string msg = "구성 완료!\nAssets/Scenes/Main.unity 를 열어 Play 하세요.";
        if (NeedsEditorRestart)
            msg += "\n\n[권장] Active Input Handling을 'Both'로 바꿨습니다. 완전히 적용하려면 Unity를 한 번 재시작하세요.\n" +
                   "(재시작 전에도 새 Input System 경로로 동작하도록 만들어 두었습니다)";
        EditorUtility.DisplayDialog("HoleIt", msg, "확인");
    }

    /// <summary>CLI용 (Unity -batchmode -executeMethod HoleItBuilder.BuildAllCli).</summary>
    public static void BuildAllCli()
    {
        bool ok = BuildAll(false);
        EditorApplication.Exit(ok ? 0 : 1);
    }

    public static bool BuildAll(bool interactive)
    {
        // TMP Settings asset may not exist in a freshly extracted project until the
        // optional Essential Resources import is run. Assign the font directly from
        // the package resources instead, so the project can build its first scene
        // automatically without requiring a manual menu/import step.
        if (!EnsureDefaultFontAvailable())
        {
            string msg = "TextMeshPro 기본 폰트를 찾을 수 없습니다. Packages/com.unity.textmeshpro 패키지가 정상 설치되었는지 확인하세요.";
            if (interactive) EditorUtility.DisplayDialog("HoleIt", msg, "확인");
            else Debug.LogError("[HoleItBuilder] " + msg);
            return false;
        }

        EnsureLayers();
        EnsureFolders();
        ConfigureProject();

        Sprite circle, ring;
        Texture2D soft;
        CreateTextures(out circle, out ring, out soft);
        AudioClips clips = CreateAudio();

        Dictionary<string, Material> mats = CreateMaterials(soft);
        Dictionary<string, EdibleData> edibles = CreateEdibles(mats);
        GameObject[] fx = CreateFx(mats["Particle"]);
        GameSettings settings = CreateSettings();
        HoleSizeTable sizeTable = CreateSizeTable();
        LevelData[] levels = CreateLevels(edibles);

        BuildScene(mats, settings, sizeTable, levels, fx, clips, circle, ring);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        VerifyBuild();
        Debug.Log("[HoleItBuilder] 구성 완료: " + ScenePath);
        return true;
    }

    /// <summary>저장된 에셋/씬을 다시 읽어서 핵심 참조가 살아 있는지 확인하고, 문제가 있으면 Console에 빨간 에러로 알린다.</summary>
    private static void VerifyBuild()
    {
        GameSettings gs = AssetDatabase.LoadAssetAtPath<GameSettings>(ResDir + "/GameSettings.asset");
        HoleSizeTable st = AssetDatabase.LoadAssetAtPath<HoleSizeTable>(ResDir + "/HoleSizeTable.asset");
        if (gs == null) Debug.LogError("[HoleItBuilder] GameSettings.asset 을 다시 읽지 못했습니다: " + ResDir);
        if (st == null) Debug.LogError("[HoleItBuilder] HoleSizeTable.asset 을 다시 읽지 못했습니다: " + ResDir);

        UnityEngine.SceneManagement.Scene scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(ScenePath, UnityEditor.SceneManagement.OpenSceneMode.Single);
        int bad = 0;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (MonoBehaviour mb in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (mb == null) { Debug.LogError("[HoleItBuilder] 씬에 Missing Script 가 있습니다: " + root.name); bad++; continue; }
                if (!(mb is GameManager) && !(mb is InputManager) && !(mb is HoleController) && !(mb is LevelManager)) continue;
                SerializedObject so = new SerializedObject(mb);
                SerializedProperty it = so.GetIterator();
                bool enter = true;
                while (it.NextVisible(enter))
                {
                    enter = false;
                    if (it.propertyType != SerializedPropertyType.ObjectReference || it.name == "m_Script") continue;
                    if (it.objectReferenceValue == null)
                    {
                        Debug.LogError("[HoleItBuilder] 연결 누락: " + mb.GetType().Name + "." + it.name);
                        bad++;
                    }
                }
            }
        }
        if (bad == 0) Debug.Log("[HoleItBuilder] 연결 검사 통과 (GameSettings/HoleSizeTable 포함)");
    }

    // ───────────────────────── 레이어 ─────────────────────────

    private static void EnsureLayers()
    {
        Object[] tm = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (tm == null || tm.Length == 0) { Debug.LogError("TagManager를 찾을 수 없습니다."); return; }
        SerializedObject so = new SerializedObject(tm[0]);
        SerializedProperty layers = so.FindProperty("layers");
        SetLayer(layers, 8, Layers.GroundName);
        SetLayer(layers, 9, Layers.EdibleName);
        SetLayer(layers, 10, Layers.FallingName);
        so.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets();
    }

    private static void SetLayer(SerializedProperty layers, int index, string layerName)
    {
        SerializedProperty p = layers.GetArrayElementAtIndex(index);
        if (string.IsNullOrEmpty(p.stringValue) || p.stringValue == layerName) p.stringValue = layerName;
        else Debug.LogWarning("레이어 " + index + " 는 이미 '" + p.stringValue + "' 입니다. '" + layerName + "' 를 직접 지정해 주세요.");
    }

    // ───────────────────────── 폴더 / 프로젝트 설정 ─────────────────────────

    private static void EnsureFolders()
    {
        string[] dirs = { Root, ResDir, MatDir, MeshDir, PrefabDir, DataDir, LevelDir, EdibleDir, TexDir, AudioDir, SceneDir };
        for (int i = 0; i < dirs.Length; i++) EnsureFolder(dirs[i]);
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    /// <summary>모바일(Android/iOS) 공통 설정. 스토어 배포용이 아니라 직접 APK 설치 기준.</summary>
    private static void ConfigureProject()
    {
        PlayerSettings.companyName = "PUVKID";
        PlayerSettings.productName = "Hole It";
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.allowedAutorotateToPortrait = true;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = false;
        PlayerSettings.allowedAutorotateToLandscapeRight = false;
        PlayerSettings.gpuSkinning = false;
        PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android, "com.puvkid.holeit");
        PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.iOS, "com.puvkid.holeit");
        PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
        PlayerSettings.Android.renderOutsideSafeArea = false;
        PlayerSettings.iOS.targetOSVersionString = "15.0";

        // Active Input Handling: 2 = Both (레거시 Input + Input System). 이 프로젝트는 레거시 Input/StandaloneInputModule을 쓴다.
        // New(1) 전용이면 터치와 UI 버튼이 전혀 동작하지 않는다.
        Object[] ps = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
        if (ps != null && ps.Length > 0)
        {
            SerializedObject so = new SerializedObject(ps[0]);
            SerializedProperty p = so.FindProperty("activeInputHandler");
            if (p != null) { p.intValue = 2; so.ApplyModifiedPropertiesWithoutUndo(); AssetDatabase.SaveAssets(); }
        }

#if !ENABLE_LEGACY_INPUT_MANAGER
        NeedsEditorRestart = true;
#endif

        QualitySettings.vSyncCount = 0;
        QualitySettings.shadows = ShadowQuality.HardOnly;
        QualitySettings.shadowResolution = ShadowResolution.Medium;
        QualitySettings.shadowDistance = 42f;
        QualitySettings.antiAliasing = 2;
        QualitySettings.skinWeights = SkinWeights.TwoBones;
    }

    // ───────────────────────── 공용 헬퍼 ─────────────────────────

    private static void SetRef(Object target, string field, Object value)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty p = so.FindProperty(field);
        if (p == null) { Debug.LogError("필드 없음: " + target.GetType().Name + "." + field); return; }
        if (value == null) Debug.LogError("[HoleItBuilder] 연결할 대상이 null: " + target.GetType().Name + "." + field);
        p.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
        if (value != null && p.objectReferenceValue != value)
            Debug.LogError("[HoleItBuilder] 연결 실패: " + target.GetType().Name + "." + field);
    }

    private static void SetRefs(Object target, string field, Object[] values)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty p = so.FindProperty(field);
        if (p == null) { Debug.LogError("필드 없음: " + target.GetType().Name + "." + field); return; }
        p.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetFloat(Object target, string field, float v)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty p = so.FindProperty(field);
        if (p == null) { Debug.LogError("필드 없음: " + target.GetType().Name + "." + field); return; }
        p.floatValue = v;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>
    /// 시각 전용 프리미티브(마스크 쿼드/Pit 원통/림 쿼드)를 만든다.
    /// 콜라이더는 제거한다: Pit 원통이나 마스크 쿼드에 콜라이더가 남으면 물체/물리 판정을 방해한다.
    /// (Scene.cs 에서 호출되지만 정의가 빠져 있어 Editor 어셈블리 전체가 컴파일 실패하던 문제)
    /// </summary>
    private static GameObject Prim(PrimitiveType type, Transform parent, string name, Vector3 localPos, Vector3 localScale, Material mat)
    {
        GameObject g = GameObject.CreatePrimitive(type);
        g.name = name;
        g.transform.SetParent(parent, false);
        g.transform.localPosition = localPos;
        g.transform.localRotation = Quaternion.identity;
        g.transform.localScale = localScale;

        Collider col = g.GetComponent<Collider>();
        if (col != null) Object.DestroyImmediate(col);

        MeshRenderer mr = g.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
        }
        return g;
    }

    private static T SaveAsset<T>(T asset, string path) where T : Object
    {
        T existing = AssetDatabase.LoadAssetAtPath<T>(path);
        if (existing != null)
        {
            EditorUtility.CopySerialized(asset, existing);
            EditorUtility.SetDirty(existing);
            Object.DestroyImmediate(asset);
            return existing;
        }
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }
}
