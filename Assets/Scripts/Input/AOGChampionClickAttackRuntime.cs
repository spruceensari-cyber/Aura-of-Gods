using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

[DefaultExecutionOrder(-38)]
public class AOGChampionClickAttackRuntime : MonoBehaviour
{
    private AOGCharacterStats stats;
    private ChampionPresentationController presentation;
    private AOGCharacterStats targetHero;
    private float nextAttack;
    private Coroutine attackRoutine;

    private void Awake()
    {
        stats = GetComponent<AOGCharacterStats>();
        presentation = GetComponent<ChampionPresentationController>();
    }

    private void Update()
    {
        AOGActiveChampion active = GetComponent<AOGActiveChampion>();
        if (active != null && !active.IsActiveChampion) return;
        if (stats == null || stats.IsDead) return;

        if (AOGInputBridge.LeftPressedThisFrame() || AOGInputBridge.RightPressedThisFrame())
        {
            if (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject())
                TrySelectHeroUnderMouse();
        }

        if (targetHero == null || targetHero.IsDead || !targetHero.gameObject.activeInHierarchy)
        {
            targetHero = null;
            return;
        }

        float distance = FlatDistance(transform.position, targetHero.transform.position);
        if (distance > stats.attackRange)
        {
            Vector3 dir = targetHero.transform.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.01f)
            {
                transform.position += dir.normalized * stats.moveSpeed * Time.deltaTime;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir.normalized), 12f * Time.deltaTime);
                presentation?.SetPlanarVelocity(dir.normalized * stats.moveSpeed);
            }
        }
        else
        {
            Face(targetHero.transform.position);
            presentation?.SetPlanarVelocity(Vector3.zero);
            if (Time.time >= nextAttack && attackRoutine == null)
            {
                nextAttack = Time.time + stats.attackCooldown;
                attackRoutine = StartCoroutine(AttackHero(targetHero));
            }
        }
    }

    private void TrySelectHeroUnderMouse()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        Ray ray = cam.ScreenPointToRay(AOGInputBridge.PointerPosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, 1000f, ~0, QueryTriggerInteraction.Ignore);
        float bestDistance = float.MaxValue;
        AOGCharacterStats best = null;
        foreach (RaycastHit hit in hits)
        {
            AOGCharacterStats candidate = hit.collider.GetComponentInParent<AOGCharacterStats>();
            if (candidate == null || candidate == stats || candidate.IsDead || candidate.team == stats.team)
                continue;

            if (hit.distance < bestDistance)
            {
                bestDistance = hit.distance;
                best = candidate;
            }
        }

        if (best != null)
            targetHero = best;
    }

    private IEnumerator AttackHero(AOGCharacterStats locked)
    {
        presentation?.PlayBasicAttack();
        float windup = presentation != null ? presentation.BasicAttackWindup : 0.22f;
        yield return new WaitForSeconds(windup);

        if (locked != null && !locked.IsDead && FlatDistance(transform.position, locked.transform.position) <= stats.attackRange + 0.8f)
        {
            locked.TakeDamage(stats.attackDamage);
            presentation?.SpawnImpactVfx(locked.transform.position + Vector3.up * 1.1f);
        }

        attackRoutine = null;
    }

    private void Face(Vector3 point)
    {
        Vector3 d = point - transform.position;
        d.y = 0f;
        if (d.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d.normalized), 12f * Time.deltaTime);
    }

    private static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}

public class AOGChampionClickAttackBootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        GameObject host = new GameObject("AOG_Champion_Click_Attack_Bootstrap");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGChampionClickAttackBootstrap>();
    }

    private void Update()
    {
        foreach (AOGActiveChampion hero in FindObjectsByType<AOGActiveChampion>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (hero != null && hero.GetComponent<AOGChampionClickAttackRuntime>() == null)
                hero.gameObject.AddComponent<AOGChampionClickAttackRuntime>();
        }
    }
}
