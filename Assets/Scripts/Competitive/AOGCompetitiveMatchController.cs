using System;
using UnityEngine;

public enum AOGMatchPhase
{
    Warmup,
    Countdown,
    Live,
    Paused,
    Finished
}

/// <summary>
/// Deterministic tournament-facing match state layer.
/// Owns match clock, countdown, pause bookkeeping, side names and best-of series metadata.
/// It does not replace authoritative networking; it provides a clean competitive state model for HUD, observers and future servers.
/// </summary>
public class AOGCompetitiveMatchController : MonoBehaviour
{
    private const string RuntimeName = "AOG_Competitive_Match_Controller";

    [Header("Match")]
    [SerializeField] private float warmupDuration = 8f;
    [SerializeField] private float countdownDuration = 5f;
    [SerializeField] private int bestOf = 3;
    [SerializeField] private string blueTeamName = "BLUE";
    [SerializeField] private string redTeamName = "RED";

    private AOGMatchPhase phase = AOGMatchPhase.Warmup;
    private float phaseStartedAt;
    private float liveStartedAt;
    private float pausedAt;
    private float totalPausedDuration;
    private int blueSeriesWins;
    private int redSeriesWins;
    private string winnerName = string.Empty;

    public static AOGCompetitiveMatchController Instance { get; private set; }
    public AOGMatchPhase Phase => phase;
    public string BlueTeamName => blueTeamName;
    public string RedTeamName => redTeamName;
    public int BestOf => bestOf;
    public int BlueSeriesWins => blueSeriesWins;
    public int RedSeriesWins => redSeriesWins;
    public string WinnerName => winnerName;

    public float MatchTime
    {
        get
        {
            if (phase == AOGMatchPhase.Warmup || phase == AOGMatchPhase.Countdown)
                return 0f;

            float end = phase == AOGMatchPhase.Paused ? pausedAt : Time.unscaledTime;
            return Mathf.Max(0f, end - liveStartedAt - totalPausedDuration);
        }
    }

    public float PhaseRemaining
    {
        get
        {
            float duration = phase == AOGMatchPhase.Warmup ? warmupDuration :
                phase == AOGMatchPhase.Countdown ? countdownDuration : 0f;
            return Mathf.Max(0f, duration - (Time.unscaledTime - phaseStartedAt));
        }
    }

    public event Action<AOGMatchPhase> OnPhaseChanged;
    public event Action OnSeriesChanged;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindObjectOfType<AOGCompetitiveMatchController>() != null)
            return;

        GameObject obj = new GameObject(RuntimeName);
        obj.AddComponent<AOGCompetitiveMatchController>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        SetPhase(AOGMatchPhase.Warmup);
    }

    void Update()
    {
        switch (phase)
        {
            case AOGMatchPhase.Warmup:
                if (PhaseRemaining <= 0f)
                    SetPhase(AOGMatchPhase.Countdown);
                break;
            case AOGMatchPhase.Countdown:
                if (PhaseRemaining <= 0f)
                {
                    liveStartedAt = Time.unscaledTime;
                    totalPausedDuration = 0f;
                    SetPhase(AOGMatchPhase.Live);
                }
                break;
        }
    }

    public void ConfigureSeries(string blueName, string redName, int seriesBestOf)
    {
        blueTeamName = string.IsNullOrWhiteSpace(blueName) ? "BLUE" : blueName.Trim();
        redTeamName = string.IsNullOrWhiteSpace(redName) ? "RED" : redName.Trim();
        bestOf = Mathf.Max(1, seriesBestOf | 1);
        blueSeriesWins = 0;
        redSeriesWins = 0;
        winnerName = string.Empty;
        OnSeriesChanged?.Invoke();
    }

    public bool RequestPause()
    {
        if (phase != AOGMatchPhase.Live)
            return false;

        pausedAt = Time.unscaledTime;
        SetPhase(AOGMatchPhase.Paused);
        Time.timeScale = 0f;
        return true;
    }

    public bool ResumeMatch()
    {
        if (phase != AOGMatchPhase.Paused)
            return false;

        totalPausedDuration += Time.unscaledTime - pausedAt;
        Time.timeScale = 1f;
        SetPhase(AOGMatchPhase.Live);
        return true;
    }

    public void RecordGameWinner(TeamType team)
    {
        if (phase == AOGMatchPhase.Finished)
            return;

        if (team == TeamType.Blue)
            blueSeriesWins++;
        else if (team == TeamType.Red)
            redSeriesWins++;
        else
            return;

        int winsNeeded = bestOf / 2 + 1;
        if (blueSeriesWins >= winsNeeded)
        {
            winnerName = blueTeamName;
            SetPhase(AOGMatchPhase.Finished);
        }
        else if (redSeriesWins >= winsNeeded)
        {
            winnerName = redTeamName;
            SetPhase(AOGMatchPhase.Finished);
        }

        OnSeriesChanged?.Invoke();
    }

    public void ResetForNextGame()
    {
        if (phase == AOGMatchPhase.Finished && !string.IsNullOrEmpty(winnerName))
            return;

        Time.timeScale = 1f;
        winnerName = string.Empty;
        SetPhase(AOGMatchPhase.Warmup);
    }

    private void SetPhase(AOGMatchPhase next)
    {
        phase = next;
        phaseStartedAt = Time.unscaledTime;
        OnPhaseChanged?.Invoke(phase);
    }
}
