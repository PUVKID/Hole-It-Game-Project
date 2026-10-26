using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static partial class HoleItBuilder
{
    [MenuItem("Pocket Pit/Build precise control edition")]
    public static void BuildGameplayEdition(){
        EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");ApplySoftLook();
        string[] beauty={"Lipstick","Perfume","NailPolish","CreamJar","Compact","Lotion","Soap","MakeupBrush","Serum","Macaron","JuiceBottle","Chocolate"};
        string[] names={"Lipstick","Perfume","Nail Polish","Cream","Compact","Lotion","Soap","Brush","Serum","Macaron","Juice","Chocolate"};
        foreach(string guid in AssetDatabase.FindAssets("t:EdibleData")){var data=AssetDatabase.LoadAssetAtPath<EdibleData>(AssetDatabase.GUIDToAssetPath(guid));data.growthValue=data.objectType==ObjectType.Bomb?0:1;EditorUtility.SetDirty(data);}
        foreach(var spec in AllSpecs()){var d=Edible(spec.key);d.displayName=spec.display;EditorUtility.SetDirty(d);}
        Edible("Bomb").displayName="Bomb";
        for(int i=0;i<beauty.Length;i++){var d=Edible(beauty[i]);d.displayName=names[i];EditorUtility.SetDirty(d);}
        var settings=AssetDatabase.LoadAssetAtPath<GameSettings>(ResDir+"/GameSettings.asset");
        settings.baseSpeed=3.8f;settings.maxSpeed=4.2f;settings.acceleration=35;settings.deceleration=100;settings.dragSensitivity=.85f;
        settings.speedByRadius=AnimationCurve.Linear(1,1,5,1);settings.swallowDuration=.34f;settings.swallowStagger=.07f;
        settings.attractSpin=0;settings.attractSpeed=0;settings.cameraSmooth=12;settings.cameraZoomSmooth=2.5f;settings.readyDelay=.8f;settings.lowTimeWarning=10;
        settings.growthPunch=.08f;settings.growthPunchDuration=.22f;EditorUtility.SetDirty(settings);
        var sizes=AssetDatabase.LoadAssetAtPath<HoleSizeTable>(ResDir+"/HoleSizeTable.asset");
        float[] thresholds={0,4,12,26,46,74},radii={.9f,1.2f,1.6f,2.1f,2.7f,3.4f},cameraSizes={8.6f,9,9.5f,10.2f,11.2f,12.5f};
        for(int i=0;i<6;i++)sizes.tiers[i]=new HoleSizeTable.Tier{growthRequired=thresholds[i],radius=radii[i],cameraSize=cameraSizes[i]};EditorUtility.SetDirty(sizes);
        foreach(string guid in AssetDatabase.FindAssets("t:LevelData")){var level=AssetDatabase.LoadAssetAtPath<LevelData>(AssetDatabase.GUIDToAssetPath(guid));BuildVariedLevel(level);EditorUtility.SetDirty(level);}
        LayoutEnglishUI();PlayerSettings.productName="Pocket Pit";PlayerSettings.bundleVersion="3.0.0";PlayerSettings.Android.bundleVersionCode=4;
        AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());HoleItProjectValidation.ValidateOrThrow();Debug.Log("PRECISE GAMEPLAY EDITION READY");
    }
    private static void BuildVariedLevel(LevelData level){
        string[][] collections={
            new[]{"Lipstick","CreamJar","Perfume","Compact"},new[]{"DonutS","CookieS","Macaron","Chocolate"},
            new[]{"NailPolish","Soap","Lotion","Serum"},new[]{"BurgerS","SodaS","FriesS","JuiceBottle"},
            new[]{"BallS","DuckS","BlockS","ToyCar"},new[]{"WaffleS","CupcakeS","CakeS","IceCreamS"},
            new[]{"Serum","Lotion","Perfume","MakeupBrush"},new[]{"CoffeeS","MilkCartonS","CookieS","CheeseS"},
            new[]{"BoxS","TrafficCone","CarS","CarBlue"},new[]{"CandyS","BananaS","PopsicleS","Macaron"}};
        string[] subjects={"Vanity","Dessert Shop","Bath Time","Picnic","Toy Room","Cake Shop","Beauty Gifts","Little Cafe","Car Street","Snack Party"};
        string[] shapes={"Clusters","Steps","Circle","Terraces","Castle","Columns"};
        int subject=(level.levelId-1)%10,pattern=(level.levelId-1)%6,round=(level.levelId-1)/10;
        var palette=collections[subject];float spacing=1.6f;foreach(string key in palette)spacing=Mathf.Max(spacing,Edible(key).footprintRadius*2+.18f);
        var places=new List<SpawnPlacement>();var centers=new List<Vector2>();var heights=new List<int>();
        if(pattern==2){
            for(int ring=0;ring<2;ring++){int n=ring==0?10:18;float radius=spacing*(ring==0?1.9f:3.35f);for(int i=0;i<n;i++){float a=(i+.3f*ring)*Mathf.PI*2/n;centers.Add(new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius);heights.Add(1+(i%3==0?2:0));}}
        }else{
            for(int z=0;z<5;z++)for(int x=0;x<6;x++){
                bool edge=x==0||x==5||z==0||z==4;
                if(pattern==4&&!edge&&!(z==2&&(x==2||x==3)))continue;
                centers.Add(new Vector2((x-2.5f)*spacing,(z-2)*spacing));
                int layers=1;
                switch(pattern){
                    case 0: layers=(x+z)%4==0?3:1;break;
                    case 1: layers=Mathf.Max(1,4-Mathf.CeilToInt(Mathf.Max(Mathf.Abs(x-2.5f),Mathf.Abs(z-2))));break;
                    case 3: layers=1+x%3;break;
                    case 4: layers=(x==0||x==5)&&(z==0||z==4)?5:edge?2:1;break;
                    case 5: layers=(x+z*2)%5==0?4:1;break;
                }
                heights.Add(layers);
            }
        }
        float minZ=float.MaxValue,maxX=0,maxZ=0;
        for(int c=0;c<centers.Count;c++){
            var pos=centers[c];minZ=Mathf.Min(minZ,pos.y);maxX=Mathf.Max(maxX,Mathf.Abs(pos.x));maxZ=Mathf.Max(maxZ,Mathf.Abs(pos.y));
            var d=Edible(palette[(c+round)%4]);int layers=Mathf.Min(heights[c],Mathf.Max(1,Mathf.FloorToInt(3.6f/d.stackHeight)));
            for(int h=0;h<layers;h++)places.Add(new SpawnPlacement{edible=d,position=pos,height=h*d.stackHeight,stacked=layers>1,yaw=subject%2==0?0:(c%3-1)*12});
        }
        var starter=Edible(palette[0]);float starterSpacing=Mathf.Max(1.6f,starter.footprintRadius*2+.2f);
        for(int i=0;i<4;i++)places.Add(new SpawnPlacement{edible=starter,position=new Vector2((i-1.5f)*starterSpacing,minZ-spacing)});
        level.holeStart=new Vector2(0,minZ-spacing-3);level.startClearRadius=2;
        level.arenaSize=new Vector2(Mathf.Max(16,maxX*2+5),Mathf.Max(20,(Mathf.Abs(level.holeStart.y)+2.5f)*2));level.mapSize=Mathf.Max(level.arenaSize.x,level.arenaSize.y);
        level.gridSize=new Vector2Int(16,20);level.initialSizeTier=1;level.growthMultiplier=1;level.movementSpeed=3.8f;level.cameraZoomMultiplier=1;
        level.levelTitle=level.levelId+". "+subjects[subject]+" · "+shapes[pattern];
        int[] budgets={25,25,30,25,35,30,25,25,40,25,30,25,30,25,45,30,25,30,40,25,35,30,30,30,40,30,30,25,35,25};level.timeLimit=budgets[level.levelId-1];level.penaltySeconds=2;level.penaltyScore=5;
        level.defaultWrongBehavior=level.levelId==27?WrongTargetBehavior.Blocked:level.levelId%10==0?WrongTargetBehavior.Penalty:WrongTargetBehavior.None;
        AddBombs(level,places,Edible("Bomb"));
        var counts=new Dictionary<EdibleData,int>();foreach(var p in places){if(!counts.ContainsKey(p.edible))counts[p.edible]=0;counts[p.edible]++;}
        level.spawnGroups=new SpawnGroup[counts.Count];int g=0;foreach(var pair in counts)level.spawnGroups[g++]=new SpawnGroup{edible=pair.Key,count=pair.Value};
        level.placements=places.ToArray();level.missions=new MissionData[3];
        for(int i=0;i<3;i++){var d=Edible(palette[i]);level.missions[i]=new MissionData{missionId="L"+level.levelId+"_M"+i,kind=MissionKind.ExactObject,target=d,icon=d.icon,description=d.displayName,requiredCount=Mathf.Max(5,Mathf.CeilToInt(counts[d]*(.78f+round*.04f)))};}
        level.theme=AssetDatabase.LoadAssetAtPath<ThemeData>(DataDir+"/Themes/"+(subject%2==0?"Dessert":"Candy")+".asset");level.schemaVersion=20;
    }
    private static void UIRect(Transform transform,Vector2 anchor,Vector2 pivot,Vector2 position,Vector2 size){if(transform==null)return;var r=(RectTransform)transform;r.anchorMin=r.anchorMax=anchor;r.pivot=pivot;r.anchoredPosition=position;r.sizeDelta=size;}
    private static void LayoutEnglishUI(){
        var canvas=UnityEngine.Object.FindFirstObjectByType<Canvas>();var hud=canvas.transform.Find("HUD");
        var scaler=canvas.GetComponent<CanvasScaler>();scaler.referenceResolution=new Vector2(1080,1920);scaler.matchWidthOrHeight=0;
        foreach(var text in canvas.GetComponentsInChildren<HoleItText>(true)){
            text.text=Korean.Text(text.text);
            foreach(var shadow in text.GetComponents<Shadow>())shadow.enabled=false;
            text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Truncate;
            text.resizeTextForBestFit=true;text.resizeTextMinSize=24;text.resizeTextMaxSize=Mathf.Max(30,Mathf.Min(text.fontSize,72));
            text.lineSpacing=1.12f;
        }
        Vector2 top=new Vector2(.5f,1),pivot=new Vector2(.5f,1);
        foreach(string name in new[]{"TimerBadge","Timer"})UIRect(hud.Find(name),top,pivot,new Vector2(0,-28),new Vector2(210,76));
        foreach(string name in new[]{"LevelBadge","LevelText"})UIRect(hud.Find(name),top,pivot,new Vector2(0,-116),new Vector2(900,50));
        var levelText=hud.Find("LevelText").GetComponent<HoleItText>();levelText.fontSize=30;levelText.resizeTextMaxSize=30;
        var timer=hud.Find("Timer").GetComponent<HoleItText>();timer.fontSize=52;timer.resizeTextMaxSize=52;
        UIRect(hud.Find("MissionPanel"),top,pivot,new Vector2(0,-182),new Vector2(960,144));
        UIRect(hud.Find("PlayHint"),top,pivot,new Vector2(0,-342),new Vector2(980,44));
        var hint=hud.Find("PlayHint").GetComponent<HoleItText>();hint.text="Drag to collect. Avoid bombs!";hint.fontSize=28;hint.resizeTextMaxSize=28;
        UIRect(hud.Find("SizeGauge"),new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(0,54),new Vector2(760,94));
        var size=hud.Find("SizeGauge/SizeText").GetComponent<HoleItText>();size.color=new Color(.25f,.22f,.28f);size.fontSize=32;size.resizeTextMaxSize=32;size.rectTransform.sizeDelta=new Vector2(740,46);
        var title=canvas.transform.Find("TitlePanel");var logo=title.Find("Logo").GetComponent<HoleItText>();logo.fontSize=96;logo.resizeTextMaxSize=96;
        title.Find("Tagline").GetComponent<HoleItText>().text="COLLECT. GROW. AVOID BOMBS.";
        foreach(string name in new[]{"Logo","Tagline","Best"}){var t=title.Find(name).GetComponent<HoleItText>();t.color=new Color(.26f,.20f,.30f);}
        var fail=canvas.transform.Find("FailPanel");
        foreach(string name in new[]{"Top3Header","Top1","Top2","Top3"})fail.Find(name).gameObject.SetActive(false);
        UIRect(fail.Find("ResultCard"),new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(850,1160));
        MoveUI(fail.Find("Title"),.72f);MoveUI(fail.Find("Score"),.64f);MoveUI(fail.Find("NewBest"),.57f);
        MoveUI(fail.Find("Missions"),.47f);MoveUI(fail.Find("RetryButton"),.33f);MoveUI(fail.Find("HomeButton"),.24f);
        var ui=UnityEngine.Object.FindFirstObjectByType<UIManager>();var so=new SerializedObject(ui);
        foreach(string field in new[]{"clearMissionsText","failMissionsText"}){var t=(HoleItText)so.FindProperty(field).objectReferenceValue;if(t==null)continue;t.fontSize=34;t.resizeTextMaxSize=34;t.resizeTextMinSize=30;t.color=new Color(.90f,.88f,.95f);t.alignment=TextAnchor.MiddleCenter;t.rectTransform.sizeDelta=new Vector2(740,160);}
        foreach(string field in new[]{"failTitleText","failScoreText","clearScoreText"}){var t=(HoleItText)so.FindProperty(field).objectReferenceValue;if(t==null)continue;t.fontSize=field=="failTitleText"?58:48;t.resizeTextMaxSize=t.fontSize;t.rectTransform.sizeDelta=new Vector2(800,120);}
        foreach(var text in canvas.GetComponentsInChildren<HoleItText>(true))if(System.Text.RegularExpressions.Regex.IsMatch(text.text,"[가-힣]"))throw new Exception("Untranslated UI: "+text.name+" = "+text.text);
        var mode=canvas.GetComponentInChildren<FamilyMode>(true);if(mode!=null){mode.hint=hint;EditorUtility.SetDirty(mode);}
    }
}
