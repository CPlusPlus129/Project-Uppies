using UnityEngine;

public class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;

    private static object _lock = new object();

    virtual protected void Awake()
    {
        if (_instance == null)
        {
            _instance = this as T;
            applicationIsQuitting = false;
            EnsureRooted(transform);
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            // Destroy duplicate instance
            Debug.Log($"[MonoSingleton] Duplicate {typeof(T)} instance found. Destroying the new one.");
            Destroy(gameObject);
        }
    }

    public static T Instance
    {
        get
        {
            if (applicationIsQuitting)
            {
                Debug.LogWarning("[Singleton] Instance '" + typeof(T) +
                                 "' already destroyed on application quit." +
                                 " Won't create again - returning null.");
                return null;
            }

            lock (_lock)
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<T>();
                    if (_instance != null)
                    {
                        EnsureRooted(_instance.transform);
                        DontDestroyOnLoad(_instance);
                    }
                    if (FindObjectsByType<T>(FindObjectsSortMode.None).Length > 1)
                    {
                        Debug.LogError("[Singleton] Something went really wrong " +
                                       " - there should never be more than 1 singleton!" +
                                       " Reopenning the scene might fix it.");
                        return _instance;
                    }

                    if (_instance == null)
                    {
                        GameObject singleton = new GameObject();
                        GameObject parent = GameObject.Find("Singleton");
                        if (parent == null)
                        {
                            parent = new GameObject("Singleton");
                            DontDestroyOnLoad(parent);
                        }
                        singleton.transform.parent = parent.transform;
                        _instance = singleton.AddComponent<T>();
                        singleton.name = "(singleton) " + typeof(T).ToString();

                        Debug.Log("[Singleton] An instance of " + typeof(T) +
                                  " is needed in the scene, so '" + singleton +
                                  "' was created with DontDestroyOnLoad.");
                        EnsureRooted(_instance.transform);
                        DontDestroyOnLoad(_instance.gameObject);
                    }
                }

                return _instance;
            }
        }
    }

    /// <summary>
    /// Returns the already-initialized instance if it exists without triggering
    /// any hierarchy changes or singleton creation side effects.
    /// </summary>
    public static bool TryGetInstance(out T instance)
    {
        instance = _instance;
        if (instance != null)
        {
            return true;
        }

        instance = FindFirstObjectByType<T>();
        if (instance != null)
        {
            _instance = instance;
            return true;
        }

        return false;
    }

    private static void EnsureRooted(Transform target)
    {
        if (target == null)
        {
            return;
        }


        if (target.parent == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Debug.Log($"[MonoSingleton] {typeof(T).Name} was parented under '{target.parent.name}'. Reparenting to root so DontDestroyOnLoad succeeds.");
            target.SetParent(null, true);
        }
        else
        {
            Debug.LogWarning($"[MonoSingleton] {typeof(T).Name} is parented under '{target.parent.name}' while editing. Move it to the scene root.");
        }
    }

    private static bool applicationIsQuitting = false;
    /// <summary>
    /// When Unity quits, it destroys objects in a random order.
    /// In principle, a Singleton is only destroyed when application quits.
    /// If any script calls Instance after it have been destroyed, 
    ///   it will create a buggy ghost object that will stay on the Editor scene
    ///   even after stopping playing the Application. Really bad!
    /// So, this was made to be sure we're not creating that buggy ghost object.
    /// </summary>
    public virtual void OnDestroy()
    {
        if (_instance == this)
            applicationIsQuitting = true;
    }
}
