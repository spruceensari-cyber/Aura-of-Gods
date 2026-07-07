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

    [Header("Combat")]
    public float aggroRange = 6f;
    public float attackRange = 2.2f;
    public float damage = 10f;
    public float attackCooldown = 1.1f;

    [Header("Target Priority")]
    public bool minionsBeforeTowers = true;

    private AOGDamageable currentTarget;
    private float nextAttackTime;

    void Update()
    {
        if (!gameObject.activeInHierarchy)
            return;

        currentTarget = FindBestTarget();

        if (currentTarget != null)
        {
            float distance = Vector3.Distance(transform.position, currentTarget.transform.position);

            Face(currentTarget.transform.position);

            if (distance <= attackRange)
            {
                Attack(currentTarget);
                return;
            }

            MoveTowards(currentTarget.transform.position);
            return;
        }

        FollowLane();
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

        Vector3 targetPos = waypoint.position;
        targetPos.y = transform.position.y;

        float distance = Vector3.Distance(transform.position, targetPos);

        if (distance <= waypointReachDistance)
        {
            currentWaypointIndex++;
            return;
        }

        MoveTowards(targetPos);
    }

    void MoveTowards(Vector3 targetPos)
    {
        targetPos.y = transform.position.y;

        Vector3 direction = targetPos - transform.position;

        if (direction.magnitude < 0.05f)
            return;

        Vector3 moveDirection = direction.normalized;
        transform.position += moveDirection * moveSpeed * Time.deltaTime;

        Face(targetPos);
    }

    void Face(Vector3 targetPos)
    {
        Vector3 direction = targetPos - transform.position;
        direction.y = 0f;

        if (direction == Vector3.zero)
            return;

        Quaternion lookRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            lookRotation,
            rotateSpeed * Time.deltaTime
        );
    }

    void Attack(AOGDamageable target)
    {
        if (Time.time < nextAttackTime)
            return;

        nextAttackTime = Time.time + attackCooldown;

        if (target != null)
        {
            target.TakeDamage(damage);
        }
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
            if (damageable == null)
                continue;

            if (!damageable.gameObject.activeInHierarchy)
                continue;

            if (damageable.currentHealth <= 0)
                continue;

            AOGCombatUnit otherUnit = damageable.GetComponent<AOGCombatUnit>();

            if (otherUnit == null)
                continue;

            if (otherUnit.team == team)
                continue;

            if (otherUnit.team == AOGTeam.Neutral)
                continue;

            float distance = Vector3.Distance(transform.position, damageable.transform.position);

            if (distance > aggroRange)
                continue;

            if (otherUnit.unitType == AOGUnitType.Minion)
            {
                if (distance < minionDistance)
                {
                    nearestEnemyMinion = damageable;
                    minionDistance = distance;
                }
            }
            else if (otherUnit.unitType == AOGUnitType.Tower)
            {
                if (distance < towerDistance)
                {
                    nearestEnemyTower = damageable;
                    towerDistance = distance;
                }
            }
            else if (otherUnit.unitType == AOGUnitType.Nexus)
            {
                if (distance < nexusDistance)
                {
                    nearestEnemyNexus = damageable;
                    nexusDistance = distance;
                }
            }
        }

        if (minionsBeforeTowers)
        {
            if (nearestEnemyMinion != null)
                return nearestEnemyMinion;

            if (nearestEnemyTower != null)
                return nearestEnemyTower;

            if (nearestEnemyNexus != null)
                return nearestEnemyNexus;
        }
        else
        {
            if (nearestEnemyTower != null)
                return nearestEnemyTower;

            if (nearestEnemyMinion != null)
                return nearestEnemyMinion;

            if (nearestEnemyNexus != null)
                return nearestEnemyNexus;
        }

        return null;
    }
}