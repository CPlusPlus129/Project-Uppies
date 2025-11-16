using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Registers behaviours and GameObjects with the ShiftStateManager so they
/// automatically disable during AfterShift and re-enable during shifts.
/// </summary>
public class ShiftPhaseToggle : MonoBehaviour
{
    public enum Phase
    {
        Shift,
        AfterShift
    }

    [SerializeField] private Phase targetPhase = Phase.AfterShift;
    [SerializeField] private List<Behaviour> targetBehaviours = new();
    [SerializeField] private List<GameObject> targetObjects = new();

    private void Awake()
    {
        CacheDefaultTargets();
        RegisterAsync().Forget();
    }

    private void CacheDefaultTargets()
    {
        if (targetBehaviours.Count == 0)
        {
            if (TryGetComponent<WeaponSystem>(out var weaponSystem))
            {
                targetBehaviours.Add(weaponSystem);
            }

            if (TryGetComponent<Weapon>(out var weapon))
            {
                targetBehaviours.Add(weapon);
            }
        }
    }

    private async UniTaskVoid RegisterAsync()
    {
        await UniTask.WaitUntil(() => ShiftStateManager.Instance != null);

        var manager = ShiftStateManager.Instance;

        foreach (var behaviour in targetBehaviours)
        {
            if (targetPhase == Phase.AfterShift)
                manager.RegisterBehaviourDisableDuringAfterShift(behaviour);
            else
                manager.RegisterBehaviourDisableDuringShift(behaviour);
        }

        foreach (var obj in targetObjects)
        {
            if (targetPhase == Phase.AfterShift)
                manager.RegisterObjectDisableDuringAfterShift(obj);
            else
                manager.RegisterObjectDisableDuringShift(obj);
        }
    }
}
