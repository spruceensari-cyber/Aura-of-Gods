using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class AOGIntegrityProfile
{
    public string PlayerId;
    public int AfkCount;
    public int LeaveCount;
    public int DodgeCount;
    public int GoodStandingMatches;
    public float PenaltyScore;
    public float QueueLockMinutes;

    public bool IsRestricted => QueueLockMinutes > 0f || PenaltyScore >= 5f;
}

public class AOGPlayerIntegrityRuntime : MonoBehaviour
{
    public static AOGPlayerIntegrityRuntime Instance { get; private set; }
    readonly Dictionary<string, AOGIntegrityProfile> profiles = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        if (FindObjectOfType<AOGPlayerIntegrityRuntime>() != null) return;
        new GameObject("AOG_Player_Integrity_Runtime").AddComponent<AOGPlayerIntegrityRuntime>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        float minutes = Time.unscaledDeltaTime / 60f;
        foreach (AOGIntegrityProfile profile in profiles.Values)
            profile.QueueLockMinutes = Mathf.Max(0f, profile.QueueLockMinutes - minutes);
    }

    public AOGIntegrityProfile GetOrCreate(string playerId)
    {
        if (string.IsNullOrWhiteSpace(playerId)) return null;
        if (!profiles.TryGetValue(playerId, out AOGIntegrityProfile profile))
        {
            profile = new AOGIntegrityProfile { PlayerId = playerId };
            profiles[playerId] = profile;
        }
        return profile;
    }

    public void ReportAfk(string playerId, bool severe)
    {
        AOGIntegrityProfile p = GetOrCreate(playerId);
        if (p == null) return;
        p.AfkCount++;
        p.GoodStandingMatches = 0;
        p.PenaltyScore += severe ? 2.2f : 1.1f;
        p.QueueLockMinutes = Mathf.Max(p.QueueLockMinutes, severe ? 30f : 10f);
    }

    public void ReportLeave(string playerId)
    {
        AOGIntegrityProfile p = GetOrCreate(playerId);
        if (p == null) return;
        p.LeaveCount++;
        p.GoodStandingMatches = 0;
        p.PenaltyScore += 2.5f;
        p.QueueLockMinutes = Mathf.Max(p.QueueLockMinutes, Mathf.Min(120f, 15f + p.LeaveCount * 10f));
    }

    public void ReportDodge(string playerId)
    {
        AOGIntegrityProfile p = GetOrCreate(playerId);
        if (p == null) return;
        p.DodgeCount++;
        p.GoodStandingMatches = 0;
        p.PenaltyScore += 0.7f;
        p.QueueLockMinutes = Mathf.Max(p.QueueLockMinutes, Mathf.Min(30f, 3f + p.DodgeCount * 2f));
    }

    public void RecordGoodStandingMatch(string playerId)
    {
        AOGIntegrityProfile p = GetOrCreate(playerId);
        if (p == null) return;
        p.GoodStandingMatches++;
        if (p.GoodStandingMatches >= 3)
        {
            p.PenaltyScore = Mathf.Max(0f, p.PenaltyScore - 0.5f);
            p.GoodStandingMatches = 0;
        }
    }
}
