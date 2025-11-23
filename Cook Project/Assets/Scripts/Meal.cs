using UnityEngine;

public class Meal : PickupableItem
{
    public float Quality { get; private set; } = 1f;

    public void SetQuality(float value)
    {
        Quality = Mathf.Clamp01(value);
    }
}
