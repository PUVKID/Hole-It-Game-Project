using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.InputSystem.EnhancedTouch;
using ETouch=UnityEngine.InputSystem.EnhancedTouch.Touch;
#endif
/// <summary>Standalone recreation using the supplied application's authored content.
/// Actors are data, not thousands of rigidbodies. Only unsupported stacks and swallowed items animate.
/// Every visible mesh uses bounded, reused instancing batches; gameplay has no per-item Update calls.</summary>
public class ReferenceGame:MonoBehaviour {
    public enum ScreenState {Home,Playing,Paused,Won,Lost}
    static readonly int[] GrowthSteps={10,20,100,150,200,300,400,500,600,700};
    public ScreenState State {get;private set;}
    public int LevelIndex {get;private set;} public int Collected {get;private set;} public float GrowthPoints {get;private set;}
    public float Remaining {get;private set;} public float Radius {get;private set;} public float VisualRadius {get;private set;}
    public Vector3 HolePosition {get;private set;} public Vector2 Velocity {get;private set;}
    public FloatingStick Stick {get;}=new FloatingStick();
    public int ActorCount=>actors.Count;public int RenderedActors {get;private set;}
    public ReferenceCatalog Catalog {get;private set;}
    public string FailureReason {get;private set;} public int[] GoalCounts=>goalCounts;
    public float MaximumSpeed=>4.8f*(speedUntil>playTime?1.3f:1);
    class Actor {public ReferenceModel model;public Vector3 pos;public Quaternion rotation;public float points,scale,radius,bottomOffset,topOffset,fallSpeed,dropTime,dropY;public int state,support=-1;public bool falling;public List<int> above=new List<int>();public float Bottom=>pos.y+bottomOffset;public float Top=>pos.y+topOffset;}
    class Batch {public ReferencePart part;public Matrix4x4[] matrices=new Matrix4x4[1023];public int count;}
    class EnvironmentDraw {public ReferenceModel model;public Matrix4x4 matrix;}
    readonly List<Rect> playground=new List<Rect>();
    readonly List<EnvironmentDraw> environmentDraws=new List<EnvironmentDraw>();readonly List<Batch> environmentBatches=new List<Batch>();
    readonly List<Actor> testBombs=new List<Actor>();
    readonly List<Actor> actors=new List<Actor>(3200);readonly List<int> dropping=new List<int>(128),falling=new List<int>(256),nearby=new List<int>(256);
    readonly Dictionary<int,Vector3[]> modelVertices=new Dictionary<int,Vector3[]>();
    readonly Dictionary<int,ReferenceModel> models=new Dictionary<int,ReferenceModel>();readonly Dictionary<long,List<int>> cells=new Dictionary<long,List<int>>();
    readonly Dictionary<int,List<Batch>> batches=new Dictionary<int,List<Batch>>();
    readonly List<RaycastResult> hits=new List<RaycastResult>();PointerEventData pointer;
    const float CellSize=3;Camera cam;Transform hole,ground,originalHole;Text holeSize,growthToast;Image growthFill;float growthFlash;readonly Dictionary<string,Sprite> styleSprites=new Dictionary<string,Sprite>();HoleVisualController holeVisual;Material floor;Light sun;
    ReferenceLevel level;Vector2 min,max;Vector3 cameraFocus;float cameraDistance=14.3f,playTime,clockTick,freezeUntil,sizeUntil,speedUntil,finderUntil;
    int[] goalCounts;int finger=-1;bool mute;bool[] boostUsed=new bool[4];int unlocked;AudioSource[] sounds;int soundIndex;float lastPop=-1;
    Canvas canvas;RectTransform canvasRect,safe,stickBase,stickKnob;GameObject hud,home,result,pause,settings;Text timer,sizeLabel,levelLabel,resultTitle,resultDetail,homeLevel,homeTitle;Text[] goalLabels;Image[] goalIcons;Button[] boostButtons;Text[] boostLabels;Sprite circle,rounded;Font font;
    public static ReferenceGame Instance {get;private set;}
    void Awake(){Instance=this;Catalog=Resources.Load<ReferenceCatalog>("ReferenceCatalog");if(Catalog==null)throw new InvalidOperationException("ReferenceCatalog missing");font=Catalog.font;foreach(var m in Catalog.models){models[m.id]=m;var points=new List<Vector3>();foreach(var part in m.parts)foreach(var v in part.mesh.vertices)points.Add(part.local.MultiplyPoint3x4(v));modelVertices[m.id]=points.ToArray();}
        Application.targetFrameRate=60;Application.runInBackground=true;QualitySettings.vSyncCount=0;Screen.sleepTimeout=SleepTimeout.NeverSleep;
#if UNITY_ANDROID && !UNITY_EDITOR
        if(Screen.width>900){int width=900;Screen.SetResolution(width,Mathf.RoundToInt(width*(float)Screen.height/Screen.width),true);}
#endif
#if ENABLE_INPUT_SYSTEM
        EnhancedTouchSupport.Enable();
#endif
        mute=PlayerPrefs.GetInt("Gaeun.Ref.Mute",0)==1;sounds=new AudioSource[4];for(int i=0;i<sounds.Length;i++){sounds[i]=gameObject.AddComponent<AudioSource>();sounds[i].playOnAwake=false;sounds[i].volume=.35f;}unlocked=Mathf.Clamp(PlayerPrefs.GetInt("Gaeun.Ref.Unlocked",1),1,30);CreateWorld();CreateUI();LoadLevel(Mathf.Clamp(PlayerPrefs.GetInt("Gaeun.Ref.Selected",0),0,unlocked-1));ShowHome();
    }
    void OnDestroy(){if(Instance==this)Instance=null;if(floor!=null)Destroy(floor);
#if ENABLE_INPUT_SYSTEM
        EnhancedTouchSupport.Disable();
#endif
    }
    void CreateWorld(){
        cam=new GameObject("Main Camera").AddComponent<Camera>();cam.tag="MainCamera";cam.fieldOfView=46;cam.nearClipPlane=.1f;cam.farClipPlane=130;cam.allowHDR=false;cam.allowMSAA=true;cam.backgroundColor=new Color(.77f,.77f,.80f);
        cam.gameObject.AddComponent<AudioListener>();sun=new GameObject("Soft Key Light").AddComponent<Light>();sun.type=LightType.Directional;sun.transform.rotation=Quaternion.Euler(55,-35,0);sun.intensity=.8f;sun.shadows=LightShadows.Soft;sun.shadowStrength=.26f;
        sun.intensity=.65f;RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.60f,.62f,.65f);RenderSettings.ambientEquatorColor=new Color(.40f,.41f,.44f);RenderSettings.ambientGroundColor=new Color(.25f,.24f,.27f);
        var g=GameObject.CreatePrimitive(PrimitiveType.Plane);g.name="Arena";Destroy(g.GetComponent<Collider>());ground=g.transform;floor=new Material(Catalog.groundMaterial);g.GetComponent<Renderer>().sharedMaterial=floor;
        hole=Instantiate(Catalog.holeVisual).transform;hole.name="Hole";var mesh=Resources.Load<Mesh>("ReferenceStyle/OriginalHole");if(mesh!=null){foreach(var t in hole.GetComponentsInChildren<Transform>())if(t.name=="Rim"||t.name=="SculptedRim")t.gameObject.SetActive(false);originalHole=new GameObject("Original Hole Geometry",typeof(MeshFilter),typeof(MeshRenderer)).transform;originalHole.SetParent(hole,false);originalHole.localPosition=new Vector3(0,.025f,0);originalHole.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=originalHole.GetComponent<MeshRenderer>();renderer.sharedMaterial=Resources.Load<Material>("ReferenceStyle/OriginalHole");renderer.shadowCastingMode=ShadowCastingMode.Off;}holeVisual=hole.GetComponent<HoleVisualController>();if(holeVisual==null)holeVisual=hole.gameObject.AddComponent<HoleVisualController>();
    }
    static long Key(int x,int z)=>((long)x<<32)^(uint)z;
    void AddCell(int i){var p=actors[i].pos;long key=Key(Mathf.FloorToInt(p.x/CellSize),Mathf.FloorToInt(p.z/CellSize));if(!cells.TryGetValue(key,out var list)){list=new List<int>(20);cells[key]=list;}list.Add(i);}
    void Query(Vector3 p,float radius,List<int> list){list.Clear();for(int z=Mathf.FloorToInt((p.z-radius)/CellSize);z<=Mathf.FloorToInt((p.z+radius)/CellSize);z++)for(int x=Mathf.FloorToInt((p.x-radius)/CellSize);x<=Mathf.FloorToInt((p.x+radius)/CellSize);x++)if(cells.TryGetValue(Key(x,z),out var cell))list.AddRange(cell);}
    public void LoadLevel(int index){
        LevelIndex=Mathf.Clamp(index,0,Catalog.levels.Length-1);level=Catalog.levels[LevelIndex];actors.Clear();cells.Clear();falling.Clear();dropping.Clear();batches.Clear();Collected=0;GrowthPoints=0;Radius=VisualRadius=.7f;growthFlash=0;Remaining=level.time;FailureReason="";playTime=0;freezeUntil=sizeUntil=speedUntil=finderUntil=0;boostUsed=new bool[4];Stick.Release();Velocity=Vector2.zero;
        min=new Vector2(-8,-29);max=new Vector2(8,-12);
        foreach(var p in level.points){var m=models[p.id];var q=new Quaternion(p.rotation[0],p.rotation[1],p.rotation[2],p.rotation[3]);if(Quaternion.Dot(q,q)<.01f)q=Quaternion.identity;else q=Quaternion.Normalize(q);
            var a=new Actor{model=m,pos=new Vector3(p.position[0],p.position[1],p.position[2]),rotation=q,scale=p.scale,points=p.points};Bounds bounds=ExactBounds(m,q,p.scale);
            a.bottomOffset=bounds.min.y;a.topOffset=bounds.max.y;a.radius=Mathf.Max(.08f,Mathf.Min(bounds.extents.x,bounds.extents.z));
            // Authored positions can sit a few millimetres below the floor.
            if(a.Bottom<0)a.pos.y-=a.Bottom;
            actors.Add(a);AddCell(actors.Count-1);min=Vector2.Min(min,new Vector2(a.pos.x-4,a.pos.z-4));max=Vector2.Max(max,new Vector2(a.pos.x+4,a.pos.z+4));
        }
        // Original stages share a forward-facing starting plaza at z=-23.5.
        HolePosition=new Vector3(0,0,-23.5f);Query(HolePosition,1,nearby);bool occupied=false;foreach(int i in nearby)if((actors[i].pos-HolePosition).sqrMagnitude<1)occupied=true;
        if(occupied)HolePosition=new Vector3(0,0,min.y+1.5f);
        min=Vector2.Min(min,new Vector2(HolePosition.x-5,HolePosition.z-5));max=Vector2.Max(max,new Vector2(HolePosition.x+5,HolePosition.z+5));
        // The original authored pattern is retained. The playable margin is wider for joystick manoeuvring.
        for(int i=0;i<actors.Count;i++){var a=actors[i];if(a.Bottom<.15f)continue;Query(a.pos,a.radius+2,nearby);float highest=-1;
            foreach(int j in nearby){if(i==j)continue;var b=actors[j];if(b.Bottom>=a.Bottom-.1f||b.Top>a.Bottom+.35f)continue;float dx=a.pos.x-b.pos.x,dz=a.pos.z-b.pos.z;if(dx*dx+dz*dz>Mathf.Pow(a.radius+b.radius*.75f,2))continue;if(b.Top>highest){highest=b.Top;a.support=j;}}
            if(a.support>=0)actors[a.support].above.Add(i);else Wake(i);
        }
        BuildEnvironment();BuildBombs();
        foreach(var a in actors)if(!batches.ContainsKey(a.model.id)){var list=new List<Batch>();foreach(var part in a.model.parts)list.Add(new Batch{part=part});batches[a.model.id]=list;}
        var mid=(min+max)*.5f;ground.position=new Vector3(mid.x,-.02f,mid.y);ground.localScale=new Vector3((max.x-min.x+35)/10,1,(max.y-min.y+35)/10);
        Color[] palette={new Color(.86f,.74f,.79f),new Color(.72f,.81f,.77f),new Color(.77f,.75f,.87f),new Color(.79f,.81f,.69f)};Color color=palette[LevelIndex%palette.Length];floor.SetColor("_ColorA",color);floor.SetColor("_ColorB",color*.98f);floor.SetFloat("_Tiles",Mathf.Max(max.x-min.x,max.y-min.y)/1.2f);
        goalCounts=new int[level.goals.Length];BuildGoals();UpdateHUD();hole.position=HolePosition;holeVisual.SetRadius(Radius,1);cameraDistance=14.3f;cameraFocus=HolePosition;PlaceCamera();
        PlayerPrefs.SetInt("Gaeun.Ref.Selected",LevelIndex);
    }
    Bounds ExactBounds(ReferenceModel model,Quaternion rotation,float scale){var points=modelVertices[model.id];var result=new Bounds(rotation*(points[0]*scale),Vector3.zero);for(int i=1;i<points.Length;i++)result.Encapsulate(rotation*(points[i]*scale));return result;}
    static Bounds WorldBounds(Bounds b,Quaternion q,float scale){Bounds result=new Bounds();for(int c=0;c<8;c++){Vector3 v=q*((b.center+Vector3.Scale(b.extents,new Vector3((c&1)==0?-1:1,(c&2)==0?-1:1,(c&4)==0?-1:1)))*scale);if(c==0)result=new Bounds(v,Vector3.zero);else result.Encapsulate(v);}return result;}
    void BuildEnvironment(){environmentDraws.Clear();environmentBatches.Clear();playground.Clear();if(Catalog.environments==null||level.environment==null||level.environment.Length==0)return;int[] themes={1,2,3,8,9};int theme=themes[LevelIndex%themes.Length];foreach(var p in level.environment){ReferenceModel model=null;foreach(var m in Catalog.environments)if(m.id==theme*10+p.kind){model=m;break;}if(model==null)continue;if(p.kind==5){float x=p.position[0],z=p.position[2],rx=model.bounds.extents.x*p.scale[0],rz=model.bounds.extents.z*p.scale[2];playground.Add(Rect.MinMaxRect(x-rx,z-rz,x+rx,z+rz));}var q=new Quaternion(p.rotation[0],p.rotation[1],p.rotation[2],p.rotation[3]);environmentDraws.Add(new EnvironmentDraw{model=model,matrix=Matrix4x4.TRS(new Vector3(p.position[0],p.position[1],p.position[2]),q,new Vector3(p.scale[0],p.scale[1],p.scale[2]))});}var groups=new Dictionary<string,Batch>();foreach(var e in environmentDraws)foreach(var part in e.model.parts){string key=part.mesh.GetInstanceID()+"_"+part.material.GetInstanceID()+"_"+part.submesh;if(!groups.TryGetValue(key,out var batch)||batch.count==1023){batch=new Batch{part=part};groups[key]=batch;environmentBatches.Add(batch);}batch.matrices[batch.count++]=e.matrix*part.local;}ground.gameObject.SetActive(environmentDraws.Count==0);}
    void BuildBombs(){if(level.bombs<=0)return;var model=Catalog.bomb;if(model==null||model.parts.Length==0)throw new InvalidOperationException("Bomb model missing");
        for(int n=0;n<level.bombs;n++){float t=(n+1f)/(level.bombs+1);Vector3 p=new Vector3(Mathf.Lerp(min.x+3,max.x-3,t),0,Mathf.Lerp(min.y+5,max.y-5,(n*.37f+.3f)%1));if(Vector3.Distance(p,HolePosition)<4)p.z+=5;Query(p,2,nearby);bool clash=false;foreach(int i in nearby)if(Vector2.Distance(new Vector2(p.x,p.z),new Vector2(actors[i].pos.x,actors[i].pos.z))<actors[i].radius+.8f)clash=true;if(clash)p.x=min.x+1.2f+n*.9f;
            actors.Add(new Actor{model=model,pos=p,rotation=Quaternion.identity,scale=1,radius=.42f,bottomOffset=0,topOffset=1});AddCell(actors.Count-1);}
    }
    void Wake(int i){var a=actors[i];if(a.falling||a.state!=0)return;a.falling=true;a.fallSpeed=0;falling.Add(i);}
    bool OnPlayground(Vector2 p){if(playground.Count==0)return true;foreach(var tile in playground)if(tile.Contains(p))return true;return false;}
    public bool CanStandAt(Vector3 p){if(!OnPlayground(new Vector2(p.x,p.z)))return false;for(int i=0;i<12;i++){float a=i*Mathf.PI/6;if(!OnPlayground(new Vector2(p.x+Mathf.Cos(a)*Radius,p.z+Mathf.Sin(a)*Radius)))return false;}return true;}
    public Vector3 ConstrainMovement(Vector3 from,Vector3 target){if(CanStandAt(target))return target;if(!CanStandAt(from))return OnPlayground(new Vector2(target.x,target.z))?target:from;var alongX=new Vector3(target.x,0,from.z);if(CanStandAt(alongX))return alongX;var alongZ=new Vector3(from.x,0,target.z);return CanStandAt(alongZ)?alongZ:from;}
    public void StartPlay(){State=ScreenState.Playing;home.SetActive(false);result.SetActive(false);pause.SetActive(false);settings.SetActive(false);hud.SetActive(true);Stick.Release();RefreshBoosters();}
    public void ShowHome(){State=ScreenState.Home;Stick.Release();Velocity=Vector2.zero;home.SetActive(true);hud.SetActive(false);result.SetActive(false);pause.SetActive(false);settings.SetActive(false);homeLevel.text="LEVEL "+(LevelIndex+1);}
    public void Retry(){LoadLevel(LevelIndex);StartPlay();}
    public void Pause(){if(State!=ScreenState.Playing)return;State=ScreenState.Paused;Stick.Release();Velocity=Vector2.zero;pause.SetActive(true);}
    public void Resume(){if(State!=ScreenState.Paused)return;State=ScreenState.Playing;pause.SetActive(false);}
    public void SetCountdownForTest(float value){Remaining=value;}
    public void BeginStick(Vector2 p){Stick.Radius=Mathf.Clamp(Screen.width*.11f,42,120);Stick.Begin(p);}
    public void MoveStick(Vector2 p){Stick.Move(p);}
    public void ReleaseStick(){Stick.Release();Velocity=Vector2.zero;}
    bool CanTouch(Vector2 p){if(EventSystem.current==null)return true;if(pointer==null)pointer=new PointerEventData(EventSystem.current);pointer.position=p;hits.Clear();EventSystem.current.RaycastAll(pointer,hits);return hits.Count==0;}
    void ReadInput(){
#if ENABLE_INPUT_SYSTEM
        var touches=ETouch.activeTouches;
        if(Stick.Held&&finger>=0){bool found=false;foreach(var t in touches)if(t.finger.index==finger){found=true;if(t.phase==UnityEngine.InputSystem.TouchPhase.Ended||t.phase==UnityEngine.InputSystem.TouchPhase.Canceled)ReleaseStick();else MoveStick(t.screenPosition);break;}if(!found)ReleaseStick();}
        else if(!Stick.Held)foreach(var t in touches)if(t.phase==UnityEngine.InputSystem.TouchPhase.Began&&CanTouch(t.screenPosition)){finger=t.finger.index;BeginStick(t.screenPosition);break;}
        if(!Application.isMobilePlatform&&touches.Count==0&&Mouse.current!=null){var m=Mouse.current;if(!Stick.Held&&m.leftButton.wasPressedThisFrame&&CanTouch(m.position.ReadValue())){finger=-1;BeginStick(m.position.ReadValue());}else if(Stick.Held&&finger==-1){if(m.leftButton.isPressed)MoveStick(m.position.ReadValue());else ReleaseStick();}}
#endif
    }
    public bool TestInput;public Vector2 TestDirection;
    void Update(){float dt=Mathf.Min(Time.deltaTime,.05f);
        if(State==ScreenState.Playing){if(!TestInput)ReadInput();Vector2 input=TestInput?Vector2.ClampMagnitude(TestDirection,1):Stick.Value;
            Vector2 desired=input*MaximumSpeed;Velocity=input.sqrMagnitude<.0001f?Vector2.zero:Vector2.Lerp(Velocity,desired,1-Mathf.Exp(-25*dt));
            HolePosition=ConstrainMovement(HolePosition,HolePosition+new Vector3(Velocity.x,0,Velocity.y)*dt);HolePosition=new Vector3(Mathf.Clamp(HolePosition.x,min.x+Radius,max.x-Radius),0,Mathf.Clamp(HolePosition.z,min.y+Radius,max.y-Radius));
            playTime+=dt;if(playTime>=freezeUntil)Remaining=Mathf.Max(0,Remaining-dt);if(Remaining<=0)Fail("TIME UP");
            UpdateFalls(dt);if(State==ScreenState.Playing)UpdateMouth(dt);float target=GrowthRadius(GrowthPoints)*(playTime<sizeUntil?1.3f:1);Radius=Mathf.Lerp(Radius,target,1-Mathf.Exp(-10*dt));VisualRadius=Mathf.Lerp(VisualRadius,GrowthVisualRadius(GrowthPoints)*(playTime<sizeUntil?1.3f:1),1-Mathf.Exp(-10*dt));
            clockTick-=dt;if(clockTick<=0){clockTick=.1f;UpdateHUD();}
        }
        hole.position=HolePosition;holeVisual.SetRadius(VisualRadius*.85f,1);if(originalHole!=null)originalHole.localScale=Vector3.one*VisualRadius;UpdateStickVisual();UpdateHoleLabels();
    }
    // Verified against the supplied ARM build: each promotion consumes its own cost.
    // Points belong to placements; item count and growth points are different quantities.
    public static int GrowthCost(int tier)=>tier<10?GrowthSteps[tier]:700+((tier+1)%10)*100;
    public static int GrowthTier(float points){int tier=0;while(points>=GrowthCost(tier)&&tier<10000){points-=GrowthCost(tier);tier++;}return tier;}
    public static float GrowthRemainder(float points){int tier=0;while(points>=GrowthCost(tier)&&tier<10000){points-=GrowthCost(tier);tier++;}return points;}
    public static float GrowthRadius(float points)=>.7f+Mathf.Min(10,GrowthTier(points))*.223f;
    public static float GrowthVisualRadius(float points)=>.7f+Mathf.Min(10,GrowthTier(points))*.25f;
    void UpdateFalls(float dt){for(int k=falling.Count-1;k>=0;k--){int i=falling[k];var a=actors[i];if(a.state!=0){a.falling=false;falling.RemoveAt(k);continue;}float floorY=0;if(a.support>=0&&actors[a.support].state==0)floorY=Mathf.Max(0,actors[a.support].Top);a.fallSpeed+=16*dt;a.pos.y=Mathf.Max(floorY-a.bottomOffset,a.pos.y-a.fallSpeed*dt);if(a.Bottom<=floorY+.005f){a.falling=false;falling.RemoveAt(k);}else foreach(int child in a.above)Wake(child);}}
    void UpdateMouth(float dt){
        for(int k=dropping.Count-1;k>=0;k--){var a=actors[dropping[k]];a.dropTime+=dt;float t=a.dropTime/.28f;a.pos.y=a.dropY-4*t*t;a.rotation=Quaternion.AngleAxis(dt*35,Vector3.right)*a.rotation;if(t<1)continue;a.state=2;dropping.RemoveAt(k);if(a.model.id==-1){Fail("BOOM! TRY AGAIN");return;}float before=GrowthRadius(GrowthPoints);Collected++;GrowthPoints+=Mathf.Max(0,a.points);bool grew=GrowthRadius(GrowthPoints)>before;if(grew)growthFlash=Time.unscaledTime+.9f;Sound(grew?1:0);for(int g=0;g<level.goals.Length;g++)if(level.goals[g].id==a.model.id)goalCounts[g]=Mathf.Min(goalCounts[g]+1,level.goals[g].amount);}
        Query(HolePosition,Radius+1,nearby);int begun=0;
        foreach(int i in nearby){var a=actors[i];if(a.state!=0||a.Bottom>.20f||a.radius>Radius*1.02f)continue;float dx=a.pos.x-HolePosition.x,dz=a.pos.z-HolePosition.z;float reach=Mathf.Max(.12f,Radius-a.radius*.45f);if(dx*dx+dz*dz>reach*reach)continue;
            a.state=1;a.dropTime=0;a.dropY=a.pos.y;dropping.Add(i);foreach(int child in a.above)Wake(child);if(++begun>=24)break;
        }
        bool done=true;if(level.goals.Length==0){foreach(var a in actors)if(a.model.id!=-1&&a.state!=2){done=false;break;}}else for(int g=0;g<goalCounts.Length;g++)if(goalCounts[g]<level.goals[g].amount)done=false;
        if(done&&dropping.Count==0)Win();
    }
    void Sound(int id){if(mute||Catalog.sounds==null||id>=Catalog.sounds.Length||Catalog.sounds[id]==null)return;if(id==0&&Time.unscaledTime-lastPop<.055f)return;lastPop=Time.unscaledTime;var s=sounds[soundIndex++%sounds.Length];s.pitch=id==0?1+(Collected%8)*.025f:1;s.PlayOneShot(Catalog.sounds[id]);}
    void Fail(string reason){State=ScreenState.Lost;FailureReason=reason;Stick.Release();Velocity=Vector2.zero;Sound(2);ShowResult(false,reason);}
    void Win(){State=ScreenState.Won;Stick.Release();Velocity=Vector2.zero;Sound(3);unlocked=Mathf.Max(unlocked,Mathf.Min(30,LevelIndex+2));PlayerPrefs.SetInt("Gaeun.Ref.Unlocked",unlocked);PlayerPrefs.Save();ShowResult(true,"LEVEL COMPLETE!");}
    void LateUpdate(){cameraFocus=Vector3.Lerp(cameraFocus,HolePosition,1-Mathf.Exp(-12*Mathf.Min(Time.deltaTime,.05f)));cameraDistance=Mathf.Lerp(cameraDistance,14.3f+(VisualRadius-1)*2,1-Mathf.Exp(-2*Time.deltaTime));PlaceCamera();RenderActors();}
    void PlaceCamera(){cam.transform.rotation=Quaternion.Euler(60,0,0);cam.transform.position=cameraFocus-cam.transform.forward*cameraDistance;}
    void RenderActors(){foreach(var b in environmentBatches)Graphics.DrawMeshInstanced(b.part.mesh,b.part.submesh,b.part.material,b.matrices,b.count,null,ShadowCastingMode.Off,true,0,cam,LightProbeUsage.Off);RenderedActors=0;foreach(var pair in batches)foreach(var b in pair.Value)b.count=0;float range=cameraDistance*.85f;
        foreach(var a in actors){if(a.state==2)continue;Vector3 delta=a.pos-cameraFocus;if(Mathf.Abs(delta.x)>range*.75f||Mathf.Abs(delta.z)>range||a.pos.y<-6)continue;RenderedActors++;var matrix=Matrix4x4.TRS(a.pos,a.rotation,Vector3.one*a.scale);foreach(var b in batches[a.model.id]){b.matrices[b.count++]=matrix*b.part.local;if(b.count==1023)Flush(b);}}
        foreach(var pair in batches)foreach(var b in pair.Value)Flush(b);
    }
    public void RenderForCapture(){RenderActors();}
    void Flush(Batch b){if(b.count==0)return;Graphics.DrawMeshInstanced(b.part.mesh,b.part.submesh,b.part.material,b.matrices,b.count,null,ShadowCastingMode.On,true,0,cam,LightProbeUsage.Off);b.count=0;}
    void OnApplicationPause(bool p){if(p)Pause();}void OnApplicationFocus(bool f){if(!f)Pause();}
    void Boost(int index){int[] activation={3,5,9,12};if(State!=ScreenState.Playing||LevelIndex+1<activation[index]||boostUsed[index])return;boostUsed[index]=true;if(index==0)sizeUntil=playTime+8;if(index==1)speedUntil=playTime+8;if(index==2)finderUntil=playTime+12;if(index==3)freezeUntil=playTime+8;Sound(4);RefreshBoosters();}
    // UI is built once, then only values change. Textures/fonts are bundled; no network is used.
    Sprite Style(string key){if(!styleSprites.TryGetValue(key,out var sprite)){sprite=Resources.Load<Sprite>("ReferenceStyle/"+key);styleSprites[key]=sprite;}return sprite;}
    Image Panel(string name,Transform parent,Vector2 anchor,Vector2 size,Color color,Vector2 offset){var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);var r=(RectTransform)go.transform;r.anchorMin=r.anchorMax=anchor;r.sizeDelta=size;r.anchoredPosition=offset;var im=go.GetComponent<Image>();im.sprite=rounded;im.type=Image.Type.Sliced;im.color=color;im.raycastTarget=false;string key=name.StartsWith("Goal")?"goal-card":name=="TimerBadge"?"timer-bg":name.StartsWith("Booster")?"booster-Btn-blue":name=="Play"||name=="Continue"||name=="Resume"?"btn-big-green":name=="Pause"?"btn-big-Blue":null;if(key!=null&&Style(key)!=null){im.sprite=Style(key);im.type=Image.Type.Simple;im.color=Color.white;}return im;}
    Text Label(string name,Transform parent,string value,int size,Color color,Vector2 anchor,Vector2 dimensions,Vector2 offset){var go=new GameObject(name,typeof(RectTransform),typeof(Text));go.transform.SetParent(parent,false);var r=(RectTransform)go.transform;r.anchorMin=r.anchorMax=anchor;r.sizeDelta=dimensions;r.anchoredPosition=offset;var t=go.GetComponent<Text>();t.font=font;t.fontSize=size;t.fontStyle=FontStyle.Normal;t.color=color;t.alignment=TextAnchor.MiddleCenter;t.text=value;t.raycastTarget=false;t.resizeTextForBestFit=true;t.resizeTextMinSize=Mathf.Max(18,size-6);t.resizeTextMaxSize=size;return t;}
    Button Btn(string name,Transform parent,string text,Vector2 anchor,Vector2 size,Vector2 offset,Color color,UnityEngine.Events.UnityAction action){var im=Panel(name,parent,anchor,size,color,offset);im.raycastTarget=true;var b=im.gameObject.AddComponent<Button>();b.targetGraphic=im;b.onClick.AddListener(action);Label("Label",im.transform,text,34,Color.white,new Vector2(.5f,.5f),size-Vector2.one*12,Vector2.zero);return b;}
    GameObject Layer(string name){var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(safe,false);var r=(RectTransform)go.transform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.sizeDelta=Vector2.zero;return go;}
    Color Ink=>new Color(.21f,.24f,.32f);Color Blue=>new Color(.16f,.55f,.85f);Color Green=>new Color(.20f,.69f,.46f);
    void WhiteOutline(Text t){t.color=Color.white;var outline=t.gameObject.AddComponent<Outline>();outline.effectColor=new Color(0,0,0,.65f);outline.effectDistance=new Vector2(2,-2);}
    void CreateUI(){
        var texture=new Texture2D(64,64,TextureFormat.RGBA32,false);for(int y=0;y<64;y++)for(int x=0;x<64;x++){float d=Vector2.Distance(new Vector2(x+.5f,y+.5f),new Vector2(32,32));texture.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(32-d)));}texture.Apply();circle=Sprite.Create(texture,new Rect(0,0,64,64),new Vector2(.5f,.5f),100);
        var rectTex=new Texture2D(32,32,TextureFormat.RGBA32,false);for(int y=0;y<32;y++)for(int x=0;x<32;x++){float dx=Mathf.Max(0,7-x,x-24),dy=Mathf.Max(0,7-y,y-24);rectTex.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(8-Mathf.Sqrt(dx*dx+dy*dy))));}rectTex.Apply();rounded=Sprite.Create(rectTex,new Rect(0,0,32,32),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(8,8,8,8));
        canvas=new GameObject("English Interface",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster)).GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvasRect=(RectTransform)canvas.transform;var scaler=canvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1080,1920);scaler.matchWidthOrHeight=0;
        safe=new GameObject("Safe Area",typeof(RectTransform)).GetComponent<RectTransform>();safe.SetParent(canvas.transform,false);Rect area=Screen.safeArea;safe.anchorMin=new Vector2(area.x/Screen.width,area.y/Screen.height);safe.anchorMax=new Vector2(area.xMax/Screen.width,area.yMax/Screen.height);safe.offsetMin=safe.offsetMax=Vector2.zero;
        var ev=new GameObject("EventSystem",typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
        ev.AddComponent<InputSystemUIInputModule>();
#else
        ev.AddComponent<StandaloneInputModule>();
#endif
        hud=Layer("Play");home=Layer("Home");result=Layer("Result");pause=Layer("Paused");settings=Layer("Settings");
        var timerBG=Panel("TimerBadge",hud.transform,new Vector2(.5f,1),new Vector2(230,84),Blue,new Vector2(0,-70));timer=Label("Time",timerBG.transform,"5:00",50,Color.white,new Vector2(.5f,.5f),new Vector2(220,80),Vector2.zero);
        Btn("Pause",hud.transform,"II",new Vector2(0,1),new Vector2(88,88),new Vector2(72,-70),Blue,Pause);
        levelLabel=Label("Level",hud.transform,"LEVEL 1",30,Ink,new Vector2(1,1),new Vector2(200,60),new Vector2(-125,-70));
        holeSize=Label("Hole Size",hud.transform,"Size 1",38,Color.white,new Vector2(.5f,.5f),new Vector2(400,70),Vector2.zero);var sizeOutline=holeSize.gameObject.AddComponent<Outline>();sizeOutline.effectColor=new Color(0,0,0,.8f);sizeOutline.effectDistance=new Vector2(2,-2);growthToast=Label("Growth Flash",hud.transform,"SIZE UP!",54,Color.white,new Vector2(.5f,.5f),new Vector2(650,90),Vector2.zero);growthToast.gameObject.AddComponent<Outline>().effectDistance=new Vector2(3,-3);
        WhiteOutline(levelLabel);
        Panel("GrowthChip",hud.transform,new Vector2(.5f,0),new Vector2(600,62),new Color(1,1,1,.88f),new Vector2(0,240));sizeLabel=Label("Growth",hud.transform,"",30,Ink,new Vector2(.5f,0),new Vector2(900,50),new Vector2(0,240));
        var progressBG=Panel("GrowthProgress",hud.transform,new Vector2(.5f,0),new Vector2(600,32),Color.white,new Vector2(0,192));progressBG.sprite=Style("progress-BG");progressBG.type=Image.Type.Simple;growthFill=Panel("Fill",progressBG.transform,new Vector2(.5f,.5f),new Vector2(580,22),Color.white,Vector2.zero);growthFill.sprite=Style("progress-green");growthFill.type=Image.Type.Filled;growthFill.fillMethod=Image.FillMethod.Horizontal;growthFill.fillOrigin=0;
        boostButtons=new Button[4];boostLabels=new Text[4];string[] boostNames={"SIZE","SPEED","FIND","FREEZE"};for(int i=0;i<4;i++){int n=i;var b=Btn("Booster"+i,hud.transform,boostNames[i],new Vector2(.5f,0),new Vector2(205,100),new Vector2((i-1.5f)*232,105),Blue,()=>Boost(n));boostButtons[i]=b;boostLabels[i]=b.GetComponentInChildren<Text>();}
        homeTitle=Label("Title",home.transform,"HOLE IT!",92,Ink,new Vector2(.5f,.84f),new Vector2(950,130),Vector2.zero);Label("ForGaeun",home.transform,"FOR GAEUN",32,Ink,new Vector2(.5f,.78f),new Vector2(800,60),Vector2.zero);
        homeLevel=Label("Level",home.transform,"LEVEL 1",42,Ink,new Vector2(.5f,.27f),new Vector2(500,70),Vector2.zero);
        WhiteOutline(homeTitle);WhiteOutline(homeLevel);WhiteOutline(home.transform.Find("ForGaeun").GetComponent<Text>());
        Btn("Previous",home.transform,"<",new Vector2(.2f,.27f),new Vector2(105,90),Vector2.zero,Blue,()=>{LoadLevel(Mathf.Max(0,LevelIndex-1));ShowHome();});
        Btn("Next",home.transform,">",new Vector2(.8f,.27f),new Vector2(105,90),Vector2.zero,Blue,()=>{LoadLevel(Mathf.Min(unlocked-1,LevelIndex+1));ShowHome();});
        Btn("Play",home.transform,"PLAY",new Vector2(.5f,.17f),new Vector2(680,150),Vector2.zero,Green,StartPlay);
        Btn("Settings",home.transform,"SETTINGS",new Vector2(.5f,.065f),new Vector2(410,82),Vector2.zero,Blue,()=>settings.SetActive(true));
        CreateModal(result,"Result");resultTitle=Label("Title",result.transform,"",64,Ink,new Vector2(.5f,.66f),new Vector2(800,120),Vector2.zero);resultDetail=Label("Detail",result.transform,"",32,Ink,new Vector2(.5f,.52f),new Vector2(780,260),Vector2.zero);
        Btn("Continue",result.transform,"CONTINUE",new Vector2(.5f,.36f),new Vector2(640,120),Vector2.zero,Green,()=>{if(State==ScreenState.Won){LoadLevel(Mathf.Min(29,LevelIndex+1));StartPlay();}else Retry();});
        Btn("Home",result.transform,"HOME",new Vector2(.5f,.27f),new Vector2(520,90),Vector2.zero,Blue,()=>{LoadLevel(LevelIndex);ShowHome();});
        CreateModal(pause,"Pause");Label("Title",pause.transform,"PAUSED",64,Ink,new Vector2(.5f,.66f),new Vector2(780,120),Vector2.zero);Btn("Resume",pause.transform,"RESUME",new Vector2(.5f,.52f),new Vector2(640,120),Vector2.zero,Green,Resume);Btn("Retry",pause.transform,"RESTART",new Vector2(.5f,.42f),new Vector2(540,100),Vector2.zero,Blue,Retry);Btn("Home",pause.transform,"HOME",new Vector2(.5f,.32f),new Vector2(540,100),Vector2.zero,Blue,()=>{LoadLevel(LevelIndex);ShowHome();});
        CreateModal(settings,"Settings");Label("Title",settings.transform,"SETTINGS",60,Ink,new Vector2(.5f,.66f),new Vector2(780,120),Vector2.zero);Label("Controls",settings.transform,"FLOATING JOYSTICK\n\nHold to move. Release to stop.\nPull farther for speed, up to the limit.",30,Ink,new Vector2(.5f,.5f),new Vector2(770,300),Vector2.zero);Btn("Done",settings.transform,"DONE",new Vector2(.5f,.32f),new Vector2(540,100),Vector2.zero,Green,()=>settings.SetActive(false));
        var pad=Panel("Joystick",canvas.transform,new Vector2(.5f,.5f),new Vector2(200,200),new Color(1,1,1,.24f),Vector2.zero);pad.sprite=Style("JoystickOutline")??circle;pad.type=Image.Type.Simple;stickBase=pad.rectTransform;var knob=Panel("Knob",pad.transform,new Vector2(.5f,.5f),new Vector2(82,82),new Color(1,1,1,.65f),Vector2.zero);knob.sprite=Style("JoystickMarker")??circle;knob.type=Image.Type.Simple;stickKnob=knob.rectTransform;stickBase.gameObject.SetActive(false);
    }
    void CreateModal(GameObject panel,string name){var shade=Panel("Shade",panel.transform,new Vector2(.5f,.5f),new Vector2(4000,4000),new Color(.12f,.17f,.25f,.55f),Vector2.zero);shade.raycastTarget=true;Panel("Card",panel.transform,new Vector2(.5f,.5f),new Vector2(940,1070),new Color(.97f,.97f,.95f),Vector2.zero);}
    void BuildGoals(){if(goalLabels!=null)foreach(var t in goalLabels)if(t!=null)Destroy(t.transform.parent.gameObject);int count=Mathf.Max(1,level.goals.Length);goalLabels=new Text[count];goalIcons=new Image[count];float width=Mathf.Min(164,990f/count-14);
        for(int i=0;i<count;i++){var card=Panel("Goal"+i,hud.transform,new Vector2(.5f,1),new Vector2(width,208),Color.white,new Vector2((i-(count-1)*.5f)*(width+14),-234));
            var icon=Panel("Icon",card.transform,new Vector2(.5f,.64f),new Vector2(width*.72f,115),Color.white,Vector2.zero);icon.type=Image.Type.Simple;icon.preserveAspect=true;icon.sprite=level.goals.Length==0?null:models[level.goals[i].id].icon;icon.enabled=icon.sprite!=null;goalIcons[i]=icon;
            goalLabels[i]=Label("Count",card.transform,"",38,Color.white,new Vector2(.5f,.24f),new Vector2(width*.9f,62),Vector2.zero);if(level.goals.Length==0){goalLabels[i].rectTransform.anchorMin=goalLabels[i].rectTransform.anchorMax=new Vector2(.5f,.5f);goalLabels[i].rectTransform.sizeDelta=new Vector2(width,110);}
        }
    }
    void UpdateHUD(){if(timer==null)return;int seconds=Mathf.CeilToInt(Remaining);timer.text=seconds/60+":"+(seconds%60).ToString("00");timer.color=seconds<=15?new Color(1,.8f,.5f):Color.white;levelLabel.text="LEVEL "+(LevelIndex+1);
        for(int i=0;i<goalCounts.Length;i++)goalLabels[i].text=goalCounts[i]>=level.goals[i].amount?"DONE":(level.goals[i].amount-goalCounts[i]).ToString();if(goalCounts.Length==0)goalLabels[0].text="COLLECT ALL\n"+Collected+" / "+level.points.Length;
        float needed=GrowthCost(GrowthTier(GrowthPoints))-GrowthRemainder(GrowthPoints);growthFill.fillAmount=GrowthRemainder(GrowthPoints)/GrowthCost(GrowthTier(GrowthPoints));
        sizeLabel.text=(playTime<freezeUntil?"CLOCK FROZEN  ·  ":"")+Mathf.CeilToInt(needed)+" POINTS TO NEXT SIZE";
        if(playTime<finderUntil){Actor best=null;float distance=float.MaxValue;foreach(var a in actors){if(a.state!=0||!Needed(a.model.id))continue;float d=(a.pos-HolePosition).sqrMagnitude;if(d<distance){best=a;distance=d;}}if(best!=null){Vector3 d=best.pos-HolePosition;sizeLabel.text="TARGET "+(Mathf.Abs(d.x)>Mathf.Abs(d.z)?d.x>0?"RIGHT":"LEFT":d.z>0?"AHEAD":"BEHIND");}}
    }
    bool Needed(int id){if(id==-1)return false;if(level.goals.Length==0)return true;for(int i=0;i<goalCounts.Length;i++)if(level.goals[i].id==id&&goalCounts[i]<level.goals[i].amount)return true;return false;}
    void RefreshBoosters(){int[] activation={3,5,9,12};string[] names={"SIZE","SPEED","FIND","FREEZE"};for(int i=0;i<4;i++){boostButtons[i].interactable=LevelIndex+1>=activation[i]&&!boostUsed[i];boostLabels[i].text=LevelIndex+1<activation[i]?names[i]+"\nLV "+activation[i]:boostUsed[i]?"USED":names[i];}}
    void UpdateHoleLabels(){if(holeSize==null)return;Vector3 screen=cam.WorldToScreenPoint(HolePosition+Vector3.back*(VisualRadius+1));RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)hud.transform,screen,canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,out var point);holeSize.rectTransform.anchoredPosition=point;holeSize.text="Size "+(GrowthTier(GrowthPoints)+1);growthToast.gameObject.SetActive(State==ScreenState.Playing&&Time.unscaledTime<growthFlash);growthToast.rectTransform.anchoredPosition=point+new Vector2(0,240+(growthFlash-Time.unscaledTime)*30);}
    void UpdateStickVisual(){stickBase.gameObject.SetActive(State==ScreenState.Playing&&Stick.Held);if(!Stick.Held)return;RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect,Stick.Origin,canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,out var local);stickBase.anchoredPosition=local;float diameter=Stick.Radius*2/canvas.scaleFactor;stickBase.sizeDelta=Vector2.one*diameter;stickKnob.sizeDelta=Vector2.one*diameter*.4f;stickKnob.anchoredPosition=Stick.Value*diameter*.32f;}
    void ShowResult(bool won,string title){hud.SetActive(false);result.SetActive(true);resultTitle.text=title;string detail="";for(int i=0;i<goalCounts.Length;i++)detail+=models[level.goals[i].id].label+"  "+goalCounts[i]+" / "+level.goals[i].amount+"\n";if(goalCounts.Length==0)detail="COLLECTED  "+Collected+" / "+level.points.Length;resultDetail.text=detail;result.transform.Find("Continue/Label").GetComponent<Text>().text=won?"NEXT LEVEL":"TRY AGAIN";}
    // Bounded hooks for desktop verification: still use the real mouth/collection path.
    public Vector3 NearestCollectible(){testBombs.Clear();foreach(var a in actors)if(a.model.id==-1&&a.state==0)testBombs.Add(a);Vector3 best=HolePosition;float score=float.MaxValue;foreach(var a in actors){if(a.state!=0||a.model.id==-1||a.radius>Radius*1.02f)continue;Vector3 aim=new Vector3(a.pos.x,0,a.pos.z);
        foreach(var bomb in testBombs){Vector3 away=aim-bomb.pos;away.y=0;float safe=Radius-bomb.radius*.45f+.3f;if(away.magnitude<safe)aim+=away.normalized*(safe-away.magnitude);}
        aim.x=Mathf.Clamp(aim.x,min.x+Radius,max.x-Radius);aim.z=Mathf.Clamp(aim.z,min.y+Radius,max.y-Radius);
        Vector3 offset=aim-a.pos;offset.y=0;if(offset.magnitude>Mathf.Max(.12f,Radius-a.radius*.45f)-.01f)continue;
        float d=(aim-HolePosition).sqrMagnitude*(Needed(a.model.id)?1:1.3f);if(a.Bottom>.3f)d+=100;if(d<score){score=d;best=aim;}}return best;}
    public string RemainingDiagnostic(){var b=new System.Text.StringBuilder();foreach(var a in actors)if(a.state!=2&&a.model.id!=-1)b.Append(a.model.id+":"+a.model.label+" radius="+a.radius+" bottom="+a.Bottom+" position="+a.pos+"; ");return b.ToString();}
    public void MoveHoleForTest(Vector3 p){HolePosition=p;}
    public void TriggerBombForTest(){foreach(var a in actors)if(a.model.id==-1){a.pos=HolePosition;a.state=1;a.dropY=0;dropping.Add(actors.IndexOf(a));return;}throw new InvalidOperationException("No bombs on this level");}
}
