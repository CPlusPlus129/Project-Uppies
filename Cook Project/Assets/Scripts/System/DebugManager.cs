#if UNITY_EDITOR || DEVELOPMENT_BUILD
using Cysharp.Threading.Tasks;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class DebugManager : MonoBehaviour, IDebugService
{
    private const string DEBUG_MODE_KEY = "ProjectGaslight_DebugMode";
    private IShiftSystem shiftSystem;
    private IDialogueService dialogueService;

    public bool IsDebugModeEnabled { get; private set; }

    private async void Awake()
    {
        await UniTask.WaitUntil(() => GameFlow.Instance.IsInitialized);
        shiftSystem = await ServiceLocator.Instance.GetAsync<IShiftSystem>();
        dialogueService = await ServiceLocator.Instance.GetAsync<IDialogueService>();
    }

    public async UniTask Init()
    {
        LoadDebugModeState();

        var debugAction = InputSystem.actions.FindAction("DebugKeys");
        debugAction.performed += OnDebugKeyClicked;
        DontDestroyOnLoad(gameObject);

        if (IsDebugModeEnabled)
        {
            Debug.Log("[DebugManager] Debug mode enabled. Press F12 to toggle debug mode.");
        }

        await UniTask.CompletedTask;
    }

    public void ToggleDebugMode()
    {
        IsDebugModeEnabled = !IsDebugModeEnabled;
        SaveDebugModeState();
        Debug.Log($"[DebugManager] Debug mode {(IsDebugModeEnabled ? "enabled" : "disabled")}");
    }

    public void OnDebugKeyClicked(InputAction.CallbackContext ctx)
    {
        var control = ctx.control;
        char last = control.path[^1];
        if (last is >= '0' and <= '3')
        {
            int index = last - '0';
            switch (index)
            {
                case 0:
                    ToggleDebugMode();
                    break;
                case 1:
                    Debug.Log($"Debug Num1: Add 1 served order");
                    shiftSystem.completedOrderCount.Value += 1;
                    break;
                case 2:
                    Debug.Log($"Debug Num2: StartDialogue story_test1");
                    dialogueService.StartDialogue("story_test1");
                    break;
                case 3:
                    Debug.Log($"Debug Num0: Set shiftsystem remain time to 3 sec");
                    shiftSystem.shiftTimer.Value = 3f;
                    break;
                default:
                    break;
            }
        }
    }

    private void LoadDebugModeState()
    {
        IsDebugModeEnabled = PlayerPrefs.GetInt(DEBUG_MODE_KEY, 0) == 1;
    }

    private void SaveDebugModeState()
    {
        PlayerPrefs.SetInt(DEBUG_MODE_KEY, IsDebugModeEnabled ? 1 : 0);
        PlayerPrefs.Save();
    }
}
#endif