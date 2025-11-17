using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Centralized mixer that limits the number of active mob movement loops and balances their volume.
/// Ensures only the highest priority emitters stay audible to prevent clutter when large groups spawn.
/// </summary>
public class MobMovementAudioManager : MonoBehaviour
{
    private static MobMovementAudioManager instance;

    [Header("Mix Limits")]
    [SerializeField, Range(1, 32)] private int maxSimultaneousVoices = 6;
    [SerializeField, Range(0f, 1.5f)] private float globalMixVolume = 0.9f;
    [SerializeField, Tooltip("Applied to ranked voices (0 = highest priority, 1 = lowest) to tame stacked loops.")]
    private AnimationCurve stackingCurve = new AnimationCurve(
        new Keyframe(0f, 1f),
        new Keyframe(1f, 0.65f));

    private readonly List<MobMovementAudio> emitters = new List<MobMovementAudio>(64);
    private readonly List<MobMovementAudio> candidates = new List<MobMovementAudio>(64);

    public static MobMovementAudioManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<MobMovementAudioManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("MobMovementAudioManager");
                    instance = go.AddComponent<MobMovementAudioManager>();
                    DontDestroyOnLoad(go);
                }
            }

            return instance;
        }
    }

    public static bool TryGetInstance(out MobMovementAudioManager manager)
    {
        manager = instance;
        return manager != null;
    }

    private void Awake()
    {
        if (transform.parent != null)
        {
            Debug.LogWarning("[MobMovementAudioManager] Instance parented under '" + transform.parent.name + "'. Reparenting to root so DontDestroyOnLoad can succeed.");
            transform.SetParent(null, true);
        }

        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        if (stackingCurve == null || stackingCurve.length == 0)
        {
            stackingCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 0.65f));
        }
    }

    internal void Register(MobMovementAudio emitter)
    {
        if (emitter != null && !emitters.Contains(emitter))
        {
            emitters.Add(emitter);
        }
    }

    internal void Unregister(MobMovementAudio emitter)
    {
        emitters.Remove(emitter);
    }

    private void LateUpdate()
    {
        float deltaTime = Time.deltaTime;
        if (deltaTime <= Mathf.Epsilon)
        {
            return;
        }

        candidates.Clear();

        for (int i = emitters.Count - 1; i >= 0; i--)
        {
            MobMovementAudio emitter = emitters[i];
            if (emitter == null || !emitter.isActiveAndEnabled)
            {
                emitters.RemoveAt(i);
                continue;
            }

            if (!emitter.HasClip)
            {
                emitter.ApplyMix(0f);
                continue;
            }

            if (emitter.RequestedVolume > 0.0001f)
            {
                candidates.Add(emitter);
            }
            else
            {
                emitter.ApplyMix(0f);
            }
        }

        if (candidates.Count == 0)
        {
            return;
        }

        candidates.Sort((a, b) => b.PriorityScore.CompareTo(a.PriorityScore));

        int allowedCount = Mathf.Min(maxSimultaneousVoices, candidates.Count);
        float voiceNormalization = allowedCount > 0 ? 1f / Mathf.Sqrt(allowedCount) : 1f;

        for (int i = 0; i < candidates.Count; i++)
        {
            MobMovementAudio emitter = candidates[i];

            if (i < allowedCount)
            {
                float rankT = allowedCount > 1 ? (float)i / (allowedCount - 1) : 0f;
                float stackScale = Mathf.Clamp01(stackingCurve.Evaluate(rankT));
                float allowedVolume = emitter.RequestedVolume * stackScale * globalMixVolume * voiceNormalization;
                emitter.ApplyMix(allowedVolume);
            }
            else
            {
                emitter.ApplyMix(0f);
            }
        }
    }

    private void OnValidate()
    {
        maxSimultaneousVoices = Mathf.Clamp(maxSimultaneousVoices, 1, 32);
        globalMixVolume = Mathf.Clamp(globalMixVolume, 0f, 1.5f);
    }
}
