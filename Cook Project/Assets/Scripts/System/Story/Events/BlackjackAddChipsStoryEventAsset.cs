using System.Threading;
using BlackjackGame;
using Cysharp.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(fileName = "BlackjackAddChipsEvent", menuName = "Game Flow/Blackjack/Add Chips")]
public class BlackjackAddChipsStoryEventAsset : StoryEventAsset
{
    [SerializeField]
    [Tooltip("Amount of chips to add (or subtract if negative).")]
    private int amount;

    public override async UniTask<StoryEventResult> ExecuteAsync(GameFlowContext context, CancellationToken cancellationToken)
    {
        var system = await ServiceLocator.Instance.GetAsync<IBlackjackSystem>();
        if (system == null)
        {
            return StoryEventResult.Failed("BlackjackSystem not found.");
        }

        system.AddChips(amount);
        return StoryEventResult.Completed($"Added {amount} chips. New balance: {system.PlayerChips.CurrentValue}");
    }
}
