using Cysharp.Threading.Tasks;
using R3;
using System.Threading.Tasks;
using UnityEngine;

public class PlayerStatSystem : MonoSingleton<PlayerStatSystem>
{
    public ReactiveProperty<int> CurrentHP { get; private set; } = new ReactiveProperty<int>(100);
    public ReactiveProperty<int> MaxHP { get; private set; } = new ReactiveProperty<int>(100);
    public Subject<(int oldValue, int newValue)> OnHPChanged { get; private set; } = new Subject<(int, int)>();
    public Subject<Unit> OnPlayerDeath { get; private set; } = new Subject<Unit>();

    public ReactiveProperty<float> CurrentStamina { get; private set; } = new ReactiveProperty<float>(100);
    public ReactiveProperty<float> MaxStamina { get; private set; } = new ReactiveProperty<float>(100);
    public ReactiveProperty<float> StaminaRecoverySpeed { get; private set; } = new ReactiveProperty<float>(10f);

    public ReactiveProperty<int> CurrentSouls { get; private set; } = new ReactiveProperty<int>(0);
    public ReactiveProperty<int> MaxSouls { get; private set; } = new ReactiveProperty<int>(100);
    public Subject<(int oldValue, int newValue)> OnSoulsChanged { get; private set; } = new Subject<(int, int)>();

    // Light System Properties
    public ReactiveProperty<float> CurrentLight { get; private set; } = new ReactiveProperty<float>(100f);
    public ReactiveProperty<float> MaxLight { get; private set; } = new ReactiveProperty<float>(100f);
    public ReactiveProperty<float> LightRecoverySpeed { get; private set; } = new ReactiveProperty<float>(5f);
    public ReactiveProperty<float> LightCostPerShot { get; private set; } = new ReactiveProperty<float>(40f);

    public ReactiveProperty<int> Money { get; private set; } = new ReactiveProperty<int>(66);
    public ReactiveProperty<int> InventorySize { get; private set; } = new ReactiveProperty<int>(4);


    public ReactiveProperty<bool> CanUseWeapon { get; private set; } = new ReactiveProperty<bool>(true);
    public ReactiveProperty<IInteractable> CurrentInteractableTarget { get; private set; } = new ReactiveProperty<IInteractable>(null);

    #region Unity gameobject lifecycle
    protected override async void Awake()
    {
        base.Awake();
        if (Instance != this) return;

        CurrentHP
            .Pairwise()
            .Subscribe(hpValues =>
            {
                OnHPChanged.OnNext((hpValues.Previous, hpValues.Current));
                if (hpValues.Current <= 0 && hpValues.Previous > 0)
                {
                    OnPlayerDeath.OnNext(Unit.Default);
                }
            })
            .AddTo(this);

        CurrentSouls
            .Pairwise()
            .Subscribe(soulValues =>
            {
                OnSoulsChanged.OnNext((soulValues.Previous, soulValues.Current));
            })
            .AddTo(this);

        await UniTask.WaitUntil(() => GameFlow.Instance.IsInitialized);
        var shiftSystem = await ServiceLocator.Instance.GetAsync<IShiftSystem>();
        shiftSystem.currentState.Subscribe(state =>
        {
            CanUseWeapon.Value = state is ShiftSystem.ShiftState.InShift or ShiftSystem.ShiftState.Overtime;
        }).AddTo(this);
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        if (Instance != this) return;
    }
    #endregion

    public void Resurrect()
    {
        CurrentHP.Value = MaxHP.CurrentValue;
    }

    public void Heal(int value, bool canOverheal = false)
    {
        if (canOverheal)
            CurrentHP.Value += value;
        else
            CurrentHP.Value = Mathf.Clamp(CurrentHP.CurrentValue + value, 0, MaxHP.CurrentValue);
    }

    public void Damage(int value)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (SROptions.Current.IsInvincible)
            return;
#endif
        CurrentHP.Value = Mathf.Max(0, CurrentHP.CurrentValue - value);
    }

    public void AddStamina(float value)
    {
        CurrentStamina.Value = Mathf.Clamp(CurrentStamina.CurrentValue + value, 0, MaxStamina.CurrentValue);
    }

    public void ConsumeStamina(float value)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (SROptions.Current.IsInfiniteStamina)
            return;
#endif
        CurrentStamina.Value = Mathf.Max(0, CurrentStamina.CurrentValue - value);
    }

    public void AddLight(float value)
    {
        CurrentLight.Value = Mathf.Clamp(CurrentLight.CurrentValue + value, 0, MaxLight.CurrentValue);
    }

    public void ConsumeLight(float value)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (SROptions.Current.IsInfiniteAmmo)
            return;
#endif
        CurrentLight.Value = Mathf.Max(0f, CurrentLight.CurrentValue - value);
    }

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
