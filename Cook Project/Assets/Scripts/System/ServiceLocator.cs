using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ServiceLocator : SimpleSingleton<ServiceLocator>
{
    private readonly Dictionary<Type, IGameService> services = new Dictionary<Type, IGameService>();
    private readonly Dictionary<Type, bool> initializationStatus = new Dictionary<Type, bool>();
    private bool isGlobalInitStarted = false;

    public async UniTask Init()
    {
        if (isGlobalInitStarted) return;
        isGlobalInitStarted = true;

        RegisterAllServices();

        // Initialize services in order
        var initTasks = new List<UniTask>();
        foreach (var kvp in services)
        {
            var serviceType = kvp.Key;
            var service = kvp.Value;
            initTasks.Add(InitializeService(serviceType, service));
        }

        await UniTask.WhenAll(initTasks);
    }

    private void RegisterAllServices()
    {
        //Service that needs other service dependencies please add it AFTER their dependencies are registered.
        Register<IAssetLoader>(() => new AssetLoader());
        Register<ITableManager>(() => new TableManager());
        Register<ISceneManagementService>(() => new SceneManagementService());
        Register<IQuestService>(() => new QuestManager(
            Get<ITableManager>()));
        Register<IShiftSystem>(() => new ShiftSystem(
            Get<IQuestService>()));
        Register<IPuzzleGameManager>(() => new PuzzleGameManager(
            Get<IQuestService>()));

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        var debugManagerGO = new UnityEngine.GameObject("DebugManager");
        var debugManager = debugManagerGO.AddComponent<DebugManager>();
        debugManagerGO.AddComponent<DebugUI>();
        Register<IDebugService>(() => debugManager);
#endif
    }

    private void Register<TInterface>(Func<TInterface> factory)
        where TInterface : class, IGameService
    {
        var serviceType = typeof(TInterface);
        var service = factory();
        services[serviceType] = service;
        initializationStatus[serviceType] = false;
    }

    private TInterface Get<TInterface>() where TInterface : IGameService
    {
        // This function is only used in register stage, not allowed for public use
        return (TInterface)services[typeof(TInterface)];
    }

    private async UniTask InitializeService(Type serviceType, IGameService service)
    {
        try
        {
            await service.Init();
            initializationStatus[serviceType] = true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to initialize {serviceType.Name}: {ex}");
            throw;
        }
    }

    /// <summary>
    /// WARNING: DO NOT USE THIS IN Init()!!!
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public async UniTask<T> GetAsync<T>() where T : IGameService
    {
        var serviceType = typeof(T);
        if (!services.TryGetValue(serviceType, out var service))
        {
            Debug.LogError($"Service {serviceType.Name} not registered");
            return default;
        }

        await UniTask.WaitUntil(() => initializationStatus.GetValueOrDefault(serviceType, false));

        return (T)service;
    }

    public bool IsServiceReady<T>() where T : class, IGameService
    {
        return initializationStatus.GetValueOrDefault(typeof(T), false);
    }

    public bool AreAllServicesReady()
    {
        foreach (var status in initializationStatus.Values)
        {
            if (!status) return false;
        }
        return true;
    }

    // get service synchronously (can only be used after services are initialized)
    public TInterface GetService<TInterface>() where TInterface : IGameService
    {
        return services.TryGetValue(typeof(TInterface), out var service) ? (TInterface)service : default;
    }

    public void Shutdown()
    {
        services.Clear();
        initializationStatus.Clear();
        isGlobalInitStarted = false;
    }

}