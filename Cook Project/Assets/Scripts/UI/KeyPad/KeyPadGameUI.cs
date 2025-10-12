using R3;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class KeyPadGameUI : MonoBehaviour
{
    public DigitalDisplay DigitalDisplay;

    private System.Action onCloseCallback;



    private void Awake()
    {
        var actions = InputSystem.actions;
        actions.FindActionMap("KeyPad").FindAction("Esc").performed += ctx => Close();

    }

    private void OnEnable()
    {
        Debug.Log("KeyPadGame Active");
        InputManager.Instance.PushActionMap("KeyPadGame");
        if (DigitalDisplay != null) DigitalDisplay.OnSolved += HandleSolved;
    }

    private void OnDisable()
    {
        InputManager.Instance.PopActionMap("KeyPadGame");
    }

    public void OpenPuzzle(System.Action onClosed)
    {
        onCloseCallback = onClosed;
        gameObject.SetActive(true);
    }
    private void HandleSolved()
    {
        // 成功后自动关
        Close();
    }

    private void Close()
    {
        gameObject.SetActive(false);
        onCloseCallback?.Invoke();
        onCloseCallback = null;
    }
}
