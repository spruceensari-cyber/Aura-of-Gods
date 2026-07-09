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
    public float ThreatScore => Games < 5
        ? 0f
        : WinRate * 55f + Mathf.Clamp(AverageKDA / 5f, 0f, 1f) * 25f + Mathf.Clamp01(ObjectiveParticipation) * 20f;
}

[Serializable]
public class AOGPlayerHeroMasteryProfile
{
    public string PlayerId;
    public List<AOGHeroPerformanceProfile> Heroes = new();
}

/// <summary>
/// Stores hero-specific performance and generates opponent-aware ban recommendations.
/// Recommendations are advisory only; final bans remain explicit draft actions.
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

    public IReadOnlyList<string> RecommendBans(IEnumerable<string> enemyPlayerIds, int count = 3)
    {
        Dictionary<string, float> threatByHero = new();

        foreach (string playerId in enemyPlayerIds)
        {
            if (!profiles.TryGetValue(playerId, out AOGPlayerHeroMasteryProfile profile)) continue;
            foreach (AOGHeroPerformanceProfile hero in profile.Heroes)
            {
                if (!threatByHero.ContainsKey(hero.HeroId)) threatByHero[hero.HeroId] = 0f;
                threatByHero[hero.HeroId] += hero.ThreatScore;
            }
        }

        return threatByHero
            .OrderByDescending(pair => pair.Value)
            .ThenBy(pair => pair.Key)
            .Take(Mathf.Max(0, count))
            .Select(pair => pair.Key)
            .ToList();
    }
}
