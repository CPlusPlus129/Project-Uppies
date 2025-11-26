using UnityEngine;
using R3;

public class StaminaBarUI : MonoBehaviour
{
    [SerializeField] private BarItem staminaBar;

    private void Awake()
    {
        var playerStatSystem = PlayerStatSystem.Instance;
        playerStatSystem.CurrentStamina.Subscribe(stamina =>
        {
            gameObject.SetActive(stamina < playerStatSystem.MaxStamina.CurrentValue);
            staminaBar.UpdateValue(stamina, playerStatSystem.MaxStamina.CurrentValue);
        }).AddTo(this);
        playerStatSystem.MaxStamina.Subscribe(maxStamina =>
        {
            gameObject.SetActive(playerStatSystem.CurrentStamina.Value < maxStamina);
            staminaBar.UpdateValue(playerStatSystem.CurrentStamina.CurrentValue, maxStamina);
        }).AddTo(this);
    }

}