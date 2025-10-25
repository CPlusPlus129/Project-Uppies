using System.Collections.Generic;
using R3;
using UnityEngine;
using UnityEngine.UI;

public class LightAmmoList : MonoBehaviour
{
    [SerializeField] private GameObject ammoPrefab;
    [SerializeField] private Image reloadGauge;
    private List<GameObject> ammoList = new List<GameObject>();

    private void Awake()
    {
        ammoPrefab.SetActive(false);
        PlayerStatSystem.Instance.CurrentLight.Subscribe(_ => UpdateUI()).AddTo(this);
        PlayerStatSystem.Instance.CanUseWeapon.Subscribe(can => gameObject.SetActive(can)).AddTo(this);
    }

    private void UpdateUI()
    {
        var playerStatSystem = PlayerStatSystem.Instance;
        var currentLight = playerStatSystem.CurrentLight.Value;
        var cost = playerStatSystem.LightCostPerShot.Value;
        var ammoCount = cost == 0 ? 0 : (int)(currentLight / cost);
        var reloadGaugeFill = (cost == 0) ? 0f : (currentLight % cost) / cost;
        UpdateAmmoList(ammoCount);
        UpdateReloadGauge(reloadGaugeFill);
    }

    private void UpdateAmmoList(int targetCount)
    {
        if (targetCount > ammoList.Count)
        {
            for (int i = ammoList.Count; i < targetCount; i++)
            {
                var ammo = Instantiate(ammoPrefab, ammoPrefab.transform.parent);
                ammo.SetActive(true);
                ammoList.Add(ammo);
            }
        }
        else if (targetCount < ammoList.Count)
        {
            var diff = ammoList.Count - targetCount;
            for (int i = 0; i < diff; i++)
            {
                var ammo = ammoList[ammoList.Count - 1];
                ammoList.RemoveAt(ammoList.Count - 1);
                Destroy(ammo);
            }
        }
    }

    private void UpdateReloadGauge(float fill)
    {
        reloadGauge.fillAmount = fill;
    }
}