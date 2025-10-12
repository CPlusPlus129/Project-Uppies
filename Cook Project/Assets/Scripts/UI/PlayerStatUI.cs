using UnityEngine;
using R3;
using TMPro;

public class PlayerStatUI : MonoBehaviour
{
    [SerializeField]
    private HPBarItem hpBar;
    [SerializeField]
    private BarItem staminaBar;
    [SerializeField]
    private LightBarItem lightBar;
    [SerializeField]
    private TextMeshProUGUI moneyValueText;

    private void Start()
    {
        var playerStatSystem = PlayerStatSystem.Instance;

        // hp and light bar register their own events

        // Staminaの変更を監視してUIを更新
        playerStatSystem.CurrentStamina.Subscribe(stamina =>
        {
            staminaBar.UpdateValue(stamina, playerStatSystem.MaxStamina.Value);
        }).AddTo(this);
        playerStatSystem.MaxStamina.Subscribe(maxStamina =>
        {
            staminaBar.UpdateValue(playerStatSystem.CurrentStamina.Value, maxStamina);
        }).AddTo(this);

        // Moneyの変更を監視してUIを更新
        playerStatSystem.Money.Subscribe(money =>
        {
            moneyValueText.text = money.ToString();
        }).AddTo(this);
    }
}
