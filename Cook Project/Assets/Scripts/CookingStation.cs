using UnityEngine;

public class CookingStation : MonoBehaviour, IInteractable
{
    public void Interact()
    {
        Debug.Log("interact with cook station");
        UIRoot.Instance.GetUIComponent<CookingUI>().Open();
    }
}