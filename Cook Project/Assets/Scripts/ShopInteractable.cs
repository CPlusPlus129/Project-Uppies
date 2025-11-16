using UnityEngine;

public class ShopInteractable : MonoBehaviour, IInteractable
{
    public void Interact()
    {
        if (!ShopSystem.Instance.IsStoreOpen)
        {
            WorldBroadcastSystem.Instance?.Broadcast("The shop is closed. Finish your shift first.", 4f);
            return;
        }

        UIRoot.Instance.GetUIComponent<ShopUI>().Open();
    }
}
