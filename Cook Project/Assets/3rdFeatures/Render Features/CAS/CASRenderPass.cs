using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace RenderFeatures.CAS
{
    public class CASRenderPass : ScriptableRenderPass
    {
        private readonly CASSettings m_Settings;
        private readonly Material m_Material;
        private RenderTextureDescriptor m_Descriptor;
        private RTHandle m_TempTextureHandle;

        private static readonly int SharpnessId = Shader.PropertyToID("_Sharpness");
        
        // Pass data for Render Graph
        private class PassData
        {
            public TextureHandle source;
            public TextureHandle destination;
            public Material material;
        }

        public CASRenderPass(Material material, CASSettings settings)
        {
            m_Settings = settings;
            m_Material = material;
            m_Descriptor = new RenderTextureDescriptor(Screen.width, Screen.height, RenderTextureFormat.Default, 0);
        }

        // --- LEGACY RENDER PATH ---
        [System.Obsolete]
        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            m_Descriptor.width = cameraTextureDescriptor.width;
            m_Descriptor.height = cameraTextureDescriptor.height;
            m_Descriptor.colorFormat = cameraTextureDescriptor.colorFormat;
            m_Descriptor.depthBufferBits = 0; 

            RenderingUtils.ReAllocateHandleIfNeeded(ref m_TempTextureHandle, m_Descriptor, name: "_CASTempTexture");
        }

        [System.Obsolete]
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (m_Material == null) return;

            var cmd = CommandBufferPool.Get("CAS Pass");
#pragma warning disable CS0618 // Type or member is obsolete
            var cameraTargetHandle = renderingData.cameraData.renderer.cameraColorTargetHandle;

            m_Material.SetFloat(SharpnessId, m_Settings.Sharpness);

            using (new ProfilingScope(cmd, new ProfilingSampler("CASRenderPass")))
            {
                Blit(cmd, cameraTargetHandle, m_TempTextureHandle, m_Material, 0);
                Blit(cmd, m_TempTextureHandle, cameraTargetHandle);
            }
#pragma warning restore CS0618 // Type or member is obsolete

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        // --- RENDER GRAPH PATH ---
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (m_Material == null) return;

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            
            // If there's no active color target, we can't do anything
            if (resourceData.isActiveTargetBackBuffer) return;

            TextureHandle cameraColor = resourceData.activeColorTexture;
            
            // Create a temporary texture for the effect
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            var desc = cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;
            
            TextureHandle tempTexture = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "_CASTempTexture", false);

            m_Material.SetFloat(SharpnessId, m_Settings.Sharpness);

            // Pass 1: Camera -> Temp (Apply CAS)
            using (var builder = renderGraph.AddRasterRenderPass<PassData>("CAS Pass", out var passData))
            {
                passData.source = cameraColor;
                passData.destination = tempTexture;
                passData.material = m_Material;

                builder.UseTexture(passData.source, AccessFlags.Read);
                builder.SetRenderAttachment(passData.destination, 0);
                
                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    Blitter.BlitTexture(context.cmd, data.source, new Vector4(1, 1, 0, 0), data.material, 0);
                });
            }
            
            // Pass 2: Temp -> Camera (Copy Back)
            using (var builder = renderGraph.AddRasterRenderPass<PassData>("CAS Copy Back", out var passData))
            {
                passData.source = tempTexture;
                passData.destination = cameraColor;
                passData.material = null; 

                builder.UseTexture(passData.source, AccessFlags.Read);
                builder.SetRenderAttachment(passData.destination, 0);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    Blitter.BlitTexture(context.cmd, data.source, new Vector4(1, 1, 0, 0), 0.0f, false);
                });
            }
        }

        public void Dispose()
        {
            m_TempTextureHandle?.Release();
        }
    }
}
