using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("Story/VIP Customer Spawn Anchor")]
public sealed class VipCustomerSpawnAnchor : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Unique identifier used by VipCustomerStoryEventAsset to target this anchor (ex: Customer1).")]
    private string anchorId = "Customer1";

    [SerializeField]
    [Tooltip("Optional transform that overrides where the VIP should spawn.")]
    private Transform spawnPoint;

    [SerializeField]
    [Tooltip("Optional transform the VIP should face after spawning.")]
    private Transform lookAtTarget;

    [SerializeField]
    [Tooltip("If true, the character will be rotated to face the Look At Target when available.")]
    private bool faceLookAtTarget = true;

    public string AnchorId => string.IsNullOrWhiteSpace(anchorId) ? name : anchorId;
    public Transform SpawnPoint => spawnPoint != null ? spawnPoint : transform;
    public Transform LookAtTarget => lookAtTarget;

    private void Reset()
    {
        if (string.IsNullOrWhiteSpace(anchorId))
        {
            anchorId = name;
        }
    }

    public void SnapTransform(Transform target)
    {
        if (target == null)
        {
            return;
        }

        var pivot = SpawnPoint;
        target.SetPositionAndRotation(pivot.position, pivot.rotation);

        if (!faceLookAtTarget || lookAtTarget == null)
        {
            return;
        }

        var lookPos = lookAtTarget.position;
        var dir = lookPos - target.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.0001f)
        {
            target.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        var pivot = SpawnPoint;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(pivot.position, 0.15f);

        if (lookAtTarget != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(pivot.position, lookAtTarget.position);
        }
    }
#endif
}
