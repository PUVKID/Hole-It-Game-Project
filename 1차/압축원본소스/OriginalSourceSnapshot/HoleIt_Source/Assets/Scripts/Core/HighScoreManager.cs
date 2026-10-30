using UnityEngine;

/// <summary>
/// 최고 점수 TOP 3 + 진행 레벨 저장. PlayerPrefs 사용(Android/iOS 모두 영속).
/// 키: HoleIt.Score0~2, HoleIt.MaxLevel. 저장 직후 PlayerPrefs.Save()로 앱 강제 종료에도 유지된다.
/// </summary>
public class HighScoreManager : MonoBehaviour
{
    private const string ScoreKey = "HoleIt.Score";
    private const string LevelKey = "HoleIt.MaxLevel";
    public const int Count = 3;

    private readonly int[] scores = new int[Count];

    public int GetScore(int rank) { return scores[rank]; }
    public int BestScore { get { return scores[0]; } }

    public int MaxUnlockedLevel { get; private set; }

    private void Awake()
    {
        Load();
    }

    public void Load()
    {
        for (int i = 0; i < Count; i++) scores[i] = PlayerPrefs.GetInt(ScoreKey + i, 0);
        MaxUnlockedLevel = Mathf.Max(1, PlayerPrefs.GetInt(LevelKey, 1));
    }

    /// <summary>점수를 TOP3에 반영한다. 반환값: 1위를 갱신했는지.</summary>
    public bool Submit(int score)
    {
        if (score <= 0) return false;

        int pos = -1;
        for (int i = 0; i < Count; i++)
        {
            if (score > scores[i]) { pos = i; break; }
        }
        if (pos < 0) return false;

        for (int i = Count - 1; i > pos; i--) scores[i] = scores[i - 1];
        scores[pos] = score;

        for (int i = 0; i < Count; i++) PlayerPrefs.SetInt(ScoreKey + i, scores[i]);
        PlayerPrefs.Save();
        return pos == 0;
    }

    /// <summary>클리어한 레벨 다음 단계를 해금.</summary>
    public void UnlockLevel(int levelNumber)
    {
        if (levelNumber <= MaxUnlockedLevel) return;
        MaxUnlockedLevel = levelNumber;
        PlayerPrefs.SetInt(LevelKey, levelNumber);
        PlayerPrefs.Save();
    }
}
