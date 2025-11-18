using UnityEngine;

public class TrapRespawn : MonoBehaviour
{

    [SerializeField] private Transform respawnPoint;
    //[SerializeField] private float upOffset = 0.25f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) {
            Debug.Log("not player object");
            return;
        }

        /*
        Debug.Log("is player object");
        Transform t = other.transform;
        Vector3 targetPos = respawnPoint.position;

        var cc = other.GetComponent<CharacterController>();
        if (cc)
        {
            cc.enabled = false;
            t.SetPositionAndRotation(targetPos, t.rotation);
            cc.enabled = true;
        }
        else
        {
            t.SetPositionAndRotation(targetPos, t.rotation);
        }
        */

        Debug.Log("is player object");
        var healthSystem = PlayerStatSystem.Instance;
        if (healthSystem == null)
        {
            Debug.LogError("PlayerLightDamage: PlayerStatSystem.Instance is NULL! Make sure PlayerStatSystem exists in the scene.", this);
            enabled = false;
            return;
        }
        int currentHP = healthSystem.CurrentHP.CurrentValue;
        healthSystem.Damage(currentHP);
    }
}
