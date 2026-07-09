using System.Collections.Generic;
using UnityEngine;

public class AOGSurrenderVoteRuntime : MonoBehaviour
{
    public static AOGSurrenderVoteRuntime Instance { get; private set; }

    [SerializeField] private float earliestVoteMinute = 15f;
    [SerializeField] private float voteDuration = 30f;

    private TeamType activeTeam;
    private float remaining;
    private bool active;
    private readonly HashSet<string> yes = new();
    private readonly HashSet<string> no = new();

    public bool IsActive => active;
    public float Remaining => remaining;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        if (FindObjectOfType<AOGSurrenderVoteRuntime>() != null) return;
        new GameObject("AOG_Surrender_Vote_Runtime").AddComponent<AOGSurrenderVoteRuntime>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        if (!active) return;
        remaining -= Time.unscaledDeltaTime;
        if (remaining <= 0f) Finish(false);
    }

    public bool StartVote(TeamType team, float matchMinutes)
    {
        if (active || matchMinutes < earliestVoteMinute) return false;
        active = true;
        activeTeam = team;
        remaining = voteDuration;
        yes.Clear();
        no.Clear();
        AOGReplayEventLogRuntime.Instance?.Record("SurrenderVoteStart", team.ToString(), string.Empty, Vector3.zero);
        return true;
    }

    public void Vote(string playerId, TeamType team, bool voteYes, int teamSize = 5)
    {
        if (!active || team != activeTeam || string.IsNullOrWhiteSpace(playerId)) return;
        yes.Remove(playerId);
        no.Remove(playerId);
        if (voteYes) yes.Add(playerId); else no.Add(playerId);

        int requiredYes = teamSize <= 4 ? teamSize : 4;
        if (yes.Count >= requiredYes)
            Finish(true);
        else if (no.Count > teamSize - requiredYes)
            Finish(false);
    }

    void Finish(bool passed)
    {
        if (!active) return;
        AOGReplayEventLogRuntime.Instance?.Record(
            passed ? "SurrenderPassed" : "SurrenderFailed",
            activeTeam.ToString(),
            yes.Count + "/" + (yes.Count + no.Count),
            Vector3.zero);

        if (passed)
        {
            TeamType winner = activeTeam == TeamType.Blue ? TeamType.Red : TeamType.Blue;
            FindObjectOfType<AOGMatchEndFlowRuntime>()?.ShowResult(winner, activeTeam);
        }

        active = false;
        remaining = 0f;
        yes.Clear();
        no.Clear();
    }
}
