using UnityEngine;
using UnityEngine.AI;

public partial class Mob
{

    #region Gizmos

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos)
        {
            return;
        }

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attack.range);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, perception.detectionRange);

        if (agent != null && agent.hasPath)
        {
            Gizmos.color = Color.cyan;
            Vector3[] corners = agent.path.corners;
            for (int i = 0; i < corners.Length - 1; i++)
            {
                Gizmos.DrawLine(corners[i], corners[i + 1]);
            }
        }
    }

    #endregion
}
