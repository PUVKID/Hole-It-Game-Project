using System;
using NUnit.Framework;
using UnityEngine;

public class ReferenceAttendanceTests
{
    static readonly DateTime FirstDate = new DateTime(2026, 10, 13, 8, 30, 0, DateTimeKind.Local);

    [Test]
    public void FirstClaimPaysExactlyOneThousandAndRapidDuplicateClaimsDoNothing()
    {
        var profile = new ReferenceProfile { coins = 237 };
        Assert.IsNotNull(profile.attendance);
        Assert.IsTrue(profile.attendance.CanClaim(FirstDate));
        Assert.IsTrue(profile.TryClaimAttendance(FirstDate, out int rewardDay));
        Assert.AreEqual(1, rewardDay);
        Assert.AreEqual(1237, profile.coins);
        Assert.AreEqual(1, profile.attendance.claimedDays);
        Assert.AreEqual(1, profile.attendance.cycleNumber);
        string claimedState = JsonUtility.ToJson(profile);

        for (int tap = 0; tap < 100; tap++)
        {
            Assert.IsFalse(profile.TryClaimAttendance(FirstDate.AddSeconds(tap), out _));
            Assert.IsFalse(profile.attendance.CanClaim(FirstDate.AddSeconds(tap)));
        }

        Assert.AreEqual(claimedState, JsonUtility.ToJson(profile), "Repeated taps must not award coins or mutate the attendance record.");
    }

    [Test]
    public void LocalMidnightUnlocksNextClaimWithoutWaitingTwentyFourHours()
    {
        var profile = new ReferenceProfile();
        var beforeMidnight = new DateTime(2026, 10, 13, 23, 59, 59, DateTimeKind.Local);
        Assert.IsTrue(profile.TryClaimAttendance(beforeMidnight, out int firstDay));
        Assert.AreEqual(1, firstDay);
        Assert.IsTrue(profile.TryClaimAttendance(beforeMidnight.AddSeconds(1), out int secondDay));
        Assert.AreEqual(2, secondDay);
        Assert.AreEqual(2000, profile.coins);
        Assert.IsFalse(profile.TryClaimAttendance(beforeMidnight.AddHours(6), out _));
    }

    [Test]
    public void AllTimesOnTheSameLocalDateShareOneClaim()
    {
        var attendance = new ReferenceAttendance();
        Assert.IsTrue(attendance.TryClaim(FirstDate.Date, out _));
        Assert.IsFalse(attendance.CanClaim(FirstDate.Date.AddHours(12)));
        Assert.IsFalse(attendance.TryClaim(FirstDate.Date.AddDays(1).AddTicks(-1), out _));
        Assert.AreEqual(1, attendance.claimedDays);
    }

    [Test]
    public void MissedDaysPreserveProgressAndDoNotAwardUnclaimedDays()
    {
        var profile = new ReferenceProfile();
        Assert.IsTrue(profile.TryClaimAttendance(FirstDate, out _));
        DateTime returnDate = FirstDate.AddDays(93);
        Assert.AreEqual(1, profile.attendance.DisplayClaimedDays(returnDate));
        Assert.AreEqual(1, profile.attendance.DisplayCycle(returnDate));
        Assert.IsTrue(profile.TryClaimAttendance(returnDate, out int rewardDay));
        Assert.AreEqual(2, rewardDay);
        Assert.AreEqual(2, profile.attendance.claimedDays);
        Assert.AreEqual(2000, profile.coins);
    }

    [TestCase(2026, 1, 31)]
    [TestCase(2026, 12, 31)]
    [TestCase(2028, 2, 28)]
    [TestCase(2028, 2, 29)]
    public void CalendarTransitionsKeepSequentialAttendance(int year, int month, int day)
    {
        var attendance = new ReferenceAttendance();
        var date = new DateTime(year, month, day, 22, 0, 0, DateTimeKind.Local);
        Assert.IsTrue(attendance.TryClaim(date, out int firstDay));
        Assert.AreEqual(1, firstDay);
        Assert.IsTrue(attendance.TryClaim(date.AddDays(1), out int secondDay));
        Assert.AreEqual(2, secondDay);
        Assert.AreEqual(1, attendance.cycleNumber);
    }

    [Test]
    public void ThirtyOneClaimsCompleteBoardAndNextDateStartsNextCycle()
    {
        var profile = new ReferenceProfile();
        for (int day = 1; day <= 31; day++)
        {
            Assert.IsTrue(profile.TryClaimAttendance(FirstDate.AddDays(day - 1), out int rewardDay));
            Assert.AreEqual(day, rewardDay);
            Assert.AreEqual(day * 1000, profile.coins);
        }

        DateTime completionDate = FirstDate.AddDays(30);
        Assert.AreEqual(31, profile.attendance.claimedDays);
        Assert.AreEqual(1, profile.attendance.cycleNumber);
        Assert.AreEqual(31, profile.attendance.DisplayClaimedDays(completionDate));
        Assert.AreEqual(1, profile.attendance.DisplayCycle(completionDate));
        Assert.IsFalse(profile.TryClaimAttendance(completionDate.AddHours(2), out _));

        string savedState = JsonUtility.ToJson(profile);
        DateTime nextDate = completionDate.AddDays(1);
        Assert.IsTrue(profile.attendance.CanClaim(nextDate));
        Assert.AreEqual(0, profile.attendance.DisplayClaimedDays(nextDate));
        Assert.AreEqual(2, profile.attendance.DisplayCycle(nextDate));
        Assert.AreEqual(savedState, JsonUtility.ToJson(profile), "Rendering the next board must not consume or change a claim.");
        Assert.IsTrue(profile.TryClaimAttendance(nextDate, out int nextRewardDay));
        Assert.AreEqual(1, nextRewardDay);
        Assert.AreEqual(1, profile.attendance.claimedDays);
        Assert.AreEqual(2, profile.attendance.cycleNumber);
        Assert.AreEqual(32000, profile.coins);
    }

    [Test]
    public void ClockRollbackNeverReopensAnEarlierOrAlreadyClaimedDate()
    {
        var profile = new ReferenceProfile();
        Assert.IsTrue(profile.TryClaimAttendance(FirstDate, out _));
        DateTime latestDate = FirstDate.AddDays(10);
        Assert.IsTrue(profile.TryClaimAttendance(latestDate, out _));
        string savedState = JsonUtility.ToJson(profile);

        foreach (DateTime attemptedDate in new[] { FirstDate.AddYears(-1), FirstDate, FirstDate.AddDays(5), latestDate.Date })
        {
            Assert.IsFalse(profile.attendance.CanClaim(attemptedDate));
            Assert.IsFalse(profile.TryClaimAttendance(attemptedDate, out _));
            Assert.AreEqual(savedState, JsonUtility.ToJson(profile));
        }

        Assert.IsTrue(profile.TryClaimAttendance(latestDate.AddDays(1), out int rewardDay));
        Assert.AreEqual(3, rewardDay);
        Assert.AreEqual(3000, profile.coins);
    }

    [Test]
    public void SavingAndReloadingKeepsSameDayBlockedAndNextDayAvailable()
    {
        var original = new ReferenceProfile { coins = 81 };
        Assert.IsTrue(original.TryClaimAttendance(FirstDate, out _));
        ReferenceProfile reloaded = JsonUtility.FromJson<ReferenceProfile>(JsonUtility.ToJson(original));
        Assert.IsNotNull(reloaded.attendance);
        Assert.AreEqual(1081, reloaded.coins);
        Assert.IsFalse(reloaded.TryClaimAttendance(FirstDate.AddHours(3), out _));
        Assert.AreEqual(1081, reloaded.coins);
        Assert.AreEqual(1, reloaded.attendance.claimedDays);
        Assert.IsTrue(reloaded.TryClaimAttendance(FirstDate.AddDays(1), out int rewardDay));
        Assert.AreEqual(2, rewardDay);
        Assert.AreEqual(2081, reloaded.coins);
    }

    [Test]
    public void PreviousReleaseProfileGainsAttendanceWithoutLosingProgress()
    {
        const string legacyJson = "{\"coins\":1729,\"hearts\":3,\"refillAt\":5000,\"boosters\":[2,3,4,5],\"freePlay\":true}";
        ReferenceProfile upgraded = JsonUtility.FromJson<ReferenceProfile>(legacyJson);
        upgraded.Refresh(4000);
        Assert.IsNotNull(upgraded.attendance);
        Assert.AreEqual(1729, upgraded.coins);
        Assert.AreEqual(3, upgraded.hearts);
        CollectionAssert.AreEqual(new[] { 2, 3, 4, 5 }, upgraded.boosters);
        Assert.IsTrue(upgraded.freePlay);
        Assert.IsTrue(upgraded.TryClaimAttendance(FirstDate, out int rewardDay));
        Assert.AreEqual(1, rewardDay);
        Assert.AreEqual(2729, upgraded.coins);
    }

    [Test]
    public void CoinOverflowRejectsWholeTransactionAndLeavesDateClaimable()
    {
        var profile = new ReferenceProfile { coins = int.MaxValue - 999 };
        string savedState = JsonUtility.ToJson(profile);
        Assert.IsFalse(profile.TryClaimAttendance(FirstDate, out _));
        Assert.AreEqual(savedState, JsonUtility.ToJson(profile));
        Assert.IsTrue(profile.attendance.CanClaim(FirstDate));

        profile.coins = int.MaxValue - 1000;
        Assert.IsTrue(profile.TryClaimAttendance(FirstDate, out int rewardDay));
        Assert.AreEqual(1, rewardDay);
        Assert.AreEqual(int.MaxValue, profile.coins);
    }

    [Test]
    public void CoinOverflowAtCycleRolloverDoesNotAdvanceBoard()
    {
        var profile = new ReferenceProfile();
        for (int day = 0; day < 31; day++)
            Assert.IsTrue(profile.TryClaimAttendance(FirstDate.AddDays(day), out _));
        profile.coins = int.MaxValue;
        string savedState = JsonUtility.ToJson(profile);
        DateTime rolloverDate = FirstDate.AddDays(31);
        Assert.IsFalse(profile.TryClaimAttendance(rolloverDate, out _));
        Assert.AreEqual(savedState, JsonUtility.ToJson(profile));

        profile.coins = 0;
        Assert.IsTrue(profile.TryClaimAttendance(rolloverDate, out int rewardDay));
        Assert.AreEqual(1, rewardDay);
        Assert.AreEqual(2, profile.attendance.cycleNumber);
        Assert.AreEqual(1000, profile.coins);
    }
}
