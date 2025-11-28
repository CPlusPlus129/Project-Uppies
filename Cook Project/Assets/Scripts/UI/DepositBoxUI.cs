using Cysharp.Threading.Tasks;
using R3;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DepositBoxUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI quotaText;
    [SerializeField] private Image fillBar;

    private IShiftSystem shiftSystem;

    private async void Awake()
    {
        await UniTask.WaitUntil(() => GameFlow.Instance.IsInitialized);
        shiftSystem = await ServiceLocator.Instance.GetAsync<IShiftSystem>();

        // Subscribe to reactive properties for real-time updates
        shiftSystem.depositedAmount.Subscribe(_ => UpdateQuotaDisplay()).AddTo(this);
        shiftSystem.quotaAmount.Subscribe(_ => UpdateQuotaDisplay()).AddTo(this);
        shiftSystem.currentState.Subscribe(UpdateVisibility).AddTo(this);

        // Initial update
        UpdateQuotaDisplay();
    }

    private void UpdateQuotaDisplay()
    {
        if (shiftSystem == null)
            return;

        int deposited = shiftSystem.depositedAmount.CurrentValue;
        int quota = shiftSystem.quotaAmount.CurrentValue;

        // Update text display
        if (quotaText != null)
        {
            quotaText.text = $"QUOTA\n{deposited}/{quota}";
        }

        // Update fill bar
        if (fillBar != null)
        {
            float ratio = quota > 0 ? (float)deposited / quota : 0f;
            fillBar.fillAmount = Mathf.Clamp01(ratio);
        }
    }

    private void UpdateVisibility(ShiftSystem.ShiftState state)
    {
        var isVisible = state == ShiftSystem.ShiftState.InShift || state == ShiftSystem.ShiftState.Overtime;
        gameObject.SetActive(isVisible);
    }
}
