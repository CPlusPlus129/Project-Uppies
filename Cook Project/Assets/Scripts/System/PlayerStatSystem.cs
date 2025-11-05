using R3;
using UnityEngine;

public class PlayerStatSystem : SimpleSingleton<PlayerStatSystem>
{
    public PlayerStatSystem()
    {
        CurrentHP
            .Pairwise()
            .Subscribe(hpValues =>
            {
                OnHPChanged.OnNext((hpValues.Previous, hpValues.Current));
                if (hpValues.Current <= 0)
                {
                    OnPlayerDeath.OnNext(Unit.Default);
                }
            })
            .AddTo(disposables);

        CurrentSouls
            .Pairwise()
            .Subscribe(soulValues =>
            {
                OnSoulsChanged.OnNext((soulValues.Previous, soulValues.Current));
            })
            .AddTo(disposables);
    }

    public ReactiveProperty<int> CurrentHP { get; private set; } = new ReactiveProperty<int>(100);
    public ReactiveProperty<int> MaxHP { get; private set; } = new ReactiveProperty<int>(100);
    public Subject<(int oldValue, int newValue)> OnHPChanged = new Subject<(int, int)>();
    public Subject<Unit> OnPlayerDeath = new Subject<Unit>();
    public ReactiveProperty<float> CurrentStamina { get; private set; } = new ReactiveProperty<float>(100);
    public ReactiveProperty<float> MaxStamina { get; private set; } = new ReactiveProperty<float>(100);

    public ReactiveProperty<int> CurrentSouls { get; private set; } = new ReactiveProperty<int>(0);
    public ReactiveProperty<int> MaxSouls { get; private set; } = new ReactiveProperty<int>(100);
    public Subject<(int oldValue, int newValue)> OnSoulsChanged = new Subject<(int, int)>();

    // Light System Properties
    public ReactiveProperty<float> CurrentLight { get; private set; } = new ReactiveProperty<float>(100f);
    public ReactiveProperty<float> MaxLight { get; private set; } = new ReactiveProperty<float>(100f);
    public ReactiveProperty<float> LightRecoverySpeed { get; private set; } = new ReactiveProperty<float>(5f);
    public ReactiveProperty<float> LightCostPerShot { get; private set; } = new ReactiveProperty<float>(40f);

    public ReactiveProperty<int> Money { get; private set; } = new ReactiveProperty<int>(66);
    public ReactiveProperty<int> InventorySize { get; private set; } = new ReactiveProperty<int>(4);

    public ReactiveProperty<float> StaminaRecoverySpeed { get; private set; } = new ReactiveProperty<float>(10f);

    public ReactiveProperty<bool> CanUseWeapon { get; private set; } = new ReactiveProperty<bool>(true);
    public ReactiveProperty<IInteractable> CurrentInteractableTarget { get; private set; } = new ReactiveProperty<IInteractable>(null);

    private CompositeDisposable disposables = new CompositeDisposable();

    public void AddSouls(int amount)
    {
        if (amount == 0)
        {
            return;
        }

        int target = CurrentSouls.Value + amount;
        target = Mathf.Clamp(target, 0, MaxSouls.Value);
        CurrentSouls.Value = target;
    }

    public bool TrySpendSouls(int cost)
    {
        if (cost <= 0)
        {
            return true;
        }

        if (CurrentSouls.Value < cost)
        {
            return false;
        }

        CurrentSouls.Value -= cost;
        return true;
    }

    public void SetMaxSouls(int maxSouls, bool clampCurrent = true)
    {
        maxSouls = Mathf.Max(0, maxSouls);
        MaxSouls.Value = maxSouls;

        if (clampCurrent)
        {
            CurrentSouls.Value = Mathf.Min(CurrentSouls.Value, MaxSouls.Value);
        }
    }

    public void SetCurrentSouls(int amount)
    {
        amount = Mathf.Clamp(amount, 0, MaxSouls.Value);
        CurrentSouls.Value = amount;
    }
}
