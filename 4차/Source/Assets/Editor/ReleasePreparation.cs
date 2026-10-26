using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
public static class ReleasePreparation
{
    [MenuItem("Pocket Pit/Prepare release")]
    public static void Prepare(){
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");
        if(GameObject.Find("Interface")!=null){if(!HoleItBuilder.BuildAll(false))throw new Exception("Scene upgrade failed");scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();}
        var font=Resources.Load<Font>("Fonts/NanumGothic-Bold");if(font==null)throw new Exception("Bundled font missing");
        PlayerSettings.companyName="Pocket Playground";PlayerSettings.productName="포켓 홀";
        PlayerSettings.bundleVersion="1.0.0";PlayerSettings.Android.bundleVersionCode=1;
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android,"com.pocketplayground.pocketpit");
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android,ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64|AndroidArchitecture.ARMv7;
        PlayerSettings.Android.minSdkVersion=AndroidSdkVersions.AndroidApiLevel26;
        PlayerSettings.Android.targetSdkVersion=AndroidSdkVersions.AndroidApiLevelAuto;
        PlayerSettings.defaultInterfaceOrientation=UIOrientation.Portrait;
        PlayerSettings.defaultScreenWidth=540;PlayerSettings.defaultScreenHeight=960;
        PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.runInBackground=true;
        PlayerSettings.SplashScreen.show=false;PlayerSettings.stripEngineCode=true;
        PlayerSettings.Android.forceInternetPermission=false;
        QualitySettings.antiAliasing=4;QualitySettings.shadows=ShadowQuality.All;QualitySettings.shadowDistance=45;
        RenderSettings.ambientLight=new Color(.34f,.32f,.40f);
        var sunlight=UnityEngine.Object.FindFirstObjectByType<Light>();sunlight.intensity=.72f;sunlight.shadows=LightShadows.Soft;sunlight.shadowStrength=.36f;
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/Main.unity",true)};
        foreach(var root in scene.GetRootGameObjects())foreach(var t in root.GetComponentsInChildren<HoleItText>(true)){
            t.font=font;t.fontStyle=FontStyle.Normal;t.text=Korean.Text(t.text);
            t.resizeTextForBestFit=true;t.resizeTextMinSize=Mathf.Max(18,t.fontSize/2);t.resizeTextMaxSize=t.fontSize;
            EditorUtility.SetDirty(t);
        }
        foreach(var guid in AssetDatabase.FindAssets("t:LevelData")){
            var level=AssetDatabase.LoadAssetAtPath<LevelData>(AssetDatabase.GUIDToAssetPath(guid));
            level.levelTitle=Korean.Text(level.levelTitle);
            for(int i=0;i<level.missions.Length;i++)level.missions[i].description=Korean.Text(level.missions[i].description);
            EditorUtility.SetDirty(level);
        }
        foreach(var guid in AssetDatabase.FindAssets("t:EdibleData")){
            var data=AssetDatabase.LoadAssetAtPath<EdibleData>(AssetDatabase.GUIDToAssetPath(guid));data.displayName=Korean.Text(data.displayName);EditorUtility.SetDirty(data);
        }
        var canvas=UnityEngine.Object.FindFirstObjectByType<Canvas>();
        var title=canvas.transform.Find("TitlePanel");
        var safe=title.GetComponent<SafeAreaFitter>();if(safe==null)title.gameObject.AddComponent<SafeAreaFitter>();
        foreach(string name in new[]{"ClearPanel","FailPanel","PausePanel","SettingsPanel","LevelSelect"}){
            var p=canvas.transform.Find(name);if(p!=null&&p.GetComponent<SafeAreaFitter>()==null)p.gameObject.AddComponent<SafeAreaFitter>();
        }
        // Keep the pause control entirely inside the left safe boundary.
        SetRect(canvas.transform.Find("HUD/PauseButton"),new Vector2(0,1),new Vector2(.5f,.5f),new Vector2(82,-78),new Vector2(100,100));
        SetRect(canvas.transform.Find("HUD/LevelBadge"),new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(0,-121),new Vector2(590,66));
        SetRect(canvas.transform.Find("HUD/LevelText"),new Vector2(.5f,1),new Vector2(.5f,1),new Vector2(0,-121),new Vector2(570,66));
        var levelText=canvas.transform.Find("HUD/LevelText").GetComponent<HoleItText>();levelText.resizeTextMaxSize=35;
        SetRect(canvas.transform.Find("HUD/SizeGauge"),new Vector2(.5f,.075f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(500,86));
        // The mission card only takes the height needed by the active mission rows.
        var mission=canvas.transform.Find("HUD/MissionPanel");
        if(mission.GetComponent<MissionCardLayout>()==null)mission.gameObject.AddComponent<MissionCardLayout>();
        SetRect(canvas.transform.Find("ClearPanel/Breakdown"),new Vector2(.5f,.52f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(720,230));
        SetRect(canvas.transform.Find("ClearPanel/Missions"),new Vector2(.5f,.395f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(720,140));
        foreach(var t in canvas.GetComponentsInChildren<HoleItText>(true))if(t.name=="Logo"){t.fontSize=112;t.resizeTextMaxSize=112;}
        if(canvas.GetComponent<FamilyMode>()==null){
            var family=canvas.gameObject.AddComponent<FamilyMode>();
            var sample=title.Find("Settings");
            var go=UnityEngine.Object.Instantiate(sample.gameObject,title);go.name="FamilyMode";
            SetRect(go.transform,new Vector2(.5f,.155f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(850,72));
            family.modeButton=go.GetComponent<Button>();family.modeLabel=go.GetComponentInChildren<HoleItText>();
            family.modeLabel.text="도전 모드 · 시간 안에 모으기";family.modeLabel.fontSize=27;family.modeLabel.resizeTextMaxSize=27;
            var hint=new GameObject("PlayHint",typeof(RectTransform),typeof(HoleItText));hint.transform.SetParent(canvas.transform.Find("HUD"),false);
            SetRect(hint.transform,new Vector2(.5f,.14f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(980,76));
            family.hint=hint.GetComponent<HoleItText>();family.hint.font=font;family.hint.fontSize=28;family.hint.alignment=TextAnchor.MiddleCenter;
            family.hint.color=new Color(.16f,.12f,.29f);family.hint.raycastTarget=false;
        }
        // A raised smooth rim gives the hole a tangible silhouette.
        var rim=GameObject.Find("PlayerHole/Visual/Rim");
        if(rim!=null&&rim.transform.Find("SculptedRim")==null){
            var mesh=MakeRim();AssetDatabase.CreateAsset(mesh,"Assets/HoleIt/Meshes/ReleaseRim.asset");
            var mat=new Material(Shader.Find("Standard"));mat.color=new Color(.95f,.87f,1f);mat.SetFloat("_Glossiness",.45f);
            AssetDatabase.CreateAsset(mat,"Assets/HoleIt/Materials/ReleaseRim.mat");
            var g=new GameObject("SculptedRim",typeof(MeshFilter),typeof(MeshRenderer));g.transform.SetParent(rim.transform,false);
            g.GetComponent<MeshFilter>().sharedMesh=mesh;g.GetComponent<MeshRenderer>().sharedMaterial=mat;
        }
        foreach(var camera in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))camera.allowMSAA=true;
        CreateIcon();AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);
        HoleItProjectValidation.ValidateOrThrow();Debug.Log("RELEASE PREPARATION PASS");
    }
    private static void SetRect(Transform t,Vector2 anchor,Vector2 pivot,Vector2 pos,Vector2 size){if(t==null)return;var r=t as RectTransform;r.anchorMin=r.anchorMax=anchor;r.pivot=pivot;r.anchoredPosition=pos;r.sizeDelta=size;}
    private static void CreateIcon(){
        int size=512;var icon=new Texture2D(size,size,TextureFormat.RGBA32,false);
        for(int y=0;y<size;y++)for(int x=0;x<size;x++){
            float u=x/(float)size,v=y/(float)size;
            Color c=Color.Lerp(new Color(.44f,.22f,.87f),new Color(.95f,.40f,.69f),v);
            c*=((x/64+y/64)%2==0)?1f:.96f;c.a=1;
            float oval=Mathf.Sqrt(Mathf.Pow((u-.5f)/.37f,2)+Mathf.Pow((v-.40f)/.255f,2));
            if(oval<1.1f)c=new Color(.38f,.16f,.55f);
            if(oval<1)c=Color.Lerp(new Color(.62f,.53f,.95f),new Color(.99f,.92f,1),v);
            if(oval<.79f)c=Color.Lerp(new Color(.015f,.01f,.035f),new Color(.13f,.08f,.22f),v);
            float donut=Mathf.Sqrt(Mathf.Pow(u-.56f,2)+Mathf.Pow((v-.72f)*1.13f,2));
            if(donut<.156f&&donut>.056f)c=new Color(.92f,.58f,.21f);
            if(donut<.142f&&donut>.068f)c=new Color(1f,.74f,.26f);
            if(donut<.13f&&donut>.078f&&((x/10+y/12)%7==0))c=new Color(1,.96f,.76f);
            float star=Mathf.Abs(u-.22f)+Mathf.Abs(v-.71f)*.6f;if(star<.032f)c=Color.white;
            float star2=Mathf.Abs(u-.82f)*.7f+Mathf.Abs(v-.57f);if(star2<.024f)c=new Color(1,.91f,.44f);
            icon.SetPixel(x,y,c);
        }
        icon.Apply();const string path="Assets/HoleIt/Textures/AppIcon.png";File.WriteAllBytes(path,icon.EncodeToPNG());UnityEngine.Object.DestroyImmediate(icon);
        AssetDatabase.ImportAsset(path);var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Default;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
        var image=AssetDatabase.LoadAssetAtPath<Texture2D>(path);PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown,new[]{image});
    }
    private static Mesh MakeRim(){
        const int n=80,m=12;var vertices=new Vector3[(n+1)*(m+1)];var triangles=new int[n*m*6];int index=0;
        for(int i=0;i<=n;i++)for(int j=0;j<=m;j++){
            float a=i*Mathf.PI*2/n,b=j*Mathf.PI*2/m,r=.463f+.031f*Mathf.Cos(b);
            vertices[i*(m+1)+j]=new Vector3(r*Mathf.Cos(a),.035f+.105f*Mathf.Sin(b),r*Mathf.Sin(a));
            if(i<n&&j<m){int k=i*(m+1)+j;triangles[index++]=k;triangles[index++]=k+1;triangles[index++]=k+m+1;triangles[index++]=k+1;triangles[index++]=k+m+2;triangles[index++]=k+m+1;}
        }
        var mesh=new Mesh{name="ReleaseRim"};mesh.vertices=vertices;mesh.triangles=triangles;mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
    }
    public static void BuildWindows(){
        Directory.CreateDirectory("Builds/Windows");
        string company=PlayerSettings.companyName,product=PlayerSettings.productName;
        try{PlayerSettings.companyName="Pocket Playground QA";PlayerSettings.productName="PocketPit Preview QA";
            var report=BuildPipeline.BuildPlayer(new[]{"Assets/Scenes/Main.unity"},"Builds/Windows/PocketPit.exe",BuildTarget.StandaloneWindows64,BuildOptions.None);
            if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("Windows QA build failed");
        }finally{PlayerSettings.companyName=company;PlayerSettings.productName=product;}
    }
}
