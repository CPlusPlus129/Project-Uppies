using TMPro;
using UnityEngine;
using R3;

public class WorldBroadcastUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI broadcastText;
    float remainTimer = 0f;

    private void Awake()
    {
        broadcastText.text = string.Empty;
        WorldBroadcastSystem.Instance.onBroadcast.Subscribe(tuple => BroadcastText(tuple.Item1, tuple.Item2)).AddTo(this);
    }

    private void Update()
    {
        if (remainTimer > 0f)
        {
            remainTimer -= Time.deltaTime;
            if (remainTimer <= 0f)
            {
                broadcastText.text = string.Empty;
            }
        }
    }

    public void BroadcastText(string content, float length)
    {
        broadcastText.text = content;
        remainTimer = length;
    }
}