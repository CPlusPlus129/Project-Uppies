using R3;

public class WorldBroadcastSystem : SimpleSingleton<WorldBroadcastSystem>
{
    public Subject<(string, float)> onBroadcast = new Subject<(string, float)>();
    public Subject<(bool isOn, string message)> onTutorialHint = new Subject<(bool isOn, string message)>();

    public void Broadcast(string content, float length = 3f)
    {
        onBroadcast.OnNext((content, length));
    }

    public void TutorialHint(bool isOn, string content)
    {
        onTutorialHint.OnNext((isOn, content));
    }
}