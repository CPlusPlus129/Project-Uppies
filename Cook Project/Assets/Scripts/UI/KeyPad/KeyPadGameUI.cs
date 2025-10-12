using R3;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class KeyPadGameUI : MonoBehaviour
{
    public DigitalDisplay DigitalDisplay;

    private System.Action onCloseCallback;
    private System.Action onSuccessCallback;


    private void Awake()
    {
        var actions = InputSystem.actions;
        actions.FindActionMap("KeyPad").FindAction("Esc").performed += ctx => Close();

    }

    private void OnEnable()
    {
        Debug.Log("KeyPadGame Active");
        InputManager.Instance.PushActionMap("KeyPad");
        if (DigitalDisplay != null) DigitalDisplay.OnSolved += HandleSolved;
    }

    private void OnDisable()
    {
        InputManager.Instance.PopActionMap("KeyPad");
    }

    public void OpenPuzzle(System.Action onSuccess, System.Action onClosed)
    {
        onSuccessCallback = onSuccess;
        onCloseCallback = onClosed;
        gameObject.SetActive(true);
    }
    private void HandleSolved()
    {
        onSuccessCallback?.Invoke();
        Close();
    }

    private void Close()
    {
        gameObject.SetActive(false);
        onCloseCallback?.Invoke();
        onCloseCallback = null;
    }
}
