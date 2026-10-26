using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public static partial class HoleItBuilder
{
    [MenuItem("Pocket Pit/Build soft lighting and bomb edition")]
    public static void BuildSoftBombEdition(){
        EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");modelMeshCache.Clear();
        smoothSurface=AssetDatabase.LoadAssetAtPath<Material>(MatDir+"/SmoothSurface.mat");
        smoothSurface.SetFloat("_Glossiness",.28f);EditorUtility.SetDirty(smoothSurface);
        string[] keys={"Lipstick","Perfume","NailPolish","CreamJar","Compact","Lotion","Soap","MakeupBrush","Serum","Macaron","JuiceBottle","Chocolate"};
        var oldHeights=new Dictionary<EdibleData,float>();var mats=new Dictionary<string,Material>();
        foreach(string key in keys){var data=Edible(key);oldHeights[data]=data.stackHeight;BuildBeauty(key,data.displayName,mats);}
        var bomb=BuildBomb(mats);
        foreach(string guid in AssetDatabase.FindAssets("t:LevelData")){
            var level=AssetDatabase.LoadAssetAtPath<LevelData>(AssetDatabase.GUIDToAssetPath(guid));
            var placements=new List<SpawnPlacement>();
            foreach(var saved in level.placements){var p=saved;if(p.edible.objectType==ObjectType.Bomb)continue;
                if(oldHeights.TryGetValue(p.edible,out float old))p.height=Mathf.Round(p.height/old)*p.edible.stackHeight;
                placements.Add(p);
            }
            AddBombs(level,placements,bomb);
            level.placements=placements.ToArray();var counts=new Dictionary<EdibleData,int>();
            foreach(var p in placements){if(!counts.ContainsKey(p.edible))counts[p.edible]=0;counts[p.edible]++;}
            level.spawnGroups=new SpawnGroup[counts.Count];int group=0;foreach(var c in counts)level.spawnGroups[group++]=new SpawnGroup{edible=c.Key,count=c.Value};
            for(int m=0;m<level.missions.Length;m++){var mission=level.missions[m];if(mission.target!=null){mission.icon=mission.target.icon;mission.description=mission.target.displayName;}level.missions[m]=mission;}
            if(level.levelId<=8||level.levelId==14||level.levelId==21||level.levelId==29)level.cameraZoomMultiplier=1.28f;
            level.schemaVersion=12;EditorUtility.SetDirty(level);
        }
        SmoothScene();AssetDatabase.SaveAssets();HoleItProjectValidation.ValidateOrThrow();Debug.Log("SOFT BOMB EDITION READY");
    }

    private static void BeautyCylinder(Transform parent,string name,Material material,Vector3 pos,Vector3 scale,float bottomRadius=.5f,float topRadius=.5f){
        string key="SoftBeauty_v3_"+Mathf.RoundToInt(bottomRadius*100)+"_"+Mathf.RoundToInt(topRadius*100);
        AddModelPart(parent,name,ModelMesh(key,()=>SoftLathe(bottomRadius,topRadius,false)),material,pos,scale,Quaternion.identity);
    }
    private static Mesh SoftLathe(float bottom,float top,bool slanted){
        const int sides=64;
        Vector2[] profile={new Vector2(0,-.5f),new Vector2(bottom*.82f,-.5f),new Vector2(bottom*.95f,-.49f),new Vector2(bottom,-.455f),new Vector2(bottom,-.40f),new Vector2(top,.40f),new Vector2(top,.455f),new Vector2(top*.95f,.49f),new Vector2(top*.82f,.5f),new Vector2(0,.5f)};
        var vertices=new Vector3[(sides+1)*profile.Length];var normals=new Vector3[vertices.Length];var tris=new List<int>();
        for(int p=0;p<profile.Length;p++){
            var tangent=profile[Mathf.Min(p+1,profile.Length-1)]-profile[Mathf.Max(0,p-1)];
            var normal=new Vector2(tangent.y,-tangent.x).normalized;
            for(int s=0;s<=sides;s++){
                float a=s*Mathf.PI*2/sides,x=Mathf.Cos(a),z=Mathf.Sin(a);int i=p*(sides+1)+s;
                float y=profile[p].y+(slanted?profile[p].x*x*.5f*Mathf.InverseLerp(-.1f,.4f,profile[p].y):0);
                vertices[i]=new Vector3(profile[p].x*x,y,profile[p].x*z);
                normals[i]=new Vector3(normal.x*x,normal.y,normal.x*z).normalized;
                if(p<profile.Length-1&&s<sides){int b=i+sides+1;tris.Add(i);tris.Add(b);tris.Add(i+1);tris.Add(i+1);tris.Add(b);tris.Add(b+1);}
            }
        }
        var mesh=new Mesh();mesh.vertices=vertices;mesh.normals=normals;mesh.SetTriangles(tris,0);mesh.RecalculateBounds();return mesh;
    }
    private static EdibleData BuildBomb(Dictionary<string,Material> mats){
        var data=Edible("Bomb");if(data==null)data=SaveAsset(ScriptableObject.CreateInstance<EdibleData>(),EdibleDir+"/Edible_Bomb.asset");
        data.objectId="Bomb";data.displayName="폭탄 · 삼키면 실패";data.objectType=ObjectType.Bomb;data.sizeTier=1;
        data.score=0;data.growthValue=0;data.canBeSwallowed=true;data.overrideWrongBehavior=true;data.wrongBehavior=WrongTargetBehavior.None;data.swallowPriority=-100;
        var body=ColorMat(mats,"BombGraphite",new Color(.10f,.12f,.15f));var red=ColorMat(mats,"BombWarningRed",new Color(.75f,.16f,.12f));
        var fuse=ColorMat(mats,"BombFuse",new Color(.48f,.36f,.20f));var ember=ColorMat(mats,"BombEmber",new Color(1f,.51f,.09f));
        var root=new GameObject("Edible_Bomb");var t=root.transform;
        PartSphere(t,"Round bomb",body,new Vector3(0,.42f,0),new Vector3(.84f,.84f,.84f));
        PartTorus(t,"Red warning band",red,new Vector3(0,.42f,0),Vector3.one, .409f,.026f);
        BeautyCylinder(t,"Fuse socket",body,new Vector3(0,.84f,0),new Vector3(.26f,.15f,.26f));
        PartCylinder(t,"Fuse",fuse,new Vector3(.035f,.98f,0),new Vector3(.065f,.19f,.065f),.5f,.5f,Quaternion.Euler(0,0,-25));
        PartSphere(t,"Amber fuse tip",ember,new Vector3(.075f,1.075f,0),Vector3.one*.11f);
        PartBox(t,"Warning stroke one",red,new Vector3(0,.46f,-.414f),new Vector3(.29f,.065f,.018f),.1f,Quaternion.Euler(0,0,45));
        PartBox(t,"Warning stroke two",red,new Vector3(0,.46f,-.416f),new Vector3(.29f,.065f,.018f),.1f,Quaternion.Euler(0,0,-45));
        var rb=root.AddComponent<Rigidbody>();rb.isKinematic=true;rb.useGravity=false;root.layer=Layers.Edible;
        SetRef(root.AddComponent<EdibleObject>(),"data",data);root.AddComponent<BoxCollider>();
        data.prefab=PrefabUtility.SaveAsPrefabAsset(root,PrefabDir+"/Edible_Bomb.prefab");UnityEngine.Object.DestroyImmediate(root);
        BakeSmooth(data,.42f);data.icon=RenderOwnIcon(data.prefab,"Bomb");EditorUtility.SetDirty(data);return data;
    }
    private static void AddBombs(LevelData level,List<SpawnPlacement> placements,EdibleData bomb){
        int wanted=level.levelId==1?2:level.levelId<11?3:4,added=0;
        var candidates=new List<Vector2>();
        // Visible approach hazards first, then optional perimeter hazards. The spawn plaza stays clear.
        candidates.Add(level.holeStart+new Vector2(-3,1.4f));candidates.Add(level.holeStart+new Vector2(3,1.4f));
        for(float z=-level.Size.y*.36f;z<level.Size.y*.4f;z+=2.6f){candidates.Add(new Vector2(-level.Size.x*.42f,z));candidates.Add(new Vector2(level.Size.x*.42f,z));}
        foreach(var pos in candidates){
            if(added>=wanted)break;
            if(Mathf.Abs(pos.x)+bomb.footprintRadius>level.Size.x/2||Mathf.Abs(pos.y)+bomb.footprintRadius>level.Size.y/2||(pos-level.holeStart).magnitude<level.startClearRadius+bomb.footprintRadius+.2f)continue;
            bool clear=true;foreach(var p in placements)if((p.position-pos).magnitude<p.edible.footprintRadius+bomb.footprintRadius+.65f){clear=false;break;}
            if(!clear)continue;
            placements.Add(new SpawnPlacement{edible=bomb,position=pos,yaw=0});added++;
        }
        if(added<2)throw new Exception("Not enough safe bomb positions in level "+level.levelId);
    }
    private static void ApplySoftLook(){
        RenderSettings.ambientMode=AmbientMode.Trilight;
        RenderSettings.ambientSkyColor=new Color(.73f,.75f,.79f);RenderSettings.ambientEquatorColor=new Color(.51f,.52f,.56f);RenderSettings.ambientGroundColor=new Color(.32f,.29f,.33f);
        RenderSettings.ambientIntensity=.8f;RenderSettings.reflectionIntensity=.35f;
        var sun=UnityEngine.Object.FindFirstObjectByType<Light>();sun.intensity=.84f;sun.color=new Color(1f,.97f,.94f);sun.shadows=LightShadows.Soft;sun.shadowStrength=.28f;sun.shadowBias=.025f;sun.shadowNormalBias=.12f;sun.transform.rotation=Quaternion.Euler(48,-32,0);
        QualitySettings.shadows=ShadowQuality.All;QualitySettings.shadowResolution=ShadowResolution.High;QualitySettings.shadowDistance=55;QualitySettings.shadowCascades=2;QualitySettings.antiAliasing=4;
        foreach(string guid in AssetDatabase.FindAssets("t:ThemeData")){
            var theme=AssetDatabase.LoadAssetAtPath<ThemeData>(AssetDatabase.GUIDToAssetPath(guid));
            Color floor=new Color(.74f,.71f,.80f);
            if(theme.themeId=="Dessert")floor=new Color(.84f,.71f,.76f);
            else if(theme.themeId=="Candy")floor=new Color(.69f,.80f,.81f);
            else if(theme.themeId.Contains("Park"))floor=new Color(.72f,.80f,.69f);
            theme.floorA=floor;theme.floorB=floor*.975f;theme.floorB.a=1;theme.background=Color.Lerp(floor,Color.white,.2f);theme.lightColor=new Color(1f,.97f,.94f);theme.rimColor=new Color(.66f,.63f,.68f);EditorUtility.SetDirty(theme);
        }
        var rim=AssetDatabase.LoadAssetAtPath<Material>(MatDir+"/ReleaseRim.mat");rim.color=new Color(.63f,.60f,.65f);rim.SetFloat("_Glossiness",.28f);EditorUtility.SetDirty(rim);
        var canvas=UnityEngine.Object.FindFirstObjectByType<Canvas>();var hint=canvas.transform.Find("HUD/PlayHint");
        if(hint!=null){hint.GetComponent<HoleItText>().text="물건을 모아 주세요 · 폭탄을 삼키면 실패!";hint.GetComponent<HoleItText>().color=new Color(.25f,.23f,.3f);}
        var badge=canvas.transform.Find("HUD/LevelBadge");if(badge!=null)badge.GetComponent<UnityEngine.UI.Image>().color=new Color(.65f,.38f,.49f,.95f);
        var size=canvas.transform.Find("HUD/SizeRoot/SizeText");if(size!=null)size.GetComponent<HoleItText>().color=new Color(.26f,.24f,.30f);
        PlayerSettings.bundleVersion="2.1.0";PlayerSettings.Android.bundleVersionCode=3;
    }
}
