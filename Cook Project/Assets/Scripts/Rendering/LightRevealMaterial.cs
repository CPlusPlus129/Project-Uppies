using UnityEngine;

namespace NvJ.Rendering
{
    /// <summary>
    /// Helper component for objects using the Light Reveal Texture shader.
    /// Makes it easier to set up and preview the light-revealed texture effect.
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    [ExecuteInEditMode]
    public class LightRevealMaterial : MonoBehaviour
    {
        [Header("Material Setup")]
        [Tooltip("The material using the LightRevealTexture shader")]
        [SerializeField] private Material lightRevealMaterial;
        
        [Header("Debug Visualization")]
        [Tooltip("Show debug information in scene view")]
        [SerializeField] private bool showDebug = true;
        
        [Tooltip("Draw a sphere showing light detection range (for reference)")]
        [SerializeField] private bool showLightRange = true;
        
        [Tooltip("Approximate range to visualize (adjust to match your light ranges)")]
        [SerializeField] private float visualLightRange = 10f;
        
        [Header("Quick Material Controls")]
        [Tooltip("Quickly adjust reveal sensitivity")]
        [SerializeField] [Range(0.1f, 10f)] private float revealSensitivity = 1f;
        
        [Tooltip("Quickly adjust reveal smoothness")]
        [SerializeField] [Range(0.01f, 1f)] private float revealSmoothness = 0.2f;
        
        [Tooltip("Sync these values to the material in real-time")]
        [SerializeField] private bool liveUpdate = false;
        
        private Renderer rendererComponent;
        private static readonly int RevealSensitivityID = Shader.PropertyToID("_RevealSensitivity");
        private static readonly int RevealSmoothnessID = Shader.PropertyToID("_RevealSmoothness");
        private static readonly int BaseMapID = Shader.PropertyToID("_BaseMap");
        private static readonly int RevealMapID = Shader.PropertyToID("_RevealMap");
        
        private void OnEnable()
        {
            rendererComponent = GetComponent<Renderer>();
            
            // Try to find the material if not assigned
            if (lightRevealMaterial == null)
            {
                lightRevealMaterial = rendererComponent.sharedMaterial;
                
                // Check if it's using the correct shader
                if (lightRevealMaterial != null && 
                    lightRevealMaterial.shader.name != "Custom/LightRevealTexture")
                {
                    Debug.LogWarning($"[LightRevealMaterial] Material on {gameObject.name} is not using the LightRevealTexture shader!", this);
                }
            }
            
            // Initialize values from material
            if (lightRevealMaterial != null)
            {
                revealSensitivity = lightRevealMaterial.GetFloat(RevealSensitivityID);
                revealSmoothness = lightRevealMaterial.GetFloat(RevealSmoothnessID);
            }
        }
        
        private void Update()
        {
            // Live update in editor only
            if (liveUpdate && lightRevealMaterial != null)
            {
                #if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    UpdateMaterialProperties();
                }
                #endif
            }
        }
        
        /// <summary>
        /// Apply the current inspector values to the material
        /// </summary>
        [ContextMenu("Apply Values to Material")]
        public void UpdateMaterialProperties()
        {
            if (lightRevealMaterial == null)
            {
                Debug.LogError("[LightRevealMaterial] No material assigned!");
                return;
            }
            
            lightRevealMaterial.SetFloat(RevealSensitivityID, revealSensitivity);
            lightRevealMaterial.SetFloat(RevealSmoothnessID, revealSmoothness);
            
            if (showDebug)
            {
                Debug.Log($"[LightRevealMaterial] Updated material properties on {gameObject.name}");
            }
        }
        
        /// <summary>
        /// Read current values from the material
        /// </summary>
        [ContextMenu("Read Values from Material")]
        public void ReadMaterialProperties()
        {
            if (lightRevealMaterial == null)
            {
                Debug.LogError("[LightRevealMaterial] No material assigned!");
                return;
            }
            
            revealSensitivity = lightRevealMaterial.GetFloat(RevealSensitivityID);
            revealSmoothness = lightRevealMaterial.GetFloat(RevealSmoothnessID);
            
            if (showDebug)
            {
                Debug.Log($"[LightRevealMaterial] Read material properties from {gameObject.name}");
            }
        }
        
        /// <summary>
        /// Check if the material is properly set up
        /// </summary>
        [ContextMenu("Validate Material Setup")]
        public void ValidateMaterialSetup()
        {
            if (lightRevealMaterial == null)
            {
                Debug.LogError("[LightRevealMaterial] No material assigned!", this);
                return;
            }
            
            if (lightRevealMaterial.shader.name != "Custom/LightRevealTexture")
            {
                Debug.LogError($"[LightRevealMaterial] Material is using wrong shader: {lightRevealMaterial.shader.name}", this);
                return;
            }
            
            // Check if textures are assigned
            bool hasBaseMap = lightRevealMaterial.GetTexture(BaseMapID) != null;
            bool hasRevealMap = lightRevealMaterial.GetTexture(RevealMapID) != null;
            
            if (!hasBaseMap)
            {
                Debug.LogWarning("[LightRevealMaterial] Base Map (dark texture) is not assigned!", this);
            }
            
            if (!hasRevealMap)
            {
                Debug.LogWarning("[LightRevealMaterial] Reveal Map (light texture) is not assigned!", this);
            }
            
            if (hasBaseMap && hasRevealMap)
            {
                Debug.Log($"[LightRevealMaterial] Material setup is valid on {gameObject.name}!", this);
            }
        }
        
        private void OnDrawGizmos()
        {
            if (!showDebug || !showLightRange) return;
            
            // Draw a wireframe sphere to show approximate light detection range
            Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, visualLightRange);
            
            #if UNITY_EDITOR
            // Draw label
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * (visualLightRange + 0.5f),
                $"Light Reveal\nSensitivity: {revealSensitivity:F1}\nSmoothness: {revealSmoothness:F2}"
            );
            #endif
        }
        
        private void OnDrawGizmosSelected()
        {
            if (!showDebug || !showLightRange) return;
            
            // Draw a more visible sphere when selected
            Gizmos.color = new Color(1f, 1f, 0f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, visualLightRange);
        }
        
        #if UNITY_EDITOR
        private void OnValidate()
        {
            // Auto-update in edit mode if live update is enabled
            if (liveUpdate && lightRevealMaterial != null && !Application.isPlaying)
            {
                UpdateMaterialProperties();
            }
        }
        #endif
    }
}
