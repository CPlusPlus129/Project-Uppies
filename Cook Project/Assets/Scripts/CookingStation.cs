using UnityEngine;

public class CookingStation : InteractableBase
{
    public override void Interact()
    {
        UIRoot.Instance.GetUIComponent<CookingUI>().Open();
    }
}
