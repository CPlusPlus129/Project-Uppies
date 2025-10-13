using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

public class ShiftSystem : IShiftSystem
{
    public enum ShiftState
    {
        None = 0,
        InShift,
        AfterShift,
        GaveOver
    }
    public ReactiveProperty<int> shiftNumber { get; } = new ReactiveProperty<int>();
    public ReactiveProperty<int> completedOrderCount { get; } = new ReactiveProperty<int>();
    public ReactiveProperty<int> requiredOrderCount { get; } = new ReactiveProperty<int>();
    public ReactiveProperty<float> shiftTimer { get; } = new ReactiveProperty<float>();
    public ReactiveProperty<ShiftState> currentState { get; } = new ReactiveProperty<ShiftState>();
    public ReplaySubject<Unit> OnGameStart { get; } = new ReplaySubject<Unit>(1);
    private readonly IQuestService questService;
    private readonly IOrderManager orderManager;
    private bool hasRunTutorial = false;
    private CompositeDisposable updateDisposible = new CompositeDisposable();
    private CompositeDisposable disposables = new CompositeDisposable();

    public ShiftSystem(IQuestService questService, IOrderManager orderManager)
    {
        this.questService = questService;
        this.orderManager = orderManager;
    }

    public async UniTask Init()
    {
        await UniTask.CompletedTask;
    }

    public void StartGame()
    {
        ResetGame();
        orderManager.OnOrderServed.Subscribe(_ =>
        {
            completedOrderCount.Value++;
        }).AddTo(disposables);
        OnGameStart.OnNext(Unit.Default);
        if (!hasRunTutorial)
        {
            //should do some dialogues first?
            StartShift(0);
            hasRunTutorial = true;
        }
        else
            StartShift(1);
    }

    public void ResetGame()
    {
        shiftNumber.Value = completedOrderCount.Value = requiredOrderCount.Value = 0;
        shiftTimer.Value = 0f;
        currentState.Value = ShiftState.AfterShift;
        hasRunTutorial = false;
        updateDisposible.Clear();
        disposables.Clear();
    }

    public void StartNextShift()
    {
        if (shiftNumber.Value + 1 >= Database.Instance.shiftData.shifts.Length)
        {
            EndGame(true);
            return;
        }
        StartShift(shiftNumber.Value + 1);
    }

    public void EndGame(bool isSuccess)
    {
        Debug.Log($"EndGame. success: {isSuccess}");
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

    private void StartShift(int num)
    {
        var s = Database.Instance.shiftData.GetShiftByNumber(num);
        currentState.Value = ShiftState.InShift;
        shiftNumber.Value = num;
        shiftTimer.Value = Database.Instance.shiftData.shiftDuration;
        completedOrderCount.Value = 0;
        requiredOrderCount.Value = s.requiredOrdersCount;
        //quest
        if (!string.IsNullOrEmpty(s.questId))
        {
            questService.StartQuest(s.questId);
        }
        //shop
        ShopSystem.Instance.RefreshShopItems();

        Observable.EveryUpdate()
            .Where(_ => currentState.Value == ShiftState.InShift)
            .Subscribe(_ =>
            {
                shiftTimer.Value -= Time.deltaTime;
                if (shiftTimer.Value <= 0f)
                {
                    shiftTimer.Value = 0f;
                    RunAfterShift();
                }
            }).AddTo(updateDisposible);
        WorldBroadcastSystem.Instance.Broadcast("Get order from customers.", 60f);
    }

    private void RunAfterShift()
    {
        updateDisposible.Clear();
        orderManager.ClearOrders();
        var passed = CheckShiftRequirementsMet();
        if (!passed)
        {
            EndGame(false);
            return;
        }
        currentState.Value = ShiftState.AfterShift;
    }

    private bool CheckShiftRequirementsMet()
    {
        var s = GetCurrentShift();
        return completedOrderCount.Value >= s.requiredOrdersCount;
    }

    private ShiftData.Shift GetCurrentShift()
    {
        return Database.Instance.shiftData.GetShiftByNumber(shiftNumber.Value);
    }
}