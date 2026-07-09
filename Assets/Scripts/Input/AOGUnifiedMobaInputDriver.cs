using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Unified MOBA command driver for the active champion. Supports movement, target selection,
/// attack chasing and attacks against minions, towers, nexuses and neutral bosses.
/// </summary>
[DefaultExecutionOrder(-50)]
public class AOGUnifiedMobaInputDriver : MonoBehaviour
{
    public Camera gameplayCamera;
    public LayerMask commandMask = ~0;
    public float maxRayDistance = 1200f;
    public float stopDistance = 0.32f;
    public float attackRangeTolerance = 0.9f;
    public bool leftClickAlsoMoves;

    private AOGCharacterStats stats;
    private ChampionPresentationController presentation;
    private Vector3 moveTarget;
    private bool hasMoveTarget;
    private Minion targetMinion;
    private TowerHealth targetTower;
    private AOGNexusCore targetNexus;
    private AOGNeutralBossAI targetBoss;
    private float nextAttackTime;
    private Coroutine attackRoutine;
    private Vector3 lastPosition;
    private readonly List<IChampionBasicAttackModifier> attackModifiers = new List<IChampionBasicAttackModifier>();

    private void Awake()
    {
        stats = GetComponent<AOGCharacterStats>();
        presentation = GetComponent<ChampionPresentationController>();
        gameplayCamera = Camera.main;
        lastPosition = transform.position;
        moveTarget = transform.position;
        RefreshAttackModifiers();
        DisableConflictingControllers();
    }

    private void OnEnable()
    {
        if (stats == null) stats = GetComponent<AOGCharacterStats>();
        if (presentation == null) presentation = GetComponent<ChampionPresentationController>();
        RefreshAttackModifiers();
    }

    private void RefreshAttackModifiers()
    {
        attackModifiers.Clear();
        foreach (MonoBehaviour behaviour in GetComponents<MonoBehaviour>())
        {
            if (behaviour is IChampionBasicAttackModifier modifier)
                attackModifiers.Add(modifier);
        }
    }

    private void DisableConflictingControllers()
    {
        AOGPlayerMOBAController oldMoba = GetComponent<AOGPlayerMOBAController>();
        if (oldMoba != null) oldMoba.enabled = false;

        PlayerAutoAttack oldAuto = GetComponent<PlayerAutoAttack>();
        if (oldAuto != null) oldAuto.enabled = false;

        PlayerAttack oldAttack = GetComponent<PlayerAttack>();
        if (oldAttack != null) oldAttack.enabled = false;

        ChampionController oldChampionController = GetComponent<ChampionController>();
        if (oldChampionController != null) oldChampionController.enabled = false;
    }

    private void Update()
    {
        if (stats == null || stats.IsDead)
            return;

        AOGActiveChampion marker = GetComponent<AOGActiveChampion>();
        if (marker != null && !marker.IsActiveChampion)
            return;

        if (gameplayCamera == null || !gameplayCamera.isActiveAndEnabled)
            gameplayCamera = Camera.main;

        HandleCommandInput();
        HandleAbilityInput();
        HandleMovement();
        HandleAttackLogic();
        UpdateAnimationVelocity();
    }

    private void HandleCommandInput()
    {
        if (AOGInputBridge.KeyPressedThisFrame(KeyCode.S))
        {
            StopAllActions();
            return;
        }

        bool commandPressed = AOGInputBridge.RightPressedThisFrame() ||
                              (leftClickAlsoMoves && AOGInputBridge.LeftPressedThisFrame());

        if (!commandPressed || gameplayCamera == null)
            return;

        Vector2 pointer = AOGInputBridge.PointerPosition;
        Ray ray = gameplayCamera.ScreenPointToRay(new Vector3(pointer.x, pointer.y, 0f));
        RaycastHit[] hits = Physics.RaycastAll(ray, maxRayDistance, commandMask, QueryTriggerInteraction.Ignore);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        if (TrySelectEnemy(hits))
            return;

        if (TryFindWalkPoint(hits, out Vector3 point))
        {
            SetMoveTarget(point);
            return;
        }

        Plane fallbackGround = new Plane(Vector3.up, new Vector3(0f, transform.position.y, 0f));
        if (fallbackGround.Raycast(ray, out float enter))
            SetMoveTarget(ray.GetPoint(enter));
    }

    private void HandleAbilityInput()
    {
        if (AOGInputBridge.KeyPressedThisFrame(KeyCode.Q)) SendMessage("CastQ", SendMessageOptions.DontRequireReceiver);
        if (AOGInputBridge.KeyPressedThisFrame(KeyCode.W)) SendMessage("CastW", SendMessageOptions.DontRequireReceiver);
        if (AOGInputBridge.KeyPressedThisFrame(KeyCode.E)) SendMessage("CastE", SendMessageOptions.DontRequireReceiver);
        if (AOGInputBridge.KeyPressedThisFrame(KeyCode.R)) SendMessage("CastR", SendMessageOptions.DontRequireReceiver);
    }

    private bool TrySelectEnemy(RaycastHit[] hits)
    {
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null || hit.collider.transform == transform || hit.collider.transform.IsChildOf(transform))
                continue;

            Minion minion = hit.collider.GetComponentInParent<Minion>();
            if (minion != null && minion.hp > 0f && minion.team != stats.team)
            {
                ClearTargets();
                targetMinion = minion;
                hasMoveTarget = false;
                return true;
            }

            TowerHealth tower = hit.collider.GetComponentInParent<TowerHealth>();
            if (tower != null && tower.hp > 0f && tower.towerTeam != stats.team)
            {
                ClearTargets();
                targetTower = tower;
                hasMoveTarget = false;
                return true;
            }

            AOGNexusCore nexus = hit.collider.GetComponentInParent<AOGNexusCore>();
            if (nexus != null && !nexus.IsDestroyed && nexus.team != stats.team)
            {
                ClearTargets();
                targetNexus = nexus;
                hasMoveTarget = false;
                return true;
            }

            AOGNeutralBossAI boss = hit.collider.GetComponentInParent<AOGNeutralBossAI>();
            if (boss != null && !boss.IsDead)
            {
                ClearTargets();
                targetBoss = boss;
                hasMoveTarget = false;
                return true;
            }
        }

        return false;
    }

    private bool TryFindWalkPoint(RaycastHit[] hits, out Vector3 point)
    {
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null)
                continue;

            Transform t = hit.collider.transform;
            if (t == transform || t.IsChildOf(transform))
                continue;

            string n = t.gameObject.name.ToLowerInvariant();
            if (n.Contains("hp_bar") || n.Contains("worldbar") || n.Contains("click_indicator") || n.Contains("readability") || n.Contains("telegraph"))
                continue;

            if (hit.collider.GetComponentInParent<Minion>() != null ||
                hit.collider.GetComponentInParent<TowerHealth>() != null ||
                hit.collider.GetComponentInParent<AOGNexusCore>() != null ||
                hit.collider.GetComponentInParent<AOGNeutralBossAI>() != null)
                continue;

            point = hit.point;
            point.y = transform.position.y;
            return true;
        }

        point = default;
        return false;
    }

    private void SetMoveTarget(Vector3 point)
    {
        moveTarget = point;
        moveTarget.y = transform.position.y;
        hasMoveTarget = true;
        ClearTargets();

        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }

        AOGActiveChampion marker = GetComponent<AOGActiveChampion>();
        Color color = marker != null ? marker.accentColor : new Color(0.20f, 0.72f, 1f, 0.95f);
        AOGMoveClickIndicator.Show(moveTarget, color);
    }

    private void HandleMovement()
    {
        if (!hasMoveTarget || attackRoutine != null)
            return;

        Vector3 delta = moveTarget - transform.position;
        delta.y = 0f;
        float distance = delta.magnitude;

        if (distance <= stopDistance)
        {
            hasMoveTarget = false;
            return;
        }

        Vector3 direction = delta / Mathf.Max(distance, 0.0001f);
        transform.position += direction * stats.moveSpeed * Time.deltaTime;
        Face(direction);
    }

    private void HandleAttackLogic()
    {
        if (attackRoutine != null)
            return;

        if (targetMinion != null)
        {
            if (targetMinion.hp <= 0f || !targetMinion.gameObject.activeInHierarchy)
            {
                targetMinion = null;
                return;
            }

            AttackOrChase(targetMinion.transform, stats.attackRange, () => StartCoroutine(HitMinionAfterWindup(targetMinion, CurrentWindup())));
            return;
        }

        if (targetTower != null)
        {
            if (targetTower.hp <= 0f || !targetTower.gameObject.activeInHierarchy)
            {
                targetTower = null;
                return;
            }

            float range = stats.attackRange + 2.8f;
            TowerHealth locked = targetTower;
            AttackOrChase(locked.transform, range, () => StartCoroutine(HitTowerAfterWindup(locked, CurrentWindup(), range)));
            return;
        }

        if (targetNexus != null)
        {
            if (targetNexus.IsDestroyed || !targetNexus.gameObject.activeInHierarchy)
            {
                targetNexus = null;
                return;
            }

            float range = stats.attackRange + 2.6f;
            AOGNexusCore locked = targetNexus;
            AttackOrChase(locked.transform, range, () => StartCoroutine(HitNexusAfterWindup(locked, CurrentWindup(), range)));
            return;
        }

        if (targetBoss != null)
        {
            if (targetBoss.IsDead || !targetBoss.gameObject.activeInHierarchy)
            {
                targetBoss = null;
                return;
            }

            float range = stats.attackRange + 1.4f;
            AOGNeutralBossAI locked = targetBoss;
            AttackOrChase(locked.transform, range, () => StartCoroutine(HitBossAfterWindup(locked, CurrentWindup(), range)));
        }
    }

    private void AttackOrChase(Transform target, float allowedRange, Func<Coroutine> beginAttack)
    {
        if (target == null)
            return;

        float distance = FlatDistance(transform.position, target.position);
        if (distance > allowedRange)
        {
            MoveToward(target.position);
            return;
        }

        hasMoveTarget = false;
        Face(target.position - transform.position);
        if (Time.time < nextAttackTime)
            return;

        nextAttackTime = Time.time + stats.attackCooldown;
        presentation?.PlayBasicAttack();
        attackRoutine = beginAttack();
    }

    private float CurrentWindup()
    {
        return presentation != null ? presentation.BasicAttackWindup : 0.2f;
    }

    private IEnumerator HitMinionAfterWindup(Minion target, float windup)
    {
        if (windup > 0f) yield return new WaitForSeconds(windup);

        if (target != null && target.hp > 0f && FlatDistance(transform.position, target.transform.position) <= stats.attackRange + attackRangeTolerance)
        {
            target.TakeDamage(stats.attackDamage, gameObject);
            foreach (IChampionBasicAttackModifier modifier in attackModifiers)
                modifier?.OnBasicAttackHit(target);
            PlayAttackImpact(target.transform.position + Vector3.up * 0.8f);
        }

        attackRoutine = null;
    }

    private IEnumerator HitTowerAfterWindup(TowerHealth target, float windup, float allowedRange)
    {
        if (windup > 0f) yield return new WaitForSeconds(windup);

        if (target != null && target.hp > 0f && FlatDistance(transform.position, target.transform.position) <= allowedRange + attackRangeTolerance)
        {
            target.TakeDamage(stats.attackDamage);
            PlayAttackImpact(target.transform.position + Vector3.up * 1.5f);
        }

        attackRoutine = null;
    }

    private IEnumerator HitNexusAfterWindup(AOGNexusCore target, float windup, float allowedRange)
    {
        if (windup > 0f) yield return new WaitForSeconds(windup);

        if (target != null && !target.IsDestroyed && FlatDistance(transform.position, target.transform.position) <= allowedRange + attackRangeTolerance)
        {
            target.TakeDamage(stats.attackDamage);
            PlayAttackImpact(target.transform.position + Vector3.up * 2.2f);
        }

        attackRoutine = null;
    }

    private IEnumerator HitBossAfterWindup(AOGNeutralBossAI target, float windup, float allowedRange)
    {
        if (windup > 0f) yield return new WaitForSeconds(windup);

        if (target != null && !target.IsDead && FlatDistance(transform.position, target.transform.position) <= allowedRange + attackRangeTolerance)
        {
            target.TakeDamage(stats.attackDamage, gameObject);
            PlayAttackImpact(target.transform.position + Vector3.up * 1.5f);
        }

        attackRoutine = null;
    }

    private void PlayAttackImpact(Vector3 position)
    {
        presentation?.audioController?.PlayAttackImpact();
        presentation?.SpawnImpactVfx(position);
    }

    private void MoveToward(Vector3 point)
    {
        Vector3 delta = point - transform.position;
        delta.y = 0f;
        if (delta.sqrMagnitude < 0.01f)
            return;

        Vector3 direction = delta.normalized;
        transform.position += direction * stats.moveSpeed * Time.deltaTime;
        Face(direction);
    }

    private void Face(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 15f * Time.deltaTime);
    }

    private void StopAllActions()
    {
        hasMoveTarget = false;
        ClearTargets();

        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }

        presentation?.SetPlanarVelocity(Vector3.zero);
    }

    private void ClearTargets()
    {
        targetMinion = null;
        targetTower = null;
        targetNexus = null;
        targetBoss = null;
    }

    private void UpdateAnimationVelocity()
    {
        float dt = Mathf.Max(Time.deltaTime, 0.0001f);
        Vector3 velocity = (transform.position - lastPosition) / dt;
        velocity.y = 0f;
        lastPosition = transform.position;
        presentation?.SetPlanarVelocity(velocity);
    }

    private static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}

public static class AOGUnifiedMobaInputBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        AttachToCandidates();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        AttachToCandidates();
    }

    private static void AttachToCandidates()
    {
        AOGPlayerMOBAController[] players = UnityEngine.Object.FindObjectsByType<AOGPlayerMOBAController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (AOGPlayerMOBAController player in players)
        {
            if (player == null)
                continue;

            AOGActiveChampion marker = player.GetComponent<AOGActiveChampion>();
            bool legacyLyra = player.gameObject.name.ToLowerInvariant().Contains("lyra");
            if (marker == null && !legacyLyra)
                continue;

            AOGUnifiedMobaInputDriver driver = player.GetComponent<AOGUnifiedMobaInputDriver>();
            if (driver == null)
                driver = player.gameObject.AddComponent<AOGUnifiedMobaInputDriver>();

            if (marker != null)
                driver.enabled = marker.IsActiveChampion;
        }
    }
}
