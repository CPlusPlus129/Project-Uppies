public class SimpleSingleton<T> where T : class, new()
{
    private static T _instance;
    public static T Instance => _instance ??= new T();
}