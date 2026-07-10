using System.Collections;
using UnityEngine;

public class AOGBotRecallShoppingRuntime : MonoBehaviour
{
    public float recallDuration = 6.5f;
    public float lowHpRecallRatio = 0.24f;
    public int goldRecallThreshold = 1150;

    private AOGCharacterStats stats;
    private AOGPlayerEconomy economy;
    private AOGTeamMemberIdentity identity;
    private AOGBotChampionAI laneAi;
    private AOGJungleChampionAIRuntime jungleAi;
    private bool recalling;
    private float recallStartHp;
    private Coroutine recallRoutine;

    private void Awake()
    {
        stats=GetComponent<AOGCharacterStats>();
        identity=GetComponent<AOGTeamMemberIdentity>();
        economy=GetComponent<AOGPlayerEconomy>();
        if (economy==null) economy=gameObject.AddComponent<AOGPlayerEconomy>();
        laneAi=GetComponent<AOGBotChampionAI>();
        jungleAi=GetComponent<AOGJungleChampionAIRuntime>();
    }

    private void Update()
    {
        if (stats==null || identity==null || identity.isHumanPlayer || stats.IsDead || recalling)
            return;
        if (AOGMatchDirector.Instance==null || AOGMatchDirector.Instance.State!=AOGMatchState.Playing)
            return;

        float hpRatio=stats.hp/Mathf.Max(1f,stats.maxHp);
        bool wantsRecall=hpRatio<=lowHpRecallRatio || (economy!=null && economy.gold>=goldRecallThreshold);
        if (wantsRecall && IsSafeToRecall())
            recallRoutine=StartCoroutine(RecallRoutine());
    }

    private bool IsSafeToRecall()
    {
        foreach (AOGCharacterStats hero in FindObjectsByType<AOGCharacterStats>(FindObjectsInactive.Exclude,FindObjectsSortMode.None))
        {
            if (hero==null || hero==stats || hero.IsDead || hero.team==stats.team) continue;
            Vector3 a=transform.position;a.y=0f;
            Vector3 b=hero.transform.position;b.y=0f;
            if (Vector3.Distance(a,b)<8.5f) return false;
        }
        return true;
    }

    private IEnumerator RecallRoutine()
    {
        recalling=true;
        recallStartHp=stats.hp;
        SetAiEnabled(false);
        Vector3 startPosition=transform.position;
        GameObject ring=AOGAbilityVisuals.CreateRing("Bot_Recall_"+identity.displayName,transform.position+Vector3.up*0.05f,1.8f,identity.team==MinionTeam.Blue?new Color(0.2f,0.65f,1f):new Color(1f,0.28f,0.3f),0.08f);

        float elapsed=0f;
        while (elapsed<recallDuration)
        {
            if (stats==null || stats.IsDead || stats.hp<recallStartHp-0.01f || FlatDistance(transform.position,startPosition)>0.20f)
            {
                if (ring!=null) Destroy(ring);
                recalling=false;
                SetAiEnabled(true);
                yield break;
            }
            elapsed+=Time.deltaTime;
            yield return null;
        }

        if (ring!=null) Destroy(ring);
        Transform spawn=AOGBaseAccessUtility.FindTeamBase(stats.team);
        if (spawn!=null)
        {
            float direction=stats.team==MinionTeam.Blue?1f:-1f;
            transform.position=spawn.position+new Vector3(0f,0.25f,2f*direction);
            transform.rotation=spawn.rotation;
        }
        stats.hp=stats.maxHp;
        BuyRoleItem();
        yield return new WaitForSeconds(0.35f);
        recalling=false;
        SetAiEnabled(true);
        recallRoutine=null;
    }

    private void BuyRoleItem()
    {
        if (economy==null || identity==null || economy.inventory.Count>=economy.inventoryCapacity)
            return;

        AOGItemDefinition item=BuildRoleItem(identity.role,economy.inventory.Count);
        if (item!=null && economy.CanBuy(item))
            economy.Buy(item);
    }

    private static AOGItemDefinition BuildRoleItem(AOGRole role,int purchaseIndex)
    {
        if (role==AOGRole.Top)
        {
            return purchaseIndex%2==0
                ? Item("bot_titan_guard","TITAN GUARD",1050,320f,0f,0f,1f,new Color(0.72f,0.35f,0.22f))
                : Item("bot_war_edge","WAR EDGE",1150,120f,28f,0f,1f,new Color(0.9f,0.48f,0.18f));
        }
        if (role==AOGRole.ADC)
        {
            return purchaseIndex%2==0
                ? Item("bot_star_bow","STAR BOW",1050,0f,30f,0f,0.90f,new Color(0.22f,0.78f,1f))
                : Item("bot_hunter_step","HUNTER STEP",900,0f,16f,0.7f,0.94f,new Color(0.38f,0.88f,0.92f));
        }
        if (role==AOGRole.Mid)
        {
            return purchaseIndex%2==0
                ? Item("bot_arcane_core","ARCANE CORE",1100,90f,34f,0f,0.94f,new Color(0.66f,0.32f,0.96f))
                : Item("bot_phase_boots","PHASE BOOTS",900,0f,10f,1.0f,1f,new Color(0.42f,0.68f,1f));
        }
        if (role==AOGRole.Support)
        {
            return purchaseIndex%2==0
                ? Item("bot_aether_veil","AETHER VEIL",950,240f,8f,0.4f,1f,new Color(0.32f,0.86f,0.72f))
                : Item("bot_guardian_light","GUARDIAN LIGHT",1050,280f,12f,0f,0.96f,new Color(0.78f,0.86f,0.52f));
        }
        return purchaseIndex%2==0
            ? Item("bot_fang_relic","FANG RELIC",1050,90f,30f,0.5f,0.96f,new Color(0.8f,0.28f,0.18f))
            : Item("bot_hunt_boots","HUNT BOOTS",900,0f,16f,1.0f,1f,new Color(0.48f,0.72f,0.52f));
    }

    private static AOGItemDefinition Item(string id,string name,int cost,float hp,float damage,float speed,float cooldown,Color accent)
    {
        return new AOGItemDefinition
        {
            id=id,
            displayName=name,
            description="ROLE BUILD",
            cost=cost,
            bonusHp=hp,
            bonusDamage=damage,
            bonusMoveSpeed=speed,
            attackCooldownMultiplier=cooldown,
            accent=accent
        };
    }

    private void SetAiEnabled(bool enabled)
    {
        if (laneAi!=null) laneAi.enabled=enabled;
        if (jungleAi==null) jungleAi=GetComponent<AOGJungleChampionAIRuntime>();
        if (jungleAi!=null) jungleAi.enabled=enabled;
    }

    private static float FlatDistance(Vector3 a,Vector3 b)
    {
        a.y=0f;b.y=0f;return Vector3.Distance(a,b);
    }
}

public class AOGBotRecallShoppingBootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindFirstObjectByType<AOGBotRecallShoppingBootstrap>()!=null) return;
        GameObject host=new GameObject("AOG_Bot_Recall_Shopping_Bootstrap");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGBotRecallShoppingBootstrap>();
    }

    private float nextScan;
    private void Update()
    {
        if (Time.unscaledTime<nextScan) return;
        nextScan=Time.unscaledTime+0.75f;
        foreach (AOGTeamMemberIdentity member in FindObjectsByType<AOGTeamMemberIdentity>(FindObjectsInactive.Exclude,FindObjectsSortMode.None))
        {
            if (member==null || member.isHumanPlayer) continue;
            if (member.GetComponent<AOGBotRecallShoppingRuntime>()==null)
                member.gameObject.AddComponent<AOGBotRecallShoppingRuntime>();
        }
    }
}
