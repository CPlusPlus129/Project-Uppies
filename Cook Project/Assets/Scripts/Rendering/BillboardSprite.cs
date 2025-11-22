using UnityEngine;

namespace NvJ.Rendering
{
    /// <summary>
    /// Helper component for setting up billboard sprites.
    /// Automatically creates and configures the sprite renderer with billboard materials.
    /// Properly handles sliced sprite sheets by using sprite UV coordinates.
    /// </summary>
    public class BillboardSprite : MonoBehaviour
    {
        [Header("Sprite Settings")]
        [SerializeField] private Sprite sprite;
        [SerializeField] private Color tint = Color.white;
        public Vector2 size = Vector2.one;
        
        [Header("Billboard Type")]
        [Tooltip("Full: Sprite always faces camera. Y-Axis Only: Sprite rotates around Y-axis only (good for characters)")]
        [SerializeField] private BillboardMode billboardMode = BillboardMode.YAxisOnly;
        
        [Header("Rendering")]
        [SerializeField] private bool useLighting = true;
        [SerializeField] private bool castShadows = true;
        [SerializeField, Range(0f, 1f)] private float alphaCutoff = 0.5f;
        
        [Header("Lighting")]
        [Tooltip("Billboard Normal: Normal faces camera (good for camera-aligned lights)\nWorld Up: Normal points up (best for overhead lights)\nSpherical: Blended normal for rounded appearance")]
        [SerializeField] private LightingMode lightingMode = LightingMode.WorldUp;
        
        public enum LightingMode
        {
            BillboardNormal = 0,
            WorldUp = 1,
            Spherical = 2
        }
        
        [Header("Sorting")]
        [SerializeField] private int sortingOrder = 0;
        [SerializeField] private string sortingLayerName = "Default";

        private MeshRenderer meshRenderer;
        private MeshFilter meshFilter;
        private Material billboardMaterial;

        public enum BillboardMode
        {
            Full = 0,
            YAxisOnly = 1
        }

        private void OnValidate()
        {
            // Only auto-update during play mode to avoid creating runtime assets in edit mode
            // In edit mode, use the "Refresh Billboard" button in the Inspector instead
            if (Application.isPlaying)
            {
                UpdateBillboard();
            }
        }

        private void Start()
        {
            SetupBillboard();
        }

        private void SetupBillboard()
        {
            // Create mesh filter and renderer if needed
            if (meshFilter == null)
            {
                meshFilter = GetComponent<MeshFilter>();
                if (meshFilter == null)
                {
                    meshFilter = gameObject.AddComponent<MeshFilter>();
                }
            }

            if (meshRenderer == null)
            {
                meshRenderer = GetComponent<MeshRenderer>();
                if (meshRenderer == null)
                {
                    meshRenderer = gameObject.AddComponent<MeshRenderer>();
                }
            }

            // Create quad mesh
            CreateQuadMesh();

            // Create and assign material
            CreateMaterial();

            // Configure renderer
            ConfigureRenderer();
        }

        private void CreateQuadMesh()
        {
            Mesh mesh = new Mesh();
            mesh.name = "Billboard Quad";

            float halfWidth = size.x * 0.5f;
            float halfHeight = size.y * 0.5f;

            // Vertices (centered on origin)
            mesh.vertices = new Vector3[]
            {
                new Vector3(-halfWidth, -halfHeight, 0),
                new Vector3(halfWidth, -halfHeight, 0),
                new Vector3(-halfWidth, halfHeight, 0),
                new Vector3(halfWidth, halfHeight, 0)
            };

            // UVs - Calculate from sprite if available
            Vector2[] uvs = CalculateSpriteUVs();
            mesh.uv = uvs;

            // Triangles
            mesh.triangles = new int[] { 0, 2, 1, 2, 3, 1 };

            // Normals (pointing back in object space, will be transformed by billboard shader)
            mesh.normals = new Vector3[]
            {
                Vector3.back,
                Vector3.back,
                Vector3.back,
                Vector3.back
            };

            // Tangents (for normal mapping support)
            mesh.tangents = new Vector4[]
            {
                new Vector4(1, 0, 0, 1),  // Right direction
                new Vector4(1, 0, 0, 1),
                new Vector4(1, 0, 0, 1),
                new Vector4(1, 0, 0, 1)
            };

            mesh.RecalculateBounds();
            meshFilter.sharedMesh = mesh;
        }

        /// <summary>
        /// Calculate proper UV coordinates for the sprite.
        /// This handles both full textures and sliced sprite sheets.
        /// </summary>
        private Vector2[] CalculateSpriteUVs()
        {
            if (sprite == null || sprite.texture == null)
            {
                // Default UVs if no sprite or texture
                return new Vector2[]
                {
                    new Vector2(0, 0),
                    new Vector2(1, 0),
                    new Vector2(0, 1),
                    new Vector2(1, 1)
                };
            }

            // Get the sprite's texture rect in pixels
            Rect textureRect = sprite.textureRect;
            Texture2D texture = sprite.texture;

            // Calculate normalized UV coordinates
            float uMin = textureRect.xMin / texture.width;
            float uMax = textureRect.xMax / texture.width;
            float vMin = textureRect.yMin / texture.height;
            float vMax = textureRect.yMax / texture.height;

            return new Vector2[]
            {
                new Vector2(uMin, vMin),  // Bottom-left
                new Vector2(uMax, vMin),  // Bottom-right
                new Vector2(uMin, vMax),  // Top-left
                new Vector2(uMax, vMax)   // Top-right
            };
        }

        private void CreateMaterial()
        {
            // Find the appropriate shader
            string shaderName = useLighting ? "Billboard/Sprite Lit" : "Billboard/Sprite Unlit";
            Shader shader = Shader.Find(shaderName);

            if (shader == null)
            {
                Debug.LogError($"BillboardSprite: Shader '{shaderName}' not found! Make sure shaders are in project.");
                return;
            }

            // Destroy old material if it exists and doesn't match the shader
            if (billboardMaterial != null && billboardMaterial.shader != shader)
            {
                if (Application.isPlaying)
                {
                    Destroy(billboardMaterial);
                }
                else
                {
                    DestroyImmediate(billboardMaterial);
                }
                billboardMaterial = null;
            }

            // Create new material if needed
            if (billboardMaterial == null)
            {
                billboardMaterial = new Material(shader);
                billboardMaterial.name = $"Billboard Material ({gameObject.name})";
            }

            UpdateMaterial();

            if (meshRenderer != null)
            {
                meshRenderer.sharedMaterial = billboardMaterial;
            }
        }

        private void UpdateMaterial()
        {
            if (billboardMaterial == null)
            {
                Debug.LogWarning($"BillboardSprite on {gameObject.name}: Cannot update material - material is null. Call CreateMaterial first.");
                return;
            }

            // Set properties
            if (sprite != null)
            {
                billboardMaterial.SetTexture("_BaseMap", sprite.texture);
            }

            billboardMaterial.SetColor("_BaseColor", tint);
            billboardMaterial.SetFloat("_BillboardType", (float)billboardMode);
            
            // Check if shader has the lighting mode property before setting it
            if (billboardMaterial.HasProperty("_LightingMode"))
            {
                billboardMaterial.SetFloat("_LightingMode", (float)lightingMode);
            }
            else if (useLighting)
            {
                Debug.LogWarning($"BillboardSprite on {gameObject.name}: Material doesn't have _LightingMode property. Shader may need to be updated.");
            }
            
            billboardMaterial.SetFloat("_Cutoff", alphaCutoff);
        }

        private void ConfigureRenderer()
        {
            if (meshRenderer == null) return;

            // Shadow settings
            meshRenderer.shadowCastingMode = castShadows 
                ? UnityEngine.Rendering.ShadowCastingMode.On 
                : UnityEngine.Rendering.ShadowCastingMode.Off;

            meshRenderer.receiveShadows = useLighting;

            // Sorting
            meshRenderer.sortingOrder = sortingOrder;
            meshRenderer.sortingLayerName = sortingLayerName;
        }

        private void UpdateBillboard()
        {
            // Ensure we have the necessary components
            if (meshRenderer == null)
            {
                meshRenderer = GetComponent<MeshRenderer>();
            }
            
            if (meshFilter == null)
            {
                meshFilter = GetComponent<MeshFilter>();
            }
            
            // If material doesn't exist or wrong shader, recreate it
            if (billboardMaterial == null || meshRenderer.sharedMaterial != billboardMaterial)
            {
                CreateMaterial();
            }
            else if (billboardMaterial != null)
            {
                UpdateMaterial();
            }
            
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                CreateQuadMesh();
            }
            
            ConfigureRenderer();
        }

        // Public API
        public void SetSprite(Sprite newSprite)
        {
            sprite = newSprite;
            
            // Update both texture and UVs
            if (sprite != null)
            {
                if (billboardMaterial != null)
                {
                    billboardMaterial.SetTexture("_BaseMap", sprite.texture);
                }
                
                // Recreate mesh with new UVs
                CreateQuadMesh();
            }
        }

        public void SetTint(Color newTint)
        {
            tint = newTint;
            if (billboardMaterial != null)
            {
                billboardMaterial.SetColor("_BaseColor", tint);
            }
        }

        public void SetSize(Vector2 newSize)
        {
            size = newSize;
            CreateQuadMesh();
        }

        public void SetBillboardMode(BillboardMode mode)
        {
            billboardMode = mode;
            if (billboardMaterial != null)
            {
                billboardMaterial.SetFloat("_BillboardType", (float)mode);
            }
        }

        public void SetLightingMode(LightingMode mode)
        {
            lightingMode = mode;
            if (billboardMaterial != null)
            {
                billboardMaterial.SetFloat("_LightingMode", (float)mode);
            }
        }

        /// <summary>
        /// Gets the current sprite being displayed
        /// </summary>
        public Sprite GetSprite()
        {
            return sprite;
        }

        /// <summary>
        /// Refreshes the billboard - useful after changing sprite in inspector
        /// </summary>
        public void Refresh()
        {
            UpdateBillboard();
        }
        
        /// <summary>
        /// Forces a complete recreation of the billboard (mesh, material, and renderer)
        /// Use this if the billboard is not responding to lighting changes
        /// </summary>
        public void ForceRecreate()
        {
            // Destroy existing material
            if (billboardMaterial != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(billboardMaterial);
                }
                else
                {
                    DestroyImmediate(billboardMaterial);
                }
                billboardMaterial = null;
            }
            
            // Recreate everything
            SetupBillboard();
            
            Debug.Log($"BillboardSprite on {gameObject.name}: Forced recreation complete.");
        }
    }
}
