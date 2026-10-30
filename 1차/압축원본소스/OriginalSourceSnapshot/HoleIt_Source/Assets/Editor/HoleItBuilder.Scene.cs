using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>빌더 - Main 씬 구성 (조명, 카메라, 바닥, Hole, 시스템 오브젝트, UI 연결).</summary>
public static partial class HoleItBuilder
{
    private static GameObject NewGO(string name, Transform parent)
    {
        GameObject g = new GameObject(name);
        if (parent != null) g.transform.SetParent(parent, false);
        return g;
    }

    private static void BuildScene(System.Collections.Generic.Dictionary<string, Material> mats, GameSettings settings, HoleSizeTable sizeTable,
        LevelData[] levels, GameObject[] fx, AudioClips clips, Sprite circle, Sprite ring)
    {
        UnityEngine.SceneManagement.Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        int groundLayer = LayerMask.NameToLayer(Layers.GroundName);

        // ── 조명 ──
        GameObject lightGo = NewGO("Directional Light", null);
        Light light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.12f;
        light.color = new Color(1f, 0.97f, 0.91f);
        light.shadows = LightShadows.Hard;
        light.shadowStrength = 0.28f;
        light.shadowBias = 0.06f;
        light.shadowNormalBias = 0.18f;
        lightGo.transform.rotation = Quaternion.Euler(48f, -32f, 0f);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.82f, 0.78f, 0.95f);

        // ── 카메라 ──
        GameObject camGo = NewGO("Main Camera", null);
        camGo.tag = "MainCamera";
        Camera cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.18f, 0.07f, 0.34f);
        cam.orthographic = true;
        cam.orthographicSize = 7f;
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = 150f;
        cam.allowHDR = false;
        cam.allowMSAA = false;
        camGo.AddComponent<AudioListener>();
        CameraFollow camFollow = camGo.AddComponent<CameraFollow>();

        // ── 바닥 ──
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.layer = groundLayer;
        ground.GetComponent<MeshRenderer>().sharedMaterial = mats["Ground"];
        ground.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        ground.transform.localScale = new Vector3(2.4f, 1f, 2.4f);

        // ── Hole ──
        GameObject holeGo = NewGO("PlayerHole", null);
        Rigidbody holeRb = holeGo.AddComponent<Rigidbody>();
        holeRb.isKinematic = true;
        holeRb.useGravity = false;
        HoleController hole = holeGo.AddComponent<HoleController>();

        GameObject visualGo = NewGO("Visual", holeGo.transform);
        HoleVisualController visual = visualGo.AddComponent<HoleVisualController>();

        GameObject maskRoot = NewGO("MaskDisc", visualGo.transform);
        maskRoot.transform.localPosition = new Vector3(0f, 0.02f, 0f);
        GameObject maskQuad = Prim(PrimitiveType.Quad, maskRoot.transform, "Quad", Vector3.zero, Vector3.one, mats["Mask"]);
        maskQuad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        GameObject pit = Prim(PrimitiveType.Cylinder, visualGo.transform, "Pit", new Vector3(0f, -3f, 0f), new Vector3(2f, 3f, 2f), mats["Pit"]);

        GameObject rimRoot = NewGO("Rim", visualGo.transform);
        rimRoot.transform.localPosition = new Vector3(0f, 0.04f, 0f);
        GameObject rimQuad = Prim(PrimitiveType.Quad, rimRoot.transform, "Quad", Vector3.zero, Vector3.one, mats["Rim"]);
        rimQuad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        SetRef(visual, "maskDisc", maskRoot.transform);
        SetRef(visual, "pit", pit.transform);
        SetRef(visual, "rim", rimRoot.transform);

        GameObject swallowGo = NewGO("SwallowTrigger", holeGo.transform);
        SphereCollider sc = swallowGo.AddComponent<SphereCollider>();
        sc.isTrigger = true;
        sc.radius = 1.5f;
        HoleSwallow swallow = swallowGo.AddComponent<HoleSwallow>();
        SetRef(swallow, "hole", hole);

        SetRef(hole, "settings", settings);
        SetRef(hole, "sizeTable", sizeTable);
        SetRef(hole, "visual", visual);

        // ── Consume Zone (바닥 아래 넓은 트리거) ──
        GameObject consumeGo = NewGO("ConsumeZone", null);
        consumeGo.transform.position = new Vector3(0f, -3.2f, 0f);
        consumeGo.transform.localScale = new Vector3(200f, 1f, 200f);
        BoxCollider cc = consumeGo.AddComponent<BoxCollider>();
        cc.isTrigger = true;
        cc.size = Vector3.one;
        consumeGo.AddComponent<HoleConsume>();

        // ── 시스템 ──
        GameObject sys = NewGO("GameSystems", null);
        GameObject poolRoot = NewGO("PoolRoot", sys.transform);

        ObjectPooler pooler = sys.AddComponent<ObjectPooler>();
        SetRef(pooler, "poolRoot", poolRoot.transform);

        MissionManager missions = sys.AddComponent<MissionManager>();
        HighScoreManager highScores = sys.AddComponent<HighScoreManager>();

        LevelManager levelMgr = sys.AddComponent<LevelManager>();
        SetRefs(levelMgr, "levels", levels);
        SetRef(levelMgr, "pooler", pooler);
        SetRef(levelMgr, "hole", hole);
        SetRef(levelMgr, "holeSwallow", swallow);
        SetRef(levelMgr, "missionManager", missions);
        SetRef(levelMgr, "ground", ground.transform);
        SetRef(levelMgr, "consumeZone", consumeGo.transform);

        SetRef(camFollow, "hole", hole);

        // ── 오디오 ──
        GameObject audioGo = NewGO("Audio", sys.transform);
        AudioManager audio = audioGo.AddComponent<AudioManager>();
        Object[] sources = new Object[6];
        for (int i = 0; i < sources.Length; i++) sources[i] = MakeSource("Sfx" + i, audioGo.transform, false);
        AudioSource loopSrc = MakeSource("MoveLoop", audioGo.transform, true);
        SetRefs(audio, "sfxSources", sources);
        SetRef(audio, "moveLoopSource", loopSrc);
        SetRef(audio, "moveLoop", clips.moveLoop);
        SetRef(audio, "attract", clips.attract);
        SetRef(audio, "swallow", clips.swallow);
        SetRef(audio, "growth", clips.growth);
        SetRef(audio, "missionComplete", clips.mission);
        SetRef(audio, "levelClear", clips.clear);
        SetRef(audio, "timeWarning", clips.warning);
        SetRef(audio, "fail", clips.fail);
        SetRef(audio, "button", clips.button);

        // ── UI ──
        UIBuild ui = BuildUI(circle, ring);

        InputManager input = sys.AddComponent<InputManager>();
        SetRef(input, "settings", settings);
        SetRef(input, "joystickUI", ui.joystick);

        // 기기에서 에러를 바로 볼 수 있는 오버레이 (문제 없으면 아무것도 안 그림)
        sys.AddComponent<RuntimeErrorOverlay>();

        // ── GameManager ──
        GameManager gm = sys.AddComponent<GameManager>();
        SetRef(gm, "settings", settings);
        SetRef(gm, "levelManager", levelMgr);
        SetRef(gm, "missionManager", missions);
        SetRef(gm, "hole", hole);
        SetRef(gm, "cameraFollow", camFollow);
        SetRef(gm, "ui", ui.manager);
        SetRef(gm, "highScores", highScores);
        SetRef(gm, "input", input);
        SetRef(gm, "swallowFx", fx[0]);
        SetRef(gm, "growthFx", fx[1]);
        SetRef(gm, "missionFx", fx[2]);
        SetRef(gm, "clearFx", fx[3]);

        // ── EventSystem ──
        GameObject es = NewGO("EventSystem", null);
        es.AddComponent<EventSystem>();
        es.AddComponent<UIInputModuleSelector>();   // 입력 설정에 맞는 UI 입력 모듈을 런타임에 구성

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
    }

    private static AudioSource MakeSource(string name, Transform parent, bool loop)
    {
        GameObject g = NewGO(name, parent);
        AudioSource s = g.AddComponent<AudioSource>();
        s.playOnAwake = false;
        s.loop = loop;
        s.spatialBlend = 0f;
        return s;
    }
}
