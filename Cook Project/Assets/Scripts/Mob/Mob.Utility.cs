using UnityEngine;

public partial class Mob
{

    #region Utility

    private static bool Approximately(Vector3 a, Vector3 b, float tolerance = 0.001f)
    {
        if (float.IsNaN(a.x) || float.IsNaN(a.y) || float.IsNaN(a.z))
        {
            return false;
        }

        if (float.IsNaN(b.x) || float.IsNaN(b.y) || float.IsNaN(b.z))
        {
            return false;
        }

        return (a - b).sqrMagnitude <= tolerance * tolerance;
    }

    #endregion
}
