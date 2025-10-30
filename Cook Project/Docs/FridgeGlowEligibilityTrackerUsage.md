Fridge Glow Eligibility Tracker Usage
=====================================

Overview
--------
- **Component**: `FridgeGlowEligibilityTracker` (`Assets/Scripts/System/FridgeGlowEligibilityTracker.cs`)
- **Responsibility**: mirrors the current `FridgeGlowManager` eligibility snapshot so designers or other systems can flip glow states on the correct fridges.
- **Initialization**: waits for `ServiceLocator` to mark `IFridgeGlowManager` as ready before subscribing; no manual setup needed beyond dropping it in the scene.

Public API
----------
- `IReadOnlyList<FoodSource> EligibleFridges`  
  Read-only list of the most recent eligible fridges. The tracker resyncs this backing list before every command, so you can safely inspect it mid-frame for UI or debugging.

- `void EnableGlowOnEligible()`  
  Refreshes the manager snapshot, then calls `EnableGlow()` on each eligible `FoodSource`. Use when you want eligible fridges to stay lit until you explicitly turn them off.

- `void DisableGlowOnEligible()`  
  Refreshes the snapshot, then calls `DisableGlow()` on each eligible fridge and clears any leftover glow on fridges that dropped out of eligibility. Call this to reset highlights.

- `void EnableGlowOnEligibleForDuration(float seconds)`  
  Same as `EnableGlowOnEligible`, but delegates timed shutoff to each fridge (`EnableGlowForDuration(seconds)`). Pass `seconds <= 0` to fall back to an indefinite glow.

Triggering Through `UnityInteractable`
--------------------------------------
Use `UnityInteractable` (`Assets/Scripts/Common/UnityInteractable.cs`) for designer-friendly hookups.

1. **Attach Components**
   - Place `FridgeGlowEligibilityTracker` on a persistent scene object (Already done in main level)
   - On the interactable object, add a `UnityInteractable` component (it auto-adds/aligns a collider on reset).

2. **Wire the Event**
   - In the inspector, expand **On Interact** (fires every activation) or **On First Interact** (single-use).
   - Click **+**, drag the tracker GameObject into the object field.
   - Choose the desired method from the dropdown:
     - `FridgeGlowEligibilityTracker → EnableGlowOnEligible()`
     - `FridgeGlowEligibilityTracker → DisableGlowOnEligible()`
     - `FridgeGlowEligibilityTracker → EnableGlowOnEligibleForDuration(float)` (enter the duration beside it).

3. **Gameplay Flow**
   - Your interaction system calls `UnityInteractable.Interact()` (already handled for objects on the **Interactable** layer).
   - The UnityEvent invokes the tracker method.
   - The tracker pulls a fresh snapshot from `FridgeGlowManager` and toggles glow on every eligible fridge automatically.

Testing Tips
------------
- In Play Mode, interact with the UnityInteractable and watch the eligible fridges light up; use the tracker’s `enableDebugLogs` to print the fridge list.
- For timed glows, confirm the highlights clear themselves after the specified duration without additional calls.
