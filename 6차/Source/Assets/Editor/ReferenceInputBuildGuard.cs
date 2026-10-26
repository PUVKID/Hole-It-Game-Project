using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

// Reject a build whose controls would be compiled out, including builds from the GUI.
public sealed class ReferenceInputBuildGuard : IPreprocessBuildWithReport {
    public int callbackOrder=>-1000;
    public void OnPreprocessBuild(BuildReport report){
        var settings=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
        var handler=settings.FindProperty("activeInputHandler");
        if(handler==null||handler.intValue!=1||!ReferenceGame.NativeInputEnabled)
            throw new BuildFailedException("ReferenceGame requires Active Input Handling = Input System Package (New). Set it in Player Settings, restart Unity, and rebuild.");
    }
}
