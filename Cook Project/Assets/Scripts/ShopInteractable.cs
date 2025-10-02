using UnityEngine;

public class ShopInteractable : MonoBehaviour, IInteractable
{
    public void Interact()
    {
        UIRoot.Instance.GetUIComponent<ShopUI>().Open();
    }
}