#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;

public class DebugUI : MonoBehaviour
{
    private IDebugService debugService;
    private bool showDebugPanel = true;
    private Rect windowRect = new Rect(10, 10, 300, 150);

    private async void Start()
    {
        debugService = await ServiceLocator.Instance.GetAsync<IDebugService>();

        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (debugService != null && debugService.IsDebugModeEnabled && Input.GetKeyDown(KeyCode.F2))
        {
            showDebugPanel = !showDebugPanel;
        }
    }

    private void OnGUI()
    {
        if (!showDebugPanel || debugService == null || !debugService.IsDebugModeEnabled)
            return;

        windowRect = GUI.Window(0, windowRect, DrawDebugWindow, "Debug Panel");
    }

    private void DrawDebugWindow(int windowID)
    {
        GUILayout.BeginVertical();

        GUILayout.Label("Debug Controls:", GUI.skin.label);
        GUILayout.Space(5);

        if (GUILayout.Button("Complete All Quests (F1)"))
        {
            // do something
        }
        
        GUILayout.Space(10);

        //if (questService != null)
        //{
        //    var ongoingCount = questService.ongoingQuestList.Count;
        //    var completedCount = questService.completedQuestList.Count;

        //    GUILayout.Label($"Ongoing Quests: {ongoingCount}");
        //    GUILayout.Label($"Completed Quests: {completedCount}");
        //}

        //GUILayout.Space(10);
        GUILayout.Label("F2: Toggle this panel");
        GUILayout.Label("F12: Toggle debug mode");

        if (GUILayout.Button("Toggle Debug Mode (F12)"))
        {
            debugService.ToggleDebugMode();
        }

        GUILayout.EndVertical();
        GUI.DragWindow();
    }
}
#endif