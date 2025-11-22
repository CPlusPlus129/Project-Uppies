using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(menuName = "Game Flow/Story Events/Vault Door Event")]
public class VaultDoorStoryEvent : StoryEventAsset
{
    [SerializeField] private string targetDoorId = "VaultDoor";
    [SerializeField] private bool open = true;
    [SerializeField] private bool waitForCompletion = true;

    public override async UniTask<StoryEventResult> ExecuteAsync(GameFlowContext context, CancellationToken cancellationToken)
    {
        var door = VaultDoorController.Get(targetDoorId);
        if (door == null)
        {
            Debug.LogError($"VaultDoorStoryEvent: Could not find VaultDoorController with ID '{targetDoorId}'");
            return StoryEventResult.Completed(); 
        }

        if (waitForCompletion)
        {
            if (open) await door.OpenAsync(cancellationToken);
            else await door.CloseAsync(cancellationToken);
        }
        else
        {
            if (open) door.OpenAsync(cancellationToken).Forget();
            else door.CloseAsync(cancellationToken).Forget();
        }

        return StoryEventResult.Completed();
    }
}
