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

    public float WinRate => Games <= 0 ? 0.5f : Wins / (float)Games;
    public float SampleConfidence => Mathf.Clamp01(Games / 20f);

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
    /// Hero claim priority. Win outcomes remain dominant, but opponent strength, role-normalized impact,
    /// KDA, objective play and sample confidence prevent simplistic stat padding from deciding claims.
    /// </summary>
    public float SelectionPriorityScore
    {
        get
        {
            float adjustedWinScore = StrengthAdjustedWinRate * 55f;
            float rawWinScore = WinRate * 15f;
            float roleScore = Mathf.Clamp01(RoleAdjustedPerformance) * 12f;
            float kdaScore = Mathf.Clamp01(AverageKDA / 6f) * 10f;
            float objectiveScore = Mathf.Clamp01(ObjectiveParticipation) * 5f;
            float confidenceScore = SampleConfidence * 3f;
            return adjustedWinScore + rawWinScore + roleScore + kdaScore + objectiveScore + confidenceScore;
        }
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

        int previousGames = hero.Games;
        float expectedWin = 1f / (1f + Mathf.Pow(10f, (opponentAverageElo - playerElo) / 400f));

        hero.Games++;
        if (won) hero.Wins++;
        hero.ExpectedWins += expectedWin;
        hero.AverageKDA = ((hero.AverageKDA * previousGames) + Mathf.Max(0f, kda)) / hero.Games;
        hero.ObjectiveParticipation = ((hero.ObjectiveParticipation * previousGames) + Mathf.Clamp01(objectiveParticipation)) / hero.Games;
        hero.RoleAdjustedPerformance = ((hero.RoleAdjustedPerformance * previousGames) + Mathf.Clamp01(roleAdjustedPerformance)) / hero.Games;
    }

    public float GetSelectionPriorityScore(string playerId, string heroId)
    {
        if (string.IsNullOrWhiteSpace(playerId) || string.IsNullOrWhiteSpace(heroId)) return 0f;
        if (!profiles.TryGetValue(playerId, out AOGPlayerHeroMasteryProfile profile)) return 0f;
        AOGHeroPerformanceProfile hero = profile.Heroes.Find(h => h.HeroId == heroId);
        return hero != null ? hero.SelectionPriorityScore : 0f;
    }

    public float GetHeroWinRate(string playerId, string heroId)
    {
        if (string.IsNullOrWhiteSpace(playerId) || string.IsNullOrWhiteSpace(heroId)) return 0.5f;
        if (!profiles.TryGetValue(playerId, out AOGPlayerHeroMasteryProfile profile)) return 0.5f;
        AOGHeroPerformanceProfile hero = profile.Heroes.Find(h => h.HeroId == heroId);
        return hero != null ? hero.WinRate : 0.5f;
    }
}
