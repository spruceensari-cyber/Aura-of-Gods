using UnityEngine;
using System.Collections.Generic;

public enum DamageType
{
    Physical,
    Magical,
    True
}

/// <summary>
/// Base champion class with stats, abilities, and combat system
/// </summary>
public class Champion : MonoBehaviour
{
    [Header("Core Stats")]
    [SerializeField] private float baseHealth = 500f;
    [SerializeField] private float baseMana = 300f;
    [SerializeField] private float baseAttackDamage = 60f;
    [SerializeField] private float baseAbilityPower = 0f;
    [SerializeField] private float baseArmor = 25f;
    [SerializeField] private float baseSpellBlock = 25f;
    
    [Header("Movement")]
    [SerializeField] private float baseMovementSpeed = 5f;
    [SerializeField] private float baseAttackSpeed = 1f;
    
    [Header("Regen")]
    [SerializeField] private float healthRegenPerSec = 0.5f;
    [SerializeField] private float manaRegenPerSec = 1f;
    
    // Current stats
    private float currentHealth;
    private float currentMana;
    private float level = 1f;
    private int experience = 0;
    private int gold = 0;
    
    // Calculated stats with items
    private float attackDamage;
    private float abilityPower;
    private float armor;
    private float spellBlock;
    private float movementSpeed;
    private float attackSpeed;
    
    // State
    private bool isCasting;
    private bool isAlive = true;
    private bool isStunned;
    private List<StatModifier> activeModifiers = new();
    
    public TeamType Team { get; set; }
    public bool IsCasting { get => isCasting; set => isCasting = value; }
    public float CurrentHealth => currentHealth;
    public float CurrentMana => currentMana;
    public float AbilityPower => abilityPower;
    public int Level => (int)level;
    
    // Events
    public delegate void DamageEvent(float damage, DamageType type);
    public event DamageEvent OnDamaged;
    public event System.Action OnDeath;
    public event System.Action OnLevelUp;
    
    void Start()
    {
        currentHealth = baseHealth;
        currentMana = baseMana;
        RecalculateStats();
    }
    
    void Update()
    {
        if (!isAlive) return;
        if (isStunned) return;
        
        // Regenerate resources
        RegenerateHealth();
        RegenerateMana();
        
        // Movement and input handled by controller
    }
    
    public void TakeDamage(float damage, DamageType type)
    {
        if (!isAlive) return;
        
        float mitigatedDamage = CalculateMitigatedDamage(damage, type);
        currentHealth -= mitigatedDamage;
        
        OnDamaged?.Invoke(mitigatedDamage, type);
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    public void SpendMana(float amount)
    {
        currentMana -= amount;
        if (currentMana < 0)
            currentMana = 0;
    }
    
    public bool HasMana(float amount)
    {
        return currentMana >= amount;
    }
    
    private float CalculateMitigatedDamage(float damage, DamageType type)
    {
        return type switch
        {
            DamageType.Physical => damage * (100f / (100f + armor)),
            DamageType.Magical => damage * (100f / (100f + spellBlock)),
            DamageType.True => damage,
            _ => damage
        };
    }
    
    private void RegenerateHealth()
    {
        if (currentHealth < baseHealth)
        {
            currentHealth += healthRegenPerSec * Time.deltaTime;
            if (currentHealth > baseHealth)
                currentHealth = baseHealth;
        }
    }
    
    private void RegenerateMana()
    {
        if (currentMana < baseMana)
        {
            currentMana += manaRegenPerSec * Time.deltaTime;
            if (currentMana > baseMana)
                currentMana = baseMana;
        }
    }
    
    public void GainExperience(int xp)
    {
        experience += xp;
        CheckLevelUp();
    }
    
    public void GainGold(int amount)
    {
        gold += amount;
    }
    
    private void CheckLevelUp()
    {
        int xpPerLevel = 100;
        int nextLevelXP = (int)level * xpPerLevel;
        
        if (experience >= nextLevelXP && level < 18)
        {
            level++;
            experience = 0;
            OnLevelUp?.Invoke();
            IncreaseStats();
        }
    }
    
    private void IncreaseStats()
    {
        baseHealth *= 1.08f;
        baseMana *= 1.06f;
        baseAttackDamage *= 1.04f;
        baseAbilityPower *= 1.04f;
        currentHealth = baseHealth;
        currentMana = baseMana;
        RecalculateStats();
    }
    
    private void RecalculateStats()
    {
        attackDamage = baseAttackDamage;
        abilityPower = baseAbilityPower;
        armor = baseArmor;
        spellBlock = baseSpellBlock;
        movementSpeed = baseMovementSpeed;
        attackSpeed = baseAttackSpeed;
        
        // Apply modifiers from items
        foreach (var modifier in activeModifiers)
        {
            attackDamage += modifier.AttackDamageBonus;
            abilityPower += modifier.AbilityPowerBonus;
            armor += modifier.ArmorBonus;
            spellBlock += modifier.SpellBlockBonus;
            movementSpeed += modifier.MovementSpeedBonus;
        }
    }
    
    public void AddStatModifier(StatModifier modifier)
    {
        activeModifiers.Add(modifier);
        RecalculateStats();
    }
    
    public void RemoveStatModifier(StatModifier modifier)
    {
        activeModifiers.Remove(modifier);
        RecalculateStats();
    }
    
    public void Stun(float duration)
    {
        StartCoroutine(System.Collections.Coroutines.WaitAndUnstun(this, duration));
    }
    
    private void Die()
    {
        isAlive = false;
        OnDeath?.Invoke();
        // Disable movement, hide, etc
        gameObject.SetActive(false);
    }
    
    public void Revive()
    {
        isAlive = true;
        currentHealth = baseHealth;
        gameObject.SetActive(true);
    }
}

public class StatModifier
{
    public float AttackDamageBonus { get; set; }
    public float AbilityPowerBonus { get; set; }
    public float ArmorBonus { get; set; }
    public float SpellBlockBonus { get; set; }
    public float MovementSpeedBonus { get; set; }
}
