using System;
using System.Collections.Generic;
using UnityEngine;

public enum AOGCombatTargetKind
{
    Champion,
    Minion,
    NeutralMonster,
    Tower,
    Nexus,
    Boss
}

public struct AOGCombatHitEvent
{
    public GameObject source;
    public GameObject target;
    public float damage;
    public bool basicAttack;
    public string abilityId;
    public AOGCombatTargetKind targetKind;
}

public struct AOGChampionDeathEvent
{
    public AOGCharacterStats victim;
    public GameObject killer;
    public IReadOnlyList<GameObject> assistants;
}

public static class AOGCombatEvents
{
    public static event Action<AOGCombatHitEvent> BasicAttackHit;
    public static event Action<AOGCombatHitEvent> AbilityHit;
    public static event Action<AOGChampionDeathEvent> ChampionDeath;

    public static void RaiseBasicAttackHit(AOGCombatHitEvent data) => BasicAttackHit?.Invoke(data);
    public static void RaiseAbilityHit(AOGCombatHitEvent data) => AbilityHit?.Invoke(data);
    public static void RaiseChampionDeath(AOGChampionDeathEvent data) => ChampionDeath?.Invoke(data);
}

public class AOGChampionDamageLedger : MonoBehaviour
{
    public float assistWindow = 10f;

    private readonly Dictionary<GameObject, float> recentAttackers = new Dictionary<GameObject, float>();

    public void RegisterDamage(GameObject source)
    {
        if (source == null || source == gameObject)
            return;
        recentAttackers[source] = Time.time;
    }

    public List<GameObject> CollectAssistants(GameObject killer)
    {
        List<GameObject> result = new List<GameObject>();
        List<GameObject> stale = new List<GameObject>();
        foreach (KeyValuePair<GameObject,float> pair in recentAttackers)
        {
            if (pair.Key == null || Time.time - pair.Value > assistWindow)
            {
                stale.Add(pair.Key);
                continue;
            }
            if (pair.Key != killer)
                result.Add(pair.Key);
        }
        foreach (GameObject key in stale)
            recentAttackers.Remove(key);
        return result;
    }

    public void ClearLedger() => recentAttackers.Clear();
}

public class AOGChampionMatchStats : MonoBehaviour
{
    public int kills;
    public int deaths;
    public int assists;
    public int currentKillStreak;

    private void OnEnable()
    {
        AOGCombatEvents.ChampionDeath += OnChampionDeath;
    }

    private void OnDisable()
    {
        AOGCombatEvents.ChampionDeath -= OnChampionDeath;
    }

    private void OnChampionDeath(AOGChampionDeathEvent data)
    {
        if (data.victim != null && data.victim.gameObject == gameObject)
        {
            deaths++;
            currentKillStreak = 0;
        }

        if (data.killer != null && BelongsToThisChampion(data.killer))
        {
            kills++;
            currentKillStreak++;
        }

        if (data.assistants != null)
        {
            foreach (GameObject assistant in data.assistants)
            {
                if (assistant != null && BelongsToThisChampion(assistant))
                {
                    assists++;
                    break;
                }
            }
        }
    }

    private bool BelongsToThisChampion(GameObject source)
    {
        if (source == null)
            return false;
        if (source == gameObject || source.transform.IsChildOf(transform))
            return true;
        AOGCharacterStats parentStats = source.GetComponentInParent<AOGCharacterStats>();
        return parentStats != null && parentStats.gameObject == gameObject;
    }
}

public class AOGCombatEventBootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindFirstObjectByType<AOGCombatEventBootstrap>() != null)
            return;
        GameObject host = new GameObject("AOG_Combat_Event_Bootstrap");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGCombatEventBootstrap>();
    }

    private float nextScan;
    private void Update()
    {
        if (Time.unscaledTime < nextScan)
            return;
        nextScan = Time.unscaledTime + 0.75f;

        foreach (AOGCharacterStats stats in FindObjectsByType<AOGCharacterStats>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (stats == null) continue;
            if (stats.GetComponent<AOGChampionDamageLedger>() == null)
                stats.gameObject.AddComponent<AOGChampionDamageLedger>();
            if (stats.GetComponent<AOGChampionMatchStats>() == null)
                stats.gameObject.AddComponent<AOGChampionMatchStats>();
        }
    }
}
