using UnityEngine;

/// <summary>상단 MISSION 패널. MissionManager 이벤트를 받아 각 줄(MissionRowUI)을 갱신한다.</summary>
public class MissionUI : MonoBehaviour
{
    [SerializeField] private MissionRowUI[] rows;

    private MissionManager manager;

    public void Bind(MissionManager mm)
    {
        if (manager != null)
        {
            manager.OnProgress -= HandleProgress;
            manager.OnMissionCompleted -= HandleCompleted;
        }
        manager = mm;
        if (manager == null) return;
        manager.OnProgress += HandleProgress;
        manager.OnMissionCompleted += HandleCompleted;
    }

    private void OnDestroy()
    {
        if (manager != null)
        {
            manager.OnProgress -= HandleProgress;
            manager.OnMissionCompleted -= HandleCompleted;
        }
    }

    /// <summary>레벨 시작 시 미션 줄 구성.</summary>
    public void Refresh()
    {
        if(manager==null || rows==null) return;
        for (int i = 0; i < rows.Length; i++)
        {
            if (i < manager.MissionCount)
            {
                MissionData m = manager.GetMission(i);
                rows[i].Setup(m);
            }
            else rows[i].Hide();
        }
    }

    private void HandleProgress(int index, int current, int required)
    {
        if (index < rows.Length) rows[index].SetCount(current, true);
    }

    private void HandleCompleted(int index)
    {
        if (index < rows.Length) rows[index].SetComplete();
    }
}
