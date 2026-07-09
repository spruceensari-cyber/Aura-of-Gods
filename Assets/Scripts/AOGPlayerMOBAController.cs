using System.Collections;
using UnityEngine;

public class AOGPlayerMOBAController : MonoBehaviour
{
    [Header("References")]
    public Camera mainCamera;
    public Animator animator;
    public ChampionPresentationController presentation;

    [Header("Movement")]
    public LayerMask groundMask = ~0;
    public float stopDistance = 0.35f;

    [Header("Auto Attack")]
    public bool autoAttackNearestEnemy = true;
    public float attackRangeTolerance = 0.8f;

    private AOGCharacterStats stats;
    private Vector3 moveTarget;
    private bool hasMoveTarget;

    private Minion targetMinion;
    private TowerHealth targetTower;

    private float nextAttackTime;
    private Coroutine attackRoutine;
    private Vector3 lastFramePosition;

    private void Start()
    {
        stats = GetComponent<AOGCharacterStats>();
        if (stats == null)
            stats = gameObject.AddComponent<AOGCharacterStats>();

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (presentation == null)
            presentation = GetComponent<ChampionPresentationController>();

        if (presentation == null)
        {
            ChampionAudioController audio = GetComponent<ChampionAudioController>();
            if (audio == null)
                audio = gameObject.AddComponent<ChampionAudioController>();

            presentation = gameObject.AddComponent<ChampionPresentationController>();
            presentation.animator = animator;
            presentation.audioController = audio;
        }

        moveTarget = transform.position;
        lastFramePosition = transform.position;
    }

    private void Update()
    {
        if (stats == null || stats.IsDead)
            return;

        if (Input.GetKeyDown(KeyCode.S))
            StopAllActions();

        HandleMouseInput();
        HandleMovement();
        HandleAttackLogic();
        UpdateAnimationVelocity();
    }

    private void HandleMouseInput()
    {
        if (!Input.GetMouseButtonDown(1) || mainCamera == null)
            return;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (!Physics.Raycast(ray, out RaycastHit hit, 500f, groundMask))
            return;

        Minion clickedMinion = hit.collider.GetComponentInParent<Minion>();
        TowerHealth clickedTower = hit.collider.GetComponentInParent<TowerHealth>();

        if (clickedMinion != null && clickedMinion.team != stats.team)
        {
            targetMinion = clickedMinion;
            targetTower = null;
            hasMoveTarget = false;
            return;
        }

        if (clickedTower != null && clickedTower.towerTeam != stats.team)
        {
            targetTower = clickedTower;
            targetMinion = null;
            hasMoveTarget = false;
            return;
        }

        moveTarget = hit.point;
        moveTarget.y = transform.position.y;
        targetMinion = null;
        targetTower = null;
        hasMoveTarget = true;
    }

    private void HandleMovement()
    {
        if (!hasMoveTarget || attackRoutine != null)
            return;

        Vector3 direction = moveTarget - transform.position;
        direction.y = 0f;

        if (direction.magnitude <= stopDistance)
        {
            hasMoveTarget = false;
            return;
        }

        transform.position += direction.normalized * stats.moveSpeed * Time.deltaTime;
        FaceTarget(moveTarget);
    }

    private void HandleAttackLogic()
    {
        if (attackRoutine != null)
            return;

        if (targetMinion != null)
        {
            AttackSelectedMinion();
            return;
        }

        if (targetTower != null)
        {
            AttackSelectedTower();
            return;
        }

        if (autoAttackNearestEnemy && !hasMoveTarget)
        {
            Minion nearest = FindNearestEnemyMinion(stats.attackRange);
            if (nearest != null)
            {
                targetMinion = nearest;
                AttackSelectedMinion();
            }
        }
    }

    private void AttackSelectedMinion()
    {
        if (targetMinion == null || targetMinion.hp <= 0f)
        {
            targetMinion = null;
            return;
        }

        float distance = FlatDistance(transform.position, targetMinion.transform.position);
        if (distance > stats.attackRange)
        {
            MoveTowardTarget(targetMinion.transform.position);
            return;
        }

        hasMoveTarget = false;
        FaceTarget(targetMinion.transform.position);

        if (Time.time < nextAttackTime)
            return;

        nextAttackTime = Time.time + stats.attackCooldown;
        Minion lockedTarget = targetMinion;
        presentation?.PlayBasicAttack();
        float windup = presentation != null ? presentation.BasicAttackWindup : Mathf.Min(0.22f, stats.attackCooldown * 0.35f);
        attackRoutine = StartCoroutine(ResolveMinionAttack(lockedTarget, windup));
    }

    private IEnumerator ResolveMinionAttack(Minion lockedTarget, float windup)
    {
        if (windup > 0f)
            yield return new WaitForSeconds(windup);

        if (lockedTarget != null && lockedTarget.gameObject.activeInHierarchy && lockedTarget.hp > 0f)
        {
            float distance = FlatDistance(transform.position, lockedTarget.transform.position);
            if (distance <= stats.attackRange + attackRangeTolerance)
            {
                lockedTarget.TakeDamage(stats.attackDamage, gameObject);
                if (presentation != null)
                {
                    presentation.audioController?.PlayAttackImpact();
                    presentation.SpawnImpactVfx(lockedTarget.transform.position + Vector3.up * 0.8f);
                }
            }
        }

        attackRoutine = null;
    }

    private void AttackSelectedTower()
    {
        if (targetTower == null || targetTower.hp <= 0f)
        {
            targetTower = null;
            return;
        }

        float distance = FlatDistance(transform.position, targetTower.transform.position);
        float allowedRange = stats.attackRange + 3f;

        if (distance > allowedRange)
        {
            MoveTowardTarget(targetTower.transform.position);
            return;
        }

        hasMoveTarget = false;
        FaceTarget(targetTower.transform.position);

        if (Time.time < nextAttackTime)
            return;

        nextAttackTime = Time.time + stats.attackCooldown;
        TowerHealth lockedTarget = targetTower;
        presentation?.PlayBasicAttack();
        float windup = presentation != null ? presentation.BasicAttackWindup : Mathf.Min(0.22f, stats.attackCooldown * 0.35f);
        attackRoutine = StartCoroutine(ResolveTowerAttack(lockedTarget, windup, allowedRange));
    }

    private IEnumerator ResolveTowerAttack(TowerHealth lockedTarget, float windup, float allowedRange)
    {
        if (windup > 0f)
            yield return new WaitForSeconds(windup);

        if (lockedTarget != null && lockedTarget.gameObject.activeInHierarchy && lockedTarget.hp > 0f)
        {
            float distance = FlatDistance(transform.position, lockedTarget.transform.position);
            if (distance <= allowedRange + attackRangeTolerance)
            {
                lockedTarget.TakeDamage(stats.attackDamage);
                if (presentation != null)
                {
                    presentation.audioController?.PlayAttackImpact();
                    presentation.SpawnImpactVfx(lockedTarget.transform.position + Vector3.up * 1.5f);
                }
            }
        }

        attackRoutine = null;
    }

    private Minion FindNearestEnemyMinion(float range)
    {
        Minion[] minions = FindObjectsByType<Minion>(FindObjectsSortMode.None);
        Minion closest = null;
        float closestDistance = Mathf.Infinity;

        foreach (Minion m in minions)
        {
            if (m == null || !m.gameObject.activeInHierarchy || m.hp <= 0f || m.team == stats.team)
                continue;

            float distance = FlatDistance(transform.position, m.transform.position);
            if (distance <= range && distance < closestDistance)
            {
                closest = m;
                closestDistance = distance;
            }
        }

        return closest;
    }

    private void MoveTowardTarget(Vector3 target)
    {
        Vector3 direction = target - transform.position;
        direction.y = 0f;

        if (direction.magnitude <= 0.1f)
            return;

        transform.position += direction.normalized * stats.moveSpeed * Time.deltaTime;
        FaceTarget(target);
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
        SetAnimatorFloatIfPresent("Speed", 0f);
    }

    private void FaceTarget(Vector3 target)
    {
        Vector3 direction = target - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
            return;

        Quaternion lookRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, 12f * Time.deltaTime);
    }

    private static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private void UpdateAnimationVelocity()
    {
        float delta = Mathf.Max(Time.deltaTime, 0.0001f);
        Vector3 velocity = (transform.position - lastFramePosition) / delta;
        velocity.y = 0f;
        lastFramePosition = transform.position;

        if (presentation != null)
        {
            presentation.SetPlanarVelocity(velocity);
            return;
        }

        SetAnimatorFloatIfPresent("Speed", Mathf.Clamp01(velocity.magnitude / Mathf.Max(stats.moveSpeed, 0.01f)));
    }

    private void SetAnimatorFloatIfPresent(string parameterName, float value)
    {
        if (animator == null)
            return;

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.name == parameterName && parameter.type == AnimatorControllerParameterType.Float)
            {
                animator.SetFloat(parameterName, value);
                return;
            }
        }
    }
}
