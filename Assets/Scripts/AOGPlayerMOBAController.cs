using UnityEngine;

public class AOGPlayerMOBAController : MonoBehaviour
{
    [Header("References")]
    public Camera mainCamera;
    public Animator animator;

    [Header("Movement")]
    public LayerMask groundMask = ~0;
    public float stopDistance = 0.35f;
    public float rotationSpeed = 14f;

    [Header("Command Feel")]
    public KeyCode holdPositionKey = KeyCode.S;
    public KeyCode attackMoveKey = KeyCode.A;
    public bool cancelAttackWindupOnMove = true;
    [Range(0.05f, 0.75f)]
    public float attackWindupPercent = 0.32f;
    public float attackMoveSearchRange = 7f;
    public float targetLeashRange = 1.5f;

    [Header("Auto Attack")]
    public bool autoAttackNearestEnemy = true;
    public bool attackMoveNearestEnemy = true;
    public float towerExtraRange = 3f;

    private AOGCharacterStats stats;
    private AOGSimpleCombatAnimator combatVisual;
    private LyraSkillSet lyraSkillSet;

    private Vector3 moveTarget;
    private bool hasMoveTarget;
    private bool attackMoveActive;
    private bool movedThisFrame;

    private Minion targetMinion;
    private TowerHealth targetTower;

    private bool attackWindingUp;
    private float attackImpactTime;
    private float nextAttackReadyTime;
    private Minion pendingMinion;
    private TowerHealth pendingTower;

    void Start()
    {
        stats = GetComponent<AOGCharacterStats>();

        if (stats == null)
            stats = gameObject.AddComponent<AOGCharacterStats>();

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        combatVisual = GetComponent<AOGSimpleCombatAnimator>();
        lyraSkillSet = GetComponent<LyraSkillSet>();

        moveTarget = transform.position;
    }

    void Update()
    {
        movedThisFrame = false;

        if (stats == null || stats.IsDead)
            return;

        CompleteAttackIfReady();

        if (Input.GetKeyDown(holdPositionKey))
        {
            StopAllActions();
        }

        HandleMouseInput();
        HandleAttackMoveScan();
        HandleMovement();
        HandleAttackLogic();
        UpdateAnimator();
    }

    void HandleMouseInput()
    {
        if (!Input.GetMouseButtonDown(1))
            return;

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera == null)
            return;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (!Physics.Raycast(ray, out RaycastHit hit, 500f, groundMask))
            return;

        Minion clickedMinion = hit.collider.GetComponentInParent<Minion>();
        TowerHealth clickedTower = hit.collider.GetComponentInParent<TowerHealth>();

        if (clickedMinion != null && clickedMinion.team != stats.team)
        {
            SetMinionTarget(clickedMinion);
            return;
        }

        if (clickedTower != null && clickedTower.towerTeam != stats.team)
        {
            SetTowerTarget(clickedTower);
            return;
        }

        bool attackMoveCommand = Input.GetKey(attackMoveKey);

        if (attackMoveCommand && attackMoveNearestEnemy)
        {
            Minion nearest = FindNearestEnemyMinion(hit.point, attackMoveSearchRange);

            if (nearest != null)
            {
                SetMinionTarget(nearest);
                return;
            }
        }

        SetMoveTarget(hit.point, attackMoveCommand);
    }

    void HandleAttackMoveScan()
    {
        if (!attackMoveActive || !hasMoveTarget)
            return;

        Minion nearest = FindNearestEnemyMinion(stats.attackRange);

        if (nearest == null)
            return;

        SetMinionTarget(nearest);
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
        movedThisFrame = true;
        FaceTarget(moveTarget);
    }

    void HandleAttackLogic()
    {
        if (attackWindingUp)
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
                SetMinionTarget(nearest);
                AttackSelectedMinion();
            }
        }
    }

    void AttackSelectedMinion()
    {
        if (!IsValidTarget(targetMinion))
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
        attackMoveActive = false;
        FaceTarget(targetMinion.transform.position);
        TryBeginAttack(targetMinion, null);
    }

    void AttackSelectedTower()
    {
        if (targetTower == null || targetTower.hp <= 0f)
        {
            targetTower = null;
            return;
        }

        float towerRange = stats.attackRange + towerExtraRange;
        float distance = FlatDistance(transform.position, targetTower.transform.position);

        if (distance > towerRange)
        {
            MoveTowardTarget(targetTower.transform.position);
            return;
        }

        hasMoveTarget = false;
        attackMoveActive = false;
        FaceTarget(targetTower.transform.position);
        TryBeginAttack(null, targetTower);
    }

    void TryBeginAttack(Minion minion, TowerHealth tower)
    {
        if (Time.time < nextAttackReadyTime)
            return;

        attackWindingUp = true;
        pendingMinion = minion;
        pendingTower = tower;

        float cooldown = Mathf.Max(0.05f, stats.attackCooldown);
        attackImpactTime = Time.time + cooldown * attackWindupPercent;
        nextAttackReadyTime = Time.time + cooldown;

        PlayAttackFeedback();
    }

    void CompleteAttackIfReady()
    {
        if (!attackWindingUp || Time.time < attackImpactTime)
            return;

        attackWindingUp = false;

        if (IsValidTarget(pendingMinion))
        {
            float allowedRange = stats.attackRange + targetLeashRange;

            if (FlatDistance(transform.position, pendingMinion.transform.position) <= allowedRange)
            {
                pendingMinion.TakeDamage(stats.attackDamage, gameObject);

                if (lyraSkillSet != null)
                    lyraSkillSet.EmpoweredBasicAttack(pendingMinion);

                Debug.Log(name + " basic attack hit -> " + pendingMinion.name);
            }
        }
        else if (pendingTower != null && pendingTower.hp > 0f)
        {
            float allowedRange = stats.attackRange + towerExtraRange + targetLeashRange;

            if (FlatDistance(transform.position, pendingTower.transform.position) <= allowedRange)
            {
                pendingTower.TakeDamage(stats.attackDamage);
                Debug.Log(name + " basic attack hit tower -> " + pendingTower.name);
            }
        }

        pendingMinion = null;
        pendingTower = null;
    }

    void PlayAttackFeedback()
    {
        if (combatVisual != null)
            combatVisual.PlayAttack();

        SetAnimatorTrigger("Attack");
    }

    void SetMoveTarget(Vector3 point, bool attackMove)
    {
        moveTarget = point;
        moveTarget.y = transform.position.y;

        targetMinion = null;
        targetTower = null;
        hasMoveTarget = true;
        attackMoveActive = attackMove;

        if (cancelAttackWindupOnMove)
            CancelPendingAttack();
    }

    void SetMinionTarget(Minion minion)
    {
        targetMinion = minion;
        targetTower = null;
        hasMoveTarget = false;
        attackMoveActive = false;

        if (cancelAttackWindupOnMove)
            CancelPendingAttack();
    }

    void SetTowerTarget(TowerHealth tower)
    {
        targetTower = tower;
        targetMinion = null;
        hasMoveTarget = false;
        attackMoveActive = false;

        if (cancelAttackWindupOnMove)
            CancelPendingAttack();
    }

    Minion FindNearestEnemyMinion(float range)
    {
        return FindNearestEnemyMinion(transform.position, range);
    }

    Minion FindNearestEnemyMinion(Vector3 origin, float range)
    {
        Minion[] minions = FindObjectsByType<Minion>(FindObjectsSortMode.None);

        Minion closest = null;
        float closestDistance = Mathf.Infinity;

        foreach (Minion minion in minions)
        {
            if (!IsValidTarget(minion))
                continue;

            float distance = FlatDistance(origin, minion.transform.position);

            if (distance <= range && distance < closestDistance)
            {
                closest = minion;
                closestDistance = distance;
            }
        }

        return closest;
    }

    bool IsValidTarget(Minion minion)
    {
        if (minion == null)
            return false;

        if (!minion.gameObject.activeInHierarchy)
            return false;

        if (minion.hp <= 0f)
            return false;

        if (minion.team == stats.team)
            return false;

        return true;
    }

    void MoveTowardTarget(Vector3 target)
    {
        Vector3 direction = target - transform.position;
        direction.y = 0f;

        if (direction.magnitude <= 0.1f)
            return;

        transform.position += direction.normalized * stats.moveSpeed * Time.deltaTime;
        movedThisFrame = true;
        FaceTarget(target);
    }

    void StopAllActions()
    {
        hasMoveTarget = false;
        attackMoveActive = false;
        targetMinion = null;
        targetTower = null;
        CancelPendingAttack();
        SetAnimatorFloat("Speed", 0f);

        Debug.Log(name + " hold position.");
    }

    void CancelPendingAttack()
    {
        attackWindingUp = false;
        pendingMinion = null;
        pendingTower = null;
    }

    void FaceTarget(Vector3 target)
    {
        Vector3 direction = target - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
            return;

        Quaternion lookRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, rotationSpeed * Time.deltaTime);
    }

    float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    void UpdateAnimator()
    {
        SetAnimatorFloat("Speed", movedThisFrame ? 1f : 0f);
    }

    void SetAnimatorFloat(string parameterName, float value)
    {
        if (animator == null)
            return;

        if (!HasAnimatorParameter(parameterName, AnimatorControllerParameterType.Float))
            return;

        animator.SetFloat(parameterName, value);
    }

    void SetAnimatorTrigger(string parameterName)
    {
        if (animator == null)
            return;

        if (!HasAnimatorParameter(parameterName, AnimatorControllerParameterType.Trigger))
            return;

        animator.ResetTrigger(parameterName);
        animator.SetTrigger(parameterName);
    }

    bool HasAnimatorParameter(string parameterName, AnimatorControllerParameterType parameterType)
    {
        if (animator == null)
            return false;

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.name == parameterName && parameter.type == parameterType)
                return true;
        }

        return false;
    }
}
