# Unity Interactable Setup

Use `UnityInteractable` when you want a designer-friendly way to hook interactions to arbitrary behaviour—dialogue, tweens, particle cues, etc.—without writing a new script for each object.

## 1. Prepare the GameObject
- Select the GameObject the player should interact with.
- Ensure the collider you want the player to target lives on the same GameObject. If the collider is on a child, either move the collider up or add the component to that child instead.

## 2. Add the Components
1. Click **Add Component** → search for **Unity Interactable** (`Assets/Scripts/Common/UnityInteractable.cs`).
2. If the object needs to trigger dialogue, also add `DialogueEventPlayer` and assign its `Default Event`.

When you reset `UnityInteractable` it will:
- Reuse or add a `BoxCollider` (auto-sizing for `BillboardSprite` objects).
- Set the object to the `Interactable` layer if it exists.

## 3. Configure Inspector Options
- **Interact Once**: Limit to a single activation. Optionally specify behaviours/colliders to disable afterward.
- **On First Interact**: UnityEvent fired the first time interaction succeeds.
- **On Interact**: UnityEvent fired on every interaction.
- **Fit Collider To Billboard**: Keep enabled for billboard sprites; it auto-sizes the collider to the sprite dimensions.

Example wiring for dialogue:
1. Drag the `DialogueEventPlayer` component into the `On Interact` list.
2. Choose `DialogueEventPlayer → Play()` for single events, or `PlayCollection()` if you have assigned a dialogue collection.

## 4. Verify Raycast Settings
- Check that the object’s layer is included in `PlayerInteract.interactLayer` (defaults to the `Interactable` layer in the Player prefab).
- Collider must be enabled; for trigger-based prompts, keep it non-trigger so the raycast hits it.

## 5. Test In Play Mode
- Aim the camera at the object. The “[E] Interact” prompt from `HUD` should appear.
- Press the interact key—your UnityEvents should fire (e.g., dialogue plays, particle effect starts).

Tip: If you need to reset a one-shot interactable for testing, call `UnityInteractable.Reset()` in the inspector or uncheck/recheck **Interact Once**.


**IMPORTANT**: If you're connecting the interactable to a `DialogueEventPlayer`, keep in mind that `DialogueEventPlayer` has a **Play Once** setting independent of the `UnityInteractable` option **Interact Once**. 
