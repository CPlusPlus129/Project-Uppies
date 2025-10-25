using UnityEngine;

[RequireComponent(typeof(Light))]
public class HeartbeatLight : MonoBehaviour
{
    [Header("Light Settings")]
    [SerializeField] private Light targetLight;
    [SerializeField] private float baseIntensity = 0.5f;
    [SerializeField] private float pulseIntensity = 1.5f;
    [SerializeField] private float heartbeatSpeed = 1.0f;

    [Header("Wave Control")]
    [Tooltip("Heartbeat curve, brightness change within a cycle¡]0~1¡^")]
    [SerializeField]
    private AnimationCurve heartbeatCurve =
        new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.1f, 1f),
            new Keyframe(0.2f, 0.4f),
            new Keyframe(0.3f, 1f),
            new Keyframe(0.6f, 0f),
            new Keyframe(1f, 0f)
        );

    private float timer;

    private void Reset()
    {
        targetLight = GetComponent<Light>();
    }

    private void Update()
    {
        timer += Time.deltaTime * heartbeatSpeed;
        if (timer > 1f) timer -= 1f; // loop

        float curveValue = heartbeatCurve.Evaluate(timer);
        targetLight.intensity = Mathf.Lerp(baseIntensity, pulseIntensity, curveValue);
    }
}
