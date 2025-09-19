using UnityEngine;
using UnityEngine.InputSystem;

public class MinigameUI : MonoBehaviour
{
    public MinigamePanel minigamePanel;
    private void Awake()
    {
        var actions = InputSystem.actions;
        actions.FindAction("Esc").performed += ctx => Close();
    }

    private void OnEnable()
    {
        // Debug message
        Debug.Log("MinigameUI enabled");

        InputManager.Instance.PushActionMap("Minigame");
    }

    private void OnDisable()
    {
        InputManager.Instance.PopActionMap("Minigame");
    }

    public void Open()
    {
        gameObject.SetActive(true);
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }
}