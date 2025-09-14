using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public PlayerMotor motor;
    public PlayerLook camlook;
    public PlayerInteract interact;
    private InputAction lookAction;
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction interactAction;

    private void Awake()
    {
        lookAction = InputSystem.actions.FindAction("Look");
        moveAction = InputSystem.actions.FindAction("Move");
        jumpAction = InputSystem.actions.FindAction("Jump");
        interactAction = InputSystem.actions.FindAction("Interact");
        //CallBack Context trigger (when the jump performed)
        jumpAction.performed += ctx => motor.Jump();
        interactAction.performed += ctx => interact.Interact();

        var scrollAction = InputSystem.actions.FindAction("Scroll");
        scrollAction.performed += ctx =>
        {
            var delta = ctx.ReadValue<Vector2>().y;
            if (delta > 0)
                InventorySystem.Instance.SelectSlot(InventorySystem.Instance.SelectedIndex.Value - 1);
            else if (delta < 0)
                InventorySystem.Instance.SelectSlot(InventorySystem.Instance.SelectedIndex.Value + 1);
        };
    }

    void Update()
    {
        var moveValue = moveAction.ReadValue<Vector2>();
        motor.ProcessMove(moveValue);
    }

    void LateUpdate()
    {
        var lookValue = lookAction.ReadValue<Vector2>();
        camlook.ProcessLook(lookValue);
    }
}