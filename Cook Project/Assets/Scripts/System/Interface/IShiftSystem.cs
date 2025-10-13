using R3;
using static ShiftSystem;

public interface IShiftSystem : IGameService
{
    ReactiveProperty<int> shiftNumber { get; }
    ReactiveProperty<int> completedOrderCount { get; }
    ReactiveProperty<int> requiredOrderCount { get; }
    ReactiveProperty<float> shiftTimer { get; }
    ReactiveProperty<ShiftState> currentState { get; }
    ReplaySubject<Unit> OnGameStart { get; }
    void StartGame();
    void StartNextShift();
    bool IsCurrentShiftQuestCompleted();
}