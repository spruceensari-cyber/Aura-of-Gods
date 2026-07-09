using UnityEngine;

/// <summary>
/// MOBA-style player controller: right-click movement, left-click attack orders and responsive Q/W/E/R casting.
/// Runtime kits may add abilities after Start, so ability bindings self-heal automatically.
/// </summary>
public class ChampionController : MonoBehaviour
{
    private Champion champion;
    private AOGCrowdControlRuntime crowdControl;
    private Rigidbody rb;
    private Camera mainCamera;
    private Vector3 moveTarget;
    private bool isMoving;

    [Header("Movement")]
    [SerializeField] private float stoppingDistance = 0.35f;
    [SerializeField] private float turnSpeed = 14f;

    [Header("Basic Attack")]
    [SerializeField] private float attackRange = 2.25f;
    [SerializeField] private float attackWindup = 0.16f;

    private readonly ChampionAbility[] abilities = new ChampionAbility[4];
    private Transform attackTarget;
    private Champion attackChampion;
    private CombatUnit attackUnit;
    private float nextAttackTime;
    private bool attackCommitted;
    private float attackCommitTime;
    private float nextAbilityRefreshTime;
    private int cachedAbilityCount = -1;

    public Vector3 MoveTarget => moveTarget;
    public bool IsMoving => isMoving;
    public Transform AttackTarget => attackTarget;
    public float AttackRange => attackRange;

    public event System.Action OnBasicAttackWindup;
    public event System.Action OnBasicAttackResolved;
    public event System.Action<AbilityKey, bool> OnAbilityInputResolved;

    void Start()
    {
        champion = GetComponent<Champion>();
        crowdControl = GetComponent<AOGCrowdControlRuntime>();
        rb = GetComponent<Rigidbody>();
        mainCamera = Camera.main;
        RefreshAbilities();
    }

    public void RefreshAbilities()
    {
        for (int i = 0; i < abilities.Length; i++) abilities[i] = null;

        ChampionAbility[] allAbilities = GetComponents<ChampionAbility>();
        cachedAbilityCount = allAbilities.Length;
        foreach (ChampionAbility ability in allAbilities)
        {
            int index = ability.Key switch
            {
                AbilityKey.Q => 0,
                AbilityKey.W => 1,
                AbilityKey.E => 2,
                AbilityKey.R => 3,
                _ => -1
            };
            if (index >= 0) abilities[index] = ability;
        }
    }

    void Update()
    {
        if (champion == null || !champion.IsAlive) return;
        if (crowdControl == null) crowdControl = GetComponent<AOGCrowdControlRuntime>();
        if (mainCamera == null) mainCamera = Camera.main;

        AutoRefreshAbilities();
        HandleOrders();
        HandleAbilities();
        UpdateAttackOrder();
        UpdateMovement();
        ResolveAttackWindup();
    }

    private void AutoRefreshAbilities()
    {
        if (Time.unscaledTime < nextAbilityRefreshTime) return;
        nextAbilityRefreshTime = Time.unscaledTime + 0.20f;

        int currentCount = GetComponents<ChampionAbility>().Length;
        bool missingSlot = abilities[0] == null || abilities[1] == null || abilities[2] == null || abilities[3] == null;
        if (currentCount != cachedAbilityCount || (currentCount > 0 && missingSlot))
            RefreshAbilities();
    }

    private void HandleOrders()
    {
        if (mainCamera == null) return;
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
        if (Input.GetMouseButtonDown(0)) TryIssueAttackOrder();
    }

    private void TryIssueAttackOrder()
    {
        if (mainCamera == null) return;
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, 1000f)) return;

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
        if (attackTarget == null) return;
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

        if (!attackCommitted && Time.time >= nextAttackTime && !champion.IsCasting && !champion.IsStunned && !(crowdControl != null && crowdControl.IsAirborne))
        {
            attackCommitted = true;
            attackCommitTime = Time.time + attackWindup;
            float attacksPerSecond = Mathf.Max(0.1f, champion.AttackSpeed);
            nextAttackTime = Time.time + (1f / attacksPerSecond);
            OnBasicAttackWindup?.Invoke();
        }
    }

    private void ResolveAttackWindup()
    {
        if (!attackCommitted || Time.time < attackCommitTime) return;
        attackCommitted = false;
        if (attackTarget == null) return;

        float distance = Vector3.Distance(
            new Vector3(transform.position.x, 0f, transform.position.z),
            new Vector3(attackTarget.position.x, 0f, attackTarget.position.z));
        if (distance > attackRange + 0.5f) return;

        if (attackChampion != null)
        {
            AOGMatchEventBridgeRuntime.Report(champion, attackChampion);
            attackChampion.TakeDamage(champion.AttackDamage, DamageType.Physical);
        }
        else if (attackUnit != null)
        {
            attackUnit.TakeDamage(champion.AttackDamage);
        }

        OnBasicAttackResolved?.Invoke();
    }

    private void UpdateMovement()
    {
        bool hardStopped = crowdControl != null && (crowdControl.IsRooted || crowdControl.IsAirborne);
        if (!isMoving || champion.IsCasting || champion.IsStunned || hardStopped)
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
        if (direction.sqrMagnitude < 0.0001f) return;
        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * turnSpeed);
    }

    private void StopHorizontalVelocity()
    {
        if (rb != null) rb.velocity = new Vector3(0f, rb.velocity.y, 0f);
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
        if (crowdControl != null && crowdControl.IsSilenced) return;
        if (Input.GetKeyDown(KeyCode.Q)) CastAbility(0);
        if (Input.GetKeyDown(KeyCode.W)) CastAbility(1);
        if (Input.GetKeyDown(KeyCode.E)) CastAbility(2);
        if (Input.GetKeyDown(KeyCode.R)) CastAbility(3);
    }

    private void CastAbility(int abilityIndex)
    {
        if (abilityIndex < 0 || abilityIndex >= abilities.Length)
            return;

        ChampionAbility ability = abilities[abilityIndex];
        if (ability == null)
        {
            RefreshAbilities();
            ability = abilities[abilityIndex];
        }

        AbilityKey key = (AbilityKey)abilityIndex;
        if (ability == null || mainCamera == null)
        {
            OnAbilityInputResolved?.Invoke(key, false);
            AOGAudioDirectorRuntime.Instance?.PlayCue(AOGAudioCue.UIBack);
            return;
        }

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Vector3 targetPosition = transform.position + transform.forward * ability.Range;
        Champion targetChampion = null;
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
        {
            targetPosition = hit.point;
            targetChampion = hit.collider.GetComponentInParent<Champion>();
        }

        bool casted = ability.Cast(targetPosition, targetChampion);
        OnAbilityInputResolved?.Invoke(ability.Key, casted);
        if (!casted)
            AOGAudioDirectorRuntime.Instance?.PlayCue(AOGAudioCue.UIBack);
    }
}
