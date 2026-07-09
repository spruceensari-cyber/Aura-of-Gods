using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum AOGBotRole { Top, Jungle, Mid, Carry, Support }

public class AOGBotChampionAI : MonoBehaviour
{
    public AOGBotRole role;
    public MinionTeam team;
    public Transform[] lanePath;
    public float decisionInterval = 0.20f;
    public float heroAggroRange = 8.5f;
    public float retreatHpRatio = 0.28f;
    public float abilityRange = 7.0f;

    private AOGCharacterStats stats;
    private ChampionPresentationController presentation;
    private int pathIndex;
    private float nextDecision;
    private float nextAttack;
    private float nextAbility;
    private Transform currentTarget;
    private Vector3 spawnPoint;

    private void Awake()
    {
        stats = GetComponent<AOGCharacterStats>();
        presentation = GetComponent<ChampionPresentationController>();
        spawnPoint = transform.position;
    }

    private void Update()
    {
        if (stats == null || stats.IsDead) return;
        if (AOGMatchDirector.Instance == null || AOGMatchDirector.Instance.State != AOGMatchState.Playing) return;

        if (Time.time >= nextDecision)
        {
            nextDecision = Time.time + decisionInterval;
            Think();
        }

        Act();
    }

    private void Think()
    {
        float hpRatio = stats.hp / Mathf.Max(1f, stats.maxHp);
        if (hpRatio <= retreatHpRatio)
        {
            currentTarget = null;
            return;
        }

        AOGCharacterStats enemyHero = FindNearestEnemyHero(heroAggroRange);
        if (enemyHero != null)
        {
            currentTarget = enemyHero.transform;
            return;
        }

        Minion enemyMinion = FindNearestEnemyMinion(7f);
        if (enemyMinion != null)
        {
            currentTarget = enemyMinion.transform;
            return;
        }

        TowerHealth tower = FindNearestEnemyTower(10f);
        if (tower != null)
        {
            currentTarget = tower.transform;
            return;
        }

        currentTarget = null;
    }

    private void Act()
    {
        float hpRatio = stats.hp / Mathf.Max(1f, stats.maxHp);
        if (hpRatio <= retreatHpRatio)
        {
            MoveToward(spawnPoint, stats.moveSpeed * 0.9f);
            return;
        }

        if (currentTarget != null && currentTarget.gameObject.activeInHierarchy)
        {
            float distance = FlatDistance(transform.position, currentTarget.position);
            float range = stats.attackRange;

            if (distance > range)
            {
                MoveToward(currentTarget.position, stats.moveSpeed);
                if (distance <= abilityRange && Time.time >= nextAbility)
                    UseAbility(currentTarget.position);
            }
            else
            {
                Face(currentTarget.position);
                if (Time.time >= nextAttack)
                {
                    nextAttack = Time.time + stats.attackCooldown;
                    presentation?.PlayBasicAttack();
                    ResolveBasicAttack(currentTarget);
                }
            }
            return;
        }

        FollowLane();
    }

    private void FollowLane()
    {
        if (lanePath == null || lanePath.Length == 0)
            return;

        pathIndex = Mathf.Clamp(pathIndex, 0, lanePath.Length - 1);
        Transform target = lanePath[pathIndex];
        if (target == null) return;

        if (FlatDistance(transform.position, target.position) < 1.5f && pathIndex < lanePath.Length - 1)
            pathIndex++;

        MoveToward(lanePath[pathIndex].position, stats.moveSpeed * 0.92f);
    }

    private void UseAbility(Vector3 point)
    {
        nextAbility = Time.time + 5.5f + (int)role * 0.25f;
        presentation?.PlayAbility((int)role % 3);
        Color c = team == MinionTeam.Blue ? new Color(0.18f, 0.58f, 1f) : new Color(1f, 0.18f, 0.24f);
        GameObject ring = AOGAbilityVisuals.CreateRing("Bot_Ability_Telegraph", point + Vector3.up * 0.06f, 2.2f, c, 0.10f);
        Destroy(ring, 0.42f);

        foreach (Collider hit in Physics.OverlapSphere(point, 2.2f, ~0, QueryTriggerInteraction.Ignore))
        {
            Minion minion = hit.GetComponentInParent<Minion>();
            if (minion != null && minion.team != team) minion.TakeDamage(stats.attackDamage * 1.35f, gameObject);

            AOGCharacterStats hero = hit.GetComponentInParent<AOGCharacterStats>();
            if (hero != null && hero != stats && hero.team != team) hero.TakeDamage(stats.attackDamage * 1.10f);
        }
    }

    private void ResolveBasicAttack(Transform target)
    {
        Minion minion = target.GetComponentInParent<Minion>();
        if (minion != null && minion.team != team)
        {
            minion.TakeDamage(stats.attackDamage, gameObject);
            return;
        }

        AOGCharacterStats hero = target.GetComponentInParent<AOGCharacterStats>();
        if (hero != null && hero != stats && hero.team != team)
        {
            hero.TakeDamage(stats.attackDamage);
            return;
        }

        TowerHealth tower = target.GetComponentInParent<TowerHealth>();
        if (tower != null && tower.towerTeam != team)
            tower.TakeDamage(stats.attackDamage * 0.75f);
    }

    private AOGCharacterStats FindNearestEnemyHero(float range)
    {
        AOGCharacterStats best = null;
        float bestDist = range;
        foreach (AOGCharacterStats hero in FindObjectsByType<AOGCharacterStats>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (hero == null || hero == stats || hero.IsDead || hero.team == team) continue;
            float d = FlatDistance(transform.position, hero.transform.position);
            if (d < bestDist) { best = hero; bestDist = d; }
        }
        return best;
    }

    private Minion FindNearestEnemyMinion(float range)
    {
        Minion best = null;
        float bestDist = range;
        foreach (Minion minion in Minion.Active)
        {
            if (minion == null || minion.hp <= 0f || minion.team == team) continue;
            float d = FlatDistance(transform.position, minion.transform.position);
            if (d < bestDist) { best = minion; bestDist = d; }
        }
        return best;
    }

    private TowerHealth FindNearestEnemyTower(float range)
    {
        TowerHealth best = null;
        float bestDist = range;
        foreach (TowerHealth tower in FindObjectsByType<TowerHealth>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (tower == null || tower.hp <= 0f || tower.towerTeam == team) continue;
            float d = FlatDistance(transform.position, tower.transform.position);
            if (d < bestDist) { best = tower; bestDist = d; }
        }
        return best;
    }

    private void MoveToward(Vector3 point, float speed)
    {
        Vector3 d = point - transform.position;
        d.y = 0f;
        if (d.sqrMagnitude < 0.01f) return;
        transform.position += d.normalized * speed * Time.deltaTime;
        Face(point);
        presentation?.SetPlanarVelocity(d.normalized * speed);
    }

    private void Face(Vector3 point)
    {
        Vector3 d = point - transform.position;
        d.y = 0f;
        if (d.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d.normalized), 10f * Time.deltaTime);
    }

    private static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f; b.y = 0f; return Vector3.Distance(a, b);
    }
}

[DefaultExecutionOrder(-700)]
public class AOGFiveVFiveBotRuntime : MonoBehaviour
{
    private bool spawned;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        GameObject host = new GameObject("AOG_5v5_Bot_Runtime");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGFiveVFiveBotRuntime>();
    }

    private void Update()
    {
        if (spawned) return;
        if (AOGMatchDirector.Instance == null || AOGMatchDirector.Instance.State != AOGMatchState.Playing) return;

        MinionSpawner spawner = FindFirstObjectByType<MinionSpawner>();
        if (spawner == null) return;

        spawned = true;
        SpawnTeam(MinionTeam.Blue, spawner, 4);
        SpawnTeam(MinionTeam.Red, spawner, 5);
    }

    private void SpawnTeam(MinionTeam team, MinionSpawner spawner, int count)
    {
        AOGBotRole[] roles = { AOGBotRole.Top, AOGBotRole.Jungle, AOGBotRole.Mid, AOGBotRole.Carry, AOGBotRole.Support };
        for (int i = 0; i < count; i++)
        {
            int roleIndex = team == MinionTeam.Blue ? i + 1 : i;
            AOGBotRole role = roles[Mathf.Clamp(roleIndex, 0, roles.Length - 1)];
            CreateBot(team, role, spawner, i);
        }
    }

    private void CreateBot(MinionTeam team, AOGBotRole role, MinionSpawner spawner, int index)
    {
        GameObject bot = new GameObject(team + "_Bot_" + role);
        Transform spawn = team == MinionTeam.Blue ? spawner.blueBaseSpawn : spawner.redBaseSpawn;
        Vector3 origin = spawn != null ? spawn.position : Vector3.zero;
        bot.transform.position = origin + new Vector3((index - 2) * 1.6f, 0.2f, team == MinionTeam.Blue ? 2.5f : -2.5f);

        Color accent = RoleColor(role, team);
        BuildBotVisual(bot.transform, role, accent);

        AOGCharacterStats stats = bot.AddComponent<AOGCharacterStats>();
        stats.team = team;
        stats.maxHp = role == AOGBotRole.Top ? 1300f : role == AOGBotRole.Support ? 1100f : 900f;
        stats.hp = stats.maxHp;
        stats.moveSpeed = role == AOGBotRole.Jungle ? 6.6f : 6.0f;
        stats.attackDamage = role == AOGBotRole.Carry ? 72f : role == AOGBotRole.Top ? 68f : 58f;
        stats.attackRange = role == AOGBotRole.Carry || role == AOGBotRole.Support ? 6.0f : 2.6f;
        stats.attackCooldown = role == AOGBotRole.Carry ? 0.82f : 1.0f;

        bot.AddComponent<ChampionAudioController>();
        bot.AddComponent<ChampionPresentationController>();
        bot.AddComponent<AOGAutoAttackPresentationRuntime>();
        if (stats.attackRange <= 3.5f) bot.AddComponent<AOGPremiumMeleeSwingRuntime>();

        CapsuleCollider capsule = bot.AddComponent<CapsuleCollider>();
        capsule.center = new Vector3(0f, 1.1f, 0f);
        capsule.height = 2.4f;
        capsule.radius = 0.65f;
        Rigidbody rb = bot.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        AOGBotChampionAI ai = bot.AddComponent<AOGBotChampionAI>();
        ai.role = role;
        ai.team = team;
        ai.lanePath = ResolveLanePath(role, spawner, team);

        AOGWorldHealthBar bar = bot.AddComponent<AOGWorldHealthBar>();
        bar.barOffset = new Vector3(0f, 3.1f, 0f);
        bar.barWidth = 1.3f;
        bar.barHeight = 0.11f;
    }

    private static Transform[] ResolveLanePath(AOGBotRole role, MinionSpawner spawner, MinionTeam team)
    {
        Transform[] source = role == AOGBotRole.Top ? spawner.topLaneWaypoints : role == AOGBotRole.Mid || role == AOGBotRole.Jungle ? spawner.midLaneWaypoints : spawner.botLaneWaypoints;
        if (source == null) return new Transform[0];
        if (team == MinionTeam.Blue) return source;

        Transform[] reversed = new Transform[source.Length];
        for (int i = 0; i < source.Length; i++) reversed[i] = source[source.Length - 1 - i];
        return reversed;
    }

    private static void BuildBotVisual(Transform parent, AOGBotRole role, Color accent)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        Material armor = new Material(shader) { color = Color.Lerp(new Color(0.03f, 0.04f, 0.06f), accent, 0.22f) };
        Material energy = new Material(shader) { color = accent };
        if (energy.HasProperty("_EmissionColor")) { energy.EnableKeyword("_EMISSION"); energy.SetColor("_EmissionColor", accent * 3.5f); }

        GameObject root = new GameObject("Bot_Visual"); root.transform.SetParent(parent, false);
        CreatePrimitive(PrimitiveType.Capsule, "Body", root.transform, new Vector3(0f,1.2f,0f), new Vector3(0.68f,1.0f,0.54f), armor);
        CreatePrimitive(PrimitiveType.Sphere, "Head", root.transform, new Vector3(0f,2.48f,0f), new Vector3(0.52f,0.58f,0.52f), armor);

        if (role == AOGBotRole.Carry || role == AOGBotRole.Support)
        {
            Transform weapon = CreatePrimitive(PrimitiveType.Cube, "Staff_Weapon", root.transform, new Vector3(0.78f,1.25f,0.2f), new Vector3(0.10f,1.2f,0.10f), energy).transform;
            weapon.localRotation = Quaternion.Euler(0f,0f,-12f);
        }
        else
        {
            Transform weapon = CreatePrimitive(PrimitiveType.Cube, "Sword_Weapon", root.transform, new Vector3(0.82f,1.18f,0.18f), new Vector3(0.13f,1.1f,0.18f), energy).transform;
            weapon.localRotation = Quaternion.Euler(12f,0f,-10f);
        }
    }

    private static GameObject CreatePrimitive(PrimitiveType type, string name, Transform parent, Vector3 pos, Vector3 scale, Material mat)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = mat;
        Collider c = go.GetComponent<Collider>(); if (c != null) Destroy(c);
        return go;
    }

    private static Color RoleColor(AOGBotRole role, MinionTeam team)
    {
        Color baseColor = team == MinionTeam.Blue ? new Color(0.18f,0.58f,1f) : new Color(1f,0.18f,0.24f);
        switch (role)
        {
            case AOGBotRole.Top: return Color.Lerp(baseColor, new Color(1f,0.65f,0.18f), 0.45f);
            case AOGBotRole.Jungle: return Color.Lerp(baseColor, new Color(0.28f,0.85f,0.35f), 0.45f);
            case AOGBotRole.Mid: return Color.Lerp(baseColor, new Color(0.65f,0.25f,0.95f), 0.45f);
            case AOGBotRole.Carry: return Color.Lerp(baseColor, new Color(0.18f,0.90f,0.95f), 0.45f);
            default: return Color.Lerp(baseColor, Color.white, 0.45f);
        }
    }
}
