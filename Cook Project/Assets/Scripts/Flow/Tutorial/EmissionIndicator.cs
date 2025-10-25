using UnityEngine;

public class EmissionIndicator : MonoBehaviour
{
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] [ColorUsage(true, true)] private Color noColor = new Color(0.749f, 0f, 0.024f); // Red
    [SerializeField] [ColorUsage(true, true)] private Color yesColor = new Color(0f, 0.749f, 0.024f); // Green

    public void SetIsOn(bool isOn)
    {
        Color finalColor = isOn ? yesColor : noColor;
        meshRenderer?.material?.SetVector("_EmissionColor", finalColor);
    }
}