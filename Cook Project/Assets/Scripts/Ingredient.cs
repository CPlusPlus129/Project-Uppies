public class Ingredient : ItemBase, IInteractable
{
    public void Interact()
    {
    }

    public void ToggleOutline(bool isOn)
    {
        // Ingredient items don't need outline functionality
        // as they're picked up rather than interacted with in place
    }
}
