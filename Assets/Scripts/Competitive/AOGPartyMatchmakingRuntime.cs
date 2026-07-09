using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class AOGPartyProfile
{
    public string PartyId;
    public List<AOGPlayerRatingProfile> Members = new();

    public float AverageElo => Members == null || Members.Count == 0
        ? 1200f
        : (float)Members.Average(m => m.Elo);

    public int Size => Members?.Count ?? 0;
}

public class AOGPartyMatchmakingRuntime : MonoBehaviour
{
    public static AOGPartyMatchmakingRuntime Instance { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        if (FindObjectOfType<AOGPartyMatchmakingRuntime>() != null) return;
        new GameObject("AOG_Party_Matchmaking_Runtime").AddComponent<AOGPartyMatchmakingRuntime>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public float CalculatePartyAdjustedElo(AOGPartyProfile party)
    {
        if (party == null || party.Size <= 1) return party?.AverageElo ?? 1200f;

        float coordinationPremium = party.Size switch
        {
            2 => 20f,
            3 => 45f,
            4 => 75f,
            _ => 100f
        };

        float spread = party.Members.Max(m => m.Elo) - party.Members.Min(m => m.Elo);
        float spreadPenalty = Mathf.Clamp(spread * 0.08f, 0f, 80f);
        return party.AverageElo + coordinationPremium + spreadPenalty;
    }

    public float PartyBalancePenalty(IEnumerable<AOGPartyProfile> blue, IEnumerable<AOGPartyProfile> red)
    {
        List<AOGPartyProfile> b = blue?.ToList() ?? new List<AOGPartyProfile>();
        List<AOGPartyProfile> r = red?.ToList() ?? new List<AOGPartyProfile>();

        int blueLargest = b.Count == 0 ? 1 : b.Max(p => p.Size);
        int redLargest = r.Count == 0 ? 1 : r.Max(p => p.Size);
        float sizeMismatch = Mathf.Abs(blueLargest - redLargest) * 50f;

        float blueCoordination = b.Sum(CalculatePartyAdjustedElo) - b.Sum(p => p.AverageElo);
        float redCoordination = r.Sum(CalculatePartyAdjustedElo) - r.Sum(p => p.AverageElo);
        return sizeMismatch + Mathf.Abs(blueCoordination - redCoordination);
    }
}
