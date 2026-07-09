using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runtime death/respawn/fountain loop for champions.
/// Keeps respawn timing and base return logic outside Champion so competitive rules can evolve independently.
/// </summary>
public class AOGRespawnSystemRuntime : MonoBehaviour
{
    private const string RuntimeName = "AOG_Respawn_System_Runtime";

    [SerializeField] private float baseRespawnSeconds = 6f;
    [SerializeField] private float perLevelRespawnSeconds = 1.1f;
    [SerializeField] private float fountainHealPerSecond = 180f;
    [SerializeField] private float fountainRadius = 8f;

    private readonly Dictionary<Champion, Vector3> fallbackSpawnPoints = new();
    private readonly HashSet<Champion> subscribed = new();
    private Transform blueBase;
    private Transform redBase;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindObjectOfType<AOGRespawnSystemRuntime>() != null)
            return;

        GameObject obj = new GameObject(RuntimeName);
        obj.AddComponent<AOGRespawnSystemRuntime>();
    }

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        ResolveBases();
        DiscoverChampions();
        HealAtFountains();
    }

    private void ResolveBases()
    {
        if (blueBase != null && redBase != null)
            return;

        MinionSpawner spawner = FindObjectOfType<MinionSpawner>();
        if (spawner == null)
            return;

        blueBase = spawner.blueBaseSpawn;
        redBase = spawner.redBaseSpawn;
    }

    private void DiscoverChampions()
    {
        Champion[] champions = Resources.FindObjectsOfTypeAll<Champion>();
        foreach (Champion champion in champions)
        {
            if (champion == null || subscribed.Contains(champion))
                continue;

            subscribed.Add(champion);
            fallbackSpawnPoints[champion] = champion.transform.position;
            champion.OnDeath += () => HandleDeath(champion);
        }
    }

    private void HandleDeath(Champion champion)
    {
        if (champion == null)
            return;

        StartCoroutine(RespawnRoutine(champion));
    }

    private IEnumerator RespawnRoutine(Champion champion)
    {
        float delay = baseRespawnSeconds + champion.Level * perLevelRespawnSeconds;
        yield return new WaitForSecondsRealtime(delay);

        Vector3 spawn = ResolveSpawnPoint(champion);
        champion.transform.position = spawn;
        champion.Revive();
    }

    private Vector3 ResolveSpawnPoint(Champion champion)
    {
        if (champion.Team == TeamType.Blue && blueBase != null)
            return blueBase.position;
        if (champion.Team == TeamType.Red && redBase != null)
            return redBase.position;

        return fallbackSpawnPoints.TryGetValue(champion, out Vector3 fallback)
            ? fallback
            : Vector3.zero;
    }

    private void HealAtFountains()
    {
        Champion[] champions = FindObjectsByType<Champion>(FindObjectsSortMode.None);
        foreach (Champion champion in champions)
        {
            if (champion == null || !champion.IsAlive)
                continue;

            Transform fountain = champion.Team == TeamType.Blue ? blueBase : redBase;
            if (fountain == null)
                continue;

            if (Vector3.Distance(champion.transform.position, fountain.position) <= fountainRadius)
                champion.Heal(fountainHealPerSecond * Time.deltaTime);
        }
    }
}
