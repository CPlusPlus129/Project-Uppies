using R3;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays the player's light meter with a fill gauge and border that fades based on light amount.
/// Fill amount maps 0%-100% to 0.155-0.61 range.
/// Border alpha decreases as light decreases.
/// </summary>
public class LightMeterUI : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private Image lightFillImage;
    [SerializeField] private Image lightBorderImage;

    [Header("Fill Amount Settings")]
    [SerializeField] private float fillAmountMin = 0.155f; // 0%
    [SerializeField] private float fillAmountMax = 0.61f;  // 100%

    [Header("Border Alpha Settings")]
    [SerializeField] private float borderAlphaMin = 0.2f;  // Alpha when light is 0%
    [SerializeField] private float borderAlphaMax = 1.0f;  // Alpha when light is 100%

    private PlayerStatSystem playerStats;

    private void Awake()
    {
        playerStats = PlayerStatSystem.Instance;

        if (playerStats != null)
        {
            playerStats.CurrentLight.Subscribe(_ => UpdateLightMeter()).AddTo(this);
            playerStats.MaxLight.Subscribe(_ => UpdateLightMeter()).AddTo(this);
            playerStats.CanUseWeapon.Subscribe(can => gameObject.SetActive(can)).AddTo(this);
        }

        UpdateLightMeter();
    }

    private void UpdateLightMeter()
    {
        if (playerStats == null)
        {
            return;
        }

        float currentLight = playerStats.CurrentLight.CurrentValue;
        float maxLight = playerStats.MaxLight.CurrentValue;
        float lightCostPerShot = playerStats.LightCostPerShot.CurrentValue;

        // Calculate light percentage (0-1)
        float lightPercentage = maxLight > 0 ? Mathf.Clamp01(currentLight / maxLight) : 0f;

        // Update fill amount
        if (lightFillImage != null)
        {
            lightFillImage.fillAmount = Mathf.Lerp(fillAmountMin, fillAmountMax, lightPercentage);
        }

        // Update border alpha
        if (lightBorderImage != null)
        {
            Color borderColor = lightBorderImage.color;
            borderColor.a = Mathf.Lerp(borderAlphaMin, borderAlphaMax, lightPercentage);
            lightBorderImage.color = borderColor;
            lightBorderImage.fillAmount = Mathf.Lerp(fillAmountMin, fillAmountMax, lightPercentage);
            //lightBorderImage.gameObject.SetActive(currentLight >= lightCostPerShot);
        }
    }

    private void OnValidate()
    {
        // Ensure min < max for fill amounts
        if (fillAmountMin > fillAmountMax)
        {
            fillAmountMin = fillAmountMax;
        }

        // Ensure alpha values are in valid range
        borderAlphaMin = Mathf.Clamp01(borderAlphaMin);
        borderAlphaMax = Mathf.Clamp01(borderAlphaMax);
    }
}
