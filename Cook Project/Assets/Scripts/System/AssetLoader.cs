using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.ResourceLocations;

public class AssetLoader : IAssetLoader
{
    //Addressables.ResourceLocators 手動排序 不然無法實現同路徑新bundle取代舊bundle
    List<IResourceLocator> resLocators = new List<IResourceLocator>();

    List<IResourceLocator> remoteLocators = new List<IResourceLocator>(); //所有手動Add進去的部分
    Dictionary<string, IResourceLocator> remoteLocatorsByPath = new Dictionary<string, IResourceLocator>();
    bool inited = false;

    public AssetLoader()
    {
    }

    public async UniTask Init()
    {
        //這東西會回傳null 靠杯喔1
        await Addressables.InitializeAsync().Task;
        UpdateLocators();
        inited = true;
    }

    #region Load Addressable's Catalog
    public async UniTask LoadRemoteCatelog(string path)
    {
        //unload old one
        UnloadCatalog(path);

        //這東西會回傳null 靠杯喔2
        await Addressables.LoadContentCatalogAsync(path, true).Task;
        //會跳這個錯誤 try catch接不到
        //ArgumentNullException value cannot be null. Parameter name: _unity_self

        IResourceLocator newLocator = null;
        foreach (var x in Addressables.ResourceLocators)
        {
            if (!resLocators.Contains(x))
            {
                newLocator = x;
                break;
            }
        }

        remoteLocators.Add(newLocator);
        remoteLocatorsByPath.Add(path, newLocator);
        UpdateLocators();
    }

    public void UnloadCatalog(string path)
    {
        if (remoteLocatorsByPath.ContainsKey(path))
        {
            Debug.Log($"[AssetLoader] UnloadCatalog: {path}");
            var target = remoteLocatorsByPath[path];
            remoteLocatorsByPath.Remove(path);
            remoteLocators.Remove(target);
            Addressables.RemoveResourceLocator(target);
        }
    }

    public void RemoveAllRemoteCatelogAndBundles()
    {
        foreach (var target in remoteLocators)
        {
            Addressables.RemoveResourceLocator(target);
        }

        remoteLocators.Clear();
        remoteLocatorsByPath.Clear();
    }

    private void UpdateLocators()
    {
        resLocators.Clear();

        foreach (var x in remoteLocators)
        {
            resLocators.Insert(0, x); //越後面的越優先
        }

        foreach (var x in Addressables.ResourceLocators)
        {
            if (!resLocators.Contains(x))
                resLocators.Add(x);
        }

        Addressables.ClearResourceLocators();
        foreach (var x in resLocators)
            Addressables.AddResourceLocator(x);
    }
    #endregion

    #region Load Function
    public async Task<GameObject> InstaniateAsync(string assetPath, Transform parent)
    {
        return await InstaniateAsync(assetPath, Vector3.zero, Quaternion.identity, parent);
    }

    public async Task<GameObject> InstaniateAsync(string assetPath, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        while (!inited)
            await UniTask.NextFrame();

        var loc = GetLocation(assetPath, typeof(GameObject));
        if (loc == null)
        {
            Debug.LogError("Path Not Found:" + assetPath);
            return null;
        }

        /*
         * Preload asset to cache. 
         * This step is necessary because InstantiateAsync will finish a frame late, 
         * which will cause the game draw a frame of the game object without Awake() called.
        */
        await Addressables.LoadAssetAsync<GameObject>(loc);

        var result = await Addressables.InstantiateAsync(loc, position, rotation, parent).Task;
        return result;
    }

    public async Task<T> LoadAsync<T>(string assetPath) where T : UnityEngine.Object
    {
        while (!inited)
            await UniTask.NextFrame();

        var loc = GetLocation(assetPath, typeof(T));
        if (loc == null)
        {
            Debug.LogError($"asset location not found: {assetPath}");
            return null;
        }

        var result = await Addressables.LoadAssetAsync<T>(loc).Task;
        if (result == null)
        {
            Debug.LogError($"asset not found: {assetPath}");
            return null;
        }

        return result;
    }

    public UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<T> LoadAsyncByHandle<T>(string assetPath) where T : UnityEngine.Object
    {
        var loc = GetLocation(assetPath, typeof(T));
        if (loc == null)
        {
            Debug.LogError($"asset not found: {assetPath}");
            return default;
        }

        return Addressables.LoadAssetAsync<T>(loc);
    }

    #endregion

    #region GetResourcesLocation
    public IResourceLocation GetLocation(string assetPath, Type type)
    {
        bool isFullPath = assetPath.StartsWith("Assets/");
        string localPath = null;
        string globalPath = null;

        if (!isFullPath)
        {
            localPath = assetPath.Replace(":/", $"/En/");
            globalPath = assetPath.Replace(":/", "/Global/");
        }
        else
        {
            localPath = assetPath;
        }

        if (!Locate(localPath, type, out IList<IResourceLocation> locs) && globalPath != null)
        {
            Locate(globalPath, type, out locs);
        }

        return (locs is { Count: > 0 }) ? locs[0] : null;
    }

    public List<IResourceLocation> GetLocations(string label, Type type)
    {
        List<IResourceLocation> result = new List<IResourceLocation>();
        List<string> pathId = new List<string>();

        if (Locate(label, type, out var locs))
        {
            foreach (var res in locs)
            {
                if (!pathId.Contains(res.PrimaryKey))
                {
                    pathId.Add(res.PrimaryKey);
                    result.Add(res);
                }
            }

        }

        return result;
    }

    private bool Locate(string path, Type type, out IList<IResourceLocation> locs)
    {
        locs = null;
        if (_Locate(path, type, out locs))
            return true;

        if (path.StartsWith("Res:/") || path.StartsWith("Res/"))
        {
            //從Res3找回Res1
            string basePath = path.Remove(0, 3);
            for (int i = 3; i >= 1; i--)
            {
                string truePath = basePath.Insert(0, $"Res{i}");
                if (_Locate(truePath, type, out locs))
                {
                    return true;
                }
            }
            return false;
        }

        return false;
    }

    private bool _Locate(string path, Type type, out IList<IResourceLocation> locs)
    {
        locs = null;
        foreach (var locator in resLocators)
        {
            if (locator.Locate(path, type, out locs))
            {
                return true;
            }
        }
        return false;
    }
    #endregion

    public void Release(UnityEngine.Object asset)
    {
        Addressables.Release(asset);
    }

    public void ReleaseInstance(GameObject gameObject)
    {
        Addressables.ReleaseInstance(gameObject);
    }
}