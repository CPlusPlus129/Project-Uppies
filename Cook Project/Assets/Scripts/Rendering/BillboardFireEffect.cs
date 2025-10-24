using UnityEngine;

namespace NvJ.Rendering
{
    /// <summary>
    /// Manages fire particle effects for billboard sprites.
    /// Automatically positions fire at the feet of the billboard sprite.
    /// </summary>
    [RequireComponent(typeof(BillboardSprite))]
    public class BillboardFireEffect : MonoBehaviour
    {
        [Header("Fire Effect Settings")]
        [Tooltip("The particle system prefab to use for the fire effect")]
        [SerializeField] private ParticleSystem fireParticlePrefab;
        
        [Tooltip("Offset from the bottom of the billboard sprite")]
        [SerializeField] private Vector3 fireOffset = new Vector3(0f, -0.5f, 0f);
        
        [Tooltip("Scale of the fire effect relative to the billboard")]
        [SerializeField] private float fireScale = 0.5f;
        
        [Tooltip("Enable/Disable the fire effect")]
        [SerializeField] private bool enableFire = true;

        [Header("Advanced Settings")]
        [Tooltip("Should the fire automatically adjust to billboard size changes?")]
        [SerializeField] private bool autoAdjustScale = true;
        
        [Tooltip("Multiplier for emission rate based on billboard size")]
        [SerializeField] private float emissionMultiplier = 20f;

        private BillboardSprite billboardSprite;
        private ParticleSystem fireParticleInstance;
        private Transform fireTransform;
        private Vector2 lastBillboardSize;

        private void Awake()
        {
            billboardSprite = GetComponent<BillboardSprite>();
            lastBillboardSize = billboardSprite.size;
        }

        private void Start()
        {
            if (enableFire)
            {
                CreateFireEffect();
            }
        }

        private void Update()
        {
            if (fireParticleInstance != null && autoAdjustScale)
            {
                // Check if billboard size has changed
                if (billboardSprite.size != lastBillboardSize)
                {
                    UpdateFireScale();
                    lastBillboardSize = billboardSprite.size;
                }
            }
        }

        private void OnValidate()
        {
            if (Application.isPlaying && fireParticleInstance != null)
            {
                UpdateFireEffect();
            }
        }

        /// <summary>
        /// Creates the fire particle effect at the feet of the billboard
        /// </summary>
        private void CreateFireEffect()
        {
            if (fireParticlePrefab == null)
            {
                Debug.LogWarning($"BillboardFireEffect on {gameObject.name}: No fire particle prefab assigned!");
                return;
            }

            // Destroy existing fire if any
            if (fireParticleInstance != null)
            {
                Destroy(fireParticleInstance.gameObject);
            }

            // Instantiate the fire particle system
            fireParticleInstance = Instantiate(fireParticlePrefab, transform);
            fireTransform = fireParticleInstance.transform;

            // Position at the feet of the billboard
            UpdateFirePosition();
            UpdateFireScale();

            // Start playing
            if (enableFire)
            {
                fireParticleInstance.Play();
            }
        }

        /// <summary>
        /// Updates the position of the fire effect based on billboard size and offset
        /// </summary>
        private void UpdateFirePosition()
        {
            if (fireTransform == null) return;

            // Calculate the bottom position of the billboard
            float billboardBottom = -billboardSprite.size.y * 0.5f;
            
            // Apply the offset
            Vector3 firePosition = new Vector3(
                fireOffset.x,
                billboardBottom + fireOffset.y,
                fireOffset.z
            );

            fireTransform.localPosition = firePosition;
        }

        /// <summary>
        /// Updates the scale of the fire effect
        /// </summary>
        private void UpdateFireScale()
        {
            if (fireTransform == null) return;

            // Scale fire based on billboard width and the fireScale multiplier
            float scaleFactor = billboardSprite.size.x * fireScale;
            fireTransform.localScale = Vector3.one * scaleFactor;

            // Optionally adjust emission rate based on size
            if (autoAdjustScale && fireParticleInstance != null)
            {
                var emission = fireParticleInstance.emission;
                emission.rateOverTimeMultiplier = scaleFactor * emissionMultiplier;
            }
        }

        /// <summary>
        /// Updates all fire effect properties
        /// </summary>
        private void UpdateFireEffect()
        {
            if (fireParticleInstance == null) return;

            UpdateFirePosition();
            UpdateFireScale();

            // Enable/disable fire
            if (enableFire && !fireParticleInstance.isPlaying)
            {
                fireParticleInstance.Play();
            }
            else if (!enableFire && fireParticleInstance.isPlaying)
            {
                fireParticleInstance.Stop();
            }
        }

        /// <summary>
        /// Enables or disables the fire effect
        /// </summary>
        public void SetFireEnabled(bool enabled)
        {
            enableFire = enabled;
            
            if (fireParticleInstance != null)
            {
                if (enabled)
                {
                    if (!fireParticleInstance.isPlaying)
                    {
                        fireParticleInstance.Play();
                    }
                }
                else
                {
                    if (fireParticleInstance.isPlaying)
                    {
                        fireParticleInstance.Stop();
                    }
                }
            }
        }

        /// <summary>
        /// Sets a new fire particle prefab and recreates the effect
        /// </summary>
        public void SetFirePrefab(ParticleSystem newPrefab)
        {
            fireParticlePrefab = newPrefab;
            CreateFireEffect();
        }

        /// <summary>
        /// Sets the fire offset from the billboard's feet
        /// </summary>
        public void SetFireOffset(Vector3 offset)
        {
            fireOffset = offset;
            UpdateFirePosition();
        }

        /// <summary>
        /// Sets the fire scale relative to the billboard size
        /// </summary>
        public void SetFireScale(float scale)
        {
            fireScale = scale;
            UpdateFireScale();
        }

        /// <summary>
        /// Gets the current fire particle system instance
        /// </summary>
        public ParticleSystem GetFireParticleSystem()
        {
            return fireParticleInstance;
        }

        private void OnDestroy()
        {
            // Clean up fire particle instance
            if (fireParticleInstance != null)
            {
                Destroy(fireParticleInstance.gameObject);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (billboardSprite == null)
            {
                billboardSprite = GetComponent<BillboardSprite>();
            }

            if (billboardSprite != null)
            {
                // Draw a small sphere at the fire position
                Gizmos.color = new Color(1f, 0.5f, 0f, 0.8f); // Orange
                
                float billboardBottom = -billboardSprite.size.y * 0.5f;
                Vector3 firePosition = transform.position + transform.TransformDirection(new Vector3(
                    fireOffset.x,
                    billboardBottom + fireOffset.y,
                    fireOffset.z
                ));

                Gizmos.DrawWireSphere(firePosition, 0.1f * fireScale);
            }
        }
    }
}
