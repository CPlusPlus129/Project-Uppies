using UnityEngine;

public class FirePillarTrapController : MonoBehaviour
{
    [Header("Trap References")]
    public GameObject flameEffect;       
    public Collider trapCollider;         

    [Header("Trap Settings")]
    public float fireDuration = 3f;       
    public float cooldownDuration = 2f;   
    public int damage = 30;               
    public float damageInterval = 0.5f;  

    [Header("Auto Start")]
    public bool autoStart = true;         

    private bool trapEnabled = true;      
    private float damageTimer = 0.5f;

    void Start()
    {
        DisableFire();

        if (autoStart)
            StartCoroutine(FireLoop());
    }


    private System.Collections.IEnumerator FireLoop()
    {
        while (trapEnabled)
        {
            EnableFire();
            yield return new WaitForSeconds(fireDuration);

            DisableFire();
            yield return new WaitForSeconds(cooldownDuration);
        }
    }

    private void EnableFire()
    {
        if (flameEffect != null) flameEffect.SetActive(true);
        if (trapCollider != null) trapCollider.enabled = true;
    }

    private void DisableFire()
    {
        if (flameEffect != null) flameEffect.SetActive(false);
        if (trapCollider != null) trapCollider.enabled = false;
    }

    private void OnTriggerStay(Collider other)
    {
        if (!trapCollider.enabled) return;

        if (other.CompareTag("Player"))
        {
            damageTimer += Time.deltaTime;
            if (damageTimer >= damageInterval)
            {
                damageTimer = 0f;
                Debug.Log("Trap hit player!");
                var playerStat = PlayerStatSystem.Instance;
                if (playerStat == null)
                {
                    Debug.LogError("PlayerLightDamage: PlayerStatSystem.Instance is NULL! Make sure PlayerStatSystem exists in the scene.", this);
                    enabled = false;
                    return;
                }
                playerStat.Damage(damage);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            damageTimer = 0f;
    }

    // Disable the trap
    public void StopTrap()
    {
        trapEnabled = false;
        StopAllCoroutines();
        DisableFire();
    }

    // Enable the trap
    public void StartTrap()
    {
        if (trapEnabled) return;

        trapEnabled = true;
        StartCoroutine(FireLoop());
    }
}
