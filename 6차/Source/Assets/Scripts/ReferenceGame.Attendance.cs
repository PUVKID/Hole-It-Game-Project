using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class ReferenceGame
{
    GameObject attendancePanel;
    Image[] attendanceCards;
    TextMeshProUGUI[] attendanceCardStates;
    TextMeshProUGUI attendanceCycleLabel, attendanceMessage, attendanceClaimLabel;
    Button attendanceClaimButton;
    string attendanceFeedback;
    float attendanceFeedbackUntil;

    public bool AttendanceIsOpen => attendancePanel != null && attendancePanel.activeSelf;
    public int AttendanceCardCount => attendanceCards == null ? 0 : attendanceCards.Length;
    public int AttendanceCoinsForTest => profile == null ? 0 : profile.coins;
    public Button AttendanceClaimButtonForTest => attendanceClaimButton;

    public void BuildAttendanceUI()
    {
        if (attendancePanel != null) return;
        attendancePanel = Layer("Daily Attendance");
        CreateModal(attendancePanel, "Daily Attendance");
        StyleCard(attendancePanel);
        attendancePanel.transform.Find("Card").GetComponent<RectTransform>().sizeDelta = new Vector2(940, 1480);

        var title = AttendanceLabel("Title", "DAILY REWARDS", 58, new Vector2(840, 100), new Vector2(0, 630));
        WhiteOutline(title);
        AttendanceLabel("Daily Reward", "1,000 COINS EVERY DAY", 32, new Vector2(820, 60), new Vector2(0, 545));
        attendanceCycleLabel = AttendanceLabel("Cycle", "", 25, new Vector2(840, 55), new Vector2(0, 490));
        attendanceCards = new Image[ReferenceAttendance.CycleLength];
        attendanceCardStates = new TextMeshProUGUI[ReferenceAttendance.CycleLength];

        for (int i = 0; i < attendanceCards.Length; i++)
        {
            int column = i % 7, row = i / 7;
            float x = row == 4 ? (column - 1) * 120 : (column - 3) * 120;
            var card = Panel("Attendance Day " + (i + 1), attendancePanel.transform,
                new Vector2(.5f, .5f), new Vector2(110, 144), Color.white, new Vector2(x, 375 - row * 160));
            card.sprite = Style("goal-card");
            card.type = Image.Type.Simple;
            attendanceCards[i] = card;

            Label("Day", card.transform, "DAY " + (i + 1), 20, Color.white,
                new Vector2(.5f, .85f), new Vector2(102, 30), Vector2.zero);
            var coin = Panel("Coin", card.transform, new Vector2(.5f, .53f), new Vector2(49, 49), Color.white, Vector2.zero);
            coin.sprite = Style("Coin-UI");
            coin.type = Image.Type.Simple;
            coin.preserveAspect = true;
            Label("Reward", card.transform, "+1,000", 23, Color.white,
                new Vector2(.5f, .285f), new Vector2(104, 32), Vector2.zero);
            var state = Label("State", card.transform, "", 18, Color.white,
                new Vector2(.5f, .105f), new Vector2(105, 25), Vector2.zero);
            state.enableAutoSizing = false;
            state.fontSize = 16;
            attendanceCardStates[i] = state;
        }

        attendanceMessage = AttendanceLabel("Message", "", 27, new Vector2(810, 80), new Vector2(0, -395));
        attendanceClaimButton = Btn("Claim Attendance", attendancePanel.transform, "CLAIM 1,000 COINS",
            new Vector2(.5f, .5f), new Vector2(700, 110), new Vector2(0, -495), Green, ClaimAttendance);
        attendanceClaimLabel = attendanceClaimButton.GetComponentInChildren<TextMeshProUGUI>();
        attendanceClaimLabel.fontSize = 34;
        attendanceClaimLabel.fontSizeMax = 34;
        attendanceClaimLabel.fontSizeMin = 24;
        Btn("Close Attendance", attendancePanel.transform, "DONE", new Vector2(.5f, .5f),
            new Vector2(490, 85), new Vector2(0, -615), Blue, CloseAttendance);
        AttendanceLabel("Keep Progress", "Missed a day? Your progress stays.", 23,
            new Vector2(820, 48), new Vector2(0, -695));
        attendancePanel.SetActive(false);
    }

    TextMeshProUGUI AttendanceLabel(string name, string value, int size, Vector2 dimensions, Vector2 offset)
    {
        return Label(name, attendancePanel.transform, value, size, Color.white,
            new Vector2(.5f, .5f), dimensions, offset);
    }

    public void OpenAttendance()
    {
        if (profile == null || State == ScreenState.Playing) return;
        if (attendancePanel == null) BuildAttendanceUI();
        CloseProfilePanels();
        settings.SetActive(false);
        attendanceFeedback = null;
        attendancePanel.transform.SetAsLastSibling();
        attendancePanel.SetActive(true);
        RefreshAttendance();
    }

    public void CloseAttendance()
    {
        if (attendancePanel != null) attendancePanel.SetActive(false);
    }

    public void RefreshAttendance()
    {
        if (attendancePanel == null || !attendancePanel.activeSelf || profile == null) return;
        if (profile.attendance == null) profile.attendance = new ReferenceAttendance();
        DateTime localDate = DateTime.Now;
        int claimed = profile.attendance.DisplayClaimedDays(localDate);
        bool available = profile.attendance.CanClaim(localDate);
        bool walletHasRoom = profile.coins <= int.MaxValue - ReferenceAttendance.Reward;
        attendanceCycleLabel.text = "31-DAY ATTENDANCE  /  CYCLE " + profile.attendance.DisplayCycle(localDate);

        for (int i = 0; i < attendanceCards.Length; i++)
        {
            bool complete = i < claimed;
            bool next = available && i == claimed;
            attendanceCards[i].sprite = Style(next ? "booster-btn-yellow" : "goal-card");
            attendanceCards[i].color = complete ? new Color(.65f, 1f, .78f) : Color.white;
            attendanceCardStates[i].text = complete ? "DONE" : next ? "TODAY" : "LOCKED";
            attendanceCardStates[i].color = complete ? new Color(.75f, 1f, .8f) : Color.white;
        }

        attendanceClaimButton.interactable = available && walletHasRoom;
        attendanceClaimLabel.text = !available ? "COME BACK TOMORROW" : !walletHasRoom ? "COIN WALLET IS FULL" : "CLAIM 1,000 COINS";
        if (!string.IsNullOrEmpty(attendanceFeedback) && Time.unscaledTime < attendanceFeedbackUntil)
            attendanceMessage.text = attendanceFeedback;
        else
            attendanceMessage.text = available
                ? "DAY " + (claimed + 1) + " IS READY!  /  " + profile.coins.ToString("N0") + " COINS"
                : "TODAY'S REWARD CLAIMED!\n" + profile.coins.ToString("N0") + " COINS";
    }

    void ClaimAttendance()
    {
        if (profile == null) return;
        bool claimed = profile.TryClaimAttendance(DateTime.Now, out int rewardDay);
        if (claimed)
        {
            SaveProfile();
            RefreshWallet();
            Sound(4);
            attendanceFeedback = "+1,000 COINS!  DAY " + rewardDay + " / 31\n" + profile.coins.ToString("N0") + " COINS IN YOUR WALLET";
        }
        else
        {
            attendanceFeedback = profile.coins > int.MaxValue - ReferenceAttendance.Reward
                ? "YOUR COIN WALLET IS FULL"
                : "TODAY'S REWARD IS ALREADY CLAIMED";
        }
        attendanceFeedbackUntil = Time.unscaledTime + 5;
        RefreshAttendance();
    }
}
