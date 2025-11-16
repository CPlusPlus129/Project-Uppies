using System;
using UnityEngine;

[CreateAssetMenu(fileName = "ShiftData", menuName = "ScriptableObjects/ShiftData")]
public class ShiftData : ScriptableObject
{
    [Header("Time Settings")]
    [Tooltip("Real-time seconds that represent the entire 9am-5pm shift window.")]
    public float shiftDuration = 300f;
    [Tooltip("Hour of day the shift starts.")]
    public float workDayStartHour = 9f;
    [Tooltip("Hour of day the shift should nominally end (ex: 17 == 5pm).")]
    public float workDayEndHour = 17f;

    [Header("Quota / Deposit Settings")]
    [Tooltip("Default amount pulled per deposit interaction when no custom amount is provided.")]
    public int defaultDepositChunk = 50;

    [Header("Debt Settings")]
    [Tooltip("Wallet balance the player begins each day with (should be negative per design).")]
    public int startingDebt = -66;
    [Tooltip("Lowest amount of money the player can reach before the day is lost.")]
    public int maxNegativeDebt = -666;
    [Tooltip("Seconds between each Satan debt collection tick once overtime begins.")]
    public float debtCollectionInterval = 5f;
    [Tooltip("Base amount removed each debt tick before multipliers are applied.")]
    public int baseDebtTickAmount = 5;
    [Tooltip("Multiplier applied each tick so the amount ramps up over time ( > 1 ).")]
    public float debtTickGrowthMultiplier = 1.2f;

    [Header("Death Penalties")]
    [Tooltip("Percent of current positive money lost when the player dies.")]
    [Range(0f, 1f)] public float deathMoneyLossPercent = 0.5f;

    public Shift[] shifts;
    public Shift GetShiftByNumber(int number)
    {
        if (number < 0 || number >= shifts.Length)
        {
            Debug.LogError("Shift number out of range");
            return null;
        }

        return shifts[number];
    }

    [Serializable]
    public class Shift
    {
        [Tooltip("Optional count of orders to complete for this shift (legacy support).")]
        public int requiredOrdersCount;
        [Tooltip("Money quota that must be deposited to clear this shift.")]
        public int quotaAmount = 500;
        [Tooltip("Does this shift include a VIP whose request must be satisfied to pass the day?")]
        public bool requiresVipCustomer = false;
        [Tooltip("Quest started automatically when the shift begins (empty for none).")]
        public string questId;
        [Tooltip("Multiplier applied to debt tick amounts for this specific shift.")]
        public float debtPressureMultiplier = 1f;
    }
}
