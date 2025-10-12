using UnityEngine;
using UnityEngine.UI;
using TMPro;
using R3;

/// <summary>
/// Light Bar UI component that displays player light resource with smooth transitions and color changes.
/// Models the HPBarItem pattern for consistent UI behavior.
/// </summary>
public class LightBarItem : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image barFill;
    [SerializeField] private Image barBackground;
    [SerializeField] private TextMeshProUGUI lightText;
    
    [Header("Visual Settings")]
    [SerializeField] private bool smoothTransition = true;
    [SerializeField] private float transitionSpeed = 5f;
    [SerializeField] private bool useColorGradient = true;
    
    [Header("Color Gradient")]
    [SerializeField] private Color highLightColor = new Color(1f, 0.92f, 0.016f); // Bright yellow
    [SerializeField] private Color mediumLightColor = new Color(1f, 0.64f, 0.0f); // Orange
    [SerializeField] private Color lowLightColor = new Color(0.8f, 0.2f, 0.2f); // Dark red
    [SerializeField] [Range(0f, 1f)] private float lowLightThreshold = 0.25f;
    [SerializeField] [Range(0f, 1f)] private float mediumLightThreshold = 0.5f;
    
    [Header("Low Light Warning Flash")]
    [SerializeField] private bool enableLowLightFlash = true;
    [SerializeField] private float flashSpeed = 2f;
    [SerializeField] private float lowLightFlashThreshold = 0.15f; // Flash when below 15%
    
    [Header("Text Display")]
    [SerializeField] private bool showLightText = true;
    [SerializeField] private bool showPercentage = false;
    
    // Internal state
    private float currentFillAmount;
    private float targetFillAmount;
    private float currentLight;
    private float maxLight;
    private float flashTimer = 0f;
    private Color originalFillColor;
    
    private void Start()
    {
        if (barFill != null)
        {
            originalFillColor = barFill.color;
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
    }
    
    private void Update()
    {
        // Smooth transition
        if (smoothTransition && barFill != null)
        {
            currentFillAmount = Mathf.Lerp(currentFillAmount, targetFillAmount, Time.deltaTime * transitionSpeed);
            barFill.fillAmount = currentFillAmount;
        }
        
        // Handle low light warning flash
        if (enableLowLightFlash && barFill != null)
        {
            float lightPercentage = maxLight > 0 ? currentLight / maxLight : 0f;
            if (lightPercentage <= lowLightFlashThreshold && lightPercentage > 0f)
            {
                flashTimer += Time.deltaTime * flashSpeed;
                float flashIntensity = (Mathf.Sin(flashTimer) + 1f) * 0.5f; // Oscillates between 0 and 1
                Color flashColor = Color.Lerp(lowLightColor, Color.white, flashIntensity * 0.3f);
                barFill.color = flashColor;
            }
            else if (useColorGradient)
            {
                Color newColor = GetLightColor(lightPercentage);
                barFill.color = newColor;
                originalFillColor = newColor;
            }
        }
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
        
        // Update color based on light percentage
        if (useColorGradient && fillAmount > lowLightFlashThreshold)
        {
            Color newColor = GetLightColor(fillAmount);
            barFill.color = newColor;
            originalFillColor = newColor;
        }
        
        // Update text display
        if (lightText != null && showLightText)
        {
            if (showPercentage)
            {
                lightText.text = $"{Mathf.RoundToInt(fillAmount * 100)}%";
            }
            else
            {
                lightText.text = $"{Mathf.RoundToInt(currentLight)} / {Mathf.RoundToInt(maxLight)}";
            }
        }
    }
    
    private Color GetLightColor(float lightPercentage)
    {
        if (lightPercentage <= lowLightThreshold)
        {
            // Low light - red/dark
            return lowLightColor;
        }
        else if (lightPercentage <= mediumLightThreshold)
        {
            // Medium light - blend from orange to red
            float t = (lightPercentage - lowLightThreshold) / (mediumLightThreshold - lowLightThreshold);
            return Color.Lerp(lowLightColor, mediumLightColor, t);
        }
        else
        {
            // High light - blend from yellow to orange
            float t = (lightPercentage - mediumLightThreshold) / (1f - mediumLightThreshold);
            return Color.Lerp(mediumLightColor, highLightColor, t);
        }
    }
    
    /// <summary>
    /// Manually update the Light bar (if not using PlayerStatSystem subscription)
    /// </summary>
    public void UpdateValue(float currentLight, float maxLight)
    {
        this.currentLight = currentLight;
        this.maxLight = maxLight;
        UpdateBar();
    }
    
    /// <summary>
    /// Set the bar colors manually
    /// </summary>
    public void SetColors(Color high, Color medium, Color low)
    {
        highLightColor = high;
        mediumLightColor = medium;
        lowLightColor = low;
        UpdateBar();
    }
}
