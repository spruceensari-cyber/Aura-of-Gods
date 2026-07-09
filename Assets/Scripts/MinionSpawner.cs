using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    public float waveRate = 26f;
    public float minionDelay = 0.30f;
    public int maxMinionsPerTeam = 54;
    public float sideSpacing = 1.0f;
    public float backSpacing = 1.45f;

    [Header("Projectile")]
    public GameObject projectilePrefab;

    private int waveNumber;
    private Coroutine waveLoop;

    private void Start()
    {
        waveLoop = StartCoroutine(WaveLoop());
    }

    private IEnumerator WaveLoop()
    {
        while (AOGMatchDirector.Instance == null || AOGMatchDirector.Instance.State != AOGMatchState.Playing)
            yield return new WaitForSecondsRealtime(0.20f);

        yield return new WaitForSeconds(1.5f);

        while (true)
        {
            StartWave();
            yield return new WaitForSeconds(waveRate);
        }
    }

    [ContextMenu("Start Wave Now")]
    private void StartWave()
    {
        if (blueMinionPrefab == null || redMinionPrefab == null)
        {
            Debug.LogError("Minion prefabs are not assigned.");
            return;
        }

        if (blueBaseSpawn == null || redBaseSpawn == null)
        {
            Debug.LogError("BlueBaseSpawn or RedBaseSpawn is missing.");
            return;
        }

        if (CountMinions(MinionTeam.Blue) >= maxMinionsPerTeam || CountMinions(MinionTeam.Red) >= maxMinionsPerTeam)
            return;

        waveNumber++;

        StartCoroutine(SpawnWave(blueMinionPrefab, MinionTeam.Blue, BuildBluePath(topLaneWaypoints), "Blue Top"));
        StartCoroutine(SpawnWave(blueMinionPrefab, MinionTeam.Blue, BuildBluePath(midLaneWaypoints), "Blue Mid"));
        StartCoroutine(SpawnWave(blueMinionPrefab, MinionTeam.Blue, BuildBluePath(botLaneWaypoints), "Blue Bot"));

        StartCoroutine(SpawnWave(redMinionPrefab, MinionTeam.Red, BuildRedPath(topLaneWaypoints), "Red Top"));
        StartCoroutine(SpawnWave(redMinionPrefab, MinionTeam.Red, BuildRedPath(midLaneWaypoints), "Red Mid"));
        StartCoroutine(SpawnWave(redMinionPrefab, MinionTeam.Red, BuildRedPath(botLaneWaypoints), "Red Bot"));
    }

    private IEnumerator SpawnWave(GameObject prefab, MinionTeam team, Vector3[] path, string laneName)
    {
        SpawnMinion(prefab, team, path, MinionRole.Melee, -sideSpacing, 0f, laneName);
        yield return new WaitForSeconds(minionDelay * 0.35f);
        SpawnMinion(prefab, team, path, MinionRole.Melee, 0f, 0f, laneName);
        yield return new WaitForSeconds(minionDelay * 0.35f);
        SpawnMinion(prefab, team, path, MinionRole.Melee, sideSpacing, 0f, laneName);

        yield return new WaitForSeconds(minionDelay);

        SpawnMinion(prefab, team, path, MinionRole.Ranged, -sideSpacing, backSpacing, laneName);
        yield return new WaitForSeconds(minionDelay * 0.35f);
        SpawnMinion(prefab, team, path, MinionRole.Ranged, 0f, backSpacing, laneName);
        yield return new WaitForSeconds(minionDelay * 0.35f);
        SpawnMinion(prefab, team, path, MinionRole.Ranged, sideSpacing, backSpacing, laneName);

        if (waveNumber % 3 == 0)
        {
            yield return new WaitForSeconds(minionDelay);
            SpawnMinion(prefab, team, path, MinionRole.Cannon, 0f, backSpacing * 2f, laneName);
        }
    }

    private void SpawnMinion(GameObject prefab, MinionTeam team, Vector3[] path, MinionRole role, float sideOffset, float backOffset, string laneName)
    {
        if (prefab == null || path == null || path.Length < 2)
            return;

        Vector3 spawnPoint = path[0];
        Vector3 firstTarget = path[1];
        Vector3 forward = firstTarget - spawnPoint;
        forward.y = 0f;
        if (forward.sqrMagnitude <= 0.0001f)
            return;

        forward.Normalize();
        Vector3 right = new Vector3(forward.z, 0f, -forward.x);
        Vector3 finalSpawn = spawnPoint + right * sideOffset - forward * backOffset;
        finalSpawn.y = spawnPoint.y + 0.15f;

        GameObject obj = Instantiate(prefab, finalSpawn, Quaternion.LookRotation(forward));
        obj.name = team + "_" + laneName.Replace(" ", "_") + "_" + role + "_Minion";

        Minion minion = obj.GetComponent<Minion>();
        if (minion == null)
            minion = obj.AddComponent<Minion>();

        minion.team = team;
        minion.role = role;
        minion.path = path;
        minion.currentPathIndex = 1;
        minion.laneWidth = sideSpacing * 2.2f;
        minion.laneOffset = sideOffset;
        minion.projectilePrefab = projectilePrefab;

        float waveScaling = 1f + Mathf.Clamp(waveNumber - 1, 0, 30) * 0.018f;

        if (role == MinionRole.Melee)
        {
            minion.maxHp = 78f * waveScaling;
            minion.damage = 9f * waveScaling;
            minion.attackRange = 1.85f;
            minion.towerAttackDistance = 2.55f;
            minion.attackRate = 1.08f;
            minion.speed = 3.15f;
            minion.attackWindup = 0.26f;
        }
        else if (role == MinionRole.Ranged)
        {
            minion.maxHp = 52f * waveScaling;
            minion.damage = 8f * waveScaling;
            minion.attackRange = 5.8f;
            minion.towerAttackDistance = 5.9f;
            minion.attackRate = 1.30f;
            minion.speed = 3.0f;
            minion.attackWindup = 0.34f;
        }
        else
        {
            minion.maxHp = 185f * waveScaling;
            minion.damage = 22f * waveScaling;
            minion.attackRange = 6.6f;
            minion.towerAttackDistance = 6.8f;
            minion.attackRate = 1.62f;
            minion.speed = 2.55f;
            minion.attackWindup = 0.46f;
        }

        minion.hp = minion.maxHp;

        AOGWorldHealthBar healthBar = obj.GetComponent<AOGWorldHealthBar>();
        if (healthBar == null)
            healthBar = obj.AddComponent<AOGWorldHealthBar>();
        healthBar.barOffset = new Vector3(0f, role == MinionRole.Cannon ? 2.25f : 2.05f, 0f);
        healthBar.barWidth = role == MinionRole.Cannon ? 1.35f : 1.05f;
        healthBar.barHeight = 0.105f;

        AOGMinionVisualFactory.Build(minion);
    }

    private Vector3[] BuildBluePath(Transform[] laneWaypoints)
    {
        List<Vector3> path = new List<Vector3>();
        if (blueBaseSpawn == null || redBaseSpawn == null)
            return path.ToArray();

        path.Add(blueBaseSpawn.position);
        if (laneWaypoints != null)
        {
            foreach (Transform waypoint in laneWaypoints)
                if (waypoint != null) path.Add(waypoint.position);
        }
        path.Add(redBaseSpawn.position);
        return path.ToArray();
    }

    private Vector3[] BuildRedPath(Transform[] laneWaypoints)
    {
        List<Vector3> path = new List<Vector3>();
        if (blueBaseSpawn == null || redBaseSpawn == null)
            return path.ToArray();

        path.Add(redBaseSpawn.position);
        if (laneWaypoints != null)
        {
            for (int i = laneWaypoints.Length - 1; i >= 0; i--)
                if (laneWaypoints[i] != null) path.Add(laneWaypoints[i].position);
        }
        path.Add(blueBaseSpawn.position);
        return path.ToArray();
    }

    private static int CountMinions(MinionTeam team)
    {
        int count = 0;
        foreach (Minion minion in Minion.Active)
        {
            if (minion != null && minion.team == team && minion.hp > 0f)
                count++;
        }
        return count;
    }
}
