using R3;

public class PlayerStatSystem : SimpleSingleton<PlayerStatSystem>
{
    public ReactiveProperty<int> CurrentHP { get; private set; } = new ReactiveProperty<int>(100);
    public ReactiveProperty<int> MaxHP { get; private set; } = new ReactiveProperty<int>(100);
    public ReactiveProperty<float> CurrentStamina { get; private set; } = new ReactiveProperty<float>(100);
    public ReactiveProperty<float> MaxStamina { get; private set; } = new ReactiveProperty<float>(100);

    // Light System Properties
    public ReactiveProperty<float> CurrentLight { get; private set; } = new ReactiveProperty<float>(100f);
    public ReactiveProperty<float> MaxLight { get; private set; } = new ReactiveProperty<float>(100f);
    public ReactiveProperty<float> LightRecoverySpeed { get; private set; } = new ReactiveProperty<float>(5f);
    public ReactiveProperty<float> LightCostPerShot { get; private set; } = new ReactiveProperty<float>(40f);

    public ReactiveProperty<int> Money { get; private set; } = new ReactiveProperty<int>(0);

    public ReactiveProperty<float> StaminaRecoverySpeed { get; private set; } = new ReactiveProperty<float>(10f);

    public ReactiveProperty<bool> CanUseWeapon { get; private set; } = new ReactiveProperty<bool>(true);
    public ReactiveProperty<IInteractable> CurrentInteractableTarget { get; private set; } = new ReactiveProperty<IInteractable>(null);
}
