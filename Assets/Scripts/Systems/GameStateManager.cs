using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Game state manager - tracks kills, deaths, objectives, and win conditions
/// </summary>
public class GameStateManager : MonoBehaviour
{
    [SerializeField] private float gameStartDelay = 30f;
    [SerializeField] private float firstBloodReward = 400f;
    
    private bool gameStarted;
    private float gameStartTime;
    private Dictionary<TeamType, TeamStats> teamStats = new();
    private bool firstBloodClaimed;
    
    void Awake()
    {
        teamStats[TeamType.Blue] = new TeamStats(TeamType.Blue);
        teamStats[TeamType.Red] = new TeamStats(TeamType.Red);
    }
    
    void Start()
    {
        gameStartTime = Time.time + gameStartDelay;
        Debug.Log($"Game starts in {gameStartDelay} seconds");
    }
    
    void Update()
    {
        if (!gameStarted && Time.time >= gameStartTime)
        {
            gameStarted = true;
            Debug.Log("=== GAME START ===");
        }
    }
    
    public void RecordKill(Champion killer, Champion victim)
    {
        if (!gameStarted) return;
        
        if (killer == null || victim == null) return;
        
        TeamStats stats = teamStats[killer.Team];
        stats.Kills++;
        
        // First blood bonus
        if (!firstBloodClaimed)
        {
            firstBloodClaimed = true;
            Debug.Log($"FIRST BLOOD! {killer.name} draws first blood!");
            killer.GainGold((int)firstBloodReward);
        }
        
        Debug.Log($"{killer.name} killed {victim.name}! Score: Blue {teamStats[TeamType.Blue].Kills} - {teamStats[TeamType.Red].Kills} Red");
    }
    
    public TeamStats GetTeamStats(TeamType team)
    {
        return teamStats.TryGetValue(team, out var stats) ? stats : null;
    }
    
    public float GetGameTime()
    {
        return Mathf.Max(0, Time.time - gameStartTime);
    }
}

public class TeamStats
{
    public TeamType Team { get; set; }
    public int Kills { get; set; }
    public int Deaths { get; set; }
    public int Objectives { get; set; }
    public int TotalGold { get; set; }
    
    public TeamStats(TeamType team)
    {
        Team = team;
    }
}
