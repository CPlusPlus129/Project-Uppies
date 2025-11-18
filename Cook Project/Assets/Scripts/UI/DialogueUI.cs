using Cysharp.Threading.Tasks;
using DialogueModule;
using UnityEngine.InputSystem;

class DialogueUI : DialogueUIManager, IUIInitializable
{
    private void OnEnable()
    {
        InputManager.Instance.PushActionMap("Dialogue");
    }

    private void OnDisable()
    {
        InputManager.Instance.PopActionMap("Dialogue");
    }

    async UniTask IUIInitializable.Init()
    {
        Init();
        InputSystem.actions.FindActionMap("Dialogue").FindAction("Next").performed += Next;
        await UniTask.CompletedTask;
    }

    #region Input Actions
    private void Next(InputAction.CallbackContext cxt)
    {
        engine.adapter.OnNext();
    }
    #endregion
}