using UnityEngine;

public class AOGLaneMinionAI : MonoBehaviour
{
    public AOGTeam team = AOGTeam.Blue;

    [Header("Lane Path")]
    public Transform[] waypoints;
    public int currentWaypointIndex = 0;

    [Header("Movement")]
    public float moveSpeed = 2.5f;
    public float rotateSpeed = 8f;
    public float waypointReachDistance = 1.2f;
    public float laneReturnDistance = 7f;
    public float formationSpacing = 0.65f;

    [Header("Combat")]
    public float aggroRange = 6f;
    public float attackRange = 2.2f;
    public float damage = 10f;
    public float attackCooldown = 1.1f;
    public float targetScanInterval = 0.25f;
    public float maxChaseDistanceFromLane = 8.5f;

    [Header("Target Priority")]
    public bool minionsBeforeTowers = true;

    private AOGDamageable currentTarget;
    private float nextAttackTime;
    private float nextTargetScanTime;
    private float formationOffset;
    private Vector3 lastLaneAnchor;

    void Awake()
    {
        int slot = Mathf.Abs(gameObject.name.GetHashCode()) % 5;
        formationOffset = (slot - 2) * formationSpacing;
    }

    void Update()
    {
        if (!gameObject.activeInHierarchy)
            return;

        UpdateLaneAnchor();
        ValidateCurrentTarget();

        if (currentTarget == null && Time.time >= nextTargetScanTime)
        {
            nextTargetScanTime = Time.time + targetScanInterval;
            currentTarget = FindBestTarget();
        }

        if (currentTarget != null)
        {
            float laneDistance = FlatDistance(transform.position, lastLaneAnchor);
            if (laneDistance > maxChaseDistanceFromLane)
            {
                currentTarget = null;
                ReturnToLane();
                return;
            }

            float distance = FlatDistance(transform.position, currentTarget.transform.position);
            Face(currentTarget.transform.position);

            if (distance <= attackRange)
            {
                Attack(currentTarget);
                return;
            }

            MoveTowards(currentTarget.transform.position);
            return;
        }

        if (FlatDistance(transform.position, lastLaneAnchor) > laneReturnDistance)
        {
            ReturnToLane();
            return;
        }

        FollowLane();
    }

    void UpdateLaneAnchor()
    {
        if (waypoints == null || waypoints.Length == 0)
        {
            lastLaneAnchor = transform.position;
            return;
        }

        int index = Mathf.Clamp(currentWaypointIndex, 0, waypoints.Length - 1);
        Transform current = waypoints[index];
        if (current == null)
        {
            lastLaneAnchor = transform.position;
            return;
        }

        Vector3 anchor = current.position;
        Vector3 direction = Vector3.forward;

        if (index > 0 && waypoints[index - 1] != null)
            direction = (current.position - waypoints[index - 1].position).normalized;
        else if (index + 1 < waypoints.Length && waypoints[index + 1] != null)
            direction = (waypoints[index + 1].position - current.position).normalized;

        Vector3 side = new Vector3(-direction.z, 0f, direction.x);
        lastLaneAnchor = anchor + side * formationOffset;
        lastLaneAnchor.y = transform.position.y;
    }

    void FollowLane()
    {
        if (waypoints == null || waypoints.Length == 0)
            return;

        if (currentWaypointIndex >= waypoints.Length)
            return;

        Transform waypoint = waypoints[currentWaypointIndex];
        if (waypoint == null)
        {
            currentWaypointIndex++;
            return;
        }

        Vector3 targetPos = lastLaneAnchor;
        float distance = FlatDistance(transform.position, targetPos);

        if (distance <= waypointReachDistance)
        {
            currentWaypointIndex++;
            UpdateLaneAnchor();
            return;
        }

        MoveTowards(targetPos);
    }

    void ReturnToLane()
    {
        MoveTowards(lastLaneAnchor);
    }

    void MoveTowards(Vector3 targetPos)
    {
        targetPos.y = transform.position.y;
        Vector3 direction = targetPos - transform.position;
        if (direction.sqrMagnitude < 0.0025f)
            return;

        Vector3 moveDirection = direction.normalized;
        transform.position += moveDirection * moveSpeed * Time.deltaTime;
        Face(targetPos);
    }

    void Face(Vector3 targetPos)
    {
        Vector3 direction = targetPos - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
            return;

        Quaternion lookRotation = Quaternion.LookRotation(direction.normalized);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, rotateSpeed * Time.deltaTime);
    }

    void Attack(AOGDamageable target)
    {
        if (Time.time < nextAttackTime)
            return;

        nextAttackTime = Time.time + attackCooldown;
        if (target != null)
            target.TakeDamage(damage);
    }

    void ValidateCurrentTarget()
    {
        if (currentTarget == null)
            return;

        if (!currentTarget.gameObject.activeInHierarchy || currentTarget.currentHealth <= 0)
        {
            currentTarget = null;
            return;
        }

        AOGCombatUnit unit = currentTarget.GetComponent<AOGCombatUnit>();
        if (unit == null || unit.team == team || unit.team == AOGTeam.Neutral)
        {
            currentTarget = null;
            return;
        }

        float targetDistance = FlatDistance(transform.position, currentTarget.transform.position);
        float laneDistance = FlatDistance(transform.position, lastLaneAnchor);
        if (targetDistance > aggroRange * 1.35f || laneDistance > maxChaseDistanceFromLane)
            currentTarget = null;
    }

    AOGDamageable FindBestTarget()
    {
        AOGDamageable[] allDamageables = FindObjectsByType<AOGDamageable>(FindObjectsSortMode.None);

        AOGDamageable nearestEnemyMinion = null;
        AOGDamageable nearestEnemyTower = null;
        AOGDamageable nearestEnemyNexus = null;

        float minionDistance = Mathf.Infinity;
        float towerDistance = Mathf.Infinity;
        float nexusDistance = Mathf.Infinity;

        foreach (AOGDamageable damageable in allDamageables)
        {
            if (damageable == null || !damageable.gameObject.activeInHierarchy || damageable.currentHealth <= 0)
                continue;

            AOGCombatUnit otherUnit = damageable.GetComponent<AOGCombatUnit>();
            if (otherUnit == null || otherUnit.team == team || otherUnit.team == AOGTeam.Neutral)
                continue;

            float distance = FlatDistance(transform.position, damageable.transform.position);
            if (distance > aggroRange)
                continue;

            if (otherUnit.unitType == AOGUnitType.Minion && distance < minionDistance)
            {
                nearestEnemyMinion = damageable;
                minionDistance = distance;
            }
            else if (otherUnit.unitType == AOGUnitType.Tower && distance < towerDistance)
            {
                nearestEnemyTower = damageable;
                towerDistance = distance;
            }
            else if (otherUnit.unitType == AOGUnitType.Nexus && distance < nexusDistance)
            {
                nearestEnemyNexus = damageable;
                nexusDistance = distance;
            }
        }

        if (minionsBeforeTowers)
            return nearestEnemyMinion != null ? nearestEnemyMinion : nearestEnemyTower != null ? nearestEnemyTower : nearestEnemyNexus;

        return nearestEnemyTower != null ? nearestEnemyTower : nearestEnemyMinion != null ? nearestEnemyMinion : nearestEnemyNexus;
    }

    static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
