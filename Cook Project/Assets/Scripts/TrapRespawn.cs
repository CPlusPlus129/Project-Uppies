using UnityEngine;

public class TrapRespawn : MonoBehaviour
{

    [SerializeField] private Transform respawnPoint;
    [SerializeField] private float upOffset = 0.25f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) {
            Debug.Log("not player object");
            return;
        }
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
    }
}
