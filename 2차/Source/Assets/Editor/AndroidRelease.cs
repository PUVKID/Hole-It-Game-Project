using UnityEditor;
using UnityEngine.Rendering;
public static class AndroidRelease
{
    public static void Build(){
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android,false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,new[]{GraphicsDeviceType.OpenGLES3});
        AssetDatabase.SaveAssets();
        HoleItBuildScript.BuildAndroid();
    }
}
