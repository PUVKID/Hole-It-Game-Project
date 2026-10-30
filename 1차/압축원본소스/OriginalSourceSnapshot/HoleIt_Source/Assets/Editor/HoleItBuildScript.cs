using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Android APK 빌드 (직접 설치용, 스토어 배포 설정 아님).
/// 메뉴: HoleIt ▸ 2. Android APK 빌드     CLI: -executeMethod HoleItBuildScript.BuildAndroid
/// 결과: 프로젝트 루트 Builds/HoleIt.apk
/// </summary>
public static class HoleItBuildScript
{
    private const string ScenePath = "Assets/Scenes/Main.unity";
    private const string OutputPath = "Builds/HoleIt.apk";

    [MenuItem("HoleIt/2. Android APK 빌드")]
    public static void BuildAndroidMenu()
    {
        if (!File.Exists(ScenePath))
        {
            EditorUtility.DisplayDialog("HoleIt", "Main 씬이 없습니다. 먼저 메뉴 HoleIt ▸ 1. 프로젝트 전체 구성 을 실행하세요.", "확인");
            return;
        }
        BuildReport r = Build();
        string msg = r.summary.result == BuildResult.Succeeded
            ? "빌드 성공!\n" + OutputPath + "\n크기: " + (r.summary.totalSize / (1024 * 1024)) + " MB"
            : "빌드 실패: " + r.summary.result + "\n콘솔 로그를 확인하세요.";
        EditorUtility.DisplayDialog("HoleIt", msg, "확인");
    }

    /// <summary>CLI용 진입점.</summary>
    public static void BuildAndroid()
    {
        BuildReport r = Build();
        EditorApplication.Exit(r.summary.result == BuildResult.Succeeded ? 0 : 1);
    }

    private static BuildReport Build()
    {
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            EditorUserBuildSettings.SwitchActiveBuildTarget(NamedBuildTarget.Android, BuildTarget.Android);

        EditorUserBuildSettings.buildAppBundle = false;      // APK
        EditorUserBuildSettings.development = false;
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Android, ManagedStrippingLevel.Low);

        Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
        BuildPlayerOptions opts = new BuildPlayerOptions
        {
            scenes = new[] { ScenePath },
            locationPathName = OutputPath,
            target = BuildTarget.Android,
            options = BuildOptions.None
        };
        BuildReport report = BuildPipeline.BuildPlayer(opts);
        Debug.Log("[HoleItBuild] 결과: " + report.summary.result + " / " + OutputPath);
        return report;
    }
}
