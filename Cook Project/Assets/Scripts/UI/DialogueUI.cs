using Cysharp.Threading.Tasks;
using DialogueModule;

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
        await UniTask.CompletedTask;
    }
}