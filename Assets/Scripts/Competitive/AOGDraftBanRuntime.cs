using System.Collections.Generic;
using UnityEngine;

public enum AOGDraftPhase { Idle, BlueBan, RedBan, BluePick, RedPick, Locked }

/// <summary>
/// Tournament draft flow with global hero uniqueness.
/// The same hero can never be picked by both teams or twice within one match.
/// </summary>
public class AOGDraftBanRuntime : MonoBehaviour
{
    public AOGDraftPhase Phase { get; private set; } = AOGDraftPhase.Idle;
    public float PhaseRemaining { get; private set; }
    public readonly List<string> BluePicks = new();
    public readonly List<string> RedPicks = new();
    public readonly List<string> BlueBans = new();
    public readonly List<string> RedBans = new();

    [SerializeField] private float phaseSeconds = 25f;
    [SerializeField] private int bansPerTeam = 3;
    [SerializeField] private int picksPerTeam = 5;

    public IEnumerable<string> AllBans
    {
        get
        {
            foreach (string hero in BlueBans) yield return hero;
            foreach (string hero in RedBans) yield return hero;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        if (FindObjectOfType<AOGDraftBanRuntime>() != null) return;
        new GameObject("AOG_Draft_Ban_Runtime").AddComponent<AOGDraftBanRuntime>();
    }

    void Awake() => DontDestroyOnLoad(gameObject);

    void Update()
    {
        if (Phase == AOGDraftPhase.Idle || Phase == AOGDraftPhase.Locked) return;
        PhaseRemaining -= Time.unscaledDeltaTime;
        if (PhaseRemaining <= 0f)
            PhaseRemaining = 0f; // explicit choice required; no silent random pick or ban
    }

    public void StartDraft()
    {
        BluePicks.Clear();
        RedPicks.Clear();
        BlueBans.Clear();
        RedBans.Clear();
        SetPhase(AOGDraftPhase.BlueBan);
    }

    public bool Submit(string heroId)
    {
        if (!IsHeroAvailable(heroId)) return false;

        switch (Phase)
        {
            case AOGDraftPhase.BlueBan:
                if (BlueBans.Count >= bansPerTeam) return false;
                BlueBans.Add(heroId);
                break;
            case AOGDraftPhase.RedBan:
                if (RedBans.Count >= bansPerTeam) return false;
                RedBans.Add(heroId);
                break;
            case AOGDraftPhase.BluePick:
                if (BluePicks.Count >= picksPerTeam) return false;
                BluePicks.Add(heroId);
                break;
            case AOGDraftPhase.RedPick:
                if (RedPicks.Count >= picksPerTeam) return false;
                RedPicks.Add(heroId);
                break;
            default:
                return false;
        }

        AOGReplayEventLogRuntime.Instance?.Record("DraftChoice", Phase.ToString(), heroId, Vector3.zero);
        Advance();
        return true;
    }

    public bool IsHeroAvailable(string heroId)
    {
        if (string.IsNullOrWhiteSpace(heroId)) return false;
        return !BlueBans.Contains(heroId)
            && !RedBans.Contains(heroId)
            && !BluePicks.Contains(heroId)
            && !RedPicks.Contains(heroId);
    }

    public IReadOnlyList<string> RecommendBansForBlue(IEnumerable<string> redPlayerIds, int count = 3)
    {
        return AOGHeroMasteryRuntime.Instance != null
            ? AOGHeroMasteryRuntime.Instance.RecommendBans(redPlayerIds, count)
            : new List<string>();
    }

    public IReadOnlyList<string> RecommendBansForRed(IEnumerable<string> bluePlayerIds, int count = 3)
    {
        return AOGHeroMasteryRuntime.Instance != null
            ? AOGHeroMasteryRuntime.Instance.RecommendBans(bluePlayerIds, count)
            : new List<string>();
    }

    void Advance()
    {
        if (BlueBans.Count < bansPerTeam || RedBans.Count < bansPerTeam)
        {
            if (Phase == AOGDraftPhase.BlueBan) SetPhase(AOGDraftPhase.RedBan);
            else SetPhase(AOGDraftPhase.BlueBan);
            return;
        }

        if (BluePicks.Count >= picksPerTeam && RedPicks.Count >= picksPerTeam)
        {
            SetPhase(AOGDraftPhase.Locked);
            return;
        }

        if (Phase == AOGDraftPhase.BlueBan || Phase == AOGDraftPhase.RedBan)
        {
            SetPhase(AOGDraftPhase.BluePick);
            return;
        }

        if (Phase == AOGDraftPhase.BluePick)
            SetPhase(AOGDraftPhase.RedPick);
        else
            SetPhase(AOGDraftPhase.BluePick);
    }

    void SetPhase(AOGDraftPhase phase)
    {
        Phase = phase;
        PhaseRemaining = phaseSeconds;
    }
}
