using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class AOGSmurfSignalProfile
{
    public string PlayerId;
    public int RankedGames;
    public float AverageRoleImpact;
    public float AverageStrengthAdjustedWin;
    public float AverageKDA;
    public float RatingAccelerationSignal;
    public bool HighConfidenceSmurfSignal;
}

/// <summary>
/// Detects likely skill/rating mismatch without punishing or forcing losses.
/// A positive signal may accelerate rating convergence; it must never place the player into intentionally bad teams.
/// </summary>
public class AOGSmurfSignalRuntime : MonoBehaviour
{
    public static AOGSmurfSignalRuntime Instance { get; private set; }
    readonly Dictionary<string, AOGSmurfSignalProfile> profiles = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        if (FindObjectOfType<AOGSmurfSignalRuntime>() != null) return;
        new GameObject("AOG_Smurf_Signal_Runtime").AddComponent<AOGSmurfSignalRuntime>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public AOGSmurfSignalProfile RegisterGame(
        string playerId,
        float roleImpact,
        float strengthAdjustedWinValue,
        float kda)
    {
        if (string.IsNullOrWhiteSpace(playerId)) return null;
        if (!profiles.TryGetValue(playerId, out AOGSmurfSignalProfile p))
        {
            p = new AOGSmurfSignalProfile { PlayerId = playerId };
            profiles[playerId] = p;
        }

        int previous = p.RankedGames;
        p.RankedGames++;
        p.AverageRoleImpact = RunningAverage(p.AverageRoleImpact, previous, Mathf.Clamp01(roleImpact));
        p.AverageStrengthAdjustedWin = RunningAverage(p.AverageStrengthAdjustedWin, previous, Mathf.Clamp01(strengthAdjustedWinValue));
        p.AverageKDA = RunningAverage(p.AverageKDA, previous, Mathf.Max(0f, kda));

        float impactSignal = Mathf.InverseLerp(0.62f, 0.90f, p.AverageRoleImpact);
        float winSignal = Mathf.InverseLerp(0.58f, 0.85f, p.AverageStrengthAdjustedWin);
        float kdaSignal = Mathf.InverseLerp(4.0f, 8.0f, p.AverageKDA);
        float sampleSignal = Mathf.Clamp01(p.RankedGames / 12f);

        p.RatingAccelerationSignal =
            impactSignal * 0.40f
            + winSignal * 0.40f
            + kdaSignal * 0.10f
            + sampleSignal * 0.10f;

        p.HighConfidenceSmurfSignal = p.RankedGames >= 6 && p.RatingAccelerationSignal >= 0.72f;
        return p;
    }

    public float GetRatingAccelerationMultiplier(string playerId)
    {
        if (!profiles.TryGetValue(playerId, out AOGSmurfSignalProfile p)) return 1f;
        if (!p.HighConfidenceSmurfSignal) return 1f;
        return Mathf.Lerp(1f, 1.75f, Mathf.InverseLerp(0.72f, 1f, p.RatingAccelerationSignal));
    }

    static float RunningAverage(float currentAverage, int previousCount, float next)
    {
        return ((currentAverage * previousCount) + next) / Mathf.Max(1, previousCount + 1);
    }
}
