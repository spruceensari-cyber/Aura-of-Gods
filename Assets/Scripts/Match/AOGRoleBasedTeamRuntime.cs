using System.Collections.Generic;
using UnityEngine;

public class AOGTeamMemberIdentity : MonoBehaviour
{
    public string championId;
    public string displayName;
    public AOGRole role;
    public MinionTeam team;
    public bool isHumanPlayer;
}

[DefaultExecutionOrder(-720)]
public class AOGRoleBasedTeamRuntime : MonoBehaviour
{
    public static AOGRoleBasedTeamRuntime Instance { get; private set; }
    public static readonly List<AOGTeamMemberIdentity> BlueRoster = new List<AOGTeamMemberIdentity>();
    public static readonly List<AOGTeamMemberIdentity> RedRoster = new List<AOGTeamMemberIdentity>();

    private bool teamsBuilt;

    public static void EnsureAndBuildTeams(AOGActiveChampion selected, AOGRole playerRole)
    {
        if (Instance == null)
        {
            GameObject host = new GameObject("AOG_Role_Based_Team_Runtime");
            Instance = host.AddComponent<AOGRoleBasedTeamRuntime>();
            DontDestroyOnLoad(host);
        }
        Instance.BuildTeams(selected, playerRole);
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
    }

    private void BuildTeams(AOGActiveChampion selected, AOGRole playerRole)
    {
        if (teamsBuilt || selected == null)
            return;

        teamsBuilt = true;
        DisableLegacyBotSpawner();
        DestroyLegacyBots();
        BlueRoster.Clear();
        RedRoster.Clear();

        AOGTeamMemberIdentity playerIdentity = selected.GetComponent<AOGTeamMemberIdentity>();
        if (playerIdentity == null) playerIdentity = selected.gameObject.AddComponent<AOGTeamMemberIdentity>();
        playerIdentity.championId = selected.championId;
        playerIdentity.displayName = selected.displayName;
        playerIdentity.role = playerRole;
        playerIdentity.team = MinionTeam.Blue;
        playerIdentity.isHumanPlayer = true;
        BlueRoster.Add(playerIdentity);

        MinionSpawner spawner = FindFirstObjectByType<MinionSpawner>();
        if (spawner == null)
        {
            Debug.LogError("AOG 5v5 roster: MinionSpawner not found; role lane paths cannot be assigned.");
            return;
        }

        foreach (AOGRole role in System.Enum.GetValues(typeof(AOGRole)))
        {
            if (role != playerRole)
            {
                ChampionBotDefinition blueDef = BlueDefinition(role, selected.championId);
                BlueRoster.Add(CreateBot(MinionTeam.Blue, role, blueDef, spawner));
            }

            ChampionBotDefinition redDef = RedDefinition(role);
            RedRoster.Add(CreateBot(MinionTeam.Red, role, redDef, spawner));
        }

        AOGTeamRosterHudRuntime.Ensure();
    }

    private AOGTeamMemberIdentity CreateBot(MinionTeam team, AOGRole role, ChampionBotDefinition definition, MinionSpawner spawner)
    {
        GameObject bot = new GameObject(team + "_Champion_" + role + "_" + definition.id);
        bot.transform.position = ResolveRoleSpawn(team, role, spawner);
        AOGOriginalChampionModelFactory.BuildChampion(bot.transform, definition.id, definition.accent);

        AOGCharacterStats stats = bot.AddComponent<AOGCharacterStats>();
        stats.team = team;
        stats.maxHp = definition.hp;
        stats.hp = stats.maxHp;
        stats.attackDamage = definition.damage;
        stats.attackRange = definition.range;
        stats.attackCooldown = definition.cooldown;
        stats.moveSpeed = definition.speed;

        bot.AddComponent<ChampionAudioController>();
        bot.AddComponent<ChampionPresentationController>();
        bot.AddComponent<AOGAutoAttackPresentationRuntime>();
        if (definition.range <= 3.5f) bot.AddComponent<AOGPremiumMeleeSwingRuntime>();

        CapsuleCollider collider = bot.AddComponent<CapsuleCollider>();
        collider.center = new Vector3(0f, 1.1f, 0f);
        collider.height = 2.4f;
        collider.radius = 0.64f;
        Rigidbody body = bot.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;

        AOGBotChampionAI ai = bot.AddComponent<AOGBotChampionAI>();
        ai.team = team;
        ai.role = ToLegacyRole(role);
        ai.lanePath = ResolvePath(role, team, spawner);
        ai.heroAggroRange = role == AOGRole.Support ? 7.5f : 9f;
        ai.retreatHpRatio = role == AOGRole.Top ? 0.20f : 0.30f;

        AOGWorldHealthBar bar = bot.AddComponent<AOGWorldHealthBar>();
        bar.barOffset = new Vector3(0f,3.2f,0f);
        bar.barWidth = 1.45f;
        bar.barHeight = 0.12f;

        AOGTeamMemberIdentity identity = bot.AddComponent<AOGTeamMemberIdentity>();
        identity.championId = definition.id;
        identity.displayName = definition.display;
        identity.role = role;
        identity.team = team;
        identity.isHumanPlayer = false;
        return identity;
    }

    private static void DisableLegacyBotSpawner()
    {
        foreach (AOGFiveVFiveBotRuntime legacy in FindObjectsByType<AOGFiveVFiveBotRuntime>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (legacy != null) legacy.enabled = false;
    }

    private static void DestroyLegacyBots()
    {
        foreach (GameObject obj in FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (obj == null) continue;
            if (obj.name.StartsWith("Blue_Bot_") || obj.name.StartsWith("Red_Bot_"))
                Destroy(obj);
        }
    }

    private static Vector3 ResolveRoleSpawn(MinionTeam team, AOGRole role, MinionSpawner spawner)
    {
        string exact = team + "_" + role + "_Spawn";
        GameObject exactObject = GameObject.Find(exact);
        if (exactObject != null) return exactObject.transform.position;

        Transform baseSpawn = team == MinionTeam.Blue ? spawner.blueBaseSpawn : spawner.redBaseSpawn;
        Vector3 origin = baseSpawn != null ? baseSpawn.position : Vector3.zero;
        float direction = team == MinionTeam.Blue ? 1f : -1f;
        return origin + role switch
        {
            AOGRole.Top => new Vector3(-3.5f,0.2f,3.2f*direction),
            AOGRole.Jungle => new Vector3(-1.6f,0.2f,2.2f*direction),
            AOGRole.Mid => new Vector3(0f,0.2f,3.2f*direction),
            AOGRole.ADC => new Vector3(1.8f,0.2f,2.4f*direction),
            _ => new Vector3(3.3f,0.2f,3.0f*direction)
        };
    }

    private static Transform[] ResolvePath(AOGRole role, MinionTeam team, MinionSpawner spawner)
    {
        Transform[] source;
        if (role == AOGRole.Top) source = spawner.topLaneWaypoints;
        else if (role == AOGRole.Mid) source = spawner.midLaneWaypoints;
        else if (role == AOGRole.ADC || role == AOGRole.Support) source = spawner.botLaneWaypoints;
        else return BuildJungleRoute(team);

        if (source == null) return new Transform[0];
        if (team == MinionTeam.Blue) return source;
        Transform[] reverse = new Transform[source.Length];
        for (int i = 0; i < source.Length; i++) reverse[i] = source[source.Length - 1 - i];
        return reverse;
    }

    private static Transform[] BuildJungleRoute(MinionTeam team)
    {
        Vector3[] blue =
        {
            new Vector3(18f,0.2f,34f),
            new Vector3(-18f,0.2f,-34f),
            new Vector3(38f,0.2f,-26f),
            new Vector3(0f,0.2f,0f)
        };
        Vector3[] red =
        {
            new Vector3(-38f,0.2f,26f),
            new Vector3(18f,0.2f,34f),
            new Vector3(-18f,0.2f,-34f),
            new Vector3(0f,0.2f,0f)
        };
        Vector3[] points = team == MinionTeam.Blue ? blue : red;
        Transform[] route = new Transform[points.Length];
        GameObject root = GameObject.Find(team + "_Jungle_Route_Runtime") ?? new GameObject(team + "_Jungle_Route_Runtime");
        for (int i = 0; i < points.Length; i++)
        {
            GameObject node = new GameObject("Jungle_Node_" + i);
            node.transform.SetParent(root.transform, false);
            node.transform.position = points[i];
            route[i] = node.transform;
        }
        return route;
    }

    private static AOGBotRole ToLegacyRole(AOGRole role)
    {
        return role switch
        {
            AOGRole.Top => AOGBotRole.Top,
            AOGRole.Jungle => AOGBotRole.Jungle,
            AOGRole.Mid => AOGBotRole.Mid,
            AOGRole.ADC => AOGBotRole.Carry,
            _ => AOGBotRole.Support
        };
    }

    private static ChampionBotDefinition BlueDefinition(AOGRole role, string selectedId)
    {
        ChampionBotDefinition def = role switch
        {
            AOGRole.Top => Def("auron","AURON",new Color(1f,0.56f,0.12f),1250f,72f,2.5f,1.05f,5.6f),
            AOGRole.Jungle => Def("dravenor","DRAVENOR",new Color(0.72f,0.22f,0.10f),1040f,70f,2.8f,0.98f,6.6f),
            AOGRole.Mid => Def("nyra","NYRA",new Color(0.94f,0.28f,0.74f),790f,57f,6.2f,0.82f,6.9f),
            AOGRole.ADC => Def("vesper","VESPER",new Color(0.12f,0.78f,0.95f),780f,58f,6.2f,0.86f,6.7f),
            _ => Def("selene","SELENE",new Color(0.36f,0.68f,1f),900f,50f,6.4f,1.02f,6.2f)
        };
        if (def.id == selectedId)
        {
            if (role == AOGRole.Top) return Def("kaelith","KAELITH",new Color(0.36f,0.18f,0.86f),980f,64f,3.2f,0.92f,6.25f);
            if (role == AOGRole.Mid) return Def("pyrelle","PYRELLE",new Color(1f,0.22f,0.035f),860f,64f,6.2f,0.92f,6.3f);
            if (role == AOGRole.ADC) return Def("lyra","LYRA",new Color(0.62f,0.28f,0.92f),900f,55f,6f,0.96f,6f);
            if (role == AOGRole.Support) return Def("seris","SERIS",new Color(0.26f,0.88f,0.92f),980f,48f,6.1f,1.04f,6.1f);
        }
        return def;
    }

    private static ChampionBotDefinition RedDefinition(AOGRole role)
    {
        return role switch
        {
            AOGRole.Top => Def("kaelith","KAELITH",new Color(0.50f,0.12f,0.78f),1050f,70f,3.0f,0.94f,6.1f),
            AOGRole.Jungle => Def("nocthyr","NOCTHYR",new Color(0.18f,0.16f,0.52f),900f,78f,2.7f,0.86f,7.0f),
            AOGRole.Mid => Def("pyrelle","PYRELLE",new Color(1f,0.18f,0.025f),860f,68f,6.0f,0.92f,6.3f),
            AOGRole.ADC => Def("vesper","VESPER",new Color(0.12f,0.68f,0.92f),780f,62f,6.4f,0.82f,6.7f),
            _ => Def("mireva","MIREVA",new Color(0.18f,0.78f,0.42f),1020f,46f,6.0f,1.08f,6.0f)
        };
    }

    private static ChampionBotDefinition Def(string id, string display, Color accent, float hp, float damage, float range, float cooldown, float speed)
    {
        return new ChampionBotDefinition { id=id, display=display, accent=accent, hp=hp, damage=damage, range=range, cooldown=cooldown, speed=speed };
    }

    private struct ChampionBotDefinition
    {
        public string id;
        public string display;
        public Color accent;
        public float hp;
        public float damage;
        public float range;
        public float cooldown;
        public float speed;
    }
}
