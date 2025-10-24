using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;

/// <summary>
/// Tutorial step that introduces the player to the game world.
/// Starts with an initial dialogue, waits for the player to reach a specific area,
/// then triggers a second dialogue.
/// </summary>
class ZeroRoomStep : ITutorialStep
{
    private readonly IDialogueService dialogueService;
    private readonly TriggerZone triggerZone;
    private readonly string firstDialogueName;
    private readonly string secondDialogueName;

    private readonly GameObject satanLight;
    private CompositeDisposable disposables = new CompositeDisposable();

    public ZeroRoomStep(IDialogueService dialogueService, TriggerZone triggerZone, string firstDialogueName, string secondDialogueName, GameObject satanLight)
    {
        this.dialogueService = dialogueService;
        this.triggerZone = triggerZone;
        this.firstDialogueName = firstDialogueName;
        this.secondDialogueName = secondDialogueName;
        this.satanLight = satanLight;
    }

    public async UniTask ExecuteAsync()
    {
        Debug.Log($"[ZeroRoomStep] Starting first dialogue: {firstDialogueName}");
        // Start the first dialogue automatically

        await dialogueService.StartDialogueAsync(firstDialogueName);
        Debug.Log($"[ZeroRoomStep] First dialogue completed: {firstDialogueName}");

        // Wait for player to enter the trigger zone
        Debug.Log("[ZeroRoomStep] Waiting for player to enter trigger zone...");
        await WaitForPlayerToEnterZone();
        Debug.Log("[ZeroRoomStep] Player entered trigger zone!");

        // Light up Satan sprite
        satanLight.gameObject.SetActive(true);
        
        // Start the second dialogue
        Debug.Log($"[ZeroRoomStep] Starting second dialogue: {secondDialogueName}");
        await dialogueService.StartDialogueAsync(secondDialogueName);
        Debug.Log($"[ZeroRoomStep] Second dialogue completed: {secondDialogueName}");
        Debug.Log("[ZeroRoomStep] Step completed!");
    }

    private async UniTask WaitForPlayerToEnterZone()
    {
        var tcs = new UniTaskCompletionSource();
        
        triggerZone.OnPlayerEnter
            .Take(1)
            .Subscribe(_ =>
            {
                Debug.Log("[ZeroRoomStep] Trigger zone activated!");
                disposables.Clear();
                tcs.TrySetResult();
            })
            .AddTo(disposables);

        await tcs.Task;
    }
}
