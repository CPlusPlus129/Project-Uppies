public class SimpleSingleton<T> where T : class, new()
{
    private static T _instance;

    public static bool HasInstance => _instance != null;

    public static T Instance => _instance ??= new T();

    public static T TryGetInstance() => _instance;

    public static void ResetInstance()
    {
        _instance = null;
    }
}
