using System;
using UnityEngine;

/// <summary>
/// Tracks the latest ability/basic-attack damage intent on a champion so legacy damage calls can
/// be routed through AOGDamagePacket without rewriting every existing ability implementation.
/// </summary>
public class AOGDamageIntentRuntime : MonoBehaviour
{
    private AOGDamageType abilityType = AOGDamageType.Physical;
    private string abilityId = "legacy";
    private float abilityExpiresAt;
    private float basicAttackUntil;

    public void BeginAbility(int slot)
    {
        string championId = ResolveChampionId();
        abilityType = ResolveType(championId,slot);
        abilityId = championId + "_slot_" + slot;
        abilityExpiresAt = Time.time + ResolveIntentWindow(championId,slot);
    }

    public void BeginBasicAttack()
    {
        basicAttackUntil = Time.time + 0.72f;
    }

    public bool TryResolve(out AOGDamageType type,out string resolvedAbilityId)
    {
        if (Time.time <= basicAttackUntil)
        {
            type = AOGDamageType.Physical;
            resolvedAbilityId = "basic_attack";
            return true;
        }

        if (Time.time <= abilityExpiresAt)
        {
            type = abilityType;
            resolvedAbilityId = abilityId;
            return true;
        }

        type = AOGDamageType.Physical;
        resolvedAbilityId = "legacy_physical";
        return false;
    }

    private string ResolveChampionId()
    {
        AOGActiveChampion active = GetComponent<AOGActiveChampion>();
        if (active != null && !string.IsNullOrEmpty(active.championId)) return active.championId.ToLowerInvariant();
        AOGTeamMemberIdentity member = GetComponent<AOGTeamMemberIdentity>();
        if (member != null && !string.IsNullOrEmpty(member.championId)) return member.championId.ToLowerInvariant();
        return gameObject.name.ToLowerInvariant();
    }

    private static AOGDamageType ResolveType(string id,int slot)
    {
        if (id.Contains("lyra") || id.Contains("nyra") || id.Contains("pyrelle") || id.Contains("selene") || id.Contains("seris") || id.Contains("mireva"))
            return AOGDamageType.Magic;

        if (id.Contains("kaelith"))
            return slot == 2 ? AOGDamageType.Physical : AOGDamageType.Magic;

        if (id.Contains("auron"))
            return slot == 3 ? AOGDamageType.Magic : AOGDamageType.Physical;

        if (id.Contains("nocthyr") && slot == 3)
            return AOGDamageType.True;

        return AOGDamageType.Physical;
    }

    private static float ResolveIntentWindow(string id,int slot)
    {
        if ((id.Contains("pyrelle") || id.Contains("selene")) && slot == 1) return 6.5f;
        if (slot == 3) return 5.0f;
        if (slot == 1) return 3.5f;
        return 2.25f;
    }
}

public struct AOGResolvedDamageEvent
{
    public GameObject source;
    public AOGCharacterStats target;
    public float rawAmount;
    public float resolvedAmount;
    public AOGDamageType damageType;
    public string abilityId;
}

public static class AOGDamageResolvedEvents
{
    public static event Action<AOGResolvedDamageEvent> DamageResolved;

    public static void Raise(AOGResolvedDamageEvent data)
    {
        DamageResolved?.Invoke(data);
    }
}

[DefaultExecutionOrder(-590)]
public class AOGDamageIntentBootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindFirstObjectByType<AOGDamageIntentBootstrap>() != null) return;
        GameObject host = new GameObject("AOG_Damage_Intent_Bootstrap");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGDamageIntentBootstrap>();
    }

    private float nextScan;
    private void Update()
    {
        if (Time.unscaledTime < nextScan) return;
        nextScan = Time.unscaledTime + 1.0f;

        foreach (AOGCharacterStats stats in AOGWorldRegistry.Characters)
        {
            if (stats == null) continue;
            if (stats.GetComponent<AOGDamageIntentRuntime>() == null)
                stats.gameObject.AddComponent<AOGDamageIntentRuntime>();
        }
    }
}
