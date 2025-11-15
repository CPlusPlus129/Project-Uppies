using UnityEngine;

public struct MinigamePerformance
{
    public float Accuracy;
    public int Mistakes;
    public float DurationSeconds;
    public float QualityScore;

    public static MinigamePerformance Default => new MinigamePerformance
    {
        Accuracy = 1f,
        Mistakes = 0,
        DurationSeconds = 0f,
        QualityScore = 1f
    };

    public MinigamePerformance(float accuracy, int mistakes, float durationSeconds, float qualityScore)
    {
        Accuracy = Mathf.Clamp01(accuracy);
        Mistakes = Mathf.Max(0, mistakes);
        DurationSeconds = Mathf.Max(0f, durationSeconds);
        QualityScore = Mathf.Clamp01(qualityScore);
    }
}
