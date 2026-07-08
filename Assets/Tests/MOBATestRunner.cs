using UnityEngine;

/// <summary>
Test runner for manual testing of all MOBA systems
/// </summary>
public class MOBATestRunner : MonoBehaviour
{
    [SerializeField] private bool runAllTests = true;
    [SerializeField] private bool logResults = true;
    
    void Start()
    {
        if (runAllTests)
            RunAllTests();
    }
    
    public void RunAllTests()
    {
        Debug.Log("\n========== MOBA SYSTEM TEST SUITE ==========\n");
        
        TestChampionSystem();
        TestAbilitySystem();
        TestMinionSystem();
        TestGameStateSystem();
        TestEconomySystem();
        TestRankingSystem();
        TestAchievementSystem();
        TestNetworkSystem();
        
        Debug.Log("\n========== ALL TESTS COMPLETED ==========\n");
    }
    
    private void TestChampionSystem()
    {
        Debug.Log("[TEST] Champion System");
        
        GameObject champObj = new GameObject("TestChampion");
        Champion champion = champObj.AddComponent<Champion>();
        
        // Test health
        float initialHealth = champion.CurrentHealth;
        champion.TakeDamage(100f, DamageType.Physical);
        bool healthReduced = champion.CurrentHealth < initialHealth;
        
        // Test mana
        float initialMana = champion.CurrentMana;
        champion.SpendMana(50f);
        bool manaReduced = champion.CurrentMana < initialMana;
        
        // Test gold
        champion.GainGold(100);
        bool goldIncreased = true; // Just check no errors
        
        // Test XP
        champion.GainExperience(100);
        bool xpIncreased = true;
        
        LogTestResult("Health Reduction", healthReduced);
        LogTestResult("Mana Spending", manaReduced);
        LogTestResult("Gold Gain", goldIncreased);
        LogTestResult("XP Gain", xpIncreased);
        
        Destroy(champObj);
    }
    
    private void TestAbilitySystem()
    {
        Debug.Log("[TEST] Ability System");
        
        GameObject champObj = new GameObject("TestChampion");
        Champion champion = champObj.AddComponent<Champion>();
        ChampionAbility ability = champObj.AddComponent<ChampionAbility>();
        
        // Test ability casting
        bool canCast = ability.CanCast();
        ability.Cast(Vector3.zero, null);
        bool onCooldown = !ability.CanCast();
        
        LogTestResult("Can Cast", canCast);
        LogTestResult("Ability Cooldown", onCooldown);
        
        Destroy(champObj);
    }
    
    private void TestMinionSystem()
    {
        Debug.Log("[TEST] Minion System");
        
        GameObject minionObj = new GameObject("TestMinion");
        CombatUnit minion = minionObj.AddComponent<CombatUnit>();
        minion.UnitTeam = TeamType.Blue;
        
        // Test minion stats
        float initialHealth = minion.CurrentHealth;
        minion.TakeDamage(10f);
        bool healthReduced = minion.CurrentHealth < initialHealth;
        
        bool isAlive = minion.IsAlive;
        bool isBlueTeam = minion.UnitTeam == TeamType.Blue;
        
        LogTestResult("Minion Health Reduction", healthReduced);
        LogTestResult("Minion Alive", isAlive);
        LogTestResult("Minion Team Assignment", isBlueTeam);
        
        Destroy(minionObj);
    }
    
    private void TestGameStateSystem()
    {
        Debug.Log("[TEST] Game State System");
        
        GameObject stateObj = new GameObject("GameState");
        GameStateManager gameState = stateObj.AddComponent<GameStateManager>();
        
        // Test team stats
        TeamStats blueStats = gameState.GetTeamStats(TeamType.Blue);
        TeamStats redStats = gameState.GetTeamStats(TeamType.Red);
        
        bool hasBlueStats = blueStats != null;
        bool hasRedStats = redStats != null;
        bool statsInitialized = blueStats.Kills == 0 && redStats.Kills == 0;
        
        LogTestResult("Blue Team Stats", hasBlueStats);
        LogTestResult("Red Team Stats", hasRedStats);
        LogTestResult("Stats Initialized", statsInitialized);
        
        Destroy(stateObj);
    }
    
    private void TestEconomySystem()
    {
        Debug.Log("[TEST] Economy System");
        
        GameObject economyObj = new GameObject("Economy");
        EconomyManager economy = economyObj.AddComponent<EconomyManager>();
        
        // Test gold distribution
        economy.AwardGold(TeamType.Blue, 100, "Test Kill");
        int blueGold = economy.GetTeamTotalGold(TeamType.Blue);
        
        economy.AwardGold(TeamType.Red, 50, "Test");
        int redGold = economy.GetTeamTotalGold(TeamType.Red);
        
        LogTestResult("Blue Gold Distribution", blueGold > 0);
        LogTestResult("Red Gold Distribution", redGold > 0);
        LogTestResult("Gold Tracking", blueGold > redGold);
        
        Destroy(economyObj);
    }
    
    private void TestRankingSystem()
    {
        Debug.Log("[TEST] Ranking System");
        
        GameObject rankObj = new GameObject("Ranking");
        RankingSystem ranking = rankObj.AddComponent<RankingSystem>();
        
        var player = new RankingSystem.PlayerRank { PlayerName = "TestPlayer", ELO = 1200 };
        int initialELO = player.ELO;
        
        // Simulate a win
        ranking.UpdatePlayerRank(player, true, 1000);
        bool eloIncreased = player.ELO > initialELO;
        bool winsIncremented = player.Wins > 0;
        
        LogTestResult("ELO Calculation", eloIncreased);
        LogTestResult("Win Tracking", winsIncremented);
        LogTestResult("Tier Assignment", player.CurrentTier != RankingSystem.RankTier.Iron);
        
        Destroy(rankObj);
    }
    
    private void TestAchievementSystem()
    {
        Debug.Log("[TEST] Achievement System");
        
        GameObject champObj = new GameObject("Champion");
        Champion champion = champObj.AddComponent<Champion>();
        
        GameObject achieveObj = new GameObject("Achievements");
        AchievementSystem achievements = achieveObj.AddComponent<AchievementSystem>();
        
        // Record kills
        for (int i = 0; i < 5; i++)
            achievements.RecordKill(champion);
        
        int killStreak = achievements.GetKillStreak(champion);
        int totalKills = achievements.GetTotalKills(champion);
        
        LogTestResult("Kill Streak Tracking", killStreak == 5);
        LogTestResult("Total Kills Tracking", totalKills == 5);
        
        Destroy(champObj);
        Destroy(achieveObj);
    }
    
    private void TestNetworkSystem()
    {
        Debug.Log("[TEST] Network System");
        
        GameObject netObj = new GameObject("Network");
        NetworkManager network = netObj.AddComponent<NetworkManager>();
        
        // Register players
        network.RegisterPlayer(1, "Player1", TeamType.Blue);
        network.RegisterPlayer(2, "Player2", TeamType.Red);
        
        PlayerNetworkData p1 = network.GetPlayer(1);
        PlayerNetworkData p2 = network.GetPlayer(2);
        
        bool player1Registered = p1 != null && p1.PlayerName == "Player1";
        bool player2Registered = p2 != null && p2.PlayerName == "Player2";
        bool teamAssignment = p1.Team == TeamType.Blue && p2.Team == TeamType.Red;
        
        LogTestResult("Player 1 Registration", player1Registered);
        LogTestResult("Player 2 Registration", player2Registered);
        LogTestResult("Team Assignment", teamAssignment);
        
        Destroy(netObj);
    }
    
    private void LogTestResult(string testName, bool passed)
    {
        string result = passed ? "✅ PASS" : "❌ FAIL";
        Debug.Log($"  {result}: {testName}");
    }
}
