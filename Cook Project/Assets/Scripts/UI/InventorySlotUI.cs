using System;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour
{
    public GameObject itemDisplay;
    public TMPro.TextMeshProUGUI item; // use text just for demo
    public Image itemImage;

    private CancellationTokenSource loadCts;
    public IAssetLoader assetLoader { get; set; }

    public async void SetItem(string itemName)
    {
        if (string.IsNullOrEmpty(itemName))
        {
            itemDisplay.SetActive(false);
            item.text = "";
            itemImage.sprite = null;
        }
        else
        {
            loadCts?.Cancel();
            loadCts = new CancellationTokenSource();
            var token = loadCts.Token;

            itemDisplay.SetActive(true);
            item.text = itemName;

            try
            {
                var sprite = await assetLoader.LoadAsync<Sprite>($"Res:/Images/ItemIcons/{itemName}.png");
                // dont update sprite if is already cancelled
                if (token.IsCancellationRequested) return;
                itemImage.sprite = sprite;
            }
            catch (OperationCanceledException)
            {
                // do nothing
            }
            catch (Exception e)
            {
                Debug.LogError($"Load failed: {e}");
            }
        }
    }
}