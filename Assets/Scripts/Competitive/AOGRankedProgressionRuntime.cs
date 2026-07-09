using System;
using System.Collections.Generic;
using UnityEngine;

public enum AOGRankTier
{
    Iron,
    Bronze,
    Silver,
    Gold,
    Platinum,
    Diamond,
    Master,
    Grandmaster,
    Apex
}

[Serializable]
public class AOGRankProfile
{
    public string PlayerId;
    public int Elo = 1200;
    public AOGRankTier Tier = AOGRankTier.Silver;
    public int Division = 3;
    public int PlacementGames;
    public int PlacementWins;
    public bool PlacementComplete;
    public int AutofillProtectionCharges = 2;
}

public class AOGRankedProgressionRuntime : MonoBehaviour
{
    public static AOGRankedProgressionRuntime Instance { get; private set; }
    readonly Dictionary<string, AOGRankProfile> profiles = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        if (FindObjectOfType<AOGRankedProgressionRuntime>() != null) return;
        new GameObject("AOG_Ranked_Progression_Runtime").AddComponent<AOGRankedProgressionRuntime>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public AOGRankProfile GetOrCreate(string playerId)
    {
        if (string.IsNullOrWhiteSpace(playerId)) return null;
        if (!profiles.TryGetValue(playerId, out AOGRankProfile profile))
        {
            profile = new AOGRankProfile { PlayerId = playerId };
            profiles[playerId] = profile;
        }
        return profile;
    }

    public void RecordRankedResult(string playerId, int newElo, bool won)
    {
        AOGRankProfile profile = GetOrCreate(playerId);
        if (profile == null) return;

        profile.Elo = Mathf.Max(0, newElo);
        if (!profile.PlacementComplete)
        {
            profile.PlacementGames++;
            if (won) profile.PlacementWins++;
            if (profile.PlacementGames >= 10)
                profile.PlacementComplete = true;
        }

        ApplyTier(profile);
    }

    public bool ConsumeAutofillProtection(string playerId)
    {
        AOGRankProfile profile = GetOrCreate(playerId);
        if (profile == null || profile.AutofillProtectionCharges <= 0) return false;
        profile.AutofillProtectionCharges--;
        return true;
    }

    public void GrantAutofillProtection(string playerId, int charges = 1)
    {
        AOGRankProfile profile = GetOrCreate(playerId);
        if (profile == null) return;
        profile.AutofillProtectionCharges = Mathf.Clamp(profile.AutofillProtectionCharges + Mathf.Max(0, charges), 0, 5);
    }

    void ApplyTier(AOGRankProfile p)
    {
        int elo = p.Elo;
        if (elo < 700) p.Tier = AOGRankTier.Iron;
        else if (elo < 1000) p.Tier = AOGRankTier.Bronze;
        else if (elo < 1300) p.Tier = AOGRankTier.Silver;
        else if (elo < 1600) p.Tier = AOGRankTier.Gold;
        else if (elo < 1900) p.Tier = AOGRankTier.Platinum;
        else if (elo < 2200) p.Tier = AOGRankTier.Diamond;
        else if (elo < 2500) p.Tier = AOGRankTier.Master;
        else if (elo < 2800) p.Tier = AOGRankTier.Grandmaster;
        else p.Tier = AOGRankTier.Apex;

        p.Division = p.Tier >= AOGRankTier.Master ? 1 : 4 - Mathf.Clamp((elo / 75) % 4, 0, 3);
    }
}
