using System;
using System.Threading;
using Cysharp.Threading.Tasks;

public sealed class GameFlowContext
{
    public GameFlow Flow { get; }
    public StoryEventRuntime Runtime { get; }
    public CancellationToken CancellationToken { get; }

    internal GameFlowContext(GameFlow flow, StoryEventRuntime runtime, CancellationToken cancellationToken)
    {
        Flow = flow ?? throw new ArgumentNullException(nameof(flow));
        Runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
        CancellationToken = cancellationToken;
    }

    public async UniTask WaitForSignalAsync(string signalId, CancellationToken cancellationToken = default)
    {
        if (cancellationToken == default || cancellationToken == CancellationToken)
        {
            await Flow.WaitForSignalAsync(signalId, CancellationToken);
            return;
        }

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(CancellationToken, cancellationToken);
        await Flow.WaitForSignalAsync(signalId, linkedCts.Token);
    }

    public void SendSignal(string signalId)
    {
        Flow.Signal(signalId);
    }

    public UniTask<T> GetServiceAsync<T>() where T : IGameService
    {
        return ServiceLocator.Instance.GetAsync<T>();
    }

    public bool IsServiceReady<T>() where T : class, IGameService
    {
        return ServiceLocator.Instance.IsServiceReady<T>();
    }

    public bool TryGetLastResult(string eventId, out StoryEventResult result)
    {
        return Flow.TryGetStoryEventResult(eventId, out result);
    }
}
