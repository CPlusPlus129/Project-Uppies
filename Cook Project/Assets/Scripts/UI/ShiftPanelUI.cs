using TMPro;
using UnityEngine;
using UnityEngine.UI;
using R3;
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

public class ShiftPanelUI : MonoBehaviour, IUIInitializable
{
    [Header("Primary Elements")]
    public TextMeshProUGUI shiftNumberText;
    public TextMeshProUGUI shiftOnOffText;
    public TextMeshProUGUI clockText;
    [Header("Supporting Elements")]
    [SerializeField] private TextMeshProUGUI shiftSubtitleText;
    [SerializeField] private Image statusBadgeImage;
    [SerializeField] private Image panelBackgroundImage;

    private IShiftSystem shiftSystem;
    private readonly Color32 offDutyBadge = new(48, 47, 66, 255);
    private readonly Color32 onDutyBadge = new(99, 214, 170, 255);
    private readonly Color32 overtimeBadge = new(255, 169, 86, 255);
    private readonly Color32 backgroundBase = new(255, 255, 255, 236);
    private readonly Color32 backgroundOvertime = new(255, 234, 200, 236);

    public async UniTask Init()
    {
        await UniTask.WaitUntil(() => GameFlow.Instance.IsInitialized);
        shiftSystem = await ServiceLocator.Instance.GetAsync<IShiftSystem>();
        shiftSystem.OnGameStart.Subscribe(_ => UpdateActiveState());
        shiftSystem.shiftNumber.Subscribe(UpdateShiftNumber).AddTo(this);
        shiftSystem.currentState.Subscribe(UpdateShiftState).AddTo(this);
        shiftSystem.currentClockHour.Subscribe(UpdateClock).AddTo(this);

        // Subscribe to TaskManager updates
        if (TaskManager.Instance != null)
        {
            TaskManager.Instance.Tasks.Subscribe(_ => UpdateShiftState(shiftSystem.currentState.Value)).AddTo(this);
        }

        UpdateAll();
    }

    private void UpdateAll()
    {
        UpdateActiveState();
        UpdateShiftNumber(shiftSystem.shiftNumber.Value);
        UpdateShiftState(shiftSystem.currentState.Value);
        UpdateClock(shiftSystem.currentClockHour.Value);
    }

    private void UpdateActiveState()
    {
        gameObject.SetActive(shiftSystem.currentState.Value != ShiftSystem.ShiftState.None);
    }

    private void UpdateShiftNumber(int number)
    {
        var displayIndex = Mathf.Max(0, number) + 1;
        shiftNumberText.text = $"Shift {displayIndex:D2}";
    }

    private void UpdateClock(float hours)
    {
        clockText.text = FormatClock(hours);
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

    private void UpdateShiftState(ShiftSystem.ShiftState state)
    {
        string label;
        string subtitle;
        Color badgeColor;
        Color backgroundColor;

        bool firstShiftPending = shiftSystem.shiftNumber.Value <= 0;
        bool treatAsIdle = state == ShiftSystem.ShiftState.None ||
                           (firstShiftPending && (state == ShiftSystem.ShiftState.AfterShift || state == ShiftSystem.ShiftState.GaveOver));

        if (treatAsIdle)
        {
            label = "OFF DUTY";
            subtitle = "Clock in when you are ready.";
            badgeColor = offDutyBadge;
            backgroundColor = backgroundBase;
        }
        else
        {
            switch (state)
            {
                case ShiftSystem.ShiftState.InShift:
                    label = "ON DUTY";
                    subtitle = "Keep orders moving and watch the clock.";
                    badgeColor = onDutyBadge;
                    backgroundColor = backgroundBase;
                    break;
                case ShiftSystem.ShiftState.Overtime:
                    label = "OVERTIME";
                    subtitle = "Push through the rush for bonus payouts.";
                    badgeColor = overtimeBadge;
                    backgroundColor = backgroundOvertime;
                    break;
                case ShiftSystem.ShiftState.AfterShift:
                case ShiftSystem.ShiftState.GaveOver:
                    label = "SHIFT COMPLETE";
                    subtitle = "Catch your breath before the next gig.";
                    badgeColor = offDutyBadge;
                    backgroundColor = backgroundBase;
                    break;
                default:
                    label = "OFF DUTY";
                    subtitle = "Clock in when you are ready.";
                    badgeColor = offDutyBadge;
                    backgroundColor = backgroundBase;
                    break;
            }
        }

        // Override subtitle with tasks if any exist
        if (TaskManager.Instance != null && TaskManager.Instance.Tasks.Value.Count > 0)
        {
            var tasks = TaskManager.Instance.Tasks.Value;
            var taskListString = "";
            foreach (var task in tasks)
            {
                if (taskListString.Length > 0) taskListString += "\n";
                var checkbox = task.IsCompleted ? "[X]" : "[ ]";
                taskListString += $"{checkbox} {task.Description}";
            }
            subtitle = taskListString;
        }

        shiftOnOffText.text = label;

        if (shiftSubtitleText != null)
        {
            shiftSubtitleText.text = subtitle;
            shiftSubtitleText.gameObject.SetActive(true);
        }

        if (statusBadgeImage != null)
            statusBadgeImage.color = badgeColor;

        if (panelBackgroundImage != null)
            panelBackgroundImage.color = backgroundColor;
    }
}
