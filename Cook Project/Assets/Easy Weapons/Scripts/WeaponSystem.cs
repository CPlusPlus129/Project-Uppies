/// <summary>
/// WeaponSystem.cs - Modernized for Unity 6.2
/// Manages weapon switching with number keys and mouse scroll
/// Supports dynamic weapon arrays and smooth transitions
/// </summary>

using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class WeaponSystem : MonoBehaviour
{
    #region Configuration
    [Header("Weapon Setup")]
    [Tooltip("All available weapons (must have Weapon component)")]
    public List<GameObject> weapons = new List<GameObject>();
    
    [Tooltip("Starting weapon index")]
    [Range(0, 9)]
    public int startingWeaponIndex = 0;
    
    [Header("Input Settings")]
    [Tooltip("Enable mouse scroll wheel switching")]
    public bool allowScrollWheel = true;
    
    [Tooltip("Enable number key switching (1-9)")]
    public bool allowNumberKeys = true;
    
    [Tooltip("Scroll direction (1 = normal, -1 = inverted)")]
    [Range(-1, 1)]
    public int scrollDirection = 1;
    
    [Header("Debug")]
    [Tooltip("Show weapon switching debug logs")]
    public bool showDebugLogs = false;
    #endregion

    #region Private State
    private int currentWeaponIndex = 0;
    private Dictionary<int, InputAction> numberKeyActions = new Dictionary<int, InputAction>();
    #endregion

    #region Unity Lifecycle
    private void Start()
    {
        ValidateWeapons();
        InitializeInputActions();
        SwitchToWeapon(startingWeaponIndex);
    }

    private void Update()
    {
        if (allowNumberKeys)
        {
            CheckNumberKeyInput();
        }
        
        if (allowScrollWheel)
        {
            CheckScrollInput();
        }
    }
    #endregion

    #region Initialization
    private void ValidateWeapons()
    {
        // Remove null entries
        weapons.RemoveAll(w => w == null);
        
        if (weapons.Count == 0)
        {
            Debug.LogWarning("[WeaponSystem] No weapons assigned!", this);
            enabled = false;
            return;
        }
        
        // Validate starting index
        if (startingWeaponIndex < 0 || startingWeaponIndex >= weapons.Count)
        {
            Debug.LogWarning($"[WeaponSystem] Invalid starting weapon index {startingWeaponIndex}. Using 0.", this);
            startingWeaponIndex = 0;
        }
        
        // Validate all weapons have Weapon component
        foreach (var weapon in weapons)
        {
            if (weapon != null && weapon.GetComponent<Weapon>() == null)
            {
                Debug.LogWarning($"[WeaponSystem] Weapon '{weapon.name}' missing Weapon component!", this);
            }
        }
    }

    private void InitializeInputActions()
    {
        // Cache number key input actions for better performance
        for (int i = 1; i <= 9; i++)
        {
            string actionName = $"Weapon {i}";
            InputAction action = InputSystem.actions?.FindAction(actionName);
            if (action != null)
            {
                numberKeyActions[i] = action;
            }
        }
    }
    #endregion

    #region Input Handling
    private void CheckNumberKeyInput()
    {
        // Check cached input actions
        foreach (var kvp in numberKeyActions)
        {
            if (kvp.Value.WasPressedThisFrame())
            {
                int weaponIndex = kvp.Key - 1; // Convert 1-9 to 0-8
                SwitchToWeapon(weaponIndex);
                return;
            }
        }
        
        // Fallback to legacy Input (for compatibility)
        for (int i = 1; i <= 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha0 + i))
            {
                SwitchToWeapon(i - 1);
                return;
            }
        }
    }

    private void CheckScrollInput()
    {
        float scrollDelta = Input.mouseScrollDelta.y;
        
        if (scrollDelta > 0f)
        {
            if (scrollDirection > 0)
                NextWeapon();
            else
                PreviousWeapon();
        }
        else if (scrollDelta < 0f)
        {
            if (scrollDirection > 0)
                PreviousWeapon();
            else
                NextWeapon();
        }
    }
    #endregion

    #region Weapon Switching
    /// <summary>
    /// Switch to a specific weapon by index
    /// </summary>
    public void SwitchToWeapon(int index)
    {
        // Validate index
        if (index < 0 || index >= weapons.Count)
        {
            if (showDebugLogs)
            {
                Debug.LogWarning($"[WeaponSystem] Invalid weapon index: {index}. Valid range: 0-{weapons.Count - 1}");
            }
            return;
        }
        
        // Don't switch if already active
        if (index == currentWeaponIndex && weapons[index].activeSelf)
        {
            return;
        }
        
        // Stop beam effects on current weapon before switching
        if (weapons[currentWeaponIndex] != null)
        {
            Weapon currentWeapon = weapons[currentWeaponIndex].GetComponent<Weapon>();
            if (currentWeapon != null)
            {
                currentWeapon.StopBeam();
            }
        }
        
        // Deactivate all weapons
        foreach (GameObject weapon in weapons)
        {
            if (weapon != null)
            {
                weapon.SetActive(false);
            }
        }
        
        // Activate selected weapon
        weapons[index].SetActive(true);
        currentWeaponIndex = index;
        
        // Notify listeners
        SendMessageUpwards("OnWeaponSwitch", index, SendMessageOptions.DontRequireReceiver);
        
        if (showDebugLogs)
        {
            Debug.Log($"[WeaponSystem] Switched to weapon {index}: {weapons[index].name}");
        }
    }

    /// <summary>
    /// Switch to the next weapon in the list (wraps around)
    /// </summary>
    public void NextWeapon()
    {
        int nextIndex = currentWeaponIndex + 1;
        if (nextIndex >= weapons.Count)
        {
            nextIndex = 0;
        }
        SwitchToWeapon(nextIndex);
    }

    /// <summary>
    /// Switch to the previous weapon in the list (wraps around)
    /// </summary>
    public void PreviousWeapon()
    {
        int prevIndex = currentWeaponIndex - 1;
        if (prevIndex < 0)
        {
            prevIndex = weapons.Count - 1;
        }
        SwitchToWeapon(prevIndex);
    }
    #endregion

    #region Public API
    /// <summary>
    /// Get the currently active weapon GameObject
    /// </summary>
    public GameObject GetCurrentWeapon()
    {
        if (currentWeaponIndex >= 0 && currentWeaponIndex < weapons.Count)
        {
            return weapons[currentWeaponIndex];
        }
        return null;
    }

    /// <summary>
    /// Get the currently active Weapon component
    /// </summary>
    public Weapon GetCurrentWeaponComponent()
    {
        GameObject currentWeapon = GetCurrentWeapon();
        return currentWeapon != null ? currentWeapon.GetComponent<Weapon>() : null;
    }

    /// <summary>
    /// Get the current weapon index
    /// </summary>
    public int GetCurrentWeaponIndex()
    {
        return currentWeaponIndex;
    }

    /// <summary>
    /// Get total number of weapons
    /// </summary>
    public int GetWeaponCount()
    {
        return weapons.Count;
    }

    /// <summary>
    /// Add a weapon to the system at runtime
    /// </summary>
    public void AddWeapon(GameObject weapon)
    {
        if (weapon != null && !weapons.Contains(weapon))
        {
            weapons.Add(weapon);
            weapon.SetActive(false);
            
            if (showDebugLogs)
            {
                Debug.Log($"[WeaponSystem] Added weapon: {weapon.name}");
            }
        }
    }

    /// <summary>
    /// Remove a weapon from the system at runtime
    /// </summary>
    public void RemoveWeapon(GameObject weapon)
    {
        if (weapons.Contains(weapon))
        {
            int removedIndex = weapons.IndexOf(weapon);
            weapons.Remove(weapon);
            
            // Adjust current index if needed
            if (currentWeaponIndex >= weapons.Count)
            {
                currentWeaponIndex = weapons.Count - 1;
            }
            
            // Switch to valid weapon if we removed the current one
            if (removedIndex == currentWeaponIndex && weapons.Count > 0)
            {
                SwitchToWeapon(currentWeaponIndex);
            }
            
            if (showDebugLogs)
            {
                Debug.Log($"[WeaponSystem] Removed weapon: {weapon.name}");
            }
        }
    }

    /// <summary>
    /// Remove a weapon by index
    /// </summary>
    public void RemoveWeaponAt(int index)
    {
        if (index >= 0 && index < weapons.Count)
        {
            GameObject weapon = weapons[index];
            RemoveWeapon(weapon);
        }
    }
    #endregion

    #region Debug
    private void OnValidate()
    {
        // Clamp starting index in editor
        if (startingWeaponIndex < 0)
            startingWeaponIndex = 0;
        
        if (weapons.Count > 0 && startingWeaponIndex >= weapons.Count)
            startingWeaponIndex = weapons.Count - 1;
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || weapons.Count == 0) return;
        
        // Draw weapon indicator
        GameObject currentWeapon = GetCurrentWeapon();
        if (currentWeapon != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(currentWeapon.transform.position, 0.1f);
        }
    }
    #endregion
}
