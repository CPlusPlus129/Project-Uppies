using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    public int id;
    [SerializeField] private MeshRenderer placeholderMesh;
    [SerializeField] private Color gizmosColor = Color.green;
    public GameObject spawnedObject { get; private set; }

    private void Awake()
    {
        if (placeholderMesh != null)
            placeholderMesh.enabled = false;
    }

    public T Spawn<T>(T prefab) where T : Component
    {
        T instance = Instantiate(prefab, transform);
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        spawnedObject = instance.gameObject;
        return instance;
    }

    public void Reset()
    {
        if (spawnedObject != null)
        {
            Destroy(spawnedObject);
            spawnedObject = null;
        }
    }

    private void OnDrawGizmos()
    {
        if (placeholderMesh != null)
        {
            Gizmos.color = gizmosColor;
            Gizmos.DrawWireCube(transform.position, transform.localScale);
            Gizmos.DrawLine(transform.position, transform.position + transform.forward * 2);
        }
    }
}