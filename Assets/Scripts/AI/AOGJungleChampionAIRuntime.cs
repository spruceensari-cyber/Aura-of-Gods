using System.Collections;
using UnityEngine;

[DefaultExecutionOrder(60)]
public class AOGJungleChampionAIRuntime : MonoBehaviour
{
    public float campSearchRadius = 80f;
    public float enemyHeroAggroRadius = 7.5f;
    public float retreatHpRatio = 0.24f;

    private AOGCharacterStats stats;
    private ChampionPresentationController presentation;
    private AOGTeamMemberIdentity identity;
    private AOGNeutralMonsterRuntime targetMonster;
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
            targetMonster = null;
            targetHero = null;
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

        if (targetMonster != null && !targetMonster.IsDead && targetMonster.gameObject.activeInHierarchy)
        {
            FightMonster(targetMonster);
            return;
        }

        presentation?.SetPlanarVelocity(Vector3.zero);
    }

    private void DecideTarget()
    {
        targetHero = FindNearestEnemyHero(enemyHeroAggroRadius);
        if (targetHero != null)
            return;

        if (targetMonster == null || targetMonster.IsDead || !targetMonster.gameObject.activeInHierarchy)
            targetMonster = FindNearestNeutralMonster(campSearchRadius);
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
            MoveToward(hero.transform.position, stats.moveSpeed);
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
            hero.TakeDamage(stats.attackDamage);
            presentation?.SpawnImpactVfx(hero.transform.position + Vector3.up * 1.0f);
        }
        attackRoutine = null;
    }

    private AOGNeutralMonsterRuntime FindNearestNeutralMonster(float radius)
    {
        AOGNeutralMonsterRuntime best = null;
        float bestDistance = radius;
        foreach (AOGNeutralMonsterRuntime monster in FindObjectsByType<AOGNeutralMonsterRuntime>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (monster == null || monster.IsDead)
                continue;
            float distance = FlatDistance(transform.position, monster.transform.position);
            if (distance < bestDistance)
            {
                best = monster;
                bestDistance = distance;
            }
        }
        return best;
    }

    private AOGCharacterStats FindNearestEnemyHero(float radius)
    {
        AOGCharacterStats best = null;
        float bestDistance = radius;
        foreach (AOGCharacterStats hero in FindObjectsByType<AOGCharacterStats>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (hero == null || hero == stats || hero.IsDead || hero.team == stats.team)
                continue;
            float distance = FlatDistance(transform.position, hero.transform.position);
            if (distance < bestDistance)
            {
                best = hero;
                bestDistance = distance;
            }
        }
        return best;
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
        GameObject host = new GameObject("AOG_Jungle_Champion_AI_Bootstrap");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGJungleChampionAIBootstrap>();
    }

    private void Update()
    {
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
