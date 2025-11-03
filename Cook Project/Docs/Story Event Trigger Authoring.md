Story Event Trigger Authoring
=============================

Overview
--------
Story Event Trigger Authoring lets designers drop trigger volumes into a scene that enqueue `StoryEventAsset` or `StorySequenceAsset` content when the player enters. It mirrors the existing dialogue trigger workflow and depends on the `StoryEventTriggerAuthoring` MonoBehaviour and the `GameFlow` system.

Creating a Trigger From the Hierarchy Menu
-----------------------------------------
1. In the Hierarchy, choose **GameObject ▸ Story ▸ Story Event Trigger**.
2. A new GameObject named `Story Event Trigger` is created with:
   - `BoxCollider` configured as a trigger.
   - `StoryEventTriggerAuthoring` component pre-attached.
3. Position/scale the collider to cover the desired activation volume.
4. (Optional) Parent the object under an existing environment root before or after creation to keep the hierarchy tidy.

Configuring the StoryEventTriggerAuthoring Component
---------------------------------------------------
- **Story Sequence** *(optional)*: Assign a `StorySequenceAsset` to enqueue a full sequence when the trigger fires. Takes priority if both sequence and single event are set.
- **Story Event** *(optional)*: Assign a `StoryEventAsset` for single-event triggers.
- **Insert At Front**: Enable to push the sequence/event to the front of the GameFlow queue so it executes immediately after the current event.
- **Triggering Tag**: Defaults to `Player`. Only colliders with this tag will fire the trigger.
- **Trigger Once**: Prevents re-triggering after the first success.
- **Disable Collider After Trigger**: Disables the collider after a successful fire (useful when `Trigger Once` is true).
- **On Triggered / On Failed (UnityEvents)**: Hook additional reactions (VFX, audio cues, etc.). `On Failed` fires when configuration or runtime state prevents the enqueue (e.g., missing assets, GameFlow not initialized).

Authoring Workflow Tips
-----------------------
- Use sequences for multi-step beats and single events for quick one-off actions.
- Keep `logStoryFlow` enabled on `GameFlow` while iterating to confirm triggers enqueue as expected.
- Combine with `StoryEventResult.NextSequence` inside events for branching after the trigger plays.
- If you need gizmo visualization, add a child mesh or gizmo script—`StoryEventTriggerAuthoring` only manages logic.

Restoring Player State
----------------------
If your triggered event (e.g., `storageRoomPuzzle1`) modifies player scale, keep follow-up events in the same sequence to restore state once the puzzle completes.

Troubleshooting
---------------
- **Trigger doesn’t fire**: Check the collider tag on the player object, ensure the collider is scaled properly, and confirm `Trigger Once` isn’t locking it out from prior tests.
- **No story plays**: Verify `GameFlow` is present/initialized and that the assigned sequence/event asset contains valid items.
- **Queue order unexpected**: Toggle `Insert At Front` depending on whether the trigger should interrupt or append to the current queue.

