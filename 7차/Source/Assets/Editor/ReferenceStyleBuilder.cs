using System.IO;
using UnityEngine;
using UnityEditor;
public static class ReferenceStyleBuilder {
    [System.Serializable]class Export {public Mat[] materials;}
    [System.Serializable]class Mat {public string key,name;public float[] color;}
    public static void RebuildAndPreview(){ReferenceBuilder.Prepare();Finish();}
    public static void Finish(){
        var catalog=Resources.Load<ReferenceCatalog>("ReferenceCatalog");var paths=new System.Collections.Generic.HashSet<string>();foreach(var m in catalog.environments)foreach(var p in m.parts)paths.Add(AssetDatabase.GetAssetPath(p.material));
        var data=JsonUtility.FromJson<Export>(File.ReadAllText("Assets/ReferenceContent/Models.json"));foreach(var a in data.materials){string path="Assets/ReferenceContent/Built/"+a.key+".mat";if(!paths.Contains(path))continue;var m=AssetDatabase.LoadAssetAtPath<Material>(path);m.name=a.name;m.shader=Shader.Find(a.name.ToLowerInvariant().Contains("transparent")?"PocketPit/ReferenceEnvironmentTransparent":"PocketPit/ReferenceEnvironment");m.color=new Color(a.color[0],a.color[1],a.color[2],a.color[3]);m.enableInstancing=true;EditorUtility.SetDirty(m);}AssetDatabase.SaveAssets();ReleasePreparation.BuildWindows();
    }
    public static void Prepare(){
        const string folder="Assets/Resources/ReferenceStyle";
        foreach(string path in Directory.GetFiles(folder,"*.png")){
            var importer=(TextureImporter)AssetImporter.GetAtPath(path.Replace('\\','/'));
            importer.maxTextureSize=1024;importer.alphaIsTransparency=true;importer.mipmapEnabled=false;
            importer.textureType=Path.GetFileName(path)=="OriginalHole.png"?TextureImporterType.Default:TextureImporterType.Sprite;
            importer.spriteImportMode=SpriteImportMode.Single;importer.SaveAndReimport();
        }
        var mesh=new Mesh{name="Original Hole"};
        using(var r=new BinaryReader(File.OpenRead(folder+"/OriginalHole.bytes"))){int count=r.ReadInt32(),subs=r.ReadInt32();var v=new Vector3[count];var n=new Vector3[count];var uv=new Vector2[count];for(int i=0;i<count;i++){v[i]=new Vector3(r.ReadSingle(),r.ReadSingle(),r.ReadSingle());n[i]=new Vector3(r.ReadSingle(),r.ReadSingle(),r.ReadSingle());uv[i]=new Vector2(r.ReadSingle(),r.ReadSingle());}mesh.vertices=v;mesh.normals=n;mesh.uv=uv;mesh.subMeshCount=subs;for(int i=0;i<subs;i++){int len=r.ReadInt32();var tri=new int[len];for(int j=0;j<len;j++)tri[j]=r.ReadInt32();mesh.SetTriangles(tri,i);}mesh.RecalculateBounds();}
        string meshPath=folder+"/OriginalHole.asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);if(old==null)AssetDatabase.CreateAsset(mesh,meshPath);else{EditorUtility.CopySerialized(mesh,old);Object.DestroyImmediate(mesh);}
        string matPath=folder+"/OriginalHole.mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(matPath);if(mat==null){mat=new Material(Shader.Find("Unlit/Texture"));AssetDatabase.CreateAsset(mat,matPath);}mat.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"/OriginalHole.png");mat.renderQueue=2010;EditorUtility.SetDirty(mat);
        AssetDatabase.SaveAssets();ReferenceBuilder.Validate();ReleasePreparation.BuildWindows();
    }
}
