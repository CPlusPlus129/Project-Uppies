#if UNITY_EDITOR || DEVELOPMENT_BUILD
public interface IDebugService : IGameService
{
    bool IsDebugModeEnabled { get; }
    void ToggleDebugMode();
}
#endif