using UnityEngine;

public partial class Mob
{
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
        Light[] allLights = FindObjectsByType<Light>(FindObjectsSortMode.None);
        if (allLights == null || allLights.Length == 0)
        {
            return;
        }

        TimedObjectDestroyer closest = null;
        Light closestLight = null;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < allLights.Length; i++)
        {
            Light light = allLights[i];
            if (!light.enabled)
            {
                continue;
            }

            float distance = Vector3.Distance(transform.position, light.transform.position);
            if (distance > lightDamage.searchRadius || distance >= closestDistance)
            {
                continue;
            }

            TimedObjectDestroyer destroyer = light.GetComponent<TimedObjectDestroyer>();
            if (destroyer == null)
            {
                destroyer = light.GetComponentInParent<TimedObjectDestroyer>();
            }

            if (destroyer == null)
            {
                continue;
            }

            closest = destroyer;
            closestLight = light;
            closestDistance = distance;
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
