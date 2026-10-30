using UnityEngine;

/// <summary>
/// 미션 진행도 관리. 데이터(MissionData)와 런타임 카운트를 분리하고, 배열을 재사용해 GC를 만들지 않는다.
/// </summary>
public class MissionManager : MonoBehaviour
{
    public const int MaxMissions = 3;

    private readonly MissionData[] missions = new MissionData[MaxMissions];
    private readonly int[] counts = new int[MaxMissions];
    private readonly bool[] done = new bool[MaxMissions];
    private int missionCount;

    /// <summary>(미션 인덱스, 현재 수, 필요 수)</summary>
    public event System.Action<int, int, int> OnProgress;
    public event System.Action<int> OnMissionCompleted;
    public event System.Action OnAllCompleted;

    public int MissionCount { get { return missionCount; } }

    public void LoadLevel(LevelData level)
    {
        missionCount = 0;
        if (level.missions != null)
        {
            for (int i = 0; i < level.missions.Length && i < MaxMissions; i++)
            {
                if (level.missions[i].target == null) continue;
                missions[missionCount] = level.missions[i];
                counts[missionCount] = 0;
                done[missionCount] = false;
                missionCount++;
            }
        }
    }

    public MissionData GetMission(int i) { return missions[i]; }
    public int GetCount(int i) { return counts[i]; }
    public bool IsDone(int i) { return done[i]; }

    public bool AllCompleted
    {
        get
        {
            if (missionCount == 0) return false;
            for (int i = 0; i < missionCount; i++) if (!done[i]) return false;
            return true;
        }
    }

    /// <summary>이 오브젝트가 (아직 끝나지 않은) 미션 대상인지.</summary>
    public bool IsTarget(EdibleData d)
    {
        for (int i = 0; i < missionCount; i++)
            if (missions[i].target == d) return true;
        return false;
    }

    /// <summary>먹은 오브젝트를 미션에 반영. 반영되면 true.</summary>
    public bool Register(EdibleData d)
    {
        bool counted = false;
        for (int i = 0; i < missionCount; i++)
        {
            if (missions[i].target != d || done[i]) continue;
            counts[i]++;
            counted = true;
            int req = missions[i].requiredCount;
            if (OnProgress != null) OnProgress(i, counts[i], req);
            if (counts[i] >= req)
            {
                done[i] = true;
                if (OnMissionCompleted != null) OnMissionCompleted(i);
            }
        }
        if (counted && AllCompleted && OnAllCompleted != null) OnAllCompleted();
        return counted;
    }
}
