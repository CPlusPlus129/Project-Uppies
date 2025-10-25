using Cysharp.Threading.Tasks;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class HUD : MonoBehaviour, IUIInitializable
{
    [SerializeField] private TextMeshProUGUI aimHint;

    public async UniTask Init()
    {
        var actionName = "Interact";
        var interactKey = InputManager.Instance.GetBindingDisplayString(actionName, "keyboard&mouse");
        var displayString = $"[ {interactKey} ] {actionName}";
        PlayerStatSystem.Instance.CurrentInteractableTarget.Subscribe(interactable =>
        {
            if (interactable != null)
            {
                aimHint.text = displayString;
                aimHint.gameObject.SetActive(true);
            }
            else
            {
                aimHint.gameObject.SetActive(false);
            }
        }).AddTo(this);
        await UniTask.CompletedTask;
    }

}