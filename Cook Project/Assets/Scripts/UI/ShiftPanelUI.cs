using TMPro;
using UnityEngine;
using R3;
using System;

public class ShiftPanelUI : MonoBehaviour
{
    public TextMeshProUGUI shiftNumberText;
    public TextMeshProUGUI shiftOnOffText;
    public TextMeshProUGUI shiftTimerText;
    public TextMeshProUGUI orderText;
    public TextMeshProUGUI questText;

    private IShiftSystem shiftSystem;
    private IQuestService questService;

    private async void Awake()
    {
        shiftSystem = await ServiceLocator.Instance.GetAsync<IShiftSystem>();
        questService = await ServiceLocator.Instance.GetAsync<IQuestService>();
        shiftSystem.shiftNumber.Subscribe(UpdateShiftNumber).AddTo(this);
        shiftSystem.currentState.Subscribe(UpdateShiftState).AddTo(this);
        shiftSystem.completedOrderCount.Subscribe(_ => UpdateOrderText()).AddTo(this);
        shiftSystem.requiredOrderCount.Subscribe(_ => UpdateOrderText()).AddTo(this);
        shiftSystem.shiftTimer.Subscribe(UpdateShiftTimer).AddTo(this);
        questService.OnQuestStarted.Subscribe(_ => UpdateQuestText()).AddTo(this);
        questService.OnQuestCompleted.Subscribe(_ => UpdateQuestText()).AddTo(this);
        questService.OnQuestFailed.Subscribe(_ => UpdateQuestText()).AddTo(this);
        UpdateAll();
    }

    private void UpdateAll()
    {
        UpdateShiftNumber(shiftSystem.shiftNumber.Value);
        UpdateShiftState(shiftSystem.currentState.Value);
        UpdateOrderText();
        UpdateShiftTimer(shiftSystem.shiftTimer.Value);
        UpdateQuestText();
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
            ShiftSystem.ShiftState.AfterShift => "Shift: Off",
            ShiftSystem.ShiftState.GaveOver => "Shift: Over",
            _ => "Shift: Unknown",
        };
    }

    private void UpdateShiftTimer(float obj)
    {
        TimeSpan time = TimeSpan.FromSeconds(obj);
        shiftTimerText.text = string.Format("Time Left: {0:D2}:{1:D2}", time.Minutes, time.Seconds);
    }

    private void UpdateOrderText()
    {
        orderText.text = $"Orders: {shiftSystem.completedOrderCount.Value}/{shiftSystem.requiredOrderCount.Value}";
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