using Cysharp.Threading.Tasks;
using UnityEngine;

public class ShiftDepositBox : MonoBehaviour, IInteractable
{
    [SerializeField] private bool depositAll = true;
    [SerializeField] private int manualDepositAmount = 0;
    private IShiftSystem shiftSystem;

    private async void Awake()
    {
        await UniTask.WaitUntil(() => GameFlow.Instance.IsInitialized);
        shiftSystem = await ServiceLocator.Instance.GetAsync<IShiftSystem>();
    }

    public void Interact()
    {
        if (shiftSystem == null)
            return;

        if (depositAll)
        {
            var deposited = shiftSystem.DepositAllAvailableFunds();
            if (deposited <= 0)
            {
                WarnPlayer();
            }
            return;
        }

        var amount = DetermineDepositAmount();
        if (!shiftSystem.TryDeposit(amount))
        {
            WarnPlayer();
        }
    }

    private int DetermineDepositAmount()
    {
        if (manualDepositAmount > 0)
        {
            return manualDepositAmount;
        }

        var data = Database.Instance?.shiftData;
        if (data == null)
            return 1;

        return Mathf.Max(1, data.defaultDepositChunk);
    }

    private void WarnPlayer()
    {
        WorldBroadcastSystem.Instance.Broadcast("No cash to drop off or deposit is locked right now.", 4f);
    }
}
