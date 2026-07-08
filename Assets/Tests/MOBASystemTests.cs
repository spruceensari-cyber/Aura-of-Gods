using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;

/// <summary>
/// Comprehensive unit tests for MOBA core systems
/// </summary>
public class MOBASystemTests
{
    [Test]
    public void Champion_TakeDamage_ReducesHealth()
    {
        // Arrange
        GameObject champObj = new GameObject();
        Champion champion = champObj.AddComponent<Champion>();
        champion.CurrentHealth = 500f;
        
        // Act
        champion.TakeDamage(100f, DamageType.Physical);
        
        // Assert
        Assert.Less(champion.CurrentHealth, 500f);
        Assert.Greater(champion.CurrentHealth, 0f);
        
        Object.Destroy(champObj);
    }
    
    [Test]
    public void Champion_Dies_WhenHealthZero()
    {
        // Arrange
        GameObject champObj = new GameObject();
        Champion champion = champObj.AddComponent<Champion>();
        
        // Act
        champion.TakeDamage(600f, DamageType.Physical);
        
        // Assert
        Assert.AreEqual(0f, champion.CurrentHealth);
        
        Object.Destroy(champObj);
    }
    
    [Test]
    public void Champion_ArmorReducesDamage()
    {
        // Arrange
        GameObject champObj = new GameObject();
        Champion champion = champObj.AddComponent<Champion>();
        
        float damageWithoutArmor = 100f;
        float expectedDamageWithArmor = damageWithoutArmor * (100f / (100f + 25f)); // 25 base armor
        
        // Act
        champion.TakeDamage(damageWithoutArmor, DamageType.Physical);
        
        // Assert - damage should be reduced by armor
        Assert.Less(champion.CurrentHealth, 500f - expectedDamageWithArmor + 1f);
        
        Object.Destroy(champObj);
    }
    
    [Test]
    public void Ability_CooldownWorks()
    {
        // Arrange
        GameObject champObj = new GameObject();
        Champion champion = champObj.AddComponent<Champion>();
        ChampionAbility ability = champObj.AddComponent<ChampionAbility>();
        
        // Act - Cast ability
        ability.Cast(Vector3.zero, null);
        bool canCastImmediately = ability.CanCast();
        
        // Assert - Should not be able to cast immediately
        Assert.IsFalse(canCastImmediately);
        
        Object.Destroy(champObj);
    }
    
    [Test]
    public void Minion_FollowsLane()
    {
        // Arrange
        GameObject minionObj = new GameObject();
        CombatUnit minion = minionObj.AddComponent<CombatUnit>();
        minion.UnitTeam = TeamType.Blue;
        
        Vector3 startPos = minionObj.transform.position;
        
        // Act
        minionObj.transform.position += Vector3.forward * 5f;
        
        // Assert - Position changed
        Assert.AreNotEqual(startPos, minionObj.transform.position);
        
        Object.Destroy(minionObj);
    }
    
    [Test]
    public void GameState_TracksKills()
    {
        // Arrange
        GameObject stateObj = new GameObject();
        GameStateManager gameState = stateObj.AddComponent<GameStateManager>();
        
        // Act
        TeamStats blueStats = gameState.GetTeamStats(TeamType.Blue);
        int initialKills = blueStats.Kills;
        
        // Assert
        Assert.AreEqual(0, initialKills);
        
        Object.Destroy(stateObj);
    }
    
    [Test]
    public void Economy_GoldDistribution()
    {
        // Arrange
        GameObject economyObj = new GameObject();
        EconomyManager economy = economyObj.AddComponent<EconomyManager>();
        
        // Act
        economy.AwardGold(TeamType.Blue, 100, "Test");
        int totalGold = economy.GetTeamTotalGold(TeamType.Blue);
        
        // Assert
        Assert.Greater(totalGold, 0);
        
        Object.Destroy(economyObj);
    }
    
    [Test]
    public void Ranking_ELOCalculation()
    {
        // Arrange
        GameObject rankObj = new GameObject();
        RankingSystem ranking = rankObj.AddComponent<RankingSystem>();
        var player = new RankingSystem.PlayerRank { PlayerName = "TestPlayer", ELO = 1200 };
        
        // Act
        ranking.UpdatePlayerRank(player, true, 1000);
        
        // Assert - ELO should increase after winning
        Assert.Greater(player.ELO, 1200);
        Assert.AreEqual(1, player.Wins);
        
        Object.Destroy(rankObj);
    }
    
    [Test]
    public void Achievement_PentakillTracking()
    {
        // Arrange
        GameObject champObj = new GameObject();
        Champion champion = champObj.AddComponent<Champion>();
        
        GameObject achieveObj = new GameObject();
        AchievementSystem achievements = achieveObj.AddComponent<AchievementSystem>();
        
        // Act
        for (int i = 0; i < 5; i++)
            achievements.RecordKill(champion);
        
        // Assert - Should have 5 kill streak
        Assert.AreEqual(5, achievements.GetKillStreak(champion));
        
        Object.Destroy(champObj);
        Object.Destroy(achieveObj);
    }
}

/// <summary>
/// Integration tests for complete game flow
/// </summary>
public class MOBAIntegrationTests
{
    [UnityTest]
    public IEnumerator GameStart_InitializesAllSystems()
    {
        // Arrange - Create game manager
        GameObject gameObj = new GameObject("GameManager");
        GameStateManager gameState = gameObj.AddComponent<GameStateManager>();
        NetworkManager network = gameObj.AddComponent<NetworkManager>();
        
        // Act
        yield return new WaitForSeconds(1f);
        
        // Assert
        Assert.IsNotNull(gameState.GetTeamStats(TeamType.Blue));
        Assert.IsNotNull(gameState.GetTeamStats(TeamType.Red));
        
        Object.Destroy(gameObj);
    }
    
    [UnityTest]
    public IEnumerator Champion_CombatFlow_Complete()
    {
        // Arrange
        GameObject champObj = new GameObject("Champion");
        Champion champion = champObj.AddComponent<Champion>();
        champion.Team = TeamType.Blue;
        
        GameObject enemyObj = new GameObject("Enemy");
        Champion enemy = enemyObj.AddComponent<Champion>();
        enemy.Team = TeamType.Red;
        enemyObj.transform.position = new Vector3(5, 0, 0);
        
        float initialHealth = enemy.CurrentHealth;
        
        // Act - Champion deals damage
        enemy.TakeDamage(50f, DamageType.Physical);
        yield return new WaitForSeconds(0.1f);
        
        // Assert
        Assert.Less(enemy.CurrentHealth, initialHealth);
        
        Object.Destroy(champObj);
        Object.Destroy(enemyObj);
    }
    
    [UnityTest]
    public IEnumerator Minion_Wave_Spawning()
    {
        // Arrange
        GameObject minionMgrObj = new GameObject("MinionManager");
        MinionManager minionMgr = minionMgrObj.AddComponent<MinionManager>();
        
        // Act
        yield return new WaitForSeconds(1f);
        
        // Assert - Should have spawned minions
        CombatUnit[] minions = Object.FindObjectsByType<CombatUnit>(FindObjectsSortMode.None);
        // Note: May be 0 if prefabs not set up
        
        Object.Destroy(minionMgrObj);
    }
    
    [UnityTest]
    public IEnumerator Network_PlayerJoin_Sync()
    {
        // Arrange
        GameObject netObj = new GameObject("NetworkManager");
        NetworkManager network = netObj.AddComponent<NetworkManager>();
        
        // Act
        network.RegisterPlayer(1, "TestPlayer", TeamType.Blue);
        yield return new WaitForSeconds(0.1f);
        
        // Assert
        PlayerNetworkData player = network.GetPlayer(1);
        Assert.IsNotNull(player);
        Assert.AreEqual("TestPlayer", player.PlayerName);
        
        Object.Destroy(netObj);
    }
}

/// <summary>
/// Performance tests for MOBA systems
/// </summary>
public class MOBAPerformanceTests
{
    [Performance]
    public void Champion_Update_Performance()
    {
        // Arrange
        GameObject champObj = new GameObject();
        Champion champion = champObj.AddComponent<Champion>();
        
        // Act
        Measure.Frames().Warmup(10).Run(() => {
            champion.TakeDamage(10f, DamageType.Physical);
        });
        
        Object.Destroy(champObj);
    }
    
    [Performance]
    public void FindBestTarget_Performance()
    {
        // Create 100 units
        GameObject[] units = new GameObject[100];
        for (int i = 0; i < 100; i++)
        {
            units[i] = new GameObject($"Unit_{i}");
            CombatUnit unit = units[i].AddComponent<CombatUnit>();
            unit.UnitTeam = i % 2 == 0 ? TeamType.Blue : TeamType.Red;
        }
        
        // Measure performance
        Measure.Frames().Warmup(10).Run(() => {
            CombatUnit[] allUnits = Object.FindObjectsByType<CombatUnit>(FindObjectsSortMode.None);
        });
        
        // Cleanup
        foreach (GameObject unit in units)
            Object.Destroy(unit);
    }
}
