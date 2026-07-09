using System.Collections.Generic;
using UnityEngine;

public enum AOGDraftPhase { Idle, BlueBan, RedBan, BluePick, RedPick, Locked }

public class AOGDraftBanRuntime : MonoBehaviour
{
    public AOGDraftPhase Phase { get; private set; } = AOGDraftPhase.Idle;
    public float PhaseRemaining { get; private set; }
    public readonly List<string> BluePicks = new();
    public readonly List<string> RedPicks = new();
    public readonly List<string> Bans = new();

    [SerializeField] private float phaseSeconds = 25f;

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
        if (PhaseRemaining <= 0f) Advance();
    }

    public void StartDraft()
    {
        BluePicks.Clear(); RedPicks.Clear(); Bans.Clear();
        SetPhase(AOGDraftPhase.BlueBan);
    }

    public bool Submit(string championId)
    {
        if (string.IsNullOrWhiteSpace(championId) || Bans.Contains(championId) || BluePicks.Contains(championId) || RedPicks.Contains(championId)) return false;
        switch (Phase)
        {
            case AOGDraftPhase.BlueBan:
            case AOGDraftPhase.RedBan: Bans.Add(championId); break;
            case AOGDraftPhase.BluePick: BluePicks.Add(championId); break;
            case AOGDraftPhase.RedPick: RedPicks.Add(championId); break;
            default: return false;
        }
        Advance();
        return true;
    }

    void Advance()
    {
        SetPhase(Phase switch
        {
            AOGDraftPhase.BlueBan => AOGDraftPhase.RedBan,
            AOGDraftPhase.RedBan => AOGDraftPhase.BluePick,
            AOGDraftPhase.BluePick => AOGDraftPhase.RedPick,
            AOGDraftPhase.RedPick => BluePicks.Count >= 3 && RedPicks.Count >= 3 ? AOGDraftPhase.Locked : AOGDraftPhase.BluePick,
            _ => AOGDraftPhase.Locked
        });
    }

    void SetPhase(AOGDraftPhase phase)
    {
        Phase = phase;
        PhaseRemaining = phaseSeconds;
    }
}
