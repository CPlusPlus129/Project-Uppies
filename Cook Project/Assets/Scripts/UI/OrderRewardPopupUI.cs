using Cysharp.Threading.Tasks;
using R3;
using System;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class OrderRewardPopupUI : MonoBehaviour, IUIInitializable
{
    [Header("Core References")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform popupRoot;
    [SerializeField] private TextMeshProUGUI titleLabel;
    [SerializeField] private TextMeshProUGUI orderMetaLabel;
    [SerializeField] private TextMeshProUGUI totalRewardLabel;
    [SerializeField] private TextMeshProUGUI totalRewardRangeLabel;
    [SerializeField] private Image totalScoreRing;

    [Header("Breakdown")]
    [SerializeField] private RewardStatLine timeStatLine;
    [SerializeField] private RewardStatLine skillStatLine;
    [SerializeField] private float statRevealDelay = 0.1f;

    [Header("Animation")]
    [SerializeField] private AnimationCurve showCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private AnimationCurve hideCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private AnimationCurve countCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField] private float showDuration = 0.35f;
    [SerializeField] private float hideDuration = 0.35f;
    [SerializeField] private float holdDuration = 1.5f;
    [SerializeField] private float baseCountDuration = 0.45f;
    [SerializeField] private float perHundredCountDuration = 0.1f;
    [SerializeField] private float overshootScale = 1.08f;
    [SerializeField] private Vector2 showDrift = new Vector2(0f, 42f);

    [Header("FX (Optional)")]
    [SerializeField] private ParticleSystem[] celebrationBursts;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip introClip;
    [SerializeField] private AudioClip countingClip;
    [SerializeField] private AudioClip payoutClip;

    private IOrderManager orderManager;
    private readonly CompositeDisposable disposables = new();
    private CancellationTokenSource activeSequenceCts;
    private Vector3 defaultScale = Vector3.one;
    private Vector2 defaultAnchoredPosition;
    private bool hasPopupRect;

    [Serializable]
    private class RewardStatLine
    {
        public TextMeshProUGUI headerLabel;
        public TextMeshProUGUI valueLabel;
        public TextMeshProUGUI descriptorLabel;
        public Image fillImage;
        public RectTransform pulseTarget;
    }

    private void Awake()
    {
        CacheReferencesFromHierarchy();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        if (popupRoot != null)
        {
            defaultScale = popupRoot.localScale;
            defaultAnchoredPosition = popupRoot.anchoredPosition;
            hasPopupRect = true;
        }
        else
        {
            defaultScale = transform.localScale;
            defaultAnchoredPosition = Vector2.zero;
            hasPopupRect = false;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
        {
            CacheReferencesFromHierarchy();
        }
    }

    [ContextMenu("Sync Child References")]
    private void SyncChildReferences()
    {
        CacheReferencesFromHierarchy();
    }
#endif

    public async UniTask Init()
    {
        gameObject.SetActive(true);
        orderManager = await ServiceLocator.Instance.GetAsync<IOrderManager>();
        orderManager.OnOrderRewarded
            .Subscribe(result => ShowRewardSequence(result).Forget())
            .AddTo(disposables);
    }

    private void OnDestroy()
    {
        disposables.Dispose();
        CancelActiveSequence();
    }

    private void CancelActiveSequence()
    {
        if (activeSequenceCts == null)
        {
            return;
        }

        activeSequenceCts.Cancel();
        activeSequenceCts.Dispose();
        activeSequenceCts = null;
    }

    private async UniTaskVoid ShowRewardSequence(OrderRewardResult reward)
    {
        CancelActiveSequence();
        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
        activeSequenceCts = linkedCts;
        var token = linkedCts.Token;

        PrepareStaticContent(reward);
        await PlayShowAnimation(token);
        await AnimateRewardAsync(reward, token);
        await UniTask.Delay(TimeSpan.FromSeconds(holdDuration), cancellationToken: token);
        await PlayHideAnimation(token);
    }

    private void PrepareStaticContent(OrderRewardResult reward)
    {
        CacheReferencesFromHierarchy();

        if (titleLabel != null)
        {
            titleLabel.text = "Reward Delivered";
        }

        if (orderMetaLabel != null)
        {
            var customer = string.IsNullOrEmpty(reward.Order?.CustomerName) ? "Customer" : reward.Order.CustomerName;
            var meal = string.IsNullOrEmpty(reward.Order?.MealName) ? "Meal" : reward.Order.MealName;
            orderMetaLabel.text = $"{customer} • {meal}";
        }

        if (totalRewardLabel != null)
        {
            totalRewardLabel.text = "$0";
        }

        if (totalRewardRangeLabel != null)
        {
            totalRewardRangeLabel.text = $"${Mathf.RoundToInt(reward.MinReward)} - ${Mathf.RoundToInt(reward.MaxReward)} Possible";
        }

        if (totalScoreRing != null)
        {
            totalScoreRing.fillAmount = Mathf.Clamp01(reward.CombinedScore);
        }

        ConfigureStatLine(timeStatLine, "Time Bonus", reward.TimeContribution, BuildTimeDescriptor(reward));
        ConfigureStatLine(skillStatLine, "Skill Bonus", reward.QualityContribution, BuildSkillDescriptor(reward));
    }

    private void CacheReferencesFromHierarchy()
    {
        if (canvasGroup == null || canvasGroup.gameObject != gameObject)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }
        popupRoot ??= (transform.Find("PopupRoot") as RectTransform) ?? GetComponent<RectTransform>();
        if (popupRoot == null)
        {
            return;
        }

        defaultScale = popupRoot.localScale;
        defaultAnchoredPosition = popupRoot.anchoredPosition;
        hasPopupRect = popupRoot != null;

        titleLabel ??= GetChildComponent<TextMeshProUGUI>("Content/Title");
        orderMetaLabel ??= GetChildComponent<TextMeshProUGUI>("Content/OrderMeta");
        totalRewardLabel ??= GetChildComponent<TextMeshProUGUI>("Content/TotalRow/TotalTexts/TotalRewardLabel");
        totalRewardRangeLabel ??= GetChildComponent<TextMeshProUGUI>("Content/TotalRow/TotalTexts/RangeLabel");
        totalScoreRing ??= GetChildComponent<Image>("Content/TotalRow/ScoreMeter/Fill");

        EnsureStatLine(ref timeStatLine, "Content/StatStack/TimeStat");
        EnsureStatLine(ref skillStatLine, "Content/StatStack/SkillStat");

        if (sfxSource == null)
        {
            sfxSource = GetComponent<AudioSource>();
        }

        if (celebrationBursts == null || celebrationBursts.Length == 0)
        {
            var ps = transform.Find("CelebrationFX")?.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                celebrationBursts = new[] { ps };
            }
        }
    }

    private void EnsureStatLine(ref RewardStatLine statLine, string rootPath)
    {
        var rootTransform = FindPopupChild(rootPath) as RectTransform;
        if (rootTransform == null)
        {
            return;
        }

        statLine ??= new RewardStatLine();
        statLine.pulseTarget ??= rootTransform;
        statLine.headerLabel ??= rootTransform.Find("TextContainer/HeaderLabel")?.GetComponent<TextMeshProUGUI>();
        statLine.valueLabel ??= rootTransform.Find("TextContainer/ValueLabel")?.GetComponent<TextMeshProUGUI>();
        statLine.descriptorLabel ??= rootTransform.Find("TextContainer/DescriptorLabel")?.GetComponent<TextMeshProUGUI>();
        statLine.fillImage ??= rootTransform.Find("Fill")?.GetComponent<Image>();
    }

    private Transform FindPopupChild(string relativePath)
    {
        Transform root = popupRoot != null ? popupRoot : transform;
        return root != null ? root.Find(relativePath) : null;
    }

    private T GetChildComponent<T>(string relativePath) where T : Component
    {
        return FindPopupChild(relativePath)?.GetComponent<T>();
    }

    private void ConfigureStatLine(RewardStatLine statLine, string header, float amount, string descriptor)
    {
        if (statLine == null)
            return;

        if (statLine.headerLabel != null)
        {
            statLine.headerLabel.text = header;
        }

        if (statLine.valueLabel != null)
        {
            statLine.valueLabel.text = "+$0";
        }

        if (statLine.descriptorLabel != null)
        {
            statLine.descriptorLabel.text = descriptor;
        }

        if (statLine.fillImage != null)
        {
            statLine.fillImage.fillAmount = 0f;
        }
    }

    private async UniTask AnimateRewardAsync(OrderRewardResult reward, CancellationToken token)
    {
        var duration = Mathf.Clamp(baseCountDuration + (reward.TotalRewardRounded / 100f) * perHundredCountDuration, baseCountDuration, baseCountDuration + 1.5f);
        await AnimateCurrency(totalRewardLabel, reward.TotalRewardRounded, duration, token);
        await UniTask.Delay(TimeSpan.FromSeconds(statRevealDelay), cancellationToken: token);
        await AnimateStatLine(timeStatLine, reward.TimeContribution, reward.TimeContributionRatio, token);
        await UniTask.Delay(TimeSpan.FromSeconds(statRevealDelay), cancellationToken: token);
        await AnimateStatLine(skillStatLine, reward.QualityContribution, reward.QualityContributionRatio, token);
    }

    private async UniTask AnimateCurrency(TextMeshProUGUI label, int targetValue, float duration, CancellationToken token)
    {
        if (label == null)
        {
            return;
        }

        float elapsed = 0f;
        int startValue = 0;
        float tickTimer = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            tickTimer += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float curved = countCurve.Evaluate(t);
            int value = Mathf.RoundToInt(Mathf.Lerp(startValue, targetValue, curved));
            label.text = $"${value:N0}";

            if (sfxSource != null && countingClip != null && tickTimer >= 0.08f)
            {
                tickTimer = 0f;
                sfxSource.PlayOneShot(countingClip);
            }

            await UniTask.NextFrame(token);
        }

        label.text = $"${targetValue:N0}";
        if (sfxSource != null && payoutClip != null)
        {
            sfxSource.PlayOneShot(payoutClip);
        }

        if (celebrationBursts != null)
        {
            foreach (var burst in celebrationBursts)
            {
                burst?.Play();
            }
        }
    }

    private async UniTask AnimateStatLine(RewardStatLine statLine, float amount, float ratio, CancellationToken token)
    {
        if (statLine == null || statLine.valueLabel == null)
        {
            return;
        }

        float elapsed = 0f;
        float duration = 0.55f;
        var baseScale = statLine.pulseTarget != null ? statLine.pulseTarget.localScale : Vector3.one;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float curved = countCurve.Evaluate(t);
            int value = Mathf.RoundToInt(Mathf.Lerp(0f, amount, curved));
            statLine.valueLabel.text = $"+${value:N0}";

            if (statLine.fillImage != null)
            {
                statLine.fillImage.fillAmount = Mathf.Lerp(0f, ratio, curved);
            }

            if (statLine.pulseTarget != null)
            {
                float pulse = Mathf.Sin(t * Mathf.PI) * 0.08f;
                statLine.pulseTarget.localScale = baseScale * (1f + pulse);
            }

            await UniTask.NextFrame(token);
        }

        statLine.valueLabel.text = $"+${Mathf.RoundToInt(amount):N0}";
        if (statLine.fillImage != null)
        {
            statLine.fillImage.fillAmount = ratio;
        }

        if (statLine.pulseTarget != null)
        {
            statLine.pulseTarget.localScale = baseScale;
        }
    }

    private async UniTask PlayShowAnimation(CancellationToken token)
    {
        gameObject.SetActive(true);
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }

        if (sfxSource != null && introClip != null)
        {
            sfxSource.PlayOneShot(introClip);
        }

        float elapsed = 0f;
        Vector3 startScale = defaultScale * 0.85f;
        Vector3 targetScale = defaultScale * overshootScale;
        Vector2 startPos = defaultAnchoredPosition - showDrift;
        Vector2 endPos = defaultAnchoredPosition;

        while (elapsed < showDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / showDuration);
            float curved = showCurve.Evaluate(t);

            if (hasPopupRect && popupRoot != null)
            {
                popupRoot.localScale = Vector3.Lerp(startScale, targetScale, curved);
                popupRoot.anchoredPosition = Vector2.Lerp(startPos, endPos, curved);
            }
            else
            {
                transform.localScale = Vector3.Lerp(startScale, targetScale, curved);
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = curved;
            }

            await UniTask.NextFrame(token);
        }

        if (hasPopupRect && popupRoot != null)
        {
            popupRoot.localScale = defaultScale;
            popupRoot.anchoredPosition = defaultAnchoredPosition;
        }
        else
        {
            transform.localScale = defaultScale;
        }
    }

    private async UniTask PlayHideAnimation(CancellationToken token)
    {
        float elapsed = 0f;
        Vector3 startScale = hasPopupRect && popupRoot != null ? popupRoot.localScale : transform.localScale;
        Vector3 targetScale = startScale * 0.9f;

        while (elapsed < hideDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / hideDuration);
            float curved = hideCurve.Evaluate(t);

            if (hasPopupRect && popupRoot != null)
            {
                popupRoot.localScale = Vector3.Lerp(startScale, targetScale, curved);
            }
            else
            {
                transform.localScale = Vector3.Lerp(startScale, targetScale, curved);
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f - curved;
            }

            await UniTask.NextFrame(token);
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }
    }

    private string BuildTimeDescriptor(OrderRewardResult reward)
    {
        var seconds = Mathf.RoundToInt(reward.CompletionSeconds);
        if (seconds <= 0)
        {
            return "Instant service";
        }

        float clampedSeconds = Mathf.Clamp(seconds, reward.QuickServeSeconds, reward.SlowServeSeconds);
        float normalized = 1f - Mathf.InverseLerp(reward.QuickServeSeconds, reward.SlowServeSeconds, clampedSeconds);

        string tier = normalized switch
        {
            >= 0.85f => "Lightning Fast",
            >= 0.6f => "Swift",
            >= 0.35f => "On Time",
            _ => "Fashionably Late"
        };

        return $"{tier} • {seconds:0}s";
    }

    private string BuildSkillDescriptor(OrderRewardResult reward)
    {
        float score = Mathf.Clamp01(reward.QualityScore);
        string grade = score switch
        {
            >= 0.9f => "Masterpiece",
            >= 0.7f => "Delicious",
            >= 0.5f => "Solid",
            >= 0.3f => "Passable",
            _ => "Needs Work"
        };

        float quality = Mathf.RoundToInt(score * 100f);
        return $"{grade} • {quality}% quality";
    }
}
