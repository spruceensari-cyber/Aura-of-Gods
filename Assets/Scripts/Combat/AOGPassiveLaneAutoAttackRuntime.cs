using System.Collections;
using UnityEngine;

[DefaultExecutionOrder(-40)]
public class AOGPassiveLaneAutoAttackRuntime : MonoBehaviour
{
    public float acquireRadius = 5.8f;
    public float movementThreshold = 0.08f;

    private AOGCharacterStats stats;
    private ChampionPresentationController presentation;
    private Vector3 lastPosition;
    private float nextAttack;
    private Coroutine attackRoutine;

    private void Awake()
    {
        stats = GetComponent<AOGCharacterStats>();
        presentation = GetComponent<ChampionPresentationController>();
        lastPosition = transform.position;
    }

    private void Update()
    {
        if (stats == null || stats.IsDead)
            return;

        AOGActiveChampion active = GetComponent<AOGActiveChampion>();
        if (active != null && !active.IsActiveChampion)
            return;

        float moved = Vector3.Distance(new Vector3(transform.position.x, 0f, transform.position.z), new Vector3(lastPosition.x, 0f, lastPosition.z));
        lastPosition = transform.position;

        if (moved > movementThreshold || attackRoutine != null || Time.time < nextAttack)
            return;

        Minion target = FindNearestEnemyMinion();
        if (target == null)
            return;

        nextAttack = Time.time + stats.attackCooldown;
        attackRoutine = StartCoroutine(AttackMinion(target));
    }

    private IEnumerator AttackMinion(Minion target)
    {
        if (target == null)
        {
            attackRoutine = null;
            yield break;
        }

        Vector3 direction = target.transform.position - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.LookRotation(direction.normalized);

        presentation?.PlayBasicAttack();
        yield return new WaitForSeconds(presentation != null ? presentation.BasicAttackWindup : 0.22f);

        if (target != null && target.hp > 0f && FlatDistance(transform.position, target.transform.position) <= stats.attackRange + 0.9f)
        {
            target.TakeDamage(stats.attackDamage, gameObject);
            presentation?.SpawnImpactVfx(target.transform.position + Vector3.up * 0.8f);
        }

        attackRoutine = null;
    }

    private Minion FindNearestEnemyMinion()
    {
        Minion best = null;
        float bestDistance = Mathf.Max(acquireRadius, stats.attackRange + 0.7f);
        foreach (Minion minion in Minion.Active)
        {
            if (minion == null || minion.hp <= 0f || minion.team == stats.team)
                continue;

            float distance = FlatDistance(transform.position, minion.transform.position);
            if (distance < bestDistance)
            {
                best = minion;
                bestDistance = distance;
            }
        }
        return best;
    }

    private static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}

public class AOGPassiveLaneAutoAttackBootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        GameObject host = new GameObject("AOG_Passive_Lane_AutoAttack_Bootstrap");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGPassiveLaneAutoAttackBootstrap>();
    }

    private void Update()
    {
        foreach (AOGActiveChampion hero in FindObjectsByType<AOGActiveChampion>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (hero != null && hero.GetComponent<AOGPassiveLaneAutoAttackRuntime>() == null)
                hero.gameObject.AddComponent<AOGPassiveLaneAutoAttackRuntime>();
        }
    }
}
