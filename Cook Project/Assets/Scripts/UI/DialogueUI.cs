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
}