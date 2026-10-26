using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static partial class HoleItBuilder
{
    private static Material smoothSurface;
    [MenuItem("Pocket Pit/Build smooth tower edition")]
    public static void BuildSmoothEdition(){
        EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");modelMeshCache.Clear();
        var mats=new Dictionary<string,Material>();
        smoothSurface=SaveAsset(new Material(Shader.Find("PocketPit/SmoothToy")){name="SmoothSurface"},MatDir+"/SmoothSurface.mat");
        // Replace the earlier offline low-poly meshes with the detailed, smooth model kit.
        foreach(var spec in AllSpecs()){
            var data=AssetDatabase.LoadAssetAtPath<EdibleData>(EdibleDir+"/Edible_"+spec.key+".asset");
            float footprint=data.footprintRadius;data.prefab=BuildDetailedEdiblePrefab(spec,data,mats);
            BakeSmooth(data,footprint);data.icon=RenderOwnIcon(data.prefab,spec.key);EditorUtility.SetDirty(data);
        }
        string[] keys={"Lipstick","Perfume","NailPolish","CreamJar","Compact","Lotion","Soap","MakeupBrush","Serum","Macaron","JuiceBottle","Chocolate"};
        string[] labels={"로즈 립스틱","복숭아 향수","민트 매니큐어","라벤더 크림","코랄 팩트","하늘빛 로션","딸기 비누","메이크업 브러시","골드 세럼","민트 마카롱","과일 주스","초콜릿"};
        for(int i=0;i<keys.Length;i++)BuildBeauty(keys[i],labels[i],mats);
        // Existing authored maps are retained for later levels; each gains real vertical stacks.
        foreach(string guid in AssetDatabase.FindAssets("t:LevelData")){
            var l=AssetDatabase.LoadAssetAtPath<LevelData>(AssetDatabase.GUIDToAssetPath(guid));
            if(l.schemaVersion>=10)continue;
            if(l.levelId<=8||l.levelId==14||l.levelId==21||l.levelId==29)BeautyLevel(l);
            else StackExisting(l);
            l.schemaVersion=10;EditorUtility.SetDirty(l);
        }
        // Refresh icons and labels after replacing model assets.
        foreach(string guid in AssetDatabase.FindAssets("t:LevelData")){
            var l=AssetDatabase.LoadAssetAtPath<LevelData>(AssetDatabase.GUIDToAssetPath(guid));
            for(int i=0;i<l.missions.Length;i++){var m=l.missions[i];if(m.target!=null){m.icon=m.target.icon;m.description=m.target.displayName;}l.missions[i]=m;}
            EditorUtility.SetDirty(l);
        }
        AssetDatabase.SaveAssets();SmoothScene();AssetDatabase.SaveAssets();
        HoleItProjectValidation.ValidateOrThrow();Debug.Log("SMOOTH TOWER EDITION READY");
    }
    private static void BakeSmooth(EdibleData data,float radius){
        string path=AssetDatabase.GetAssetPath(data.prefab);var root=PrefabUtility.LoadPrefabContents(path);
        try{
            var verts=new List<Vector3>();var normals=new List<Vector3>();var colors=new List<Color>();var triangles=new List<int>();var surfaces=new List<Vector2>();
            foreach(var f in root.GetComponentsInChildren<MeshFilter>()){
                var mesh=f.sharedMesh;var mat=f.GetComponent<Renderer>().sharedMaterial;var matrix=root.transform.worldToLocalMatrix*f.transform.localToWorldMatrix;
                var v=mesh.vertices;var n=mesh.normals;int start=verts.Count;Color c=mat.HasProperty("_Color")?mat.color:Color.white;
                var finish=mat.name.Contains("Gold")?new Vector2(.55f,.36f):mat.name.Contains("Ivory")?new Vector2(0,.22f):new Vector2(0,.3f);
                for(int i=0;i<v.Length;i++){verts.Add(matrix.MultiplyPoint3x4(v[i]));normals.Add(matrix.inverse.transpose.MultiplyVector(n[i]).normalized);colors.Add(c);surfaces.Add(finish);}
                foreach(int t in mesh.triangles)triangles.Add(start+t);
            }
            var bounds=new Bounds(verts[0],Vector3.zero);foreach(var v in verts)bounds.Encapsulate(v);
            float scale=radius/new Vector2(bounds.extents.x,bounds.extents.z).magnitude;
            for(int i=0;i<verts.Count;i++)verts[i]=new Vector3(verts[i].x,(verts[i].y-bounds.min.y),verts[i].z)*scale;
            var combined=new Mesh{name="Smooth_"+data.objectId,indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};
            combined.SetVertices(verts);combined.SetNormals(normals);combined.SetColors(colors);combined.SetUVs(1,surfaces);combined.SetTriangles(triangles,0);combined.RecalculateBounds();
            var saved=SaveAsset(combined,MeshDir+"/SmoothComplete_"+data.objectId+".asset");
            for(int i=root.transform.childCount-1;i>=0;i--)UnityEngine.Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
            foreach(var renderer in root.GetComponents<Renderer>())UnityEngine.Object.DestroyImmediate(renderer);
            foreach(var filter in root.GetComponents<MeshFilter>())UnityEngine.Object.DestroyImmediate(filter);
            var visual=new GameObject("Smooth model",typeof(MeshFilter),typeof(MeshRenderer));visual.transform.SetParent(root.transform,false);
            visual.GetComponent<MeshFilter>().sharedMesh=saved;visual.GetComponent<MeshRenderer>().sharedMaterial=smoothSurface;
            foreach(var c in root.GetComponents<Collider>())UnityEngine.Object.DestroyImmediate(c);
            var box=root.AddComponent<BoxCollider>();box.center=saved.bounds.center;box.size=saved.bounds.size;
            data.stackHeight=saved.bounds.size.y+.012f;data.footprintRadius=radius;
            PrefabUtility.SaveAsPrefabAsset(root,path);
        }finally{PrefabUtility.UnloadPrefabContents(root);}
    }
    private static void BuildBeauty(string key,string label,Dictionary<string,Material> mats){
        string path=EdibleDir+"/Edible_"+key+".asset";var data=AssetDatabase.LoadAssetAtPath<EdibleData>(path);
        if(data==null){data=ScriptableObject.CreateInstance<EdibleData>();data.objectId=key;data=SaveAsset(data,path);}
        data.displayName=label;data.objectType=ObjectType.Food;data.sizeTier=1;data.score=12;data.growthValue=.32f;data.canBeSwallowed=true;
        var root=new GameObject("Edible_"+key);var t=root.transform;
        var pink=ColorMat(mats,"BeautyRose",new Color(.76f,.31f,.42f));var mint=ColorMat(mats,"BeautyMint",new Color(.41f,.65f,.57f));
        var lavender=ColorMat(mats,"BeautyLavender",new Color(.57f,.48f,.68f));var peach=ColorMat(mats,"BeautyPeach",new Color(.83f,.65f,.52f));
        var gold=ColorMat(mats,"BeautyGold",new Color(.72f,.55f,.32f));var white=ColorMat(mats,"BeautyIvory",new Color(.87f,.83f,.76f));
        var dark=ColorMat(mats,"BeautyCharcoal",new Color(.16f,.17f,.20f));var blue=ColorMat(mats,"BeautyBlue",new Color(.39f,.60f,.72f));
        switch(key){
            case "Lipstick":
                BeautyCylinder(t,"Case",dark,new Vector3(0,.29f,0),new Vector3(.48f,.58f,.48f));
                BeautyCylinder(t,"Gold collar",gold,new Vector3(0,.59f,0),new Vector3(.50f,.09f,.50f));
                BeautyCylinder(t,"Gold sleeve",gold,new Vector3(0,.75f,0),new Vector3(.34f,.28f,.34f));
                AddModelPart(t,"Angled lipstick bullet",ModelMesh("BeautyBullet_v3",()=>SoftLathe(.5f,.5f,true)),pink,new Vector3(0,1.015f,0),new Vector3(.30f,.53f,.30f),Quaternion.identity);break;
            case "Perfume":
                PartBox(t,"Rounded glass",peach,new Vector3(0,.38f,0),new Vector3(.83f,.76f,.55f),.2f);
                PartBox(t,"Front label",white,new Vector3(0,.38f,-.279f),new Vector3(.43f,.30f,.015f),.15f);
                PartBox(t,"Gold stripe",gold,new Vector3(0,.37f,-.291f),new Vector3(.24f,.045f,.012f),.15f);
                BeautyCylinder(t,"Atomizer",gold,new Vector3(0,.84f,0),new Vector3(.25f,.16f,.25f));
                PartBox(t,"Crystal cap",dark,new Vector3(0,.99f,0),new Vector3(.36f,.23f,.31f),.18f);break;
            case "NailPolish":
                PartBox(t,"Polish bottle",mint,new Vector3(0,.25f,0),new Vector3(.59f,.5f,.59f),.2f);
                BeautyCylinder(t,"Long cap",dark,new Vector3(0,.78f,0),new Vector3(.29f,.56f,.29f),.5f,.42f);
                PartBox(t,"Gloss highlight",white,new Vector3(-.13f,.28f,-.3f),new Vector3(.04f,.27f,.01f),.2f);break;
            case "CreamJar":
                BeautyCylinder(t,"Cream jar",lavender,new Vector3(0,.22f,0),new Vector3(.86f,.44f,.86f),.46f,.5f);
                BeautyCylinder(t,"Gold seam",gold,new Vector3(0,.43f,0),new Vector3(.88f,.06f,.88f));
                BeautyCylinder(t,"Pearl lid",white,new Vector3(0,.51f,0),new Vector3(.9f,.12f,.9f));
                BeautyCylinder(t,"Lid emboss",lavender,new Vector3(0,.577f,0),new Vector3(.24f,.02f,.24f));break;
            case "Compact":
                BeautyCylinder(t,"Blush compact",pink,new Vector3(0,.13f,0),new Vector3(.95f,.26f,.95f));
                BeautyCylinder(t,"Gold trim",gold,new Vector3(0,.15f,0),new Vector3(.96f,.035f,.96f));
                BeautyCylinder(t,"Soft lid",peach,new Vector3(0,.275f,0),new Vector3(.86f,.04f,.86f));
                PartTorus(t,"Lid emblem",gold,new Vector3(0,.304f,0),Vector3.one*.3f);break;
            case "Lotion":
                PartBox(t,"Lotion bottle",blue,new Vector3(0,.51f,0),new Vector3(.58f,1.02f,.39f),.22f);
                PartBox(t,"Ivory label",white,new Vector3(0,.48f,-.2f),new Vector3(.32f,.42f,.015f),.14f);
                BeautyCylinder(t,"Pump neck",gold,new Vector3(0,1.06f,0),new Vector3(.20f,.12f,.20f));
                PartBox(t,"Pump head",white,new Vector3(.08f,1.16f,0),new Vector3(.38f,.09f,.17f),.2f);break;
            case "Soap":
                PartBox(t,"Smooth soap",pink,new Vector3(0,.19f,0),new Vector3(.87f,.38f,.61f),.22f);
                PartSphere(t,"Molded oval",peach,new Vector3(0,.379f,0),new Vector3(.40f,.025f,.22f));break;
            case "MakeupBrush":
                BeautyCylinder(t,"Handle",lavender,new Vector3(0,.38f,0),new Vector3(.17f,.76f,.17f),.32f,.5f);
                BeautyCylinder(t,"Ferrule",gold,new Vector3(0,.84f,0),new Vector3(.25f,.2f,.25f));
                PartSphere(t,"Velvet bristles",peach,new Vector3(0,1.08f,0),new Vector3(.47f,.48f,.40f));break;
            case "Serum":
                BeautyCylinder(t,"Serum bottle",gold,new Vector3(0,.37f,0),new Vector3(.50f,.74f,.50f),.48f,.44f);
                PartBox(t,"Paper label",white,new Vector3(0,.36f,-.246f),new Vector3(.26f,.29f,.016f),.13f);
                BeautyCylinder(t,"Dropper ring",dark,new Vector3(0,.78f,0),new Vector3(.34f,.12f,.34f));
                PartSphere(t,"Dropper",white,new Vector3(0,.96f,0),new Vector3(.21f,.29f,.21f));break;
            case "Macaron":
                PartSphere(t,"Lower shell",mint,new Vector3(0,.17f,0),new Vector3(.88f,.29f,.88f));
                BeautyCylinder(t,"Vanilla ganache",white,new Vector3(0,.30f,0),new Vector3(.83f,.13f,.83f));
                PartSphere(t,"Upper shell",mint,new Vector3(0,.43f,0),new Vector3(.88f,.29f,.88f));break;
            case "JuiceBottle":
                BeautyCylinder(t,"Bottle",mint,new Vector3(0,.38f,0),new Vector3(.51f,.76f,.51f),.47f,.44f);
                PartSphere(t,"Shoulder",mint,new Vector3(0,.75f,0),new Vector3(.45f,.25f,.45f));
                BeautyCylinder(t,"Cap",gold,new Vector3(0,.94f,0),new Vector3(.27f,.16f,.27f));
                BeautyCylinder(t,"Fruit label",peach,new Vector3(0,.43f,0),new Vector3(.52f,.34f,.52f));break;
            default:
                var cocoa=ColorMat(mats,"ChocolateRich",new Color(.32f,.10f,.045f));
                PartBox(t,"Chocolate base",cocoa,new Vector3(0,.07f,0),new Vector3(.65f,.14f,1.05f),.15f);
                for(int x=0;x<2;x++)for(int z=0;z<3;z++)PartBox(t,"Chocolate square",cocoa,new Vector3((x-.5f)*.30f,.17f,(z-1)*.32f),new Vector3(.27f,.13f,.28f),.15f);
                PartBox(t,"Gold wrapper",gold,new Vector3(0,.155f,.37f),new Vector3(.67f,.32f,.36f),.08f);break;
        }
        var rb=root.AddComponent<Rigidbody>();rb.isKinematic=true;rb.useGravity=false;
        var edible=root.AddComponent<EdibleObject>();SetRef(edible,"data",data);root.AddComponent<BoxCollider>();root.layer=Layers.Edible;
        data.prefab=PrefabUtility.SaveAsPrefabAsset(root,PrefabDir+"/Edible_"+key+".prefab");UnityEngine.Object.DestroyImmediate(root);
        BakeSmooth(data,key=="MakeupBrush"?.33f:key=="Lipstick"?.38f:.57f);
        data.icon=RenderOwnIcon(data.prefab,key);EditorUtility.SetDirty(data);
    }
    private static EdibleData Edible(string key){return AssetDatabase.LoadAssetAtPath<EdibleData>(EdibleDir+"/Edible_"+key+".asset");}
    private static void BeautyLevel(LevelData l){
        string[][] collections={new[]{"Lipstick","CreamJar","Perfume","Compact"},new[]{"DonutS","Macaron","CookieS","Chocolate"},new[]{"NailPolish","Soap","Lotion","Serum"},new[]{"BurgerS","SodaS","FriesS","JuiceBottle"},new[]{"Compact","MakeupBrush","Perfume","Lipstick"},new[]{"Macaron","CakeS","DonutS","CupcakeS"},new[]{"Serum","Lotion","NailPolish","CreamJar"},new[]{"ToyCar","BlockS","DuckS","BallS"}};
        string[] titles={"로즈 뷰티 타워","달콤한 디저트 탑","민트 드레싱룸","맛있는 피크닉","복숭아 메이크업","마카롱 파티","향기로운 선물","장난감 캐슬"};int index=(l.levelId-1)%8;
        var palette=collections[index];var placements=new List<SpawnPlacement>();var counts=new Dictionary<EdibleData,int>();
        float spacing=1.75f;foreach(string key in palette)spacing=Mathf.Max(spacing,Edible(key).footprintRadius*2+.12f);
        for(int z=0;z<6;z++)for(int x=0;x<7;x++){
            int col=z*7+x;var d=Edible(palette[(x/2+z/2)%palette.Length]);float height=d.stackHeight;
            int layers=Mathf.Clamp(Mathf.FloorToInt((3.7f+(col%3)*.6f)/height),3,10);
            for(int layer=0;layer<layers;layer++)placements.Add(new SpawnPlacement{edible=d,position=new Vector2((x-3)*spacing,(z-2.5f)*spacing),height=layer*height,stacked=true,yaw=0});
            int before;counts.TryGetValue(d,out before);counts[d]=before+layers;
        }
        l.arenaSize=new Vector2(Mathf.Max(16,spacing*6+4),Mathf.Max(20,spacing*5+9));l.gridSize=new Vector2Int(16,20);l.mapSize=20;l.holeStart=new Vector2(0,-spacing*2.5f-3.3f);l.startClearRadius=2;
        l.levelTitle=l.levelId.ToString("00")+"  "+titles[index];l.timeLimit=l.levelId==1?0:150;l.cameraZoomMultiplier=1.05f;l.growthMultiplier=.38f;
        l.defaultWrongBehavior=WrongTargetBehavior.None;l.placements=placements.ToArray();l.spawnGroups=new SpawnGroup[counts.Count];int g=0;
        foreach(var pair in counts)l.spawnGroups[g++]=new SpawnGroup{edible=pair.Key,count=pair.Value};
        l.missions=new MissionData[3];for(int i=0;i<3;i++){var d=Edible(palette[i]);l.missions[i]=new MissionData{missionId="L"+l.levelId+"_M"+i,kind=MissionKind.ExactObject,target=d,icon=d.icon,description=d.displayName,requiredCount=Mathf.CeilToInt(counts[d]*.72f)};}
        l.theme=AssetDatabase.LoadAssetAtPath<ThemeData>(DataDir+"/Themes/"+(index%2==0?"Dessert":"Candy")+".asset");
    }
    private static void StackExisting(LevelData l){
        var p=new List<SpawnPlacement>();var counts=new Dictionary<EdibleData,int>();
        for(int i=0;i<l.placements.Length;i++){
            var original=l.placements[i];int layers=(i%3==0&&original.edible.sizeTier<=3&&!original.decorative)?Mathf.Clamp(Mathf.FloorToInt(4/original.edible.stackHeight),2,7):1;
            for(int h=0;h<layers;h++){var item=original;item.height=h*item.edible.stackHeight;item.stacked=layers>1;p.Add(item);}
            int count;counts.TryGetValue(original.edible,out count);counts[original.edible]=count+layers;
        }
        l.placements=p.ToArray();l.spawnGroups=new SpawnGroup[counts.Count];int n=0;foreach(var item in counts)l.spawnGroups[n++]=new SpawnGroup{edible=item.Key,count=item.Value};
        for(int m=0;m<l.missions.Length;m++)if(l.missions[m].target!=null){var mission=l.missions[m];mission.requiredCount=Math.Min(counts[mission.target],mission.requiredCount*2);l.missions[m]=mission;}
        if(l.timeLimit>0)l.timeLimit*=1.75f;
    }
    [MenuItem("Pocket Pit/Finalize smooth scene")]
    public static void FinalizeSmooth(){EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");SmoothScene();AssetDatabase.SaveAssets();HoleItProjectValidation.ValidateOrThrow();}
    public static void RefreshTowerLayouts(){foreach(string guid in AssetDatabase.FindAssets("t:LevelData")){var l=AssetDatabase.LoadAssetAtPath<LevelData>(AssetDatabase.GUIDToAssetPath(guid));if(l.levelId<=8||l.levelId==14||l.levelId==21||l.levelId==29){BeautyLevel(l);l.schemaVersion=11;EditorUtility.SetDirty(l);}}FinalizeSmooth();}
    private static void SmoothScene(){
        var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        RenderSettings.ambientLight=new Color(.52f,.53f,.60f);var sun=UnityEngine.Object.FindFirstObjectByType<Light>();sun.intensity=.95f;sun.shadowStrength=.3f;sun.shadows=LightShadows.Soft;
        sun.transform.rotation=Quaternion.Euler(55,-25,0);
        var settings=AssetDatabase.LoadAssetAtPath<GameSettings>(ResDir+"/GameSettings.asset");settings.swallowStagger=.035f;settings.attractSpeed=8;settings.swallowDuration=.38f;settings.attractSpin=0;settings.cameraPitch=57;
        EditorUtility.SetDirty(settings);
        var rim=GameObject.Find("PlayerHole/Visual/Rim");if(rim!=null){var flat=rim.transform.Find("Quad");if(flat!=null)flat.gameObject.SetActive(false);}
        var rimMaterial=AssetDatabase.LoadAssetAtPath<Material>(MatDir+"/ReleaseRim.mat");rimMaterial.color=new Color(.94f,.84f,.95f);rimMaterial.SetFloat("_Glossiness",.25f);EditorUtility.SetDirty(rimMaterial);
        var pit=AssetDatabase.LoadAssetAtPath<Material>(MatDir+"/HolePit.mat");pit.SetColor("_TopColor",new Color(.14f,.12f,.17f));pit.SetColor("_DeepColor",new Color(.075f,.06f,.095f));EditorUtility.SetDirty(pit);
        foreach(string guid in AssetDatabase.FindAssets("t:ThemeData")){
            var theme=AssetDatabase.LoadAssetAtPath<ThemeData>(AssetDatabase.GUIDToAssetPath(guid));
            if(theme.themeId=="Dessert"){theme.floorA=new Color(.98f,.48f,.70f);theme.floorB=new Color(.95f,.41f,.65f);theme.background=new Color(.97f,.57f,.75f);}
            if(theme.themeId=="Candy"){theme.floorA=new Color(.54f,.77f,.94f);theme.floorB=new Color(.45f,.71f,.91f);theme.background=new Color(.61f,.8f,.95f);}
            EditorUtility.SetDirty(theme);
        }
        // Invisible collision boundaries keep descending stacks on the board.
        for(int i=0;i<4;i++){var rail=GameObject.Find("Boundary"+i);if(rail==null)continue;var box=rail.GetComponent<BoxCollider>();if(box==null)box=rail.AddComponent<BoxCollider>();box.center=new Vector3(0,20,0);box.size=new Vector3(1,42,1);}
        var canvas=UnityEngine.Object.FindFirstObjectByType<Canvas>();var title=canvas.transform.Find("TitlePanel");
        foreach(string name in new[]{"TitleCard","BestScorePill","Footer","HoleEmblem_Ring","HoleEmblem_Core","HoleEmblem_Glint"}){var tr=title.Find(name);if(tr!=null)tr.gameObject.SetActive(false);}
        foreach(Transform t in title)if(t.name.StartsWith("DecorDot"))t.gameObject.SetActive(false);
        title.GetComponent<UnityEngine.UI.Image>().color=new Color(.1f,.02f,.15f,.06f);
        MoveUI(title.Find("Logo"),.91f);MoveUI(title.Find("Tagline"),.85f);MoveUI(title.Find("LevelSelectPill"),.32f);MoveUI(title.Find("LevelText"),.32f);
        MoveUI(title.Find("PrevLevel"),.32f);MoveUI(title.Find("NextLevel"),.32f);MoveUI(title.Find("PlayButton"),.21f);MoveUI(title.Find("Best"),.40f);
        MoveUI(title.Find("FamilyMode"),.115f);MoveUI(title.Find("Settings"),.055f);MoveUI(title.Find("Levels"),.055f);
        title.Find("Tagline").GetComponent<HoleItText>().text="쏙쏙 수집 놀이";
        var hint=canvas.transform.Find("HUD/PlayHint");MoveUI(hint,.23f);
        PlayerSettings.bundleVersion="2.0.0";PlayerSettings.Android.bundleVersionCode=2;
        // The supplied GraphicsSettings only listed custom shaders. Retain default UI shaders in players.
        var graphics=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
        var shaders=graphics.FindProperty("m_AlwaysIncludedShaders");
        foreach(string shaderName in new[]{"UI/Default","UI/Default Font","Sprites/Default","PocketPit/SmoothToy"}){
            var shader=Shader.Find(shaderName);if(shader==null)throw new Exception("Required shader missing: "+shaderName);
            bool found=false;for(int i=0;i<shaders.arraySize;i++)if(shaders.GetArrayElementAtIndex(i).objectReferenceValue==shader)found=true;
            if(!found){int i=shaders.arraySize;shaders.InsertArrayElementAtIndex(i);shaders.GetArrayElementAtIndex(i).objectReferenceValue=shader;}
        }
        graphics.ApplyModifiedPropertiesWithoutUndo();
        ApplySoftLook();
        EditorSceneManager.SaveScene(scene);
    }
    private static void MoveUI(Transform t,float y){if(t==null)return;var r=(RectTransform)t;var a=r.anchorMin;a.y=y;r.anchorMin=r.anchorMax=a;var p=r.anchoredPosition;p.y=0;r.anchoredPosition=p;}
}

