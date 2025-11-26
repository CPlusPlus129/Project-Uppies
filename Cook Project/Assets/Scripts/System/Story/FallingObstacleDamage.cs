using UnityEngine;

/// <summary>
/// Applies damage to the player upon collision.
/// </summary>
public class FallingObstacleDamage : MonoBehaviour
{
    [Tooltip("Amount of damage to deal to the player.")]
    public int damageAmount = 10;

    [Tooltip("Whether to destroy this object after hitting the player.")]
    public bool destroyOnHit = false;

    private bool hasDealtDamage = false;

    private void OnCollisionEnter(Collision collision)
    {
        HandleCollision(collision.gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        HandleCollision(other.gameObject);
    }

    private void HandleCollision(GameObject hitObject)
    {
        if (hasDealtDamage) return;

        if (hitObject.CompareTag("Player"))
        {
            PlayerStatSystem.Instance.Damage(damageAmount);
            Debug.Log($"[FallingObstacleDamage] Dealt {damageAmount} damage to player.", this);
            hasDealtDamage = true;

            if (destroyOnHit)
            {
                Destroy(gameObject);
            }
        }
    }
}
