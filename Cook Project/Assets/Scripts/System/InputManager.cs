using R3;
using System.Collections.Generic;
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
        UpdateCursorState();
    }

    public void PushActionMap(string mapName)
    {
        actionMapStack.Add(currentMap);
        SwitchMap(mapName);
        onActionMapChanged.OnNext(mapName);
    }

    public void PopActionMap(string mapName)
    {
        if (actionMapStack.Count == 0)
        {
            Debug.LogWarning("ActionMap stack is empty. Cannot revert.");
            return;
        }

        string previousMap = actionMapStack[actionMapStack.Count - 1];
        if (!string.IsNullOrEmpty(mapName) && previousMap != mapName)
        {
            Debug.LogWarning($"ActionMap stack mismatch. Expected '{mapName}' but top of stack is '{previousMap}'.");
        }

        actionMapStack.RemoveAt(actionMapStack.Count - 1);
        SwitchMap(previousMap);
        onActionMapChanged.OnNext(previousMap);
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
        currentMap = nextMap;
        UpdateCursorState();
    }

    private void UpdateCursorState()
    {
        if (currentMap == "Player")
            Cursor.lockState = CursorLockMode.Locked;
        else
            Cursor.lockState = CursorLockMode.None;
    }

    public string GetBindingDisplayString(string actionName, string controlScheme)
    {
        var action = InputSystem.actions.FindAction(actionName, true);
        if (action == null)
        {
            Debug.LogError($"Action '{actionName}' not found.");
            return string.Empty;
        }
        var bindingIndex = action.GetBindingIndex(InputBinding.MaskByGroup(controlScheme));
        return action.GetBindingDisplayString(bindingIndex);
    }
}
