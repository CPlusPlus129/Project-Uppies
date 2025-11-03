using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private PlayerMotor motor;
    [SerializeField] private PlayerLook camlook;
    [SerializeField] private PlayerInteract interact;
    [SerializeField] private Weapon lightGun;
    private PlayerActionController actionController = new PlayerActionController();
    private InputAction lookAction;
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction interactAction;
    private IInventorySystem inventorySystem;
    private CompositeDisposable disposables = new CompositeDisposable();

    private async void Awake()
    {
        //UIRoot.Instance.SetVisible(false);        

        SubscribeEvents();

        await UniTask.WaitUntil(() => GameFlow.Instance.IsInitialized);
        inventorySystem = await ServiceLocator.Instance.GetAsync<IInventorySystem>();
        actionController.inventorySystem = inventorySystem;
        interact.inventorySystem = inventorySystem;
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
    }

    void Update()
    {
        var lookValue = lookAction.ReadValue<Vector2>();
        camlook.ProcessLook(lookValue);

        interact.UpdateCurrentInteractableTarget(camlook.cam);

        var moveValue = moveAction.ReadValue<Vector2>();
        motor.ProcessMove(moveValue);
    }

    private void SubscribeEvents()
    {
        lookAction = InputSystem.actions.FindAction("Look");
        moveAction = InputSystem.actions.FindAction("Move");
        jumpAction = InputSystem.actions.FindAction("Jump");
        interactAction = InputSystem.actions.FindAction("Interact");
        jumpAction.performed += motor.Jump;
        interactAction.performed += OnInteractKey;

        var discardAction = InputSystem.actions.FindAction("Discard");
        discardAction.performed += actionController.DropItem;
        var scrollAction = InputSystem.actions.FindAction("Scroll");
        scrollAction.performed += actionController.ScrollHotBar;
        var hotbarAction = InputSystem.actions.FindAction("HotbarShortcut");
        hotbarAction.performed += actionController.OnItemHotbarClicked;
        var sprintAction = InputSystem.actions.FindAction("Sprint");
        sprintAction.performed += motor.TrySprint;
        sprintAction.canceled += motor.StopSprint;

        PlayerStatSystem.Instance.CanUseWeapon.Subscribe(can =>
        {
            lightGun.gameObject.SetActive(can);
        }).AddTo(disposables);
    }

    private void UnsubscribeEvents()
    {
        jumpAction.performed -= motor.Jump;
        interactAction.performed -= OnInteractKey;

        var discardAction = InputSystem.actions.FindAction("Discard");
        discardAction.performed -= actionController.DropItem;
        var scrollAction = InputSystem.actions.FindAction("Scroll");
        scrollAction.performed -= actionController.ScrollHotBar;
        var hotbarAction = InputSystem.actions.FindAction("HotbarShortcut");
        hotbarAction.performed -= actionController.OnItemHotbarClicked;
        var sprintAction = InputSystem.actions.FindAction("Sprint");
        sprintAction.performed -= motor.TrySprint;
        sprintAction.canceled -= motor.StopSprint;

        disposables.Clear();
    }

    private void OnInteractKey(InputAction.CallbackContext ctx)
    {
        interact.Interact(camlook.cam);
    }
}
