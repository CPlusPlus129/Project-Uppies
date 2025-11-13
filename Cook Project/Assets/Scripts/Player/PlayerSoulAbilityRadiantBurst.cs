using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class PlayerSoulAbilityRadiantBurst : MonoBehaviour
{
    [Header("Light Pulse")]
    [SerializeField, Min(0f)] private float burstDuration = 1.25f;
    [SerializeField, Min(0f)] private float burstRange = 10f;
    [SerializeField, Min(0f)] private float burstIntensity = 15f;
    [SerializeField] private Color burstColor = Color.white;

    [Header("Offensive Field")]
    [SerializeField, Min(0f)] private float effectRadius = 6f;
    [SerializeField, Min(0f)] private float pushForce = 14f;
    [SerializeField, Min(0f)] private float damageAmount = 22f;
    [SerializeField, Tooltip("Layers to consider when pushing/damaging mobs.")] private LayerMask targetLayerMask = Physics.AllLayers;

    [Header("Feedback")]
    [SerializeField] private ParticleSystem burstParticles;

    private Light activeLight;
    private Coroutine burstRoutine;

    public void TriggerBurst()
    {
        if (burstRoutine != null)
        {
            StopCoroutine(burstRoutine);
            CleanupLight();
        }

        burstRoutine = StartCoroutine(BurstCoroutine());
    }

    private IEnumerator BurstCoroutine()
    {
        activeLight = CreateBurstLight();
        PushAndDamageEnemies();

        if (burstParticles != null)
        {
            burstParticles.Play(true);
        }

        float elapsed = 0f;
        while (elapsed < burstDuration)
        {
            elapsed += Time.deltaTime;

            if (activeLight != null)
            {
                float normalized = Mathf.Clamp01(elapsed / burstDuration);
                activeLight.intensity = Mathf.Lerp(burstIntensity, 0f, normalized);
            }

            yield return null;
        }

        CleanupLight();

        if (burstParticles != null)
        {
            burstParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            burstParticles.Clear();
        }

        burstRoutine = null;
    }

    private Light CreateBurstLight()
    {
        GameObject lightHost = new GameObject("RadiantBurstLight");
        lightHost.transform.SetParent(transform);
        lightHost.transform.localPosition = Vector3.zero;
        lightHost.transform.localRotation = Quaternion.identity;

        Light light = lightHost.AddComponent<Light>();
        light.color = burstColor;
        light.range = burstRange;
        light.intensity = burstIntensity;
        light.type = LightType.Point;
        light.shadows = LightShadows.None;
        light.renderMode = LightRenderMode.ForcePixel;
        light.useColorTemperature = false;
        light.enabled = true;

        return light;
    }

    private void CleanupLight()
    {
        if (activeLight != null)
        {
            Destroy(activeLight.gameObject);
            activeLight = null;
        }
    }

    private void PushAndDamageEnemies()
    {
        if (effectRadius <= 0f)
        {
            return;
        }

        Collider[] hits = Physics.OverlapSphere(transform.position, effectRadius, targetLayerMask);
        if (hits == null || hits.Length == 0)
        {
            return;
        }

        var processedMobs = new HashSet<int>();

        foreach (Collider hit in hits)
        {
            if (hit == null)
            {
                continue;
            }

            Mob mob = hit.GetComponentInParent<Mob>();
            if (mob == null || !mob.IsAlive)
            {
                continue;
            }

            if (!processedMobs.Add(mob.GetInstanceID()))
            {
                continue;
            }

            Vector3 direction = mob.transform.position - transform.position;
            float distance = direction.magnitude;
            if (distance <= 0.001f)
            {
                direction = Vector3.up;
            }
            else
            {
                direction /= distance;
            }

            float distanceFactor = 1f - Mathf.Clamp01(distance / effectRadius);
            float appliedForce = Mathf.Max(0.5f, distanceFactor) * pushForce;

            Rigidbody mobBody = mob.GetComponent<Rigidbody>();
            if (mobBody != null)
            {
                mobBody.AddForce(direction * appliedForce, ForceMode.Impulse);
            }

            int damage = Mathf.Max(1, Mathf.RoundToInt(damageAmount));
            mob.TakeDamage(damage);
        }
    }
}
