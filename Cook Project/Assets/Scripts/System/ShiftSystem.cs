using System.Collections.Generic;
using R3;
using UnityEngine;

public class ShiftSystem : SimpleSingleton<ShiftSystem>
{
    public enum ShiftState
    {
        Tutorial,
        InShift,
        AfterShift,
        GaveOver
    }
    public ReactiveProperty<int> shiftNumber = new ReactiveProperty<int>();
    public ReactiveProperty<int> ordersCompletedThisShift = new ReactiveProperty<int>();
    public ReactiveProperty<float> shiftTimer = new ReactiveProperty<float>();
    public ReactiveProperty<ShiftState> currentState = new ReactiveProperty<ShiftState>();
    private bool hasRunTutorial = false;
    private List<string> completedQuest = new List<string>(); //might need a quest system?
    private CompositeDisposable disposables = new CompositeDisposable();

    public void StartGame()
    {
        ResetGame();
        if (!hasRunTutorial)
            RunTutorial();
        else
            StartShift(1);
    }

    public void ResetGame()
    {
        shiftNumber.Value = ordersCompletedThisShift.Value = 0;
        shiftTimer.Value = 0f;
        currentState.Value = ShiftState.AfterShift;
        hasRunTutorial = false;
        completedQuest.Clear();
        disposables.Clear();
    }

    public void EndGame(bool isSuccess)
    {
        Debug.Log($"EndGame. success: {isSuccess}");
    }

    private void RunTutorial()
    {
        currentState.Value = ShiftState.Tutorial;
        hasRunTutorial = true;
    }

    private void StartShift(int shift)
    {
        currentState.Value = ShiftState.InShift;
        shiftNumber.Value = shift;
        shiftTimer.Value = Database.Instance.shiftData.shiftDuration;
        Observable.EveryUpdate()
            .Where(_ => currentState.Value == ShiftState.InShift)
            .Subscribe(_ =>
            {
                shiftTimer.Value -= Time.deltaTime;
                if (shiftTimer.Value <= 0f)
                {
                    RunAfterShift();
                }
            }).AddTo(disposables);
    }

    private void RunAfterShift()
    {
        disposables.Clear();
        var passed = CheckShiftRequirementsMet();
        if (!passed)
        {
            EndGame(false);
            return;
        }
        currentState.Value = ShiftState.AfterShift;
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
        return ordersCompletedThisShift.Value >= s.requiredOrdersCount;
    }
}