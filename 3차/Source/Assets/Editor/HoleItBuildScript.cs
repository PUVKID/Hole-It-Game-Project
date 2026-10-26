
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
public static class HoleItBuildScript
{
    private const string ScenePath="Assets/Scenes/Main.unity";
    private const string OutputPath="Builds/Android/HoleIt.apk";
    [MenuItem("HoleIt/2. Android APK 빌드")]
    public static void BuildAndroidMenu(){try{var r=Build();EditorUtility.DisplayDialog("Pocket Pit",r.summary.result+"\n"+OutputPath,"확인");}catch(Exception ex){Debug.LogException(ex);EditorUtility.DisplayDialog("Pocket Pit","빌드 차단 또는 실패\n"+ex.Message,"확인");}}
    public static void BuildAndroid(){try{var r=Build();EditorApplication.Exit(r.summary.result==BuildResult.Succeeded?0:1);}catch(Exception ex){Debug.LogException(ex);EditorApplication.Exit(1);}}
    private static BuildReport Build(){
        if(!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android,BuildTarget.Android))throw new InvalidOperationException("Android Build Support, SDK/NDK and OpenJDK must be installed for this Unity editor.");
        if(!File.Exists(ScenePath))throw new FileNotFoundException("Main scene missing",ScenePath);
        HoleItProjectValidation.ValidateOrThrow();
        if(PlayerSettings.defaultInterfaceOrientation!=UIOrientation.Portrait)throw new InvalidOperationException("Project must use Portrait orientation.");
        if(string.IsNullOrEmpty(PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android)))throw new InvalidOperationException("Android application identifier is empty.");
        if(PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android)!=ScriptingImplementation.IL2CPP)throw new InvalidOperationException("Select IL2CPP in Android Player Settings; backend will not be changed silently.");
        if((PlayerSettings.Android.targetArchitectures&AndroidArchitecture.ARM64)==0)throw new InvalidOperationException("ARM64 must be enabled.");
        if(EditorUserBuildSettings.activeBuildTarget!=BuildTarget.Android){
            if(Application.isBatchMode)throw new InvalidOperationException("Launch Unity with -buildTarget Android before -executeMethod HoleItBuildScript.BuildAndroid.");
            if(!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android,BuildTarget.Android))throw new InvalidOperationException("Could not switch to Android.");
            throw new InvalidOperationException("Android target selected. Allow the Editor to finish recompiling, then run HoleIt > 2. Android APK 빌드 again.");
        }
        EditorUserBuildSettings.buildAppBundle=false;Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName=OutputPath,target=BuildTarget.Android,options=BuildOptions.None});
        Directory.CreateDirectory("TestResults");File.WriteAllText("TestResults/AndroidBuild.txt",DateTime.UtcNow.ToString("O")+"\n"+report.summary.result+"\nBytes: "+report.summary.totalSize+"\nErrors: "+report.summary.totalErrors);
        if(report.summary.result==BuildResult.Succeeded&&!File.Exists(OutputPath))throw new IOException("Build reported success but APK does not exist");return report;
    }
}
