using UnityEngine;

public class AOGPlayerMOBAController : MonoBehaviour
{
    [Header("References")]
    public Camera mainCamera;
    public Animator animator;

    [Header("Movement")]
    public LayerMask groundMask = ~0;
    public float stopDistance = 0.35f;

    [Header("Auto Attack")]
    public bool autoAttackNearestEnemy = true;

    private AOGCharacterStats stats;
    private Vector3 moveTarget;
    private bool hasMoveTarget;

    private Minion targetMinion;
    private TowerHealth targetTower;

    private float nextAttackTime;

    void Start()
    {
        stats = GetComponent<AOGCharacterStats>();

        if (stats == null)
            stats = gameObject.AddComponent<AOGCharacterStats>();

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        moveTarget = transform.position;
    }

    void Update()
    {
        if (stats == null || stats.IsDead)
            return;

        if (Input.GetKeyDown(KeyCode.S))
        {
            StopAllActions();
        }

        HandleMouseInput();
        HandleMovement();
        HandleAttackLogic();
        UpdateAnimator();
    }

    void HandleMouseInput()
    {
        if (!Input.GetMouseButtonDown(1))
            return;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, 500f, groundMask))
        {
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
    }

    void HandleMovement()
    {
        if (!hasMoveTarget)
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

    void HandleAttackLogic()
    {
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

    void AttackSelectedMinion()
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

        if (Time.time >= nextAttackTime)
        {
            nextAttackTime = Time.time + stats.attackCooldown;

            targetMinion.TakeDamage(stats.attackDamage, gameObject);

            AOGSimpleCombatAnimator visual = GetComponent<AOGSimpleCombatAnimator>();
if (visual != null)
    visual.PlayAttack();

            LyraSkillSet lyra = GetComponent<LyraSkillSet>();
            if (lyra != null)
                lyra.EmpoweredBasicAttack(targetMinion);

            if (animator != null)
                animator.SetTrigger("Attack");

            Debug.Log(name + " auto attack yaptı -> " + targetMinion.name);
        }
    }

    void AttackSelectedTower()
    {
        if (targetTower == null || targetTower.hp <= 0f)
        {
            targetTower = null;
            return;
        }

        float distance = FlatDistance(transform.position, targetTower.transform.position);

        if (distance > stats.attackRange + 3f)
        {
            MoveTowardTarget(targetTower.transform.position);
            return;
        }

        hasMoveTarget = false;
        FaceTarget(targetTower.transform.position);

        if (Time.time >= nextAttackTime)
        {
            nextAttackTime = Time.time + stats.attackCooldown;

            targetTower.TakeDamage(stats.attackDamage);

AOGSimpleCombatAnimator visual = GetComponent<AOGSimpleCombatAnimator>();
if (visual != null)
    visual.PlayAttack();
            if (animator != null)
                animator.SetTrigger("Attack");

            Debug.Log(name + " kuleye auto attack yaptı -> " + targetTower.name);
        }
    }

    Minion FindNearestEnemyMinion(float range)
    {
        Minion[] minions = FindObjectsByType<Minion>(FindObjectsSortMode.None);

        Minion closest = null;
        float closestDistance = Mathf.Infinity;

        foreach (Minion m in minions)
        {
            if (m == null) continue;
            if (!m.gameObject.activeInHierarchy) continue;
            if (m.hp <= 0f) continue;
            if (m.team == stats.team) continue;

            float distance = FlatDistance(transform.position, m.transform.position);

            if (distance <= range && distance < closestDistance)
            {
                closest = m;
                closestDistance = distance;
            }
        }

        return closest;
    }

    void MoveTowardTarget(Vector3 target)
    {
        Vector3 direction = target - transform.position;
        direction.y = 0f;

        if (direction.magnitude <= 0.1f)
            return;

        transform.position += direction.normalized * stats.moveSpeed * Time.deltaTime;
        FaceTarget(target);
    }

    void StopAllActions()
    {
        hasMoveTarget = false;
        targetMinion = null;
        targetTower = null;

        if (animator != null)
            animator.SetFloat("Speed", 0f);

        Debug.Log(name + " durdu.");
    }

    void FaceTarget(Vector3 target)
    {
        Vector3 direction = target - transform.position;
        direction.y = 0f;

        if (direction == Vector3.zero)
            return;

        Quaternion lookRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, 12f * Time.deltaTime);
    }

    float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    void UpdateAnimator()
    {
        if (animator == null)
            return;

        float speedValue = hasMoveTarget ? 1f : 0f;

        animator.SetFloat("Speed", speedValue);
    }
}