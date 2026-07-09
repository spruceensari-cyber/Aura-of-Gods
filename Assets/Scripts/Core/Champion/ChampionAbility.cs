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
/// Data-driven champion ability runtime with cooldown, mana cost, targeting and damage execution.
/// Supports editor-authored abilities and runtime champion kits without reflection.
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
    [SerializeField] private float abilityPower = 0.6f;
    [SerializeField] private float range = 10f;
    [SerializeField] private float aoeRadius = 3f;

    private Champion champion;
    private float nextAvailableTime;
    private Coroutine castCoroutine;

    public AbilityKey Key => abilityKey;
    public AbilityType Type => abilityType;
    public string AbilityName => string.IsNullOrWhiteSpace(abilityName) ? abilityKey.ToString() : abilityName;
    public string Description => description;
    public float ManaCost => manaCost;
    public float CooldownSeconds => cooldownSeconds;
    public float CastTime => castTime;
    public float BaseDamage => baseDamage;
    public float AbilityPowerRatio => abilityPower;
    public float Range => range;
    public float AOERadius => aoeRadius;
    public bool IsOnCooldown => GetCooldownRemaining() > 0f;
    public Vector3 LastTargetPosition { get; private set; }
    public Champion LastTargetChampion { get; private set; }

    public event System.Action<ChampionAbility> OnCastStarted;
    public event System.Action<ChampionAbility> OnCastCompleted;
    public event System.Action<ChampionAbility> OnCooldownReady;

    void Awake()
    {
        champion = GetComponent<Champion>();
    }

    void Start()
    {
        if (champion == null)
            champion = GetComponent<Champion>();

        nextAvailableTime = 0f;
    }

    public void ConfigureRuntime(
        AbilityKey key,
        AbilityType type,
        string displayName,
        string abilityDescription,
        float mana,
        float cooldown,
        float windup,
        float damage,
        float apRatio,
        float abilityRange,
        float radius)
    {
        abilityKey = key;
        abilityType = type;
        abilityName = displayName;
        description = abilityDescription;
        manaCost = Mathf.Max(0f, mana);
        cooldownSeconds = Mathf.Max(0.01f, cooldown);
        castTime = Mathf.Max(0f, windup);
        baseDamage = Mathf.Max(0f, damage);
        abilityPower = Mathf.Max(0f, apRatio);
        range = Mathf.Max(0.1f, abilityRange);
        aoeRadius = Mathf.Max(0.1f, radius);
    }

    public bool CanCast()
    {
        if (champion == null)
            champion = GetComponent<Champion>();

        return champion != null
            && champion.IsAlive
            && !champion.IsStunned
            && !champion.IsCasting
            && Time.time >= nextAvailableTime
            && champion.HasMana(manaCost);
    }

    public bool Cast(Vector3 targetPosition, Champion targetChampion = null)
    {
        if (!CanCast())
            return false;

        if (abilityType == AbilityType.SingleTarget)
        {
            if (targetChampion == null || targetChampion == champion || targetChampion.Team == champion.Team)
                return false;

            if (Vector3.Distance(transform.position, targetChampion.transform.position) > range)
                return false;
        }
        else
        {
            Vector3 flatDelta = targetPosition - transform.position;
            flatDelta.y = 0f;
            if (flatDelta.magnitude > range)
                targetPosition = transform.position + flatDelta.normalized * range;
        }

        if (!champion.SpendMana(manaCost))
            return false;

        LastTargetPosition = targetPosition;
        LastTargetChampion = targetChampion;
        nextAvailableTime = Time.time + Mathf.Max(0.01f, cooldownSeconds);
        OnCastStarted?.Invoke(this);

        if (castCoroutine != null)
            StopCoroutine(castCoroutine);

        castCoroutine = StartCoroutine(CastRoutine(targetPosition, targetChampion));
        return true;
    }

    private IEnumerator CastRoutine(Vector3 targetPosition, Champion targetChampion)
    {
        champion.IsCasting = true;

        if (castTime > 0f)
            yield return new WaitForSeconds(castTime);

        ExecuteAbility(targetPosition, targetChampion);
        champion.IsCasting = false;
        castCoroutine = null;
        OnCastCompleted?.Invoke(this);

        float remaining = GetCooldownRemaining();
        if (remaining > 0f)
            yield return new WaitForSeconds(remaining);

        OnCooldownReady?.Invoke(this);
    }

    private void ExecuteAbility(Vector3 targetPosition, Champion targetChampion)
    {
        float damage = baseDamage + (champion.AbilityPower * abilityPower);

        switch (abilityType)
        {
            case AbilityType.SingleTarget:
                if (targetChampion != null && targetChampion.Team != champion.Team)
                    targetChampion.TakeDamage(damage, DamageType.Magical);
                break;

            case AbilityType.AOE:
                DealAOEDamage(targetPosition, damage);
                break;

            case AbilityType.Linear:
                DealLinearDamage(targetPosition, damage);
                break;

            case AbilityType.Instant:
                DealAOEDamage(transform.position, damage);
                break;

            case AbilityType.Channeled:
                DealAOEDamage(targetPosition, damage);
                break;
        }
    }

    private void DealAOEDamage(Vector3 center, float damage)
    {
        Collider[] hits = Physics.OverlapSphere(center, aoeRadius);
        foreach (Collider hit in hits)
        {
            Champion targetChampion = hit.GetComponentInParent<Champion>();
            if (targetChampion != null && targetChampion != champion && targetChampion.Team != champion.Team)
                targetChampion.TakeDamage(damage, DamageType.Magical);

            CombatUnit targetUnit = hit.GetComponentInParent<CombatUnit>();
            if (targetUnit != null && targetUnit.UnitTeam != champion.Team)
                targetUnit.TakeDamage(damage);
        }
    }

    private void DealLinearDamage(Vector3 targetPosition, float damage)
    {
        Vector3 direction = targetPosition - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f)
            direction = transform.forward;

        RaycastHit[] hits = Physics.RaycastAll(transform.position, direction.normalized, range);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            Champion targetChampion = hit.collider.GetComponentInParent<Champion>();
            if (targetChampion != null && targetChampion != champion && targetChampion.Team != champion.Team)
            {
                targetChampion.TakeDamage(damage, DamageType.Magical);
                return;
            }

            CombatUnit targetUnit = hit.collider.GetComponentInParent<CombatUnit>();
            if (targetUnit != null && targetUnit.UnitTeam != champion.Team)
            {
                targetUnit.TakeDamage(damage);
                return;
            }
        }
    }

    public float GetCooldownRemaining()
    {
        return Mathf.Max(0f, nextAvailableTime - Time.time);
    }

    public float GetCooldownPercent()
    {
        if (cooldownSeconds <= 0f)
            return 1f;

        return 1f - Mathf.Clamp01(GetCooldownRemaining() / cooldownSeconds);
    }
}
