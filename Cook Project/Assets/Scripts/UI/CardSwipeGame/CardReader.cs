using UnityEngine;
using UnityEngine.UI;
using R3;

public class CardReader : MonoBehaviour
{
    [SerializeField] private Image statusLight;
    [SerializeField] private Image readerSlot;

    [Header("Status Colors")]
    [SerializeField] private Color readyColor = Color.yellow;
    [SerializeField] private Color successColor = Color.green;
    [SerializeField] private Color failColor = Color.red;

    private void Awake()
    {
        SetStatus(CardReaderStatus.Ready);
    }

    public void SetStatus(CardReaderStatus status)
    {
        switch (status)
        {
            case CardReaderStatus.Ready:
                statusLight.color = readyColor;
                break;
            case CardReaderStatus.Success:
                statusLight.color = successColor;
                break;
            case CardReaderStatus.Fail:
                statusLight.color = failColor;
                break;
        }
    }

    public void PlaySuccessAnimation()
    {
        SetStatus(CardReaderStatus.Success);
        statusLight.transform.localScale = Vector3.one * 1.2f;

        Observable.Timer(System.TimeSpan.FromSeconds(0.1f))
            .Subscribe(_ => statusLight.transform.localScale = Vector3.one)
            .AddTo(this);
    }

    public void PlayFailAnimation()
    {
        SetStatus(CardReaderStatus.Fail);

        var originalPos = statusLight.transform.localPosition;
        statusLight.transform.localPosition = originalPos + Vector3.right * 5f;

        Observable.Timer(System.TimeSpan.FromSeconds(0.1f))
            .Subscribe(_ => statusLight.transform.localPosition = originalPos - Vector3.right * 5f)
            .AddTo(this);

        Observable.Timer(System.TimeSpan.FromSeconds(0.2f))
            .Subscribe(_ => statusLight.transform.localPosition = originalPos)
            .AddTo(this);
    }
}

public enum CardReaderStatus
{
    Ready,
    Success,
    Fail
}