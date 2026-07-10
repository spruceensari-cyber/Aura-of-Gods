using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

[DefaultExecutionOrder(-36)]
public class AOGEnemyTargetSelectionRuntime : MonoBehaviour
{
    private AOGCharacterStats stats;
    private ChampionPresentationController presentation;
    private AOGCharacterStats selectedHero;
    private Minion selectedMinion;
    private TowerHealth selectedTower;
    private GameObject targetRing;
    private float nextAttack;
    private Coroutine attackRoutine;

    private void Awake()
    {
        stats = GetComponent<AOGCharacterStats>();
        presentation = GetComponent<ChampionPresentationController>();
    }

    private void OnDisable()
    {
        ClearTarget();
    }

    private void Update()
    {
        AOGActiveChampion active = GetComponent<AOGActiveChampion>();
        if (active != null && !active.IsActiveChampion)
            return;
        if (stats == null || stats.IsDead)
            return;

        if ((AOGInputBridge.LeftPressedThisFrame() || AOGInputBridge.RightPressedThisFrame()) &&
            (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject()))
        {
            TrySelectTargetUnderPointer();
        }

        RefreshTargetRing();
        ProcessSelectedTarget();
    }

    private void TrySelectTargetUnderPointer()
    {
        Camera cam = Camera.main;
        if (cam == null)
            return;

        Ray ray = cam.ScreenPointToRay(AOGInputBridge.PointerPosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, 1200f, ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit hit in hits)
        {
            AOGCharacterStats hero = hit.collider.GetComponentInParent<AOGCharacterStats>();
            if (hero != null && hero != stats && !hero.IsDead && hero.team != stats.team)
            {
                SelectHero(hero);
                return;
            }

            Minion minion = hit.collider.GetComponentInParent<Minion>();
            if (minion != null && minion.hp > 0f && minion.team != stats.team)
            {
                SelectMinion(minion);
                return;
            }

            TowerHealth tower = hit.collider.GetComponentInParent<TowerHealth>();
            if (tower != null && tower.hp > 0f && tower.towerTeam != stats.team)
            {
                SelectTower(tower);
                return;
            }
        }
    }

    private void SelectHero(AOGCharacterStats hero)
    {
        ClearTarget();
        selectedHero = hero;
        BuildRing(hero.transform, new Color(1f, 0.18f, 0.28f, 1f), 1.35f);
    }

    private void SelectMinion(Minion minion)
    {
        ClearTarget();
        selectedMinion = minion;
        BuildRing(minion.transform, new Color(1f, 0.34f, 0.18f, 1f), 0.78f);
    }

    private void SelectTower(TowerHealth tower)
    {
        ClearTarget();
        selectedTower = tower;
        BuildRing(tower.transform, new Color(1f, 0.14f, 0.12f, 1f), 2.3f);
    }

    private void BuildRing(Transform target, Color color, float radius)
    {
        if (target == null)
            return;

        targetRing = AOGAbilityVisuals.CreateRing("Selected_Target_Ring", target.position + Vector3.up * 0.08f, radius, color, 0.10f);
    }

    private void RefreshTargetRing()
    {
        if (targetRing == null)
            return;

        Transform target = CurrentTargetTransform();
        if (target == null)
        {
            Destroy(targetRing);
            targetRing = null;
            return;
        }

        targetRing.transform.position = target.position + Vector3.up * 0.08f;
        float pulse = 1f + Mathf.Sin(Time.unscaledTime * 7f) * 0.08f;
        targetRing.transform.localScale = Vector3.one * pulse;
    }

    private void ProcessSelectedTarget()
    {
        Transform target = CurrentTargetTransform();
        if (target == null)
            return;

        if (!TargetIsAlive())
        {
            ClearTarget();
            return;
        }

        float range = selectedTower != null ? stats.attackRange + 2.5f : stats.attackRange;
        float distance = FlatDistance(transform.position, target.position);

        if (distance > range)
        {
            MoveToward(target.position);
            return;
        }

        Face(target.position);
        presentation?.SetPlanarVelocity(Vector3.zero);
        if (Time.time >= nextAttack && attackRoutine == null)
        {
            nextAttack = Time.time + stats.attackCooldown;
            attackRoutine = StartCoroutine(AttackSelectedTarget(target, range));
        }
    }

    private IEnumerator AttackSelectedTarget(Transform lockedTarget, float range)
    {
        presentation?.PlayBasicAttack();
        float windup = presentation != null ? presentation.BasicAttackWindup : 0.22f;
        yield return new WaitForSeconds(windup);

        if (lockedTarget != null && FlatDistance(transform.position, lockedTarget.position) <= range + 0.85f)
        {
            if (selectedHero != null && !selectedHero.IsDead)
                selectedHero.TakeDamage(stats.attackDamage);
            else if (selectedMinion != null && selectedMinion.hp > 0f)
                selectedMinion.TakeDamage(stats.attackDamage, gameObject);
            else if (selectedTower != null && selectedTower.hp > 0f)
                selectedTower.TakeDamage(stats.attackDamage);

            presentation?.SpawnImpactVfx(lockedTarget.position + Vector3.up * 0.9f);
        }

        attackRoutine = null;
    }

    private Transform CurrentTargetTransform()
    {
        if (selectedHero != null) return selectedHero.transform;
        if (selectedMinion != null) return selectedMinion.transform;
        if (selectedTower != null) return selectedTower.transform;
        return null;
    }

    private bool TargetIsAlive()
    {
        if (selectedHero != null) return !selectedHero.IsDead && selectedHero.gameObject.activeInHierarchy;
        if (selectedMinion != null) return selectedMinion.hp > 0f && selectedMinion.gameObject.activeInHierarchy;
        if (selectedTower != null) return selectedTower.hp > 0f && selectedTower.gameObject.activeInHierarchy;
        return false;
    }

    private void MoveToward(Vector3 point)
    {
        Vector3 direction = point - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.01f)
            return;

        direction.Normalize();
        transform.position += direction * stats.moveSpeed * Time.deltaTime;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), 12f * Time.deltaTime);
        presentation?.SetPlanarVelocity(direction * stats.moveSpeed);
    }

    private void Face(Vector3 point)
    {
        Vector3 direction = point - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction.normalized), 12f * Time.deltaTime);
    }

    private void ClearTarget()
    {
        selectedHero = null;
        selectedMinion = null;
        selectedTower = null;
        if (targetRing != null)
        {
            Destroy(targetRing);
            targetRing = null;
        }
        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }
    }

    private static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}

public class AOGEnemyTargetSelectionBootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        GameObject host = new GameObject("AOG_Enemy_Target_Selection_Bootstrap");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGEnemyTargetSelectionBootstrap>();
    }

    private void Update()
    {
        foreach (AOGActiveChampion champion in FindObjectsByType<AOGActiveChampion>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (champion != null && champion.GetComponent<AOGEnemyTargetSelectionRuntime>() == null)
                champion.gameObject.AddComponent<AOGEnemyTargetSelectionRuntime>();
        }
    }
}
