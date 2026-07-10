using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Competitive macro layer for non-human team members. It does not deal damage or replace champion AI.
/// It removes omniscient hero locks, adds finite last-seen memory, tunes objective urgency and allows
/// Support/Jungle roles to place team wards around contested river/objective approaches.
/// </summary>
[DefaultExecutionOrder(70)]
public class AOGVisionAwareMacroAIRuntime : MonoBehaviour
{
    public float visionThinkInterval = 0.20f;
    public float macroThinkInterval = 0.75f;
    public float lastSeenMemorySeconds = 4.5f;
    public float wardCooldown = 78f;
    public float wardSearchRadius = 13f;

    private AOGTeamMemberIdentity identity;
    private AOGCharacterStats stats;
    private AOGBotChampionAI laneAi;
    private AOGJungleChampionAIRuntime jungleAi;

    private float nextVisionThink;
    private float nextMacroThink;
    private float nextWard;
    private float baseLaneAggro;
    private float baseLaneRetreat;
    private float baseObjectiveJoin;
    private float baseJungleAggro;
    private bool captured;

    private FieldInfo laneTargetField;
    private FieldInfo jungleTargetHeroField;

    private readonly Dictionary<AOGCharacterStats,LastSeenRecord> memory = new Dictionary<AOGCharacterStats,LastSeenRecord>();

    private struct LastSeenRecord
    {
        public Vector3 position;
        public float seenAt;
    }

    private void Awake()
    {
        identity = GetComponent<AOGTeamMemberIdentity>();
        stats = GetComponent<AOGCharacterStats>();
        laneAi = GetComponent<AOGBotChampionAI>();
        jungleAi = GetComponent<AOGJungleChampionAIRuntime>();

        BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        laneTargetField = typeof(AOGBotChampionAI).GetField("currentTarget",flags);
        jungleTargetHeroField = typeof(AOGJungleChampionAIRuntime).GetField("targetHero",flags);
        CaptureBaseValues();
    }

    private void Update()
    {
        if (identity == null || identity.isHumanPlayer || stats == null || stats.IsDead) return;
        if (AOGMatchDirector.Instance == null || AOGMatchDirector.Instance.State != AOGMatchState.Playing) return;

        if (!captured) CaptureBaseValues();

        if (Time.time >= nextVisionThink)
        {
            nextVisionThink = Time.time + visionThinkInterval;
            RefreshEnemyMemory();
            BreakOmniscientLocks();
        }

        if (Time.time >= nextMacroThink)
        {
            nextMacroThink = Time.time + macroThinkInterval;
            EvaluateMacroState();
        }
    }

    private void CaptureBaseValues()
    {
        laneAi = GetComponent<AOGBotChampionAI>();
        jungleAi = GetComponent<AOGJungleChampionAIRuntime>();

        if (laneAi != null)
        {
            baseLaneAggro = laneAi.heroAggroRange;
            baseLaneRetreat = laneAi.retreatHpRatio;
            captured = true;
        }
        if (jungleAi != null)
        {
            baseObjectiveJoin = jungleAi.objectiveJoinRadius;
            baseJungleAggro = jungleAi.enemyHeroAggroRadius;
            captured = true;
        }
    }

    private void RefreshEnemyMemory()
    {
        List<AOGCharacterStats> stale = null;
        foreach (AOGCharacterStats hero in AOGWorldRegistry.Characters)
        {
            if (hero == null || hero == stats || hero.team == stats.team || hero.IsDead) continue;
            if (AOGVisionAuthorityRuntime.IsVisibleToTeam(hero.transform.position,stats.team))
            {
                memory[hero] = new LastSeenRecord { position = hero.transform.position, seenAt = Time.time };
            }
        }

        foreach (KeyValuePair<AOGCharacterStats,LastSeenRecord> pair in memory)
        {
            if (pair.Key == null || pair.Key.IsDead || Time.time-pair.Value.seenAt > lastSeenMemorySeconds)
            {
                if (stale == null) stale = new List<AOGCharacterStats>();
                stale.Add(pair.Key);
            }
        }

        if (stale != null)
            foreach (AOGCharacterStats key in stale) memory.Remove(key);
    }

    private void BreakOmniscientLocks()
    {
        if (laneAi != null && laneTargetField != null)
        {
            Transform target = laneTargetField.GetValue(laneAi) as Transform;
            AOGCharacterStats enemy = target != null ? target.GetComponentInParent<AOGCharacterStats>() : null;
            if (enemy != null && enemy.team != stats.team && !AOGVisionAuthorityRuntime.IsVisibleToTeam(enemy.transform.position,stats.team))
                laneTargetField.SetValue(laneAi,null);
        }

        if (jungleAi != null && jungleTargetHeroField != null)
        {
            AOGCharacterStats enemy = jungleTargetHeroField.GetValue(jungleAi) as AOGCharacterStats;
            if (enemy != null && !AOGVisionAuthorityRuntime.IsVisibleToTeam(enemy.transform.position,stats.team))
                jungleTargetHeroField.SetValue(jungleAi,null);
        }
    }

    private void EvaluateMacroState()
    {
        AOGNeutralBossAI objective = FindPriorityObjective();
        bool objectiveEngaged = objective != null && objective.hp < objective.maxHp*0.985f;
        bool lateGame = AOGMatchDirector.Instance != null && AOGMatchDirector.Instance.MatchTime >= 540f;
        int nearbyAllies = CountHeroes(transform.position,15f,stats.team,true);
        int nearbyVisibleEnemies = CountHeroes(transform.position,15f,stats.team,false);
        bool outnumbered = nearbyVisibleEnemies > nearbyAllies+1;

        if (laneAi != null)
        {
            float objectiveDistance = objective != null ? FlatDistance(transform.position,objective.transform.position) : float.MaxValue;
            bool objectiveFightNearby = objectiveEngaged && objectiveDistance <= 34f;

            laneAi.heroAggroRange = outnumbered
                ? Mathf.Max(5f,baseLaneAggro-2f)
                : objectiveFightNearby ? baseLaneAggro+2.2f : baseLaneAggro;

            laneAi.retreatHpRatio = outnumbered
                ? Mathf.Max(baseLaneRetreat,0.38f)
                : objectiveFightNearby ? Mathf.Max(0.20f,baseLaneRetreat-0.04f) : baseLaneRetreat;
        }

        if (jungleAi != null)
        {
            jungleAi.objectiveJoinRadius = objective != null
                ? Mathf.Max(baseObjectiveJoin, objectiveEngaged ? 72f : lateGame ? 64f : 54f)
                : baseObjectiveJoin;
            jungleAi.enemyHeroAggroRadius = outnumbered
                ? Mathf.Max(6f,baseJungleAggro-1.5f)
                : baseJungleAggro;
        }

        if ((identity.role == AOGRole.Support || identity.role == AOGRole.Jungle) && Time.time >= nextWard)
            TryPlaceMacroWard(objective,objectiveEngaged,lateGame);
    }

    private void TryPlaceMacroWard(AOGNeutralBossAI objective,bool objectiveEngaged,bool lateGame)
    {
        float hpRatio = stats.hp/Mathf.Max(1f,stats.maxHp);
        if (hpRatio < 0.48f) return;

        Vector3 point = ResolveWardPoint(objective,objectiveEngaged,lateGame);
        if (FlatDistance(transform.position,point) > wardSearchRadius) return;
        if (HasAlliedWardNear(point,10f))
        {
            nextWard = Time.time+12f;
            return;
        }

        AOGWardRuntime.Spawn(point,stats.team);
        nextWard = Time.time+wardCooldown;
    }

    private Vector3 ResolveWardPoint(AOGNeutralBossAI objective,bool objectiveEngaged,bool lateGame)
    {
        if (objective != null && (objectiveEngaged || lateGame))
        {
            Vector3 direction = transform.position-objective.transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.01f) direction = transform.forward;
            return objective.transform.position+direction.normalized*7f;
        }

        Vector3[] bluePoints =
        {
            new Vector3(-18f,0f,-2f), new Vector3(10f,0f,-18f),
            new Vector3(24f,0f,10f), new Vector3(-6f,0f,22f)
        };
        Vector3[] redPoints =
        {
            new Vector3(18f,0f,2f), new Vector3(-10f,0f,18f),
            new Vector3(-24f,0f,-10f), new Vector3(6f,0f,-22f)
        };
        Vector3[] points = stats.team == MinionTeam.Blue ? bluePoints : redPoints;

        Vector3 best = points[0];
        float bestDistance = FlatDistance(transform.position,best);
        for (int i=1;i<points.Length;i++)
        {
            float distance = FlatDistance(transform.position,points[i]);
            if (distance < bestDistance)
            {
                best = points[i];
                bestDistance = distance;
            }
        }
        return best;
    }

    private bool HasAlliedWardNear(Vector3 point,float radius)
    {
        foreach (AOGWardRuntime ward in AOGVisionAuthorityRuntime.ActiveWards)
        {
            if (ward == null || ward.team != stats.team) continue;
            if (FlatDistance(ward.transform.position,point) <= radius) return true;
        }
        return false;
    }

    private AOGNeutralBossAI FindPriorityObjective()
    {
        AOGNeutralBossAI best = null;
        float bestScore = float.MaxValue;

        foreach (AOGNeutralBossAI boss in AOGWorldRegistry.Bosses)
        {
            if (boss == null || boss.IsDead) continue;
            float hpRatio = boss.hp/Mathf.Max(1f,boss.maxHp);
            float distance = FlatDistance(transform.position,boss.transform.position);
            bool visible = AOGVisionAuthorityRuntime.IsVisibleToTeam(boss.transform.position,stats.team);
            bool engaged = hpRatio < 0.985f;

            float score = distance + hpRatio*8f + (engaged ? -24f : 0f) + (visible ? -6f : 0f);
            if (score < bestScore)
            {
                best = boss;
                bestScore = score;
            }
        }
        return best;
    }

    private int CountHeroes(Vector3 point,float radius,MinionTeam observer,bool allied)
    {
        int count = 0;
        foreach (AOGCharacterStats hero in AOGWorldRegistry.Characters)
        {
            if (hero == null || hero.IsDead) continue;
            bool sameTeam = hero.team == observer;
            if (allied != sameTeam) continue;
            if (!allied && !AOGVisionAuthorityRuntime.IsVisibleToTeam(hero.transform.position,observer)) continue;
            if (FlatDistance(point,hero.transform.position) <= radius) count++;
        }
        return count;
    }

    public bool TryGetRecentEnemyMemory(out Vector3 lastSeenPosition)
    {
        lastSeenPosition = transform.position;
        float newest = float.MinValue;
        bool found = false;
        foreach (KeyValuePair<AOGCharacterStats,LastSeenRecord> pair in memory)
        {
            if (pair.Key == null) continue;
            if (pair.Value.seenAt > newest)
            {
                newest = pair.Value.seenAt;
                lastSeenPosition = pair.Value.position;
                found = true;
            }
        }
        return found;
    }

    private static float FlatDistance(Vector3 a,Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a,b);
    }
}

[DefaultExecutionOrder(-570)]
public class AOGVisionAwareMacroAIBootstrap : MonoBehaviour
{
    private float nextAttach;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindFirstObjectByType<AOGVisionAwareMacroAIBootstrap>() != null) return;
        GameObject host = new GameObject("AOG_Vision_Aware_Macro_AI_Bootstrap");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGVisionAwareMacroAIBootstrap>();
    }

    private void Update()
    {
        if (Time.unscaledTime < nextAttach) return;
        nextAttach = Time.unscaledTime+0.8f;

        foreach (AOGTeamMemberIdentity member in AOGWorldRegistry.TeamMembers)
        {
            if (member == null || member.isHumanPlayer) continue;
            if (member.GetComponent<AOGVisionAwareMacroAIRuntime>() == null)
                member.gameObject.AddComponent<AOGVisionAwareMacroAIRuntime>();
        }
    }
}
