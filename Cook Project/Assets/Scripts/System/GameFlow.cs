public class GameFlow : MonoSingleton<GameFlow>
{
    protected override void Awake()
    {
        StartGame();
    }

    private void StartGame()
    {
        ShiftSystem.Instance.StartGame();
    }
}