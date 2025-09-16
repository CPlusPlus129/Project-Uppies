using R3;
using UnityEngine;

// this is a temporary solution to activate CompleteUI
public class CompleteActivator : MonoBehaviour
{
    private void Awake()
    {
        OrderManager.Instance.OnOrderServed.Take(1).Subscribe(_ =>
        {
            UIRoot.Instance.GetUIComponent<CompleteUI>().Open();
        }).AddTo(this);
    }
}