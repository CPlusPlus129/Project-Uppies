using R3;

public class WorldBroadcastSystem : SimpleSingleton<WorldBroadcastSystem>
{
    public Subject<(string, float)> onBroadcast = new Subject<(string, float)>();

    public void Broadcast(string content, float length = 3f)
    {
        onBroadcast.OnNext((content, length));
    }
}