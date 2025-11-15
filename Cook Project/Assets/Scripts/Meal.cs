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
}
