using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum DamageType
{
    Physical,
    Magical,
    True
}

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

    [Header("Progression")]
    [SerializeField, Min(1)] private int maxLevel = 18;
    [SerializeField, Min(1)] private int baseExperiencePerLevel = 100;

    private float currentHealth;
    private float currentMana;
    private int level = 1;
    private int experience;
    private int gold;

    private float attackDamage;
    private float abilityPower;
    private float armor;
    private float spellBlock;
    private float movementSpeed;
    private float attackSpeed;

    private bool isCasting;
    private bool isAlive = true;
    private bool isStunned;
    private readonly List<StatModifier> activeModifiers = new();

    public TeamType Team { get; set; }
    public bool IsCasting { get => isCasting; set => isCasting = value; }
    public bool IsAlive => isAlive;
    public bool IsStunned => isStunned;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => baseHealth;
    public float HealthPercent => baseHealth <= 0f ? 0f : currentHealth / baseHealth;
    public float CurrentMana => currentMana;
    public float MaxMana => baseMana;
    public float ManaPercent => baseMana <= 0f ? 0f : currentMana / baseMana;

    public float AttackDamage => attackDamage;
    public float AbilityPower => abilityPower;
    public float Armor => armor;
    public float SpellBlock => spellBlock;
    public float MovementSpeed => movementSpeed;
    public float AttackSpeed => attackSpeed;

    public int Level => level;
    public int MaxLevel => maxLevel;
    public int Experience => experience;
    public int ExperienceToNextLevel => level >= maxLevel ? 0 : level * baseExperiencePerLevel;
    public float ExperiencePercent => level >= maxLevel || ExperienceToNextLevel <= 0
        ? 1f
        : Mathf.Clamp01((float)experience / ExperienceToNextLevel);
    public int Gold => gold;

    public delegate void DamageEvent(float damage, DamageType type);
    public event DamageEvent OnDamaged;
    public event System.Action OnDeath;
    public event System.Action OnLevelUp;
    public event System.Action OnResourcesChanged;
    public event System.Action OnProgressionChanged;

    void Awake()
    {
        SanitizeSerializedStats();
    }

    void Start()
    {
        SanitizeSerializedStats();
        currentHealth = baseHealth;
        currentMana = baseMana;
        RecalculateStats();
        NotifyResourcesChanged();
        OnProgressionChanged?.Invoke();
    }

    private void SanitizeSerializedStats()
    {
        if (baseHealth <= 0f) baseHealth = 500f;
        if (baseMana <= 0f) baseMana = 300f;
        if (baseAttackDamage <= 0f) baseAttackDamage = 60f;
        if (baseArmor <= 0f) baseArmor = 25f;
        if (baseSpellBlock <= 0f) baseSpellBlock = 25f;
        if (baseMovementSpeed <= 0f) baseMovementSpeed = 5f;
        if (baseAttackSpeed <= 0f) baseAttackSpeed = 1f;
        if (maxLevel <= 0) maxLevel = 18;
        if (baseExperiencePerLevel <= 0) baseExperiencePerLevel = 100;
        if (healthRegenPerSec < 0f) healthRegenPerSec = 0.5f;
        if (manaRegenPerSec < 0f) manaRegenPerSec = 1f;
    }

    void Update()
    {
        if (!isAlive) return;
        RegenerateHealth();
        RegenerateMana();
    }

    public void TakeDamage(float damage, DamageType type)
    {
        if (!isAlive || damage <= 0f) return;
        float mitigatedDamage = CalculateMitigatedDamage(damage, type);
        currentHealth = Mathf.Max(0f, currentHealth - mitigatedDamage);
        OnDamaged?.Invoke(mitigatedDamage, type);
        NotifyResourcesChanged();
        if (currentHealth <= 0f) Die();
    }

    public void Heal(float amount)
    {
        if (!isAlive || amount <= 0f) return;
        float previous = currentHealth;
        currentHealth = Mathf.Min(baseHealth, currentHealth + amount);
        if (!Mathf.Approximately(previous, currentHealth)) NotifyResourcesChanged();
    }

    public bool SpendMana(float amount)
    {
        if (amount < 0f || currentMana < amount) return false;
        currentMana -= amount;
        NotifyResourcesChanged();
        return true;
    }

    public bool HasMana(float amount) => currentMana >= amount;

    public void RestoreMana(float amount)
    {
        if (!isAlive || amount <= 0f) return;
        float previous = currentMana;
        currentMana = Mathf.Min(baseMana, currentMana + amount);
        if (!Mathf.Approximately(previous, currentMana)) NotifyResourcesChanged();
    }

    private float CalculateMitigatedDamage(float damage, DamageType type)
    {
        return type switch
        {
            DamageType.Physical => damage * (100f / (100f + Mathf.Max(-99f, armor))),
            DamageType.Magical => damage * (100f / (100f + Mathf.Max(-99f, spellBlock))),
            DamageType.True => damage,
            _ => damage
        };
    }

    private void RegenerateHealth()
    {
        if (currentHealth >= baseHealth || healthRegenPerSec <= 0f) return;
        float previous = currentHealth;
        currentHealth = Mathf.Min(baseHealth, currentHealth + healthRegenPerSec * Time.deltaTime);
        if ((int)previous != (int)currentHealth) NotifyResourcesChanged();
    }

    private void RegenerateMana()
    {
        if (currentMana >= baseMana || manaRegenPerSec <= 0f) return;
        float previous = currentMana;
        currentMana = Mathf.Min(baseMana, currentMana + manaRegenPerSec * Time.deltaTime);
        if ((int)previous != (int)currentMana) NotifyResourcesChanged();
    }

    public void GainExperience(int xp)
    {
        if (xp <= 0 || level >= maxLevel) return;
        experience += xp;
        while (level < maxLevel && experience >= ExperienceToNextLevel)
        {
            experience -= ExperienceToNextLevel;
            level++;
            IncreaseStats();
            OnLevelUp?.Invoke();
        }
        if (level >= maxLevel) experience = 0;
        OnProgressionChanged?.Invoke();
    }

    public void GainGold(int amount)
    {
        if (amount <= 0) return;
        gold += amount;
        OnProgressionChanged?.Invoke();
    }

    public bool SpendGold(int amount)
    {
        if (amount < 0 || gold < amount) return false;
        gold -= amount;
        OnProgressionChanged?.Invoke();
        return true;
    }

    private void IncreaseStats()
    {
        float oldHealthPercent = HealthPercent;
        float oldManaPercent = ManaPercent;
        baseHealth *= 1.08f;
        baseMana *= 1.06f;
        baseAttackDamage *= 1.04f;
        baseAbilityPower *= 1.04f;
        RecalculateStats();
        currentHealth = baseHealth * Mathf.Clamp01(oldHealthPercent + 0.15f);
        currentMana = baseMana * Mathf.Clamp01(oldManaPercent + 0.15f);
        NotifyResourcesChanged();
    }

    private void RecalculateStats()
    {
        attackDamage = baseAttackDamage;
        abilityPower = baseAbilityPower;
        armor = baseArmor;
        spellBlock = baseSpellBlock;
        movementSpeed = baseMovementSpeed;
        attackSpeed = baseAttackSpeed;

        foreach (StatModifier modifier in activeModifiers)
        {
            attackDamage += modifier.AttackDamageBonus;
            abilityPower += modifier.AbilityPowerBonus;
            armor += modifier.ArmorBonus;
            spellBlock += modifier.SpellBlockBonus;
            movementSpeed += modifier.MovementSpeedBonus;
            attackSpeed += modifier.AttackSpeedBonus;
        }

        attackSpeed = Mathf.Max(0.1f, attackSpeed);
        movementSpeed = Mathf.Max(0.1f, movementSpeed);
    }

    public void AddStatModifier(StatModifier modifier)
    {
        if (modifier == null) return;
        activeModifiers.Add(modifier);
        RecalculateStats();
    }

    public void RemoveStatModifier(StatModifier modifier)
    {
        if (modifier == null) return;
        activeModifiers.Remove(modifier);
        RecalculateStats();
    }

    public void Stun(float duration)
    {
        if (!isAlive) return;
        StopCoroutine(nameof(StunRoutine));
        StartCoroutine(StunRoutine(duration));
    }

    private IEnumerator StunRoutine(float duration)
    {
        isStunned = true;
        yield return new WaitForSeconds(Mathf.Max(0f, duration));
        isStunned = false;
    }

    private void Die()
    {
        isAlive = false;
        OnDeath?.Invoke();
        gameObject.SetActive(false);
    }

    public void Revive()
    {
        isAlive = true;
        currentHealth = baseHealth;
        currentMana = baseMana;
        gameObject.SetActive(true);
        NotifyResourcesChanged();
    }

    private void NotifyResourcesChanged() => OnResourcesChanged?.Invoke();
}

public class StatModifier
{
    public float AttackDamageBonus { get; set; }
    public float AbilityPowerBonus { get; set; }
    public float ArmorBonus { get; set; }
    public float SpellBlockBonus { get; set; }
    public float MovementSpeedBonus { get; set; }
    public float AttackSpeedBonus { get; set; }
}
