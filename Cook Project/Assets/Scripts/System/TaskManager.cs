using System.Collections.Generic;
using R3;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System;

public class TaskManager : MonoSingleton<TaskManager>
{
    public struct TaskData
    {
        public string Id;
        public string Description;
        public bool IsCompleted;
    }

    private readonly List<TaskData> _activeTasks = new();
    private readonly HashSet<string> _completedTaskHistory = new();

    // Changed to expose TaskData instead of just strings
    public readonly ReactiveProperty<List<TaskData>> Tasks = new(new List<TaskData>());

    public void AddTask(string id, string description)
    {
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(description)) return;

        var index = _activeTasks.FindIndex(x => x.Id == id);
        if (index != -1)
        {
            var data = _activeTasks[index];
            data.Description = description;
            // Preserve completion status if already completed
            if (_completedTaskHistory.Contains(id))
            {
                data.IsCompleted = true;
            }
            else
            {
                data.IsCompleted = false;
            }
            _activeTasks[index] = data;
            Debug.Log($"[TaskManager] Updated existing task: {id}");
        }
        else
        {
            bool isAlreadyCompleted = _completedTaskHistory.Contains(id);
            _activeTasks.Add(new TaskData { Id = id, Description = description, IsCompleted = isAlreadyCompleted });
            Debug.Log($"[TaskManager] Added new task: {id} (Completed: {isAlreadyCompleted})");
        }

        UpdateTasksList();
    }

    public void RemoveTask(string id)
    {
        int removed = _activeTasks.RemoveAll(x => x.Id == id);
        if (removed > 0)
        {
            Debug.Log($"[TaskManager] Removed task: {id}");
            UpdateTasksList();
        }
        else
        {
            Debug.LogWarning($"[TaskManager] Failed to remove task: {id} (not found)");
        }
    }

    public void CompleteTask(string id)
    {
        // Always record completion in history
        if (_completedTaskHistory.Add(id))
        {
            Debug.Log($"[TaskManager] Recorded completion for task ID: {id}");
        }

        var index = _activeTasks.FindIndex(x => x.Id == id);
        if (index != -1)
        {
            // Mark as completed
            var data = _activeTasks[index];
            data.IsCompleted = true;
            _activeTasks[index] = data;
            Debug.Log($"[TaskManager] Marked active task as completed: {id}");
            UpdateTasksList();
        }
        else
        {
            Debug.Log($"[TaskManager] Task {id} completed but not currently active. It will be marked complete if added later.");
        }
    }

    public void ClearTasks()
    {
        _activeTasks.Clear();
        // Optional: Clear history too? Usually clearing tasks implies resetting state.
        _completedTaskHistory.Clear();
        UpdateTasksList();
    }

    public void ClearCompletedTasks()
    {
        _activeTasks.RemoveAll(x => x.IsCompleted);
        UpdateTasksList();
    }

    public void RemoveFromHistory(string id)
    {
        if (_completedTaskHistory.Remove(id))
        {
            Debug.Log($"[TaskManager] Removed task from history: {id}");
        }
    }

    public bool IsTaskCompleted(string id)
    {
        return _completedTaskHistory.Contains(id);
    }

    private void UpdateTasksList()
    {
        // Create a copy to trigger updates
        Tasks.Value = new List<TaskData>(_activeTasks);
    }
}