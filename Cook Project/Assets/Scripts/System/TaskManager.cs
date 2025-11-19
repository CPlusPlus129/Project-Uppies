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
            data.IsCompleted = false;
            _activeTasks[index] = data;
        }
        else
        {
            _activeTasks.Add(new TaskData { Id = id, Description = description, IsCompleted = false });
        }
        
        UpdateTasksList();
    }

    public void RemoveTask(string id)
    {
        int removed = _activeTasks.RemoveAll(x => x.Id == id);
        if (removed > 0)
        {
            UpdateTasksList();
        }
    }

    public async void CompleteTask(string id, float delaySeconds)
    {
        var index = _activeTasks.FindIndex(x => x.Id == id);
        if (index != -1)
        {
            // Mark as completed
            var data = _activeTasks[index];
            data.IsCompleted = true;
            _activeTasks[index] = data;
            UpdateTasksList();

            // Wait
            if (delaySeconds > 0)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(delaySeconds));
            }

            // Remove
            RemoveTask(id);
        }
    }

    public void ClearTasks()
    {
        _activeTasks.Clear();
        UpdateTasksList();
    }

    private void UpdateTasksList()
    {
        // Create a copy to trigger updates
        Tasks.Value = new List<TaskData>(_activeTasks);
    }
}
