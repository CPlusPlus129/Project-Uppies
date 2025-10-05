using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RendererUtils;

/// <summary>
/// Render Pass that renders fridge glow objects on top of everything else
/// This allows the glow to be visible through walls
/// Unity 6.2 URP Render Graph implementation
/// </summary>
public class FridgeGlowRenderPass : ScriptableRenderPass
{
    private const string PassName = "FridgeGlowPass";
    
    private FilteringSettings filteringSettings;
    private ShaderTagId shaderTagId;
    
    private class PassData
    {
        internal RendererListHandle rendererList;
    }
    
    public FridgeGlowRenderPass(RenderPassEvent renderPassEvent, LayerMask layerMask)
    {
        this.renderPassEvent = renderPassEvent;
        filteringSettings = new FilteringSettings(RenderQueueRange.all, layerMask);
        shaderTagId = new ShaderTagId("FridgeGlowPass");
        profilingSampler = new ProfilingSampler(PassName);
    }
    
    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
        UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
        UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
        UniversalLightData lightData = frameData.Get<UniversalLightData>();
        
        // Don't render in scene view (remove this line to see glow in editor)
        if (cameraData.cameraType == CameraType.SceneView) return;
        
        using (var builder = renderGraph.AddRasterRenderPass<PassData>(PassName, out var passData, profilingSampler))
        {
            RendererListDesc rendererListDesc = new RendererListDesc(shaderTagId, renderingData.cullResults, cameraData.camera)
            {
                sortingCriteria = cameraData.defaultOpaqueSortFlags,
                renderQueueRange = RenderQueueRange.all,
                layerMask = filteringSettings.layerMask,
            };
            
            passData.rendererList = renderGraph.CreateRendererList(rendererListDesc);
            
            builder.UseRendererList(passData.rendererList);
            builder.SetRenderAttachment(resourceData.activeColorTexture, 0);
            builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture);
            
            builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
            {
                context.cmd.DrawRendererList(data.rendererList);
            });
        }
    }
}
