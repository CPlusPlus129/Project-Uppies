using System.Collections.Generic;
using UnityEngine;

public partial class Mob
{
    private static readonly Dictionary<int, TimedObjectDestroyer> s_destroyerCache = new Dictionary<int, TimedObjectDestroyer>();

    #region Combat & Damage

    private void TryAttackPlayer(float distanceToPlayer, float deltaTime)
    {
        if (Time.time - lastAttackTime < attack.cooldown)
        {
            return;
        }

        if (distanceToPlayer > attack.range)
        {
            return;
        }

        lastAttackTime = Time.time;

        PlayerStatSystem playerStats = PlayerStatSystem.Instance;
        if (playerStats != null)
        {
            int currentHP = playerStats.CurrentHP.Value;
            int damage = Mathf.RoundToInt(attack.damage);
            int newHP = Mathf.Max(0, currentHP - damage);
            playerStats.Damage(damage);

            if (showDebug)
            {
                Debug.Log($"Mob attacked player for {damage} damage. Player HP: {newHP}/{playerStats.MaxHP.Value}", this);
            }
        }

        DamageNearestLightSource();
    }

    public void TakeDamage(int amount)
    {
        if (!isAlive)
        {
            return;
        }

        currentHealth = Mathf.Max(0, currentHealth - amount);

        if (healthBarController != null)
        {
            healthBarController.FlashDamage();
        }

        if (showDebug)
        {
            Debug.Log($"Mob took {amount} damage. HP: {currentHealth}/{health.maxHealth}", this);
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (!isAlive)
        {
            return;
        }

        isAlive = false;
        ClearPath();
        if (!body.isKinematic)
        {
            body.linearVelocity = Vector3.zero;
        }
        body.isKinematic = true;
        agent.enabled = false;

        SpawnDeathParticles();
        AwardSouls();
        Died?.Invoke(this);
        MobDied?.Invoke(this);
        enabled = false;

        Destroy(gameObject, death.despawnDelay);
    }

    private void SpawnDeathParticles()
    {
        if (death.deathParticles == null)
        {
            return;
        }

        Vector3 position = transform.position + death.particleOffset;
        GameObject particle = Instantiate(death.deathParticles, position, Quaternion.identity);

        if (death.autoDestroyParticles)
        {
            ParticleSystem ps = particle.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                Destroy(particle, ps.main.duration + ps.main.startLifetimeMultiplier);
            }
            else
            {
                Destroy(particle, 5f);
            }
        }
    }

    private void AwardSouls()
    {
        if (death.soulReward <= 0)
        {
            return;
        }

        PlayerStatSystem playerStats = PlayerStatSystem.Instance;
        if (playerStats == null)
        {
            return;
        }

        playerStats.AddSouls(death.soulReward);

        if (showDebug)
        {
            Debug.Log($"Mob awarded {death.soulReward} souls. Player Souls: {playerStats.CurrentSouls.Value}/{playerStats.MaxSouls.Value}", this);
        }
    }

    public void DamageNearestLightSource()
    {
        IReadOnlyList<Light> allLights = MobLightUtility.GetLights();
        if (allLights.Count == 0)
        {
            return;
        }

        TimedObjectDestroyer closest = null;
        Light closestLight = null;
        float closestSqrDistance = float.MaxValue;
        float maxDistance = Mathf.Max(0.1f, lightDamage.searchRadius);
        float maxDistanceSqr = maxDistance * maxDistance;

        for (int i = 0; i < allLights.Count; i++)
        {
            Light light = allLights[i];
            if (light == null || !light.enabled)
            {
                continue;
            }

            Vector3 offset = light.transform.position - transform.position;
            float sqrDistance = offset.sqrMagnitude;
            if (sqrDistance > maxDistanceSqr || sqrDistance >= closestSqrDistance)
            {
                continue;
            }

            if (!TryGetTimedDestroyer(light, out TimedObjectDestroyer destroyer))
            {
                continue;
            }
            closest = destroyer;
            closestLight = light;
            closestSqrDistance = sqrDistance;
        }

        if (closest == null)
        {
            return;
        }

        closest.lifeTime = Mathf.Max(0.1f, closest.lifeTime - lightDamage.attackLifetimeReduction);

        if (closest.lifeTime < 1f && closestLight != null)
        {
            StartCoroutine(FlickerLight(closestLight));
        }
    }

    private static bool TryGetTimedDestroyer(Light light, out TimedObjectDestroyer destroyer)
    {
        destroyer = null;
        if (light == null)
        {
            return false;
        }

        int instanceId = light.GetInstanceID();
        if (s_destroyerCache.TryGetValue(instanceId, out destroyer) && destroyer != null)
        {
            return true;
        }

        // Search for the component (expensive part)
        if (!light.TryGetComponent(out destroyer))
        {
            destroyer = light.GetComponentInParent<TimedObjectDestroyer>();
        }

        if (destroyer != null)
        {
            s_destroyerCache[instanceId] = destroyer;
            return true;
        }

        return false;
    }

    private System.Collections.IEnumerator FlickerLight(Light light)
    {
        if (light == null)
        {
            yield break;
        }

        float originalIntensity = light.intensity;
        float duration = 0.35f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (light == null)
            {
                yield break;
            }

            light.intensity = originalIntensity * UnityEngine.Random.Range(0.5f, 1f);
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (light != null)
        {
            light.intensity = originalIntensity;
        }
    }

    #endregion
}
