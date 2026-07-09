using UnityEngine;

/// <summary>
/// MOBA-style player controller: right-click movement, left-click attack orders and Q/W/E/R casting.
/// Keeps combat orders separate from presentation so models and animation rigs can be replaced later.
/// </summary>
public class ChampionController : MonoBehaviour
{
    private Champion champion;
    private Rigidbody rb;
    private Camera mainCamera;
    private Vector3 moveTarget;
    private bool isMoving;

    [Header("Movement")]
    [SerializeField] private float stoppingDistance = 0.35f;
    [SerializeField] private float turnSpeed = 12f;

    [Header("Basic Attack")]
    [SerializeField] private float attackRange = 2.25f;
    [SerializeField] private float attackWindup = 0.18f;

    private ChampionAbility[] abilities = new ChampionAbility[4];
    private Transform attackTarget;
    private Champion attackChampion;
    private CombatUnit attackUnit;
    private float nextAttackTime;
    private bool attackCommitted;
    private float attackCommitTime;

    public Vector3 MoveTarget => moveTarget;
    public bool IsMoving => isMoving;
    public Transform AttackTarget => attackTarget;

    void Start()
    {
        champion = GetComponent<Champion>();
        rb = GetComponent<Rigidbody>();
        mainCamera = Camera.main;

        ChampionAbility[] allAbilities = GetComponents<ChampionAbility>();
        for (int i = 0; i < Mathf.Min(allAbilities.Length, abilities.Length); i++)
            abilities[i] = allAbilities[i];
    }

    void Update()
    {
        if (champion == null || !champion.IsAlive)
            return;

        if (mainCamera == null)
            mainCamera = Camera.main;

        HandleOrders();
        HandleAbilities();
        UpdateAttackOrder();
        UpdateMovement();
        ResolveAttackWindup();
    }

    private void HandleOrders()
    {
        if (mainCamera == null)
            return;

        if (Input.GetMouseButtonDown(1))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
            {
                ClearAttackOrder();
                moveTarget = hit.point;
                moveTarget.y = transform.position.y;
                isMoving = true;
            }
        }

        if (Input.GetMouseButtonDown(0))
            TryIssueAttackOrder();
    }

    private void TryIssueAttackOrder()
    {
        if (mainCamera == null)
            return;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, 1000f))
            return;

        Champion targetChampion = hit.collider.GetComponentInParent<Champion>();
        if (targetChampion != null && targetChampion != champion && targetChampion.Team != champion.Team)
        {
            SetAttackOrder(targetChampion.transform, targetChampion, null);
            return;
        }

        CombatUnit targetUnit = hit.collider.GetComponentInParent<CombatUnit>();
        if (targetUnit != null && targetUnit.UnitTeam != champion.Team)
            SetAttackOrder(targetUnit.transform, null, targetUnit);
    }

    private void SetAttackOrder(Transform target, Champion targetChampion, CombatUnit targetUnit)
    {
        attackTarget = target;
        attackChampion = targetChampion;
        attackUnit = targetUnit;
        isMoving = false;
    }

    private void UpdateAttackOrder()
    {
        if (attackTarget == null)
            return;

        bool validChampion = attackChampion == null || (attackChampion.IsAlive && attackChampion.gameObject.activeInHierarchy);
        bool validUnit = attackUnit == null || attackUnit.IsAlive;
        if (!validChampion || !validUnit)
        {
            ClearAttackOrder();
            return;
        }

        Vector3 flatTarget = attackTarget.position;
        flatTarget.y = transform.position.y;
        float distance = Vector3.Distance(transform.position, flatTarget);

        if (distance > attackRange)
        {
            moveTarget = flatTarget;
            isMoving = true;
            return;
        }

        isMoving = false;
        StopHorizontalVelocity();
        FaceDirection(flatTarget - transform.position);

        if (!attackCommitted && Time.time >= nextAttackTime && !champion.IsCasting && !champion.IsStunned)
        {
            attackCommitted = true;
            attackCommitTime = Time.time + attackWindup;
            float attacksPerSecond = Mathf.Max(0.1f, champion.AttackSpeed);
            nextAttackTime = Time.time + (1f / attacksPerSecond);
        }
    }

    private void ResolveAttackWindup()
    {
        if (!attackCommitted || Time.time < attackCommitTime)
            return;

        attackCommitted = false;
        if (attackTarget == null)
            return;

        float distance = Vector3.Distance(
            new Vector3(transform.position.x, 0f, transform.position.z),
            new Vector3(attackTarget.position.x, 0f, attackTarget.position.z));

        if (distance > attackRange + 0.5f)
            return;

        if (attackChampion != null)
            attackChampion.TakeDamage(champion.AttackDamage, DamageType.Physical);
        else if (attackUnit != null)
            attackUnit.TakeDamage(champion.AttackDamage);
    }

    private void UpdateMovement()
    {
        if (!isMoving || champion.IsCasting || champion.IsStunned)
        {
            StopHorizontalVelocity();
            return;
        }

        Vector3 direction = moveTarget - transform.position;
        direction.y = 0f;
        float distance = direction.magnitude;

        if (distance <= stoppingDistance)
        {
            isMoving = false;
            StopHorizontalVelocity();
            return;
        }

        direction /= distance;
        FaceDirection(direction);

        float speed = champion.MovementSpeed;
        if (rb != null)
            rb.velocity = new Vector3(direction.x * speed, rb.velocity.y, direction.z * speed);
        else
            transform.position += direction * speed * Time.deltaTime;
    }

    private void FaceDirection(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * turnSpeed);
    }

    private void StopHorizontalVelocity()
    {
        if (rb != null)
            rb.velocity = new Vector3(0f, rb.velocity.y, 0f);
    }

    private void ClearAttackOrder()
    {
        attackTarget = null;
        attackChampion = null;
        attackUnit = null;
        attackCommitted = false;
    }

    private void HandleAbilities()
    {
        if (Input.GetKeyDown(KeyCode.Q)) CastAbility(0);
        if (Input.GetKeyDown(KeyCode.W)) CastAbility(1);
        if (Input.GetKeyDown(KeyCode.E)) CastAbility(2);
        if (Input.GetKeyDown(KeyCode.R)) CastAbility(3);
    }

    private void CastAbility(int abilityIndex)
    {
        if (abilityIndex < 0 || abilityIndex >= abilities.Length || abilities[abilityIndex] == null || mainCamera == null)
            return;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Vector3 targetPosition = transform.position + transform.forward * abilities[abilityIndex].Range;
        Champion targetChampion = null;

        if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
        {
            targetPosition = hit.point;
            targetChampion = hit.collider.GetComponentInParent<Champion>();
        }

        abilities[abilityIndex].Cast(targetPosition, targetChampion);
    }
}
