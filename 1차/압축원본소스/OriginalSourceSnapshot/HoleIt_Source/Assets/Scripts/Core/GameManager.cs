using UnityEngine;

/// <summary>
/// 게임 흐름 총괄: Title → Ready → Playing → Clearing → LevelClear → (Next) / Failed.
/// 세부 기능은 LevelManager / MissionManager / HoleController / UIManager / HighScoreManager 에 위임한다.
/// </summary>
public class GameManager : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private GameSettings settings;
    [SerializeField] private LevelManager levelManager;
    [SerializeField] private MissionManager missionManager;
    [SerializeField] private HoleController hole;
    [SerializeField] private CameraFollow cameraFollow;
    [SerializeField] private UIManager ui;
    [SerializeField] private HighScoreManager highScores;
    [SerializeField] private InputManager input;

    [Header("FX 프리팹 (풀링)")]
    [SerializeField] private GameObject swallowFx;
    [SerializeField] private GameObject growthFx;
    [SerializeField] private GameObject missionFx;
    [SerializeField] private GameObject clearFx;

    public static GameManager Instance { get; private set; }
    public GameState State { get; private set; }
    public int TotalScore { get { return runScore + levelScore; } }

    private int selectedLevel;
    private int runScore;       // 이전 레벨까지 확정된 점수
    private int levelScore;     // 현재 레벨에서 획득한 오브젝트 점수
    private int missionBonusAcc;
    private float timeRemaining;
    private bool unlimited;
    private int lastWarnSecond = -1;
    private float stateTimer;
    private GameState stateBeforePause;
    private bool runSubmitted;
    private bool lastRunNewBest;

    private void Awake()
    {
        Instance = this;
        Layers.Init();

        // 씬 연결이 하나라도 비어 있어도 동작하도록 자동으로 찾는다
        settings = GameSettings.Resolve(settings);
        if (levelManager == null) levelManager = FindFirstObjectByType<LevelManager>();
        if (missionManager == null) missionManager = FindFirstObjectByType<MissionManager>();
        if (hole == null) hole = FindFirstObjectByType<HoleController>();
        if (cameraFollow == null) cameraFollow = FindFirstObjectByType<CameraFollow>();
        if (ui == null) ui = FindFirstObjectByType<UIManager>(FindObjectsInactive.Include);
        if (highScores == null) highScores = FindFirstObjectByType<HighScoreManager>();
        if (input == null) input = FindFirstObjectByType<InputManager>();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        Physics.gravity = new Vector3(0f, -settings.fallGravity, 0f);

        ui.OnPlay += HandlePlay;
        ui.OnNext += HandleNext;
        ui.OnRetry += HandleRetry;
        ui.OnHome += HandleHome;
        ui.OnPause += Pause;
        ui.OnResume += Resume;
        ui.OnLevelStep += HandleLevelStep;

        ui.Missions.Bind(missionManager);
        missionManager.OnMissionCompleted += HandleMissionCompleted;
        missionManager.OnAllCompleted += HandleAllCompleted;
        hole.OnTierUp += HandleTierUp;
        hole.OnGrowth += HandleGrowth;

        // FX는 처음 쓸 때 풀 확장 경고가 뜨지 않도록 미리 만들어 둔다
        ObjectPooler.Instance.Prewarm(swallowFx, 24);
        ObjectPooler.Instance.Prewarm(growthFx, 6);
        ObjectPooler.Instance.Prewarm(missionFx, 6);
        ObjectPooler.Instance.Prewarm(clearFx, 3);

        selectedLevel = Mathf.Clamp(highScores.MaxUnlockedLevel - 1, 0, levelManager.LevelCount - 1);
        EnterTitle();
    }

    // ───────────────────────── 상태 전환 ─────────────────────────

    private void EnterTitle()
    {
        Time.timeScale = 1f;
        State = GameState.Title;
        hole.SetControlEnabled(false);
        input.SetInputEnabled(false);
        levelManager.Load(selectedLevel);
        cameraFollow.ResetForLevel(levelManager.Current);
        ui.ShowTitle(highScores.BestScore, selectedLevel + 1, Mathf.Min(highScores.MaxUnlockedLevel, levelManager.LevelCount));
    }

    private void StartLevel(int index)
    {
        selectedLevel = Mathf.Clamp(index, 0, levelManager.LevelCount - 1);
        levelManager.Load(selectedLevel);
        LevelData level = levelManager.Current;
        cameraFollow.ResetForLevel(level);

        levelScore = 0;
        missionBonusAcc = 0;
        unlimited = level.timeLimit <= 0f;
        timeRemaining = level.timeLimit;
        lastWarnSecond = -1;

        Time.timeScale = 1f;
        State = GameState.Ready;
        stateTimer = settings.readyDelay;
        hole.SetControlEnabled(false);
        input.SetInputEnabled(false);

        ui.ShowReady(level.levelTitle);
        ui.SetScore(TotalScore);
        ui.SetSize(hole.Tier, hole.SizeTable.GetProgressToNext(hole.Growth), false);
        ui.SetTimer(timeRemaining, unlimited, false);
    }

    private void BeginPlaying()
    {
        State = GameState.Playing;
        hole.SetControlEnabled(true);
        input.SetInputEnabled(true);
        ui.ShowPlaying();
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        switch (State)
        {
            case GameState.Ready:
                stateTimer -= dt;
                if (stateTimer <= 0f) BeginPlaying();
                break;

            case GameState.Playing:
                UpdatePlaying(dt);
                break;

            case GameState.Clearing:
                stateTimer -= dt;
                if (stateTimer <= 0f) FinishLevel();
                break;
        }
    }

    private void UpdatePlaying(float dt)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.SetMoveIntensity(hole.Velocity.magnitude / Mathf.Max(0.1f, hole.CurrentMaxSpeed()));

        if (unlimited) return;

        timeRemaining -= dt;
        bool warning = timeRemaining <= settings.lowTimeWarning;
        ui.SetTimer(timeRemaining, false, warning);

        if (warning)
        {
            int sec = Mathf.CeilToInt(timeRemaining);
            if (sec != lastWarnSecond && sec > 0)
            {
                lastWarnSecond = sec;
                if (AudioManager.Instance != null) AudioManager.Instance.PlayTimeWarning();
            }
        }

        if (timeRemaining <= 0f) FailLevel("TIME UP");
    }

    // ───────────────────────── 소비 / 점수 / 미션 ─────────────────────────

    /// <summary>EdibleObject.Consume()에서 호출. 점수/성장/미션/연출을 처리한다.</summary>
    public void OnEdibleConsumed(EdibleObject e)
    {
        if (State != GameState.Playing && State != GameState.Clearing) return;
        EdibleData d = e.Data;
        LevelData level = levelManager.Current;
        bool isTarget = missionManager.IsTarget(d);
        Vector3 fxPos = hole.Position + Vector3.up * 0.3f;

        // Optional time pickup: adds useful seconds without being a required mission item.
        if (d.objectType == ObjectType.TimeBoost && !unlimited)
        {
            float bonus = Mathf.Max(1f, d.specialTimeBonus);
            timeRemaining += bonus;
            ui.SetTimer(timeRemaining, false, timeRemaining <= settings.lowTimeWarning);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayGrowth();
        }

        if (!isTarget && ResolveBehavior(d) == WrongTargetBehavior.Penalty)
        {
            levelScore = Mathf.Max(0, levelScore - level.penaltyScore);
            if (!unlimited) timeRemaining = Mathf.Max(0.1f, timeRemaining - level.penaltySeconds);
            ui.SetScore(TotalScore);
            if (AudioManager.Instance != null) AudioManager.Instance.PlayTimeWarning();
            return;
        }

        levelScore += d.score;
        ui.SetScore(TotalScore);
        hole.AddGrowth(d.growthValue);
        missionManager.Register(d);

        PooledFx.Spawn(swallowFx, fxPos, Mathf.Max(0.5f, hole.Radius * 0.6f));
        if (AudioManager.Instance != null) AudioManager.Instance.PlaySwallow();
    }

    private WrongTargetBehavior ResolveBehavior(EdibleData d)
    {
        if (d == null) return WrongTargetBehavior.Blocked;
        if (d.overrideWrongBehavior) return d.wrongBehavior;
        if (levelManager == null || levelManager.Current == null) return WrongTargetBehavior.Blocked;
        return levelManager.Current.defaultWrongBehavior;
    }

    /// <summary>미션 대상이 아닌 오브젝트가 Blocked 설정이면 Hole이 삼킬 수 없다.</summary>
    public bool IsBlocked(EdibleData d)
    {
        if (d == null) return true;
        // Mission objects are always valid targets, even when another mission is completed.
        if (missionManager != null && missionManager.IsTarget(d)) return false;
        // Time pickups are deliberately optional objectives and must remain collectable.
        if (d.objectType == ObjectType.TimeBoost) return false;
        if (ResolveBehavior(d) != WrongTargetBehavior.Blocked) return false;

        // Hole it-style progression: while a larger required target is still inaccessible,
        // smaller items may be swallowed as growth fodder. Once the hole reaches the highest
        // unfinished target tier, non-target objects are blocked to preserve puzzle intent.
        LevelData level = levelManager != null ? levelManager.Current : null;
        if (level == null || level.missions == null || hole == null || missionManager == null) return true;
        int highestUnfinishedTier = 1;
        for (int i = 0; i < missionManager.MissionCount; i++)
        {
            if (missionManager.IsDone(i)) continue;
            MissionData m = missionManager.GetMission(i);
            if (m.target != null) highestUnfinishedTier = Mathf.Max(highestUnfinishedTier, m.target.sizeTier);
        }
        if (hole.Tier < highestUnfinishedTier && d.sizeTier < highestUnfinishedTier && d.sizeTier <= hole.Tier)
            return false;
        return true;
    }

    private void HandleMissionCompleted(int index)
    {
        missionBonusAcc += levelManager.Current.missionBonus;
        PooledFx.Spawn(missionFx, hole.Position + Vector3.up * 0.5f, Mathf.Max(0.6f, hole.Radius * 0.7f));
        if (AudioManager.Instance != null) AudioManager.Instance.PlayMissionComplete();
    }

    private void HandleAllCompleted()
    {
        if (State != GameState.Playing) return;
        State = GameState.Clearing;
        stateTimer = settings.clearDelay;   // 마지막 오브젝트 직후 0.3~0.8초 여유
        hole.SetControlEnabled(false);
        input.SetInputEnabled(false);
        ui.ShowMissionComplete();
    }

    private void HandleTierUp(int tier)
    {
        ui.SetSize(tier, hole.SizeTable.GetProgressToNext(hole.Growth), true);
        PooledFx.Spawn(growthFx, hole.Position + Vector3.up * 0.2f, Mathf.Max(0.8f, hole.Radius));
        if (AudioManager.Instance != null) AudioManager.Instance.PlayGrowth();
    }

    private void HandleGrowth(float progress)
    {
        ui.SetSize(hole.Tier, progress, false);
    }

    // ───────────────────────── 클리어 / 실패 ─────────────────────────

    private void FinishLevel()
    {
        LevelData level = levelManager.Current;
        int timeBonus = unlimited ? 0 : Mathf.FloorToInt(Mathf.Max(0f, timeRemaining)) * level.timeBonusPerSecond;
        int objects = levelScore;
        runScore += levelScore + missionBonusAcc + timeBonus + level.clearBonus;
        levelScore = 0;

        highScores.UnlockLevel(selectedLevel + 2);
        State = GameState.LevelClear;
        bool last = selectedLevel >= levelManager.LevelCount - 1;

        PooledFx.Spawn(clearFx, hole.Position + Vector3.up * 0.5f, Mathf.Max(1f, hole.Radius));
        if (AudioManager.Instance != null) { AudioManager.Instance.PlayLevelClear(); AudioManager.Instance.SetMoveIntensity(0f); }
        ui.SetScore(runScore);

        if (last)
        {
            // 마지막 레벨: 기록을 확정하고 결과(TOP 3) 화면으로
            SubmitRun();
            ui.ShowFail("ALL CLEAR!", runScore, highScores, lastRunNewBest);
        }
        else
        {
            ui.ShowLevelClear(objects, runScore, missionBonusAcc, timeBonus, level.clearBonus, false);
        }
    }

    private void FailLevel(string reason)
    {
        if (State == GameState.Failed) return;
        State = GameState.Failed;
        hole.SetControlEnabled(false);
        input.SetInputEnabled(false);
        runScore += levelScore + missionBonusAcc;
        levelScore = 0;
        SubmitRun();
        if (AudioManager.Instance != null) { AudioManager.Instance.PlayFail(); AudioManager.Instance.SetMoveIntensity(0f); }
        ui.ShowFail(reason, runScore, highScores, lastRunNewBest);
    }

    private void SubmitRun()
    {
        if (runSubmitted) return;
        runSubmitted = true;
        lastRunNewBest = highScores.Submit(runScore);
    }

    // ───────────────────────── 버튼 핸들러 ─────────────────────────

    private void HandlePlay()
    {
        runScore = 0;
        runSubmitted = false;
        lastRunNewBest = false;
        StartLevel(selectedLevel);
    }

    private void HandleNext()
    {
        StartLevel(selectedLevel + 1);
    }

    private void HandleRetry()
    {
        // 결과 화면에서의 재도전: 새 런으로 시작 (전체 클리어였다면 1레벨부터)
        int idx = selectedLevel >= levelManager.LevelCount - 1 && runSubmitted && State == GameState.LevelClear ? 0 : selectedLevel;
        runScore = 0;
        runSubmitted = false;
        lastRunNewBest = false;
        StartLevel(idx);
    }

    private void HandleHome()
    {
        // 진행 중이던 런 점수를 확정하고 타이틀로
        if (!runSubmitted)
        {
            if (State == GameState.Paused || State == GameState.Playing || State == GameState.Clearing)
                runScore += levelScore + missionBonusAcc;
            SubmitRun();
        }
        levelScore = 0;
        EnterTitle();
    }

    private void HandleLevelStep(int dir)
    {
        int max = Mathf.Min(highScores.MaxUnlockedLevel, levelManager.LevelCount) - 1;
        selectedLevel = Mathf.Clamp(selectedLevel + dir, 0, max);
        levelManager.Load(selectedLevel);
        cameraFollow.ResetForLevel(levelManager.Current);
        ui.SetTitleLevel(selectedLevel + 1, max + 1);
    }

    // ───────────────────────── Pause ─────────────────────────

    public void Pause()
    {
        if (State != GameState.Playing) return;
        stateBeforePause = State;
        State = GameState.Paused;
        Time.timeScale = 0f;
        input.SetInputEnabled(false);
        if (AudioManager.Instance != null) AudioManager.Instance.SetMoveIntensity(0f);
        ui.ShowPause(true);
    }

    public void Resume()
    {
        if (State != GameState.Paused) return;
        State = stateBeforePause;
        Time.timeScale = 1f;
        input.SetInputEnabled(true);
        ui.ShowPause(false);
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused) Pause();
    }

    private void OnApplicationFocus(bool focus)
    {
        if (!focus) Pause();
    }
}
