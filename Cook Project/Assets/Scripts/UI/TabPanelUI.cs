using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using UnityEngine.InputSystem;

public class TabPanelUI : MonoBehaviour, IUIInitializable
{
    [SerializeField] private UIAnimationController uiAnim;
    private ReactiveProperty<bool> currentOpenState = new ReactiveProperty<bool>(false);
    private float tabButtonCooldown = 0.2f;
    private float tabButtonCooldownTimer = 0;

    public async UniTask Init()
    {
        SubscribeEvents();

        await UniTask.CompletedTask;
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
    }

    private void SubscribeEvents()
    {
        InputSystem.actions.FindAction("Tab").performed += OnTabButton;

        currentOpenState.Subscribe(isOpen =>
        {
            if (isOpen)
            {
                uiAnim.Open();
            }
            else
            {
                uiAnim.Close();
            }
        }).AddTo(this);
        Observable.EveryUpdate().Subscribe(_ =>
        {
            if(tabButtonCooldownTimer > 0)
            {
                tabButtonCooldownTimer -= Time.deltaTime;
            }
        }).AddTo(this);
    }

    private void UnsubscribeEvents()
    {
        InputSystem.actions.FindAction("Tab").performed -= OnTabButton;
    }

    private void OnTabButton(InputAction.CallbackContext cxt)
    {
        if(tabButtonCooldownTimer > 0)
        {
            return;
        }
        tabButtonCooldownTimer = tabButtonCooldown;
        currentOpenState.Value = !currentOpenState.Value;
    }
}