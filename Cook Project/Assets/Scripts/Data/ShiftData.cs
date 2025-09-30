using System;
using UnityEngine;

[CreateAssetMenu(fileName = "ShiftData", menuName = "ScriptableObjects/ShiftData")]
public class ShiftData : ScriptableObject
{
    public float shiftDuration = 300f; // 5 minutes
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
        public int requiredOrdersCount;
        //TODO quest datas
        public string questId;
        public string questName;
        public string questDescription;
    }
}
