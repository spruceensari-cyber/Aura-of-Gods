using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Advanced AI for bot champions - positioning, ability usage, team fights
/// </summary>
public class BotChampionAI : MonoBehaviour
{
    private Champion champion;
    private ChampionController controller;
    private float decisionTimer;
    [SerializeField] private float decisionInterval = 1f;
    [SerializeField] private float aggroRange = 15f;
    [SerializeField] private float safeDistance = 8f;
    
    private Champion nearestEnemy;
    private Vector3 strategicPosition;
    private AIState currentState = AIState.Idle;
    
    enum AIState
    {
        Idle,
        Farming,
        Engaging,
        Retreating,
        Teamfighting
    }
    
    void Start()
    {
        champion = GetComponent<Champion>();
        controller = GetComponent<ChampionController>();
        decisionTimer = decisionInterval;
    }
    
    void Update()
    {
        decisionTimer -= Time.deltaTime;
        
        if (decisionTimer <= 0)
        {
            MakeDecision();
            decisionTimer = decisionInterval;
        }
        
        ExecuteState();
    }
    
    private void MakeDecision()
    {
        nearestEnemy = FindNearestEnemy();
        
        if (nearestEnemy != null)
        {
            float healthPercent = champion.CurrentHealth / 500f; // Placeholder max health
            float distanceToEnemy = Vector3.Distance(transform.position, nearestEnemy.transform.position);
            
            // Decision logic
            if (healthPercent < 0.3f)
            {
                currentState = AIState.Retreating;
            }
            else if (distanceToEnemy < aggroRange)
            {
                currentState = AIState.Engaging;
            }
            else
            {
                currentState = AIState.Farming;
            }
        }
        else
        {
            currentState = AIState.Farming;
        }
    }
    
    private void ExecuteState()
    {
        switch (currentState)
        {
            case AIState.Engaging:
                EngageEnemy();
                break;
            case AIState.Retreating:
                Retreat();
                break;
            case AIState.Farming:
                FarmMinions();
                break;
        }
    }
    
    private void EngageEnemy()
    {
        if (nearestEnemy == null) return;
        
        // Move towards enemy
        Vector3 direction = (nearestEnemy.transform.position - transform.position).normalized;
        transform.position += direction * 3f * Time.deltaTime;
        
        // Cast ability if in range
        if (Random.value > 0.7f && champion.HasMana(50f))
        {
            // Random ability cast (simplified)
            champion.SpendMana(50f);
        }
    }
    
    private void Retreat()
    {
        // Move away from nearest enemy
        if (nearestEnemy != null)
        {
            Vector3 awayDirection = (transform.position - nearestEnemy.transform.position).normalized;
            transform.position += awayDirection * 5f * Time.deltaTime;
        }
    }
    
    private void FarmMinions()
    {
        // Simple farming behavior - move towards minions
        CombatUnit[] units = FindObjectsByType<CombatUnit>(FindObjectsSortMode.None);
        
        CombatUnit nearestMinion = null;
        float minDistance = Mathf.Infinity;
        
        foreach (CombatUnit unit in units)
        {
            if (unit.UnitType == UnitType.Minion && unit.UnitTeam != champion.Team)
            {
                float dist = Vector3.Distance(transform.position, unit.transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    nearestMinion = unit;
                }
            }
        }
        
        if (nearestMinion != null && minDistance < 20f)
        {
            Vector3 direction = (nearestMinion.transform.position - transform.position).normalized;
            transform.position += direction * 2.5f * Time.deltaTime;
        }
    }
    
    private Champion FindNearestEnemy()
    {
        Champion[] allChampions = FindObjectsByType<Champion>(FindObjectsSortMode.None);
        Champion nearest = null;
        float minDistance = Mathf.Infinity;
        
        foreach (Champion champ in allChampions)
        {
            if (champ.Team != champion.Team)
            {
                float dist = Vector3.Distance(transform.position, champ.transform.position);
                if (dist < minDistance && dist < aggroRange)
                {
                    minDistance = dist;
                    nearest = champ;
                }
            }
        }
        
        return nearest;
    }
}
