using Cysharp.Threading.Tasks;
using R3;
using R3.Triggers;
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
    public Vector3 SpawnPosition { get; set; }

    private async void Awake()
    {
        //UIRoot.Instance.SetVisible(false);        

        SpawnPosition = transform.position;
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

    private void OnUpdate_General()
    {
        var lookValue = lookAction.ReadValue<Vector2>();
        camlook.ProcessLook(lookValue);

        interact.UpdateCurrentInteractableTarget(camlook.cam);
    }

    private void OnUpdate_Move()
    {
        var moveValue = moveAction.ReadValue<Vector2>();
        motor.ProcessMove(moveValue);
    }

    public void Teleport(Vector3 position)
    {
        var cc = GetComponent<CharacterController>();
        if (cc == null)
            return;

        cc.enabled = false;
        cc.transform.position = position;
        cc.enabled = true;
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

        var playerStat = PlayerStatSystem.Instance;
        //whenever this object is destroyed or playerstatsystem is destroyed, this signal will fire.
        var terminationSignal = Observable.Merge(
            this.gameObject.OnDestroyAsObservable(),
            playerStat.OnDestroyed
        );
        // update that is not related to moving
        Observable.EveryUpdate()
            .TakeUntil(terminationSignal)
            .Where(_ => isActiveAndEnabled)
            .Subscribe(_ => OnUpdate_General())
            .AddTo(disposables);
        // update moving
        Observable.EveryUpdate()
            .TakeUntil(terminationSignal)
            .Where(_ => isActiveAndEnabled && playerStat.CanMove.CurrentValue)
            .Subscribe(_ => OnUpdate_Move())
            .AddTo(disposables);

        playerStat.CanUseWeapon
            .TakeUntil(terminationSignal)
            .Where(_ => isActiveAndEnabled)
            .Subscribe(can =>
            {
                lightGun.gameObject.SetActive(can);
            })
            .AddTo(disposables);
        
        playerStat.OnPlayerDeath
            .TakeUntil(terminationSignal)
            .Where(_ => isActiveAndEnabled)
            .Subscribe(_ => HandleOnDeath())
            .AddTo(disposables);

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

        disposables.Dispose();
    }

    private void OnInteractKey(InputAction.CallbackContext ctx)
    {
        interact.Interact(camlook.cam);
    }

    private async void HandleOnDeath()
    {
        var playerStat = PlayerStatSystem.Instance;
        UIRoot.Instance.GetUIComponent<DeathUI>()?.Open();
        playerStat.CanMove.Value = false;
        //if you don't wait on this, resurrect will set currentHP and the newest event from that will not fire, it's recommended to wait at least a frame.
        await UniTask.Delay(2000);
        if (playerStat == null)
            return;
        UIRoot.Instance.GetUIComponent<DeathUI>()?.Close();
        playerStat.Resurrect();
        playerStat.CanMove.Value = true;
        //Respawn at position
        if(playerStat.RespawnPosition.Value != Vector3.zero)
            Teleport(playerStat.RespawnPosition.Value);
        else
            Teleport(SpawnPosition);
    }
}
