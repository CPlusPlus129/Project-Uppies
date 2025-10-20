using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

public interface IAssetLoader : IGameService
{
    Task<T> LoadAsync<T>(string assetPath) where T : UnityEngine.Object;
    AsyncOperationHandle<T> LoadAsyncByHandle<T>(string assetPath) where T : UnityEngine.Object;
    Task<GameObject> InstaniateAsync(string assetPath, Transform parent);
    Task<GameObject> InstaniateAsync(string assetPath, Vector3 position, Quaternion rotation, Transform parent = null);
    void Release(UnityEngine.Object asset);
    void ReleaseInstance(GameObject gameObject);
}