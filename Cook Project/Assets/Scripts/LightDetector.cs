
using UnityEngine;

/// <summary>
/// Detects the light level at its position by reading from a dedicated 1x1 camera and RenderTexture.
/// The light level is exposed as a public float from 0 (dark) to 1 (bright).
/// </summary>
public class LightDetector : MonoBehaviour
{
    [Header("Detector Setup")]
    [SerializeField]
    [Tooltip("The 1x1 RenderTexture that the detector camera renders to.")]
    private RenderTexture _renderTexture;

    [Header("Light Level")]
    [Range(0f, 1f)]
    [Tooltip("The current detected light level, from 0 (dark) to 1 (bright).")]
    private float _lightLevel;

    /// <summary>
    /// The current detected light level, normalized from 0 (dark) to 1 (bright).
    /// </summary>
    public float LightLevel => _lightLevel;

    // A 1x1 texture to read the pixel data from the RenderTexture.
    private Texture2D _lightLevelTexture;
    private Rect _rect = new Rect(0, 0, 1, 1);

    private void Start()
    {
        if (_renderTexture == null)
        {
            Debug.LogError("RenderTexture is not set on the LightDetector. Please assign it in the Inspector.", this);
            enabled = false;
            return;
        }

        _lightLevelTexture = new Texture2D(1, 1, TextureFormat.RFloat, false);
    }

    private void OnDestroy()
    {
        if (_lightLevelTexture != null)
        {
            Destroy(_lightLevelTexture);
        }
    }

    // Using LateUpdate to ensure the reading happens after all rendering for the frame is complete.
    private void LateUpdate()
    {
        ReadLightLevel();
    }

    private void ReadLightLevel()
    {
        // Temporarily set the active RenderTexture to our detector's texture.
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = _renderTexture;

        // Read the single pixel from the active RenderTexture.
        _lightLevelTexture.ReadPixels(_rect, 0, 0);
        _lightLevelTexture.Apply();

        // Restore the previously active RenderTexture.
        RenderTexture.active = previous;

        // The color's grayscale value is a good approximation of brightness.
        Color pixelColor = _lightLevelTexture.GetPixel(0, 0);
        _lightLevel = pixelColor.grayscale;
    }
}
