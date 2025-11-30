using Cysharp.Threading.Tasks;
using R3;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class TabPanelUI : MonoBehaviour, IUIInitializable
{
    [Header("UI Reference")]
    [SerializeField] private UIAnimationController uiAnim;
    [SerializeField] private TaskItem taskItemPrefab;
    [SerializeField] private OrderUI orderUI;
    private ReactiveProperty<bool> currentOpenState = new ReactiveProperty<bool>(false);
    private float tabButtonCooldown = 0.2f;
    private float tabButtonCooldownTimer = 0;
    private List<TaskItem> taskItemList = new List<TaskItem>();

    public async UniTask Init()
    {
        taskItemPrefab.gameObject.SetActive(false);
        SubscribeEvents();

        await UniTask.CompletedTask;
    }

    private void OnDestroy()
    {
        UnsubscribeEvents();
    }

    private void SubscribeEvents()
    {
        InputSystem.actions.FindAction("Tab").performed += OnTabButton;

        currentOpenState.Subscribe(isOpen =>
        {
            if (isOpen)
            {
                uiAnim.Open();
            }
            else
            {
                uiAnim.Close();
            }
        }).AddTo(this);

        Observable.EveryUpdate().Subscribe(_ =>
        {
            if(tabButtonCooldownTimer > 0)
            {
                tabButtonCooldownTimer -= Time.deltaTime;
            }
        }).AddTo(this);

            TaskManager.Instance?.Tasks.Subscribe(UpdateTaskList).AddTo(this);

    }

    private void UnsubscribeEvents()
    {
        InputSystem.actions.FindAction("Tab").performed -= OnTabButton;
    }

    private void OnTabButton(InputAction.CallbackContext cxt)
    {
        if(tabButtonCooldownTimer > 0)
        {
            return;
        }
        tabButtonCooldownTimer = tabButtonCooldown;
        currentOpenState.Value = !currentOpenState.Value;
    }

    private void UpdateTaskList(List<TaskManager.TaskData> taskList)
    {
        for(int i = taskItemList.Count - 1; i >= 0; i--)
    {
            var existingItem = taskItemList[i];
            bool taskExists = taskList.Any(task => task.Id == existingItem.TaskData.Id);

            if (!taskExists)
            {
                Destroy(existingItem.gameObject);
                taskItemList.RemoveAt(i);
            }
        }

        foreach (var task in taskList)
        {
            var existingItem = taskItemList.FirstOrDefault(item => item.TaskData.Id == task.Id);

            if (existingItem == null)
            {
                // Item dont exist, instantiate
                var newItem = Instantiate(taskItemPrefab, taskItemPrefab.transform.parent);
                newItem.gameObject.SetActive(true);
                newItem.SetupUI(task);
                taskItemList.Add(newItem);
            }
            else
            {
                // Item exists, update UI
                existingItem.SetupUI(task);
            }
        }
    }
}