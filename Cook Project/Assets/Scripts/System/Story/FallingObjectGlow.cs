using UnityEngine;

/// <summary>
/// Controls the emission intensity of a falling object, ramping it up during the fall
/// and dissipating it after landing.
/// Updated to use FridgeGlowController for consistent "see-through" effects.
/// </summary>
[RequireComponent(typeof(FridgeGlowController))]
public class FallingObjectGlow : MonoBehaviour
{
    public Color glowColor = new Color(1f, 0.5f, 0f); // Orange-ish default
    public float maxIntensity = 3f;
    public float fallDuration = 1.5f; // Approximate time until impact
    public float dissipateDuration = 2.0f; // Time to fade out after impact
    
    [Header("Fridge Glow Settings")]
    public float glowScale = 1.1f;
    public float fresnelPower = 3f;

    private FridgeGlowController fridgeGlow;
    private float startTime;
    private bool isInitialized = false;

    private void Start()
    {
        fridgeGlow = GetComponent<FridgeGlowController>();
        // Ensure controller exists (RequireComponent adds it, but good to be safe)
        if (fridgeGlow == null) fridgeGlow = gameObject.AddComponent<FridgeGlowController>();

        // Configure initial state
        // We set pulseVariation to 0 because we want to manually drive the intensity via Update
        fridgeGlow.ConfigureGlow(
            color: glowColor,
            intensity: 0f, // Start at 0
            newPulseSpeed: 0f, // Disable auto-pulse
            newScale: glowScale,
            newFresnelPower: fresnelPower,
            newPulseVariation: 0f
        );

        // Force update target mesh if needed (FridgeGlowController tries on Awake, but we might want to be sure)
        // fridgeGlow.SetTargetMesh(...) // Auto-resolve in FridgeGlowController is usually sufficient

        fridgeGlow.StartGlowing();

        startTime = Time.time;
        isInitialized = true;
    }

    private void Update()
    {
        if (!isInitialized || fridgeGlow == null) return;

        float elapsed = Time.time - startTime;
        float currentIntensity = 0f;

        if (elapsed < fallDuration)
        {
            // Phase 1: Falling - Ramp intensity up (exponentially looks better for "incoming" feel)
            float t = elapsed / fallDuration;
            currentIntensity = Mathf.Lerp(0f, maxIntensity, t * t); 
        }
        else if (elapsed < fallDuration + dissipateDuration)
        {
            // Phase 2: Landed - Linear fade out
            float t = (elapsed - fallDuration) / dissipateDuration;
            currentIntensity = Mathf.Lerp(maxIntensity, 0f, t);
        }
        else
        {
            // Phase 3: Done
            currentIntensity = 0f;
            fridgeGlow.StopGlowing();
            enabled = false; // Stop updating
        }

        // Drive the intensity manually
        // We only update intensity; other params stay as configured
        fridgeGlow.ConfigureGlow(intensity: currentIntensity);
    }
}
