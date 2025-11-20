# Task List System Guide

The Task List System provides a way to display objectives to the player on the Shift Panel UI (and potentially other UIs). It supports adding, removing, and completing tasks, with persistence handling for repeatable shifts.

## Core Components

### 1. TaskManager (`TaskManager.cs`)
The central singleton that manages the state of all tasks.
-   **Active Tasks**: A list of currently visible tasks.
-   **History**: A `HashSet` of task IDs that have been completed. This is used to auto-complete tasks if they are re-added (e.g., when reloading a save or restarting a sequence), but can cause issues for repeatable tasks if not managed correctly.

**Key Methods:**
-   `AddTask(string id, string description)`: Adds a task. If the ID is in history, it is immediately marked as completed.
-   `CompleteTask(string id)`: Marks a task as completed (`[X]`) and adds it to history.
-   `RemoveTask(string id)`: Removes a task from the active list.
-   `ClearTasks()`: Clears all active tasks.
-   `ClearCompletedTasks()`: Removes all completed tasks from the active list (useful for cleaning up UI).
-   `RemoveFromHistory(string id)`: **Crucial for repeatable tasks.** Removes an ID from the completion history so it can be started fresh.

### 2. ShiftPanelUI (`ShiftPanelUI.cs`)
The UI component that visualizes the tasks.
-   It subscribes to `TaskManager.Tasks`.
-   It renders tasks in the "Subtitle" area of the Shift Panel.
-   Format: `[ ] Description` or `[X] Description`.

## Story Events integration

You can manipulate tasks using Story Events in the GameFlow.

### AddTaskEvent
Adds a task to the list.
-   **Task Id**: Unique identifier.
-   **Description**: Text shown to the player.

### RemoveTaskEvent
**Note:** Despite the name, this event currently calls `CompleteTask`, which marks the task as completed (`[X]`) but keeps it in the list until cleared.
-   **Target Event Id**: The ID of the task to complete.

## Best Practices & Common Pitfalls

### 1. Repeatable Tasks (The "History" Trap)
If you have a task that appears in *every* shift (e.g., "Quota", "Clock in"), you **MUST** clear its history before adding it again. Otherwise, `TaskManager` will remember it was completed in the previous shift and immediately mark it as `[X]`.

**How to fix:**
Call `TaskManager.Instance.RemoveFromHistory("YourTaskID")` before adding the task.
*See `ShiftSystem.StartShift` and `StartAfterShiftStateStoryEventAsset` for examples.*

### 2. Cleaning Up the UI
When a shift ends or a major state change occurs, you should clear completed tasks to keep the UI clean.
-   Use `TaskManager.Instance.ClearCompletedTasks()` to remove `[X]` items.
-   Use `TaskManager.Instance.RemoveTask("ID")` to remove specific items if they are no longer relevant (even if not completed).

### 3. Task IDs
-   Use consistent, unique string IDs for tasks (e.g., `QuotaTask`, `StorageRoom1Task`).
-   Avoid changing IDs for the same logical task, as this breaks history tracking (unless that's desired).

## Example Usage (Code)

```csharp
// Adding a one-off task
TaskManager.Instance.AddTask("FindKey", "Find the basement key.");

// Completing it
TaskManager.Instance.CompleteTask("FindKey");

// Adding a repeatable task (e.g., daily chore)
// 1. Forget previous completion
TaskManager.Instance.RemoveFromHistory("DailyChore");
// 2. Add fresh
TaskManager.Instance.AddTask("DailyChore", "Sweep the floor.");
```
