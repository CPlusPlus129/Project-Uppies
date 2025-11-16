using System.Globalization;
using NvJ.Rendering;
using TMPro;
using UnityEngine;

/// <summary>
/// Simple world-space popup that celebrates money rewards using the same billboard
/// system as mobs so both the icon sprite and the numeric text always face the camera.
/// </summary>
[RequireComponent(typeof(BillboardSprite))]
public class MoneyPopupEffect : MonoBehaviour
{
    [System.Serializable]
    public struct PopupStyle
    {
        public Sprite BackgroundSprite;
        public Vector2 SpriteSize;
        public Color SpriteTint;
        public Color TextColor;
        public float Lifetime;
        public float RiseDistance;
        public float StartScale;
        public float EndScale;
        public float FontSize;
        public bool YAxisOnly;
        public AnimationCurve ScaleCurve;
        public AnimationCurve AlphaCurve;

        public static PopupStyle FromSettings(Mob.MoneyRewardSettings settings)
        {
            PopupStyle style = Default();
            if (settings == null)
            {
                return style;
            }

            style.BackgroundSprite = settings.popupSprite;
            style.SpriteSize = settings.popupSpriteSize;
            style.SpriteTint = settings.popupSpriteTint;
            style.TextColor = settings.popupTextColor;
            style.Lifetime = Mathf.Max(0.05f, settings.popupLifetime);
            style.RiseDistance = Mathf.Max(0f, settings.popupRiseDistance);
            style.StartScale = Mathf.Max(0.01f, settings.popupStartScale);
            style.EndScale = Mathf.Max(style.StartScale, settings.popupEndScale);
            style.FontSize = Mathf.Max(0.1f, settings.popupFontSize);
            style.YAxisOnly = settings.popupYAxisOnly;
            style.ScaleCurve = settings.popupScaleCurve;
            style.AlphaCurve = settings.popupAlphaCurve;
            return style;
        }

        public static PopupStyle Default()
        {
            return new PopupStyle
            {
                BackgroundSprite = null,
                SpriteSize = new Vector2(1.4f, 0.6f),
                SpriteTint = new Color(1f, 0.95f, 0.75f, 0.9f),
                TextColor = Color.white,
                Lifetime = 1.1f,
                RiseDistance = 1.1f,
                StartScale = 0.6f,
                EndScale = 1.05f,
                FontSize = 2.4f,
                YAxisOnly = true,
                ScaleCurve = AnimationCurve.EaseInOut(0f, 0.3f, 1f, 1f),
                AlphaCurve = new AnimationCurve(
                    new Keyframe(0f, 0f),
                    new Keyframe(0.1f, 1f),
                    new Keyframe(0.85f, 1f),
                    new Keyframe(1f, 0f))
            };
        }
    }

    private PopupStyle style;
    private int payout;
    private BillboardSprite billboardSprite;
    private TextMeshPro amountText;
    private Vector3 startPosition;
    private float elapsed;
    private Color baseSpriteTint;
    private Color baseTextColor;
    private bool initialized;
    private MeshRenderer billboardRenderer;
    private bool hasBackgroundSprite;

    public static MoneyPopupEffect Spawn(int payout, Vector3 position, PopupStyle style)
    {
        GameObject go = new GameObject("Money Popup");
        go.transform.position = position;
        MoneyPopupEffect effect = go.AddComponent<MoneyPopupEffect>();
        effect.Initialize(payout, style);
        return effect;
    }

    private void Awake()
    {
        billboardSprite = GetComponent<BillboardSprite>();
        billboardRenderer = GetComponent<MeshRenderer>();
        EnsureTextComponent();
    }

    private void EnsureTextComponent()
    {
        if (amountText != null)
        {
            return;
        }

        GameObject textObj = new GameObject("Amount Text");
        textObj.transform.SetParent(transform, false);
        amountText = textObj.AddComponent<TextMeshPro>();
        amountText.alignment = TextAlignmentOptions.Center;
        amountText.fontStyle = FontStyles.Bold;
        amountText.enableAutoSizing = false;
        amountText.fontSize = 2.4f;
        amountText.text = string.Empty;
        amountText.rectTransform.sizeDelta = new Vector2(3.2f, 1.1f);
        amountText.renderer.sortingOrder = 50;
    }

    public void Initialize(int amount, PopupStyle popupStyle)
    {
        payout = amount;
        style = popupStyle;
        if (style.ScaleCurve == null || style.ScaleCurve.length == 0)
        {
            style.ScaleCurve = PopupStyle.Default().ScaleCurve;
        }

        if (style.AlphaCurve == null || style.AlphaCurve.length == 0)
        {
            style.AlphaCurve = PopupStyle.Default().AlphaCurve;
        }

        ApplyStyle();
        initialized = true;
    }

    private void ApplyStyle()
    {
        startPosition = transform.position;
        elapsed = 0f;
        hasBackgroundSprite = style.BackgroundSprite != null;
        baseSpriteTint = hasBackgroundSprite ? style.SpriteTint : Color.clear;
        baseTextColor = style.TextColor;

        if (billboardSprite != null)
        {
            if (billboardRenderer == null)
            {
                billboardRenderer = billboardSprite.GetComponent<MeshRenderer>();
            }

            if (billboardRenderer != null)
            {
                billboardRenderer.enabled = hasBackgroundSprite;
                billboardRenderer.forceRenderingOff = !hasBackgroundSprite;
                if (!hasBackgroundSprite)
                {
                    billboardRenderer.sharedMaterial = null;
                }
            }

            if (hasBackgroundSprite)
            {
                billboardSprite.SetSprite(style.BackgroundSprite);
                billboardSprite.SetSize(style.SpriteSize);
                billboardSprite.SetTint(style.SpriteTint);
            }
            else
            {
                billboardSprite.SetSprite(null);
                billboardSprite.SetTint(Color.clear);
            }

            billboardSprite.SetBillboardMode(style.YAxisOnly
                ? BillboardSprite.BillboardMode.YAxisOnly
                : BillboardSprite.BillboardMode.Full);
        }

        if (amountText != null)
        {
            amountText.fontSize = style.FontSize;
            amountText.color = style.TextColor;
            amountText.text = string.Format(CultureInfo.InvariantCulture, "+${0:N0}", payout);
        }

        transform.localScale = Vector3.one * Mathf.Clamp(style.StartScale, 0.01f, 50f);
    }

    private void Update()
    {
        if (!initialized)
        {
            return;
        }

        float lifetime = Mathf.Max(0.05f, style.Lifetime);
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / lifetime);

        float scaleFactor = Mathf.Lerp(style.StartScale, style.EndScale, SafeEvaluate(style.ScaleCurve, t));
        transform.localScale = Vector3.one * scaleFactor;

        float riseT = Mathf.SmoothStep(0f, 1f, t);
        transform.position = startPosition + Vector3.up * style.RiseDistance * riseT;

        float alpha = Mathf.Clamp01(SafeEvaluate(style.AlphaCurve, t));
        ApplyAlpha(alpha);
        AlignToCamera();

        if (elapsed >= lifetime)
        {
            Destroy(gameObject);
        }
    }

    private static float SafeEvaluate(AnimationCurve curve, float t)
    {
        return curve != null && curve.length > 0 ? curve.Evaluate(t) : t;
    }

    private void ApplyAlpha(float alpha)
    {
        if (amountText != null)
        {
            Color textColor = baseTextColor;
            textColor.a *= alpha;
            amountText.color = textColor;
        }

        if (billboardSprite != null && hasBackgroundSprite)
        {
            Color tint = baseSpriteTint;
            tint.a *= alpha;
            billboardSprite.SetTint(tint);
        }
    }

    private void AlignToCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            return;
        }

        Vector3 forward = cam.transform.forward;
        if (style.YAxisOnly)
        {
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
            {
                return;
            }
            forward.Normalize();
        }

        transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
    }
}
