using UnityEngine;
using System.Collections;

public enum AbilityKey
{
    Q, W, E, R, Passive
}

public enum AbilityType
{
    SingleTarget,
    AOE,
    Linear,
    Instant,
    Channeled
}

/// <summary>
/// Professional ability system for champions
/// Supports cooldowns, mana costs, cast time, and effects
/// </summary>
public class ChampionAbility : MonoBehaviour
{
    [SerializeField] private AbilityKey abilityKey;
    [SerializeField] private AbilityType abilityType;
    [SerializeField] private string abilityName;
    [SerializeField] private string description;
    
    [Header("Costs & Cooldown")]
    [SerializeField] private float manaCost = 50f;
    [SerializeField] private float cooldownSeconds = 5f;
    [SerializeField] private float castTime = 0.5f;
    
    [Header("Damage & Range")]
    [SerializeField] private float baseDamage = 50f;
    [SerializeField] private float abilityPower = 0.6f; // 60% AP scaling
    [SerializeField] private float range = 10f;
    [SerializeField] private float aoeRadius = 3f;
    
    private Champion champion;
    private float nextAvailableTime;
    private float castEndTime;
    private Coroutine castCoroutine;
    private bool isOnCooldown;
    
    void Start()
    {
        champion = GetComponent<Champion>();
        nextAvailableTime = 0f;
    }
    
    public bool CanCast()
    {
        if (champion == null) return false;
        if (Time.time < nextAvailableTime) return false;
        if (champion.CurrentMana < manaCost) return false;
        if (isOnCooldown) return false;
        
        return true;
    }
    
    public void Cast(Vector3 targetPosition, Champion targetChampion = null)
    {
        if (!CanCast())
        {
            Debug.LogWarning($"Cannot cast {abilityName}");
            return;
        }
        
        champion.SpendMana(manaCost);
        
        if (castTime > 0)
        {
            if (castCoroutine != null)
                StopCoroutine(castCoroutine);
            castCoroutine = StartCoroutine(CastWithDelay(targetPosition, targetChampion));
        }
        else
        {
            ExecuteAbility(targetPosition, targetChampion);
        }
        
        StartCooldown();
    }
    
    private IEnumerator CastWithDelay(Vector3 targetPosition, Champion targetChampion)
    {
        champion.IsCasting = true;
        yield return new WaitForSeconds(castTime);
        ExecuteAbility(targetPosition, targetChampion);
        champion.IsCasting = false;
    }
    
    private void ExecuteAbility(Vector3 targetPosition, Champion targetChampion)
    {
        float damage = baseDamage + (champion.AbilityPower * abilityPower);
        
        switch (abilityType)
        {
            case AbilityType.SingleTarget:
                if (targetChampion != null)
                {
                    targetChampion.TakeDamage(damage, DamageType.Magical);
                }
                break;
                
            case AbilityType.AOE:
                DealAOEDamage(targetPosition, damage);
                break;
                
            case AbilityType.Linear:
                DealLinearDamage(targetPosition, damage);
                break;
        }
    }
    
    private void DealAOEDamage(Vector3 center, float damage)
    {
        Collider[] hits = Physics.OverlapSphere(center, aoeRadius);
        foreach (Collider hit in hits)
        {
            if (hit.TryGetComponent<Champion>(out var targetChampion))
            {
                if (targetChampion.Team != champion.Team)
                {
                    targetChampion.TakeDamage(damage, DamageType.Magical);
                }
            }
        }
    }
    
    private void DealLinearDamage(Vector3 direction, float damage)
    {
        RaycastHit[] hits = Physics.RaycastAll(transform.position, direction.normalized, range);
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.TryGetComponent<Champion>(out var targetChampion))
            {
                if (targetChampion.Team != champion.Team)
                {
                    targetChampion.TakeDamage(damage, DamageType.Magical);
                    break; // Only hit first target for linear abilities
                }
            }
        }
    }
    
    private void StartCooldown()
    {
        isOnCooldown = true;
        nextAvailableTime = Time.time + cooldownSeconds;
        StartCoroutine(CooldownCoroutine());
    }
    
    private IEnumerator CooldownCoroutine()
    {
        yield return new WaitForSeconds(cooldownSeconds);
        isOnCooldown = false;
    }
    
    public float GetCooldownRemaining()
    {
        return Mathf.Max(0, nextAvailableTime - Time.time);
    }
    
    public float GetCooldownPercent()
    {
        return 1f - (GetCooldownRemaining() / cooldownSeconds);
    }
}
