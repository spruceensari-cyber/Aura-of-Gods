using UnityEngine;

/// <summary>
/// Adaptive tactical layer. It does not move characters or select targets directly.
/// Instead it tunes the existing role AI parameters based on local teamfight context.
/// </summary>
public class AOGRoleTacticalDirectiveRuntime : MonoBehaviour
{
    private AOGTeamMemberIdentity identity;
    private AOGCharacterStats stats;
    private AOGBotChampionAI laneAi;
    private AOGJungleChampionAIRuntime jungleAi;

    private float baseAggro;
    private float baseRetreat;
    private float baseAbilityRange;
    private float baseJungleAggro;
    private float baseJungleRetreat;
    private float baseObjectiveJoin;
    private bool initialized;
    private float nextThink;

    private void Awake()
    {
        identity=GetComponent<AOGTeamMemberIdentity>();
        stats=GetComponent<AOGCharacterStats>();
        laneAi=GetComponent<AOGBotChampionAI>();
        jungleAi=GetComponent<AOGJungleChampionAIRuntime>();
        CaptureBaseValues();
    }

    private void Update()
    {
        if(identity==null||identity.isHumanPlayer||stats==null||stats.IsDead)
            return;
        if(AOGMatchDirector.Instance==null||AOGMatchDirector.Instance.State!=AOGMatchState.Playing)
            return;
        if(Time.time<nextThink)
            return;

        nextThink=Time.time+0.35f;
        if(!initialized)CaptureBaseValues();
        EvaluateLocalBattlefield();
    }

    private void CaptureBaseValues()
    {
        laneAi=GetComponent<AOGBotChampionAI>();
        jungleAi=GetComponent<AOGJungleChampionAIRuntime>();
        if(laneAi!=null)
        {
            baseAggro=laneAi.heroAggroRange;
            baseRetreat=laneAi.retreatHpRatio;
            baseAbilityRange=laneAi.abilityRange;
            initialized=true;
        }
        if(jungleAi!=null)
        {
            baseJungleAggro=jungleAi.enemyHeroAggroRadius;
            baseJungleRetreat=jungleAi.retreatHpRatio;
            baseObjectiveJoin=jungleAi.objectiveJoinRadius;
            initialized=true;
        }
    }

    private void EvaluateLocalBattlefield()
    {
        int allies=0;
        int enemies=0;
        int alliedFrontliners=0;
        int enemyDivers=0;
        bool carryThreatened=false;

        AOGCharacterStats alliedCarry=FindAlliedRole(AOGRole.ADC,30f);
        foreach(AOGCharacterStats hero in FindObjectsByType<AOGCharacterStats>(FindObjectsInactive.Exclude,FindObjectsSortMode.None))
        {
            if(hero==null||hero==stats||hero.IsDead)continue;
            float distance=FlatDistance(transform.position,hero.transform.position);
            if(distance>13f)continue;

            if(hero.team==stats.team)
            {
                allies++;
                AOGTeamMemberIdentity member=hero.GetComponent<AOGTeamMemberIdentity>();
                if(member!=null&&(member.role==AOGRole.Top||member.role==AOGRole.Jungle))alliedFrontliners++;
            }
            else
            {
                enemies++;
                AOGTeamMemberIdentity member=hero.GetComponent<AOGTeamMemberIdentity>();
                if(member!=null&&(member.role==AOGRole.Top||member.role==AOGRole.Jungle))enemyDivers++;
                if(alliedCarry!=null&&FlatDistance(alliedCarry.transform.position,hero.transform.position)<=6.5f)carryThreatened=true;
            }
        }

        bool outnumbered=enemies>allies+1;
        bool advantaged=allies>=enemies+1;
        bool frontlinePresent=alliedFrontliners>0;

        if(identity.role==AOGRole.Jungle)
        {
            ApplyJungleDirective(outnumbered,advantaged);
            return;
        }

        if(laneAi==null)return;

        switch(identity.role)
        {
            case AOGRole.ADC:
                laneAi.heroAggroRange=frontlinePresent&&advantaged?baseAggro+2.0f:outnumbered?Mathf.Max(5.2f,baseAggro-2.2f):baseAggro;
                laneAi.retreatHpRatio=outnumbered?Mathf.Max(0.44f,baseRetreat+0.10f):frontlinePresent?Mathf.Max(0.30f,baseRetreat-0.02f):baseRetreat;
                laneAi.abilityRange=frontlinePresent?baseAbilityRange+1.0f:baseAbilityRange;
                break;

            case AOGRole.Support:
                laneAi.heroAggroRange=carryThreatened?baseAggro+3.0f:outnumbered?baseAggro+1.0f:baseAggro;
                laneAi.retreatHpRatio=carryThreatened?Mathf.Max(0.24f,baseRetreat-0.04f):baseRetreat;
                laneAi.abilityRange=carryThreatened?baseAbilityRange+1.5f:baseAbilityRange;
                break;

            case AOGRole.Top:
                laneAi.heroAggroRange=advantaged?baseAggro+2.5f:outnumbered?Mathf.Max(6f,baseAggro-1.5f):baseAggro;
                laneAi.retreatHpRatio=advantaged?Mathf.Max(0.16f,baseRetreat-0.05f):outnumbered?Mathf.Max(0.30f,baseRetreat+0.08f):baseRetreat;
                laneAi.abilityRange=baseAbilityRange+(advantaged?0.8f:0f);
                break;

            case AOGRole.Mid:
                laneAi.heroAggroRange=frontlinePresent?baseAggro+1.8f:outnumbered?Mathf.Max(6.5f,baseAggro-1.4f):baseAggro;
                laneAi.retreatHpRatio=outnumbered?Mathf.Max(0.36f,baseRetreat+0.08f):baseRetreat;
                laneAi.abilityRange=frontlinePresent?baseAbilityRange+1.6f:baseAbilityRange;
                break;
        }
    }

    private void ApplyJungleDirective(bool outnumbered,bool advantaged)
    {
        if(jungleAi==null)
        {
            jungleAi=GetComponent<AOGJungleChampionAIRuntime>();
            if(jungleAi==null)return;
            baseJungleAggro=jungleAi.enemyHeroAggroRadius;
            baseJungleRetreat=jungleAi.retreatHpRatio;
            baseObjectiveJoin=jungleAi.objectiveJoinRadius;
        }

        float hpRatio=stats.hp/Mathf.Max(1f,stats.maxHp);
        bool objectiveAlive=FindActiveObjectiveWithin(70f)!=null;
        bool lateGame=AOGMatchDirector.Instance!=null&&AOGMatchDirector.Instance.MatchTime>=540f;

        jungleAi.enemyHeroAggroRadius=advantaged?baseJungleAggro+2.5f:outnumbered?Mathf.Max(6f,baseJungleAggro-1.5f):baseJungleAggro;
        jungleAi.retreatHpRatio=outnumbered?Mathf.Max(0.34f,baseJungleRetreat+0.10f):baseJungleRetreat;
        jungleAi.objectiveJoinRadius=objectiveAlive&&hpRatio>=0.68f?(lateGame?Mathf.Max(baseObjectiveJoin,68f):Mathf.Max(baseObjectiveJoin,56f)):baseObjectiveJoin;
        jungleAi.campSearchRadius=objectiveAlive&&hpRatio>=0.68f?Mathf.Min(jungleAi.campSearchRadius,58f):90f;
    }

    private AOGNeutralBossAI FindActiveObjectiveWithin(float radius)
    {
        AOGNeutralBossAI best=null;
        float bestDistance=radius;
        foreach(AOGNeutralBossAI boss in FindObjectsByType<AOGNeutralBossAI>(FindObjectsInactive.Exclude,FindObjectsSortMode.None))
        {
            if(boss==null||boss.IsDead)continue;
            float distance=FlatDistance(transform.position,boss.transform.position);
            if(distance<bestDistance)
            {
                best=boss;
                bestDistance=distance;
            }
        }
        return best;
    }

    private AOGCharacterStats FindAlliedRole(AOGRole role,float range)
    {
        AOGCharacterStats best=null;
        float bestDistance=range;
        foreach(AOGTeamMemberIdentity member in FindObjectsByType<AOGTeamMemberIdentity>(FindObjectsInactive.Exclude,FindObjectsSortMode.None))
        {
            if(member==null||member.team!=stats.team||member.role!=role)continue;
            AOGCharacterStats candidate=member.GetComponent<AOGCharacterStats>();
            if(candidate==null||candidate.IsDead)continue;
            float distance=FlatDistance(transform.position,candidate.transform.position);
            if(distance<bestDistance)
            {
                best=candidate;
                bestDistance=distance;
            }
        }
        return best;
    }

    private static float FlatDistance(Vector3 a,Vector3 b)
    {
        a.y=0f;b.y=0f;return Vector3.Distance(a,b);
    }
}

public class AOGRoleTacticalDirectiveBootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if(FindFirstObjectByType<AOGRoleTacticalDirectiveBootstrap>()!=null)return;
        GameObject host=new GameObject("AOG_Role_Tactical_Directive_Bootstrap");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGRoleTacticalDirectiveBootstrap>();
    }

    private float nextScan;
    private void Update()
    {
        if(Time.unscaledTime<nextScan)return;
        nextScan=Time.unscaledTime+0.8f;
        foreach(AOGTeamMemberIdentity member in FindObjectsByType<AOGTeamMemberIdentity>(FindObjectsInactive.Exclude,FindObjectsSortMode.None))
        {
            if(member==null||member.isHumanPlayer)continue;
            if(member.GetComponent<AOGRoleTacticalDirectiveRuntime>()==null)
                member.gameObject.AddComponent<AOGRoleTacticalDirectiveRuntime>();
        }
    }
}
