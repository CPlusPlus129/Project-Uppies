using R3;
using UnityEngine;

public class DamagedIndicator : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float maxEffectHpPercentage = 0.05f; // how much HP loss (as a percentage of max HP) causes the maximum effect
    [SerializeField] private float fadeDuration = 2f;
    [SerializeField] private float fadeHoldDuration = 0.2f;
    private float fadeHoldTimer = 0f;

    private void Awake()
    {
        canvasGroup.alpha = 0f;
        if (fadeDuration <= 0f)
        {
            Debug.LogWarning("DamagedIndicator: fadeDuration must be greater than 0. Setting to default value of 2f.");
            fadeDuration = 2f;
        }
        PlayerStatSystem.Instance.OnHPChanged
            .Subscribe(hpValues =>
            {
                if (hpValues.newValue < hpValues.oldValue)
                    ShowDamagedIndicator(hpValues.oldValue - hpValues.newValue);
            })
            .AddTo(this);
    }

    private void Update()
    {
        if (fadeHoldTimer > 0f)
        {
            fadeHoldTimer -= Time.deltaTime;
        }
        else if (canvasGroup.alpha > 0f)
        {
            canvasGroup.alpha -= Time.deltaTime / fadeDuration;
            if (canvasGroup.alpha < 0f)
                canvasGroup.alpha = 0f;
        }
    }

    private void ShowDamagedIndicator(float delta)
    {
        var maxHp = PlayerStatSystem.Instance.MaxHP.Value;
        if (maxHp <= 0) return;
        var hpPercent = delta / maxHp;
        var effectStrength = Mathf.Clamp01(hpPercent / maxEffectHpPercentage);
        canvasGroup.alpha = Mathf.Max(canvasGroup.alpha, effectStrength);
        fadeHoldTimer = fadeHoldDuration;
    }

}