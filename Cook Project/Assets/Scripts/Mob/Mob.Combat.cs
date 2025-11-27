using System.Collections.Generic;
using UnityEngine;

public partial class Mob
{
    private static readonly Dictionary<int, TimedObjectDestroyer> s_destroyerCache = new Dictionary<int, TimedObjectDestroyer>();
    private static readonly Dictionary<int, LightProjectile> s_projectileCache = new Dictionary<int, LightProjectile>();

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

        if (hasRegistered)
        {
            ActiveMobs.Remove(this);
            hasRegistered = false;
        }

        SpawnDeathParticles();
        AwardSouls();
        AwardMoney();
        Died?.Invoke(this);
        MobDied?.Invoke(this);

        Destroy(gameObject, death.despawnDelay);

        TriggerDeathPresentation();
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

    private void AwardMoney()
    {
        if (moneyReward == null || !moneyReward.enabled)
        {
            return;
        }

        int minReward = Mathf.Max(0, Mathf.Min(moneyReward.minMoney, moneyReward.maxMoney));
        int maxReward = Mathf.Max(minReward, Mathf.Max(moneyReward.minMoney, moneyReward.maxMoney));

        if (maxReward <= 0)
        {
            return;
        }

        int payout = UnityEngine.Random.Range(minReward, maxReward + 1);
        if (payout <= 0)
        {
            return;
        }

        PlayerStatSystem playerStats = PlayerStatSystem.Instance;
        if (playerStats == null)
        {
            return;
        }

        playerStats.Money.Value += payout;
        SpawnMoneyPopup(payout);

        if (showDebug)
        {
            Debug.Log($"Mob awarded ${payout}. Player money: {playerStats.Money.Value}", this);
        }
    }

    private void SpawnMoneyPopup(int payout)
    {
        if (moneyReward == null || !moneyReward.enabled)
        {
            return;
        }

        Vector3 offset = moneyReward.popupOffset;
        float horizontal = moneyReward.popupHorizontalJitter;
        float depth = moneyReward.popupDepthJitter;
        float vertical = moneyReward.popupVerticalJitter;
        Vector3 jitter = new Vector3(
            UnityEngine.Random.Range(-horizontal, horizontal),
            UnityEngine.Random.Range(0f, vertical),
            UnityEngine.Random.Range(-depth, depth));
        Vector3 spawnPos = transform.position + offset + jitter;

        MoneyPopupEffect.PopupStyle style = MoneyPopupEffect.PopupStyle.FromSettings(moneyReward);
        MoneyPopupEffect.Spawn(payout, spawnPos, style);
    }

    public void DamageNearestLightSource()
    {
        IReadOnlyList<Light> allLights = MobLightUtility.GetLights();
        if (allLights.Count == 0)
        {
            return;
        }

        TimedObjectDestroyer closestDestroyer = null;
        LightProjectile closestProjectile = null;
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

            // Try to find LightProjectile first (new system)
            if (TryGetLightProjectile(light, out LightProjectile projectile))
            {
                closestProjectile = projectile;
                closestLight = light;
                closestSqrDistance = sqrDistance;
            }
            // Fallback to TimedObjectDestroyer (old system)
            else if (TryGetTimedDestroyer(light, out TimedObjectDestroyer destroyer))
            {
                closestDestroyer = destroyer;
                closestLight = light;
                closestSqrDistance = sqrDistance;
            }
        }

        // Handle LightProjectile (new system)
        if (closestProjectile != null)
        {
            closestProjectile.ReduceLifetime(lightDamage.attackLifetimeReduction);

            if (closestProjectile.GetRemainingLifetime() < 1f && closestLight != null)
            {
                StartCoroutine(FlickerLight(closestLight));
            }
        }
        // Handle TimedObjectDestroyer (old system)
        else if (closestDestroyer != null)
        {
            closestDestroyer.lifeTime = Mathf.Max(0.1f, closestDestroyer.lifeTime - lightDamage.attackLifetimeReduction);

            if (closestDestroyer.lifeTime < 1f && closestLight != null)
            {
                StartCoroutine(FlickerLight(closestLight));
            }
        }
    }

    private static bool TryGetLightProjectile(Light light, out LightProjectile projectile)
    {
        projectile = null;
        if (light == null)
        {
            return false;
        }

        int instanceId = light.GetInstanceID();
        if (s_projectileCache.TryGetValue(instanceId, out projectile) && projectile != null)
        {
            return true;
        }

        // Search for LightProjectile component (expensive part)
        if (!light.TryGetComponent(out projectile))
        {
            projectile = light.GetComponentInParent<LightProjectile>();
        }

        if (projectile != null)
        {
            s_projectileCache[instanceId] = projectile;
            return true;
        }

        return false;
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

        // Search for component (expensive part)
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
