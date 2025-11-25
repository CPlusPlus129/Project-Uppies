using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using R3;

public class ShiftCalendar : InteractableBase
{
    private IShiftSystem shiftSystem;
    [SerializeField]
    [Tooltip("Signal emitted when the player chooses to exit AfterShift via this object.")]
    private string afterShiftExitSignalId = "after_shift_exit";
    [Header("Display")]
    [SerializeField]
    [Tooltip("Current shift status display text.")]
    private TextMeshPro shiftStatusText;
    [SerializeField]
    [Tooltip("Current shift number display text.")]
    private TextMeshPro shiftNumberText;

    protected override void Awake()
    {
        base.Awake();
        InitializeAsync().Forget();
    }

    private async UniTaskVoid InitializeAsync()
    {
        await UniTask.WaitUntil(() => GameFlow.Instance.IsInitialized);
        shiftSystem = await ServiceLocator.Instance.GetAsync<IShiftSystem>();
        shiftSystem.shiftNumber.Subscribe(UpdateShiftNumber).AddTo(this);
        shiftSystem.currentState.Subscribe(UpdateShiftState).AddTo(this);
    }

    public override void Interact()
    {
        var state = shiftSystem.currentState.Value;
        if (state != ShiftSystem.ShiftState.AfterShift && state != ShiftSystem.ShiftState.None)
        {
            WorldBroadcastSystem.Instance.Broadcast("You can only start next shift when your shift is off.");
            return;
        }

        if (state != ShiftSystem.ShiftState.AfterShift)
        {
            WorldBroadcastSystem.Instance.Broadcast("You haven't completed the shift requirements yet!");
            return;
        }

        if (shiftSystem.HasTasksRequiredBeforeShiftStarts())
        {
            WorldBroadcastSystem.Instance.Broadcast("You must complete all required tasks before starting the next shift!", 4f);
            return;
        }

        EmitAfterShiftExitSignal();
        if (shiftSystem.IsAfterShiftReadyForNextShift)
        {
            shiftSystem.StartNextShift();
        }
        else
        {
            shiftSystem.RestartCurrentShift();
        }
    }

    private void EmitAfterShiftExitSignal()
    {
        var signalId = string.IsNullOrWhiteSpace(afterShiftExitSignalId) ? "after_shift_exit" : afterShiftExitSignalId.Trim();
        GameFlow.Instance?.Signal(signalId);
    }

    private void UpdateShiftNumber(int number)
    {
        var displayIndex = Mathf.Max(0, number) + 1;
        shiftNumberText.text = $"{displayIndex}";
    }

    private void UpdateShiftState(ShiftSystem.ShiftState state)
    {
        bool firstShiftPending = shiftSystem.shiftNumber.Value <= 0;
        bool treatAsIdle = state == ShiftSystem.ShiftState.None ||
                           (firstShiftPending && (state == ShiftSystem.ShiftState.AfterShift || state == ShiftSystem.ShiftState.GaveOver));

        string label = treatAsIdle ? "OFF DUTY" : state switch
        {
            ShiftSystem.ShiftState.None => "OFF DUTY",
            ShiftSystem.ShiftState.InShift => "ON DUTY",
            ShiftSystem.ShiftState.Overtime => "OVERTIME",
            ShiftSystem.ShiftState.AfterShift => "SHIFT COMPLETE",
            ShiftSystem.ShiftState.GaveOver => "BANKRUPT",
            _ => "Unknown State"
        };

        shiftStatusText.text = label;
    }
}
