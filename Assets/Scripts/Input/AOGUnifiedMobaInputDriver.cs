using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Runtime command driver for Lyra. It works with Legacy Input, New Input System or Both
/// through AOGInputBridge and deliberately does not depend on EventSystem pointer blocking.
/// </summary>
[DefaultExecutionOrder(-50)]
public class AOGUnifiedMobaInputDriver : MonoBehaviour
{
    public Camera gameplayCamera;
    public LayerMask commandMask = ~0;
    public float maxRayDistance = 1200f;
    public float stopDistance = 0.32f;
    public float attackRangeTolerance = 0.9f;
    public bool leftClickAlsoMoves = true;

    private AOGCharacterStats stats;
    private ChampionPresentationController presentation;
    private Vector3 moveTarget;
    private bool hasMoveTarget;
    private Minion targetMinion;
    private TowerHealth targetTower;
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

        foreach (MonoBehaviour behaviour in GetComponents<MonoBehaviour>())
        {
            if (behaviour is IChampionBasicAttackModifier modifier)
                attackModifiers.Add(modifier);
        }

        DisableConflictingControllers();
    }

    private void DisableConflictingControllers()
    {
        AOGPlayerMOBAController oldMoba = GetComponent<AOGPlayerMOBAController>();
        if (oldMoba != null)
            oldMoba.enabled = false;

        PlayerAutoAttack oldAuto = GetComponent<PlayerAutoAttack>();
        if (oldAuto != null)
            oldAuto.enabled = false;

        PlayerAttack oldAttack = GetComponent<PlayerAttack>();
        if (oldAttack != null)
            oldAttack.enabled = false;

        ChampionController oldChampionController = GetComponent<ChampionController>();
        if (oldChampionController != null)
            oldChampionController.enabled = false;
    }

    private void Update()
    {
        if (stats == null || stats.IsDead)
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
            if (hit.collider == null || hit.collider.transform.IsChildOf(transform))
                continue;

            Minion minion = hit.collider.GetComponentInParent<Minion>();
            if (minion != null && minion.hp > 0f && minion.team != stats.team)
            {
                targetMinion = minion;
                targetTower = null;
                hasMoveTarget = false;
                return true;
            }

            TowerHealth tower = hit.collider.GetComponentInParent<TowerHealth>();
            if (tower != null && tower.hp > 0f && tower.towerTeam != stats.team)
            {
                targetTower = tower;
                targetMinion = null;
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
            if (n.Contains("hp_bar") || n.Contains("worldbar") || n.Contains("click_indicator") || n.Contains("readability"))
                continue;

            if (hit.collider.GetComponentInParent<Minion>() != null || hit.collider.GetComponentInParent<TowerHealth>() != null)
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
        targetMinion = null;
        targetTower = null;

        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }

        AOGMoveClickIndicator.Show(moveTarget, new Color(0.20f, 0.72f, 1f, 0.95f));
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

            float distance = FlatDistance(transform.position, targetMinion.transform.position);
            if (distance > stats.attackRange)
            {
                MoveToward(targetMinion.transform.position);
                return;
            }

            Face(targetMinion.transform.position - transform.position);
            if (Time.time >= nextAttackTime)
            {
                nextAttackTime = Time.time + stats.attackCooldown;
                Minion locked = targetMinion;
                presentation?.PlayBasicAttack();
                float windup = presentation != null ? presentation.BasicAttackWindup : 0.2f;
                attackRoutine = StartCoroutine(HitMinionAfterWindup(locked, windup));
            }
            return;
        }

        if (targetTower != null)
        {
            if (targetTower.hp <= 0f || !targetTower.gameObject.activeInHierarchy)
            {
                targetTower = null;
                return;
            }

            float allowed = stats.attackRange + 3f;
            float distance = FlatDistance(transform.position, targetTower.transform.position);
            if (distance > allowed)
            {
                MoveToward(targetTower.transform.position);
                return;
            }

            Face(targetTower.transform.position - transform.position);
            if (Time.time >= nextAttackTime)
            {
                nextAttackTime = Time.time + stats.attackCooldown;
                TowerHealth locked = targetTower;
                presentation?.PlayBasicAttack();
                float windup = presentation != null ? presentation.BasicAttackWindup : 0.2f;
                attackRoutine = StartCoroutine(HitTowerAfterWindup(locked, windup, allowed));
            }
        }
    }

    private IEnumerator HitMinionAfterWindup(Minion target, float windup)
    {
        if (windup > 0f)
            yield return new WaitForSeconds(windup);

        if (target != null && target.hp > 0f && FlatDistance(transform.position, target.transform.position) <= stats.attackRange + attackRangeTolerance)
        {
            target.TakeDamage(stats.attackDamage, gameObject);
            foreach (IChampionBasicAttackModifier modifier in attackModifiers)
                modifier?.OnBasicAttackHit(target);

            presentation?.audioController?.PlayAttackImpact();
            presentation?.SpawnImpactVfx(target.transform.position + Vector3.up * 0.8f);
        }

        attackRoutine = null;
    }

    private IEnumerator HitTowerAfterWindup(TowerHealth target, float windup, float allowedRange)
    {
        if (windup > 0f)
            yield return new WaitForSeconds(windup);

        if (target != null && target.hp > 0f && FlatDistance(transform.position, target.transform.position) <= allowedRange + attackRangeTolerance)
        {
            target.TakeDamage(stats.attackDamage);
            presentation?.audioController?.PlayAttackImpact();
            presentation?.SpawnImpactVfx(target.transform.position + Vector3.up * 1.5f);
        }

        attackRoutine = null;
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
        targetMinion = null;
        targetTower = null;

        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }

        presentation?.SetPlanarVelocity(Vector3.zero);
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
        AttachToLyra();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        AttachToLyra();
    }

    private static void AttachToLyra()
    {
        AOGPlayerMOBAController[] players = UnityEngine.Object.FindObjectsByType<AOGPlayerMOBAController>(FindObjectsSortMode.None);
        foreach (AOGPlayerMOBAController player in players)
        {
            if (player == null || !player.gameObject.name.ToLowerInvariant().Contains("lyra"))
                continue;

            if (player.GetComponent<AOGUnifiedMobaInputDriver>() == null)
                player.gameObject.AddComponent<AOGUnifiedMobaInputDriver>();
            return;
        }
    }
}
