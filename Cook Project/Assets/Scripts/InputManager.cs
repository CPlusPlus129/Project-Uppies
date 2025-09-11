using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    private PlayerInput playerInput;
    private PlayerInput.OnFootActions onFoot;
    private PlayerMotor motor;
    private PlayerLook camlook;

    [SerializeField] private float interactDistance = 3f; //idk where to put so put this here for now

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        playerInput = new PlayerInput();
        onFoot = playerInput.OnFoot;
        motor = GetComponent<PlayerMotor>();
        camlook = GetComponent<PlayerLook>();
        //CallBack Context trigger (when the jump performed)
        onFoot.Jump.performed += onJump => motor.Jump();
        onFoot.Interact.performed += onInteract => OnInteract();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        motor.ProcessMove(onFoot.Movement.ReadValue<Vector2>());
    }

    void LateUpdate()
    {
        camlook.ProcessLook(onFoot.Look.ReadValue<Vector2>());
    }
    
    private void OnEnable()
    {
        onFoot.Enable();
    }

    private void OnDisable(){
        onFoot.Disable();
    }

    private void OnInteract()
    {
        if (!Physics.Raycast(camlook.cam.transform.position, camlook.cam.transform.forward, out RaycastHit hit, interactDistance))
            return;

        hit.collider.GetComponent<IInteractable>()?.Interact();
    }
}
