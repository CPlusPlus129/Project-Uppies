using UnityEngine;

public class CookingStation : MonoBehaviour, IInteractable
{
    public void Interact()
    {
        UIRoot.Instance.GetUIComponent<CookingUI>().Open();
    }
}