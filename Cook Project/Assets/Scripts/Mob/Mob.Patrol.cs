using UnityEngine;
using UnityEngine.AI;

public partial class Mob
{

    #region Patrol

    private bool TryPickPatrolDestination(out Vector3 destination)
    {
        for (int attempt = 0; attempt < 6; attempt++)
        {
            Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * patrol.radius;
            Vector3 candidate = spawnPosition + new Vector3(randomCircle.x, 0f, randomCircle.y);

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, locomotion.destinationSampleRadius, navMeshAreaMask))
            {
                destination = hit.position;
                return true;
            }
        }

        destination = transform.position;
        return false;
    }

    #endregion
}
