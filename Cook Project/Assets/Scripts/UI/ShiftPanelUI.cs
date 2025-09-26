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
        ShiftSystem.Instance.specialQuest.Subscribe(UpdateQuestText).AddTo(this);
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

    private void UpdateQuestText(string obj)
    {
        questText.text = string.IsNullOrEmpty(obj) ? "Quest: None" : $"Quest: {obj}";
    }
}