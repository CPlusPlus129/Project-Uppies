using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Drives a Skyrim-style clairvoyance particle from the player's feet to the currently glowing fridge.
/// Computes a NavMesh path to active glow targets and slides a particle effect along the path.
/// </summary>
public class PlayerFridgeGuidance : MonoBehaviour
{
    [Header("Effect")]
    [SerializeField] private ParticleSystem guidanceParticlePrefab;
    [SerializeField] private float particleHeightOffset = 0.1f;
    [SerializeField] private float particleMoveSpeed = 5f;

    [Header("Path Settings")]
    [SerializeField] private float pathUpdateInterval = 0.35f;
    [SerializeField] private float navMeshSampleRadius = 2f;
    [SerializeField] private float retargetInterval = 1.5f;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;

    private readonly List<Vector3> pathPoints = new List<Vector3>();
    private readonly List<float> segmentLengths = new List<float>();
    private readonly Dictionary<FoodSource, FridgeGlowController> controllerCache = new Dictionary<FoodSource, FridgeGlowController>();

    private ParticleSystem guidanceParticleInstance;
    private Transform guidanceTransform;
    private IFridgeGlowManager fridgeGlowManager;
    private NavMeshPath navMeshPath;

    private FoodSource currentTarget;
    private float pathTimer;
    private float travelDistance;
    private float pathLength;
    private int lastEligibilityVersion = -1;
    private bool hasValidPath;
    private bool isInitialized;
    private float retargetTimer;

    private void Awake()
    {
        navMeshPath = new NavMeshPath();
        InitializeAsync().Forget();
    }

    private async UniTaskVoid InitializeAsync()
    {
        if (guidanceParticlePrefab == null)
        {
            Debug.LogError("PlayerFridgeGuidance: Guidance particle prefab is not assigned.");
            enabled = false;
            return;
        }

        CancellationToken destroyToken = this.GetCancellationTokenOnDestroy();

        try
        {
            await UniTask.WaitUntil(() => GameFlow.Instance != null && GameFlow.Instance.isInitialized, cancellationToken: destroyToken);
            fridgeGlowManager = await ServiceLocator.Instance.GetAsync<IFridgeGlowManager>();
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (destroyToken.IsCancellationRequested)
        {
            return;
        }

        if (fridgeGlowManager == null)
        {
            Debug.LogError("PlayerFridgeGuidance: Failed to resolve IFridgeGlowManager.");
            enabled = false;
            return;
        }

        CreateParticleInstance();
        fridgeGlowManager.EligibleFridgesChanged += HandleEligibleFridgesChanged;
        lastEligibilityVersion = fridgeGlowManager.EligibilityVersion;
        UpdateTarget(fridgeGlowManager.GetEligibleFridgesSnapshot());

        isInitialized = true;
    }

    private void OnDestroy()
    {
        if (fridgeGlowManager != null)
        {
            fridgeGlowManager.EligibleFridgesChanged -= HandleEligibleFridgesChanged;
        }

        if (guidanceParticleInstance != null)
        {
            Destroy(guidanceParticleInstance.gameObject);
            guidanceParticleInstance = null;
            guidanceTransform = null;
        }
    }

    private void Update()
    {
        if (!isInitialized || guidanceParticleInstance == null)
        {
            return;
        }

        retargetTimer += Time.deltaTime;
        if (retargetTimer >= retargetInterval)
        {
            retargetTimer = 0f;
            if (fridgeGlowManager != null && (currentTarget == null || !hasValidPath))
            {
                UpdateTarget(fridgeGlowManager.GetEligibleFridgesSnapshot());
            }
        }

        if (fridgeGlowManager != null && fridgeGlowManager.EligibilityVersion != lastEligibilityVersion)
        {
            lastEligibilityVersion = fridgeGlowManager.EligibilityVersion;
            UpdateTarget(fridgeGlowManager.GetEligibleFridgesSnapshot());
        }
        else if (currentTarget != null && !IsFridgeCurrentlyGlowing(currentTarget))
        {
            UpdateTarget(fridgeGlowManager?.GetEligibleFridgesSnapshot());
        }

        pathTimer += Time.deltaTime;
        if (pathTimer >= pathUpdateInterval)
        {
            pathTimer = 0f;
            RebuildPath();
        }

        if (!hasValidPath)
        {
            return;
        }

        MoveParticleAlongPath(Time.deltaTime);
    }

    private void HandleEligibleFridgesChanged(IReadOnlyCollection<FoodSource> fridges)
    {
        UpdateTarget(fridges);
    }

    private void UpdateTarget(IReadOnlyCollection<FoodSource> fridges)
    {
        FoodSource selected = SelectBestTarget(fridges);

        if (selected == null)
        {
            ClearPath();
            currentTarget = null;
            return;
        }

        if (currentTarget == selected && hasValidPath)
        {
            return;
        }

        currentTarget = selected;
        if (enableDebugLogs)
        {
            Debug.Log($"PlayerFridgeGuidance: Target fridge set to {currentTarget.ItemName}");
        }

        RebuildPath(forceResetTravel: true);
    }

    private FoodSource SelectBestTarget(IReadOnlyCollection<FoodSource> fridges)
    {
        if (fridges == null)
        {
            return null;
        }

        Vector3 playerPos = transform.position;
        float bestDistanceSq = float.MaxValue;
        FoodSource best = null;

        foreach (FoodSource fridge in fridges)
        {
            if (fridge == null || !fridge.gameObject.activeInHierarchy)
            {
                continue;
            }

            if (!IsFridgeCurrentlyGlowing(fridge))
            {
                continue;
            }

            float distanceSq = (fridge.transform.position - playerPos).sqrMagnitude;
            if (distanceSq < bestDistanceSq)
            {
                bestDistanceSq = distanceSq;
                best = fridge;
            }
        }

        return best;
    }

    private bool IsFridgeCurrentlyGlowing(FoodSource fridge)
    {
        if (fridge == null)
        {
            return false;
        }

        if (!controllerCache.TryGetValue(fridge, out FridgeGlowController controller) || controller == null)
        {
            if (!fridge.TryGetComponent(out controller))
            {
                controller = null;
            }
            controllerCache[fridge] = controller;
        }

        return controller != null && controller.IsGlowing;
    }

    private void CreateParticleInstance()
    {
        if (guidanceParticleInstance != null)
        {
            return;
        }

        guidanceParticleInstance = Instantiate(guidanceParticlePrefab, transform.position, Quaternion.identity);
        guidanceTransform = guidanceParticleInstance.transform;
        guidanceParticleInstance.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        guidanceParticleInstance.gameObject.SetActive(false);
    }

    private void RebuildPath(bool forceResetTravel = false)
    {
        if (currentTarget == null || !currentTarget.gameObject.activeInHierarchy)
        {
            ClearPath();
            return;
        }

        Vector3 playerPos = transform.position;
        Vector3 targetPos = currentTarget.transform.position;

        if (NavMesh.SamplePosition(playerPos, out NavMeshHit startHit, navMeshSampleRadius, NavMesh.AllAreas))
        {
            playerPos = startHit.position;
        }

        if (!NavMesh.SamplePosition(targetPos, out NavMeshHit endHit, navMeshSampleRadius, NavMesh.AllAreas))
        {
            if (enableDebugLogs)
            {
                Debug.LogWarning($"PlayerFridgeGuidance: Unable to sample NavMesh near target fridge {currentTarget.ItemName}");
            }
            ClearPath();
            return;
        }

        bool pathBuilt = NavMesh.CalculatePath(playerPos, endHit.position, NavMesh.AllAreas, navMeshPath);

        if (!pathBuilt || navMeshPath.status != NavMeshPathStatus.PathComplete || navMeshPath.corners == null || navMeshPath.corners.Length < 2)
        {
            if (enableDebugLogs)
            {
                Debug.LogWarning("PlayerFridgeGuidance: Failed to compute NavMesh path to glowing fridge.");
            }
            ClearPath();
            return;
        }

        pathPoints.Clear();
        segmentLengths.Clear();
        pathLength = 0f;

        for (int i = 0; i < navMeshPath.corners.Length; i++)
        {
            Vector3 corner = navMeshPath.corners[i];
            corner.y += particleHeightOffset;
            pathPoints.Add(corner);
        }

        for (int i = 0; i < pathPoints.Count - 1; i++)
        {
            float segmentLength = Vector3.Distance(pathPoints[i], pathPoints[i + 1]);
            segmentLengths.Add(segmentLength);
            pathLength += segmentLength;
        }

        hasValidPath = pathLength > Mathf.Epsilon;

        if (!hasValidPath)
        {
            ClearPath();
            return;
        }

        if (forceResetTravel)
        {
            travelDistance = 0f;
        }
        else
        {
            travelDistance = Mathf.Min(travelDistance, pathLength);
        }

        guidanceParticleInstance.gameObject.SetActive(true);
        guidanceParticleInstance.Play();
        guidanceTransform.position = pathPoints[0];
        guidanceTransform.forward = (pathPoints.Count > 1 ? (pathPoints[1] - pathPoints[0]).normalized : transform.forward);
    }

    private void MoveParticleAlongPath(float deltaTime)
    {
        if (!hasValidPath || guidanceTransform == null)
        {
            return;
        }

        travelDistance += particleMoveSpeed * deltaTime;

        if (travelDistance > pathLength)
        {
            travelDistance = 0f;
        }

        Vector3 forward;
        Vector3 newPosition = EvaluatePositionAtDistance(travelDistance, out forward);
        guidanceTransform.position = newPosition;

        if (forward.sqrMagnitude > 0.0001f)
        {
            guidanceTransform.rotation = Quaternion.LookRotation(forward, Vector3.up);
        }
    }

    private Vector3 EvaluatePositionAtDistance(float distance, out Vector3 forward)
    {
        forward = Vector3.zero;

        if (pathPoints.Count == 0)
        {
            return transform.position;
        }

        float remaining = distance;
        for (int i = 0; i < segmentLengths.Count; i++)
        {
            float segmentLength = segmentLengths[i];
            if (segmentLength <= Mathf.Epsilon)
            {
                continue;
            }

            if (remaining <= segmentLength)
            {
                float t = remaining / segmentLength;
                Vector3 start = pathPoints[i];
                Vector3 end = pathPoints[i + 1];
                forward = (end - start).normalized;
                return Vector3.Lerp(start, end, t);
            }

            remaining -= segmentLength;
        }

        if (segmentLengths.Count > 0)
        {
            Vector3 lastStart = pathPoints[pathPoints.Count - 2];
            Vector3 lastEnd = pathPoints[pathPoints.Count - 1];
            forward = (lastEnd - lastStart).normalized;
        }

        return pathPoints[pathPoints.Count - 1];
    }

    private void ClearPath()
    {
        hasValidPath = false;
        pathPoints.Clear();
        segmentLengths.Clear();
        pathLength = 0f;
        travelDistance = 0f;

        if (guidanceParticleInstance != null)
        {
            guidanceParticleInstance.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            guidanceParticleInstance.gameObject.SetActive(false);
        }
    }
}
