using UnityEngine;

public class Customer : MonoBehaviour, IInteractable
{
    public string customerName;

    public void Interact()
    {
        Debug.Log($"interact with customer {customerName}");
    }
}