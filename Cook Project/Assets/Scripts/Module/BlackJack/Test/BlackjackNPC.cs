using TMPro;
using UnityEngine;

namespace BlackjackGame
{
    public class BlackjackNPC : InteractableBase
    {
        [SerializeField] private string npcName;
        [SerializeField] private TextMeshPro nameText;

        protected override void Awake()
        {
            base.Awake();
            if (nameText != null)
                nameText.text = npcName;
        }

        [Header("Story Integration")]
        [SerializeField] private StorySequenceAsset preGameSequence;
        [SerializeField] private StorySequenceAsset postGameSequence;
        [SerializeField] private StorySequenceAsset onWinRoundSequence;
        [SerializeField] private StorySequenceAsset onLoseRoundSequence;

        private BlackjackUI _currentUI;

        public override async void Interact()
        {
            if (preGameSequence != null)
            {
                GameFlow.Instance.EnqueueSequence(preGameSequence, insertAtFront: true);
                await WaitForSequenceCompletion(preGameSequence);
            }

            var ui = UIRoot.Instance.GetUIComponent<BlackjackUI>();
            _currentUI = ui;
            
            // Subscribe to events
            ui.OnRoundCompleted += HandleRoundCompleted;
            ui.OnClosed += HandleUIClosed;
            
            ui.Open();
        }

        private void HandleRoundCompleted(RoundResult result, int chipsWon)
        {
            if (result == RoundResult.PlayerWin || result == RoundResult.PlayerBlackjack)
            {
                if (onWinRoundSequence != null)
                    GameFlow.Instance.EnqueueSequence(onWinRoundSequence, insertAtFront: true);
            }
            else if (result == RoundResult.DealerWin)
            {
                if (onLoseRoundSequence != null)
                    GameFlow.Instance.EnqueueSequence(onLoseRoundSequence, insertAtFront: true);
            }
        }

        private void HandleUIClosed()
        {
            var ui = UIRoot.Instance.GetUIComponent<BlackjackUI>();
            if (ui != null)
            {
                ui.OnRoundCompleted -= HandleRoundCompleted;
                ui.OnClosed -= HandleUIClosed;
            }
            _currentUI = null;

            if (postGameSequence != null)
            {
                GameFlow.Instance.EnqueueSequence(postGameSequence, insertAtFront: true);
            }
        }

        private async Cysharp.Threading.Tasks.UniTask WaitForSequenceCompletion(StorySequenceAsset sequence)
        {
            // Wait until the sequence is no longer in the pending events or active
            await Cysharp.Threading.Tasks.UniTask.WaitUntil(() => 
            {
                // Check if current event belongs to sequence
                if (GameFlow.Instance.CurrentStoryEvent?.SourceSequence == sequence)
                    return false;
                
                // Check if any queued event belongs to sequence
                var snapshot = GameFlow.Instance.GetPendingEventsSnapshot();
                foreach (var evt in snapshot)
                {
                    if (evt.SourceSequence == sequence)
                        return false;
                }
                
                return true;
            });
        }

        private void OnDestroy()
        {
            if (_currentUI != null)
            {
                _currentUI.OnRoundCompleted -= HandleRoundCompleted;
                _currentUI.OnClosed -= HandleUIClosed;
                _currentUI = null;
            }
        }
    }
}
