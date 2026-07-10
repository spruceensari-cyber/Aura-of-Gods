using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum AOGNeutralMonsterType
{
    SpiritWolf,
    EmberRaptor,
    StoneGuardian,
    AetherSentinel,
    InfernalBrute
}

public class AOGNeutralMonsterRuntime : MonoBehaviour
{
    public AOGNeutralMonsterType monsterType;
    public float maxHp = 500f;
    public float hp = 500f;
    public float damage = 28f;
    public float moveSpeed = 3.0f;
    public float attackRange = 2.2f;
    public float attackCooldown = 1.15f;
    public float aggroRadius = 7.5f;
    public float leashRadius = 10f;
    public int goldReward = 55;
    public int xpReward = 90;

    public bool IsDead => dead || hp <= 0f;
    public event Action<AOGNeutralMonsterRuntime, GameObject> Died;

    private Vector3 homePosition;
    private Quaternion homeRotation;
    private AOGCharacterStats target;
    private float nextAttack;
    private GameObject lastAttacker;
    private bool returning;
    private bool dead;
    private ChampionPresentationController presentation;

    private void Awake()
    {
        homePosition = transform.position;
        homeRotation = transform.rotation;
        presentation = GetComponent<ChampionPresentationController>();
    }

    private void Update()
    {
        if (dead || AOGMatchDirector.Instance == null || AOGMatchDirector.Instance.State != AOGMatchState.Playing)
            return;

        if (Vector3.Distance(Flat(transform.position), Flat(homePosition)) > leashRadius)
        {
            target = null;
            returning = true;
        }

        if (returning)
        {
            ReturnHome();
            return;
        }

        if (target == null || target.IsDead || !target.gameObject.activeInHierarchy || Vector3.Distance(Flat(transform.position), Flat(target.transform.position)) > aggroRadius * 1.65f)
            target = FindNearestChampion(aggroRadius);

        if (target == null)
        {
            presentation?.SetPlanarVelocity(Vector3.zero);
            return;
        }

        float distance = Vector3.Distance(Flat(transform.position), Flat(target.transform.position));
        if (distance > attackRange)
        {
            MoveToward(target.transform.position);
            return;
        }

        Face(target.transform.position);
        presentation?.SetPlanarVelocity(Vector3.zero);
        if (Time.time >= nextAttack)
        {
            nextAttack = Time.time + attackCooldown;
            StartCoroutine(AttackRoutine(target));
        }
    }

    public void TakeDamage(float amount, GameObject attacker)
    {
        if (dead || amount <= 0f)
            return;

        hp = Mathf.Clamp(hp - amount, 0f, maxHp);
        lastAttacker = attacker;
        if (attacker != null)
        {
            AOGCharacterStats attackerStats = attacker.GetComponentInParent<AOGCharacterStats>();
            if (attackerStats != null && !attackerStats.IsDead)
                target = attackerStats;
        }

        presentation?.PlayHitReaction();
        if (hp <= 0f)
            Die();
    }

    public void ResetHome(Vector3 position, Quaternion rotation)
    {
        homePosition = position;
        homeRotation = rotation;
        transform.position = position;
        transform.rotation = rotation;
        hp = maxHp;
        target = null;
        dead = false;
        returning = false;
    }

    private IEnumerator AttackRoutine(AOGCharacterStats locked)
    {
        presentation?.PlayBasicAttack();
        yield return new WaitForSeconds(0.28f);
        if (dead || locked == null || locked.IsDead)
            yield break;

        if (Vector3.Distance(Flat(transform.position), Flat(locked.transform.position)) <= attackRange + 0.55f)
        {
            locked.TakeDamage(damage);
            Color accent = AOGNeutralCreatureModelFactory.AccentFor(monsterType);
            GameObject ring = AOGAbilityVisuals.CreateRing("Neutral_Monster_Impact", locked.transform.position + Vector3.up * 0.08f, 0.72f, accent, 0.06f);
            Destroy(ring, 0.30f);
        }
    }

    private void Die()
    {
        if (dead)
            return;
        dead = true;
        hp = 0f;
        target = null;
        presentation?.PlayDeath();
        RewardKiller(lastAttacker);
        Died?.Invoke(this, lastAttacker);
        StartCoroutine(DeathPresentation());
    }

    private IEnumerator DeathPresentation()
    {
        Vector3 startScale = transform.localScale;
        float elapsed = 0f;
        while (elapsed < 0.55f)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / 0.55f);
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t * t);
            transform.Rotate(Vector3.up, 220f * Time.deltaTime, Space.World);
            yield return null;
        }
        gameObject.SetActive(false);
    }

    private void RewardKiller(GameObject attacker)
    {
        if (attacker == null)
            return;

        AOGPlayerEconomy economy = attacker.GetComponentInParent<AOGPlayerEconomy>();
        economy?.AddGold(goldReward);

        AOGChampionProgression progression = attacker.GetComponentInParent<AOGChampionProgression>();
        progression?.AddExperience(xpReward);
    }

    private void ReturnHome()
    {
        float distance = Vector3.Distance(Flat(transform.position), Flat(homePosition));
        if (distance <= 0.35f)
        {
            returning = false;
            transform.position = homePosition;
            transform.rotation = homeRotation;
            hp = maxHp;
            presentation?.SetPlanarVelocity(Vector3.zero);
            return;
        }

        hp = Mathf.MoveTowards(hp, maxHp, maxHp * 0.30f * Time.deltaTime);
        MoveToward(homePosition);
    }

    private AOGCharacterStats FindNearestChampion(float radius)
    {
        AOGCharacterStats best = null;
        float bestDistance = radius;
        foreach (AOGCharacterStats hero in FindObjectsByType<AOGCharacterStats>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (hero == null || hero.IsDead)
                continue;
            float d = Vector3.Distance(Flat(transform.position), Flat(hero.transform.position));
            if (d < bestDistance)
            {
                best = hero;
                bestDistance = d;
            }
        }
        return best;
    }

    private void MoveToward(Vector3 point)
    {
        Vector3 delta = point - transform.position;
        delta.y = 0f;
        if (delta.sqrMagnitude < 0.01f)
            return;
        Vector3 direction = delta.normalized;
        transform.position += direction * moveSpeed * Time.deltaTime;
        Face(point);
        presentation?.SetPlanarVelocity(direction * moveSpeed);
    }

    private void Face(Vector3 point)
    {
        Vector3 delta = point - transform.position;
        delta.y = 0f;
        if (delta.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(delta.normalized), 9f * Time.deltaTime);
    }

    private static Vector3 Flat(Vector3 value)
    {
        value.y = 0f;
        return value;
    }
}

public class AOGNeutralCampController : MonoBehaviour
{
    public string campId;
    public string displayName;
    public float respawnDelay = 42f;
    public AOGNeutralMonsterType monsterType;
    public int monsterCount = 3;
    public Color accent = Color.white;

    private readonly List<AOGNeutralMonsterRuntime> monsters = new List<AOGNeutralMonsterRuntime>();
    private bool respawning;

    public void Initialize(string id, string label, AOGNeutralMonsterType type, int count, Color color, float respawn)
    {
        campId = id;
        displayName = label;
        monsterType = type;
        monsterCount = Mathf.Max(1, count);
        accent = color;
        respawnDelay = respawn;
        BuildArena();
        SpawnCamp();
    }

    private void SpawnCamp()
    {
        monsters.Clear();
        for (int i = 0; i < monsterCount; i++)
        {
            GameObject monsterObject = new GameObject(displayName.Replace(" ", "_") + "_" + i);
            monsterObject.transform.SetParent(transform, false);
            float angle = monsterCount == 1 ? 0f : i * Mathf.PI * 2f / monsterCount;
            float radius = monsterCount == 1 ? 0f : 1.55f;
            monsterObject.transform.localPosition = new Vector3(Mathf.Cos(angle) * radius, 0.2f, Mathf.Sin(angle) * radius);

            AOGNeutralCreatureModelFactory.BuildMonster(monsterObject.transform, monsterType, accent, i == 0);
            AOGNeutralMonsterRuntime monster = monsterObject.AddComponent<AOGNeutralMonsterRuntime>();
            ConfigureStats(monster, i == 0);
            monster.Died += OnMonsterDied;

            monsterObject.AddComponent<ChampionAudioController>();
            monsterObject.AddComponent<ChampionPresentationController>();
            monsterObject.AddComponent<AOGAutoAttackPresentationRuntime>();

            CapsuleCollider collider = monsterObject.AddComponent<CapsuleCollider>();
            collider.center = new Vector3(0f, 1.1f, 0f);
            collider.height = i == 0 ? 2.8f : 2.2f;
            collider.radius = i == 0 ? 0.88f : 0.62f;
            Rigidbody body = monsterObject.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;

            AOGNeutralMonsterHealthBar bar = monsterObject.AddComponent<AOGNeutralMonsterHealthBar>();
            bar.offset = new Vector3(0f, i == 0 ? 3.3f : 2.65f, 0f);
            bar.width = i == 0 ? 1.8f : 1.2f;
            bar.accent = accent;
            monsters.Add(monster);
        }
    }

    private void ConfigureStats(AOGNeutralMonsterRuntime monster, bool leader)
    {
        monster.monsterType = monsterType;
        float hpBase = monsterType switch
        {
            AOGNeutralMonsterType.SpiritWolf => 460f,
            AOGNeutralMonsterType.EmberRaptor => 390f,
            AOGNeutralMonsterType.StoneGuardian => 720f,
            AOGNeutralMonsterType.AetherSentinel => 1150f,
            _ => 1250f
        };
        float damageBase = monsterType switch
        {
            AOGNeutralMonsterType.SpiritWolf => 28f,
            AOGNeutralMonsterType.EmberRaptor => 25f,
            AOGNeutralMonsterType.StoneGuardian => 36f,
            AOGNeutralMonsterType.AetherSentinel => 44f,
            _ => 48f
        };
        monster.maxHp = hpBase * (leader ? 1.35f : 0.72f);
        monster.hp = monster.maxHp;
        monster.damage = damageBase * (leader ? 1.20f : 0.82f);
        monster.moveSpeed = monsterType == AOGNeutralMonsterType.SpiritWolf ? 3.8f : 3.0f;
        monster.attackRange = monsterType == AOGNeutralMonsterType.AetherSentinel ? 4.6f : 2.2f;
        monster.attackCooldown = monsterType == AOGNeutralMonsterType.EmberRaptor ? 0.88f : 1.12f;
        monster.aggroRadius = 7.5f;
        monster.leashRadius = 10.5f;
        monster.goldReward = leader ? 85 : 35;
        monster.xpReward = leader ? 150 : 65;
    }

    private void OnMonsterDied(AOGNeutralMonsterRuntime monster, GameObject killer)
    {
        if (respawning)
            return;

        foreach (AOGNeutralMonsterRuntime member in monsters)
            if (member != null && !member.IsDead)
                return;

        StartCoroutine(RespawnCamp());
    }

    private IEnumerator RespawnCamp()
    {
        respawning = true;
        yield return new WaitForSeconds(respawnDelay);

        foreach (AOGNeutralMonsterRuntime monster in monsters)
        {
            if (monster != null)
                Destroy(monster.gameObject);
        }
        monsters.Clear();
        yield return null;
        SpawnCamp();
        respawning = false;
    }

    private void BuildArena()
    {
        GameObject ring = AOGAbilityVisuals.CreateRing(displayName + "_Camp_Ring", transform.position + Vector3.up * 0.05f, 4.8f, accent, 0.07f);
        ring.transform.SetParent(transform, true);

        GameObject lightObject = new GameObject("Camp_Aura_Light");
        lightObject.transform.SetParent(transform, false);
        lightObject.transform.localPosition = new Vector3(0f, 3.4f, 0f);
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = accent;
        light.intensity = 1.1f;
        light.range = 9f;
        light.shadows = LightShadows.None;
    }
}

[DefaultExecutionOrder(-660)]
public class AOGNeutralJungleSystemRuntime : MonoBehaviour
{
    private static AOGNeutralJungleSystemRuntime instance;
    private bool built;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (instance != null)
            return;
        GameObject host = new GameObject("AOG_Neutral_Jungle_System");
        instance = host.AddComponent<AOGNeutralJungleSystemRuntime>();
        DontDestroyOnLoad(host);
    }

    private void Update()
    {
        if (built || AOGMatchDirector.Instance == null)
            return;
        DisableLegacyJunglePrototype();
        built = true;

        CreateCamp("spirit_wolves", "SPIRIT WOLVES", AOGNeutralMonsterType.SpiritWolf, new Vector3(18f,0.2f,34f), 3, new Color(0.18f,0.72f,1f), 38f);
        CreateCamp("ember_raptors", "EMBER RAPTORS", AOGNeutralMonsterType.EmberRaptor, new Vector3(-18f,0.2f,-34f), 4, new Color(1f,0.26f,0.05f), 36f);
        CreateCamp("stone_guardians", "STONE GUARDIANS", AOGNeutralMonsterType.StoneGuardian, new Vector3(38f,0.2f,-26f), 2, new Color(0.72f,0.62f,0.42f), 44f);
        CreateCamp("shadow_wolves", "SHADE WOLVES", AOGNeutralMonsterType.SpiritWolf, new Vector3(-38f,0.2f,26f), 3, new Color(0.46f,0.20f,0.88f), 38f);
        CreateCamp("aether_sentinel", "AETHER SENTINEL", AOGNeutralMonsterType.AetherSentinel, new Vector3(30f,0.2f,12f), 1, new Color(0.12f,0.78f,0.96f), 70f);
        CreateCamp("infernal_brute", "INFERNAL BRUTE", AOGNeutralMonsterType.InfernalBrute, new Vector3(-30f,0.2f,-12f), 1, new Color(1f,0.12f,0.04f), 70f);
    }

    private void CreateCamp(string id, string label, AOGNeutralMonsterType type, Vector3 position, int count, Color accent, float respawn)
    {
        GameObject root = new GameObject("NeutralCamp_" + id);
        root.transform.position = position;
        AOGNeutralCampController camp = root.AddComponent<AOGNeutralCampController>();
        camp.Initialize(id, label, type, count, accent, respawn);
    }

    private static void DisableLegacyJunglePrototype()
    {
        foreach (AOGJungleCampRuntime legacy in FindObjectsByType<AOGJungleCampRuntime>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (legacy == null) continue;
            legacy.enabled = false;
            legacy.gameObject.SetActive(false);
        }

        foreach (GameObject obj in FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (obj == null) continue;
            string lower = obj.name.ToLowerInvariant();
            if (lower.Contains("ashfang camp") || lower.Contains("frostclaw camp") || lower.Contains("voidling camp") || lower.Contains("aetherling camp"))
                Destroy(obj);
        }
    }
}

public class AOGNeutralMonsterHealthBar : MonoBehaviour
{
    public Vector3 offset = new Vector3(0f, 2.8f, 0f);
    public float width = 1.4f;
    public float height = 0.12f;
    public Color accent = new Color(0.76f,0.30f,1f);

    private AOGNeutralMonsterRuntime target;
    private Transform root;
    private Transform fill;

    private void Start()
    {
        target = GetComponent<AOGNeutralMonsterRuntime>();
        Build();
    }

    private void LateUpdate()
    {
        if (target == null || root == null || fill == null)
            return;
        float ratio = Mathf.Clamp01(target.hp / Mathf.Max(1f, target.maxHp));
        fill.localScale = new Vector3(ratio, 1f, 1f);
        fill.localPosition = new Vector3(-(1f-ratio)*0.5f,0f,-0.012f);
        Camera cam = Camera.main;
        if (cam != null) root.rotation = cam.transform.rotation;
    }

    private void Build()
    {
        GameObject rootObject = new GameObject("Neutral_HP_Bar");
        rootObject.transform.SetParent(transform, false);
        rootObject.transform.localPosition = offset;
        root = rootObject.transform;
        GameObject border = CreateCube("Border", root, new Vector3(width+0.12f,height+0.09f,0.04f), new Color(0.01f,0.012f,0.018f));
        GameObject bg = CreateCube("Background", border.transform, new Vector3(0.92f,0.58f,0.75f), new Color(0.06f,0.045f,0.07f));
        GameObject fillObject = CreateCube("Fill", bg.transform, new Vector3(1f,0.76f,0.65f), accent);
        fill = fillObject.transform;
    }

    private static GameObject CreateCube(string name, Transform parent, Vector3 scale, Color color)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localScale = scale;
        Renderer renderer = go.GetComponent<Renderer>();
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        Material material = new Material(shader) { color = color };
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor",color);
        renderer.sharedMaterial = material;
        Collider collider = go.GetComponent<Collider>();
        if (collider != null) Destroy(collider);
        return go;
    }
}
