using UnityEngine;
/// <summary>Central state and reward authority. A session token invalidates stale pooled work.</summary>
public class GameManager : MonoBehaviour
{
    [SerializeField] private GameSettings settings;
    [SerializeField] private LevelManager levelManager;
    [SerializeField] private MissionManager missionManager;
    [SerializeField] private HoleController hole;
    [SerializeField] private CameraFollow cameraFollow;
    [SerializeField] private UIManager ui;
    [SerializeField] private HighScoreManager highScores;
    [SerializeField] private InputManager input;
    [SerializeField] private GameObject swallowFx,growthFx,missionFx,clearFx;
    public static GameManager Instance{get;private set;}
    public GameState State{get;private set;}=GameState.Boot;
    public int SessionId{get;private set;}
    public int TotalScore{get{return NumberGuard.Score((long)levelScore+missionBonusAcc);}}
    public float TimeRemaining{get{return timeRemaining;}}
    public int SelectedLevel{get{return selectedLevel+1;}}
    public int UnlockedLevelCount{get{return highScores!=null?Mathf.Min(highScores.MaxUnlockedLevel,levelManager.LevelCount):1;}}
    private int selectedLevel,levelScore,missionBonusAcc,lastWarnSecond=-1;
    private float timeRemaining,stateTimer;
    private bool unlimited,subscribed,ready;
    private void Awake(){Instance=this;Layers.Init();settings=GameSettings.Resolve(settings);}
    private void Start(){
        Application.targetFrameRate=60;QualitySettings.vSyncCount=0;Screen.sleepTimeout=SleepTimeout.NeverSleep;
        if(levelManager==null||missionManager==null||hole==null||cameraFollow==null||ui==null||highScores==null||input==null||ObjectPooler.Instance==null||hole.SizeTable==null||!hole.SizeTable.IsValid()){
            Debug.LogError("[HoleIt] Required serialized scene references or growth table are invalid. Run HoleIt > Validate Project.");enabled=false;return;
        }
        ui.OnPlay+=HandlePlay;ui.OnNext+=HandleNext;ui.OnRetry+=HandleRetry;ui.OnHome+=HandleHome;ui.OnPause+=Pause;ui.OnResume+=Resume;ui.OnLevelStep+=HandleLevelStep;
        ui.Missions.Bind(missionManager);missionManager.OnMissionCompleted+=HandleMissionCompleted;missionManager.OnAllCompleted+=HandleAllCompleted;hole.OnTierUp+=HandleTierUp;hole.OnGrowth+=HandleGrowth;subscribed=true;
        ObjectPooler.Instance.Prewarm(swallowFx,24);ObjectPooler.Instance.Prewarm(growthFx,6);ObjectPooler.Instance.Prewarm(missionFx,6);ObjectPooler.Instance.Prewarm(clearFx,3);
        selectedLevel=Mathf.Clamp(highScores.MaxUnlockedLevel-1,0,levelManager.LevelCount-1);ready=true;EnterTitle();
    }
    private void OnDestroy(){
        if(Instance==this)Instance=null;Time.timeScale=1f;
        if(!subscribed)return;
        if(ui!=null){ui.OnPlay-=HandlePlay;ui.OnNext-=HandleNext;ui.OnRetry-=HandleRetry;ui.OnHome-=HandleHome;ui.OnPause-=Pause;ui.OnResume-=Resume;ui.OnLevelStep-=HandleLevelStep;}
        if(missionManager!=null){missionManager.OnMissionCompleted-=HandleMissionCompleted;missionManager.OnAllCompleted-=HandleAllCompleted;}
        if(hole!=null){hole.OnTierUp-=HandleTierUp;hole.OnGrowth-=HandleGrowth;}
    }
    private void BeginLoading(){State=GameState.Loading;SessionId++;Time.timeScale=1;hole.SetControlEnabled(false);input.SetInputEnabled(false);if(AudioManager.Instance!=null)AudioManager.Instance.StopMovement();}
    private bool LoadSelected(){
        if(!levelManager.Load(selectedLevel)){State=GameState.Failed;ui.ShowFail("LEVEL DATA ERROR",0,highScores,false);return false;}
        cameraFollow.ResetForLevel(levelManager.Current);highScores.SelectLevel(levelManager.Current.levelId);return true;
    }
    private void EnterTitle(){BeginLoading();if(!LoadSelected())return;State=GameState.Title;ui.ShowTitle(highScores.BestScore,selectedLevel+1,Mathf.Min(highScores.MaxUnlockedLevel,levelManager.LevelCount));}
    private void StartLevel(int index){
        if(index<0||index>=levelManager.LevelCount||index>=highScores.MaxUnlockedLevel)return;
        BeginLoading();selectedLevel=index;if(!LoadSelected())return;
        LevelData level=levelManager.Current;levelScore=0;missionBonusAcc=0;unlimited=level.timeLimit==0;timeRemaining=level.timeLimit;lastWarnSecond=-1;
        State=GameState.Ready;stateTimer=Mathf.Max(0.1f,settings.readyDelay);
        ui.ShowReady(level.levelTitle);ui.SetScore(0);ui.SetSize(hole.Tier,hole.SizeTable.GetProgressToNext(hole.Growth),false);ui.SetTimer(timeRemaining,unlimited,false);
        if(AudioManager.Instance!=null)AudioManager.Instance.PlayLevelStart();
    }
    private void Update(){
        if(!ready)return;float dt=Time.deltaTime;
        if(State==GameState.Ready){stateTimer-=dt;if(stateTimer<=0){State=GameState.Playing;hole.SetControlEnabled(true);input.SetInputEnabled(true);ui.ShowPlaying();}}
        else if(State==GameState.Playing){
            if(AudioManager.Instance!=null)AudioManager.Instance.SetMoveIntensity(hole.Velocity.magnitude/Mathf.Max(0.1f,hole.CurrentMaxSpeed()));
            if(!unlimited){timeRemaining=Mathf.Max(0,timeRemaining-dt);UpdateTimer();if(timeRemaining<=0)FailLevel("TIME UP");}
        }else if(State==GameState.Clearing){stateTimer-=dt;if(stateTimer<=0)FinishLevel();}
    }
    private void UpdateTimer(){
        bool warning=!unlimited && timeRemaining<=settings.lowTimeWarning;ui.SetTimer(timeRemaining,unlimited,warning);
        if(!warning){lastWarnSecond=-1;return;}
        int s=Mathf.CeilToInt(timeRemaining);if(s>0&&s!=lastWarnSecond){lastWarnSecond=s;if(AudioManager.Instance!=null)AudioManager.Instance.PlayTimeWarning();}
    }
    public void OnEdibleConsumed(EdibleObject e){
        if(State!=GameState.Playing||e==null||!e.TryClaimReward(SessionId))return;
        EdibleData d=e.Data;LevelData level=levelManager.Current;bool target=missionManager.IsTarget(d);
        bool penalty=!target && ResolveBehavior(d)==WrongTargetBehavior.Penalty;
        levelScore=NumberGuard.Score((long)levelScore+Mathf.Max(0,d.score)-(penalty?Mathf.Max(0,level.penaltyScore):0));
        hole.AddGrowth(d.growthValue);
        if(d.objectType==ObjectType.TimeBoost && !unlimited){timeRemaining=Mathf.Min(3600,timeRemaining+Mathf.Max(0,d.specialTimeBonus));if(AudioManager.Instance!=null)AudioManager.Instance.PlayBooster();}
        if(penalty&&!unlimited)timeRemaining=Mathf.Max(0,timeRemaining-Mathf.Max(0,level.penaltySeconds));
        PooledFx.Spawn(swallowFx,hole.Position+Vector3.up*0.3f,Mathf.Max(0.5f,hole.Radius*0.6f));
        if(AudioManager.Instance!=null)AudioManager.Instance.PlaySwallow();
        ui.ShowScorePopup(Mathf.Max(0,d.score)-(penalty?Mathf.Max(0,level.penaltyScore):0));ui.SetScore(TotalScore);UpdateTimer();
        if(!unlimited && timeRemaining<=0){FailLevel("TIME UP");return;}
        if(ResolveBehavior(d)==WrongTargetBehavior.Blocked)missionManager.ReleaseReservation(d);
        missionManager.Register(d); // All-completed may transition state. Nothing awards afterwards.
    }
    private WrongTargetBehavior ResolveBehavior(EdibleData d){return d.overrideWrongBehavior?d.wrongBehavior:levelManager.Current.defaultWrongBehavior;}
    public bool IsBlocked(EdibleData d){if(d==null||levelManager.Current==null)return true;return ResolveBehavior(d)==WrongTargetBehavior.Blocked&&!missionManager.CanReserveTarget(d);}
    public void CancelSwallow(EdibleData d){if(d!=null && missionManager!=null && levelManager!=null && levelManager.Current!=null && ResolveBehavior(d)==WrongTargetBehavior.Blocked)missionManager.ReleaseReservation(d);}
    public bool ReserveSwallow(EdibleData d){return ResolveBehavior(d)!=WrongTargetBehavior.Blocked||missionManager.ReserveTarget(d);}
    private void HandleMissionCompleted(int index){if(State!=GameState.Playing)return;missionBonusAcc=NumberGuard.Score((long)missionBonusAcc+Mathf.Max(0,levelManager.Current.missionBonus));ui.SetScore(TotalScore);PooledFx.Spawn(missionFx,hole.Position+Vector3.up*0.3f,hole.Radius);if(AudioManager.Instance!=null)AudioManager.Instance.PlayMissionComplete();}
    private void HandleAllCompleted(){if(State!=GameState.Playing)return;State=GameState.Clearing;stateTimer=Mathf.Clamp(settings.clearDelay,0.3f,0.8f);StopControl();ui.ShowMissionComplete();}
    private void HandleTierUp(int tier){ui.SetSize(tier,hole.SizeTable.GetProgressToNext(hole.Growth),true);PooledFx.Spawn(growthFx,hole.Position+Vector3.up*0.1f,hole.Radius);if(AudioManager.Instance!=null)AudioManager.Instance.PlayGrowth();}
    private void HandleGrowth(float progress){ui.SetSize(hole.Tier,progress,false);}
    private void StopControl(){hole.SetControlEnabled(false);input.SetInputEnabled(false);if(AudioManager.Instance!=null)AudioManager.Instance.StopMovement();}
    private void FinishLevel(){
        if(State!=GameState.Clearing)return;LevelData l=levelManager.Current;
        int timeBonus=unlimited?0:NumberGuard.Score((long)Mathf.FloorToInt(timeRemaining)*Mathf.Max(0,l.timeBonusPerSecond));
        int total=NumberGuard.Score((long)TotalScore+timeBonus+Mathf.Max(0,l.clearBonus));
        State=GameState.LevelClear;highScores.CompleteLevel(l.levelId,total);levelManager.StopSwallows();
        PooledFx.Spawn(clearFx,hole.Position+Vector3.up*0.3f,hole.Radius);if(AudioManager.Instance!=null)AudioManager.Instance.PlayLevelClear();
        ui.SetScore(total);ui.ShowLevelClear(levelScore,total,missionBonusAcc,timeBonus,l.clearBonus,selectedLevel==levelManager.LevelCount-1);ui.SetResultMissions(missionManager.DescribeProgress());
    }
    private void FailLevel(string reason){if(State!=GameState.Playing)return;State=GameState.Failed;StopControl();levelManager.StopSwallows();ui.ShowFail(reason,TotalScore,highScores,false);ui.SetResultMissions(missionManager.DescribeProgress());if(AudioManager.Instance!=null)AudioManager.Instance.PlayFail();}
    private void HandlePlay(){if(State==GameState.Title)StartLevel(selectedLevel);}
    private void HandleNext(){if(State==GameState.LevelClear)StartLevel(selectedLevel+1);}
    private void HandleRetry(){if(State==GameState.Failed||State==GameState.LevelClear||State==GameState.Paused)StartLevel(selectedLevel);}
    private void HandleHome(){if(State!=GameState.Loading&&State!=GameState.Boot)EnterTitle();}
    private void HandleLevelStep(int direction){if(State!=GameState.Title)return;selectedLevel=Mathf.Clamp(selectedLevel+direction,0,Mathf.Min(highScores.MaxUnlockedLevel,levelManager.LevelCount)-1);EnterTitle();}
    public void SelectLevel(int index){if(State!=GameState.Title||index<0||index>=levelManager.LevelCount||index>=highScores.MaxUnlockedLevel)return;selectedLevel=index;EnterTitle();}
    public void Pause(){if(State!=GameState.Playing)return;State=GameState.Paused;Time.timeScale=0;StopControl();ui.ShowPause(true);}
    public void Resume(){if(State!=GameState.Paused)return;Time.timeScale=1;State=GameState.Playing;hole.SetControlEnabled(true);input.SetInputEnabled(true);ui.ShowPause(false);}
    private void OnApplicationPause(bool paused){if(paused)Pause();}
    private void OnApplicationFocus(bool focus){if(!focus)Pause();}
}
