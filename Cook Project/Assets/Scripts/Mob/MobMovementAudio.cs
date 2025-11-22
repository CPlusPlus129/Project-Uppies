using UnityEngine;

[RequireComponent(typeof(Mob))]
[RequireComponent(typeof(AudioSource))]
public class MobMovementAudio : MonoBehaviour
{
    [Header("Audio Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip movementClip;
    [SerializeField, Range(0f, 1f)] private float maxVolume = 0.8f;
    [SerializeField] private float fadeSpeed = 6f;

    [Header("Movement Detection")]
    [SerializeField, Tooltip("Minimum planar speed (m/s) before the loop begins to fade in.")]
    private float speedThreshold = 0.15f;
    [SerializeField, Tooltip("Planar speed (m/s) that yields full movement volume.")]
    private float fullSpeed = 3f;
    [SerializeField, Tooltip("Lerp factor for smoothing raw speed samples.")]
    private float speedSmoothing = 8f;

    [Header("Distance Attenuation")]
    [SerializeField, Tooltip("Automatically bind to the player's transform when possible.")]
    private bool autoAssignListener = true;
    [SerializeField, Tooltip("Listener whose distance controls volume. Defaults to the player if left empty.")]
    private Transform listener;
    [SerializeField, Tooltip("Distance (m) beyond which the loop is fully muted.")]
    private float audibleRadius = 12f;
    [SerializeField, Tooltip("Distance (m) at which the loop reaches full volume.")]
    private float fullVolumeRadius = 3f;
    [SerializeField, Tooltip("Curves the fade-in once inside the audible radius (0 = at max distance, 1 = at full volume distance).")]
    private AnimationCurve distanceCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private Mob mob;
    private Vector3 previousPosition;
    private float smoothedSpeed;
    private float requestedVolume;
    private float priorityScore;
    private float lastListenerDistance = float.PositiveInfinity;

    internal float RequestedVolume => requestedVolume;
    internal float PriorityScore => priorityScore;
    internal bool HasClip => audioSource != null && audioSource.clip != null;
    internal float ListenerDistance => lastListenerDistance;

    private void Awake()
    {
        mob = GetComponent<Mob>();

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        ConfigureAudioSource();
        previousPosition = transform.position;
    }

    private void OnEnable()
    {
        previousPosition = transform.position;
        smoothedSpeed = 0f;
        requestedVolume = 0f;
        priorityScore = 0f;
        lastListenerDistance = float.PositiveInfinity;

        MobMovementAudioManager.Instance.Register(this);
    }

    private void OnDisable()
    {
        if (MonoSingleton<MobMovementAudioManager>.TryGetInstance(out var manager))
        {
            manager.Unregister(this);
        }

        if (audioSource != null)
        {
            audioSource.Stop();
            audioSource.volume = 0f;
        }
    }

    private void Start()
    {
        if (listener == null && autoAssignListener)
        {
            TryResolveListener();
        }
    }

    private void Update()
    {
        if (audioSource == null)
        {
            return;
        }

        float deltaTime = Time.deltaTime;
        if (deltaTime <= Mathf.Epsilon)
        {
            return;
        }

        Vector3 displacement = transform.position - previousPosition;
        previousPosition = transform.position;
        displacement.y = 0f;

        float instantaneousSpeed = displacement.magnitude / deltaTime;
        smoothedSpeed = Mathf.Lerp(smoothedSpeed, instantaneousSpeed, 1f - Mathf.Exp(-speedSmoothing * deltaTime));

        float movementFactor = Mathf.InverseLerp(speedThreshold, fullSpeed, smoothedSpeed);
        float distanceFactor = CalculateDistanceFactor(out lastListenerDistance);

        requestedVolume = maxVolume * movementFactor * distanceFactor;
        priorityScore = requestedVolume;

        if (!HasClip)
        {
            requestedVolume = 0f;
            priorityScore = 0f;
        }
        else if (audibleRadius > 0f && float.IsFinite(lastListenerDistance))
        {
            float proximity = 1f - Mathf.Clamp01(lastListenerDistance / audibleRadius);
            priorityScore += proximity * 0.05f;
        }
    }

    internal void ApplyMix(float allowedVolume)
    {
        if (audioSource == null)
        {
            return;
        }

        allowedVolume = Mathf.Max(0f, allowedVolume);
        float deltaTime = Time.deltaTime;
        float newVolume = Mathf.MoveTowards(audioSource.volume, allowedVolume, fadeSpeed * deltaTime);
        audioSource.volume = newVolume;

        if (!HasClip)
        {
            if (audioSource.isPlaying)
            {
                audioSource.Stop();
            }
            return;
        }

        if (newVolume > 0.001f && !audioSource.isPlaying)
        {
            audioSource.Play();
        }
        else if (newVolume <= 0.001f && audioSource.isPlaying && allowedVolume <= 0f)
        {
            audioSource.Stop();
        }
    }

    private void ConfigureAudioSource()
    {
        if (audioSource == null)
        {
            return;
        }

        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.volume = 0f;

        if (movementClip != null)
        {
            audioSource.clip = movementClip;
        }
    }

    private float CalculateDistanceFactor(out float distance)
    {
        if (listener == null)
        {
            distance = 0f;
            return 1f;
        }

        distance = Vector3.Distance(transform.position, listener.position);

        float maxDistance = Mathf.Max(audibleRadius, fullVolumeRadius);
        if (maxDistance <= 0f)
        {
            return 1f;
        }

        if (distance >= maxDistance)
        {
            return 0f;
        }

        float minDistance = Mathf.Clamp(fullVolumeRadius, 0f, maxDistance);
        float t;
        if (minDistance <= 0f)
        {
            t = Mathf.InverseLerp(maxDistance, 0f, distance);
        }
        else
        {
            t = Mathf.InverseLerp(maxDistance, minDistance, distance);
        }

        return Mathf.Clamp01(distanceCurve.Evaluate(Mathf.Clamp01(t)));
    }

    private void TryResolveListener()
    {
        if (mob != null && mob.PlayerTransform != null)
        {
            listener = mob.PlayerTransform;
            return;
        }

        PlayerController playerController = FindFirstObjectByType<PlayerController>();
        if (playerController != null)
        {
            listener = playerController.transform;
        }
    }

    private void OnValidate()
    {
        maxVolume = Mathf.Clamp01(maxVolume);
        fadeSpeed = Mathf.Max(0f, fadeSpeed);
        speedThreshold = Mathf.Max(0f, speedThreshold);
        fullSpeed = Mathf.Max(speedThreshold + 0.01f, fullSpeed);
        speedSmoothing = Mathf.Max(0f, speedSmoothing);
        audibleRadius = Mathf.Max(0f, audibleRadius);
        fullVolumeRadius = Mathf.Clamp(fullVolumeRadius, 0f, audibleRadius);
    }
}
