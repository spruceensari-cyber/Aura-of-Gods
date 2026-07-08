using UnityEngine;

/// <summary>
/// ELO-based ranking system for competitive play
/// </summary>
public class RankingSystem : MonoBehaviour
{
    [SerializeField] private float kFactor = 32f; // K-factor for ELO calculation
    
    public class PlayerRank
    {
        public string PlayerName { get; set; }
        public int ELO { get; set; } = 1200; // Starting ELO
        public int Wins { get; set; }
        public int Losses { get; set; }
        public int WinStreak { get; set; }
        public RankTier CurrentTier { get; set; } = RankTier.Bronze;
        public int SeasonGames { get; set; }
        public int SeasonReward { get; set; }
    }
    
    public enum RankTier
    {
        Iron = 0,
        Bronze = 400,
        Silver = 800,
        Gold = 1200,
        Platinum = 1600,
        Diamond = 2000,
        Master = 2400,
        Grandmaster = 2800,
        Challenger = 3200
    }
    
    public void UpdatePlayerRank(PlayerRank player, bool won, int opponentELO)
    {
        float expectedScore = 1f / (1f + Mathf.Pow(10, (opponentELO - player.ELO) / 400f));
        int eloChange = (int)(kFactor * ((won ? 1 : 0) - expectedScore));
        
        player.ELO += eloChange;
        
        if (won)
        {
            player.Wins++;
            player.WinStreak++;
        }
        else
        {
            player.Losses++;
            player.WinStreak = 0;
        }
        
        player.SeasonGames++;
        UpdateTier(player);
        
        Debug.Log($"{player.PlayerName}: ELO {player.ELO} ({(won ? "+" : "")}{eloChange}), Tier: {player.CurrentTier}");
    }
    
    private void UpdateTier(PlayerRank player)
    {
        RankTier newTier = RankTier.Iron;
        
        if (player.ELO >= 3200) newTier = RankTier.Challenger;
        else if (player.ELO >= 2800) newTier = RankTier.Grandmaster;
        else if (player.ELO >= 2400) newTier = RankTier.Master;
        else if (player.ELO >= 2000) newTier = RankTier.Diamond;
        else if (player.ELO >= 1600) newTier = RankTier.Platinum;
        else if (player.ELO >= 1200) newTier = RankTier.Gold;
        else if (player.ELO >= 800) newTier = RankTier.Silver;
        else if (player.ELO >= 400) newTier = RankTier.Bronze;
        else newTier = RankTier.Iron;
        
        player.CurrentTier = newTier;
    }
    
    public string GetTierDisplay(RankTier tier)
    {
        return tier switch
        {
            RankTier.Iron => "♦ Iron",
            RankTier.Bronze => "◆ Bronze",
            RankTier.Silver => "⬡ Silver",
            RankTier.Gold => "⬟ Gold",
            RankTier.Platinum => "⬢ Platinum",
            RankTier.Diamond => "◇ Diamond",
            RankTier.Master => "✦ Master",
            RankTier.Grandmaster => "✧ Grandmaster",
            RankTier.Challenger => "👑 Challenger",
            _ => "Unknown"
        };
    }
}

/// <summary>
/// Season rewards and milestones
/// </summary>
public class SeasonRewards : MonoBehaviour
{
    public int GetSeasonReward(RankingSystem.RankTier tier, int wins)
    {
        int baseReward = tier switch
        {
            RankingSystem.RankTier.Iron => 300,
            RankingSystem.RankTier.Bronze => 500,
            RankingSystem.RankTier.Silver => 800,
            RankingSystem.RankTier.Gold => 1200,
            RankingSystem.RankTier.Platinum => 1800,
            RankingSystem.RankTier.Diamond => 2500,
            RankingSystem.RankTier.Master => 3500,
            RankingSystem.RankTier.Grandmaster => 5000,
            RankingSystem.RankTier.Challenger => 10000,
            _ => 0
        };
        
        return baseReward + (wins * 50);
    }
}
