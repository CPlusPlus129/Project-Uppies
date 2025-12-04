using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UI;

using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

[CreateAssetMenu(fileName = "ShowEndingScreenEvent", menuName = "Game Flow/Story Events/Show Ending Screen")]
public class ShowEndingScreenStoryEventAsset : StoryEventAsset
{
    public override async UniTask<StoryEventResult> ExecuteAsync(GameFlowContext context, CancellationToken cancellationToken)
    {
        var endingController = FindFirstObjectByType<EndingScreenController>(FindObjectsInactive.Include);

        if (endingController == null)
        {
            Debug.LogError("ShowEndingScreenStoryEvent: No EndingScreenController found in the scene.");
            return StoryEventResult.Failed("No EndingScreenController found.");
        }

        // Push UI input map to ensure player can interact with the menu
        // We do this before showing the screen
        if (InputManager.Instance != null)
        {
            InputManager.Instance.PushActionMap("UI");

            // Fix: Ensure InputSystemUIInputModule is using the correct actions from InputManager
            // This handles cases where the scene EventSystem is using DefaultInputActions
            var eventSystem = UnityEngine.EventSystems.EventSystem.current;
            if (eventSystem != null)
            {
                var uiModule = eventSystem.GetComponent<InputSystemUIInputModule>();
                if (uiModule != null)
                {
                    var uiMap = InputManager.Instance.GetActionMap("UI");
                    if (uiMap != null)
                    {
                        // Map actions to the module
                        if (uiMap.FindAction("Navigate") is var move && move != null) uiModule.move = InputActionReference.Create(move);
                        if (uiMap.FindAction("Submit") is var submit && submit != null) uiModule.submit = InputActionReference.Create(submit);
                        if (uiMap.FindAction("Cancel") is var cancel && cancel != null) uiModule.cancel = InputActionReference.Create(cancel);
                        if (uiMap.FindAction("Point") is var point && point != null) uiModule.point = InputActionReference.Create(point);
                        if (uiMap.FindAction("Click") is var click && click != null) uiModule.leftClick = InputActionReference.Create(click);
                        if (uiMap.FindAction("RightClick") is var rClick && rClick != null) uiModule.rightClick = InputActionReference.Create(rClick);
                        if (uiMap.FindAction("MiddleClick") is var mClick && mClick != null) uiModule.middleClick = InputActionReference.Create(mClick);
                        if (uiMap.FindAction("ScrollWheel") is var scroll && scroll != null) uiModule.scrollWheel = InputActionReference.Create(scroll);
                    }
                }
            }
        }

        // Show the screen
        endingController.Show();

        try
        {
            // Wait for the user to click continue (which resolves the task in the controller)
            // We attach the cancellation token so the event can be cancelled if the story flow is interrupted
            await endingController.WaitForContinueAsync().AttachExternalCancellation(cancellationToken);
        }
        finally
        {
            // Always pop the input map when we're done, even if cancelled
            if (InputManager.Instance != null)
            {
                InputManager.Instance.PopActionMap("UI");
            }
        }

        return StoryEventResult.Completed("Ending screen flow finished.");
    }
}
