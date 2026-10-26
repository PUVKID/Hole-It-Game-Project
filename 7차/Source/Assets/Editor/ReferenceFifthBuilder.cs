using System.IO;
using UnityEditor;
using UnityEngine;
public static class ReferenceFifthBuilder {
    public static void RefreshAudio(){
        foreach(var path in Directory.GetFiles("Assets/Resources/ReferenceStyle","Booster*.png")){var t=(TextureImporter)AssetImporter.GetAtPath(path.Replace('\\','/'));t.textureType=TextureImporterType.Sprite;t.spriteImportMode=SpriteImportMode.Single;t.mipmapEnabled=false;t.alphaIsTransparency=true;t.SaveAndReimport();}
        foreach(var path in Directory.GetFiles("Assets/Resources/CotonArt","*.png")){var t=(TextureImporter)AssetImporter.GetAtPath(path.Replace('\\','/'));t.npotScale=TextureImporterNPOTScale.None;t.SaveAndReimport();}
        var c=Resources.Load<ReferenceCatalog>("ReferenceCatalog");
        string[] names={"item-collect-bubble","hole_grow_01","GameFail","_win","BoosteCollect","Bomb","_button_click_new","goal-complete","Freeze","speed_booster"};
        c.sounds=new AudioClip[names.Length];for(int i=0;i<names.Length;i++){c.sounds[i]=Resources.Load<AudioClip>("ReferenceAudio/"+names[i]);if(c.sounds[i]==null)throw new System.Exception("Missing original audio "+names[i]);}
        EditorUtility.SetDirty(c);AssetDatabase.SaveAssets();ReferenceCollisionBuilder.Apply();
    }
    public static void Prepare(){
        foreach(var folder in new[]{"Assets/Resources/CotonArt","Assets/Resources/ReferenceStyle"})foreach(var path in Directory.GetFiles(folder,"*.png")){
            var t=(TextureImporter)AssetImporter.GetAtPath(path.Replace('\\','/'));
            t.maxTextureSize=2048;t.npotScale=TextureImporterNPOTScale.None;t.mipmapEnabled=false;t.filterMode=FilterMode.Bilinear;t.textureCompression=TextureImporterCompression.CompressedHQ;
            t.textureType=folder.Contains("CotonArt")||path.EndsWith("OriginalHole.png")?TextureImporterType.Default:TextureImporterType.Sprite;
            t.spriteImportMode=SpriteImportMode.Single;t.alphaIsTransparency=t.textureType==TextureImporterType.Sprite;t.SaveAndReimport();
        }
        ReferenceBuilder.Prepare();
        QualitySettings.antiAliasing=4;QualitySettings.shadowResolution=ShadowResolution.High;QualitySettings.shadowDistance=32;
        AssetDatabase.SaveAssets();Debug.Log("FIFTH EDITION CONTENT PREPARED");
    }
}
