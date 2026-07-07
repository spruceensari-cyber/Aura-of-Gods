using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MinionSpawner : MonoBehaviour
{
    public GameObject blueMinionPrefab;
    public GameObject redMinionPrefab;

    [Header("Base Spawns")]
    public Transform blueBaseSpawn;
    public Transform redBaseSpawn;

    [Header("Lane Waypoints - Blue to Red Order")]
    public Transform[] topLaneWaypoints;
    public Transform[] midLaneWaypoints;
    public Transform[] botLaneWaypoints;

    [Header("Wave Settings")]
    public float waveRate = 30f;
    public float minionDelay = 0.15f;
    public int maxMinionsPerTeam = 45;
    public float sideSpacing = 1.2f;
    public float backSpacing = 1.6f;

    [Header("Projectile")]
    public GameObject projectilePrefab;

    private int waveNumber = 0;

    void Start()
    {
        InvokeRepeating(nameof(StartWave), 2f, waveRate);
    }

    [ContextMenu("Start Wave Now")]
    void StartWave()
    {
        if (blueMinionPrefab == null)
        {
            Debug.LogError("Blue Minion Prefab boş.");
            return;
        }

        if (redMinionPrefab == null)
        {
            Debug.LogError("Red Minion Prefab boş.");
            return;
        }

        if (blueBaseSpawn == null || redBaseSpawn == null)
        {
            Debug.LogError("BlueBaseSpawn veya RedBaseSpawn atanmamış.");
            return;
        }

        if (CountMinions(MinionTeam.Blue) >= maxMinionsPerTeam)
            return;

        if (CountMinions(MinionTeam.Red) >= maxMinionsPerTeam)
            return;

        waveNumber++;

        StartCoroutine(SpawnWave(blueMinionPrefab, MinionTeam.Blue, BuildBluePath(topLaneWaypoints), "Blue Top"));
        StartCoroutine(SpawnWave(blueMinionPrefab, MinionTeam.Blue, BuildBluePath(midLaneWaypoints), "Blue Mid"));
        StartCoroutine(SpawnWave(blueMinionPrefab, MinionTeam.Blue, BuildBluePath(botLaneWaypoints), "Blue Bot"));

        StartCoroutine(SpawnWave(redMinionPrefab, MinionTeam.Red, BuildRedPath(topLaneWaypoints), "Red Top"));
        StartCoroutine(SpawnWave(redMinionPrefab, MinionTeam.Red, BuildRedPath(midLaneWaypoints), "Red Mid"));
        StartCoroutine(SpawnWave(redMinionPrefab, MinionTeam.Red, BuildRedPath(botLaneWaypoints), "Red Bot"));
    }

    IEnumerator SpawnWave(GameObject prefab, MinionTeam team, Vector3[] path, string laneName)
    {
        SpawnMinion(prefab, team, path, MinionRole.Melee, -sideSpacing, 0f, laneName);
        SpawnMinion(prefab, team, path, MinionRole.Melee, 0f, 0f, laneName);
        SpawnMinion(prefab, team, path, MinionRole.Melee, sideSpacing, 0f, laneName);

        yield return new WaitForSeconds(minionDelay);

        SpawnMinion(prefab, team, path, MinionRole.Ranged, -sideSpacing, backSpacing, laneName);
        SpawnMinion(prefab, team, path, MinionRole.Ranged, 0f, backSpacing, laneName);
        SpawnMinion(prefab, team, path, MinionRole.Ranged, sideSpacing, backSpacing, laneName);

        if (waveNumber % 3 == 0)
        {
            yield return new WaitForSeconds(minionDelay);
            SpawnMinion(prefab, team, path, MinionRole.Cannon, 0f, backSpacing * 2f, laneName);
        }
    }

    void SpawnMinion(
        GameObject prefab,
        MinionTeam team,
        Vector3[] path,
        MinionRole role,
        float sideOffset,
        float backOffset,
        string laneName
    )
    {
        if (prefab == null)
        {
            Debug.LogError("Minion prefab boş.");
            return;
        }

        if (path == null || path.Length < 2)
        {
            Debug.LogError("Path eksik: " + laneName + ". Waypointleri GameManager'a bağla.");
            return;
        }

        Vector3 spawnPoint = path[0];
        Vector3 firstTarget = path[1];

        Vector3 forward = firstTarget - spawnPoint;
        forward.y = 0f;

        if (forward.magnitude <= 0.01f)
        {
            Debug.LogError("Spawn ve ilk waypoint aynı yerde: " + laneName);
            return;
        }

        forward.Normalize();

        Vector3 right = new Vector3(forward.z, 0f, -forward.x);
        Vector3 finalSpawn = spawnPoint + right * sideOffset - forward * backOffset;
        finalSpawn.y = spawnPoint.y + 1.2f;

        GameObject obj = Instantiate(prefab, finalSpawn, Quaternion.LookRotation(forward));
        obj.name = team + "_" + laneName + "_" + role + "_Minion";

        Minion m = obj.GetComponent<Minion>();

        if (m == null)
        {
            m = obj.AddComponent<Minion>();
            Debug.LogWarning("Prefab üzerinde Minion scripti yoktu, otomatik eklendi: " + prefab.name);
        }
AOGWorldHealthBar healthBar = obj.GetComponent<AOGWorldHealthBar>();

if (healthBar == null)
{
    healthBar = obj.AddComponent<AOGWorldHealthBar>();
}

healthBar.barOffset = new Vector3(0f, 1.6f, 0f);
healthBar.barWidth = 0.8f;
healthBar.barHeight = 0.08f;
        m.team = team;
        m.role = role;
        m.path = path;
        m.currentPathIndex = 1;
        m.laneWidth = 1.0f;
        m.projectilePrefab = projectilePrefab;

        Vector3 originalScale = obj.transform.localScale;

        if (role == MinionRole.Melee)
        {
            m.maxHp = 65f;
            m.hp = 65f;
            m.damage = 8f;
            m.attackRange = 2.2f;
            m.attackRate = 1.1f;
            m.speed = 3.1f;

            obj.transform.localScale = originalScale * 1f;
        }
        else if (role == MinionRole.Ranged)
        {
            m.maxHp = 45f;
            m.hp = 45f;
            m.damage = 7f;
            m.attackRange = 6f;
            m.attackRate = 1.3f;
            m.speed = 2.9f;

            obj.transform.localScale = originalScale * 0.9f;
        }
        else if (role == MinionRole.Cannon)
        {
            m.maxHp = 140f;
            m.hp = 140f;
            m.damage = 18f;
            m.attackRange = 7f;
            m.attackRate = 1.6f;
            m.speed = 2.4f;

            obj.transform.localScale = new Vector3(
                originalScale.x * 1.6f,
                originalScale.y * 1.2f,
                originalScale.z * 1.6f
            );
        }

        Debug.Log("Spawn edildi: " + obj.name + " | Lane: " + laneName + " | Path Length: " + path.Length);
    }

    Vector3[] BuildBluePath(Transform[] laneWaypoints)
    {
        List<Vector3> path = new List<Vector3>();

        if (blueBaseSpawn == null || redBaseSpawn == null)
        {
            Debug.LogError("BlueBaseSpawn veya RedBaseSpawn atanmamış.");
            return path.ToArray();
        }

        path.Add(blueBaseSpawn.position);

        if (laneWaypoints != null)
        {
            foreach (Transform t in laneWaypoints)
            {
                if (t != null)
                    path.Add(t.position);
            }
        }

        path.Add(redBaseSpawn.position);

        return path.ToArray();
    }

    Vector3[] BuildRedPath(Transform[] laneWaypoints)
    {
        List<Vector3> path = new List<Vector3>();

        if (blueBaseSpawn == null || redBaseSpawn == null)
        {
            Debug.LogError("BlueBaseSpawn veya RedBaseSpawn atanmamış.");
            return path.ToArray();
        }

        path.Add(redBaseSpawn.position);

        if (laneWaypoints != null)
        {
            for (int i = laneWaypoints.Length - 1; i >= 0; i--)
            {
                if (laneWaypoints[i] != null)
                    path.Add(laneWaypoints[i].position);
            }
        }

        path.Add(blueBaseSpawn.position);

        return path.ToArray();
    }

    int CountMinions(MinionTeam team)
    {
        Minion[] all = FindObjectsByType<Minion>(FindObjectsSortMode.None);

        int count = 0;

        foreach (Minion m in all)
        {
            if (m != null && m.team == team)
                count++;
        }

        return count;
    }
}