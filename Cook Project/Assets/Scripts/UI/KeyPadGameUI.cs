using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class KeyPadGameUI : MonoBehaviour
{
    public DigitalDisplay DigitalDisplay;

    private void Awake()
    {
        var actions = InputSystem.actions;
        actions.FindActionMap("KeyPad").FindAction("Esc").performed += ctx => Close();


    }

    private void OnEnable()
    {
        Debug.Log("KeyPadGame Active");
        InputManager.Instance.PushActionMap("KeyPadGame");
    }

    private void OnDisable()
    {
        InputManager.Instance.PopActionMap("KeyPadGame");
    }

    private void Open()
    {
        gameObject.SetActive(true);
    }

    private void Close()
    {
        gameObject.SetActive(false);
    }
}
