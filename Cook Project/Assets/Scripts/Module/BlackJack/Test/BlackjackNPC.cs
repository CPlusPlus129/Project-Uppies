using TMPro;
using UnityEngine;
using R3;
using Cysharp.Threading.Tasks;

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
        [SerializeField] private StoryEventAsset preGameEvent;
        [SerializeField] private StoryEventAsset postGameEvent;
        [SerializeField] private StoryEventAsset onWinRoundEvent;
        [SerializeField] private StoryEventAsset onLoseRoundEvent;

        [Header("Task Integration")]
        [SerializeField] private string taskIdToComplete;
        [SerializeField] private bool requireTaskToStart;

        private BlackjackUI _currentUI;

        public override async void Interact()
        {
            Debug.Log($"[BlackjackNPC] Interact called on {this.GetInstanceID()}. PreGameEvent: {preGameEvent?.name}");
            if (requireTaskToStart && !string.IsNullOrEmpty(taskIdToComplete))
            {
                if (TaskManager.Instance == null) return;
                
                bool hasTask = false;
                // Check if task exists in active list
                var tasks = TaskManager.Instance.Tasks.Value;
                foreach (var task in tasks)
                {
                    if (task.Id == taskIdToComplete)
                    {
                        hasTask = true;
                        break;
                    }
                }

                if (!hasTask) return;
            }

            if (preGameEvent != null)
            {
                var runtime = GameFlow.Instance.EnqueueEvent(preGameEvent, insertAtFront: true);
                
                // Wait for completion
                try 
                {
                    await GameFlow.Instance.OnStoryEventFinished
                        .Where(x => x.runtime == runtime)
                        .FirstAsync(cancellationToken: this.GetCancellationTokenOnDestroy());
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"Error waiting for pre-game event: {e}");
                }
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
                if (onWinRoundEvent != null)
                    GameFlow.Instance.EnqueueEvent(onWinRoundEvent, insertAtFront: true);
            }
            else if (result == RoundResult.DealerWin)
            {
                if (onLoseRoundEvent != null)
                    GameFlow.Instance.EnqueueEvent(onLoseRoundEvent, insertAtFront: true);
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

            if (!string.IsNullOrEmpty(taskIdToComplete) && TaskManager.Instance != null)
            {
                TaskManager.Instance.CompleteTask(taskIdToComplete);
            }

            if (postGameEvent != null)
            {
                GameFlow.Instance.EnqueueEvent(postGameEvent, insertAtFront: true);
            }
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
