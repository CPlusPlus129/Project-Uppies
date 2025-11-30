using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TaskItem : MonoBehaviour
{
    [Header("UI Reference")]
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI taskDescription;
    [SerializeField] private GameObject completeIndicator;
    [Header("Icon")] //im just too lazy to deal with async load by addressables here
    [SerializeField] private Sprite undefinedIcon;
    [SerializeField] private Sprite onShiftIcon;
    [SerializeField] private Sprite offShiftIcon;

    public TaskManager.TaskData TaskData { get; private set; }

    public void SetupUI(TaskManager.TaskData task)
    {
        taskDescription.text = task.Description;
        completeIndicator.SetActive(task.IsCompleted);
        TaskData = task;

        //icon setup
        if (task.dueBeforeShiftEnds)
        {
            icon.sprite = onShiftIcon;
        }
        else if (task.dueBeforeShiftStarts)
        {
            icon.sprite = offShiftIcon;
        }
        else
        {
            icon.sprite = undefinedIcon;
        }
    }
}