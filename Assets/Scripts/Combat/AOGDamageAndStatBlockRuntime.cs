using System;
using UnityEngine;

public enum AOGDamageType
{
    Physical,
    Magic,
    True
}

[Serializable]
public struct AOGDamagePacket
{
    public float amount;
    public AOGDamageType type;
    public GameObject source;
    public string abilityId;
}

/// <summary>
/// Modular advanced combat stats layered over the existing AOGCharacterStats fields.
/// Existing attackDamage/moveSpeed/maxHp remain authoritative for legacy compatibility.
/// </summary>
public class AOGCombatStatBlock : MonoBehaviour
{
    [Header("Offense")]
    public float abilityPower;
    public float attackSpeedBonus;
    public float abilityHaste;
    public float lifesteal;
    public float spellVamp;

    [Header("Defense")]
    public float armor = 25f;
    public float magicResistance = 25f;
    public float controlResistance;

    [Header("Resource")]
    public float maxResourceBonus;
    public float resourceRegenBonus;

    public float ResolveIncomingDamage(AOGDamagePacket packet)
    {
        float amount = Mathf.Max(0f,packet.amount);
        if (packet.type == AOGDamageType.True)
            return amount;

        float resistance = packet.type == AOGDamageType.Magic ? magicResistance : armor;
        if (resistance >= 0f)
            return amount * (100f / (100f + resistance));

        // Stable negative resistance amplification.
        return amount * (2f - 100f / (100f - resistance));
    }

    public float AbilityCooldownMultiplier
    {
        get
        {
            float haste = Mathf.Max(0f,abilityHaste);
            return 100f / (100f + haste);
        }
    }
}

public static class AOGDamageUtility
{
    public static void Apply(AOGCharacterStats target,float amount,AOGDamageType type,GameObject source,string abilityId)
    {
        if (target == null || amount <= 0f)
            return;
        target.TakeDamage(new AOGDamagePacket
        {
            amount=amount,
            type=type,
            source=source,
            abilityId=abilityId
        });
    }
}

/// <summary>
/// Applies advanced sustain stats from confirmed combat events. Damage authority stays in
/// existing combat systems; this only heals the source after successful hit events.
/// </summary>
public class AOGAdvancedSustainRuntime : MonoBehaviour
{
    private AOGCharacterStats stats;
    private AOGCombatStatBlock block;

    private void Awake()
    {
        stats=GetComponent<AOGCharacterStats>();
        block=GetComponent<AOGCombatStatBlock>();
    }

    private void OnEnable()
    {
        AOGCombatEvents.BasicAttackHit += OnBasicHit;
        AOGCombatEvents.AbilityHit += OnAbilityHit;
    }

    private void OnDisable()
    {
        AOGCombatEvents.BasicAttackHit -= OnBasicHit;
        AOGCombatEvents.AbilityHit -= OnAbilityHit;
    }

    private void OnBasicHit(AOGCombatHitEvent hit)
    {
        if (!BelongsToMe(hit.source) || stats == null || block == null || block.lifesteal <= 0f)
            return;
        stats.hp=Mathf.Min(stats.maxHp,stats.hp+Mathf.Max(0f,hit.damage)*Mathf.Clamp01(block.lifesteal));
    }

    private void OnAbilityHit(AOGCombatHitEvent hit)
    {
        if (!BelongsToMe(hit.source) || stats == null || block == null || block.spellVamp <= 0f)
            return;
        stats.hp=Mathf.Min(stats.maxHp,stats.hp+Mathf.Max(0f,hit.damage)*Mathf.Clamp01(block.spellVamp));
    }

    private bool BelongsToMe(GameObject source)
    {
        if (source==null)return false;
        if (source==gameObject||source.transform.IsChildOf(transform))return true;
        AOGCharacterStats parent=source.GetComponentInParent<AOGCharacterStats>();
        return parent==stats;
    }
}

public class AOGCombatStatBootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if(FindFirstObjectByType<AOGCombatStatBootstrap>()!=null)return;
        GameObject host=new GameObject("AOG_Combat_Stat_Bootstrap");DontDestroyOnLoad(host);host.AddComponent<AOGCombatStatBootstrap>();
    }

    private float nextScan;
    private void Update()
    {
        if(Time.unscaledTime<nextScan)return;
        nextScan=Time.unscaledTime+0.75f;
        foreach(AOGCharacterStats hero in FindObjectsByType<AOGCharacterStats>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {
            if(hero==null)continue;
            AOGCombatStatBlock block=hero.GetComponent<AOGCombatStatBlock>();
            if(block==null)block=hero.gameObject.AddComponent<AOGCombatStatBlock>();
            if(hero.GetComponent<AOGAdvancedSustainRuntime>()==null)hero.gameObject.AddComponent<AOGAdvancedSustainRuntime>();
        }
    }
}
