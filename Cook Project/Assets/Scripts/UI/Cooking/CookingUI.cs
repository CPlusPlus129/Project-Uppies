using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using R3;

public class CookingUI : MonoBehaviour
{
    public RecipeSelectionPanel selectionPanel;

    private void Awake()
    {
        var actions = InputSystem.actions;
        actions.FindAction("Esc").performed += ctx => Close();
        selectionPanel.OnCloseComplete.Subscribe(_ => OnPanelCloseComplete()).AddTo(this);
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
        selectionPanel.Open();
    }

    public void Close()
    {
        selectionPanel.Close();
    }

    private void OnPanelCloseComplete()
    {
        gameObject.SetActive(false);
    }
}