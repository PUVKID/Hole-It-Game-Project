from pathlib import Path
root=Path(__file__).parent/'AndroidFinal'
p=root/'Assets/Scripts/ReferenceGame.cs';s=p.read_text()
def rep(a,b):
 global s
 assert a in s,a[:120]
 s=s.replace(a,b)
rep('public int LevelIndex {get;private set;} public int Collected {get;private set;}', 'public int LevelIndex {get;private set;} public int Collected {get;private set;} public float GrowthPoints {get;private set;}')
rep('public float scale,radius,','public float points,scale,radius,')
rep('readonly List<Actor> actors=', 'class EnvironmentDraw {public ReferenceModel model;public Matrix4x4 matrix;}\n    readonly List<EnvironmentDraw> environmentDraws=new List<EnvironmentDraw>();\n    readonly List<Actor> actors=')
rep('Collected=0;Radius=VisualRadius=1;', 'Collected=0;GrowthPoints=0;Radius=VisualRadius=.7f;')
rep('rotation=q,scale=p.scale};', 'rotation=q,scale=p.scale,points=p.points};')
rep('BuildBombs();', 'BuildEnvironment();BuildBombs();')
rep('GrowthRadius(Collected)', 'GrowthRadius(GrowthPoints)')
rep('GrowthVisualRadius(Collected)', 'GrowthVisualRadius(GrowthPoints)')
rep('GrowthTier(Collected)', 'GrowthTier(GrowthPoints)')
rep('Collected++;bool grew=', 'Collected++;GrowthPoints+=Mathf.Max(0,a.points);bool grew=')
rep('holeVisual.SetRadius(VisualRadius,1);if(originalHole!=null)originalHole.localScale=Vector3.one*(VisualRadius/.85f);','holeVisual.SetRadius(VisualRadius*.85f,1);if(originalHole!=null)originalHole.localScale=Vector3.one*VisualRadius;')
rep('public static int GrowthTier(int count){int tier=0;foreach(int n in GrowthSteps)if(count>=n)tier++;return tier+Mathf.Max(0,(count-700)/100);}\n    public static float GrowthRadius(int count)=>1+GrowthTier(count)*.223f;\n    public static float GrowthVisualRadius(int count)=>1+GrowthTier(count)*.25f;', '''// Verified against the supplied ARM build: each promotion consumes its own cost.
    // Points belong to placements; item count and growth points are different quantities.
    public static int GrowthCost(int tier)=>tier<10?GrowthSteps[tier]:700+((tier+1)%10)*100;
    public static int GrowthTier(float points){int tier=0;while(points>=GrowthCost(tier)&&tier<10000){points-=GrowthCost(tier);tier++;}return tier;}
    public static float GrowthRemainder(float points){int tier=0;while(points>=GrowthCost(tier)&&tier<10000){points-=GrowthCost(tier);tier++;}return points;}
    public static float GrowthRadius(float points)=>.7f+Mathf.Min(10,GrowthTier(points))*.223f;
    public static float GrowthVisualRadius(float points)=>.7f+Mathf.Min(10,GrowthTier(points))*.25f;''')
rep('int next=((Collected-700)/100+1)*100+700;foreach(int n in GrowthSteps)if(n>Collected){next=n;break;}', 'float needed=GrowthCost(GrowthTier(GrowthPoints))-GrowthRemainder(GrowthPoints);')
rep('(next-Collected)+" TO GROW"', 'Mathf.CeilToInt(needed)+" POINTS TO NEXT SIZE"')
rep('screen,null,out var point)', 'screen,canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,out var point)')
rep('Stick.Origin,null,out var local)', 'Stick.Origin,canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,out var local)')
rep('void BuildBombs(){', '''void BuildEnvironment(){environmentDraws.Clear();if(Catalog.environments==null||level.environment==null||level.environment.Length==0)return;int[] themes={1,2,3,8,9};int theme=themes[LevelIndex%themes.Length];foreach(var p in level.environment){ReferenceModel model=null;foreach(var m in Catalog.environments)if(m.id==theme*10+p.kind){model=m;break;}if(model==null)continue;var q=new Quaternion(p.rotation[0],p.rotation[1],p.rotation[2],p.rotation[3]);environmentDraws.Add(new EnvironmentDraw{model=model,matrix=Matrix4x4.TRS(new Vector3(p.position[0],p.position[1],p.position[2]),q,new Vector3(p.scale[0],p.scale[1],p.scale[2]))});}ground.gameObject.SetActive(environmentDraws.Count==0);}
    void BuildBombs(){''')
rep('void RenderActors(){RenderedActors=0;', 'void RenderActors(){foreach(var e in environmentDraws)foreach(var p in e.model.parts)Graphics.DrawMesh(p.mesh,e.matrix*p.local,p.material,0,cam,p.submesh,null,ShadowCastingMode.Off,true);RenderedActors=0;')
p.write_text(s)
shader=(root/'Assets/Shaders/ReferenceSoft.shader').read_text().replace('PocketPit/ReferenceSoft','PocketPit/ReferenceEnvironment').replace('LOD 120','LOD 120\nStencil { Ref 1 ReadMask 1 WriteMask 0 Comp NotEqual Pass Keep }')
(root/'Assets/Shaders/ReferenceEnvironment.shader').write_text(shader)
test=root/'Assets/Tests/Reference/ReferenceTests.cs';s=test.read_text().replace('Assert.Greater(g.Radius,1.2f)','Assert.Greater(g.Radius,1.14f)')
insert='''    [Test]public void GrowthConsumesWeightedPointsAndCapsPhysicalSize(){Assert.AreEqual(0,ReferenceGame.GrowthTier(9));Assert.AreEqual(1,ReferenceGame.GrowthTier(10));Assert.AreEqual(1,ReferenceGame.GrowthTier(29));Assert.AreEqual(2,ReferenceGame.GrowthTier(30));Assert.AreEqual(2,ReferenceGame.GrowthTier(129));Assert.AreEqual(3,ReferenceGame.GrowthTier(130));Assert.AreEqual(10,ReferenceGame.GrowthTier(2980));Assert.AreEqual(11,ReferenceGame.GrowthTier(3780));Assert.AreEqual(2.93f,ReferenceGame.GrowthRadius(2980),.001f);Assert.AreEqual(2.93f,ReferenceGame.GrowthRadius(10000),.001f);Assert.AreEqual(3.2f,ReferenceGame.GrowthVisualRadius(10000),.001f);Assert.AreEqual(3,ReferenceGame.GrowthRemainder(133));}
'''
s=s.replace('    [Test]public void Joystick',insert+'    [Test]public void Joystick');test.write_text(s)
