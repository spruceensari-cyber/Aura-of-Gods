using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Adds a lightweight tactical layer over AOGBotChampionAI without replacing its movement/attack authority.
/// Improves retreat thresholds, last-hit selection, carry spacing and tower-dive restraint.
/// </summary>
[DefaultExecutionOrder(90)]
public class AOGOriginalBotTacticalDirectorRuntime : MonoBehaviour
{
    private readonly List<Minion> nearby = new List<Minion>(24);
    private FieldInfo currentTargetField;
    private AOGBotChampionAI ai;
    private AOGCharacterStats stats;
    private float nextThink;

    private void Awake()
    {
        ai=GetComponent<AOGBotChampionAI>();
        stats=GetComponent<AOGCharacterStats>();
        currentTargetField=typeof(AOGBotChampionAI).GetField("currentTarget",BindingFlags.Instance|BindingFlags.NonPublic);
        TuneRole();
    }

    private void Update()
    {
        if(ai==null || stats==null || stats.IsDead) return;
        if(AOGMatchDirector.Instance==null || AOGMatchDirector.Instance.State!=AOGMatchState.Playing) return;
        if(Time.time<nextThink) return;
        nextThink=Time.time+0.16f;
        TacticalThink();
    }

    private void TuneRole()
    {
        if(ai==null) return;
        ai.decisionInterval=0.24f;
        switch(ai.role)
        {
            case AOGBotRole.Carry:
                ai.retreatHpRatio=0.38f; ai.heroAggroRange=7.6f; ai.abilityRange=7.4f; break;
            case AOGBotRole.Support:
                ai.retreatHpRatio=0.42f; ai.heroAggroRange=7.0f; ai.abilityRange=7.8f; break;
            case AOGBotRole.Mid:
                ai.retreatHpRatio=0.34f; ai.heroAggroRange=8.8f; ai.abilityRange=8.2f; break;
            case AOGBotRole.Top:
                ai.retreatHpRatio=0.31f; ai.heroAggroRange=8.2f; ai.abilityRange=6.8f; break;
            default:
                ai.retreatHpRatio=0.36f; ai.heroAggroRange=7.8f; ai.abilityRange=7.2f; break;
        }
    }

    private void TacticalThink()
    {
        float hpRatio=stats.hp/Mathf.Max(1f,stats.maxHp);
        if(hpRatio<=ai.retreatHpRatio)
        {
            SetTarget(null);
            return;
        }

        Transform target=GetTarget();
        if(target!=null && IsUnsafeTowerDive(target,hpRatio))
        {
            SetTarget(null);
            return;
        }

        if(ai.role==AOGBotRole.Support) return;

        Minion lastHit=FindLastHitMinion();
        if(lastHit!=null)
            SetTarget(lastHit.transform);
    }

    private Minion FindLastHitMinion()
    {
        AOGMinionSpatialGridRuntime.Query(transform.position,8.5f,nearby);
        Minion best=null;
        float bestScore=float.MaxValue;
        for(int i=0;i<nearby.Count;i++)
        {
            Minion minion=nearby[i];
            if(minion==null || minion.hp<=0f || minion.team==ai.team) continue;
            float expected=Mathf.Max(1f,stats.attackDamage*1.15f);
            if(minion.hp>expected*1.55f) continue;
            float distance=FlatDistance(transform.position,minion.transform.position);
            float score=minion.hp/expected+distance*0.08f;
            if(score<bestScore){best=minion;bestScore=score;}
        }
        return best;
    }

    private bool IsUnsafeTowerDive(Transform target,float hpRatio)
    {
        AOGCharacterStats enemyHero=target.GetComponentInParent<AOGCharacterStats>();
        if(enemyHero==null) return false;

        foreach(TowerHealth tower in AOGWorldRegistry.Towers)
        {
            if(tower==null || tower.hp<=0f || tower.towerTeam==ai.team) continue;
            float targetTowerDistance=FlatDistance(enemyHero.transform.position,tower.transform.position);
            float selfTowerDistance=FlatDistance(transform.position,tower.transform.position);
            if(targetTowerDistance<10.5f && selfTowerDistance<13.5f)
            {
                bool strongDive=hpRatio>0.72f && enemyHero.hp/enemyHero.maxHp<0.18f;
                return !strongDive;
            }
        }
        return false;
    }

    private Transform GetTarget()
    {
        return currentTargetField!=null ? currentTargetField.GetValue(ai) as Transform : null;
    }

    private void SetTarget(Transform target)
    {
        if(currentTargetField!=null) currentTargetField.SetValue(ai,target);
    }

    private static float FlatDistance(Vector3 a,Vector3 b)
    {
        a.y=0f;b.y=0f;return Vector3.Distance(a,b);
    }
}

[DefaultExecutionOrder(80)]
public class AOGOriginalBotTacticalBootstrap : MonoBehaviour
{
    private float nextRefresh;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if(FindFirstObjectByType<AOGOriginalBotTacticalBootstrap>()!=null) return;
        GameObject host=new GameObject("AOG_Original_Bot_Tactical_Bootstrap");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGOriginalBotTacticalBootstrap>();
    }

    private void Update()
    {
        if(Time.unscaledTime<nextRefresh) return;
        nextRefresh=Time.unscaledTime+1.0f;
        foreach(AOGBotChampionAI bot in FindObjectsByType<AOGBotChampionAI>(FindObjectsInactive.Exclude,FindObjectsSortMode.None))
            if(bot!=null && bot.GetComponent<AOGOriginalBotTacticalDirectorRuntime>()==null)
                bot.gameObject.AddComponent<AOGOriginalBotTacticalDirectorRuntime>();
    }
}
