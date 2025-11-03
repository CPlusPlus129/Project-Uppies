GameFlow System Guide
=====================

Overview
--------
- **Component**: `GameFlow` (`Assets/Scripts/System/GameFlow.cs`)
- **Responsibilities**: Boots core services, manages a queue of story events, coordinates story progression, and exposes pause/signal hooks for other systems.
- **Runtime Entry**: Auto-starts on Awake when the singleton instance is created. Optional `startingSequence` can be enqueued automatically.
- **Primary Assets**: Story sequences (`StorySequenceAsset`) and story events (`StoryEventAsset` subclasses) under `Assets/Scripts/System/Story/`.

Core Concepts
-------------
- **Story Queue**: `GameFlow` maintains a `LinkedList<StoryEventRuntime>`. Sequences and individual events enqueue work, and the manager processes each item in order.
- **StorySequenceAsset**: ScriptableObject (`StorySequenceAsset.cs`) that lists `StoryEventAsset` references to execute in order. Use **Create ▸ Game Flow ▸ Story Sequence** to author new ones.
- **StoryEventAsset**: Abstract base class for event assets. Provides `CanExecuteAsync` for preconditions and `ExecuteAsync` for the actual action. Mark `replayable` to allow repeat runs.
- **StoryEventRuntime**: Lightweight wrapper that tracks the event asset, sequence origin, queue index, and current state (`StoryEventState`).
- **StoryEventResult**: Immutable struct returned by events to indicate completion, skipping, failure, or to enqueue follow-up sequences and pause the flow.
- **GameFlowContext**: Runtime context passed to each event. Provides access to `GameFlow`, the current runtime entry, service locator helpers, and signal utilities.
- **UnityEventStoryEvent**: Example asset that fires serialized `UnityEvent`s, optionally waits for a signal or delay. Great for designer-owned beats without code.

Lifecycle
---------
1. **Boot**: On Awake, `GameFlow` initializes services, loads tables, runs any custom setup, enqueues the optional `startingSequence`, and marks `IsInitialized` true.
2. **Processing Loop**: `RunStoryLoopAsync` pulls events from the queue. If paused, the loop waits until `ResumeStoryFlow` is called.
3. **Pre-check**: `ShouldRunEventAsync` skips non-replayable events that already completed, then consults `CanExecuteAsync` for additional gating.
4. **Execution**: The event's `ExecuteAsync` runs. Handle cancellation via the provided `CancellationToken`.
5. **Result Handling**:
   - Completed/skipped/failed states update history (`StoryHistory`).
   - `StoryEventResult.NextSequence` is enqueued automatically.
   - `StoryEventResult.PauseFlow` temporarily halts processing until manually resumed.
6. **Signals**: Events and other systems can `await context.WaitForSignalAsync(id)` and later call `GameFlow.Signal(id)` to resume.
7. **Teardown**: On destroy, the loop cancels, signal waiters are released, and all subjects complete.

Authoring Sequences
-------------------
1. **Create Events**: Right-click in the Project window → **Create ▸ Game Flow ▸ Story Events ▸ Unity Event** (or your custom subclass). Configure inspector fields and ensure unique `EventId` values.
2. **Create Sequence**: Right-click → **Create ▸ Game Flow ▸ Story Sequence**. Add events in execution order. Use meaningful `sequenceId` strings for debugging.
3. **Assign to GameFlow**: In the persistent singleton GameObject, drag the sequence into `startingSequence` or call `GameFlow.Instance.EnqueueSequence` at runtime.
4. **Test**: Enter Play Mode, verify the expected event order, and watch the Console for optional logs when `logStoryFlow` is enabled.

Custom Story Events
-------------------
- Derive from `StoryEventAsset` and override `ExecuteAsync` (and optionally `CanExecuteAsync`).
- Use `context.GetServiceAsync<T>()` to pull dependencies via `ServiceLocator`.
- Use `context.WaitForSignalAsync` for async coordination (e.g., wait for the player to finish a dialogue or interactable).
- Return `StoryEventResult.Completed(...)` for success, `StoryEventResult.Pause(...)` if you need to halt the queue until a later signal, or supply a `NextSequence` to branch the narrative.
- Mark `replayable` false for one-off beats. `GameFlow` automatically skips such events once they've completed successfully.
- Need a turnkey dialogue beat? Use `DialogueStoryEventAsset` (**Create ▸ Game Flow ▸ Story Events ▸ Dialogue Event**) to trigger one or more `DialogueEventAsset` instances through `IDialogueService`. It respects each asset’s `PlayOnce` flag, can broadcast optional hint text through `WorldBroadcastSystem`, and runs the queue only after the dialogue finishes.

Signals & Pausing
-----------------
- Call `GameFlow.Signal("YourSignalId")` from any script to release listeners waiting on that id.
- `WaitForSignalAsync` coalesces concurrent waiters; each signal fulfills the outstanding task and removes the entry.
- Use `GameFlow.PauseStoryFlow(reason)` to halt automatic processing (useful for cutscenes); call `ResumeStoryFlow()` when safe to continue.

Debugging & Observability
-------------------------
- Toggle `logStoryFlow` on the `GameFlow` component to stream queue operations to the Console.
- Subscribe to `OnStoryEventQueued`, `OnStoryEventStarted`, `OnStoryEventFinished`, or `OnStoryEventFailed` (R3 `Subject<T>`) for custom HUDs or analytics.
- Inspect `GameFlow.CurrentStoryEvent`, `GameFlow.GetPendingEventsSnapshot()`, or `GameFlow.StoryHistory` from the debugger to understand current state.

Best Practices
--------------
- Keep event IDs unique and descriptive (`intro_start_dialogue`, `minigame_shrink_begin`). They double as signal defaults and history keys.
- For designer-authored sequences, document expected prerequisites in the `designerNotes` field.
- Prefer short, composable events over multifunction monoliths; it simplifies branching and reuse.
- When gating by world state, implement `CanExecuteAsync` so events gracefully skip instead of throwing errors.
- Always handle cancellation in long-running tasks (e.g., wrap loops with `cancellationToken.ThrowIfCancellationRequested()`).

Extending the System
--------------------
- **Branching**: Return different `StoryEventResult.NextSequence` assets based on runtime conditions to branch storylines.
- **Dynamic Enqueue**: Systems can call `GameFlow.EnqueueSequence` or `EnqueueEvent` with `insertAtFront: true` for high-priority beats.
- **Interoperability**: Use `StoryEventResult.PauseFlow` to hand control to other managers (dialogue, minigames) and emit a signal when they finish.
- **Testing**: Create Edit Mode tests around custom event logic by instantiating the asset, constructing a `GameFlowContext` stub, and invoking `ExecuteAsync` with a mocked cancellation token.

Troubleshooting
---------------
- **Events never run**: Confirm `GameFlow` reached `IsInitialized == true` and `autoPlayStartingSequence` (or manual enqueue) is configured.
- **Queue stalls**: Look for events returning `StoryEventResult.Pause(...)` or waiting on signals that are never fired.
- **Repeated events**: Set `replayable` false to skip once completed. Verify no other scripts re-enqueue the same asset.
- **Singleton duplicates**: `GameFlow` derives from `MonoSingleton`. Ensure only one instance exists in your bootstrap scenes.
