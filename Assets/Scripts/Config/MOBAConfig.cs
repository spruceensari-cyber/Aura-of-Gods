using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Centralized configuration for MOBA balance and mechanics
/// All game constants defined here for easy tweaking
/// </summary>
[CreateAssetMenu(fileName = "MOBAConfig", menuName = "Aura of Gods/MOBA Config")]
public class MOBAConfig : ScriptableObject
{
    [Header("Game Rules")]
    public float gameStartDelay = 30f;
    public float minGameDuration = 600f; // 10 minutes minimum
    public int maxLevel = 18;
    
    [Header("Economy")]
    public int startingGold = 500;
    public float passiveGoldPerSecond = 1.25f;
    public int killGoldBase = 100;
    public int minionGoldMelee = 40;
    public int minionGoldRanged = 45;
    public int minionGoldCannon = 120;
    
    [Header("Experience")]
    public int killXPBase = 300;
    public int minionXP = 50;
    public float xpShareRange = 20f;
    
    [Header("Combat")]
    public float baseAttackSpeed = 0.625f; // 1.6 attacks per second
    public float attackSpeedPerLevel = 0.02f;
    public float armorPerLevel = 3.5f;
    public float spellBlockPerLevel = 1.25f;
    
    [Header("Champion Stats")]
    public float championMoveSpeed = 5f;
    public float champHealthRegenPerLevel = 15f;
    public float champManaRegenPerLevel = 3f;
    
    [Header("Map")]
    public float mapWidth = 100f;
    public float mapHeight = 100f;
    public int numberOfLanes = 3;
    public int turretsPerLane = 3;
    
    [Header("Minions")]
    public float minionSpawnInterval = 30f; // Spawn every 30 seconds
    public int minionsPerWave = 6;
    public float minionMoveSpeed = 3f;
    public float minionAttackRange = 2f;
    
    [Header("Objectives")]
    public float dragonRespawnTime = 300f; // 5 minutes
    public float baronRespawnTime = 420f; // 7 minutes
    public int dragonGoldReward = 500;
    public int baronGoldReward = 1500;
}
