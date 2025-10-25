using Cysharp.Threading.Tasks;
using UnityEngine;

namespace NvJ.Rendering
{
    /// <summary>
    /// Manages a particle effect that covers the entire billboard sprite.
    /// Can be used for effects like teleporting or dissolving.
    /// </summary>
    [RequireComponent(typeof(BillboardSprite))]
    public class FlamePillarEffect : MonoBehaviour
    {
        [Header("Particle Effect Settings")]
        [Tooltip("The particle system prefab to use for the effect")]
        [SerializeField] private ParticleSystem particlePrefab;
        
        [Tooltip("Offset from the center of the billboard sprite")]
        [SerializeField] private Vector3 particleOffset = Vector3.zero;
        
        [Tooltip("Scale of the effect relative to the billboard")]
        [SerializeField] private float particleScale = 1.0f;
        
        [Tooltip("Enable/Disable the effect on start")]
        [SerializeField] private bool playOnStart = true;

        [Header("Advanced Settings")]
        [Tooltip("Should the effect automatically adjust to billboard size changes?")]
        [SerializeField] private bool autoAdjustScale = true;
        
        [Tooltip("Multiplier for emission rate based on billboard size")]
        [SerializeField] private float emissionMultiplier = 50f;

        private BillboardSprite billboardSprite;
        private ParticleSystem particleInstance;
        private Transform particleTransform;
        private Vector2 lastBillboardSize;

        private void Awake()
        {
            billboardSprite = GetComponent<BillboardSprite>();
            lastBillboardSize = billboardSprite.size;
        }

        private void Start()
        {
            if (playOnStart)
            {
                CreateEffect();
            }
        }

        private void Update()
        {
            if (particleInstance != null && autoAdjustScale)
            {
                if (billboardSprite.size != lastBillboardSize)
                {
                    UpdateParticleScale();
                    lastBillboardSize = billboardSprite.size;
                }
            }
        }

        private void OnValidate()
        {
            if (Application.isPlaying && particleInstance != null)
            {
                UpdateEffect();
            }
        }

        /// <summary>
        /// Creates the particle effect.
        /// </summary>
        private void CreateEffect()
        {
            if (particlePrefab == null)
            {
                Debug.LogWarning($"FlamePillarEffect on {gameObject.name}: No particle prefab assigned!");
                return;
            }

            if (particleInstance != null)
            {
                Destroy(particleInstance.gameObject);
            }

            particleInstance = Instantiate(particlePrefab, transform);
            particleTransform = particleInstance.transform;

            UpdateParticlePosition();
            UpdateParticleScale();

            if (playOnStart)
            {
                particleInstance.Play();
            }
        }

        /// <summary>
        /// Updates the position of the effect based on billboard size and offset.
        /// </summary>
        private void UpdateParticlePosition()
        {
            if (particleTransform == null) return;
            particleTransform.localPosition = particleOffset;
        }

        /// <summary>
        /// Updates the scale of the effect to cover the billboard.
        /// </summary>
        private void UpdateParticleScale()
        {
            if (particleTransform == null) return;

            float scaleFactor = Mathf.Max(billboardSprite.size.x, billboardSprite.size.y) * particleScale;
            particleTransform.localScale = Vector3.one * scaleFactor;

            if (autoAdjustScale && particleInstance != null)
            {
                var emission = particleInstance.emission;
                emission.rateOverTimeMultiplier = scaleFactor * emissionMultiplier;
            }
        }

        /// <summary>
        /// Updates all effect properties.
        /// </summary>
        private void UpdateEffect()
        {
            if (particleInstance == null) return;

            UpdateParticlePosition();
            UpdateParticleScale();

            if (playOnStart && !particleInstance.isPlaying)
            {
                particleInstance.Play();
            }
            else if (!playOnStart && particleInstance.isPlaying)
            {
                particleInstance.Stop();
            }
        }

        /// <summary>
        /// Plays the effect and destroys the GameObject after a delay.
        /// </summary>
        /// <param name="destructionDelay">Delay in seconds before destroying the GameObject.</param>
        public void PlayAndDestroy(float destructionDelay)
        {
            if (particleInstance == null)
            {
                CreateEffect();
            }
            
            if (particleInstance != null)
            {
                particleInstance.Play();
                Destroy(gameObject, destructionDelay);
            }
        }

        public async UniTask PlayTeleportEffect(int startRate = 10, int endRate = 50, float rampDuration = 2f, bool hideSprite = false)
        {
            // Reset the state
            GetComponent<BillboardSprite>().enabled = true;
            gameObject.SetActive(true);

            if (particleInstance == null)
            {
                CreateEffect();
            }
            
            var emission = particleInstance.emission;
            emission.enabled = true;

            // Ramp up
            await ChangeEmissionRateOverTime(startRate, endRate, rampDuration);

            // Hide sprite
            if (hideSprite)
            {
                GetComponent<MeshRenderer>().enabled = false;
                GetComponent<BillboardFireEffect>().SetFireEnabled(false);
            }

            // Ramp down
            await ChangeEmissionRateOverTime(endRate, 0, rampDuration/2);

            // Wait for particles to die
            await UniTask.Delay(2000);

            // Deactivate for next time
            gameObject.SetActive(false);
        }

        public async UniTask ChangeEmissionRateOverTime(float startRate, float endRate, float duration)
        {
            if (particleInstance == null)
            {
                CreateEffect();
            }

            if (particleInstance == null)
            {
                Debug.LogWarning($"FlamePillarEffect on {gameObject.name}: No particle prefab assigned!");
                return;
            }

            var emission = particleInstance.emission;
            float time = 0;

            while (time < duration)
            {
                time += Time.deltaTime;
                float rate = Mathf.Lerp(startRate, endRate, time / duration);
                emission.rateOverTime = rate;
                await UniTask.Yield();
            }

            emission.rateOverTime = endRate;
        }

        public void SetParticlePrefab(ParticleSystem newPrefab)
        {
            particlePrefab = newPrefab;
            CreateEffect();
        }

        public void SetParticleOffset(Vector3 offset)
        {
            particleOffset = offset;
            UpdateParticlePosition();
        }



        public void SetParticleScale(float scale)
        {
            particleScale = scale;
            UpdateParticleScale();
        }



        public ParticleSystem GetParticleSystem()
        {
            return particleInstance;
        }

        private void OnDestroy()
        {
            if (particleInstance != null)
            {
                Destroy(particleInstance.gameObject);
            }
        }
    }
}