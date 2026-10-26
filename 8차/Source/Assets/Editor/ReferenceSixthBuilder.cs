using UnityEditor;
using UnityEngine;

public static class ReferenceSixthBuilder
{
    public static void Prepare()
    {
        ReferenceTypographyBuilder.Prepare();
        ReferenceBuilder.RefreshCatalog();
        ReferenceFifthBuilder.RefreshAudio();
        var needed=new System.Collections.Generic.HashSet<string>(AssetDatabase.GetDependencies("Assets/Resources/ReferenceCatalog.asset",true));
        foreach(var path in System.IO.Directory.GetFiles("Assets/ReferenceContent/Built","Collision_*.asset")){
            string normalized=path.Replace('\\','/');
            if(System.Text.RegularExpressions.Regex.IsMatch(System.IO.Path.GetFileName(path),@"^Collision_-?\d+\.asset$")&&!needed.Contains(normalized))
                AssetDatabase.DeleteAsset(normalized);
        }
        PlayerSettings.productName="Hole It!";
        PlayerSettings.bundleVersion="7.0.0";
        PlayerSettings.Android.bundleVersionCode=9;
        QualitySettings.antiAliasing=4;
        AssetDatabase.SaveAssets();
        Debug.Log("EDITION SIX PREPARED");
    }
}
