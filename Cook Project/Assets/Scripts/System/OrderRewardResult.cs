using UnityEngine;

public struct OrderRewardResult
{
    public Order Order;
    public Meal Meal;
    public float TotalReward;
    public int RoundedReward;
    public float CombinedScore;
    public float CompletionSeconds;
    public float TimeScore;
    public float QualityScore;
    public float TimeContribution;
    public float QualityContribution;
    public float TimeContributionRatio;
    public float QualityContributionRatio;
    public float TimeWeightNormalized;
    public float QualityWeightNormalized;
    public float QuickServeSeconds;
    public float SlowServeSeconds;
    public float MinReward;
    public float MaxReward;

    public int TotalRewardRounded => RoundedReward != 0 ? RoundedReward : Mathf.RoundToInt(TotalReward);
}
