using R3;
using static ShiftSystem;

public interface IShiftSystem : IGameService
{
    ReactiveProperty<int> shiftNumber { get; }
    ReactiveProperty<int> completedOrderCount { get; }
    ReactiveProperty<int> requiredOrderCount { get; }
    ReactiveProperty<int> depositedAmount { get; }
    ReactiveProperty<int> quotaAmount { get; }
    ReactiveProperty<float> shiftTimer { get; }
    ReactiveProperty<float> currentClockHour { get; }
    ReactiveProperty<ShiftState> currentState { get; }
    ReplaySubject<Unit> OnGameStart { get; }
    void StartGame();
    void StartNextShift();
    bool IsCurrentShiftQuestCompleted();
    bool TryDeposit(int amount);
    int DepositAllAvailableFunds();
    bool HasMetQuota();
}
