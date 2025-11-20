using UnityEngine;

public class KeyPadGameTrigger : InteractableBase
{
    [SerializeField]
    private SecretWall wall;

    private bool isActive;

    public override void Interact()
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
