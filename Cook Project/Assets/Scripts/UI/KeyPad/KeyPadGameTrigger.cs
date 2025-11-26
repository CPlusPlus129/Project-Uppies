using System.Linq;
using UnityEngine;

public class KeyPadGameTrigger : InteractableBase
{
    [SerializeField] private string requiredTaskId;
    [SerializeField] private string onSuccessSignalId;
    [SerializeField] private SecretWall wall;

    private bool isActive;

    public override void Interact()
    {
        if (isActive) return;

        if(!string.IsNullOrEmpty(requiredTaskId) && !TaskManager.Instance.Tasks.Value.Any(x => x.Id == requiredTaskId))
        {
            WorldBroadcastSystem.Instance.Broadcast("Pressed the button and nothing happened.");
            return;
        }

        var keyPadUI = UIRoot.Instance.GetUIComponent<KeyPadGameUI>();
        if (keyPadUI == null) { Debug.LogWarning("KeyPadGameUI is not assigned."); return; }

        isActive = true;
        keyPadUI.OpenPuzzle(OnSuccess, OnCloseUI);
    }

    private void OnSuccess()
    {
        wall?.SetOpen(true);
        if (!string.IsNullOrEmpty(requiredTaskId))
        {
            TaskManager.Instance.CompleteTask(requiredTaskId);
        }
        if(!string.IsNullOrEmpty(onSuccessSignalId))
        {
            GameFlow.Instance.Signal(onSuccessSignalId);
        }
    }

    private void OnCloseUI()
    {
        isActive = false;
    }
}
