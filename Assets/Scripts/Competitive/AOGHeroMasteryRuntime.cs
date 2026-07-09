using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class AOGHeroPerformanceProfile
{
    public string HeroId;
    public int Games;
    public int Wins;
    public float AverageKDA;
    public float ObjectiveParticipation;

    public float WinRate => Games <= 0 ? 0.5f : Wins / (float)Games;
    public float SelectionPriorityScore => Games < 3
        ? WinRate * 40f
        : WinRate * 70f + Mathf.Clamp(AverageKDA / 5f, 0f, 1f) * 20f + Mathf.Clamp01(ObjectiveParticipation) * 10f;
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
        hero.Games++;
        if (won) hero.Wins++;
        hero.AverageKDA = ((hero.AverageKDA * previousGames) + Mathf.Max(0f, kda)) / hero.Games;
        hero.ObjectiveParticipation = ((hero.ObjectiveParticipation * previousGames) + Mathf.Clamp01(objectiveParticipation)) / hero.Games;
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
