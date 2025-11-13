using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays a health bar above the mob's head in world space.
/// Similar to ExplosionLifetimeBarController but tracks health instead of lifetime.
/// </summary>
public class MobHealthBarController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Mob mob;
    [SerializeField] private Slider healthBar;
    
    [Header("Positioning")]
    [SerializeField] private Vector3 offset = new Vector3(0, 2f, 0);
    [Tooltip("If true, bar follows the mob. If false, bar stays at spawn position.")]
    [SerializeField] private bool followMob = true;
    
    [Header("Appearance")]
    [SerializeField] private Vector2 barSize = new Vector2(1f, 0.1f);
    [SerializeField] private Color healthyColor = new Color(0.2f, 0.8f, 0.2f, 0.9f); // Green
    [SerializeField] private Color midHealthColor = new Color(1f, 0.8f, 0f, 0.9f); // Yellow
    [SerializeField] private Color lowHealthColor = new Color(1f, 0.2f, 0.2f, 0.9f); // Red
    [SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 0.5f);
    [Tooltip("Health percentage below which to use low health color")]
    [SerializeField] [Range(0f, 1f)] private float lowHealthThreshold = 0.3f;
    [Tooltip("Health percentage below which to use mid health color")]
    [SerializeField] [Range(0f, 1f)] private float midHealthThreshold = 0.6f;
    
    [Header("Behavior")]
    [SerializeField] private bool hideWhenFullHealth = true;
    [SerializeField] private bool hideWhenDead = true;
    [SerializeField] private float fadeOutDuration = 0.5f;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;
    
    private Camera mainCamera;
    private Canvas canvas;
    private Image fillImage;
    private Image backgroundImage;
    private CanvasGroup canvasGroup;
    private Vector3 worldPosition;
    private bool isInitialized = false;
    private float currentAlpha = 1f;
    private bool isFadingOut = false;

    void Start()
    {
        mainCamera = Camera.main;
        
        // Auto-find mob component if not assigned
        if (mob == null)
        {
            mob = GetComponent<Mob>();
            if (mob == null)
            {
                Debug.LogError("MobHealthBarController: No Mob component found! Please assign one or add it to the mob.", this);
                enabled = false;
                return;
            }
        }
        
        // Create or setup the health bar
        if (healthBar == null)
        {
            CreateHealthBar();
        }
        else
        {
            // If using a pre-made slider, ensure it's in world space
            canvas = healthBar.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                SetupWorldSpaceCanvas();
                fillImage = healthBar.fillRect.GetComponent<Image>();
            }
        }
        
        isInitialized = true;
        
        // Initial position
        UpdatePosition();
        
        // Initial health value
        UpdateHealthBar();
        
        if (showDebugInfo)
        {
            Debug.Log($"MobHealthBarController initialized for {gameObject.name}");
        }
    }

    void Update()
    {
        if (!isInitialized || canvas == null || mob == null) return;
        
        // Update position to follow mob
        if (followMob)
        {
            UpdatePosition();
        }
        
        // Update health bar value and color
        UpdateHealthBar();
        
        // Handle visibility
        HandleVisibility();
        
        // Always face camera
        if (mainCamera != null)
        {
            canvas.transform.LookAt(mainCamera.transform);
            canvas.transform.Rotate(0, 180, 0);
        }
    }

    void UpdatePosition()
    {
        if (canvas != null)
        {
            worldPosition = transform.position + offset;
            canvas.transform.position = worldPosition;
        }
    }

    void UpdateHealthBar()
    {
        if (healthBar == null || mob == null) return;
        
        // Calculate health percentage
        float healthPercentage = mob.HealthPercentage;
        
        // Update slider value
        healthBar.value = healthPercentage;
        
        // Update fill color based on health percentage
        if (fillImage != null)
        {
            Color targetColor;
            
            if (healthPercentage <= lowHealthThreshold)
            {
                // Low health - use low health color
                targetColor = lowHealthColor;
            }
            else if (healthPercentage <= midHealthThreshold)
            {
                // Mid health - interpolate between mid and low
                float t = (healthPercentage - lowHealthThreshold) / (midHealthThreshold - lowHealthThreshold);
                targetColor = Color.Lerp(lowHealthColor, midHealthColor, t);
            }
            else
            {
                // High health - interpolate between healthy and mid
                float t = (healthPercentage - midHealthThreshold) / (1f - midHealthThreshold);
                targetColor = Color.Lerp(midHealthColor, healthyColor, t);
            }
            
            fillImage.color = targetColor;
        }
        
        if (showDebugInfo && Time.frameCount % 60 == 0) // Log every 60 frames
        {
            Debug.Log($"Health Bar: {mob.CurrentHealth}/{mob.MaxHealth} ({healthPercentage:P0})");
        }
    }

    void HandleVisibility()
    {
        if (canvasGroup == null || mob == null) return;
        
        bool shouldBeVisible = true;
        
        // Hide when at full health
        if (hideWhenFullHealth && mob.CurrentHealth >= mob.MaxHealth)
        {
            shouldBeVisible = false;
        }
        
        // Hide when dead
        if (hideWhenDead && !mob.IsAlive)
        {
            shouldBeVisible = false;
            if (!isFadingOut)
            {
                isFadingOut = true;
            }
        }
        
        // Smoothly fade in/out
        float targetAlpha = shouldBeVisible ? 1f : 0f;
        float fadeSpeed = isFadingOut ? (1f / fadeOutDuration) : 3f;
        currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, Time.deltaTime * fadeSpeed);
        canvasGroup.alpha = currentAlpha;
    }

    void SetupWorldSpaceCanvas()
    {
        if (canvas == null) return;
        
        // Force the canvas to world space mode
        canvas.renderMode = RenderMode.WorldSpace;
        
        // Position it at the mob's location
        worldPosition = transform.position + offset;
        canvas.transform.position = worldPosition;
        
        if (mainCamera != null)
        {
            canvas.transform.LookAt(mainCamera.transform);
            canvas.transform.Rotate(0, 180, 0);
        }
        
        // Scale it appropriately for world space
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(200, 20);
        canvas.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
        
        // Add canvas group for fading
        if (canvas.GetComponent<CanvasGroup>() == null)
        {
            canvasGroup = canvas.gameObject.AddComponent<CanvasGroup>();
        }
    }

    void CreateHealthBar()
    {
        // Create a world-space canvas for the health bar
        GameObject canvasObj = new GameObject("HealthBarCanvas");
        canvas = canvasObj.AddComponent<Canvas>();
        
        // CRITICAL: Set to World Space
        canvas.renderMode = RenderMode.WorldSpace;
        
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();
        
        // Add canvas group for fading
        canvasGroup = canvasObj.AddComponent<CanvasGroup>();
        
        // Position the canvas at the mob location + offset
        worldPosition = transform.position + offset;
        canvasObj.transform.position = worldPosition;
        
        if (mainCamera != null)
        {
            canvasObj.transform.LookAt(mainCamera.transform);
            canvasObj.transform.Rotate(0, 180, 0);
        }
        
        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(200, 20);
        canvasObj.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
        
        // Make canvas a child of the mob so it moves with it
        canvasObj.transform.SetParent(transform, true);
        
        // Create background
        GameObject background = new GameObject("Background");
        background.transform.SetParent(canvasObj.transform, false);
        backgroundImage = background.AddComponent<Image>();
        backgroundImage.color = backgroundColor;
        RectTransform bgRect = background.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        
        // Create slider
        GameObject sliderObj = new GameObject("Slider");
        sliderObj.transform.SetParent(canvasObj.transform, false);
        healthBar = sliderObj.AddComponent<Slider>();
        
        RectTransform sliderRect = sliderObj.GetComponent<RectTransform>();
        sliderRect.anchorMin = Vector2.zero;
        sliderRect.anchorMax = Vector2.one;
        sliderRect.offsetMin = new Vector2(5, 5); // Add padding
        sliderRect.offsetMax = new Vector2(-5, -5);
        
        // Create fill area
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObj.transform, false);
        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.sizeDelta = Vector2.zero;
        
        // Create fill
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        fillImage = fill.AddComponent<Image>();
        fillImage.color = healthyColor;
        
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;
        
        healthBar.fillRect = fillRect;
        healthBar.interactable = false;
        healthBar.value = 1;
        
        if (showDebugInfo)
        {
            Debug.Log("Created health bar UI for mob");
        }
    }
    
    /// <summary>
    /// Manually trigger a flash effect (e.g., when taking damage)
    /// </summary>
    public void FlashDamage()
    {
        if (fillImage != null)
        {
            StartCoroutine(FlashDamageCoroutine());
        }
    }
    
    private System.Collections.IEnumerator FlashDamageCoroutine()
    {
        Color originalColor = fillImage.color;
        fillImage.color = Color.white;
        yield return new WaitForSeconds(0.1f);
        fillImage.color = originalColor;
    }
    
    void OnDestroy()
    {
        // Clean up the canvas when the mob is destroyed
        if (canvas != null && canvas.gameObject != null)
        {
            Destroy(canvas.gameObject);
        }
    }
}
