using UnityEngine;

/// <summary>
/// Be aware this will not prevent a non singleton constructor
///   such as `T myT = new T();`
/// To prevent that, add `protected T () {}` to your singleton class.
/// 
/// As a note, this is made as MonoBehaviour because we use Unity lifecycle.
/// </summary>
public class SceneSingleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;

    private static object _lock = new object();

    virtual protected void Awake()
    {
        if (_instance == null)
            _instance = this as T;
        else if (_instance != this)
        {
            // Destroy duplicate instance
            Debug.Log($"[SceneSingleton] Duplicate {typeof(T)} instance found. Destroying the new one.");
            Destroy(gameObject);
        }
    }

    virtual protected void OnDestroy()
    {
        if(_instance == this)
            _instance = null;
    }

    public static T Instance
    {
        get { return _instance; }
    }
}
