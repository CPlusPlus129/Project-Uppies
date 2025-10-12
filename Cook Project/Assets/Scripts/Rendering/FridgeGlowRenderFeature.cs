using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// URP Render Feature that adds a custom render pass for fridge glow effects
/// This integrates the glow rendering into the URP pipeline
/// Unity 6.2 URP implementation
/// </summary>
public class FridgeGlowRenderFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        [Tooltip("When to inject the render pass in the rendering pipeline")]
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
        
        [Tooltip("Layer mask for glow objects. Set this to 'FridgeGlow' layer")]
        public LayerMask glowLayerMask = -1;
        
        [Tooltip("Enable this feature")]
        public bool isEnabled = true;
        
        [Tooltip("Enable debug warnings")]
        public bool enableDebugWarnings = false;
    }
    
    public Settings settings = new Settings();
    private FridgeGlowRenderPass renderPass;
    
    public override void Create()
    {
        if (settings.glowLayerMask == 0 && settings.enableDebugWarnings)
        {
            Debug.LogWarning("FridgeGlowRenderFeature: Glow layer mask is not set! Please assign the 'FridgeGlow' layer in the feature settings.");
        }
        
        renderPass = new FridgeGlowRenderPass(settings.renderPassEvent, settings.glowLayerMask);
    }
    
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (!settings.isEnabled) return;
        
        if (settings.glowLayerMask == 0)
        {
            if (settings.enableDebugWarnings)
            {
                Debug.LogWarning("FridgeGlowRenderFeature: Glow layer mask is 0 - no objects will be rendered!");
            }
            return;
        }
        
        renderer.EnqueuePass(renderPass);
    }
    
    protected override void Dispose(bool disposing)
    {
        // Cleanup if needed
    }
}
