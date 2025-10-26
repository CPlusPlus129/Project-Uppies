using Cysharp.Threading.Tasks;
using R3;
using System;
using System.Collections.Generic;
using UnityEngine;

//This purpose is too specific, will need to rename or refactor in the future
public class CompleteUI : MonoBehaviour, IUIInitializable
{
    public WorldPosFollowUI followPrefab;
    private IOrderManager orderManager;
    private List<WorldPosFollowUI> displayingFollowUIs = new List<WorldPosFollowUI>();
    private CompositeDisposable disposables = new CompositeDisposable();

    public async UniTask Init()
    {
        followPrefab.gameObject.SetActive(false);
        orderManager = await ServiceLocator.Instance.GetAsync<IOrderManager>();
        orderManager.OnOrderServed.Subscribe(order =>
        {
            // need to get customer transform from order somehow, this is inefficient
            var allCustomerArr = UnityEngine.Object.FindObjectsByType<Customer>(FindObjectsSortMode.None);
            foreach (var customer in allCustomerArr)
            {
                if (customer.customerName == order.CustomerName)
                {
                    var followUI = Instantiate(followPrefab, followPrefab.transform.parent);
                    followUI.target = customer.transform;
                    followUI.gameObject.SetActive(true);
                    displayingFollowUIs.Add(followUI);
                    Observable.Timer(TimeSpan.FromSeconds(2))
                    .Take(1)
                    .Subscribe(_ =>
                    {
                        displayingFollowUIs.Remove(followUI);
                        Destroy(followUI.gameObject);
                    }).AddTo(disposables);
                    break;
                }
            }
        }).AddTo(this);
    }

    private void OnDisable()
    {
        DestroyAllFollowUIs();
    }

    private void DestroyAllFollowUIs()
    {
        disposables.Clear();
        foreach (var ui in displayingFollowUIs)
        {
            Destroy(ui.gameObject);
        }
        displayingFollowUIs.Clear();
    }

    public void Open()
    {
        gameObject.SetActive(true);
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }
}