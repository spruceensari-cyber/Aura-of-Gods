using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class AOGHeroPerformanceProfile
{
    public string HeroId;
    public int Games;
    public int Wins;
    public float ExpectedWins;
    public float AverageKDA;
    public float ObjectiveParticipation;
    public float RoleAdjustedPerformance;
    public List<float> RecentResults = new();

    public float WinRate => Games <= 0 ? 0.5f : Wins / (float)Games;
    public float SampleConfidence => Mathf.Clamp01(Games / 20f);
    public float RecentForm => RecentResults == null || RecentResults.Count == 0 ? 0.5f : Average(RecentResults);

    /// <summary>
    /// Strength-adjusted win rate: compares actual wins with expected wins from player/opponent Elo.
    /// 0.50 is neutral. Beating stronger opposition moves this above raw expectation.
    /// </summary>
    public float StrengthAdjustedWinRate
    {
        get
        {
            if (Games <= 0) return 0.5f;
            float overPerformance = (Wins - ExpectedWins) / Games;
            return Mathf.Clamp01(0.5f + overPerformance);
        }
    }

    /// <summary>
    /// Hero claim priority. Win outcomes remain dominant. Role impact, KDA, objective contribution,
    /// sample confidence and recent form are secondary signals. Recent form never alters matchmaking.
    /// </summary>
    public float SelectionPriorityScore
    {
        get
        {
            float adjustedWinScore = StrengthAdjustedWinRate * 55f;
            float rawWinScore = WinRate * 15f;
            float roleScore = Mathf.Clamp01(RoleAdjustedPerformance) * 12f;
            float kdaScore = Mathf.Clamp01(AverageKDA / 6f) * 8f;
            float objectiveScore = Mathf.Clamp01(ObjectiveParticipation) * 5f;
            float confidenceScore = SampleConfidence * 3f;
            float recentFormScore = Mathf.Clamp01(RecentForm) * 2f;
            return adjustedWinScore + rawWinScore + roleScore + kdaScore + objectiveScore + confidenceScore + recentFormScore;
        }
    }

    public void PushRecentResult(float value, int window = 20)
    {
        RecentResults ??= new List<float>();
        RecentResults.Add(Mathf.Clamp01(value));
        while (RecentResults.Count > Mathf.Max(1, window))
            RecentResults.RemoveAt(0);
    }

    private static float Average(List<float> values)
    {
        if (values == null || values.Count == 0) return 0.5f;
        float total = 0f;
        foreach (float value in values) total += value;
        return total / values.Count;
    }
}

[Serializable]
public class AOGPlayerHeroMasteryProfile
{
    public string PlayerId;
    public List<AOGHeroPerformanceProfile> Heroes = new();
}

/// <summary>
/// Stores hero-specific performance and exposes selection-priority scoring.
/// Hero ownership is global/open; when multiple players claim the same hero in one match,
/// the player with the stronger proven hero performance receives priority.
/// </summary>
public class AOGHeroMasteryRuntime : MonoBehaviour
{
    public static AOGHeroMasteryRuntime Instance { get; private set; }
    private readonly Dictionary<string, AOGPlayerHeroMasteryProfile> profiles = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindObjectOfType<AOGHeroMasteryRuntime>() != null) return;
        new GameObject("AOG_Hero_Mastery_Runtime").AddComponent<AOGHeroMasteryRuntime>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void RegisterMatch(string playerId, string heroId, bool won, float kda, float objectiveParticipation)
    {
        RegisterMatch(playerId, heroId, won, kda, objectiveParticipation, 1200, 1200, 0.5f);
    }

    public void RegisterMatch(
        string playerId,
        string heroId,
        bool won,
        float kda,
        float objectiveParticipation,
        int playerElo,
        int opponentAverageElo,
        float roleAdjustedPerformance)
    {
        if (string.IsNullOrWhiteSpace(playerId) || string.IsNullOrWhiteSpace(heroId)) return;

        AOGHeroPerformanceProfile hero = GetOrCreateHeroProfile(playerId, heroId);
        int previousGames = hero.Games;
        float expectedWin = 1f / (1f + Mathf.Pow(10f, (opponentAverageElo - playerElo) / 400f));

        hero.Games++;
        if (won) hero.Wins++;
        hero.ExpectedWins += expectedWin;
        hero.AverageKDA = ((hero.AverageKDA * previousGames) + Mathf.Max(0f, kda)) / hero.Games;
        hero.ObjectiveParticipation = ((hero.ObjectiveParticipation * previousGames) + Mathf.Clamp01(objectiveParticipation)) / hero.Games;
        hero.RoleAdjustedPerformance = ((hero.RoleAdjustedPerformance * previousGames) + Mathf.Clamp01(roleAdjustedPerformance)) / hero.Games;

        float recentComposite =
            (won ? 0.55f : 0f)
            + Mathf.Clamp01(roleAdjustedPerformance) * 0.25f
            + Mathf.Clamp01(kda / 6f) * 0.10f
            + Mathf.Clamp01(objectiveParticipation) * 0.10f;
        hero.PushRecentResult(recentComposite);
    }

    public void RegisterRoleMetrics(
        string playerId,
        string heroId,
        bool won,
        float kda,
        int playerElo,
        int opponentAverageElo,
        AOGRoleMatchMetrics metrics)
    {
        float roleImpact = AOGRoleImpactRuntime.Calculate(metrics);
        float objective = metrics != null ? Mathf.Clamp01(metrics.ObjectiveParticipation) : 0.5f;
        RegisterMatch(playerId, heroId, won, kda, objective, playerElo, opponentAverageElo, roleImpact);
    }

    public float GetSelectionPriorityScore(string playerId, string heroId)
    {
        if (string.IsNullOrWhiteSpace(playerId) || string.IsNullOrWhiteSpace(heroId)) return 0f;
        AOGHeroPerformanceProfile hero = TryGetHeroProfile(playerId, heroId);
        return hero != null ? hero.SelectionPriorityScore : 0f;
    }

    public float GetHeroWinRate(string playerId, string heroId)
    {
        if (string.IsNullOrWhiteSpace(playerId) || string.IsNullOrWhiteSpace(heroId)) return 0.5f;
        AOGHeroPerformanceProfile hero = TryGetHeroProfile(playerId, heroId);
        return hero != null ? hero.WinRate : 0.5f;
    }

    private AOGHeroPerformanceProfile GetOrCreateHeroProfile(string playerId, string heroId)
    {
        if (!profiles.TryGetValue(playerId, out AOGPlayerHeroMasteryProfile profile))
        {
            profile = new AOGPlayerHeroMasteryProfile { PlayerId = playerId };
            profiles[playerId] = profile;
        }

        AOGHeroPerformanceProfile hero = profile.Heroes.Find(h => h.HeroId == heroId);
        if (hero == null)
        {
            hero = new AOGHeroPerformanceProfile { HeroId = heroId };
            profile.Heroes.Add(hero);
        }
        return hero;
    }

    private AOGHeroPerformanceProfile TryGetHeroProfile(string playerId, string heroId)
    {
        if (!profiles.TryGetValue(playerId, out AOGPlayerHeroMasteryProfile profile)) return null;
        return profile.Heroes.Find(h => h.HeroId == heroId);
    }
}
