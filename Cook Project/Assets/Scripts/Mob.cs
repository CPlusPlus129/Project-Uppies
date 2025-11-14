using System;
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
    private NavMeshAgent agent;
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

    private static Collider[] s_flockingColliderBuffer = new Collider[32];
    private static Mob[] s_flockingMobBuffer = new Mob[32];
    private int flockingLayerMask;

    private Vector3 knockbackVelocity;
    private float knockbackDecaySpeed;

    private int currentHealth;
    private bool hasRegistered;
    private int navMeshAreaMask = NavMesh.AllAreas;

    private const float KinematicCollisionSkin = 0.02f;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        agent = GetComponent<NavMeshAgent>();

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
    }

    private void OnEnable()
    {
        if (!hasRegistered)
        {
            ActiveMobs.Add(this);
            hasRegistered = true;
        }
    }

    private void OnDisable()
    {
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
}
