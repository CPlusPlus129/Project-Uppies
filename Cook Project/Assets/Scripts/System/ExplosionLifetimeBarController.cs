using UnityEngine;
using UnityEngine.UI;

public class ExplosionLifetimeBarController : MonoBehaviour
{
    [SerializeField] private Slider lifetimeBar;
    [SerializeField] private Vector3 offset = new Vector3(0, 2, 0);
    [SerializeField] private Vector2 barSize = new Vector2(1f, 0.1f);
    
    private float maxLifetime;
    private float currentLifetime;
    private Camera mainCamera;
    private Canvas canvas;
    private Vector3 worldPosition;

    void Start()
    {
        mainCamera = Camera.main;
        
        // Store the world position where the bar should stay
        worldPosition = transform.position + offset;
        
        // Get the TimedObjectDestroyer component
        TimedObjectDestroyer destroyer = GetComponent<TimedObjectDestroyer>();
        if (destroyer != null)
        {
            maxLifetime = destroyer.lifeTime;
            currentLifetime = maxLifetime;
        }
        else
        {
            Debug.LogWarning("No TimedObjectDestroyer found, using default lifetime");
            maxLifetime = 6f;
            currentLifetime = maxLifetime;
        }
        
        // Create the canvas if we're instantiating this at runtime
        if (lifetimeBar == null)
        {
            CreateLifetimeBar();
        }
        else
        {
            // If using a pre-made slider, ensure it's in world space
            canvas = lifetimeBar.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                SetupWorldSpaceCanvas();
            }
        }
    }

    void Update()
    {
        currentLifetime -= Time.deltaTime;
        
        if (lifetimeBar != null)
        {
            // Update the slider value
            lifetimeBar.value = Mathf.Clamp01(currentLifetime / maxLifetime);
            
            // Keep the bar at the world position and facing the camera
            if (canvas != null && mainCamera != null)
            {
                canvas.transform.position = worldPosition;
                canvas.transform.LookAt(mainCamera.transform);
                canvas.transform.Rotate(0, 180, 0);
            }
        }
    }

    void SetupWorldSpaceCanvas()
    {
        // Force the canvas to world space mode
        canvas.renderMode = RenderMode.WorldSpace;
        
        // Position it at the explosion location
        canvas.transform.position = worldPosition;
        canvas.transform.LookAt(mainCamera.transform);
        canvas.transform.Rotate(0, 180, 0);
        
        // Scale it appropriately for world space
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(200, 20);
        canvas.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
    }
    

    void CreateLifetimeBar()
    {
        // Create a bar that stays at the explosion location in world space
        GameObject canvasObj = new GameObject("LifetimeCanvas");
        canvas = canvasObj.AddComponent<Canvas>();
        
        // CRITICAL: Set to World Space
        canvas.renderMode = RenderMode.WorldSpace;
        
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();
        
        // Position the canvas at the explosion location + offset (stays here)
        canvasObj.transform.position = worldPosition;
        canvasObj.transform.LookAt(mainCamera.transform);
        canvasObj.transform.Rotate(0, 180, 0);
        
        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(200, 20);
        canvasObj.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
        
        // Create background (optional - makes it easier to see)
        GameObject background = new GameObject("Background");
        background.transform.SetParent(canvasObj.transform, false);
        Image bgImage = background.AddComponent<Image>();
        bgImage.color = new Color(0f, 0f, 0f, 0.5f); // Semi-transparent black
        RectTransform bgRect = background.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;
        
        // Create slider
        GameObject sliderObj = new GameObject("Slider");
        sliderObj.transform.SetParent(canvasObj.transform, false);
        lifetimeBar = sliderObj.AddComponent<Slider>();
        
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
        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = new Color(0.2f, 0.5f, 1f, 0.8f); // Brighter translucent blue
        
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.sizeDelta = Vector2.zero;
        
        lifetimeBar.fillRect = fillRect;
        lifetimeBar.interactable = false;
        lifetimeBar.value = 1;
        
        // Destroy the canvas when the explosion is destroyed
        Destroy(canvasObj, maxLifetime);
    }
}
