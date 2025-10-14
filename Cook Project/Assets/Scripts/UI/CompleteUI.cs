using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;

public class CompleteUI : MonoBehaviour //This purpose is too specific, will need to rename or refactor in the future
{
    public WorldPosFollowUI followPrefab;
    private IOrderManager orderManager;

    private async void Awake()
    {
        followPrefab.gameObject.SetActive(false);
        await UniTask.WaitUntil(() => GameFlow.Instance.isInitialized);
        orderManager = await ServiceLocator.Instance.GetAsync<IOrderManager>();
        orderManager.OnOrderServed.Subscribe(order =>
        {
            // need to get customer transform from order somehow, this is inefficient
            var allCustomerArr = Object.FindObjectsByType<Customer>(FindObjectsSortMode.None);
            foreach (var customer in allCustomerArr)
            {
                if (customer.customerName == order.CustomerName)
                {
                    var followUI = Instantiate(followPrefab, followPrefab.transform.parent);
                    followUI.target = customer.transform;
                    followUI.gameObject.SetActive(true);
                    Destroy(followUI.gameObject, 2f); // Destroy after 2 seconds
                    break;
                }
            }
        }).AddTo(this);
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