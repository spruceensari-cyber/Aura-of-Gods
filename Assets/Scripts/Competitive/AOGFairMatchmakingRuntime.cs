using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class AOGPlayerRatingProfile
{
    public string PlayerId;
    public int Elo = 1200;
    public float RatingDeviation = 120f;
    public int Wins;
    public int Losses;
    public int CurrentWinStreak;
    public int CurrentLossStreak;
    public string PreferredRole = "Fill";

    public int GamesPlayed => Wins + Losses;
    public float WinRate => GamesPlayed <= 0 ? 0.5f : Wins / (float)GamesPlayed;
}

[Serializable]
public class AOGQueueEntry
{
    public AOGPlayerRatingProfile Profile;
    public float QueueSeconds;
}

public class AOGMatchCandidate
{
    public readonly List<AOGPlayerRatingProfile> Blue = new();
    public readonly List<AOGPlayerRatingProfile> Red = new();
    public float EloDifference;
    public float Quality;
}

/// <summary>
/// Fair Elo-first matchmaking. No forced-loss pools, streak penalties, hidden resets or bad-team assignment.
/// Match quality is determined by rating balance, uncertainty and role fit only.
/// </summary>
public class AOGFairMatchmakingRuntime : MonoBehaviour
{
    public static AOGFairMatchmakingRuntime Instance { get; private set; }

    [SerializeField] private int teamSize = 5;
    [SerializeField] private int baseEloWindow = 90;
    [SerializeField] private int maxEloWindow = 320;
    [SerializeField] private float expansionPerSecond = 1.4f;
    [SerializeField] private int eloKFactor = 28;

    private readonly List<AOGQueueEntry> queue = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindObjectOfType<AOGFairMatchmakingRuntime>() != null) return;
        new GameObject("AOG_Fair_Matchmaking_Runtime").AddComponent<AOGFairMatchmakingRuntime>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        foreach (AOGQueueEntry entry in queue)
            entry.QueueSeconds += Time.unscaledDeltaTime;
    }

    public void Enqueue(AOGPlayerRatingProfile profile)
    {
        if (profile == null || string.IsNullOrWhiteSpace(profile.PlayerId)) return;
        if (queue.Exists(q => q.Profile.PlayerId == profile.PlayerId)) return;
        queue.Add(new AOGQueueEntry { Profile = profile, QueueSeconds = 0f });
    }

    public void Dequeue(string playerId)
    {
        queue.RemoveAll(q => q.Profile.PlayerId == playerId);
    }

    public bool TryBuildMatch(out AOGMatchCandidate candidate)
    {
        candidate = null;
        if (queue.Count < teamSize * 2) return false;

        List<AOGQueueEntry> ordered = queue
            .OrderByDescending(q => q.QueueSeconds)
            .ThenBy(q => q.Profile.Elo)
            .Take(teamSize * 2)
            .ToList();

        int dynamicWindow = Mathf.Min(maxEloWindow, baseEloWindow + Mathf.RoundToInt(ordered.Max(q => q.QueueSeconds) * expansionPerSecond));
        int minElo = ordered.Min(q => q.Profile.Elo);
        int maxElo = ordered.Max(q => q.Profile.Elo);
        if (maxElo - minElo > dynamicWindow) return false;

        AOGMatchCandidate best = BuildBestSplit(ordered.Select(q => q.Profile).ToList());
        if (best == null) return false;

        foreach (AOGPlayerRatingProfile player in best.Blue.Concat(best.Red).ToList())
            Dequeue(player.PlayerId);

        candidate = best;
        return true;
    }

    private AOGMatchCandidate BuildBestSplit(List<AOGPlayerRatingProfile> players)
    {
        AOGMatchCandidate best = null;
        float bestScore = float.MaxValue;
        int n = players.Count;

        for (int mask = 0; mask < (1 << n); mask++)
        {
            if (CountBits(mask) != teamSize) continue;

            List<AOGPlayerRatingProfile> blue = new();
            List<AOGPlayerRatingProfile> red = new();
            for (int i = 0; i < n; i++)
            {
                if ((mask & (1 << i)) != 0) blue.Add(players[i]);
                else red.Add(players[i]);
            }

            float blueAvg = blue.Average(p => p.Elo);
            float redAvg = red.Average(p => p.Elo);
            float eloGap = Mathf.Abs(blueAvg - redAvg);
            float rolePenalty = RolePenalty(blue) + RolePenalty(red);
            float uncertaintyPenalty = Mathf.Abs(blue.Average(p => p.RatingDeviation) - red.Average(p => p.RatingDeviation)) * 0.08f;
            float score = eloGap + rolePenalty + uncertaintyPenalty;

            if (score < bestScore)
            {
                bestScore = score;
                best = new AOGMatchCandidate
                {
                    EloDifference = eloGap,
                    Quality = 1f / (1f + score / 100f)
                };
                best.Blue.AddRange(blue);
                best.Red.AddRange(red);
            }
        }

        return best;
    }

    private float RolePenalty(List<AOGPlayerRatingProfile> team)
    {
        string[] core = { "Top", "Jungle", "Mid", "Bot", "Support" };
        float penalty = 0f;
        foreach (string role in core)
        {
            bool covered = team.Exists(p => p.PreferredRole == role || p.PreferredRole == "Fill");
            if (!covered) penalty += 35f;
        }
        return penalty;
    }

    public void ApplyRankedResult(AOGPlayerRatingProfile winner, AOGPlayerRatingProfile loser)
    {
        if (winner == null || loser == null) return;

        float expectedWinner = 1f / (1f + Mathf.Pow(10f, (loser.Elo - winner.Elo) / 400f));
        float expectedLoser = 1f - expectedWinner;
        winner.Elo += Mathf.RoundToInt(eloKFactor * (1f - expectedWinner));
        loser.Elo += Mathf.RoundToInt(eloKFactor * (0f - expectedLoser));

        winner.Wins++;
        winner.CurrentWinStreak++;
        winner.CurrentLossStreak = 0;
        loser.Losses++;
        loser.CurrentLossStreak++;
        loser.CurrentWinStreak = 0;

        winner.RatingDeviation = Mathf.Max(45f, winner.RatingDeviation * 0.985f);
        loser.RatingDeviation = Mathf.Max(45f, loser.RatingDeviation * 0.985f);
    }

    private static int CountBits(int value)
    {
        int count = 0;
        while (value != 0)
        {
            count += value & 1;
            value >>= 1;
        }
        return count;
    }
}
