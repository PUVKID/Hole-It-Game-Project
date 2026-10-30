from pathlib import Path
p=Path(__file__).parent/'AndroidFinal/Assets/Scripts/ReferenceGame.cs'
s=p.read_text(encoding='utf-8')
def rep(a,b):
 global s
 assert a in s,a[:100]
 s=s.replace(a,b)
rep('public float Remaining {get;private set;} public float Radius {get;private set;}','public float Remaining {get;private set;} public float Radius {get;private set;} public float VisualRadius {get;private set;}')
rep('const float CellSize=3;Camera cam;Transform hole,ground;', 'const float CellSize=3;Camera cam;Transform hole,ground,originalHole;Text holeSize,growthToast;float growthFlash;readonly Dictionary<string,Sprite> styleSprites=new Dictionary<string,Sprite>();')
rep('cameraDistance=24','cameraDistance=14.3f')
rep('Radius=1;Remaining=level.time','Radius=VisualRadius=1;growthFlash=0;Remaining=level.time')
rep('cameraFocus=HolePosition+Vector3.forward*3','cameraFocus=HolePosition')
rep('HolePosition+Vector3.forward*3,','HolePosition,')
rep('24+(Radius-1)*2','14.3f+(VisualRadius-1)*2')
rep('Quaternion.Euler(62,0,0)','Quaternion.Euler(60,0,0)')
rep('hole.name="Hole";holeVisual=', 'hole.name="Hole";var mesh=Resources.Load<Mesh>("ReferenceStyle/OriginalHole");if(mesh!=null){foreach(var r in hole.GetComponentsInChildren<MeshRenderer>())if(r.name=="Rim"||r.name=="SculptedRim")r.enabled=false;originalHole=new GameObject("Original Hole Geometry",typeof(MeshFilter),typeof(MeshRenderer)).transform;originalHole.SetParent(hole,false);originalHole.localPosition=new Vector3(0,.025f,0);originalHole.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=originalHole.GetComponent<MeshRenderer>();renderer.sharedMaterial=Resources.Load<Material>("ReferenceStyle/OriginalHole");renderer.shadowCastingMode=ShadowCastingMode.Off;}holeVisual=')
rep('Radius=Mathf.Lerp(Radius,target,1-Mathf.Exp(-10*dt));', 'Radius=Mathf.Lerp(Radius,target,1-Mathf.Exp(-10*dt));VisualRadius=Mathf.Lerp(VisualRadius,GrowthVisualRadius(Collected)*(playTime<sizeUntil?1.3f:1),1-Mathf.Exp(-10*dt));')
rep('hole.position=HolePosition;holeVisual.SetRadius(Radius,1);UpdateStickVisual();', 'hole.position=HolePosition;holeVisual.SetRadius(VisualRadius,1);if(originalHole!=null)originalHole.localScale=Vector3.one*(VisualRadius/.85f);UpdateStickVisual();UpdateHoleLabels();')
rep('public static float GrowthRadius(int count){int tier=0;foreach(int n in GrowthSteps)if(count>=n)tier++;return 1+tier*.223f+Mathf.Max(0,(count-700)/100)*.223f;}', 'public static int GrowthTier(int count){int tier=0;foreach(int n in GrowthSteps)if(count>=n)tier++;return tier+Mathf.Max(0,(count-700)/100);}\n    public static float GrowthRadius(int count)=>1+GrowthTier(count)*.223f;\n    public static float GrowthVisualRadius(int count)=>1+GrowthTier(count)*.25f;')
rep('Collected++;Sound(GrowthRadius(Collected)>before?1:0);', 'Collected++;bool grew=GrowthRadius(Collected)>before;if(grew)growthFlash=Time.unscaledTime+.9f;Sound(grew?1:0);')
rep('Image Panel(string name,', '''Sprite Style(string key){if(!styleSprites.TryGetValue(key,out var sprite)){sprite=Resources.Load<Sprite>("ReferenceStyle/"+key);styleSprites[key]=sprite;}return sprite;}
    Image Panel(string name,''')
rep('im.raycastTarget=false;return im;', '''im.raycastTarget=false;string key=name.StartsWith("Goal")?"goal-card":name=="TimerBadge"?"timer-bg":name.StartsWith("Booster")?"booster-Btn-blue":name=="Play"||name=="Continue"||name=="Resume"?"btn-big-green":name=="Pause"?"btn-big-Blue":null;if(key!=null&&Style(key)!=null){im.sprite=Style(key);im.type=Image.Type.Simple;im.color=Color.white;}return im;''')
rep('Panel("GrowthChip",hud.transform', '''holeSize=Label("Hole Size",hud.transform,"Size 1",38,Color.white,new Vector2(.5f,.5f),new Vector2(400,70),Vector2.zero);var sizeOutline=holeSize.gameObject.AddComponent<Outline>();sizeOutline.effectColor=new Color(0,0,0,.8f);sizeOutline.effectDistance=new Vector2(2,-2);growthToast=Label("Growth Flash",hud.transform,"SIZE UP!",54,Color.white,new Vector2(.5f,.5f),new Vector2(650,90),Vector2.zero);growthToast.gameObject.AddComponent<Outline>().effectDistance=new Vector2(3,-3);
        Panel("GrowthChip",hud.transform''')
rep('float width=Mathf.Min(295,990f/count-14);','float width=Mathf.Min(164,990f/count-14);')
rep('new Vector2(width,134),new Color(1,1,1,.94f),new Vector2((i-(count-1)*.5f)*(width+14),-198)', 'new Vector2(width,208),Color.white,new Vector2((i-(count-1)*.5f)*(width+14),-234)')
rep('new Vector2(.33f,.58f),new Vector2(width*.43f,86)','new Vector2(.5f,.64f),new Vector2(width*.72f,115)')
rep('"",40,Ink,new Vector2(.72f,.5f),new Vector2(width*.5f,110)', '"",38,Color.white,new Vector2(.5f,.24f),new Vector2(width*.9f,62)')
rep('pad.sprite=circle;', 'pad.sprite=Style("JoystickOutline")??circle;')
rep('knob.sprite=circle;', 'knob.sprite=Style("JoystickMarker")??circle;')
rep('void UpdateStickVisual(){', '''void UpdateHoleLabels(){if(holeSize==null)return;Vector3 screen=cam.WorldToScreenPoint(HolePosition+Vector3.back*(VisualRadius+1));RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)hud.transform,screen,null,out var point);holeSize.rectTransform.anchoredPosition=point;holeSize.text="Size "+(GrowthTier(Collected)+1);growthToast.gameObject.SetActive(State==ScreenState.Playing&&Time.unscaledTime<growthFlash);growthToast.rectTransform.anchoredPosition=point+new Vector2(0,240+(growthFlash-Time.unscaledTime)*30);}
    void UpdateStickVisual(){''')
p.write_text(s,encoding='utf-8')
