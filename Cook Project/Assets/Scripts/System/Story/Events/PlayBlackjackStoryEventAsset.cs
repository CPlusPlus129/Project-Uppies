using System.Threading;
using BlackjackGame;
using Cysharp.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayBlackjackEvent", menuName = "Game Flow/Blackjack/Play Blackjack")]
public class PlayBlackjackStoryEventAsset : StoryEventAsset
{
    [Header("Configuration")]
    [SerializeField]
    [Tooltip("If true, the story flow pauses until the Blackjack UI is closed.")]
    private bool waitForClose = true;

    [SerializeField]
    [Tooltip("If true, forces the cheat mode (player wins) to be active.")]
    private bool forceCheatMode = false;

    public override async UniTask<StoryEventResult> ExecuteAsync(GameFlowContext context, CancellationToken cancellationToken)
    {
        var ui = Object.FindFirstObjectByType<BlackjackUI>(FindObjectsInactive.Include);
        if (ui == null)
        {
            return StoryEventResult.Failed("BlackjackUI not found in scene.");
        }

        var system = await ServiceLocator.Instance.GetAsync<IBlackjackSystem>();
        if (system == null)
        {
             return StoryEventResult.Failed("BlackjackSystem not found.");
        }
        
        if (forceCheatMode)
        {
            system.ActivateCheatMode(true);
        }

        ui.Open();

        if (waitForClose)
        {
            var tcs = new UniTaskCompletionSource();
            
            void OnClose()
            {
                ui.OnClosed -= OnClose;
                tcs.TrySetResult();
            }

            ui.OnClosed += OnClose;

            // Handle cancellation
            using (cancellationToken.Register(() => {
                ui.OnClosed -= OnClose;
                ui.Close();
                tcs.TrySetCanceled();
            }))
            {
                await tcs.Task;
            }
        }

        return StoryEventResult.Completed("Blackjack session finished.");
    }
}
