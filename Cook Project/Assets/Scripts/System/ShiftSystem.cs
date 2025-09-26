using System.Collections.Generic;
using R3;
using UnityEngine;

public class ShiftSystem : SimpleSingleton<ShiftSystem>
{
    public enum ShiftState
    {
        None = 0,
        InShift,
        AfterShift,
        GaveOver
    }
    public ReactiveProperty<int> shiftNumber = new ReactiveProperty<int>();
    public ReactiveProperty<int> completedOrderCount = new ReactiveProperty<int>();
    public ReactiveProperty<int> requiredOrderCount = new ReactiveProperty<int>();
    public ReactiveProperty<float> shiftTimer = new ReactiveProperty<float>();
    public ReactiveProperty<string> specialQuest = new ReactiveProperty<string>();
    public ReactiveProperty<ShiftState> currentState = new ReactiveProperty<ShiftState>();
    private bool hasRunTutorial = false;
    private List<string> completedQuest = new List<string>(); //might need a quest system?
    private CompositeDisposable updateDisposible = new CompositeDisposable();
    private CompositeDisposable disposables = new CompositeDisposable();

    public void StartGame()
    {
        ResetGame();
        OrderManager.Instance.OnOrderServed.Subscribe(_ =>
        {
            completedOrderCount.Value++;
        }).AddTo(disposables);
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
        specialQuest.Value = "";
        currentState.Value = ShiftState.AfterShift;
        hasRunTutorial = false;
        completedQuest.Clear();
        updateDisposible.Clear();
        disposables.Clear();
    }

    public void StartNextShift()
    {
        if(shiftNumber.Value + 1 >= Database.Instance.shiftData.shifts.Length)
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

    private void StartShift(int shift)
    {
        var s = Database.Instance.shiftData.GetShiftByNumber(shift);
        currentState.Value = ShiftState.InShift;
        shiftNumber.Value = shift;
        shiftTimer.Value = Database.Instance.shiftData.shiftDuration;
        requiredOrderCount.Value = s.requiredOrdersCount;
        specialQuest.Value = s.specialQuest;
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
    }

    private void RunAfterShift()
    {
        updateDisposible.Clear();
        var passed = CheckShiftRequirementsMet();
        if (!passed)
        {
            EndGame(false);
            return;
        }
        currentState.Value = ShiftState.AfterShift;
        //TODO design ig
        var s = Database.Instance.shiftData.GetShiftByNumber(shiftNumber.Value);
        if (!string.IsNullOrEmpty(s.specialQuest))
        {
            RunSpecialQuest(s.specialQuest);
        }
    }

    private void RunSpecialQuest(string questName)
    {
        //Run puzzle
    }

    private bool CheckShiftRequirementsMet()
    {
        var s = Database.Instance.shiftData.GetShiftByNumber(shiftNumber.Value);
        return completedOrderCount.Value >= s.requiredOrdersCount;
    }
}