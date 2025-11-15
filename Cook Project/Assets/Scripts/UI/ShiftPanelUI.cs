using TMPro;
using UnityEngine;
using R3;
using System;
using Cysharp.Threading.Tasks;

public class ShiftPanelUI : MonoBehaviour, IUIInitializable
{
    public TextMeshProUGUI shiftNumberText;
    public TextMeshProUGUI shiftOnOffText;
    public TextMeshProUGUI shiftTimerText;
    public TextMeshProUGUI orderText;
    public TextMeshProUGUI questText;

    private IShiftSystem shiftSystem;
    private IQuestService questService;
    private string cachedTimeRemaining = "--";
    private string cachedClock = "--";

    public async UniTask Init()
    {
        await UniTask.WaitUntil(() => GameFlow.Instance.IsInitialized);
        shiftSystem = await ServiceLocator.Instance.GetAsync<IShiftSystem>();
        questService = await ServiceLocator.Instance.GetAsync<IQuestService>();
        shiftSystem.OnGameStart.Subscribe(_ => UpdateActiveState());
        shiftSystem.shiftNumber.Subscribe(UpdateShiftNumber).AddTo(this);
        shiftSystem.currentState.Subscribe(UpdateShiftState).AddTo(this);
        shiftSystem.completedOrderCount.Subscribe(_ => UpdateOrderText()).AddTo(this);
        shiftSystem.requiredOrderCount.Subscribe(_ => UpdateOrderText()).AddTo(this);
        shiftSystem.depositedAmount.Subscribe(_ => UpdateOrderText()).AddTo(this);
        shiftSystem.quotaAmount.Subscribe(_ => UpdateOrderText()).AddTo(this);
        shiftSystem.shiftTimer.Subscribe(UpdateShiftTimer).AddTo(this);
        shiftSystem.currentClockHour.Subscribe(UpdateClock).AddTo(this);
        questService.OnQuestStarted.Subscribe(_ => UpdateQuestText()).AddTo(this);
        questService.OnQuestCompleted.Subscribe(_ => UpdateQuestText()).AddTo(this);
        questService.OnQuestFailed.Subscribe(_ => UpdateQuestText()).AddTo(this);
        UpdateAll();
    }

    private void UpdateAll()
    {
        UpdateActiveState();
        UpdateShiftNumber(shiftSystem.shiftNumber.Value);
        UpdateShiftState(shiftSystem.currentState.Value);
        UpdateOrderText();
        UpdateShiftTimer(shiftSystem.shiftTimer.Value);
        UpdateClock(shiftSystem.currentClockHour.Value);
        UpdateQuestText();
    }

    private void UpdateActiveState()
    {
        gameObject.SetActive(shiftSystem.currentState.Value != ShiftSystem.ShiftState.None);
    }

    private void UpdateShiftNumber(int number)
    {
        shiftNumberText.text = $"Shift: {number}";
    }

    private void UpdateShiftState(ShiftSystem.ShiftState state)
    {
        shiftOnOffText.text = state switch
        {
            ShiftSystem.ShiftState.None => "Shift: None",
            ShiftSystem.ShiftState.InShift => "Shift: On",
            ShiftSystem.ShiftState.Overtime => "Shift: Overtime",
            ShiftSystem.ShiftState.AfterShift => "Shift: Off",
            ShiftSystem.ShiftState.GaveOver => "Shift: Over",
            _ => "Shift: Unknown",
        };
    }

    private void UpdateShiftTimer(float obj)
    {
        TimeSpan time = TimeSpan.FromSeconds(Mathf.Max(0f, obj));
        cachedTimeRemaining = string.Format("{0:D2}:{1:D2}", time.Minutes, time.Seconds);
        ApplyClockDisplay();
    }

    private void UpdateClock(float hours)
    {
        cachedClock = FormatClock(hours);
        ApplyClockDisplay();
    }

    private void ApplyClockDisplay()
    {
        shiftTimerText.text = $"Time Left: {cachedTimeRemaining}\nClock: {cachedClock}";
    }

    private void UpdateOrderText()
    {
        orderText.text = $"Quota: ${shiftSystem.depositedAmount.Value}/{shiftSystem.quotaAmount.Value}\nOrders: {shiftSystem.completedOrderCount.Value}/{shiftSystem.requiredOrderCount.Value}";
    }

    private string FormatClock(float hours)
    {
        if (hours < 0f)
            hours = 0f;

        var totalMinutes = Mathf.RoundToInt(hours * 60f);
        var totalHours = Mathf.FloorToInt(hours);
        var minutes = Mathf.Clamp(totalMinutes - totalHours * 60, 0, 59);
        var normalizedHour = ((totalHours % 24) + 24) % 24;
        var suffix = normalizedHour >= 12 ? "PM" : "AM";
        var hour12 = normalizedHour % 12;
        if (hour12 == 0)
            hour12 = 12;
        return $"{hour12:D2}:{minutes:D2} {suffix}";
    }

    private void UpdateQuestText()
    {
        var activeQuests = questService.ongoingQuestList;

        if (activeQuests.Count == 0)
        {
            questText.text = "<size=36><b>Quest:</b></size> <color=#888888>None</color>";
            return;
        }

        var questDisplay = "<size=36><b>Active Quests:</b></size>\n\n";

        for (int i = 0; i < activeQuests.Count; i++)
        {
            var quest = activeQuests[i];
            questDisplay += $"<size=30><b><color=#FFD700>{quest.Title}</color></b></size>\n";
            questDisplay += $"<size=24><color=#CCCCCC>{quest.Description}</color></size>";

            if (i < activeQuests.Count - 1)
            {
                questDisplay += "\n\n";
            }
        }

        questText.text = questDisplay;
    }
}
