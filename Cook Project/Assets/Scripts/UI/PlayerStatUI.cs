using Cysharp.Threading.Tasks;
using R3;
using System.Threading;
using TMPro;
using UnityEngine;

public class PlayerStatUI : MonoBehaviour
{
    [SerializeField]
    private HPBarItem hpBar;
    [SerializeField]
    private BarItem staminaBar;
    [SerializeField]
    private LightBarItem lightBar;
    [SerializeField]
    private TextMeshProUGUI moneyValueText;
    [SerializeField]
    private AnimationCurve moneyCountCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField]
    private float minMoneyCountDuration = 0.35f;
    [SerializeField]
    private float maxMoneyCountDuration = 1.2f;
    [SerializeField]
    private AudioSource moneyAudioSource;
    [SerializeField]
    private AudioClip moneyTickClip;

    private CancellationTokenSource moneyAnimationCts;
    private int lastDisplayedMoney;
    private bool hasInitializedMoney;

    private void Start()
    {
        var playerStatSystem = PlayerStatSystem.Instance;
        lastDisplayedMoney = playerStatSystem.Money.Value;
        if (moneyValueText != null)
        {
            moneyValueText.text = lastDisplayedMoney.ToString("N0");
        }

        // hp and light bar register their own events

        playerStatSystem.CurrentStamina.Subscribe(stamina =>
        {
            staminaBar.UpdateValue(stamina, playerStatSystem.MaxStamina.Value);
        }).AddTo(this);
        playerStatSystem.MaxStamina.Subscribe(maxStamina =>
        {
            staminaBar.UpdateValue(playerStatSystem.CurrentStamina.Value, maxStamina);
        }).AddTo(this);

        playerStatSystem.Money
            .Subscribe(money => AnimateMoneyValue(money).Forget())
            .AddTo(this);
    }

    private void OnDestroy()
    {
        CancelMoneyAnimation();
    }

    private void CancelMoneyAnimation()
    {
        if (moneyAnimationCts == null)
        {
            return;
        }

        moneyAnimationCts.Cancel();
        moneyAnimationCts.Dispose();
        moneyAnimationCts = null;
    }

    private async UniTaskVoid AnimateMoneyValue(int target)
    {
        if (moneyValueText == null)
        {
            return;
        }

        if (!hasInitializedMoney)
        {
            hasInitializedMoney = true;
            lastDisplayedMoney = target;
            moneyValueText.text = target.ToString("N0");
            return;
        }

        if (target == lastDisplayedMoney)
        {
            return;
        }

        CancelMoneyAnimation();
        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
        moneyAnimationCts = linkedCts;
        var token = linkedCts.Token;

        int startValue = lastDisplayedMoney;
        float delta = Mathf.Abs(target - startValue);
        float duration = Mathf.Clamp(minMoneyCountDuration + (delta / 500f) * 0.15f, minMoneyCountDuration, maxMoneyCountDuration);
        float elapsed = 0f;
        float tickTimer = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            tickTimer += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float curved = moneyCountCurve.Evaluate(t);
            int value = Mathf.RoundToInt(Mathf.Lerp(startValue, target, curved));
            moneyValueText.text = value.ToString("N0");

            if (moneyAudioSource != null && moneyTickClip != null && tickTimer >= 0.08f)
            {
                tickTimer = 0f;
                moneyAudioSource.PlayOneShot(moneyTickClip);
            }

            await UniTask.NextFrame(token);
        }

        lastDisplayedMoney = target;
        moneyValueText.text = target.ToString("N0");
    }
}
