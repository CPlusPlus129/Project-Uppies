using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace RenderFeatures.CAS
{
    [Serializable]
    public class CASSettings
    {
        [Range(0.0f, 1.0f)]
        public float Sharpness = 0.5f;
    }

    public class CASRendererFeature : ScriptableRendererFeature
    {
        [SerializeField]
        private CASSettings settings = new CASSettings();

        [SerializeField]
        private Shader shader;

        private Material m_Material;
        private CASRenderPass m_RenderPass;

        public override void Create()
        {
            if (shader == null)
            {
                // Try to find the shader if not assigned
                shader = Shader.Find("CustomEffects/CAS");
                if (shader == null)
                    return;
            }

            m_Material = new Material(shader);
            m_RenderPass = new CASRenderPass(m_Material, settings)
            {
                renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing // Apply after post-processing for final touch, or before if preferred
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (shader == null || m_Material == null)
                return;

            if (renderingData.cameraData.cameraType == CameraType.Game)
            {
                renderer.EnqueuePass(m_RenderPass);
            }
        }

        protected override void Dispose(bool disposing)
        {
            m_RenderPass?.Dispose();
            if (m_Material != null)
            {
#if UNITY_EDITOR
                if (Application.isPlaying)
                    Destroy(m_Material);
                else
                    DestroyImmediate(m_Material);
#else
                Destroy(m_Material);
#endif
            }
        }
    }
}
