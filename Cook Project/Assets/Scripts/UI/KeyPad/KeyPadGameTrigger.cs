using UnityEngine;

public class KeyPadGameTrigger : MonoBehaviour, IInteractable
{
    [SerializeField]
    private SecretWall wall;

    private bool isActive;

    public void Interact()
    {
        if (isActive) return;
        var keyPadUI = UIRoot.Instance.GetUIComponent<KeyPadGameUI>();
        if (keyPadUI == null) { Debug.LogWarning("KeyPadGameUI is not assigned."); return; }

        isActive = true;
        keyPadUI.OpenPuzzle(OnSuccess, OnCloseUI);
    }

    private void OnSuccess()
    {
        wall.SetOpen(true);
    }

    private void OnCloseUI()
    {
        isActive = false;
    }
}
