using Cysharp.Threading.Tasks;
using NvJ.Rendering;
using UnityEngine;

/// <summary>
/// Tutorial step that introduces the player to the game world.
/// Starts with an initial dialogue, waits for the player to reach a specific area,
/// then triggers a second dialogue.
/// </summary>
class ZeroRoomStep : ITutorialStep
{
    private readonly IDialogueService dialogueService;
    private readonly TriggerZone triggerZone;
    private readonly string firstDialogueName = "tutorial_start";
    private readonly string secondDialogueName= "meeting_satan";

    private readonly GameObject satanLight;
    private readonly FlamePillarEffect flamePillarEffect;

    public ZeroRoomStep(TutorialContext context)
    {
        this.dialogueService = context.DialogueService;
        this.triggerZone = context.TriggerZones.Dequeue();
        this.satanLight = context.satanLight;
        this.flamePillarEffect = context.satanTeleportEffect;
    }

    public async UniTask ExecuteAsync()
    {
        triggerZone.gameObject.SetActive(true);
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

        // Satan Exits!!
        await flamePillarEffect.PlayTeleportEffect(50, 500, 2f, true);

        // Turn Lighting off
        satanLight.gameObject.SetActive(false);
        triggerZone.gameObject.SetActive(false);
        Debug.Log("[ZeroRoomStep] Step completed!");
    }

    private async UniTask WaitForPlayerToEnterZone()
    {
        await TutorialDialogueStepUtility.WaitForTriggerAsync(triggerZone);
        Debug.Log("[ZeroRoomStep] Trigger zone activated!");
    }
}
