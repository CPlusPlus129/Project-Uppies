using UnityEngine;
using UnityEngine.UI;
using TMPro;
using R3;
using Cysharp.Threading.Tasks;

/// <summary>
/// Light Bar UI component that displays player light resource with smooth transitions and color changes.
/// Models the HPBarItem pattern for consistent UI behavior.
/// </summary>
public class LightBarItem : MonoBehaviour, IUIInitializable
{
    [Header("UI References")]
    [SerializeField] private Image barFill;

    [Header("Transition Settings")]
    [SerializeField] private bool smoothTransition = true;
    [SerializeField] private float transitionSpeed = 5f;

    // Internal state
    private float currentFillAmount;
    private float targetFillAmount;
    private float currentLight;
    private float maxLight;

    public async UniTask Init()
    {
        if (barFill != null)
        {
            currentFillAmount = barFill.fillAmount;
            targetFillAmount = currentFillAmount;
        }

        // Subscribe to the PlayerStatSystem
        var playerStatSystem = PlayerStatSystem.Instance;
        if (playerStatSystem != null)
        {
            // Subscribe to Light changes
            playerStatSystem.CurrentLight.Subscribe(light =>
            {
                currentLight = light;
                UpdateBar();
            }).AddTo(this);

            playerStatSystem.MaxLight.Subscribe(maxLightValue =>
            {
                maxLight = maxLightValue;
                UpdateBar();
            }).AddTo(this);
        }
        else
        {
            Debug.LogError("LightBarItem: PlayerStatSystem not found!");
        }

        Observable.EveryUpdate()
            .Where(_ => smoothTransition && barFill != null)
            .Subscribe(_ =>
            {
                // Smooth transition
                currentFillAmount = Mathf.Lerp(currentFillAmount, targetFillAmount, Time.deltaTime * transitionSpeed);
                barFill.fillAmount = currentFillAmount;
            }).AddTo(this);

        await UniTask.CompletedTask;
    }

    private void UpdateBar()
    {
        if (barFill == null) return;

        // Calculate fill amount
        float fillAmount = maxLight > 0 ? currentLight / maxLight : 0f;
        fillAmount = Mathf.Clamp01(fillAmount);

        if (smoothTransition)
        {
            targetFillAmount = fillAmount;
        }
        else
        {
            barFill.fillAmount = fillAmount;
            currentFillAmount = fillAmount;
        }

    }
    
}
