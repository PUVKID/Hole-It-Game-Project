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


    [MenuItem("HoleIt/1. 프로젝트 전체 구성")]
    public static void BuildAllMenu()
    {
        if (!BuildAll(true)) return;
        string msg = "구성 완료!\nAssets/Scenes/Main.unity 를 열어 Play 하세요.";
        EditorUtility.DisplayDialog("HoleIt", msg, "확인");
    }

    /// <summary>CLI용 (Unity -batchmode -executeMethod HoleItBuilder.BuildAllCli).</summary>
    public static void BuildAllCli()
    {
        bool ok = BuildAll(false);
        EditorApplication.Exit(ok ? 0 : 1);
    }


    private static bool building;
    public static bool BuildAll(bool interactive)
    {
        if(building||EditorApplication.isCompiling) return false;
        if(interactive && File.Exists(ScenePath) && !EditorUtility.DisplayDialog("Pocket Pit", "Main 씬을 다시 구성합니다. 기존 씬은 Documentation/SceneBackups에 백업하며 레벨/설정의 Inspector 수정값은 유지합니다.", "재구성", "취소"))return false;
        if(interactive && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return false;
        building=true;
        try {
            EnsureFolders();EnsureLayers();ConfigureProject();modelMeshCache.Clear();
            if(!EnsureDefaultFontAvailable())throw new System.InvalidOperationException("Unity built-in LegacyRuntime font is unavailable");
            if(File.Exists(ScenePath)){Directory.CreateDirectory("Documentation/SceneBackups");File.Copy(ScenePath,"Documentation/SceneBackups/Main_"+System.DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff")+".unity",true);}
            Sprite circle=AssetDatabase.LoadAssetAtPath<Sprite>(TexDir+"/Circle.png"),ring=AssetDatabase.LoadAssetAtPath<Sprite>(TexDir+"/Ring.png");Texture2D soft;
            CreateTextures(out circle,out ring,out soft);
            AudioClips clips=CreateAudio();Dictionary<string,Material> mats=CreateMaterials(soft);
            Dictionary<string,EdibleData> edibles=CreateEdibles(mats);GameObject[] fx=CreateFx(mats["Particle"]);
            GameSettings settings=CreateSettings();HoleSizeTable sizes=CreateSizeTable();LevelData[] levels=CreateLevels(edibles);
            AssetDatabase.SaveAssets();AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach(var level in levels){var errors=LevelValidator.Validate(level,sizes);if(errors.Count>0)throw new System.InvalidOperationException(level.name+": "+string.Join("; ",errors));}
            BuildScene(mats,settings,sizes,levels,fx,clips,circle,ring);AssetDatabase.SaveAssets();
            HoleItProjectValidation.ValidateOrThrow();Debug.Log("[HoleItBuilder] 구성 및 Editor 데이터 검사 완료: "+ScenePath);return true;
        } catch(System.Exception ex){Debug.LogException(ex);if(interactive)EditorUtility.DisplayDialog("Pocket Pit","구성 실패. Console의 첫 오류를 확인하세요.\n"+ex.Message,"확인");return false;}
        finally {building=false;}
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
        PlayerSettings.defaultInterfaceOrientation=UIOrientation.Portrait;
        PlayerSettings.allowedAutorotateToPortrait=true;PlayerSettings.allowedAutorotateToPortraitUpsideDown=false;
        PlayerSettings.allowedAutorotateToLandscapeLeft=false;PlayerSettings.allowedAutorotateToLandscapeRight=false;
        // Package identifier/backend/input settings already exist in the delivered project and are not silently overwritten.
        if(string.IsNullOrEmpty(PlayerSettings.productName)||PlayerSettings.productName=="Hole It")PlayerSettings.productName="Pocket Pit";
        if(string.IsNullOrEmpty(PlayerSettings.companyName))PlayerSettings.companyName="Independent Studio";
        try {
            if(PlayerSettings.GetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android).StartsWith("com.DefaultCompany"))
                PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android,"com.pocketpit.game");
            if(PlayerSettings.GetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android)!=ScriptingImplementation.IL2CPP)
                PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android,ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64;
            if(PlayerSettings.Android.minSdkVersion<AndroidSdkVersions.AndroidApiLevel26)PlayerSettings.Android.minSdkVersion=AndroidSdkVersions.AndroidApiLevel26;
        } catch(System.Exception ex){Debug.LogWarning("[HoleItBuilder] Android 기본 설정 적용 건너뜀: "+ex.Message);}
        QualitySettings.vSyncCount=0;QualitySettings.shadows=ShadowQuality.HardOnly;
        QualitySettings.shadowResolution=ShadowResolution.Medium;QualitySettings.shadowDistance=35;
        QualitySettings.antiAliasing=2;
    }

    // ───────────────────────── 공용 헬퍼 ─────────────────────────

    private static void SetRef(Object target, string field, Object value)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty p = so.FindProperty(field);
        if (p == null) throw new System.MissingFieldException(target.GetType().Name,field);
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
        if (p == null) throw new System.MissingFieldException(target.GetType().Name,field);
        p.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetFloat(Object target, string field, float v)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty p = so.FindProperty(field);
        if (p == null) throw new System.MissingFieldException(target.GetType().Name,field);
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
        // 주 오브젝트 이름은 파일명과 같아야 한다 (new Material(shader) 는 셰이더 이름을 name 으로 가져 경고가 났음).
        asset.name = Path.GetFileNameWithoutExtension(path);
        T existing = AssetDatabase.LoadAssetAtPath<T>(path);
        if (existing != null)
        {
            EditorUtility.CopySerialized(asset, existing);
            existing.name = asset.name;
            EditorUtility.SetDirty(existing);
            Object.DestroyImmediate(asset);
            return existing;
        }
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }
}
