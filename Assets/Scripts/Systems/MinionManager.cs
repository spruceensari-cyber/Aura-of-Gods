using UnityEngine;
using System.Collections.Generic;

/// <summary>
Automated minion wave manager - spawns minions every 30 seconds
/// </summary>
public class MinionManager : MonoBehaviour
{
    [SerializeField] private GameObject minionPrefab;
    [SerializeField] private float spawnInterval = 30f;
    [SerializeField] private Transform[] blueLanePaths;
    [SerializeField] private Transform[] redLanePaths;
    
    private float nextSpawnTime;
    private int waveCount;
    
    void Start()
    {
        nextSpawnTime = Time.time + spawnInterval;
    }
    
    void Update()
    {
        if (Time.time >= nextSpawnTime)
        {
            SpawnWave();
            nextSpawnTime = Time.time + spawnInterval;
        }
    }
    
    private void SpawnWave()
    {
        waveCount++;
        
        // Spawn blue minions
        for (int i = 0; i < 3; i++) // 3 lanes
        {
            for (int j = 0; j < 2; j++) // 2 minions per lane
            {
                SpawnMinion(TeamType.Blue, blueLanePaths[i]);
            }
        }
        
        // Spawn red minions
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 2; j++)
            {
                SpawnMinion(TeamType.Red, redLanePaths[i]);
            }
        }
        
        Debug.Log($"Wave {waveCount} spawned!");
    }
    
    private void SpawnMinion(TeamType team, Transform lanePath)
    {
        GameObject minion = Instantiate(minionPrefab);
        minion.name = $"Minion_{team}_{waveCount}";
        minion.transform.position = lanePath.position + Random.insideUnitSphere * 2f;
        
        CombatUnit unit = minion.AddComponent<CombatUnit>();
        unit.baseHealth = 50f;
        
        // Minions are AI controlled
    }
}
