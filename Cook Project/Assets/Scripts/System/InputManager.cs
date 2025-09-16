using R3;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoSingleton<InputManager>
{
    private string currentMap = "Player";
    private List<string> actionMapStack = new List<string>();
    public Subject<string> onActionMapChanged { get; } = new Subject<string>();

    protected override void Awake()
    {
        base.Awake();
        // project-wide input settings solution
        foreach (var map in InputSystem.actions.actionMaps)
        {
            if (map.name is not "Player" or "UI")
                map.Disable();
        }
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    public void PushActionMap(string mapName)
    {
        actionMapStack.Add(currentMap);
        SwitchMap(mapName);
        onActionMapChanged.OnNext(mapName);
    }

    public void PopActionMap(string mapName)
    {
        if (actionMapStack.Count > 0)
        {
            actionMapStack.Remove(mapName);
            string previousMap = actionMapStack.Last();
            SwitchMap(previousMap);
            onActionMapChanged.OnNext(previousMap);
        }
        else
        {
            Debug.LogWarning("ActionMap stack is empty. Cannot revert.");
        }
    }

    public void SetActionMap(string mapName)
    {
        actionMapStack.Clear();
        SwitchMap(mapName);
        onActionMapChanged.OnNext(mapName);
    }

    public string GetCurrentMap() => currentMap;

    public InputActionMap GetActionMap(string mapName)
    {
        var actionMap = InputSystem.actions.FindActionMap(mapName, true);
        if (actionMap == null)
        {
            Debug.LogError($"ActionMap '{mapName}' not found.");
            return null;
        }
        return actionMap;
    }

    private void SwitchMap(string nextMap)
    {
        var asset = InputSystem.actions;
        asset.FindActionMap(currentMap)?.Disable();
        asset.FindActionMap(nextMap)?.Enable();

        if (nextMap == "Player")
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }
}