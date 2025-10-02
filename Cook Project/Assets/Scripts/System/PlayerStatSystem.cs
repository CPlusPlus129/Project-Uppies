using R3;

public class PlayerStatSystem : SimpleSingleton<PlayerStatSystem>
{
    public ReactiveProperty<int> CurrentHP { get; private set; } = new ReactiveProperty<int>(100);
    public ReactiveProperty<int> MaxHP { get; private set; } = new ReactiveProperty<int>(100);
    public ReactiveProperty<float> CurrentStamina { get; private set; } = new ReactiveProperty<float>(100);
    public ReactiveProperty<float> MaxStamina { get; private set; } = new ReactiveProperty<float>(100);

    public ReactiveProperty<int> Money { get; private set; } = new ReactiveProperty<int>(200);

    public ReactiveProperty<float> StaminaRecoverySpeed { get; private set; } = new ReactiveProperty<float>(10f);
}