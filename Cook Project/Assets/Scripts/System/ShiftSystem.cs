using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

public class ShiftSystem : IShiftSystem
{
    public enum ShiftState
    {
        None = 0,
        InShift,
        Overtime,
        AfterShift,
        GaveOver
    }
    public ReactiveProperty<int> shiftNumber { get; } = new ReactiveProperty<int>();
    public ReactiveProperty<int> completedOrderCount { get; } = new ReactiveProperty<int>();
    public ReactiveProperty<int> requiredOrderCount { get; } = new ReactiveProperty<int>();
    public ReactiveProperty<int> depositedAmount { get; } = new ReactiveProperty<int>();
    public ReactiveProperty<int> quotaAmount { get; } = new ReactiveProperty<int>();
    public ReactiveProperty<float> shiftTimer { get; } = new ReactiveProperty<float>();
    public ReactiveProperty<float> currentClockHour { get; } = new ReactiveProperty<float>();
    public ReactiveProperty<ShiftState> currentState { get; } = new ReactiveProperty<ShiftState>();
    public ReplaySubject<Unit> OnGameStart { get; } = new ReplaySubject<Unit>(1);
    public bool IsAfterShiftReadyForNextShift => afterShiftReadyForNextShift;
    private readonly IQuestService questService;
    private readonly IOrderManager orderManager;
    private readonly IInventorySystem inventorySystem;
    private readonly CompositeDisposable updateDisposible = new CompositeDisposable();
    private readonly CompositeDisposable disposables = new CompositeDisposable();
    private readonly CompositeDisposable systemDisposables = new CompositeDisposable();
    private ShiftData.Shift activeShift;
    private float shiftElapsedSeconds;
    private float overtimeElapsedSeconds;
    private CancellationTokenSource debtCollectionCts;
    private IDisposable playerDeathSubscription;
    private bool afterShiftReadyForNextShift;
    private bool isDialoguePaused;

    public ShiftSystem(IQuestService questService, IOrderManager orderManager, IInventorySystem inventorySystem)
    {
        this.questService = questService;
        this.orderManager = orderManager;
        this.inventorySystem = inventorySystem;
    }

    public async UniTask Init()
    {
        await SubscribeToDialogueEvents();
        SubscribeToPlayerDeathAsync().Forget();
    }

    private async UniTask SubscribeToDialogueEvents()
    {
        var dialogueService = await ServiceLocator.Instance.GetAsync<IDialogueService>();
        if (dialogueService != null)
        {
            dialogueService.onBeginScenario.Subscribe(_ => isDialoguePaused = true).AddTo(systemDisposables);
            dialogueService.onEndScenario.Subscribe(_ => isDialoguePaused = false).AddTo(systemDisposables);
        }
    }

    public void StartGame()
    {
        ResetGame();
        orderManager.OnOrderServed.Subscribe(_ =>
        {
            completedOrderCount.Value++;
        }).AddTo(disposables);
        OnGameStart.OnNext(Unit.Default);
        StartShift(0);
    }

    public void ResetGame()
    {
        shiftElapsedSeconds = 0f;
        overtimeElapsedSeconds = 0f;
        shiftNumber.Value = 0;
        completedOrderCount.Value = 0;
        requiredOrderCount.Value = 0;
        depositedAmount.Value = 0;
        quotaAmount.Value = 0;
        shiftTimer.Value = 0f;
        currentClockHour.Value = Database.Instance.shiftData.workDayStartHour;
        currentState.Value = ShiftState.None;
        updateDisposible.Clear();
        disposables.Clear();
        debtCollectionCts?.Cancel();
        ResetWalletToStartingDebt();
        ShopSystem.Instance?.SetStoreAvailability(false);
        afterShiftReadyForNextShift = false;

        if (TaskManager.Instance != null)
        {
            TaskManager.Instance.RemoveTask("QuotaTask");
            TaskManager.Instance.ClearTasks();
        }
    }

    private async UniTaskVoid SubscribeToPlayerDeathAsync()
    {
        await UniTask.WaitUntil(() => PlayerStatSystem.Instance != null);
        playerDeathSubscription?.Dispose();
        playerDeathSubscription = PlayerStatSystem.Instance.OnPlayerDeath
            .Subscribe(_ => ApplyPlayerDeathPenalty());
    }

    public void StartNextShift()
    {
        if (currentState.Value != ShiftState.AfterShift && currentState.Value != ShiftState.None)
        {
            WorldBroadcastSystem.Instance.Broadcast("You can't start the next shift until you've clocked out.", 4f);
            return;
        }

        if (shiftNumber.Value + 1 >= Database.Instance.shiftData.shifts.Length)
        {
            EndGame(true);
            return;
        }
        StartShift(shiftNumber.Value + 1);
    }

    public void RestartCurrentShift()
    {
        RestartShift(shiftNumber.Value);
    }

    public void RestartShift(int shiftIndex)
    {
        var shiftData = Database.Instance?.shiftData;
        if (shiftData == null || shiftData.shifts == null || shiftData.shifts.Length == 0)
        {
            Debug.LogWarning("[ShiftSystem] Cannot restart shift because shift data is missing.");
            return;
        }

        var clampedIndex = Mathf.Clamp(shiftIndex, 0, shiftData.shifts.Length - 1);
        StartShift(clampedIndex);
    }

    public void EnterAfterShiftState(bool markShiftCompleted = false)
    {
        updateDisposible.Clear();
        debtCollectionCts?.Cancel();
        orderManager.ClearOrders();

        // Remove quota task if it wasn't completed (if it was completed, it will remove itself after delay)
        if (TaskManager.Instance != null)
        {
            if (!HasMetQuota())
            {
                TaskManager.Instance.RemoveTask("QuotaTask");
            }
            // Clear completed tasks from UI so they don't persist on the After Shift screen
            TaskManager.Instance.ClearCompletedTasks();
        }

        if (currentState.Value == ShiftState.AfterShift)
        {
            ShopSystem.Instance?.SetStoreAvailability(true);
            afterShiftReadyForNextShift = markShiftCompleted || afterShiftReadyForNextShift;
            return;
        }

        currentState.Value = ShiftState.AfterShift;
        ShopSystem.Instance?.SetStoreAvailability(true);
        afterShiftReadyForNextShift = markShiftCompleted;
    }

    public void ExitAfterShiftState()
    {
        if (currentState.Value != ShiftState.AfterShift)
        {
            ShopSystem.Instance?.SetStoreAvailability(false);
            return;
        }

        currentState.Value = ShiftState.None;
        ShopSystem.Instance?.SetStoreAvailability(false);
        afterShiftReadyForNextShift = false;
    }

    public bool ForceCompleteActiveShift(bool autoCompleteActiveQuest = true)
    {
        if (currentState.Value != ShiftState.InShift && currentState.Value != ShiftState.Overtime)
        {
            return false;
        }

        var shiftData = Database.Instance?.shiftData;
        if (shiftData == null)
        {
            Debug.LogWarning("[ShiftSystem] Cannot fast-forward shift because ShiftData is missing.");
            return false;
        }

        shiftElapsedSeconds = shiftData.shiftDuration;
        shiftTimer.Value = 0f;
        completedOrderCount.Value = Mathf.Max(completedOrderCount.Value, requiredOrderCount.Value);

        if (quotaAmount.Value > 0)
        {
            depositedAmount.Value = Mathf.Max(depositedAmount.Value, quotaAmount.Value);
        }

        if (autoCompleteActiveQuest && activeShift != null && !string.IsNullOrWhiteSpace(activeShift.questId))
        {
            questService?.CompleteQuest(activeShift.questId);
        }

        CompleteShift();
        return true;
    }

    public bool ForceFastForwardTimer()
    {
        var targetRemainTime = 3f;
        if (currentState.Value != ShiftState.InShift && currentState.Value != ShiftState.Overtime)
        {
            return false;
        }

        var shiftData = Database.Instance?.shiftData;
        if (shiftData == null)
        {
            Debug.LogWarning("[ShiftSystem] Cannot fast-forward shift because ShiftData is missing.");
            return false;
        }

        shiftElapsedSeconds = Mathf.Max(0, shiftData.shiftDuration - targetRemainTime);
        shiftTimer.Value = Mathf.Min(targetRemainTime, shiftData.shiftDuration);

        return true;
    }

    public bool IsCurrentShiftQuestCompleted()
    {
        var s = GetCurrentShift();
        if (s == null)
            return false;

        var questId = s.questId;
        if (string.IsNullOrEmpty(questId))
            return true;

        return questService.GetQuestStatus(questId) == QuestStatus.Completed;
    }

    public bool TryDeposit(int amount)
    {
        var deposited = DepositInternal(Mathf.Max(1, amount));
        return deposited > 0;
    }

    public int DepositAllAvailableFunds()
    {
        var stats = PlayerStatSystem.Instance;
        var available = stats == null ? 0 : Mathf.Max(0, stats.Money.Value);
        if (available <= 0)
        {
            return DepositInternal(available);
        }

        if (quotaAmount.Value <= 0)
        {
            return DepositInternal(available);
        }

        var remainingToQuota = Mathf.Max(0, quotaAmount.Value - depositedAmount.Value);
        if (remainingToQuota <= 0)
        {
            WorldBroadcastSystem.Instance.Broadcast("Quota already met. Clock out!", 4f);
            return 0;
        }

        var request = Mathf.Min(available, remainingToQuota);
        return DepositInternal(request);
    }

    public bool HasMetQuota()
    {
        if (quotaAmount.Value <= 0)
            return true;
        return depositedAmount.Value >= quotaAmount.Value;
    }

    public bool HasTasksRequiredBeforeShiftEnds()
    {
        return TaskManager.Instance.Tasks.Value.Any(x => !x.IsCompleted && x.dueBeforeShiftEnds);
    }

    public bool HasTasksRequiredBeforeShiftStarts()
    {
        return TaskManager.Instance.Tasks.Value.Any(x => !x.IsCompleted && x.dueBeforeShiftStarts);
    }

    private int DepositInternal(int requestedAmount)
    {
        if (!CanInteractWithDepositBox())
            return 0;

        var stats = PlayerStatSystem.Instance;
        if (stats == null)
            return 0;

        var available = Mathf.Max(0, stats.Money.Value);
        if (available == 0)
        {
            WorldBroadcastSystem.Instance.Broadcast("You don't have any money to deposit.", 3f);
            return 0;
        }

        var depositAmount = Mathf.Clamp(requestedAmount, 1, available);
        if (quotaAmount.Value > 0)
        {
            var remainingToQuota = Mathf.Max(0, quotaAmount.Value - depositedAmount.Value);
            if (remainingToQuota <= 0)
            {
                WorldBroadcastSystem.Instance.Broadcast("Quota already met. Clock out!", 4f);
                return 0;
            }

            depositAmount = Mathf.Min(depositAmount, remainingToQuota);
        }

        if (depositAmount <= 0)
        {
            return 0;
        }

        stats.Money.Value -= depositAmount;
        depositedAmount.Value += depositAmount;

        WorldBroadcastSystem.Instance.Broadcast($"Deposited ${depositAmount}. ({depositedAmount.Value}/{quotaAmount.Value})", 4f);

        return depositAmount;
    }

    private bool CanInteractWithDepositBox()
    {
        return currentState.Value == ShiftState.InShift || currentState.Value == ShiftState.Overtime;
    }

    private void StartShift(int num)
    {
        var data = Database.Instance.shiftData;
        activeShift = data.GetShiftByNumber(num);
        if (activeShift == null)
        {
            Debug.LogError($"Shift {num} is not defined.");
            return;
        }

        orderManager.ClearOrders();
        updateDisposible.Clear();
        debtCollectionCts?.Cancel();

        if (TaskManager.Instance != null)
        {
            TaskManager.Instance.ClearCompletedTasks();
            // Ensure QuotaTask history is cleared so it doesn't auto-complete in the new shift
            TaskManager.Instance.RemoveFromHistory("QuotaTask");
        }

        shiftElapsedSeconds = 0f;
        overtimeElapsedSeconds = 0f;
        shiftNumber.Value = num;
        currentState.Value = ShiftState.InShift;
        shiftTimer.Value = data.shiftDuration;
        currentClockHour.Value = data.workDayStartHour;
        completedOrderCount.Value = 0;
        requiredOrderCount.Value = activeShift.requiredOrdersCount;
        depositedAmount.Value = 0;
        quotaAmount.Value = Mathf.Max(0, activeShift.quotaAmount);
        afterShiftReadyForNextShift = false;

        if (!string.IsNullOrEmpty(activeShift.questId))
        {
            questService.StartQuest(activeShift.questId);
        }

        ShopSystem.Instance.RefreshShopItems();
        ShopSystem.Instance?.SetStoreAvailability(false);

        Observable.EveryUpdate()
            .Subscribe(_ => TickShift(Time.deltaTime))
            .AddTo(updateDisposible);

        WorldBroadcastSystem.Instance.Broadcast("Clock in! Serve orders and deposit cash to hit quota.", 6f);

        // Track Quota Task
        depositedAmount.Subscribe(_ => UpdateQuotaTask()).AddTo(updateDisposible);
        quotaAmount.Subscribe(_ => UpdateQuotaTask()).AddTo(updateDisposible);
    }

    private void UpdateQuotaTask()
    {
        if (TaskManager.Instance == null) return;

        if (quotaAmount.Value <= 0)
        {
            TaskManager.Instance.RemoveTask("QuotaTask");
            return;
        }

        // Always update the description first so it shows the correct amount
        var task = new TaskManager.TaskData()
        {
            Id = "QuotaTask",
            Description = $"Quota: ${depositedAmount.Value}/{quotaAmount.Value}"
        };
        TaskManager.Instance.AddTask(task);

        if (HasMetQuota())
        {
            TaskManager.Instance.CompleteTask("QuotaTask");
        }
    }

    private void TickShift(float deltaTime)
    {
        if (currentState.Value != ShiftState.InShift && currentState.Value != ShiftState.Overtime)
            return;

        if (isDialoguePaused)
            return;

        shiftElapsedSeconds += deltaTime;

        if (currentState.Value == ShiftState.InShift)
        {
            var remain = Mathf.Max(0f, Database.Instance.shiftData.shiftDuration - shiftElapsedSeconds);
            shiftTimer.Value = remain;

            if (remain <= 0f)
            {
                if (HasMetQuota() && !HasTasksRequiredBeforeShiftEnds())
                {
                    CompleteShift();
                }
                else
                {
                    EnterOvertime();
                }
                return;
            }
        }
        else
        {
            overtimeElapsedSeconds += deltaTime;
            if (HasMetQuota() && !HasTasksRequiredBeforeShiftEnds())
            {
                CompleteShift();
                return;
            }
        }

        UpdateClockHour();
    }

    private void UpdateClockHour()
    {
        var data = Database.Instance.shiftData;
        var workHours = Mathf.Max(1f, data.workDayEndHour - data.workDayStartHour);
        var elapsedForClock = Mathf.Min(shiftElapsedSeconds, data.shiftDuration);
        var normalized = elapsedForClock / Mathf.Max(0.0001f, data.shiftDuration);
        var clock = data.workDayStartHour + workHours * normalized;

        if (currentState.Value == ShiftState.Overtime && shiftElapsedSeconds > data.shiftDuration)
        {
            var hoursPerSecond = workHours / Mathf.Max(0.0001f, data.shiftDuration);
            var overtimeHours = (shiftElapsedSeconds - data.shiftDuration) * hoursPerSecond;
            clock = data.workDayEndHour + overtimeHours;
        }

        currentClockHour.Value = clock;
    }

    private void EnterOvertime()
    {
        shiftTimer.Value = 0f;
        currentState.Value = ShiftState.Overtime;
        WorldBroadcastSystem.Instance.Broadcast("5pm hit! Satan starts collecting if you miss quota or fail to complete your tasks.", 6f);
        BeginDebtCollectionLoop();
    }

    private void BeginDebtCollectionLoop()
    {
        debtCollectionCts?.Cancel();
        debtCollectionCts = new CancellationTokenSource();
        RunDebtCollectionAsync(debtCollectionCts.Token).Forget();
    }

    private async UniTaskVoid RunDebtCollectionAsync(CancellationToken token)
    {
        var data = Database.Instance.shiftData;
        var interval = Mathf.Max(0.5f, data.debtCollectionInterval);
        var growth = Mathf.Max(1.01f, data.debtTickGrowthMultiplier);
        var baseAmount = Mathf.Max(1, data.baseDebtTickAmount);
        var multiplier = activeShift != null ? Mathf.Max(0.1f, activeShift.debtPressureMultiplier) : 1f;
        var tick = 0;

        try
        {
            while (!token.IsCancellationRequested && currentState.Value == ShiftState.Overtime && !HasMetQuota())
            {
                await UniTask.Delay(TimeSpan.FromSeconds(interval), cancellationToken: token);
                tick++;
                var scaled = Mathf.CeilToInt(baseAmount * multiplier * Mathf.Pow(growth, tick - 1));
                ApplyDebtTick(scaled);
            }
        }
        catch (OperationCanceledException)
        {
            // ignored
        }
    }

    private void ApplyDebtTick(int amount)
    {
        if (amount <= 0)
            return;

        var stats = PlayerStatSystem.Instance;
        if (stats == null)
            return;

        stats.Money.Value -= amount;
        WorldBroadcastSystem.Instance.Broadcast($"Satan collects ${amount}.", 4f);

        if (stats.Money.Value <= Database.Instance.shiftData.maxNegativeDebt)
        {
            TriggerShiftFailure();
        }
    }

    private void CompleteShift()
    {
        EnterAfterShiftState(markShiftCompleted: true);
        WorldBroadcastSystem.Instance.Broadcast("Quota met! Shift complete.", 6f);
    }

    private void TriggerShiftFailure()
    {
        updateDisposible.Clear();
        debtCollectionCts?.Cancel();
        orderManager.ClearOrders();
        currentState.Value = ShiftState.GaveOver;
        WorldBroadcastSystem.Instance.Broadcast("Satan collected too much. Day reset!", 6f);

        // Wait for the message to be read before resetting
        UniTask.Delay(TimeSpan.FromSeconds(6f))
            .ContinueWith(HandleDayLoss)
            .Forget();

        ShopSystem.Instance?.SetStoreAvailability(false);
    }

    private void HandleDayLoss()
    {
        ResetWalletToStartingDebt();
        inventorySystem?.ClearInventory();
        depositedAmount.Value = 0;
        quotaAmount.Value = 0;
        completedOrderCount.Value = 0;
        requiredOrderCount.Value = 0;
        shiftNumber.Value = 0;
        shiftTimer.Value = Database.Instance.shiftData.shiftDuration;
        ShopSystem.Instance?.SetStoreAvailability(false);

        // restart the day automatically
        StartShift(0);
    }

    private void ResetWalletToStartingDebt()
    {
        var stats = PlayerStatSystem.Instance;
        if (stats == null)
            return;

        stats.Money.Value = Database.Instance.shiftData.startingDebt;
    }

    public void EndGame(bool isSuccess)
    {
        if (isSuccess)
        {
            updateDisposible.Clear();
            debtCollectionCts?.Cancel();
            currentState.Value = ShiftState.GaveOver;
            WorldBroadcastSystem.Instance.Broadcast("All shifts complete!", 6f);
            ShopSystem.Instance?.SetStoreAvailability(false);
            return;
        }

        TriggerShiftFailure();
    }

    private ShiftData.Shift GetCurrentShift()
    {
        return Database.Instance.shiftData.GetShiftByNumber(shiftNumber.Value);
    }

    private void ApplyPlayerDeathPenalty()
    {
        inventorySystem?.ClearInventory();

        var stats = PlayerStatSystem.Instance;
        var data = Database.Instance?.shiftData;
        if (stats == null || data == null)
        {
            return;
        }

        var currentMoney = stats.Money.Value;
        if (currentMoney < 0)
        {
            return;
        }

        var percent = Mathf.Clamp01(data.deathMoneyLossPercent);
        if (percent <= 0f || currentMoney == 0)
        {
            return;
        }

        var loss = Mathf.CeilToInt(currentMoney * percent);
        loss = Mathf.Clamp(loss, 0, currentMoney);
        if (loss <= 0)
        {
            return;
        }

        stats.Money.Value -= loss;
        WorldBroadcastSystem.Instance.Broadcast($"You lost ${loss} when you died.", 4f);
    }

    public void Dispose()
    {
        debtCollectionCts?.Dispose();
        playerDeathSubscription?.Dispose();
        updateDisposible.Dispose();
        disposables.Dispose();
        systemDisposables.Dispose();
    }
}
