using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 모든 화면(Title / HUD / Ready / Clear / Fail / Pause)의 표시 담당. 로직은 GameManager가 가진다.
/// 텍스트는 이벤트/초 단위로 갱신한다. string.Format 할당은 있으므로 실제 GC는 Profiler로 확인한다.
/// UI 문구는 Unity 내장 폰트를 사용하는 영어이다. 한국어 현지화는 별도 글리프 지원이 필요하다.
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject titlePanel;
    [SerializeField] private GameObject hudPanel;
    [SerializeField] private GameObject readyBanner;
    [SerializeField] private GameObject missionCompleteBanner;
    [SerializeField] private GameObject clearPanel;
    [SerializeField] private GameObject failPanel;
    [SerializeField] private GameObject pausePanel;

    [Header("Title")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button levelPrevButton;
    [SerializeField] private Button levelNextButton;
    [SerializeField] private HoleItText titleLevelText;
    [SerializeField] private HoleItText titleBestText;

    [Header("HUD")]
    [SerializeField] private HoleItText levelText;
    [SerializeField] private HoleItText timerText;
    [SerializeField] private HoleItText scoreText;
    [SerializeField] private HoleItText sizeText;
    [SerializeField] private Image sizeFill;
    [SerializeField] private Button pauseButton;
    [SerializeField] private MissionUI missionUI;
    [SerializeField] private UIPunch sizePunch;
    [SerializeField] private UIPunch scorePunch;
    [SerializeField] private Color timerNormal = Color.white;
    [SerializeField] private Color timerWarning = new Color(1f, 0.35f, 0.3f);

    [Header("Ready")]
    [SerializeField] private HoleItText readyLevelText;

    [Header("Clear")]
    [SerializeField] private HoleItText clearScoreText;
    [SerializeField] private HoleItText clearBreakdownText;
    [SerializeField] private Button clearNextButton;
    [SerializeField] private Button clearHomeButton;

    [Header("Fail / Result")]
    [SerializeField] private HoleItText failTitleText;
    [SerializeField] private HoleItText failScoreText;
    [SerializeField] private HoleItText[] topScoreTexts;
    [SerializeField] private GameObject newBestObject;
    [SerializeField] private Button failRetryButton;
    [SerializeField] private Button failHomeButton;

    [Header("Pause")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button pauseHomeButton;


    [Header("Additional screens")]
    [SerializeField] private GameObject settingsPanel, levelSelectPanel;
    [SerializeField] private Button settingsButton,settingsCloseButton,levelSelectButton,levelSelectCloseButton;
    [SerializeField] private Button controlModeButton,muteButton,pauseRestartButton,clearRetryButton,pauseSettingsButton;
    [SerializeField] private HoleItText controlModeText,muteText,clearMissionsText,failMissionsText;
    [SerializeField] private Slider masterSlider,musicSlider,sfxSlider;
    [SerializeField] private Button[] levelButtons;
    [SerializeField] private HoleItText[] popupTexts;
    private float[] popupTimes;
    private float lastClick=-100f;
    public event System.Action OnPlay;
    public event System.Action OnNext;
    public event System.Action OnRetry;
    public event System.Action OnHome;
    public event System.Action OnPause;
    public event System.Action OnResume;
    public event System.Action<int> OnLevelStep;

    private int lastTimerSecond = -1;
    private int lastScore = -1;
    private bool timerWarn;

    public MissionUI Missions { get { return missionUI; } }

    private void Awake()
    {
        Hook(playButton, HandlePlay);
        Hook(levelPrevButton, HandlePrev);
        Hook(levelNextButton, HandleNextLevelSelect);
        Hook(pauseButton, HandlePause);
        Hook(resumeButton, HandleResume);
        Hook(pauseHomeButton, HandleHome);
        Hook(clearNextButton, HandleNext);
        Hook(clearHomeButton, HandleHome);
        Hook(failRetryButton, HandleRetry);
        Hook(failHomeButton, HandleHome);
        Hook(pauseRestartButton, HandleRetry); Hook(clearRetryButton, HandleRetry);
        Hook(settingsButton, ShowSettings); Hook(pauseSettingsButton,ShowSettings);
        Hook(settingsCloseButton,CloseSettings);Hook(levelSelectButton,ShowLevelSelect);Hook(levelSelectCloseButton,CloseLevelSelect);
        Hook(controlModeButton,ToggleControls);Hook(muteButton,ToggleMute);
        if(masterSlider!=null){masterSlider.SetValueWithoutNotify(ReadVolume("Master",0.85f));masterSlider.onValueChanged.AddListener(SetMaster);}
        if(musicSlider!=null){musicSlider.SetValueWithoutNotify(ReadVolume("Music",0.3f));musicSlider.onValueChanged.AddListener(SetMusic);}
        if(sfxSlider!=null){sfxSlider.SetValueWithoutNotify(ReadVolume("SFX",0.8f));sfxSlider.onValueChanged.AddListener(SetSfx);}
        if(levelButtons!=null)for(int i=0;i<levelButtons.Length;i++){int index=i;if(levelButtons[i]!=null)levelButtons[i].onClick.AddListener(()=>SelectLevel(index));}
        popupTimes=new float[popupTexts!=null?popupTexts.Length:0];
        for(int i=0;i<popupTimes.Length;i++)if(popupTexts[i]!=null)popupTexts[i].gameObject.SetActive(false);
    }

    private static void Hook(Button b, UnityEngine.Events.UnityAction a)
    {
        if (b != null) b.onClick.AddListener(a);
    }

    private void Click() { if (AudioManager.Instance != null) AudioManager.Instance.PlayButton(); }
    private void HandlePlay() { if(!AcceptClick())return; Click(); if (OnPlay != null) OnPlay(); }
    private void HandlePrev() { if(!AcceptClick())return; Click(); if (OnLevelStep != null) OnLevelStep(-1); }
    private void HandleNextLevelSelect() { if(!AcceptClick())return; Click(); if (OnLevelStep != null) OnLevelStep(1); }
    private void HandlePause() { if(!AcceptClick())return; Click(); if (OnPause != null) OnPause(); }
    private void HandleResume() { if(!AcceptClick())return; Click(); if (OnResume != null) OnResume(); }
    private void HandleHome() { if(!AcceptClick())return; Click(); if (OnHome != null) OnHome(); }
    private void HandleNext() { if(!AcceptClick())return; Click(); if (OnNext != null) OnNext(); }
    private void HandleRetry() { if(!AcceptClick())return; Click(); if (OnRetry != null) OnRetry(); }

    // ───────── 화면 전환 ─────────

    private void HideAll()
    {
        titlePanel.SetActive(false);
        hudPanel.SetActive(false);
        readyBanner.SetActive(false);
        missionCompleteBanner.SetActive(false);
        clearPanel.SetActive(false);
        failPanel.SetActive(false);
        pausePanel.SetActive(false);
        if(settingsPanel!=null)settingsPanel.SetActive(false);
        if(levelSelectPanel!=null)levelSelectPanel.SetActive(false);
        if(popupTimes!=null)for(int i=0;i<popupTimes.Length;i++){popupTimes[i]=0;if(popupTexts[i]!=null)popupTexts[i].gameObject.SetActive(false);}
    }

    public void ShowTitle(int bestScore, int levelNumber, int unlockedLevel)
    {
        HideAll();
        titlePanel.SetActive(true);
        SetTitleLevel(levelNumber, unlockedLevel);
        if (titleBestText != null) titleBestText.SetText("BEST SCORE  {0}", bestScore);
    }

    public void SetTitleLevel(int levelNumber, int unlockedLevel)
    {
        if (titleLevelText != null) titleLevelText.SetText("LEVEL {0}", levelNumber);
        if (levelPrevButton != null) levelPrevButton.interactable = levelNumber > 1;
        if (levelNextButton != null) levelNextButton.interactable = levelNumber < unlockedLevel;
    }

    public void ShowReady(string title)
    {
        HideAll();
        hudPanel.SetActive(true);
        readyBanner.SetActive(true);
        if (readyLevelText != null) readyLevelText.SetText(title);
        if (levelText != null) levelText.SetText(title);
        missionUI.Refresh();
        lastTimerSecond = -1;
        lastScore = -1;
        SetTimerColor(false);
    }

    public void ShowPlaying()
    {
        readyBanner.SetActive(false);
        missionCompleteBanner.SetActive(false);
        hudPanel.SetActive(true);
    }

    public void ShowMissionComplete()
    {
        missionCompleteBanner.SetActive(true);
        UIPunch p = missionCompleteBanner.GetComponent<UIPunch>();
        if (p != null) p.Play(1.2f);
    }

    public void ShowLevelClear(int levelScore, int totalScore, int missionBonus, int timeBonus, int clearBonus, bool lastLevel)
    {
        HideAll();
        clearPanel.SetActive(true);
        if (clearScoreText != null) clearScoreText.SetText("SCORE  {0}", totalScore);
        if (clearBreakdownText != null)
            clearBreakdownText.SetText("Objects  +{0}\nMission  +{1}\nTime  +{2}\nClear  +{3}", levelScore, missionBonus, timeBonus, clearBonus);
        if (clearNextButton != null) clearNextButton.gameObject.SetActive(!lastLevel);
    }

    public void ShowFail(string reason, int totalScore, HighScoreManager hs, bool newBest)
    {
        HideAll();
        failPanel.SetActive(true);
        if (failTitleText != null) failTitleText.SetText(reason);
        if (failScoreText != null) failScoreText.SetText("SCORE  {0}", totalScore);
        RefreshTop3(hs);
        if (newBestObject != null)
        {
            newBestObject.SetActive(newBest);
            if (newBest)
            {
                UIPunch p = newBestObject.GetComponent<UIPunch>();
                if (p != null) p.Play(2f);
            }
        }
    }

    /// <summary>클리어 마지막 레벨 / 홈 이동 결과에서도 TOP3를 보여주기 위한 공용 갱신.</summary>
    public void RefreshTop3(HighScoreManager hs)
    {
        if (topScoreTexts == null) return;
        for (int i = 0; hs != null && i < topScoreTexts.Length && i < HighScoreManager.Count; i++)
            if (topScoreTexts[i] != null) topScoreTexts[i].SetText("{0}.  {1}", i + 1, hs.GetScore(i));
    }

    public void ShowPause(bool show)
    {
        pausePanel.SetActive(show);
    }

    // ───────── HUD 갱신 ─────────

    public void SetTimer(float seconds, bool unlimited, bool warning)
    {
        if (timerText == null) return;
        if (unlimited)
        {
            if (lastTimerSecond != -2) { timerText.SetText("--:--"); lastTimerSecond = -2; }
            return;
        }
        int s = Mathf.CeilToInt(Mathf.Max(0f, seconds));
        if (s != lastTimerSecond)
        {
            lastTimerSecond = s;
            // Avoid unsupported icon glyphs; update the timer text only when the visible second changes.
            if (s % 60 < 10) timerText.SetText("{0}:0{1}", s / 60, s % 60);
            else timerText.SetText("{0}:{1}", s / 60, s % 60);
        }
        if (warning != timerWarn) SetTimerColor(warning);
    }

    private void SetTimerColor(bool warning)
    {
        timerWarn = warning;
        if (timerText != null) timerText.color = warning ? timerWarning : timerNormal;
    }

    public void SetScore(int score)
    {
        if (scoreText == null || score == lastScore) return;
        bool up = lastScore >= 0 && score > lastScore;
        lastScore = score;
        scoreText.SetText("{0}", score);
        if (up && scorePunch != null) scorePunch.Play(0.6f);
    }

    public void SetSize(int tier, float progress, bool punch)
    {
        if (sizeText != null){var hole=FindFirstObjectByType<HoleController>();
            if(hole!=null&&tier<hole.SizeTable.TierCount)sizeText.text="SIZE "+tier+"  ·  "+Mathf.CeilToInt(hole.SizeTable.GetTier(tier+1).growthRequired-hole.Growth)+" TO GROW";
            else sizeText.text="SIZE "+tier+"  ·  MAX SIZE";
        }
        if (sizeFill != null) sizeFill.fillAmount = progress;
        if (punch && sizePunch != null) sizePunch.Play();
    }

    private bool AcceptClick(){if(Time.unscaledTime-lastClick<0.18f)return false;lastClick=Time.unscaledTime;return true;}
    private static float ReadVolume(string key,float defaultValue){float v=PlayerPrefs.GetFloat("HoleIt.Audio."+key,defaultValue);return NumberGuard.Finite(v)?Mathf.Clamp01(v):defaultValue;}
    private void ShowSettings(){if(!AcceptClick())return;Click();RefreshSettings();if(settingsPanel!=null)settingsPanel.SetActive(true);}
    private void CloseSettings(){if(settingsPanel!=null)settingsPanel.SetActive(false);PlayerPrefs.Save();}
    private void ShowLevelSelect(){if(!AcceptClick())return;Click();if(levelButtons!=null)for(int i=0;i<levelButtons.Length;i++)if(levelButtons[i]!=null)levelButtons[i].interactable=GameManager.Instance!=null&&i<GameManager.Instance.UnlockedLevelCount;if(levelSelectPanel!=null)levelSelectPanel.SetActive(true);}
    private void CloseLevelSelect(){if(levelSelectPanel!=null)levelSelectPanel.SetActive(false);}
    private void SelectLevel(int index){if(!AcceptClick())return;CloseLevelSelect();if(GameManager.Instance!=null)GameManager.Instance.SelectLevel(index);}
    private void ToggleControls(){if(!AcceptClick()||InputManager.Instance==null)return;InputManager.Instance.SetMode(InputManager.Instance.Mode==ControlMode.Drag?ControlMode.FixedJoystick:ControlMode.Drag);RefreshSettings();}
    private void ToggleMute(){if(!AcceptClick())return;bool muted=PlayerPrefs.GetInt("HoleIt.Audio.Mute",0)==1;PlayerPrefs.SetInt("HoleIt.Audio.Mute",muted?0:1);PlayerPrefs.Save();if(AudioManager.Instance!=null)AudioManager.Instance.RefreshVolumes();RefreshSettings();}
    private void RefreshSettings(){if(controlModeText!=null)controlModeText.SetText(InputManager.Instance!=null&&InputManager.Instance.Mode==ControlMode.FixedJoystick?"CONTROL: JOYSTICK":"CONTROL: DRAG");if(muteText!=null)muteText.SetText(PlayerPrefs.GetInt("HoleIt.Audio.Mute",0)==1?"SOUND: OFF":"SOUND: ON");}
    private void SetMaster(float v){SetVolume("Master",v);}
    private void SetMusic(float v){SetVolume("Music",v);}
    private void SetSfx(float v){SetVolume("SFX",v);}
    private void SetVolume(string key,float value){PlayerPrefs.SetFloat("HoleIt.Audio."+key,Mathf.Clamp01(value));if(AudioManager.Instance!=null)AudioManager.Instance.RefreshVolumes();}
    public void SetResultMissions(string value){if(clearMissionsText!=null)clearMissionsText.SetText(value);if(failMissionsText!=null)failMissionsText.SetText(value);}
    public void ShowScorePopup(int value){
        if(popupTimes==null)return;for(int i=0;i<popupTimes.Length;i++)if(popupTimes[i]<=0&&popupTexts[i]!=null){popupTimes[i]=0.8f;popupTexts[i].gameObject.SetActive(true);popupTexts[i].SetText(value>=0?"+{0}":"{0}",value);popupTexts[i].rectTransform.anchoredPosition=new Vector2((i%3-1)*95,0);return;}
    }
    private void Update(){
        if(timerWarn&&timerText!=null&&GameManager.Instance!=null&&GameManager.Instance.State==GameState.Playing)timerText.rectTransform.localScale=Vector3.one*(1+0.05f*Mathf.Sin(Time.time*7));
        else if(timerText!=null)timerText.rectTransform.localScale=Vector3.one;
        if(popupTimes==null)return;
        for(int i=0;i<popupTimes.Length;i++)if(popupTimes[i]>0&&popupTexts[i]!=null){popupTimes[i]-=Time.deltaTime;var rt=popupTexts[i].rectTransform;rt.anchoredPosition+=Vector2.up*(65*Time.deltaTime);if(popupTimes[i]<=0)popupTexts[i].gameObject.SetActive(false);}
    }
}
