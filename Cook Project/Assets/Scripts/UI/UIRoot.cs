using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

public class UIRoot : MonoSingleton<UIRoot>
{
    protected override void Awake()
    {
        base.Awake();
        InitChildren().Forget();
    }

    public T GetUIComponent<T>() where T : Component
    {
        return GetComponentInChildren<T>(true);
    }

    public T[] GetUIComponents<T>() where T : Component
    {
        return GetComponentsInChildren<T>(true);
    }

    public void CloseAll()
    {
        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(false);
        }
    }

    public void SetVisible(bool isVisible)
    {
        var canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = isVisible ? 1 : 0;
        }
    }

    private async UniTaskVoid InitChildren()
    {
        await UniTask.WaitUntil(() => GameFlow.Instance.isInitialized);
        var uiList = GetComponentsInChildren<IUIInitializable>(true);
        var taskList = new List<UniTask>();
        foreach (var item in uiList)
        {
            taskList.Add(item.Init());
        }
        await UniTask.WhenAll(taskList);
    }
}