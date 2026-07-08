using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Manages team economy - gold distribution, item purchases, and team resources
/// </summary>
public class EconomyManager : MonoBehaviour
{
    private Dictionary<TeamType, TeamEconomy> teamEconomies = new();
    
    void Start()
    {
        teamEconomies[TeamType.Blue] = new TeamEconomy(TeamType.Blue);
        teamEconomies[TeamType.Red] = new TeamEconomy(TeamType.Red);
    }
    
    public void AwardGold(TeamType team, int amount, string reason)
    {
        if (teamEconomies.TryGetValue(team, out var economy))
        {
            economy.TotalGoldEarned += amount;
            Debug.Log($"[{team}] +{amount} gold ({reason}). Total: {economy.TotalGoldEarned}");
        }
    }
    
    public void DistributeKillGold(TeamType killingTeam, int victimLevel)
    {
        int baseGold = 100 + (victimLevel * 10);
        int assistBonus = baseGold / 2;
        
        AwardGold(killingTeam, baseGold, "Kill");
        // Additional gold to assists handled by game state
    }
    
    public void DistributeMinionGold(TeamType killingTeam, MinionType minionType)
    {
        int goldAmount = minionType switch
        {
            MinionType.Melee => 40,
            MinionType.Ranged => 45,
            MinionType.Cannon => 120,
            _ => 0
        };
        
        AwardGold(killingTeam, goldAmount, "Minion Kill");
    }
    
    public void DistributePassiveGold()
    {
        float gameTime = Time.time / 60f; // Convert to minutes
        float passivePerSecond = Mathf.Clamp(gameTime / 5f, 1f, 8f); // Scales from 1 to 8 per second
        
        foreach (var team in teamEconomies.Values)
        {
            team.PassiveGold += passivePerSecond * Time.deltaTime;
        }
    }
    
    public int GetTeamTotalGold(TeamType team)
    {
        return teamEconomies.TryGetValue(team, out var economy) 
            ? economy.TotalGoldEarned 
            : 0;
    }
}

public enum MinionType
{
    Melee,
    Ranged,
    Cannon
}

public class TeamEconomy
{
    public TeamType Team { get; }
    public int TotalGoldEarned { get; set; }
    public float PassiveGold { get; set; }
    public List<Item> TeamItems { get; } = new();
    
    public TeamEconomy(TeamType team)
    {
        Team = team;
    }
}

public class Item
{
    public string Name { get; set; }
    public int Cost { get; set; }
    public StatModifier Stats { get; set; }
    public string Description { get; set; }
    public List<string> BuildsInto { get; set; } = new();
}
