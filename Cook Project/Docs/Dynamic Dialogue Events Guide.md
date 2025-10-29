# Dialogue Event Quickstart

## 1. Create a Dialogue Event Asset
- Right-click in the Project window → **Create > Dialogue > Event Asset**.
- Set the `Label` to match the row you want from the dialogue CSV.
- Optionally add a hint (surfaced by systems such as tutorials) and leave **Play Once** checked if it should only fire once per scene load.

## 2. Drop a Dialogue Trigger
- Use **GameObject > Dialogue > Box Trigger** (or add `DialogueTriggerAuthoring` to an existing object).
- The prefab comes with:
  - `BoxCollider` set to trigger.
  - `DialogueEventPlayer` to talk to the dialogue service.
  - `DialogueTriggerAuthoring` that listens for tagged colliders (defaults to `Player`).

## 3. Configure Behaviour
- Assign your new `DialogueEventAsset` to the `Dialogue Event Player` component.
- Pick a trigger mode: **Play Immediately**, **Queue If Busy** (default), or **Try Play Only**.
- Hook optional UnityEvents:
  - `Dialogue Event Player` → `On Dialogue Started/Completed/Denied`.
  - `Dialogue Trigger Authoring` → `On Enter` (fires once the trigger succeeds) and `On Denied`.
- Use the custom inspector buttons to size the collider (`Configure Box Trigger`, `Fit Collider To Children`) or to create another event asset quickly.

## 4. Optional: Manual Triggering
- Any script can store a reference to `DialogueEventPlayer` and call:
  ```csharp
  await dialogueEventPlayer.Play(someEventAsset);
  dialogueEventPlayer.TryPlay();
  dialogueEventPlayer.PlayAndQueue(ambientEventAsset);
  ```
- Use `DialogueServiceExtensions.PlayDialogueAsync(asset)` if you already have an `IDialogueService`.

## 5. Tutorial Utilities
- `TutorialDialogueStepUtility` exposes helpers so tutorial steps can play dialogue and wait for trigger zones without duplicating boilerplate:
  ```csharp
  await TutorialDialogueStepUtility.PlayDialogueWithTriggerAsync(
      context.DialogueService,
      introEvent,
      context.TriggerZones.Dequeue(),
      followUpEvent);
  ```
