using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

public class TutorialFlow : MonoBehaviour
{
    [SerializeField] private Customer customer;
    [SerializeField] private SimpleDoor firstDoor;
    private void Start()
    {
        StartTutorial().Forget();
    }

    private async UniTaskVoid StartTutorial()
    {
        await UniTask.WaitUntil(() => GameFlow.Instance.isInitialized);
#if UNITY_WEBGL && !UNITY_EDITOR
        await UniTask.Delay(2000); //wait for webgl to load
#endif
        Debug.Log("Start tutorial!");
        var dialogueService = await ServiceLocator.Instance.GetAsync<IDialogueService>();
        var orderManager = await ServiceLocator.Instance.GetAsync<IOrderManager>();
        var steps = new List<ITutorialStep>
        {
            new FirstRoomStep(dialogueService, orderManager, customer, firstDoor),
            // new SecondRoomStep(...),
            // new ThirdRoomStep(...)
        };

        foreach (var step in steps)
        {
            await step.ExecuteAsync();
        }

        Debug.Log("Tutorial Finished!");
    }

}