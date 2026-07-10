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
    private AOGCharacterStats protectedCarry;

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
        if (hpRatio <= EffectiveRetreatRatio())
        {
            currentTarget = null;
            return;
        }

        if (role == AOGBotRole.Support)
        {
            protectedCarry = FindAlliedCarry(18f);
            AOGCharacterStats threat = protectedCarry != null ? FindEnemyNearPoint(protectedCarry.transform.position, 7.5f) : null;
            currentTarget = threat != null ? threat.transform : null;
            return;
        }

        if (role == AOGBotRole.Carry)
        {
            AOGCharacterStats threat = FindNearestEnemyHero(heroAggroRange);
            if (threat != null)
            {
                currentTarget = threat.transform;
                return;
            }

            Minion safeFarm = FindNearestEnemyMinion(8.5f);
            if (safeFarm != null)
            {
                currentTarget = safeFarm.transform;
                return;
            }

            TowerHealth safeTower = FindNearestEnemyTower(11f);
            currentTarget = safeTower != null ? safeTower.transform : null;
            return;
        }

        if (role == AOGBotRole.Mid)
        {
            AOGCharacterStats enemyHero = FindNearestEnemyHero(heroAggroRange + 1.5f);
            if (enemyHero != null)
            {
                currentTarget = enemyHero.transform;
                return;
            }

            Minion waveTarget = FindNearestEnemyMinion(8f);
            if (waveTarget != null)
            {
                currentTarget = waveTarget.transform;
                return;
            }

            currentTarget = null;
            return;
        }

        AOGCharacterStats enemy = FindNearestEnemyHero(heroAggroRange);
        if (enemy != null)
        {
            currentTarget = enemy.transform;
            return;
        }

        Minion minion = FindNearestEnemyMinion(7f);
        if (minion != null)
        {
            currentTarget = minion.transform;
            return;
        }

        TowerHealth tower = FindNearestEnemyTower(role == AOGBotRole.Top ? 12f : 10f);
        currentTarget = tower != null ? tower.transform : null;
    }

    private void Act()
    {
        float hpRatio = stats.hp / Mathf.Max(1f, stats.maxHp);
        if (hpRatio <= EffectiveRetreatRatio())
        {
            MoveToward(spawnPoint, stats.moveSpeed * 0.9f);
            return;
        }

        if (role == AOGBotRole.Support && currentTarget == null)
        {
            FollowCarryOrLane();
            return;
        }

        if (currentTarget != null && currentTarget.gameObject.activeInHierarchy)
        {
            if (role == AOGBotRole.Carry && TryMaintainCarrySpacing(currentTarget))
                return;

            float distance = FlatDistance(transform.position, currentTarget.position);
            float range = stats.attackRange;

            if (distance > range)
            {
                MoveToward(currentTarget.position, stats.moveSpeed * RoleMoveMultiplier());
                if (distance <= abilityRange && Time.time >= nextAbility)
                    UseAbility(currentTarget.position);
            }
            else
            {
                Face(currentTarget.position);
                presentation?.SetPlanarVelocity(Vector3.zero);
                if (Time.time >= nextAttack)
                {
                    nextAttack = Time.time + stats.attackCooldown;
                    presentation?.PlayBasicAttack();
                    ResolveBasicAttack(currentTarget);
                }
            }
            return;
        }

        if (role == AOGBotRole.Mid && ShouldRoam())
        {
            AOGCharacterStats roamTarget = FindWeakEnemyHero(28f);
            if (roamTarget != null)
            {
                MoveToward(roamTarget.transform.position, stats.moveSpeed * 1.03f);
                return;
            }
        }

        FollowLane();
    }

    private bool TryMaintainCarrySpacing(Transform threat)
    {
        AOGCharacterStats enemyHero = threat.GetComponentInParent<AOGCharacterStats>();
        if (enemyHero == null)
            return false;

        float distance = FlatDistance(transform.position, enemyHero.transform.position);
        float desiredMin = Mathf.Max(3.8f, stats.attackRange * 0.68f);
        if (distance >= desiredMin)
            return false;

        Vector3 away = transform.position - enemyHero.transform.position;
        away.y = 0f;
        if (away.sqrMagnitude < 0.01f) away = -transform.forward;
        MoveToward(transform.position + away.normalized * 4f, stats.moveSpeed * 1.08f);
        return true;
    }

    private void FollowCarryOrLane()
    {
        if (protectedCarry == null || protectedCarry.IsDead)
            protectedCarry = FindAlliedCarry(24f);

        if (protectedCarry != null)
        {
            float distance = FlatDistance(transform.position, protectedCarry.transform.position);
            if (distance > 4.8f)
            {
                Vector3 offset = protectedCarry.transform.position - protectedCarry.transform.forward * 2.2f;
                MoveToward(offset, stats.moveSpeed * 0.98f);
                return;
            }

            if (stats.hp < stats.maxHp * 0.92f && Time.time >= nextAbility)
            {
                nextAbility = Time.time + 7.5f;
                stats.hp = Mathf.Min(stats.maxHp, stats.hp + 34f);
                protectedCarry.hp = Mathf.Min(protectedCarry.maxHp, protectedCarry.hp + 46f);
                Color c = team == MinionTeam.Blue ? new Color(0.24f,0.76f,1f) : new Color(1f,0.30f,0.36f);
                GameObject ring = AOGAbilityVisuals.CreateRing("Support_Protection_Pulse", protectedCarry.transform.position + Vector3.up * 0.05f, 1.7f, c, 0.08f);
                Destroy(ring,0.4f);
            }
            presentation?.SetPlanarVelocity(Vector3.zero);
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

        MoveToward(lanePath[pathIndex].position, stats.moveSpeed * 0.92f * RoleMoveMultiplier());
    }

    private void UseAbility(Vector3 point)
    {
        nextAbility = Time.time + RoleAbilityCooldown();
        presentation?.PlayAbility((int)role % 3);
        Color c = team == MinionTeam.Blue ? new Color(0.18f, 0.58f, 1f) : new Color(1f, 0.18f, 0.24f);
        float radius = role == AOGBotRole.Top ? 2.8f : role == AOGBotRole.Mid ? 2.4f : 2.2f;
        GameObject ring = AOGAbilityVisuals.CreateRing("Bot_Ability_Telegraph", point + Vector3.up * 0.06f, radius, c, 0.10f);
        Destroy(ring, 0.42f);

        foreach (Collider hit in Physics.OverlapSphere(point, radius, ~0, QueryTriggerInteraction.Ignore))
        {
            Minion minion = hit.GetComponentInParent<Minion>();
            if (minion != null && minion.team != team && role != AOGBotRole.Support)
                minion.TakeDamage(stats.attackDamage * (role == AOGBotRole.Mid ? 1.55f : 1.25f), gameObject);

            AOGCharacterStats hero = hit.GetComponentInParent<AOGCharacterStats>();
            if (hero != null && hero != stats && hero.team != team)
                hero.TakeDamage(stats.attackDamage * (role == AOGBotRole.Top ? 1.2f : 1.1f), gameObject);
        }
    }

    private void ResolveBasicAttack(Transform target)
    {
        Minion minion = target.GetComponentInParent<Minion>();
        if (minion != null && minion.team != team)
        {
            if (role != AOGBotRole.Support)
                minion.TakeDamage(stats.attackDamage, gameObject);
            return;
        }

        AOGCharacterStats hero = target.GetComponentInParent<AOGCharacterStats>();
        if (hero != null && hero != stats && hero.team != team)
        {
            hero.TakeDamage(stats.attackDamage, gameObject);
            return;
        }

        TowerHealth tower = target.GetComponentInParent<TowerHealth>();
        if (tower != null && tower.towerTeam != team)
            tower.TakeDamage(stats.attackDamage * (role == AOGBotRole.Top ? 0.90f : 0.75f));
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

    private AOGCharacterStats FindEnemyNearPoint(Vector3 point,float range)
    {
        AOGCharacterStats best = null;
        float bestDist = range;
        foreach (AOGCharacterStats hero in FindObjectsByType<AOGCharacterStats>(FindObjectsInactive.Exclude,FindObjectsSortMode.None))
        {
            if (hero == null || hero == stats || hero.IsDead || hero.team == team) continue;
            float d = FlatDistance(point,hero.transform.position);
            if (d < bestDist) { best=hero; bestDist=d; }
        }
        return best;
    }

    private AOGCharacterStats FindWeakEnemyHero(float range)
    {
        AOGCharacterStats best = null;
        float bestScore = float.MaxValue;
        foreach (AOGCharacterStats hero in FindObjectsByType<AOGCharacterStats>(FindObjectsInactive.Exclude,FindObjectsSortMode.None))
        {
            if (hero == null || hero == stats || hero.IsDead || hero.team == team) continue;
            float distance = FlatDistance(transform.position,hero.transform.position);
            if (distance > range) continue;
            float score = hero.hp / Mathf.Max(1f,hero.maxHp) + distance / range * 0.45f;
            if (score < bestScore) { best=hero; bestScore=score; }
        }
        return best;
    }

    private AOGCharacterStats FindAlliedCarry(float range)
    {
        AOGCharacterStats best = null;
        float bestDist = range;
        foreach (AOGTeamMemberIdentity member in FindObjectsByType<AOGTeamMemberIdentity>(FindObjectsInactive.Exclude,FindObjectsSortMode.None))
        {
            if (member == null || member.team != team || member.role != AOGRole.ADC) continue;
            AOGCharacterStats ally = member.GetComponent<AOGCharacterStats>();
            if (ally == null || ally.IsDead) continue;
            float d = FlatDistance(transform.position,ally.transform.position);
            if (d < bestDist) { best=ally; bestDist=d; }
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

    private float EffectiveRetreatRatio()
    {
        if (role == AOGBotRole.Carry) return Mathf.Max(retreatHpRatio,0.34f);
        if (role == AOGBotRole.Support) return Mathf.Max(retreatHpRatio,0.31f);
        if (role == AOGBotRole.Top) return Mathf.Min(retreatHpRatio,0.22f);
        return retreatHpRatio;
    }

    private float RoleMoveMultiplier()
    {
        if (role == AOGBotRole.Mid) return 1.03f;
        if (role == AOGBotRole.Carry) return 0.98f;
        if (role == AOGBotRole.Top) return 0.95f;
        return 1f;
    }

    private float RoleAbilityCooldown()
    {
        if (role == AOGBotRole.Mid) return 4.8f;
        if (role == AOGBotRole.Top) return 6.2f;
        if (role == AOGBotRole.Support) return 7.5f;
        return 5.8f;
    }

    private bool ShouldRoam()
    {
        if (role != AOGBotRole.Mid || lanePath == null || lanePath.Length == 0) return false;
        bool nearbyWave = FindNearestEnemyMinion(9f) != null;
        return !nearbyWave && pathIndex >= Mathf.Max(0,lanePath.Length/3);
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
