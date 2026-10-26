using UnityEngine;
public class MissionManager : MonoBehaviour
{
    public const int MaxMissions=3;
    private readonly MissionData[] missions=new MissionData[MaxMissions];
    private readonly int[] counts=new int[MaxMissions];
    private readonly int[] reserved=new int[MaxMissions];
    private readonly bool[] done=new bool[MaxMissions];
    private int missionCount;
    private bool announced;
    public event System.Action<int,int,int> OnProgress;
    public event System.Action<int> OnMissionCompleted;
    public event System.Action OnAllCompleted;
    public int MissionCount { get { return missionCount; } }
    public void LoadLevel(LevelData level) {
        missionCount=0; announced=false; System.Array.Clear(reserved,0,reserved.Length); System.Array.Clear(counts,0,counts.Length); System.Array.Clear(done,0,done.Length);
        if(level==null || level.missions==null) return;
        if(level.missions.Length>MaxMissions) throw new System.InvalidOperationException("Maximum 3 missions supported by this HUD.");
        for(int i=0;i<level.missions.Length;i++) missions[missionCount++]=level.missions[i];
    }
    public MissionData GetMission(int i) { return i>=0 && i<missionCount?missions[i]:default(MissionData); }
    public int GetCount(int i) { return i>=0 && i<missionCount?counts[i]:0; }
    public bool IsDone(int i) { return i>=0 && i<missionCount && done[i]; }
    public bool AllCompleted { get { if(missionCount==0) return false; for(int i=0;i<missionCount;i++) if(!done[i]) return false; return true; } }
    public bool IsTarget(EdibleData d) { for(int i=0;i<missionCount;i++) if(missions[i].Matches(d)) return true; return false; }
    public bool IsActiveTarget(EdibleData d) { for(int i=0;i<missionCount;i++) if(!done[i] && missions[i].Matches(d)) return true; return false; }

    public bool CanReserveTarget(EdibleData d){for(int i=0;i<missionCount;i++)if(missions[i].Matches(d)&&counts[i]+reserved[i]<missions[i].requiredCount)return true;return false;}
    public bool ReserveTarget(EdibleData d){if(!CanReserveTarget(d))return false;for(int i=0;i<missionCount;i++)if(missions[i].Matches(d)&&counts[i]+reserved[i]<missions[i].requiredCount)reserved[i]++;return true;}
    public void ReleaseReservation(EdibleData d){for(int i=0;i<missionCount;i++)if(missions[i].Matches(d)&&reserved[i]>0)reserved[i]--;}
    public bool Register(EdibleData d) {
        bool counted=false;
        for(int i=0;i<missionCount;i++) {
            if(done[i] || !missions[i].Matches(d)) continue;
            counts[i]=Mathf.Min(counts[i]+1,missions[i].requiredCount); counted=true;
            if(OnProgress!=null) OnProgress(i,counts[i],missions[i].requiredCount);
            if(counts[i]>=missions[i].requiredCount) { done[i]=true; if(OnMissionCompleted!=null) OnMissionCompleted(i); }
        }
        if(!announced && AllCompleted) { announced=true; if(OnAllCompleted!=null) OnAllCompleted(); }
        return counted;
    }
    public string DescribeProgress() {
        var b=new System.Text.StringBuilder();
        for(int i=0;i<missionCount;i++) { if(i>0)b.Append('\n'); b.Append(missions[i].Label).Append("  ").Append(counts[i]).Append('/').Append(missions[i].requiredCount); }
        return b.ToString();
    }
}
