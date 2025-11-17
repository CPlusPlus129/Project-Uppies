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

    public void Open()
    {
        gameObject.SetActive(true);
        selectionPanel.Open();
        InputManager.Instance.PushActionMap("Cooking");
    }

    public void Close()
    {
        selectionPanel.Close();
        InputManager.Instance.PopActionMap("Cooking");
    }

    private void OnPanelCloseComplete()
    {
        gameObject.SetActive(false);
    }
}