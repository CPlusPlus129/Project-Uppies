using UnityEngine;

public class Meal : ItemBase, IInteractable
{
    public float Quality { get; private set; } = 1f;

    public void SetQuality(float value)
    {
        Quality = Mathf.Clamp01(value);
    }

    public void Interact()
    {
    }

    public void ToggleOutline(bool isOn)
    {
        // Meal items don't need outline functionality
        // as they're picked up rather than interacted with in place
    }
}
