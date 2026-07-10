using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Central runtime registries for frequently queried world objects.
/// Objects register through lightweight handles; one low-frequency discovery pass attaches
/// handles to objects created by legacy/runtime systems that do not register themselves yet.
/// </summary>
public static class AOGWorldRegistry
{
    private static readonly HashSet<AOGCharacterStats> characters = new HashSet<AOGCharacterStats>();
    private static readonly HashSet<AOGTeamMemberIdentity> teamMembers = new HashSet<AOGTeamMemberIdentity>();
    private static readonly HashSet<AOGNeutralMonsterRuntime> neutralMonsters = new HashSet<AOGNeutralMonsterRuntime>();
    private static readonly HashSet<AOGNeutralBossAI> bosses = new HashSet<AOGNeutralBossAI>();
    private static readonly HashSet<TowerHealth> towers = new HashSet<TowerHealth>();
    private static readonly HashSet<AOGStrategicLaneSeal> seals = new HashSet<AOGStrategicLaneSeal>();
    private static readonly HashSet<AOGNexusCore> nexuses = new HashSet<AOGNexusCore>();

    public static IEnumerable<AOGCharacterStats> Characters => characters;
    public static IEnumerable<AOGTeamMemberIdentity> TeamMembers => teamMembers;
    public static IEnumerable<AOGNeutralMonsterRuntime> NeutralMonsters => neutralMonsters;
    public static IEnumerable<AOGNeutralBossAI> Bosses => bosses;
    public static IEnumerable<TowerHealth> Towers => towers;
    public static IEnumerable<AOGStrategicLaneSeal> Seals => seals;
    public static IEnumerable<AOGNexusCore> Nexuses => nexuses;

    internal static void Register(AOGWorldRegistryHandle handle)
    {
        if (handle == null) return;
        AOGCharacterStats character = handle.GetComponent<AOGCharacterStats>(); if (character != null) characters.Add(character);
        AOGTeamMemberIdentity member = handle.GetComponent<AOGTeamMemberIdentity>(); if (member != null) teamMembers.Add(member);
        AOGNeutralMonsterRuntime monster = handle.GetComponent<AOGNeutralMonsterRuntime>(); if (monster != null) neutralMonsters.Add(monster);
        AOGNeutralBossAI boss = handle.GetComponent<AOGNeutralBossAI>(); if (boss != null) bosses.Add(boss);
        TowerHealth tower = handle.GetComponent<TowerHealth>(); if (tower != null) towers.Add(tower);
        AOGStrategicLaneSeal seal = handle.GetComponent<AOGStrategicLaneSeal>(); if (seal != null) seals.Add(seal);
        AOGNexusCore nexus = handle.GetComponent<AOGNexusCore>(); if (nexus != null) nexuses.Add(nexus);
    }

    internal static void Unregister(AOGWorldRegistryHandle handle)
    {
        if (handle == null) return;
        AOGCharacterStats character = handle.GetComponent<AOGCharacterStats>(); if (character != null) characters.Remove(character);
        AOGTeamMemberIdentity member = handle.GetComponent<AOGTeamMemberIdentity>(); if (member != null) teamMembers.Remove(member);
        AOGNeutralMonsterRuntime monster = handle.GetComponent<AOGNeutralMonsterRuntime>(); if (monster != null) neutralMonsters.Remove(monster);
        AOGNeutralBossAI boss = handle.GetComponent<AOGNeutralBossAI>(); if (boss != null) bosses.Remove(boss);
        TowerHealth tower = handle.GetComponent<TowerHealth>(); if (tower != null) towers.Remove(tower);
        AOGStrategicLaneSeal seal = handle.GetComponent<AOGStrategicLaneSeal>(); if (seal != null) seals.Remove(seal);
        AOGNexusCore nexus = handle.GetComponent<AOGNexusCore>(); if (nexus != null) nexuses.Remove(nexus);
    }

    internal static void PruneNulls()
    {
        characters.RemoveWhere(x => x == null);
        teamMembers.RemoveWhere(x => x == null);
        neutralMonsters.RemoveWhere(x => x == null);
        bosses.RemoveWhere(x => x == null);
        towers.RemoveWhere(x => x == null);
        seals.RemoveWhere(x => x == null);
        nexuses.RemoveWhere(x => x == null);
    }
}

public class AOGWorldRegistryHandle : MonoBehaviour
{
    private void OnEnable() { AOGWorldRegistry.Register(this); }
    private void OnDisable() { AOGWorldRegistry.Unregister(this); }
}

[DefaultExecutionOrder(-650)]
public class AOGWorldRegistryBootstrap : MonoBehaviour
{
    private float nextDiscovery;
    private float nextPrune;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindFirstObjectByType<AOGWorldRegistryBootstrap>() != null) return;
        GameObject host = new GameObject("AOG_World_Registry_Bootstrap");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGWorldRegistryBootstrap>();
    }

    private void Update()
    {
        if (Time.unscaledTime >= nextDiscovery)
        {
            nextDiscovery = Time.unscaledTime + 2.0f;
            DiscoverUnregisteredObjects();
        }
        if (Time.unscaledTime >= nextPrune)
        {
            nextPrune = Time.unscaledTime + 5f;
            AOGWorldRegistry.PruneNulls();
        }
    }

    private void DiscoverUnregisteredObjects()
    {
        AttachHandles(FindObjectsByType<AOGCharacterStats>(FindObjectsInactive.Include,FindObjectsSortMode.None));
        AttachHandles(FindObjectsByType<AOGTeamMemberIdentity>(FindObjectsInactive.Include,FindObjectsSortMode.None));
        AttachHandles(FindObjectsByType<AOGNeutralMonsterRuntime>(FindObjectsInactive.Include,FindObjectsSortMode.None));
        AttachHandles(FindObjectsByType<AOGNeutralBossAI>(FindObjectsInactive.Include,FindObjectsSortMode.None));
        AttachHandles(FindObjectsByType<TowerHealth>(FindObjectsInactive.Include,FindObjectsSortMode.None));
        AttachHandles(FindObjectsByType<AOGStrategicLaneSeal>(FindObjectsInactive.Include,FindObjectsSortMode.None));
        AttachHandles(FindObjectsByType<AOGNexusCore>(FindObjectsInactive.Include,FindObjectsSortMode.None));
    }

    private static void AttachHandles<T>(T[] objects) where T : Component
    {
        foreach (T component in objects)
        {
            if (component == null) continue;
            if (component.GetComponent<AOGWorldRegistryHandle>() == null)
                component.gameObject.AddComponent<AOGWorldRegistryHandle>();
        }
    }
}
