using System.Collections.Generic;
using R3;
using UnityEngine;

public class LightAmmoList : MonoBehaviour
{
    [SerializeField] private GameObject ammoPrefab;
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
        if (ammoCount > ammoList.Count)
        {
            for (int i = ammoList.Count; i < ammoCount; i++)
            {
                var ammo = Instantiate(ammoPrefab, ammoPrefab.transform.parent);
                ammo.SetActive(true);
                ammoList.Add(ammo);
            }
        }
        else if(ammoCount < ammoList.Count)
        {
            var diff = ammoList.Count - ammoCount;
            for (int i = 0; i < diff; i++)
            {
                var ammo = ammoList[ammoList.Count - 1];
                ammoList.RemoveAt(ammoList.Count - 1);
                Destroy(ammo);
            }
        }
    }
}