using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private PlayerMotor motor;
    [SerializeField] private PlayerLook camlook;
    [SerializeField] private PlayerInteract interact;
    private PlayerActionController actionController = new PlayerActionController();
    private InputAction lookAction;
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction interactAction;

    private void Awake()
    {
        Debug.Log("Set audiolistener volume to 0.1");
        AudioListener.volume = 0.1f;

        lookAction = InputSystem.actions.FindAction("Look");
        moveAction = InputSystem.actions.FindAction("Move");
        jumpAction = InputSystem.actions.FindAction("Jump");
        interactAction = InputSystem.actions.FindAction("Interact");
        //CallBack Context trigger (when the jump performed)
        jumpAction.performed += ctx => motor.Jump();
        interactAction.performed += ctx => interact.Interact(camlook.cam);

        var discardAction = InputSystem.actions.FindAction("Discard");
        discardAction.performed += ctx => actionController.DropItem();
        var scrollAction = InputSystem.actions.FindAction("Scroll");
        scrollAction.performed += actionController.ScrollHotBar;
        var hotbarAction = InputSystem.actions.FindAction("HotbarShortcut");
        hotbarAction.performed += actionController.OnItemHotbarClicked;
        var sprintAction = InputSystem.actions.FindAction("Sprint");
        sprintAction.performed += ctx => motor.TrySprint();
        sprintAction.canceled += ctx => motor.StopSprint();
        //DEBUG
        var debugAction = InputSystem.actions.FindAction("DebugKeys");
        debugAction.performed += actionController.OnDebugKeyClicked;
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