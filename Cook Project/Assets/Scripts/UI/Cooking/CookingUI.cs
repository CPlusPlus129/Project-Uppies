using UnityEngine;
using UnityEngine.InputSystem;

public class CookingUI : MonoBehaviour
{
    public RecipeSelectionPanel selectionPanel;

    private void Awake()
    {
        var actions = InputSystem.actions;
        actions.FindAction("Esc").performed += ctx => Close();
    }

    private void OnEnable()
    {
        InputManager.Instance.PushActionMap("Cooking");
    }

    private void OnDisable()
    {
        InputManager.Instance.PopActionMap("Cooking");
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