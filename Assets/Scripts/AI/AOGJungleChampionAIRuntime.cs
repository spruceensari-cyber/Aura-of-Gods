using System.Collections;
using UnityEngine;

[DefaultExecutionOrder(60)]
public class AOGJungleChampionAIRuntime : MonoBehaviour
{
    public float campSearchRadius = 90f;
    public float enemyHeroAggroRadius = 8.5f;
    public float retreatHpRatio = 0.24f;
    public float objectiveJoinRadius = 46f;

    private AOGCharacterStats stats;
    private ChampionPresentationController presentation;
    private AOGTeamMemberIdentity identity;
    private AOGNeutralMonsterRuntime targetMonster;
    private AOGNeutralBossAI targetBoss;
    private AOGCharacterStats targetHero;
    private Vector3 spawnPoint;
    private float nextDecision;
    private float nextAttack;
    private Coroutine attackRoutine;

    private void Awake()
    {
        stats = GetComponent<AOGCharacterStats>();
        presentation = GetComponent<ChampionPresentationController>();
        identity = GetComponent<AOGTeamMemberIdentity>();
        spawnPoint = transform.position;
    }

    private void Update()
    {
        if (stats == null || stats.IsDead || identity == null || identity.role != AOGRole.Jungle)
            return;
        if (AOGMatchDirector.Instance == null || AOGMatchDirector.Instance.State != AOGMatchState.Playing)
            return;

        if (stats.hp / Mathf.Max(1f, stats.maxHp) <= retreatHpRatio)
        {
            ClearTargets();
            MoveToward(spawnPoint, stats.moveSpeed * 0.92f);
            return;
        }

        if (Time.time >= nextDecision)
        {
            nextDecision = Time.time + 0.35f;
            DecideTarget();
        }

        if (targetHero != null && !targetHero.IsDead && targetHero.gameObject.activeInHierarchy)
        {
            FightHero(targetHero);
            return;
        }

        if (targetBoss != null && !targetBoss.IsDead && targetBoss.gameObject.activeInHierarchy)
        {
            FightBoss(targetBoss);
            return;
        }

        if (targetMonster != null && !targetMonster.IsDead && targetMonster.gameObject.activeInHierarchy)
        {
            FightMonster(targetMonster);
            return;
        }

        presentation?.SetPlanarVelocity(Vector3.zero);
    }

    private void DecideTarget()
    {
        targetHero = FindGankOpportunity(enemyHeroAggroRadius);
        if (targetHero != null)
        {
            targetBoss = null;
            targetMonster = null;
            return;
        }

        if (ShouldContestObjective())
        {
            AOGNeutralBossAI boss = FindBestObjective(objectiveJoinRadius);
            if (boss != null)
            {
                targetBoss = boss;
                targetMonster = null;
                return;
            }
        }

        targetBoss = null;
        if (targetMonster == null || targetMonster.IsDead || !targetMonster.gameObject.activeInHierarchy)
            targetMonster = FindBestNeutralMonster(campSearchRadius);
    }

    private void FightMonster(AOGNeutralMonsterRuntime monster)
    {
        float range = Mathf.Max(stats.attackRange, 2.4f);
        float distance = FlatDistance(transform.position, monster.transform.position);
        if (distance > range)
        {
            MoveToward(monster.transform.position, stats.moveSpeed);
            return;
        }

        Face(monster.transform.position);
        presentation?.SetPlanarVelocity(Vector3.zero);
        if (Time.time >= nextAttack && attackRoutine == null)
        {
            nextAttack = Time.time + stats.attackCooldown;
            attackRoutine = StartCoroutine(AttackMonster(monster));
        }
    }

    private void FightHero(AOGCharacterStats hero)
    {
        float distance = FlatDistance(transform.position, hero.transform.position);
        if (distance > stats.attackRange)
        {
            MoveToward(hero.transform.position, stats.moveSpeed * 1.06f);
            return;
        }

        Face(hero.transform.position);
        presentation?.SetPlanarVelocity(Vector3.zero);
        if (Time.time >= nextAttack && attackRoutine == null)
        {
            nextAttack = Time.time + stats.attackCooldown;
            attackRoutine = StartCoroutine(AttackHero(hero));
        }
    }

    private void FightBoss(AOGNeutralBossAI boss)
    {
        float range = Mathf.Max(stats.attackRange,2.8f);
        float distance = FlatDistance(transform.position,boss.transform.position);
        if (distance > range)
        {
            MoveToward(boss.transform.position,stats.moveSpeed*1.02f);
            return;
        }

        Face(boss.transform.position);
        presentation?.SetPlanarVelocity(Vector3.zero);
        if (Time.time >= nextAttack && attackRoutine == null)
        {
            nextAttack = Time.time + stats.attackCooldown;
            attackRoutine = StartCoroutine(AttackBoss(boss));
        }
    }

    private IEnumerator AttackMonster(AOGNeutralMonsterRuntime monster)
    {
        presentation?.PlayBasicAttack();
        float windup = presentation != null ? presentation.BasicAttackWindup : 0.24f;
        yield return new WaitForSeconds(windup);
        if (monster != null && !monster.IsDead && FlatDistance(transform.position, monster.transform.position) <= Mathf.Max(stats.attackRange,2.4f)+0.8f)
        {
            monster.TakeDamage(stats.attackDamage, gameObject);
            presentation?.SpawnImpactVfx(monster.transform.position + Vector3.up * 1.0f);
        }
        attackRoutine = null;
    }

    private IEnumerator AttackHero(AOGCharacterStats hero)
    {
        presentation?.PlayBasicAttack();
        float windup = presentation != null ? presentation.BasicAttackWindup : 0.24f;
        yield return new WaitForSeconds(windup);
        if (hero != null && !hero.IsDead && hero.team != stats.team && FlatDistance(transform.position, hero.transform.position) <= stats.attackRange + 0.8f)
        {
            hero.TakeDamage(stats.attackDamage,gameObject);
            AOGCombatEvents.RaiseBasicAttackHit(new AOGCombatHitEvent
            {
                source=gameObject,
                target=hero.gameObject,
                damage=stats.attackDamage,
                basicAttack=true,
                abilityId="jungle_gank_basic",
                targetKind=AOGCombatTargetKind.Champion
            });
            presentation?.SpawnImpactVfx(hero.transform.position + Vector3.up * 1.0f);
        }
        attackRoutine = null;
    }

    private IEnumerator AttackBoss(AOGNeutralBossAI boss)
    {
        presentation?.PlayBasicAttack();
        float windup = presentation != null ? presentation.BasicAttackWindup : 0.24f;
        yield return new WaitForSeconds(windup);
        if (boss != null && !boss.IsDead && FlatDistance(transform.position,boss.transform.position) <= Mathf.Max(stats.attackRange,2.8f)+0.9f)
        {
            boss.TakeDamage(stats.attackDamage,gameObject);
            AOGCombatEvents.RaiseBasicAttackHit(new AOGCombatHitEvent
            {
                source=gameObject,
                target=boss.gameObject,
                damage=stats.attackDamage,
                basicAttack=true,
                abilityId="jungle_objective_basic",
                targetKind=AOGCombatTargetKind.Boss
            });
            presentation?.SpawnImpactVfx(boss.transform.position+Vector3.up*1.2f);
        }
        attackRoutine=null;
    }

    private AOGNeutralMonsterRuntime FindBestNeutralMonster(float radius)
    {
        AOGNeutralMonsterRuntime best = null;
        float bestScore = float.MaxValue;
        foreach (AOGNeutralMonsterRuntime monster in FindObjectsByType<AOGNeutralMonsterRuntime>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (monster == null || monster.IsDead)
                continue;

            float distance = FlatDistance(transform.position, monster.transform.position);
            if (distance > radius)
                continue;

            float priorityBonus = monster.monsterType == AOGNeutralMonsterType.AetherSentinel ? -24f :
                                  monster.monsterType == AOGNeutralMonsterType.InfernalBrute ? -28f : 0f;
            float score = distance + priorityBonus;
            if (score < bestScore)
            {
                best = monster;
                bestScore = score;
            }
        }
        return best;
    }

    private AOGCharacterStats FindGankOpportunity(float radius)
    {
        AOGCharacterStats best = null;
        float bestScore = float.MaxValue;
        foreach (AOGCharacterStats hero in FindObjectsByType<AOGCharacterStats>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (hero == null || hero == stats || hero.IsDead || hero.team == stats.team)
                continue;

            float distance = FlatDistance(transform.position, hero.transform.position);
            if (distance > radius)
                continue;

            float hpRatio = hero.hp / Mathf.Max(1f,hero.maxHp);
            float score = hpRatio * 7f + distance * 0.25f;
            if (score < bestScore)
            {
                best = hero;
                bestScore = score;
            }
        }
        return best;
    }

    private bool ShouldContestObjective()
    {
        if (AOGMatchDirector.Instance == null)
            return false;

        float hpRatio = stats.hp / Mathf.Max(1f,stats.maxHp);
        if (hpRatio < 0.62f)
            return false;

        return AOGMatchDirector.Instance.MatchTime >= 210f;
    }

    private AOGNeutralBossAI FindBestObjective(float radius)
    {
        AOGNeutralBossAI best = null;
        float bestScore = float.MaxValue;
        foreach (AOGNeutralBossAI boss in FindObjectsByType<AOGNeutralBossAI>(FindObjectsInactive.Exclude,FindObjectsSortMode.None))
        {
            if (boss == null || boss.IsDead)
                continue;

            float distance = FlatDistance(transform.position,boss.transform.position);
            if (distance > radius)
                continue;

            float hpRatio = boss.hp / Mathf.Max(1f,boss.maxHp);
            bool engaged = hpRatio < 0.98f;
            float score = distance + (engaged ? -18f : 0f) + hpRatio * 6f;
            if (score < bestScore)
            {
                best=boss;
                bestScore=score;
            }
        }
        return best;
    }

    private void ClearTargets()
    {
        targetHero=null;
        targetMonster=null;
        targetBoss=null;
    }

    private void MoveToward(Vector3 point, float speed)
    {
        Vector3 delta = point - transform.position;
        delta.y = 0f;
        if (delta.sqrMagnitude < 0.01f)
            return;
        Vector3 direction = delta.normalized;
        transform.position += direction * speed * Time.deltaTime;
        Face(point);
        presentation?.SetPlanarVelocity(direction * speed);
    }

    private void Face(Vector3 point)
    {
        Vector3 delta = point - transform.position;
        delta.y = 0f;
        if (delta.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(delta.normalized), 10f * Time.deltaTime);
    }

    private static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a,b);
    }
}

public class AOGJungleChampionAIBootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindFirstObjectByType<AOGJungleChampionAIBootstrap>() != null)
            return;
        GameObject host = new GameObject("AOG_Jungle_Champion_AI_Bootstrap");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGJungleChampionAIBootstrap>();
    }

    private float nextScan;
    private void Update()
    {
        if (Time.unscaledTime < nextScan)
            return;
        nextScan = Time.unscaledTime + 0.75f;

        foreach (AOGTeamMemberIdentity member in FindObjectsByType<AOGTeamMemberIdentity>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (member == null || member.isHumanPlayer || member.role != AOGRole.Jungle)
                continue;

            AOGBotChampionAI laneAi = member.GetComponent<AOGBotChampionAI>();
            if (laneAi != null)
                laneAi.enabled = false;

            if (member.GetComponent<AOGJungleChampionAIRuntime>() == null)
                member.gameObject.AddComponent<AOGJungleChampionAIRuntime>();
        }
    }
}
