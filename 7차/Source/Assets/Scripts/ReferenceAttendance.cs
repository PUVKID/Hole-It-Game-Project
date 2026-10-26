using System;

/// <summary>Local calendar attendance. Display queries never consume a reward.</summary>
[Serializable]
public sealed class ReferenceAttendance
{
    public int claimedDays, cycleNumber = 1, lastClaimDateKey;
    public const int Reward = 1000, CycleLength = 31;
    static int DateKey(DateTime date) => date.Year * 10000 + date.Month * 100 + date.Day;
    int Days => Math.Max(0, Math.Min(CycleLength, claimedDays));
    int Cycle => Math.Max(1, cycleNumber);
    public bool CanClaim(DateTime localDate) => DateKey(localDate) > lastClaimDateKey;
    bool StartsNextCycle(DateTime localDate) => Days == CycleLength && CanClaim(localDate);
    public int DisplayClaimedDays(DateTime localDate) => StartsNextCycle(localDate) ? 0 : Days;
    public int DisplayCycle(DateTime localDate) => StartsNextCycle(localDate) && Cycle < int.MaxValue ? Cycle + 1 : Cycle;
    public bool TryClaim(DateTime localDate, out int rewardDay)
    {
        rewardDay = 0;
        if (!CanClaim(localDate)) return false;
        int nextCycle = DisplayCycle(localDate);
        rewardDay = DisplayClaimedDays(localDate) + 1;
        claimedDays = rewardDay;
        cycleNumber = nextCycle;
        lastClaimDateKey = DateKey(localDate);
        return true;
    }
}
