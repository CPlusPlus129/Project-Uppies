using Cysharp.Threading.Tasks;
using R3;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public class HUD : MonoBehaviour, IUIInitializable
{
    [SerializeField] private AimHintUI aimHint;
    
    public async UniTask Init()
    {
        PlayerStatSystem.Instance.CurrentInteractableTarget.Subscribe(aimHint.UpdateHint).AddTo(this);
        await UniTask.CompletedTask;
    }

    public void Open() =>gameObject.SetActive(true);
    public void Close() =>gameObject.SetActive(false);

    
}
