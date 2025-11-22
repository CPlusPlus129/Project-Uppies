using R3;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoSingleton<InputManager>
{
    private readonly Stack<string> activeMapStack = new();
    private string currentMap = "Player";

    public Subject<string> onActionMapChanged { get; } = new Subject<string>();

    protected override void Awake()
    {
        base.Awake();

        // Initialize: disable all non-default maps
        foreach (var map in InputSystem.actions.actionMaps)
        {
            if (map.name is not "Player" or "UI")
                map.Disable();
        }

        // Push the default map into stack
        activeMapStack.Push(currentMap);
        UpdateCursorState();
    }

    /// <summary>
    /// Push a new ActionMap onto the stack (e.g., opening a UI)
    /// </summary>
    public void PushActionMap(string mapName)
    {
        if (string.IsNullOrEmpty(mapName))
        {
            Debug.LogError("Cannot push null or empty map name");
            return;
        }

        if (currentMap == mapName)
        {
            Debug.LogWarning($"Map '{mapName}' is already active. Ignoring push.");
            return;
        }

        //Debug.Log($"[InputManager] Push: {mapName} (stack before: {string.Join(" > ", activeMapStack.Reverse())})");

        activeMapStack.Push(mapName);
        SwitchToMap(mapName);
    }

    /// <summary>
    /// Pop the specified ActionMap from the stack (e.g., closing a UI)
    /// </summary>
    public void PopActionMap(string mapName)
    {
        if (!activeMapStack.Contains(mapName))
        {
            Debug.Log($"[InputManager] Cannot pop '{mapName}': it is not in the stack.");
            return;
        }

        if (activeMapStack.Count <= 1)
        {
            Debug.LogWarning($"Cannot pop '{mapName}': stack only contains base map '{activeMapStack.Peek()}'");
            return;
        }

        string topMap = activeMapStack.Peek();

        if (topMap != mapName)
        {
            Debug.LogWarning($"[InputManager] Pop mismatch! Expected '{mapName}' but top is '{topMap}'. Stack: {string.Join(" > ", activeMapStack.Reverse())}");

            // Attempt to fix: remove the map from stack if it exists
            if (activeMapStack.Contains(mapName))
            {
                Debug.LogWarning($"Attempting to remove '{mapName}' from middle of stack (this indicates incorrect usage)");
                var tempStack = new Stack<string>();
                bool found = false;

                while (activeMapStack.Count > 0)
                {
                    string map = activeMapStack.Pop();
                    if (map == mapName)
                    {
                        found = true;
                        break;
                    }
                    tempStack.Push(map);
                }

                // Restore stack (without the removed item)
                while (tempStack.Count > 0)
                {
                    activeMapStack.Push(tempStack.Pop());
                }

                if (found)
                {
                    Debug.LogWarning($"Removed '{mapName}' from stack. Current stack: {string.Join(" > ", activeMapStack.Reverse())}");
                }
            }
            return;
        }

        // Normal pop
        activeMapStack.Pop();
        string previousMap = activeMapStack.Peek();

        //Debug.Log($"[InputManager] Pop: {mapName} → back to {previousMap}");
        SwitchToMap(previousMap);
    }

    /// <summary>
    /// Directly set the ActionMap (clears stack and sets as base map)
    /// Use for scene transitions or complete resets
    /// </summary>
    public void SetActionMap(string mapName)
    {
        //Debug.Log($"[InputManager] SetActionMap: {mapName} (clearing stack)");

        activeMapStack.Clear();
        activeMapStack.Push(mapName);
        SwitchToMap(mapName);
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

    private void SwitchToMap(string nextMap)
    {
        if (currentMap == nextMap)
            return;

        var asset = InputSystem.actions;

        // Disable current map
        var currentActionMap = asset.FindActionMap(currentMap);
        if (currentActionMap != null)
        {
            currentActionMap.Disable();
            Debug.Log($"[InputManager] Disabled: {currentMap}");
        }

        // Enable new map
        var nextActionMap = asset.FindActionMap(nextMap);
        if (nextActionMap != null)
        {
            nextActionMap.Enable();
            currentMap = nextMap;
            Debug.Log($"[InputManager] Enabled: {nextMap}");
        }
        else
        {
            Debug.LogError($"Cannot switch to '{nextMap}': ActionMap not found");
            return;
        }

        UpdateCursorState();
        onActionMapChanged.OnNext(nextMap);
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

    // Debug utility: log current stack state
    public void LogStackState()
    {
        Debug.Log($"[InputManager] Current: {currentMap}, Stack: {string.Join(" > ", activeMapStack.Reverse())}");
    }
}
