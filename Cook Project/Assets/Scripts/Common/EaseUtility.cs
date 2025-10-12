using UnityEngine;

public enum EaseType
{
    Linear,
    InQuad,
    OutQuad,
    InOutQuad,
    InCubic,
    OutCubic,
    InOutCubic,
    InSine,
    OutSine,
    InOutSine,
    InBack,
    OutBack,
    InOutBack
}

public static class EaseUtility
{
    public static float Evaluate(EaseType easeType, float t)
    {
        t = Mathf.Clamp01(t);

        switch (easeType)
        {
            case EaseType.Linear:
                return t;

            case EaseType.InQuad:
                return t * t;

            case EaseType.OutQuad:
                return 1f - (1f - t) * (1f - t);

            case EaseType.InOutQuad:
                return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;

            case EaseType.InCubic:
                return t * t * t;

            case EaseType.OutCubic:
                return 1f - Mathf.Pow(1f - t, 3f);

            case EaseType.InOutCubic:
                return t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;

            case EaseType.InSine:
                return 1f - Mathf.Cos(t * Mathf.PI / 2f);

            case EaseType.OutSine:
                return Mathf.Sin(t * Mathf.PI / 2f);

            case EaseType.InOutSine:
                return -(Mathf.Cos(Mathf.PI * t) - 1f) / 2f;

            case EaseType.InBack:
                float c1 = 1.70158f;
                float c3 = c1 + 1f;
                return c3 * t * t * t - c1 * t * t;

            case EaseType.OutBack:
                float c1_out = 1.70158f;
                float c3_out = c1_out + 1f;
                return 1f + c3_out * Mathf.Pow(t - 1f, 3f) + c1_out * Mathf.Pow(t - 1f, 2f);

            case EaseType.InOutBack:
                float c1_inout = 1.70158f;
                float c2 = c1_inout * 1.525f;
                return t < 0.5f
                    ? (Mathf.Pow(2f * t, 2f) * ((c2 + 1f) * 2f * t - c2)) / 2f
                    : (Mathf.Pow(2f * t - 2f, 2f) * ((c2 + 1f) * (t * 2f - 2f) + c2) + 2f) / 2f;

            default:
                return t;
        }
    }
}