using System.Collections.Generic;
using UnityEngine;

public class UIRoot : MonoSingleton<UIRoot>
{
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
}