using UnityEngine;

public class SpawnPointList : MonoBehaviour
{
    [HideInInspector] public SpawnPoint[] spawnPoints;

    private void Awake()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            spawnPoints = GetComponentsInChildren<SpawnPoint>();
        }
    }
    public SpawnPoint GetSpawnPointById(int id)
    {
        foreach (var spawnPoint in spawnPoints)
        {
            if (spawnPoint.id == id)
                return spawnPoint;
        }
        return null;
    }
}