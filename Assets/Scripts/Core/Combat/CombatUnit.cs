using UnityEngine;

/// <summary>
/// Base class for all combat units (Champions, Minions, Towers, Structures)
/// Handles basic combat interactions and team identification
/// </summary>
public class CombatUnit : MonoBehaviour
{
    [SerializeField] protected TeamType unitTeam = TeamType.Blue;
    [SerializeField] protected UnitType unitType;
    [SerializeField] protected float baseHealth = 100f;
    
    protected float currentHealth;
    protected bool isAlive = true;
    
    public TeamType UnitTeam => unitTeam;
    public UnitType UnitType => unitType;
    public float CurrentHealth => currentHealth;
    public float HealthPercent => currentHealth / baseHealth;
    public bool IsAlive => isAlive;
    
    public void Configure(TeamType team, UnitType type, float health)
    {
        unitTeam = team;
        unitType = type;
        baseHealth = Mathf.Max(1f, health);
        currentHealth = baseHealth;
        isAlive = true;
        OnHealthChanged?.Invoke(currentHealth);
    }
    public delegate void HealthChangeEvent(float healthAmount);
    public event HealthChangeEvent OnHealthChanged;
    public event System.Action OnDeath;
    
    protected virtual void Start()
    {
        currentHealth = baseHealth;
    }
    
    public virtual void TakeDamage(float damage)
    {
        if (!isAlive) return;
        
        currentHealth -= damage;
        OnHealthChanged?.Invoke(currentHealth);
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    public virtual void Heal(float amount)
    {
        if (!isAlive) return;
        
        currentHealth = Mathf.Min(currentHealth + amount, baseHealth);
        OnHealthChanged?.Invoke(currentHealth);
    }
    
    protected virtual void Die()
    {
        isAlive = false;
        OnDeath?.Invoke();
    }
    
    public bool IsEnemyOf(CombatUnit other)
    {
        return other != null && unitTeam != other.unitTeam && other.unitTeam != TeamType.Neutral;
    }
    
    public bool IsAllyOf(CombatUnit other)
    {
        return other != null && unitTeam == other.unitTeam;
    }
}

public enum UnitType
{
    Champion,
    Minion,
    Tower,
    Structure,
    Neutral
}
