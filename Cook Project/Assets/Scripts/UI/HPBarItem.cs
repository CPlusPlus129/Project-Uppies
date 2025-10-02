using UnityEngine;
using UnityEngine.UI;
using TMPro;
using R3;

/// <summary>
/// Enhanced HP Bar that displays player health with smooth transitions, color changes, and damage effects.
/// </summary>
public class HPBarItem : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image barFill;
    [SerializeField] private Image barBackground;
    [SerializeField] private TextMeshProUGUI hpText;
    
    [Header("Visual Settings")]
    [SerializeField] private bool smoothTransition = true;
    [SerializeField] private float transitionSpeed = 5f;
    [SerializeField] private bool useColorGradient = true;
    
    [Header("Color Gradient")]
    [SerializeField] private Color highHealthColor = Color.green;
    [SerializeField] private Color mediumHealthColor = Color.yellow;
    [SerializeField] private Color lowHealthColor = Color.red;
    [SerializeField] [Range(0f, 1f)] private float lowHealthThreshold = 0.25f;
    [SerializeField] [Range(0f, 1f)] private float mediumHealthThreshold = 0.5f;
    
    [Header("Damage Flash")]
    [SerializeField] private bool enableDamageFlash = false;
    [SerializeField] private float minDamageForFlash = 5f; // Only flash for significant damage
    [SerializeField] private Color damageFlashColor = Color.white;
    [SerializeField] private float flashDuration = 0.15f;
    
    [Header("Text Display")]
    [SerializeField] private bool showHPText = true;
    [SerializeField] private bool showPercentage = false;
    
    // Internal state
    private float currentFillAmount;
    private float targetFillAmount;
    private int currentHP;
    private int maxHP;
    private bool isFlashing = false;
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
            // Subscribe to HP changes
            playerStatSystem.CurrentHP.Subscribe(hp =>
            {
                int previousHP = currentHP;
                int damageTaken = previousHP - hp;
                currentHP = hp;
                
                // Trigger damage flash only if enabled and damage is significant
                if (enableDamageFlash && damageTaken >= minDamageForFlash && previousHP > 0)
                {
                    TriggerDamageFlash();
                }
                
                UpdateBar();
            }).AddTo(this);
            
            playerStatSystem.MaxHP.Subscribe(hp =>
            {
                maxHP = hp;
                UpdateBar();
            }).AddTo(this);
        }
        else
        {
            Debug.LogError("HPBarItem: PlayerStatSystem not found!");
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
        
        // Handle damage flash
        if (isFlashing)
        {
            flashTimer += Time.deltaTime;
            if (flashTimer >= flashDuration)
            {
                isFlashing = false;
                flashTimer = 0f;
                if (barFill != null)
                {
                    barFill.color = originalFillColor;
                }
            }
        }
    }
    
    private void UpdateBar()
    {
        if (barFill == null) return;
        
        // Calculate fill amount
        float fillAmount = maxHP > 0 ? (float)currentHP / maxHP : 0f;
        
        if (smoothTransition)
        {
            targetFillAmount = fillAmount;
        }
        else
        {
            barFill.fillAmount = fillAmount;
            currentFillAmount = fillAmount;
        }
        
        // Update color based on health percentage
        if (useColorGradient && !isFlashing)
        {
            Color newColor = GetHealthColor(fillAmount);
            barFill.color = newColor;
            originalFillColor = newColor;
        }
        
        // Update text display
        if (hpText != null && showHPText)
        {
            if (showPercentage)
            {
                hpText.text = $"{Mathf.RoundToInt(fillAmount * 100)}%";
            }
            else
            {
                hpText.text = $"{currentHP} / {maxHP}";
            }
        }
    }
    
    private Color GetHealthColor(float healthPercentage)
    {
        if (healthPercentage <= lowHealthThreshold)
        {
            // Low health - red
            return lowHealthColor;
        }
        else if (healthPercentage <= mediumHealthThreshold)
        {
            // Medium health - blend from yellow to red
            float t = (healthPercentage - lowHealthThreshold) / (mediumHealthThreshold - lowHealthThreshold);
            return Color.Lerp(lowHealthColor, mediumHealthColor, t);
        }
        else
        {
            // High health - blend from green to yellow
            float t = (healthPercentage - mediumHealthThreshold) / (1f - mediumHealthThreshold);
            return Color.Lerp(mediumHealthColor, highHealthColor, t);
        }
    }
    
    private void TriggerDamageFlash()
    {
        if (barFill == null) return;
        
        isFlashing = true;
        flashTimer = 0f;
        barFill.color = damageFlashColor;
    }
    
    /// <summary>
    /// Manually update the HP bar (if not using PlayerStatSystem subscription)
    /// </summary>
    public void UpdateValue(int currentHP, int maxHP)
    {
        this.currentHP = currentHP;
        this.maxHP = maxHP;
        UpdateBar();
    }
    
    /// <summary>
    /// Set the bar colors manually
    /// </summary>
    public void SetColors(Color high, Color medium, Color low)
    {
        highHealthColor = high;
        mediumHealthColor = medium;
        lowHealthColor = low;
        UpdateBar();
    }
}
