using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class TransitionCanvas : MonoBehaviour, IUIInitializable
{
    [Header("Fade Settings")]
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private EaseType fadeInEase = EaseType.OutQuad;
    [SerializeField] private EaseType fadeOutEase = EaseType.InQuad;

    public async UniTask Init()
    {
        SetupCanvas();
        await UniTask.CompletedTask;
    }

    private void SetupCanvas()
    {
        if (fadeImage == null)
        {
            fadeImage = GetComponentInChildren<Image>();
        }

        if (fadeImage == null)
        {
            var imageGO = new GameObject("FadeImage");
            imageGO.transform.SetParent(transform);

            fadeImage = imageGO.AddComponent<Image>();
            fadeImage.color = Color.black;

            var rectTransform = fadeImage.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.sizeDelta = Vector2.zero;
            rectTransform.anchoredPosition = Vector2.zero;
        }

        SetAlpha(0f);
        gameObject.SetActive(false);
    }

    public async UniTask FadeOut(float duration = -1f, EaseType? easeType = null)
    {
        if (duration < 0) duration = fadeDuration;
        EaseType ease = easeType ?? fadeOutEase;

        gameObject.SetActive(true);
        await FadeToAlpha(1f, duration, ease);
    }

    public async UniTask FadeIn(float duration = -1f, EaseType? easeType = null)
    {
        if (duration < 0) duration = fadeDuration;
        EaseType ease = easeType ?? fadeInEase;

        await FadeToAlpha(0f, duration, ease);
        gameObject.SetActive(false);
    }

    private async UniTask FadeToAlpha(float targetAlpha, float duration, EaseType easeType)
    {
        if (fadeImage == null) return;

        float startAlpha = fadeImage.color.a;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.unscaledDeltaTime;
            float normalizedTime = elapsedTime / duration;

            // Apply ease curve
            float easedTime = EaseUtility.Evaluate(easeType, normalizedTime);
            float currentAlpha = Mathf.Lerp(startAlpha, targetAlpha, easedTime);

            SetAlpha(currentAlpha);
            await UniTask.Yield();
        }

        SetAlpha(targetAlpha);
    }

    private void SetAlpha(float alpha)
    {
        if (fadeImage != null)
        {
            var color = fadeImage.color;
            color.a = alpha;
            fadeImage.color = color;
        }
    }

    public async UniTask FullFadeTransition(float fadeOutDuration = -1f, float fadeInDuration = -1f,
        EaseType? fadeOutEase = null, EaseType? fadeInEase = null)
    {
        await FadeOut(fadeOutDuration, fadeOutEase);
        await FadeIn(fadeInDuration, fadeInEase);
    }
}