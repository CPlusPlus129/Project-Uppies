using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UIRoot : MonoSingleton<UIRoot>
{
    protected override async void Awake()
    {
        base.Awake();
        await UniTask.WaitUntil(() => GameFlow.Instance.isInitialized);
        InitChildren();
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

    private void InitChildren()
    {
        var uiList = GetComponentsInChildren<IUIInitializable>(true);
        foreach (var item in uiList)
        {
            item.Init();
        }
    }
}