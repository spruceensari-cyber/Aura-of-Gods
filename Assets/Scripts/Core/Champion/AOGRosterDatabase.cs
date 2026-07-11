using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class AOGChampionDefinition
{
    public string id;
    public string displayName;
    public string title;
    public AOGRole primaryRole;
    public AOGRole secondaryRole;
    public GameObject gameplayPrefab;
    public Sprite portrait;
    public Color accentColor;
    public float baseHp;
    public float baseAttackDamage;
    public float attackRange;
    public float moveSpeed;
    public float attackCooldown;

    public AOGChampionDefinition(
        string championId,
        string championDisplayName,
        string championTitle,
        AOGRole primary,
        AOGRole secondary,
        Color accent,
        float hp,
        float damage,
        float range,
        float speed,
        float cooldown)
    {
        id = championId;
        displayName = championDisplayName;
        title = championTitle;
        primaryRole = primary;
        secondaryRole = secondary;
        accentColor = accent;
        baseHp = hp;
        baseAttackDamage = damage;
        attackRange = range;
        moveSpeed = speed;
        attackCooldown = cooldown;
    }
}

[DefaultExecutionOrder(-980)]
public sealed class AOGRosterDatabase : MonoBehaviour
{
    public static AOGRosterDatabase Instance { get; private set; }

    private readonly List<AOGChampionDefinition> definitions = new List<AOGChampionDefinition>();
    private readonly Dictionary<string, AOGChampionDefinition> definitionsById =
        new Dictionary<string, AOGChampionDefinition>(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<AOGChampionDefinition> All => definitions;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        EnsureInstance();
    }

    public static AOGRosterDatabase EnsureInstance()
    {
        if (Instance != null)
            return Instance;

        AOGRosterDatabase existing = FindFirstObjectByType<AOGRosterDatabase>();
        if (existing != null)
        {
            Instance = existing;
            return existing;
        }

        GameObject host = new GameObject("AOG_Roster_Database");
        Instance = host.AddComponent<AOGRosterDatabase>();
        DontDestroyOnLoad(host);
        return Instance;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildDefinitions();
    }

    public AOGChampionDefinition GetDefinition(string championId)
    {
        if (string.IsNullOrWhiteSpace(championId))
            return null;

        if (definitions.Count == 0)
            BuildDefinitions();

        definitionsById.TryGetValue(championId, out AOGChampionDefinition definition);
        return definition;
    }

    private void BuildDefinitions()
    {
        if (definitions.Count > 0)
            return;

        Add(new AOGChampionDefinition("lyra", "LYRA", "MOON HUNTRESS", AOGRole.Mid, AOGRole.ADC,
            new Color(0.62f, 0.28f, 0.92f), 900f, 55f, 5.3f, 6.0f, 1.0f));
        Add(new AOGChampionDefinition("kaelith", "KAELITH", "ECLIPSE REAVER", AOGRole.Top, AOGRole.Jungle,
            new Color(0.36f, 0.18f, 0.86f), 980f, 64f, 3.2f, 6.25f, 0.92f));
        Add(new AOGChampionDefinition("auron", "AURON", "SOLAR VANGUARD", AOGRole.Top, AOGRole.Support,
            new Color(1.0f, 0.56f, 0.12f), 1250f, 72f, 2.5f, 5.6f, 1.05f));
        Add(new AOGChampionDefinition("vesper", "VESPER", "VOID ARCHER", AOGRole.ADC, AOGRole.Mid,
            new Color(0.12f, 0.78f, 0.95f), 780f, 58f, 6.2f, 6.7f, 0.86f));
        Add(new AOGChampionDefinition("nyra", "NYRA", "SPIRIT VIXEN", AOGRole.Mid, AOGRole.Jungle,
            new Color(0.94f, 0.28f, 0.74f), 790f, 57f, 6.2f, 6.9f, 0.82f));
        Add(new AOGChampionDefinition("pyrelle", "PYRELLE", "FLAME SOVEREIGN", AOGRole.Mid, AOGRole.Top,
            new Color(1.0f, 0.22f, 0.035f), 860f, 64f, 6.2f, 6.3f, 0.92f));
        Add(new AOGChampionDefinition("selene", "SELENE", "ASTRAL ORACLE", AOGRole.Mid, AOGRole.Support,
            new Color(0.36f, 0.68f, 1.0f), 790f, 57f, 6.2f, 6.3f, 0.92f));
        Add(new AOGChampionDefinition("seris", "SERIS", "AETHER VEIL", AOGRole.Support, AOGRole.Mid,
            new Color(0.38f, 0.88f, 0.90f), 860f, 46f, 5.8f, 6.0f, 1.0f));
        Add(new AOGChampionDefinition("mireva", "MIREVA", "BLOOM WARDEN", AOGRole.Support, AOGRole.Top,
            new Color(0.20f, 0.82f, 0.48f), 920f, 48f, 5.2f, 5.8f, 1.0f));
        Add(new AOGChampionDefinition("dravenor", "DRAVENOR", "FANG STALKER", AOGRole.Jungle, AOGRole.Top,
            new Color(0.78f, 0.22f, 0.10f), 1040f, 70f, 2.8f, 6.45f, 0.94f));
        Add(new AOGChampionDefinition("nocthyr", "NOCTHYR", "SHADE TRACKER", AOGRole.Jungle, AOGRole.Mid,
            new Color(0.18f, 0.22f, 0.58f), 860f, 67f, 3.0f, 6.7f, 0.88f));
    }

    private void Add(AOGChampionDefinition definition)
    {
        definitions.Add(definition);
        definitionsById.Add(definition.id, definition);
    }
}
