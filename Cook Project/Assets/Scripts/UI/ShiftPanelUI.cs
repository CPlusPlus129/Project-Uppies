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

    private void Awake()
    {
        ShiftSystem.Instance.shiftNumber.Subscribe(UpdateShiftNumber).AddTo(this);
        ShiftSystem.Instance.currentState.Subscribe(UpdateShiftState).AddTo(this);
        ShiftSystem.Instance.completedOrderCount.Subscribe(_ => UpdateOrderText()).AddTo(this);
        ShiftSystem.Instance.requiredOrderCount.Subscribe(_ => UpdateOrderText()).AddTo(this);
        ShiftSystem.Instance.shiftTimer.Subscribe(UpdateShiftTimer).AddTo(this);
        QuestManager.Instance.OnQuestStarted.Subscribe(_ => UpdateQuestText()).AddTo(this);
        QuestManager.Instance.OnQuestCompleted.Subscribe(_ => UpdateQuestText()).AddTo(this);
        QuestManager.Instance.OnQuestFailed.Subscribe(_ => UpdateQuestText()).AddTo(this);
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
        var shiftSystem = ShiftSystem.Instance;
        orderText.text = $"Orders: {shiftSystem.completedOrderCount.Value}/{shiftSystem.requiredOrderCount.Value}";
    }

    private void UpdateQuestText()
    {
        var activeQuests = QuestManager.Instance.GetActiveQuests();

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