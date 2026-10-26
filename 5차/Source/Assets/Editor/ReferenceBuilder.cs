using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UnityEditor.Build.Reporting;
public static class ReferenceBuilder {
    public static bool SkipTextureImport; public static void RefreshCatalog(){SkipTextureImport=true;Prepare();QualitySettings.antiAliasing=4;AssetDatabase.SaveAssets();}
    const string Folder="Assets/ReferenceContent";
    [Serializable] class Export {public Model[] models,environments;public Mat[] materials;}
    [Serializable] class Model {public int id;public string name,icon;public Node[] nodes;public float mass=1,linearDamping,angularDamping=1;}
    [Serializable] class ColliderInfo {public string kind,mesh;public float[] center,size;public float radius,height;public int direction;}
    [Serializable] class Node {public ColliderInfo[] colliders;public int parent;public string name,mesh;public string[] materials;public float[] position,rotation,scale;}
    [Serializable] class Mat {public string key,name,texture;public float[] color;public float smoothness,edgeStart,edgeEnd,gloss;public float[] shadow;}
    static Vector3 V(float[] a){return new Vector3(a[0],a[1],a[2]);}
    static Quaternion Q(float[] a){return new Quaternion(a[0],a[1],a[2],a[3]);}
    public static void Prepare(){
        Directory.CreateDirectory(Folder+"/Built");Directory.CreateDirectory("Assets/Resources");
        // Keep the previously delivered scene as a source reference, outside build settings.
        if(!File.Exists("Assets/Scenes/Edition2.unity"))AssetDatabase.CopyAsset("Assets/Scenes/Main.unity","Assets/Scenes/Edition2.unity");
        EditorSceneManager.OpenScene("Assets/Scenes/Edition2.unity");
        var old=GameObject.Find("PlayerHole/Visual");if(old==null)throw new Exception("Hole visual missing");
        var clone=UnityEngine.Object.Instantiate(old);clone.name="SimpleHole";clone.transform.position=Vector3.zero;
        var holePrefab=PrefabUtility.SaveAsPrefabAsset(clone,Folder+"/Built/SimpleHole.prefab");UnityEngine.Object.DestroyImmediate(clone);
        var ground=GameObject.Find("Ground").GetComponent<Renderer>().sharedMaterial;
        if(!SkipTextureImport)foreach(var path in Directory.GetFiles(Folder,"*.png")){
            var ti=(TextureImporter)AssetImporter.GetAtPath(path.Replace('\\','/'));if(ti==null)continue;
            ti.maxTextureSize=2048;ti.filterMode=FilterMode.Trilinear;ti.mipmapEnabled=!Path.GetFileName(path).StartsWith("Icon");ti.textureCompression=TextureImporterCompression.Compressed;
            if(Path.GetFileName(path).StartsWith("Icon")){ti.textureType=TextureImporterType.Sprite;ti.spriteImportMode=SpriteImportMode.Single;ti.alphaIsTransparency=true;}
            ti.SaveAndReimport();
        }
        var exp=JsonUtility.FromJson<Export>(File.ReadAllText(Folder+"/Models.json"));var mats=new Dictionary<string,Material>();var meshes=new Dictionary<string,Mesh>();
        foreach(var a in exp.materials){
            string path=Folder+"/Built/"+a.key+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(mat==null){mat=new Material(Shader.Find("PocketPit/ReferenceSoft"));AssetDatabase.CreateAsset(mat,path);}
            mat.shader=Shader.Find("PocketPit/ReferenceSoft");mat.name=a.name;mat.color=new Color(a.color[0],a.color[1],a.color[2],a.color[3]);mat.enableInstancing=true;mat.SetFloat("_Glossiness",a.gloss);mat.SetFloat("_EdgeStart",a.edgeStart);mat.SetFloat("_EdgeEnd",a.edgeEnd);if(a.shadow!=null)mat.SetColor("_ShadowColor",new Color(a.shadow[0],a.shadow[1],a.shadow[2],1));
            if(!string.IsNullOrEmpty(a.texture))mat.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/"+a.texture+".png");mats[a.key]=mat;EditorUtility.SetDirty(mat);
        }
        Mesh GetMesh(string key){if(meshes.TryGetValue(key,out var m))return m;string path=Folder+"/Built/"+key+".asset";m=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(m!=null){meshes[key]=m;return m;}
            using(var r=new BinaryReader(File.OpenRead(Folder+"/"+key+".bytes"))){int n=r.ReadInt32(),subs=r.ReadInt32();var vs=new Vector3[n];var ns=new Vector3[n];var uv=new Vector2[n];for(int i=0;i<n;i++){vs[i]=new Vector3(r.ReadSingle(),r.ReadSingle(),r.ReadSingle());ns[i]=new Vector3(r.ReadSingle(),r.ReadSingle(),r.ReadSingle());uv[i]=new Vector2(r.ReadSingle(),r.ReadSingle());}m=new Mesh{name=key,indexFormat=n>65535?IndexFormat.UInt32:IndexFormat.UInt16};m.vertices=vs;m.normals=ns;m.uv=uv;m.subMeshCount=subs;for(int i=0;i<subs;i++){int len=r.ReadInt32();var ts=new int[len];for(int j=0;j<len;j++)ts[j]=r.ReadInt32();m.SetTriangles(ts,i);}m.RecalculateBounds();AssetDatabase.CreateAsset(m,path);meshes[key]=m;return m;}}
        var models=new List<ReferenceModel>();
        foreach(var a in exp.models.Concat(exp.environments)){var matrices=new Matrix4x4[a.nodes.Length];var parts=new List<ReferencePart>();var collision=new List<CombineInstance>();Bounds bounds=new Bounds();bool any=false;
            for(int i=0;i<a.nodes.Length;i++){var n=a.nodes[i];matrices[i]=n.parent<0?Matrix4x4.identity:matrices[n.parent]*Matrix4x4.TRS(V(n.position),Q(n.rotation),V(n.scale));if(n.colliders!=null)foreach(var col in n.colliders){
                    if(!string.IsNullOrEmpty(col.mesh))collision.Add(new CombineInstance{mesh=GetMesh(col.mesh),transform=matrices[i]});
                    else {var primitive=GameObject.CreatePrimitive(col.kind=="SphereCollider"?PrimitiveType.Sphere:col.kind=="CapsuleCollider"?PrimitiveType.Capsule:PrimitiveType.Cube);var mesh=primitive.GetComponent<MeshFilter>().sharedMesh;Vector3 size=col.kind=="BoxCollider"?V(col.size):col.kind=="SphereCollider"?Vector3.one*col.radius*2:new Vector3(col.radius*2,col.height/2,col.radius*2);Quaternion orientation=col.kind=="CapsuleCollider"&&col.direction!=1?Quaternion.Euler(col.direction==2?90:0,0,col.direction==0?90:0):Quaternion.identity;collision.Add(new CombineInstance{mesh=mesh,transform=matrices[i]*Matrix4x4.TRS(V(col.center),orientation,size)});UnityEngine.Object.DestroyImmediate(primitive);}
                }if(string.IsNullOrEmpty(n.mesh))continue;var m=GetMesh(n.mesh);
                for(int sub=0;sub<m.subMeshCount;sub++)parts.Add(new ReferencePart{mesh=m,material=mats[n.materials[Mathf.Min(sub,n.materials.Length-1)]],submesh=sub,local=matrices[i]});
                for(int c=0;c<8;c++){var v=m.bounds.center+Vector3.Scale(m.bounds.extents,new Vector3((c&1)==0?-1:1,(c&2)==0?-1:1,(c&4)==0?-1:1));v=matrices[i].MultiplyPoint3x4(v);if(!any){bounds=new Bounds(v,Vector3.zero);any=true;}else bounds.Encapsulate(v);}
            }
            if(parts.Count==0)throw new Exception("Empty model "+a.name);
            Mesh collisionMesh=null;if(collision.Count>0){string path=Folder+"/Built/Collision_"+a.id+".asset";collisionMesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(collisionMesh==null){collisionMesh=new Mesh{name="Collision "+a.name,indexFormat=IndexFormat.UInt32};collisionMesh.CombineMeshes(collision.ToArray());AssetDatabase.CreateAsset(collisionMesh,path);}}
            models.Add(new ReferenceModel{collisionParts=collision.Select(c=>new ReferenceCollision{mesh=c.mesh,local=c.transform}).ToArray(),collisionMesh=collisionMesh,mass=a.mass,linearDamping=a.linearDamping,angularDamping=a.angularDamping,id=a.id,label=System.Text.RegularExpressions.Regex.Replace(a.name,"([a-z])([A-Z])","$1 $2"),icon=AssetDatabase.LoadAssetAtPath<Sprite>(Folder+"/"+a.icon+".png"),bounds=bounds,parts=parts.ToArray()});
        }
        var cat=AssetDatabase.LoadAssetAtPath<ReferenceCatalog>("Assets/Resources/ReferenceCatalog.asset");if(cat==null){cat=ScriptableObject.CreateInstance<ReferenceCatalog>();AssetDatabase.CreateAsset(cat,"Assets/Resources/ReferenceCatalog.asset");}
        var bombData=AssetDatabase.LoadAssetAtPath<EdibleData>(AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("Edible_Bomb t:EdibleData")[0]));var bm=bombData.prefab.GetComponentInChildren<MeshFilter>();var br=bombData.prefab.GetComponentInChildren<MeshRenderer>();br.sharedMaterial.enableInstancing=true;EditorUtility.SetDirty(br.sharedMaterial);cat.bomb=new ReferenceModel{id=-1,label="Bomb",bounds=bm.sharedMesh.bounds,parts=new[]{new ReferencePart{mesh=bm.sharedMesh,material=br.sharedMaterial,submesh=0}}};
        cat.models=models.Take(exp.models.Length).ToArray();cat.environments=models.Skip(exp.models.Length).ToArray();foreach(var m in cat.environments)foreach(var p in m.parts){p.material.shader=Shader.Find(p.material.name.ToLowerInvariant().Contains("transparent")?"PocketPit/ReferenceEnvironmentTransparent":"PocketPit/ReferenceEnvironment");EditorUtility.SetDirty(p.material);}cat.levels=JsonUtility.FromJson<ReferenceLevels>(File.ReadAllText(Folder+"/Levels.json")).levels;cat.holeVisual=holePrefab;cat.groundMaterial=ground;cat.font=Resources.Load<Font>("Fonts/NanumGothic-Bold");EditorUtility.SetDirty(cat);
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);new GameObject("GaeunGame").AddComponent<ReferenceGame>();
        EditorSceneManager.SaveScene(scene,"Assets/Scenes/Main.unity");EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/Main.unity",true)};
        PlayerSettings.productName="Gaeun's Hole";PlayerSettings.bundleVersion="5.0.0";PlayerSettings.Android.bundleVersionCode=7;
        PlayerSettings.gpuSkinning=false;PlayerSettings.SplashScreen.show=false;PlayerSettings.Android.optimizedFramePacing=true;
        QualitySettings.antiAliasing=2;QualitySettings.shadowCascades=1;QualitySettings.shadowResolution=ShadowResolution.Medium;QualitySettings.shadowDistance=24;QualitySettings.pixelLightCount=1;
        AssetDatabase.SaveAssets();Validate();Debug.Log("REFERENCE EDITION READY");
    }
    public static void Validate(){var c=Resources.Load<ReferenceCatalog>("ReferenceCatalog");if(c==null||c.levels.Length!=465||c.models.Length!=869||c.font==null||c.holeVisual==null)throw new Exception("Reference content incomplete");var ids=new HashSet<int>(c.models.Select(m=>m.id));foreach(var l in c.levels){if(l.time<60||l.points.Length<1)throw new Exception("Level data incomplete");foreach(var p in l.points)if(!ids.Contains(p.id))throw new Exception("Missing model "+p.id);} }
    public static void FinalizeAndPreview(){var c=Resources.Load<ReferenceCatalog>("ReferenceCatalog");c.sounds=new[]{"Swallow","Growth","Fail","Clear","Booster"}.Select(n=>AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/HoleIt/Audio/"+n+".wav")).ToArray();EditorUtility.SetDirty(c);AssetDatabase.SaveAssets();Validate();ReleasePreparation.BuildWindows();}
    public static void BuildAndroid(){try{Validate();PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android,false);PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,new[]{GraphicsDeviceType.OpenGLES3});Directory.CreateDirectory("Builds/Android");var r=BuildPipeline.BuildPlayer(new[]{"Assets/Scenes/Main.unity"},"Builds/Android/HoleIt.apk",BuildTarget.Android,BuildOptions.None);Directory.CreateDirectory("TestResults");File.WriteAllText("TestResults/AndroidBuild.txt",DateTime.UtcNow.ToString("O")+"\n"+r.summary.result+"\nErrors: "+r.summary.totalErrors);EditorApplication.Exit(r.summary.result==BuildResult.Succeeded?0:1);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}


