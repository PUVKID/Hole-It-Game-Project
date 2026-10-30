using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 모든 화면(Title / HUD / Ready / Clear / Fail / Pause)의 표시 담당. 로직은 GameManager가 가진다.
/// 텍스트는 TMP SetText(format, args)를 사용해 문자열 GC를 만들지 않고, 값이 바뀔 때만 갱신한다.
/// UI 문구는 영어 (기본 TMP 폰트로 표시 가능). 한국어 문구가 필요하면 한글 TMP 폰트 에셋을 지정할 것.
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
    [SerializeField] private TMP_Text titleLevelText;
    [SerializeField] private TMP_Text titleBestText;

    [Header("HUD")]
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text sizeText;
    [SerializeField] private Image sizeFill;
    [SerializeField] private Button pauseButton;
    [SerializeField] private MissionUI missionUI;
    [SerializeField] private UIPunch sizePunch;
    [SerializeField] private UIPunch scorePunch;
    [SerializeField] private Color timerNormal = Color.white;
    [SerializeField] private Color timerWarning = new Color(1f, 0.35f, 0.3f);

    [Header("Ready")]
    [SerializeField] private TMP_Text readyLevelText;

    [Header("Clear")]
    [SerializeField] private TMP_Text clearScoreText;
    [SerializeField] private TMP_Text clearBreakdownText;
    [SerializeField] private Button clearNextButton;
    [SerializeField] private Button clearHomeButton;

    [Header("Fail / Result")]
    [SerializeField] private TMP_Text failTitleText;
    [SerializeField] private TMP_Text failScoreText;
    [SerializeField] private TMP_Text[] topScoreTexts;
    [SerializeField] private GameObject newBestObject;
    [SerializeField] private Button failRetryButton;
    [SerializeField] private Button failHomeButton;

    [Header("Pause")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button pauseHomeButton;

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
    }

    private static void Hook(Button b, UnityEngine.Events.UnityAction a)
    {
        if (b != null) b.onClick.AddListener(a);
    }

    private void Click() { if (AudioManager.Instance != null) AudioManager.Instance.PlayButton(); }
    private void HandlePlay() { Click(); if (OnPlay != null) OnPlay(); }
    private void HandlePrev() { Click(); if (OnLevelStep != null) OnLevelStep(-1); }
    private void HandleNextLevelSelect() { Click(); if (OnLevelStep != null) OnLevelStep(1); }
    private void HandlePause() { Click(); if (OnPause != null) OnPause(); }
    private void HandleResume() { Click(); if (OnResume != null) OnResume(); }
    private void HandleHome() { Click(); if (OnHome != null) OnHome(); }
    private void HandleNext() { Click(); if (OnNext != null) OnNext(); }
    private void HandleRetry() { Click(); if (OnRetry != null) OnRetry(); }

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
        missionCompleteBanner.SetActive(false);
        clearPanel.SetActive(true);
        if (clearScoreText != null) clearScoreText.SetText("SCORE  {0}", totalScore);
        if (clearBreakdownText != null)
            clearBreakdownText.SetText("Objects  +{0}\nMission  +{1}\nTime  +{2}\nClear  +{3}", levelScore, missionBonus, timeBonus, clearBonus);
        if (clearNextButton != null) clearNextButton.gameObject.SetActive(!lastLevel);
    }

    public void ShowFail(string reason, int totalScore, HighScoreManager hs, bool newBest)
    {
        hudPanel.SetActive(false);
        missionCompleteBanner.SetActive(false);
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
        for (int i = 0; i < topScoreTexts.Length && i < HighScoreManager.Count; i++)
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
            // TMP SetText의 {1:00}은 소수 자리수 의미라 0 패딩이 안 되므로 분기 처리 (GC 없음)
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
        if (sizeText != null) sizeText.SetText("SIZE {0}", tier);
        if (sizeFill != null) sizeFill.fillAmount = progress;
        if (punch && sizePunch != null) sizePunch.Play();
    }
}
