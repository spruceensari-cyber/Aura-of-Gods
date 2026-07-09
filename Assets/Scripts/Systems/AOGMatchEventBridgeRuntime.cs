using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Bridges champion deaths into game-state scorekeeping, kill feed and announcer events.
/// Attacker attribution is tracked from the latest champion damage source when available.
/// </summary>
public class AOGMatchEventBridgeRuntime : MonoBehaviour
{
    private readonly HashSet<Champion> bound = new();
    private readonly Dictionary<Champion, Champion> lastAttacker = new();
    private readonly Dictionary<Champion, float> lastAttackTime = new();
    private const float AttributionWindow = 8f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindObjectOfType<AOGMatchEventBridgeRuntime>() != null)
            return;
        GameObject obj = new GameObject("AOG_Match_Event_Bridge_Runtime");
        obj.AddComponent<AOGMatchEventBridgeRuntime>();
    }

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        foreach (Champion champion in Resources.FindObjectsOfTypeAll<Champion>())
        {
            if (champion == null || !champion.gameObject.scene.IsValid() || bound.Contains(champion))
                continue;

            bound.Add(champion);
            champion.OnDeath += () => HandleDeath(champion);
        }
    }

    public void RegisterChampionDamage(Champion attacker, Champion victim)
    {
        if (attacker == null || victim == null || attacker == victim)
            return;
        lastAttacker[victim] = attacker;
        lastAttackTime[victim] = Time.time;
    }

    private void HandleDeath(Champion victim)
    {
        Champion killer = null;
        if (lastAttacker.TryGetValue(victim, out Champion tracked) && tracked != null &&
            lastAttackTime.TryGetValue(victim, out float time) && Time.time - time <= AttributionWindow)
            killer = tracked;

        if (killer != null)
        {
            FindObjectOfType<GameStateManager>()?.RecordKill(killer, victim);
            FindObjectOfType<AOGMatchHUDRuntime>()?.AddKillFeed(killer.name, victim.name);
            AOGAnnouncerRuntime announcer = FindObjectOfType<AOGAnnouncerRuntime>();
            announcer?.Announce("ELIMINATION", AOGAudioCue.ChampionDeath, 1.1f);
        }
        else
        {
            FindObjectOfType<AOGMatchHUDRuntime>()?.AddKillFeed("WORLD", victim.name);
        }

        lastAttacker.Remove(victim);
        lastAttackTime.Remove(victim);
    }

    public static void Report(Champion attacker, Champion victim)
    {
        FindObjectOfType<AOGMatchEventBridgeRuntime>()?.RegisterChampionDamage(attacker, victim);
    }
}
