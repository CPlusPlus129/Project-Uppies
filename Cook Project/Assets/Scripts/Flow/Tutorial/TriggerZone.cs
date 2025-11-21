using R3;
using UnityEngine;

/// <summary>
/// A simple trigger zone that fires an event when the player enters it.
/// Attach this to a GameObject with a BoxCollider set to IsTrigger.
/// </summary>
public class TriggerZone : MonoBehaviour
{
    [SerializeField] private bool triggerOnce = true;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private GameObject respawnPoint;

    private Subject<Unit> onPlayerEnter = new Subject<Unit>();
    private bool hasTriggered = false;

    /// <summary>
    /// Observable that fires when the player enters the trigger zone.
    /// </summary>
    public Observable<Unit> OnPlayerEnter => onPlayerEnter;

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[TriggerZone] OnTriggerEnter called. Tag: {other.tag}, HasTriggered: {hasTriggered}");
        
        if (triggerOnce && hasTriggered)
        {
            Debug.Log("[TriggerZone] Already triggered, ignoring.");
            return;
        }

        if (other.CompareTag(playerTag))
        {
            Debug.Log("[TriggerZone] Player entered! Firing event.");
            hasTriggered = true;
            var playerStat = PlayerStatSystem.Instance;
            if (playerStat != null && respawnPoint != null) {
                playerStat.SetRespawnPosition(respawnPoint.transform.position);
            }
            onPlayerEnter.OnNext(Unit.Default);
        }
        else
        {
            Debug.Log($"[TriggerZone] Tag mismatch. Expected '{playerTag}', got '{other.tag}'");
        }
    }

    private void OnDestroy()
    {
        onPlayerEnter.Dispose();
    }
}
