using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Adds recall, passive gold and role-based shopping to existing role bots.
/// Existing lane/jungle AI components remain authoritative outside recall/shop states.
/// </summary>
public class AOGBotRecallRoleShoppingRuntime : MonoBehaviour
{
    public float recallDuration = 5.8f;
    public float safeEnemyRadius = 11f;
    public float lowHpRecallRatio = 0.34f;
    public int goldRecallThreshold = 1100;
    public float botGoldInterval = 1f;
    public int botGoldPerInterval = 4;

    private AOGTeamMemberIdentity identity;
    private AOGCharacterStats stats;
    private AOGPlayerEconomy economy;
    private AOGAdvancedInventoryStatsRuntime advancedStats;
    private AOGBotChampionAI laneAi;
    private AOGJungleChampionAIRuntime jungleAi;

    private readonly List<AOGAdvancedItemDefinition> build = new List<AOGAdvancedItemDefinition>();
    private bool channeling;
    private bool shopping;
    private bool aiStateCaptured;
    private bool laneAiWasEnabled;
    private bool jungleAiWasEnabled;
    private float recallStarted;
    private float recallStartHp;
    private Vector3 recallStartPosition;
    private float nextDecision;
    private float nextIncome;
    private int buildIndex;
    private GameObject recallRing;

    private void Awake()
    {
        identity = GetComponent<AOGTeamMemberIdentity>();
        stats = GetComponent<AOGCharacterStats>();
        laneAi = GetComponent<AOGBotChampionAI>();
        jungleAi = GetComponent<AOGJungleChampionAIRuntime>();

        economy = GetComponent<AOGPlayerEconomy>();
        if (economy == null) economy = gameObject.AddComponent<AOGPlayerEconomy>();
        advancedStats = GetComponent<AOGAdvancedInventoryStatsRuntime>();
        if (advancedStats == null) advancedStats = gameObject.AddComponent<AOGAdvancedInventoryStatsRuntime>();

        BuildRolePlan();
        nextIncome = Time.unscaledTime + 8f;
    }

    private void OnDisable()
    {
        CancelRecallAndRestoreAi();
    }

    private void Update()
    {
        if (identity == null || identity.isHumanPlayer || stats == null || stats.IsDead)
        {
            if (channeling || shopping) CancelRecallAndRestoreAi();
            return;
        }

        if (AOGMatchDirector.Instance == null || AOGMatchDirector.Instance.State != AOGMatchState.Playing)
            return;

        TickPassiveIncome();

        if (channeling)
        {
            UpdateRecall();
            return;
        }

        if (shopping)
            return;

        if (Time.unscaledTime < nextDecision)
            return;
        nextDecision = Time.unscaledTime + 0.45f;

        if (ShouldRecall() && IsSafeToRecall())
            BeginRecall();
    }

    private void TickPassiveIncome()
    {
        if (economy == null || Time.unscaledTime < nextIncome) return;
        nextIncome += Mathf.Max(0.5f,botGoldInterval);
        economy.AddGold(Mathf.Max(1,botGoldPerInterval));
    }

    private bool ShouldRecall()
    {
        float hpRatio = stats.hp / Mathf.Max(1f,stats.maxHp);
        bool lowHp = hpRatio <= lowHpRecallRatio;
        bool enoughGold = economy != null && economy.gold >= Mathf.Max(goldRecallThreshold,NextItemCost());
        bool inventoryFull = economy != null && economy.inventory.Count >= economy.inventoryCapacity;
        bool buildComplete = buildIndex >= build.Count;
        return !inventoryFull && !buildComplete && (lowHp || enoughGold);
    }

    private bool IsSafeToRecall()
    {
        foreach (AOGCharacterStats hero in FindObjectsByType<AOGCharacterStats>(FindObjectsInactive.Exclude,FindObjectsSortMode.None))
        {
            if (hero == null || hero == stats || hero.IsDead || hero.team == stats.team) continue;
            if (FlatDistance(transform.position,hero.transform.position) <= safeEnemyRadius)
                return false;
        }
        return true;
    }

    private void BeginRecall()
    {
        channeling = true;
        recallStarted = Time.time;
        recallStartHp = stats.hp;
        recallStartPosition = transform.position;
        CaptureAndDisableAi();

        Color accent = identity.team == MinionTeam.Blue ? new Color(0.22f,0.68f,1f) : new Color(1f,0.24f,0.30f);
        recallRing = AOGAbilityVisuals.CreateRing("Bot_Recall_Channel",transform.position+Vector3.up*0.05f,2.0f,accent,0.08f);
    }

    private void UpdateRecall()
    {
        if (stats.IsDead || stats.hp < recallStartHp-0.01f || FlatDistance(transform.position,recallStartPosition)>0.22f || !IsSafeToRecall())
        {
            CancelRecallAndRestoreAi();
            return;
        }

        if (Time.time-recallStarted < recallDuration)
            return;

        CompleteRecall();
    }

    private void CompleteRecall()
    {
        channeling = false;
        if (recallRing != null) Destroy(recallRing);
        recallRing = null;

        Transform spawn = AOGBaseAccessUtility.FindTeamBase(identity.team);
        if (spawn != null)
        {
            float direction = identity.team == MinionTeam.Blue ? 1f : -1f;
            Vector3 offset = RoleBaseOffset(identity.role,direction);
            transform.position = spawn.position + offset;
            transform.rotation = spawn.rotation;
        }

        stats.hp = stats.maxHp;
        StartCoroutine(ShopThenReturn());
    }

    private IEnumerator ShopThenReturn()
    {
        shopping = true;
        yield return new WaitForSeconds(0.35f);

        int purchases = 0;
        while (buildIndex < build.Count && economy != null && purchases < 3)
        {
            AOGAdvancedItemDefinition item = build[buildIndex];
            if (!economy.CanBuy(item)) break;
            if (!economy.Buy(item)) break;
            buildIndex++;
            purchases++;
            advancedStats?.Refresh();
            SpawnPurchasePulse(item.accent);
            yield return new WaitForSeconds(0.22f);
        }

        yield return new WaitForSeconds(0.55f);
        shopping = false;
        RestoreAiState();
    }

    private void CancelRecallAndRestoreAi()
    {
        channeling = false;
        shopping = false;
        if (recallRing != null) Destroy(recallRing);
        recallRing = null;
        RestoreAiState();
    }

    private void CaptureAndDisableAi()
    {
        laneAi = GetComponent<AOGBotChampionAI>();
        jungleAi = GetComponent<AOGJungleChampionAIRuntime>();
        laneAiWasEnabled = laneAi != null && laneAi.enabled;
        jungleAiWasEnabled = jungleAi != null && jungleAi.enabled;
        aiStateCaptured = true;
        if (laneAi != null) laneAi.enabled = false;
        if (jungleAi != null) jungleAi.enabled = false;
    }

    private void RestoreAiState()
    {
        if (!aiStateCaptured) return;
        laneAi = GetComponent<AOGBotChampionAI>();
        jungleAi = GetComponent<AOGJungleChampionAIRuntime>();
        if (laneAi != null) laneAi.enabled = laneAiWasEnabled;
        if (jungleAi != null) jungleAi.enabled = jungleAiWasEnabled;
        aiStateCaptured = false;
    }

    private void BuildRolePlan()
    {
        build.Clear();
        if (identity == null) return;

        switch (identity.role)
        {
            case AOGRole.Top:
                build.Add(Item("bot_citadel_plate","CITADEL PLATE",1450,320f,0f,0f,0f,38f,0f,0f,0f,0f,0f,new Color(0.46f,0.58f,0.66f)));
                build.Add(Item("bot_oracle_bastion","ORACLE BASTION",1550,260f,0f,0f,0f,0f,42f,0f,0f,0f,0f,new Color(0.38f,0.76f,0.70f)));
                build.Add(Item("bot_warclock_core","WARCLOCK CORE",1550,0f,18f,0f,0f,0f,0f,0f,28f,0f,0f,new Color(0.82f,0.56f,0.22f)));
                break;
            case AOGRole.Jungle:
                build.Add(Item("bot_riftstep","RIFTSTEP GREAVES",1050,0f,0f,1.15f,0f,0f,0f,0f,0f,0f,0f,new Color(0.32f,0.78f,0.92f)));
                build.Add(Item("bot_moonwell","MOONWELL SIGIL",1350,0f,18f,0f,0f,0f,0f,0f,0f,0.10f,0f,new Color(0.46f,0.58f,1f)));
                build.Add(Item("bot_sunsteel","SUNSTEEL EDGE",1350,0f,32f,0f,0f,0f,0f,0.08f,0f,0f,0f,new Color(0.95f,0.52f,0.18f)));
                break;
            case AOGRole.Mid:
                build.Add(Item("bot_astral_codex","ASTRAL CODEX",1400,0f,0f,0f,72f,0f,0f,0f,10f,0f,0f,new Color(0.34f,0.68f,1f)));
                build.Add(Item("bot_chronicle","CHRONICLE SHARD",1300,0f,0f,0f,45f,0f,0f,0f,22f,0f,0f,new Color(0.44f,0.42f,0.96f)));
                build.Add(Item("bot_ember_crown","EMBER CROWN",1750,0f,0f,0f,96f,0f,0f,0f,0f,0f,0.06f,new Color(1f,0.24f,0.05f)));
                break;
            case AOGRole.ADC:
                build.Add(Item("bot_sunsteel_adc","SUNSTEEL EDGE",1350,0f,32f,0f,0f,0f,0f,0.08f,0f,0f,0f,new Color(0.95f,0.52f,0.18f)));
                build.Add(Item("bot_void_repeater","VOID REPEATER",1650,0f,24f,0f,0f,0f,0f,0.18f,0f,0f,0f,new Color(0.58f,0.26f,0.88f)));
                build.Add(Item("bot_moonwell_adc","MOONWELL SIGIL",1350,0f,18f,0f,0f,0f,0f,0f,0f,0.10f,0f,new Color(0.46f,0.58f,1f)));
                break;
            default:
                build.Add(Item("bot_veil_compass","VEIL COMPASS",1500,0f,0f,0.45f,0f,0f,0f,0f,18f,0f,0f,new Color(0.42f,0.86f,0.66f)));
                build.Add(Item("bot_oracle_support","ORACLE BASTION",1550,260f,0f,0f,0f,0f,42f,0f,0f,0f,0f,new Color(0.38f,0.76f,0.70f)));
                build.Add(Item("bot_spirit_chalice","SPIRIT CHALICE",1450,0f,0f,0f,52f,0f,0f,0f,0f,0f,0.10f,new Color(0.94f,0.34f,0.72f)));
                break;
        }
    }

    private static AOGAdvancedItemDefinition Item(string id,string name,int cost,float hp,float ad,float move,float ap,float armor,float mr,float attackSpeed,float haste,float lifesteal,float spellVamp,Color accent)
    {
        return new AOGAdvancedItemDefinition
        {
            id=id,
            displayName=name,
            cost=cost,
            bonusHp=hp,
            bonusDamage=ad,
            bonusMoveSpeed=move,
            abilityPower=ap,
            armor=armor,
            magicResistance=mr,
            attackSpeedBonus=attackSpeed,
            abilityHaste=haste,
            lifesteal=lifesteal,
            spellVamp=spellVamp,
            accent=accent
        };
    }

    private int NextItemCost()
    {
        if (buildIndex < 0 || buildIndex >= build.Count) return goldRecallThreshold;
        return build[buildIndex].cost;
    }

    private void SpawnPurchasePulse(Color accent)
    {
        GameObject ring = AOGAbilityVisuals.CreateRing("Bot_Item_Purchase",transform.position+Vector3.up*0.05f,1.7f,accent,0.08f);
        Destroy(ring,0.45f);
    }

    private static Vector3 RoleBaseOffset(AOGRole role,float direction)
    {
        switch (role)
        {
            case AOGRole.Top: return new Vector3(-3.0f,0.25f,2.8f*direction);
            case AOGRole.Jungle: return new Vector3(-1.4f,0.25f,2.2f*direction);
            case AOGRole.Mid: return new Vector3(0f,0.25f,2.7f*direction);
            case AOGRole.ADC: return new Vector3(1.5f,0.25f,2.3f*direction);
            default: return new Vector3(3.0f,0.25f,2.6f*direction);
        }
    }

    private static float FlatDistance(Vector3 a,Vector3 b)
    {
        a.y=0f;
        b.y=0f;
        return Vector3.Distance(a,b);
    }
}

public class AOGBotRecallRoleShoppingBootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindFirstObjectByType<AOGBotRecallRoleShoppingBootstrap>() != null) return;
        GameObject host = new GameObject("AOG_Bot_Recall_Role_Shopping_Bootstrap");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGBotRecallRoleShoppingBootstrap>();
    }

    private float nextScan;
    private void Update()
    {
        if (Time.unscaledTime < nextScan) return;
        nextScan = Time.unscaledTime + 0.75f;

        foreach (AOGTeamMemberIdentity member in FindObjectsByType<AOGTeamMemberIdentity>(FindObjectsInactive.Exclude,FindObjectsSortMode.None))
        {
            if (member == null || member.isHumanPlayer) continue;
            if (member.GetComponent<AOGBotRecallRoleShoppingRuntime>() == null)
                member.gameObject.AddComponent<AOGBotRecallRoleShoppingRuntime>();
        }
    }
}
