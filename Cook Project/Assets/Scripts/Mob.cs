using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// High level mob brain that handles perception, navigation, combat, and health.
/// Navigation is driven through a NavMeshAgent that feeds a custom locomotion controller so
/// mobs continue moving even while adjusting their path or attacking.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(NavMeshAgent))]
public partial class Mob : MonoBehaviour
{
    #region Public Accessors

    public Transform PlayerTransform => player;
    public static event Action<Mob> MobDied;
    public event Action<Mob> Died;

    #endregion

    #region State

    private enum MobState
    {
        Idle,
        Patrol,
        Chase,
        Attack,
        BreakOff
    }

    private static readonly List<Mob> ActiveMobs = new List<Mob>();
    private Rigidbody body;
    [SerializeField] private NavMeshAgent agent;
    private Animator cachedAnimator;
    private bool isAlive = true;
    private MobState state = MobState.Idle;
    private float stateTimer;
    private float idleDuration;
    private float lastAttackTime = -999f;
    private float lastSeenPlayerTime = float.NegativeInfinity;
    private Vector3 lastKnownPlayerPosition;
    private Vector3 currentDestination;
    private Vector3 destinationRequest;
    private float nextPathRefreshTime;
    private Vector3 desiredPlanarVelocity;
    private Vector3 currentPlanarVelocity;
    private Vector3 playerVelocity;
    private Vector3 previousPlayerPosition;
    private float previousPlayerSampleTime;
    private int orbitDirection;
    private Vector3 spawnPosition;
    private int cachedPlayerLayerMask;
    private int cachedPlayerInstanceId;
    private readonly List<Collider> cachedPlayerColliders = new List<Collider>(8);
    private Vector3 cachedPlayerScale = new Vector3(float.NaN, float.NaN, float.NaN);
    private float nextPlayerLayerRefreshTime;
    private RaycastHit[] visibilityHits = new RaycastHit[32];
    private bool cachedPlayerVisible;
    private int cachedVisibilityFrame = -1;

    private Transform cachedPresentationRoot;
    private Vector3 presentationBaseLocalPosition;
    private Vector3 presentationBaseLocalScale = Vector3.one;
    private Quaternion presentationBaseLocalRotation = Quaternion.identity;
    private bool hasPresentationBaseline;
    private RendererState[] presentationRenderers = Array.Empty<RendererState>();
    private MaterialPropertyBlock deathPresentationBlock;
    private Coroutine deathPresentationRoutine;

    private static Collider[] s_flockingColliderBuffer = new Collider[32];
    private static Mob[] s_flockingMobBuffer = new Mob[32];
    private int flockingLayerMask;

    private Vector3 knockbackVelocity;
    private float knockbackDecaySpeed;

    private int currentHealth;
    private bool hasRegistered;
    private int navMeshAreaMask = NavMesh.AllAreas;

    private const float KinematicCollisionSkin = 0.02f;
    private static readonly int ShaderColorId = Shader.PropertyToID("_Color");
    private static readonly int ShaderBaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ShaderEmissionColorId = Shader.PropertyToID("_EmissionColor");

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        EnsureAgentReference();
        cachedAnimator = GetComponent<Animator>();

        ConfigureRigidbody();
        ConfigureAgent();

        spawnPosition = transform.position;
        currentHealth = health.maxHealth;
        orbitDirection = UnityEngine.Random.value < 0.5f ? 1 : -1;
        idleDuration = GetNextIdleDuration();
        previousPlayerPosition = Vector3.positiveInfinity;
        previousPlayerSampleTime = Time.time;
        flockingLayerMask = 1 << gameObject.layer;

        TryAutoAssignReferences();
        ResetDeathPresentationState(true);
    }

    private void OnValidate()
    {
        EnsureAgentReference();
    }

    private void OnEnable()
    {
        ResetDeathPresentationState();
        if (!hasRegistered)
        {
            ActiveMobs.Add(this);
            hasRegistered = true;
        }
    }

    private void OnDisable()
    {
        StopDeathPresentationRoutine();
        if (hasRegistered)
        {
            ActiveMobs.Remove(this);
            hasRegistered = false;
        }
    }

    private void Start()
    {
        TryAutoAssignPlayer();
        EnsurePlayerLayerMaskUpToDate(true);
        if (healthBarController == null)
        {
            healthBarController = GetComponent<MobHealthBarController>();
        }
    }

    private void Update()
    {
        if (!isAlive)
        {
            return;
        }

        float deltaTime = Time.deltaTime;
        SamplePlayerMotion(deltaTime);
        UpdatePerception(deltaTime);
        UpdateStateMachine(deltaTime);
        UpdateNavigation(deltaTime);
    }

    private void FixedUpdate()
    {
        if (!isAlive)
        {
            return;
        }

        float deltaTime = Time.fixedDeltaTime;
        ApplyMovement(deltaTime);
        UpdateFacing(deltaTime);
    }

    #endregion

    #region Initialization Helpers

    private void ConfigureRigidbody()
    {
        body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.useGravity = false;
        body.detectCollisions = true;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        body.isKinematic = true;
    }

    private void ConfigureAgent()
    {
        if (agent == null)
        {
            return;
        }

        agent.updatePosition = false;
        agent.updateRotation = false;
        agent.speed = locomotion.baseSpeed;
        agent.acceleration = locomotion.acceleration;
        agent.stoppingDistance = 0f;
        agent.autoBraking = false;
        agent.autoRepath = true;
        agent.angularSpeed = locomotion.turnRate;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
    }

    private void EnsureAgentReference()
    {
        if (agent == null)
        {
            agent = GetComponent<NavMeshAgent>();
            if (agent == null)
            {
                agent = gameObject.AddComponent<NavMeshAgent>();
            }
        }
    }

    private void TryAutoAssignReferences()
    {
        if (healthBarController == null)
        {
            healthBarController = GetComponent<MobHealthBarController>();
        }
    }

    private void TryAutoAssignPlayer()
    {
        if (player != null)
        {
            return;
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            EnsurePlayerLayerMaskUpToDate(true);
        }
        else if (showDebug)
        {
            Debug.LogWarning("Mob: Unable to find player with tag 'Player'. Assign target manually.", this);
        }
    }

    #endregion

    #region Death Presentation

    private void ResetDeathPresentationState(bool rebuildCache = false)
    {
        StopDeathPresentationRoutine();
        if (deathPresentation == null)
        {
            return;
        }

        RefreshPresentationCache(rebuildCache);

        if (cachedPresentationRoot != null && hasPresentationBaseline)
        {
            cachedPresentationRoot.localPosition = presentationBaseLocalPosition;
            cachedPresentationRoot.localRotation = presentationBaseLocalRotation;
            cachedPresentationRoot.localScale = presentationBaseLocalScale;
        }

        ClearRendererOverrides();
    }

    private void RefreshPresentationCache(bool forceRendererRebuild)
    {
        if (deathPresentation == null)
        {
            return;
        }

        Transform desiredRoot = deathPresentation.rootOverride != null ? deathPresentation.rootOverride : cachedPresentationRoot;
        if (desiredRoot == null)
        {
            desiredRoot = FindFallbackPresentationRoot();
        }

        if (desiredRoot == null)
        {
            desiredRoot = transform;
        }

        if (desiredRoot != cachedPresentationRoot)
        {
            cachedPresentationRoot = desiredRoot;
            hasPresentationBaseline = false;
            forceRendererRebuild = true;
        }

        if (cachedPresentationRoot != null && !hasPresentationBaseline)
        {
            presentationBaseLocalPosition = cachedPresentationRoot.localPosition;
            presentationBaseLocalRotation = cachedPresentationRoot.localRotation;
            presentationBaseLocalScale = cachedPresentationRoot.localScale;
            hasPresentationBaseline = true;
        }

        if (forceRendererRebuild || presentationRenderers == null || presentationRenderers.Length == 0)
        {
            CacheRendererStates();
        }
    }

    private Transform FindFallbackPresentationRoot()
    {
        if (deathPresentation != null && deathPresentation.rootOverride != null)
        {
            return deathPresentation.rootOverride;
        }

        Transform candidate = null;
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child.GetComponentInChildren<Renderer>(true) != null)
            {
                candidate = child;
                break;
            }
        }

        if (candidate != null)
        {
            return candidate;
        }

        Renderer renderer = GetComponentInChildren<Renderer>(true);
        if (renderer != null)
        {
            Transform current = renderer.transform;
            while (current.parent != null && current.parent != transform)
            {
                current = current.parent;
            }

            if (current != null && current.parent == transform)
            {
                return current;
            }

            return renderer.transform;
        }

        return transform;
    }

    private void CacheRendererStates()
    {
        if (cachedPresentationRoot == null)
        {
            presentationRenderers = Array.Empty<RendererState>();
            return;
        }

        Renderer[] renderers = cachedPresentationRoot.GetComponentsInChildren<Renderer>(true);
        if (renderers == null || renderers.Length == 0)
        {
            presentationRenderers = Array.Empty<RendererState>();
            return;
        }

        if (deathPresentationBlock == null)
        {
            deathPresentationBlock = new MaterialPropertyBlock();
        }

        presentationRenderers = new RendererState[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            RendererState state = new RendererState { Renderer = renderer };
            if (renderer != null)
            {
                Material sharedMaterial = renderer.sharedMaterial;
                if (sharedMaterial != null)
                {
                    if (sharedMaterial.HasProperty(ShaderColorId))
                    {
                        state.HasColor = true;
                        state.Color = sharedMaterial.GetColor(ShaderColorId);
                    }

                    if (sharedMaterial.HasProperty(ShaderBaseColorId))
                    {
                        state.HasBaseColor = true;
                        state.BaseColor = sharedMaterial.GetColor(ShaderBaseColorId);
                    }

                    if (sharedMaterial.HasProperty(ShaderEmissionColorId))
                    {
                        state.HasEmission = true;
                        state.EmissionColor = sharedMaterial.GetColor(ShaderEmissionColorId);
                    }
                }
            }

            presentationRenderers[i] = state;
        }
    }

    private void ClearRendererOverrides()
    {
        if (presentationRenderers == null || presentationRenderers.Length == 0 || deathPresentationBlock == null)
        {
            return;
        }

        deathPresentationBlock.Clear();
        for (int i = 0; i < presentationRenderers.Length; i++)
        {
            Renderer renderer = presentationRenderers[i].Renderer;
            if (renderer == null)
            {
                continue;
            }

            renderer.SetPropertyBlock(deathPresentationBlock);
        }
    }

    private void StopDeathPresentationRoutine()
    {
        if (deathPresentationRoutine != null)
        {
            StopCoroutine(deathPresentationRoutine);
            deathPresentationRoutine = null;
        }
    }

    private void TriggerDeathPresentation()
    {
        if (deathPresentation == null || !deathPresentation.enabled || !gameObject.activeInHierarchy)
        {
            enabled = false;
            return;
        }

        RefreshPresentationCache(true);
        StopDeathPresentationRoutine();
        deathPresentationRoutine = StartCoroutine(DeathPresentationRoutine());
    }

    private IEnumerator DeathPresentationRoutine()
    {
        if (cachedPresentationRoot == null)
        {
            enabled = false;
            yield break;
        }

        if (deathPresentation.disableAnimator && cachedAnimator != null)
        {
            cachedAnimator.enabled = false;
        }

        float duration = Mathf.Max(0.05f, deathPresentation.duration);
        Vector2 spinRange = deathPresentation.spinDegrees;
        float minSpin = Mathf.Min(spinRange.x, spinRange.y);
        float maxSpin = Mathf.Max(spinRange.x, spinRange.y);
        float totalSpin = UnityEngine.Random.Range(minSpin, maxSpin);
        if (UnityEngine.Random.value < 0.5f)
        {
            totalSpin *= -1f;
        }

        Vector3 wobbleAxis = UnityEngine.Random.insideUnitSphere;
        wobbleAxis.y = 0f;
        if (wobbleAxis.sqrMagnitude < Mathf.Epsilon)
        {
            wobbleAxis = Vector3.forward;
        }
        wobbleAxis.Normalize();

        Vector3 drift = Vector3.zero;
        if (deathPresentation.driftRadius > 0f)
        {
            Vector2 driftCircle = UnityEngine.Random.insideUnitCircle * deathPresentation.driftRadius;
            drift = new Vector3(driftCircle.x, 0f, driftCircle.y);
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            ApplyDeathTransform(cachedPresentationRoot, t, totalSpin, wobbleAxis, drift);

            float fade = deathPresentation.fadeCurve.Evaluate(t);
            float emission = deathPresentation.emissionCurve.Evaluate(t);
            float flash = deathPresentation.flashDuration <= Mathf.Epsilon
                ? 0f
                : Mathf.Clamp01(1f - (elapsed / deathPresentation.flashDuration));
            ApplyRendererPresentation(fade, emission, flash);

            yield return null;
        }

        ApplyRendererPresentation(0f, 0f, 0f);
        deathPresentationRoutine = null;
        enabled = false;
    }

    private void ApplyDeathTransform(Transform target, float normalizedTime, float totalSpin, Vector3 wobbleAxis, Vector3 drift)
    {
        if (target == null)
        {
            return;
        }

        float easedTime = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(normalizedTime));
        float heightOffset = deathPresentation.heightCurve.Evaluate(easedTime) * deathPresentation.heightMultiplier;
        float scaleFactor = Mathf.Max(0f, deathPresentation.scaleCurve.Evaluate(easedTime));
        float wobbleEnvelope = 1f - Mathf.Clamp01(easedTime * 0.85f);
        float wobbleAngle = Mathf.Sin(easedTime * Mathf.PI * deathPresentation.wobbleFrequency) * deathPresentation.tiltAngle * wobbleEnvelope;

        Vector3 offset = drift * easedTime;
        offset.y += heightOffset;

        target.localPosition = presentationBaseLocalPosition + offset;
        target.localRotation = presentationBaseLocalRotation
            * Quaternion.AngleAxis(totalSpin * easedTime, Vector3.up)
            * Quaternion.AngleAxis(wobbleAngle, wobbleAxis);
        target.localScale = presentationBaseLocalScale * scaleFactor;
    }

    private void ApplyRendererPresentation(float visibility, float emissionWeight, float flashWeight)
    {
        if (presentationRenderers == null || presentationRenderers.Length == 0 || deathPresentationBlock == null)
        {
            return;
        }

        float clampedVisibility = Mathf.Clamp01(visibility);
        float baseEmission = Mathf.Max(0f, emissionWeight);
        float flashIntensity = flashWeight <= 0f ? 0f : flashWeight * Mathf.Max(0f, deathPresentation.flashIntensity);

        for (int i = 0; i < presentationRenderers.Length; i++)
        {
            RendererState state = presentationRenderers[i];
            Renderer renderer = state.Renderer;
            if (renderer == null)
            {
                continue;
            }

            deathPresentationBlock.Clear();

            if (state.HasColor)
            {
                Color color = state.Color;
                color.a *= clampedVisibility;
                deathPresentationBlock.SetColor(ShaderColorId, color);
            }

            if (state.HasBaseColor)
            {
                Color baseColor = state.BaseColor;
                baseColor.a *= clampedVisibility;
                deathPresentationBlock.SetColor(ShaderBaseColorId, baseColor);
            }

            if (state.HasEmission)
            {
                Color emission = state.EmissionColor * baseEmission;
                if (flashIntensity > 0f)
                {
                    emission += deathPresentation.flashColor * flashIntensity;
                }

                deathPresentationBlock.SetColor(ShaderEmissionColorId, emission);
            }

            renderer.SetPropertyBlock(deathPresentationBlock);
        }
    }

    private struct RendererState
    {
        public Renderer Renderer;
        public bool HasColor;
        public bool HasBaseColor;
        public bool HasEmission;
        public Color Color;
        public Color BaseColor;
        public Color EmissionColor;
    }

    #endregion
}
