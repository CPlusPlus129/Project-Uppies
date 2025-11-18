using TMPro;
using UnityEngine;

public class HintItem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI keyText;
    [SerializeField] private TextMeshProUGUI hintText;

    public void SetHint(string key, string hint)
    {
        if (keyText != null)
        {
            keyText.text = key;
        }

        if (hintText != null)
        {
            hintText.text = hint;
        }
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
