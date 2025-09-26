using UnityEngine;
using R3;

public class PlayerStatUI : MonoBehaviour
{
    [SerializeField]
    private BarItem hpBar;
    [SerializeField]
    private BarItem staminaBar;

    private void Start()
    {
        var playerStatSystem = PlayerStatSystem.Instance;
        // HPの変更を監視してUIを更新
        playerStatSystem.CurrentHP.Subscribe(hp =>
        {
            hpBar.UpdateValue(hp, playerStatSystem.MaxHP.Value);
        }).AddTo(this);
        playerStatSystem.MaxHP.Subscribe(maxHp =>
        {
            hpBar.UpdateValue(playerStatSystem.CurrentHP.Value, maxHp);
        }).AddTo(this);
        // Staminaの変更を監視してUIを更新
        playerStatSystem.CurrentStamina.Subscribe(stamina =>
        {
            staminaBar.UpdateValue(stamina, playerStatSystem.MaxStamina.Value);
        }).AddTo(this);
        playerStatSystem.MaxStamina.Subscribe(maxStamina =>
        {
            staminaBar.UpdateValue(playerStatSystem.CurrentStamina.Value, maxStamina);
        }).AddTo(this);
    }
}