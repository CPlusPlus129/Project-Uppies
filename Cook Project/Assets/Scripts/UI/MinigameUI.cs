using UnityEngine;
using UnityEngine.InputSystem;
using R3;

public class MinigameUI : MonoBehaviour
{
    public MinigamePanel minigamePanel;
    private void Awake()
    {
        var actions = InputSystem.actions;
        actions.FindAction("Esc").performed += ctx => Close();
        minigamePanel.OnCloseComplete.Subscribe(_ => OnPanelCloseComplete()).AddTo(this);
    }

    public void Open()
    {
        gameObject.SetActive(true);
        minigamePanel.Open();
        InputManager.Instance.PushActionMap("Minigame");
    }

    public void Close()
    {
        minigamePanel.Close();
        InputManager.Instance.PopActionMap("Minigame");
    }

    private void OnPanelCloseComplete()
    {
        gameObject.SetActive(false);
    }
}