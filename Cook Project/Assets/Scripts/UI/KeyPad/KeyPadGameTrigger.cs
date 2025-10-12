using UnityEngine;

public class KeyPadGameTrigger : MonoBehaviour, IInteractable
{
    [SerializeField]
    private KeyPadGameUI keyPadUI;

    private bool isActive;

    public void Interact()
    {
        if (isActive) return;
        if (keyPadUI == null) { Debug.LogWarning("KeyPadGameUI is not assigned."); return; }

        isActive = true;
        keyPadUI.OpenPuzzle(() => { isActive = false; });
    }
}
