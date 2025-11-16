using System;
using UnityEngine;

public partial class Mob
{
    #region Inspector Configuration

    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private MobHealthBarController healthBarController;

    [Serializable]
    private class LocomotionSettings
    {
        [Header("Speed")]
        public float baseSpeed = 3.5f;
        [Tooltip("Multiplier applied while chasing the player.")]
        public float chaseSpeedMultiplier = 1.15f;
        [Tooltip("Multiplier applied while attacking, keeps forward momentum up close.")]
        public float attackSpeedMultiplier = 1.25f;
        [Header("Motion Tuning")]
        public float acceleration = 18f;
        public float deceleration = 22f;
        [Tooltip("Degrees per second the mob can rotate while in motion.")]
        public float turnRate = 540f;
        [Tooltip("Damping used when blending towards the desired planar velocity.")]
        public float velocitySmoothing = 12f;
        [Tooltip("How strongly to keep mobs glued to the ground.")]
        public float gravityCompensation = 2f;

        [Header("Navigation Sampling")]
        [Min(0.1f)] public float destinationSampleRadius = 1.25f;
        [Tooltip("If the cached path endpoint drifts further than this (meters), rebuild immediately.")]
        [Min(0.05f)] public float maxDestinationDrift = 0.75f;
        [Tooltip("Time between path rebuilds for non-combat states.")]
        [Min(0.05f)] public float basePathRefreshInterval = 0.45f;
        [Tooltip("Time between path rebuilds for chase state.")]
        [Min(0.05f)] public float chasePathRefreshInterval = 0.18f;
        [Tooltip("Time between path rebuilds for attack state.")]
        [Min(0.05f)] public float attackPathRefreshInterval = 0.1f;
        [Tooltip("Maximum lookahead time (seconds) when predicting the player's future position.")]
        [Min(0f)] public float targetPredictionTime = 0.35f;
        [Tooltip("Cap on predicted offset distance to avoid overshooting corners.")]
        [Min(0.1f)] public float maxPredictionDistance = 6f;
    }

    [Serializable]
    private class AttackSettings
    {
        public float range = 2f;
        public float preferredDistance = 1.5f;
        public float distanceTolerance = 0.6f;
        [Tooltip("Forward pursuit weight when blending nav velocity with attack orbiting.")]
        [Range(0f, 1f)] public float pursuitBlend = 0.55f;
        [Tooltip("Speed (meters/sec) used for orbiting around the player.")]
        public float orbitSpeed = 2.6f;
        [Tooltip("Additional tangential boost applied based on proximity to the preferred distance.")]
        public float orbitBoost = 1.1f;
        [Tooltip("Radial correction factor helping mobs maintain preferred distance during orbit.")]
        public float radialSpringStrength = 3f;
        public float damage = 10f;
        public float cooldown = 1.3f;
    }

    [Serializable]
    private class PerceptionSettings
    {
        public float detectionRange = 30f;
        [Range(0f, 180f)] public float fieldOfView = 160f;
        public float lostSightGrace = 1.6f;
        public LayerMask obstacleLayer = Physics.DefaultRaycastLayers;
        [Tooltip("Layers considered valid targets. Leave empty to auto-detect from the assigned player's colliders.")]
        public LayerMask targetLayers = 0;
    }

    [Serializable]
    private class PatrolSettings
    {
        public bool enabled = true;
        public Vector2 idleTimeRange = new Vector2(1.5f, 3f);
        [Min(0.5f)] public float radius = 6f;
        [Min(0.1f)] public float tolerance = 0.75f;
    }

    [Serializable]
    private class HealthSettings
    {
        public int maxHealth = 50;
    }

    [Serializable]
    public class MoneyRewardSettings
    {
        public bool enabled = true;
        [Min(0)] public int minMoney = 2;
        [Min(0)] public int maxMoney = 5;
        [Header("Popup Placement")]
        public Vector3 popupOffset = new Vector3(0f, 1.35f, 0f);
        [Min(0f)] public float popupHorizontalJitter = 0.35f;
        [Min(0f)] public float popupDepthJitter = 0.15f;
        [Min(0f)] public float popupVerticalJitter = 0.2f;
        [Header("Popup Visual")]
        public Sprite popupSprite;
        public Vector2 popupSpriteSize = new Vector2(1.6f, 0.65f);
        public Color popupSpriteTint = new Color(1f, 0.94f, 0.45f, 0.95f);
        public Color popupTextColor = new Color(1f, 0.98f, 0.88f, 1f);
        [Min(0.1f)] public float popupLifetime = 1.15f;
        [Min(0f)] public float popupRiseDistance = 1.25f;
        public float popupStartScale = 0.65f;
        public float popupEndScale = 1.15f;
        public float popupFontSize = 2.5f;
        public bool popupYAxisOnly = true;
        [Header("Animation Curves")]
        public AnimationCurve popupScaleCurve = AnimationCurve.EaseInOut(0f, 0.4f, 1f, 1.05f);
        public AnimationCurve popupAlphaCurve = new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 10f),
            new Keyframe(0.12f, 1f),
            new Keyframe(0.85f, 1f),
            new Keyframe(1f, 0f));
    }

    [Serializable]
    private class DeathSettings
    {
        public float despawnDelay = 2f;
        public GameObject deathParticles = null;
        public Vector3 particleOffset = Vector3.zero;
        public bool autoDestroyParticles = true;
        [Header("Souls")]
        [Min(0)] public int soulReward = 0;
    }

    [Serializable]
    private class LightDamageSettings
    {
        public float attackLifetimeReduction = 2f;
        public float searchRadius = 15f;
    }

    [Serializable]
    private class FlockingSettings
    {
        public bool enabled = true;
        [Min(0.5f)] public float neighborRadius = 3f;
        public float separationWeight = 2.2f;
        public float alignmentWeight = 0.7f;
        public float cohesionWeight = 0.35f;
    }

    [SerializeField] private LocomotionSettings locomotion = new LocomotionSettings();
    [SerializeField] private AttackSettings attack = new AttackSettings();
    [SerializeField] private PerceptionSettings perception = new PerceptionSettings();
    [SerializeField] private PatrolSettings patrol = new PatrolSettings();
    [SerializeField] private HealthSettings health = new HealthSettings();
    [Header("Death")]
    [SerializeField] private DeathSettings death = new DeathSettings();
    [Header("Money Rewards")]
    [SerializeField] private MoneyRewardSettings moneyReward = new MoneyRewardSettings();
    [SerializeField] private LightDamageSettings lightDamage = new LightDamageSettings();
    [SerializeField] private FlockingSettings flocking = new FlockingSettings();

    [Header("Debug")]
    [SerializeField] private bool showDebug = false;
    [SerializeField] private bool drawGizmos = false;

    #endregion
}
