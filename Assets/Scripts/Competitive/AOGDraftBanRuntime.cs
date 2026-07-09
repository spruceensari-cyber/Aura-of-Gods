using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum AOGDraftPhase { Idle, Claiming, Locked }

public class AOGHeroClaim
{
    public string PlayerId;
    public TeamType Team;
    public string HeroId;
    public float PriorityScore;
    public float HeroWinRate;
}

/// <summary>
/// Ban-free hero claim draft.
/// Every account may choose any hero. If multiple players in the same match claim the same hero,
/// the strongest proven hero performer receives that hero; speed of clicking is irrelevant.
/// </summary>
public class AOGDraftBanRuntime : MonoBehaviour
{
    public AOGDraftPhase Phase { get; private set; } = AOGDraftPhase.Idle;
    public float PhaseRemaining { get; private set; }
    public readonly Dictionary<string, string> LockedHeroByPlayer = new();

    [SerializeField] private float claimWindowSeconds = 35f;

    private readonly List<AOGHeroClaim> claims = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        if (FindObjectOfType<AOGDraftBanRuntime>() != null) return;
        new GameObject("AOG_Hero_Claim_Draft_Runtime").AddComponent<AOGDraftBanRuntime>();
    }

    void Awake() => DontDestroyOnLoad(gameObject);

    void Update()
    {
        if (Phase != AOGDraftPhase.Claiming) return;
        PhaseRemaining -= Time.unscaledDeltaTime;
        if (PhaseRemaining <= 0f)
            ResolveClaims();
    }

    public void StartDraft()
    {
        claims.Clear();
        LockedHeroByPlayer.Clear();
        Phase = AOGDraftPhase.Claiming;
        PhaseRemaining = claimWindowSeconds;
    }

    public bool SubmitClaim(string playerId, TeamType team, string heroId)
    {
        if (Phase != AOGDraftPhase.Claiming || string.IsNullOrWhiteSpace(playerId) || string.IsNullOrWhiteSpace(heroId))
            return false;

        claims.RemoveAll(c => c.PlayerId == playerId);
        float priority = AOGHeroMasteryRuntime.Instance != null
            ? AOGHeroMasteryRuntime.Instance.GetSelectionPriorityScore(playerId, heroId)
            : 0f;
        float winRate = AOGHeroMasteryRuntime.Instance != null
            ? AOGHeroMasteryRuntime.Instance.GetHeroWinRate(playerId, heroId)
            : 0.5f;

        claims.Add(new AOGHeroClaim
        {
            PlayerId = playerId,
            Team = team,
            HeroId = heroId,
            PriorityScore = priority,
            HeroWinRate = winRate
        });

        AOGReplayEventLogRuntime.Instance?.Record("HeroClaim", playerId, heroId, Vector3.zero);
        return true;
    }

    public void ResolveClaims()
    {
        if (Phase != AOGDraftPhase.Claiming) return;

        LockedHeroByPlayer.Clear();

        foreach (IGrouping<string, AOGHeroClaim> group in claims.GroupBy(c => c.HeroId))
        {
            AOGHeroClaim winner = group
                .OrderByDescending(c => c.PriorityScore)
                .ThenByDescending(c => c.HeroWinRate)
                .ThenBy(c => c.PlayerId)
                .First();

            LockedHeroByPlayer[winner.PlayerId] = winner.HeroId;
            AOGReplayEventLogRuntime.Instance?.Record("HeroClaimWon", winner.PlayerId, winner.HeroId, Vector3.zero);
        }

        Phase = AOGDraftPhase.Locked;
        PhaseRemaining = 0f;
    }

    public bool HasWonHero(string playerId, string heroId)
    {
        return LockedHeroByPlayer.TryGetValue(playerId, out string locked) && locked == heroId;
    }

    public IReadOnlyList<AOGHeroClaim> GetClaimsForHero(string heroId)
    {
        return claims.Where(c => c.HeroId == heroId)
            .OrderByDescending(c => c.PriorityScore)
            .ThenByDescending(c => c.HeroWinRate)
            .ToList();
    }
}
