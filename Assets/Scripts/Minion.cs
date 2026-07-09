using UnityEngine;

public enum MinionTeam { Blue, Red }
public enum MinionRole { Melee, Ranged, Cannon }

public class Minion : MonoBehaviour
{
    public MinionTeam team;
    public MinionRole role;

    [Header("Path / Lane")]
    public Vector3[] path;
    public int currentPathIndex = 1;
    public float waypointReachDistance = 0.9f;
    public float laneWidth = 1.0f;
    public float formationOffset;
    public float separationRadius = 1.15f;
    public float separationStrength = 1.2f;
    public float rejoinStrength = 3.5f;

    [Header("Stats")]
    public float speed = 3f;
    public float maxHp = 50f;
    public float hp = 50f;
    public float damage = 8f;
    public float attackRange = 3.5f;
    public float attackRate = 1.1f;

    [Header("Projectile")]
    public GameObject projectilePrefab;

    [Header("Combat")]
    public float aggroRange = 5.5f;
    public float towerAggroRange = 18f;
    public float towerAttackDistance = 9f;
    public float rotationSpeed = 8f;

    private float nextAttackTime;
    private Animator animator;
    private float segmentT;
    private Vector3 lastLanePoint;

    void Start()
    {
        if (hp <= 0f) hp = maxHp;
        animator = GetComponentInChildren<Animator>();
        if (path != null && path.Length > 1)
            lastLanePoint = path[0];
    }

    void Update()
    {
        if (hp <= 0f) return;

        Minion enemyMinion = FindEnemyMinionInRange();
        if (enemyMinion != null)
        {
            FaceTarget(enemyMinion.transform.position);
            if (Vector3.Distance(transform.position, enemyMinion.transform.position) <= attackRange)
            {
                AttackMinion(enemyMinion);
                return;
            }
        }

        TowerHealth enemyTower = FindEnemyTowerInRange();
        if (enemyTower != null)
        {
            FaceTarget(enemyTower.transform.position);
            if (Vector3.Distance(transform.position, enemyTower.transform.position) <= towerAttackDistance)
            {
                AttackTower(enemyTower);
                return;
            }
        }

        MoveAlongPath();
    }

    void MoveAlongPath()
    {
        if (path == null || path.Length < 2 || currentPathIndex >= path.Length)
            return;

        int i1 = Mathf.Clamp(currentPathIndex - 1, 0, path.Length - 1);
        int i2 = Mathf.Clamp(currentPathIndex, 0, path.Length - 1);
        int i0 = Mathf.Clamp(i1 - 1, 0, path.Length - 1);
        int i3 = Mathf.Clamp(i2 + 1, 0, path.Length - 1);

        Vector3 p0 = path[i0];
        Vector3 p1 = path[i1];
        Vector3 p2 = path[i2];
        Vector3 p3 = path[i3];

        float segmentLength = Mathf.Max(0.1f, Vector3.Distance(p1, p2));
        segmentT += (speed / segmentLength) * Time.deltaTime;

        while (segmentT >= 1f && currentPathIndex < path.Length - 1)
        {
            segmentT -= 1f;
            currentPathIndex++;
            i1 = Mathf.Clamp(currentPathIndex - 1, 0, path.Length - 1);
            i2 = Mathf.Clamp(currentPathIndex, 0, path.Length - 1);
            i0 = Mathf.Clamp(i1 - 1, 0, path.Length - 1);
            i3 = Mathf.Clamp(i2 + 1, 0, path.Length - 1);
            p0 = path[i0]; p1 = path[i1]; p2 = path[i2]; p3 = path[i3];
        }

        Vector3 center = CatmullRom(p0, p1, p2, p3, Mathf.Clamp01(segmentT));
        Vector3 ahead = CatmullRom(p0, p1, p2, p3, Mathf.Clamp01(segmentT + 0.04f));
        Vector3 tangent = ahead - center;
        tangent.y = 0f;
        if (tangent.sqrMagnitude < 0.001f) tangent = (p2 - p1).normalized;
        tangent.Normalize();

        Vector3 right = new Vector3(tangent.z, 0f, -tangent.x);
        Vector3 laneTarget = center + right * Mathf.Clamp(formationOffset, -laneWidth, laneWidth);
        laneTarget.y = transform.position.y;

        Vector3 desired = (laneTarget - transform.position) * rejoinStrength;
        desired += ComputeSeparation();
        desired.y = 0f;
        if (desired.sqrMagnitude > 0.001f)
        {
            Vector3 dir = desired.normalized;
            transform.position += dir * speed * Time.deltaTime;
            FaceTarget(transform.position + dir);
        }

        lastLanePoint = laneTarget;
    }

    Vector3 ComputeSeparation()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, separationRadius);
        Vector3 force = Vector3.zero;
        int count = 0;
        foreach (Collider hit in hits)
        {
            Minion other = hit.GetComponentInParent<Minion>();
            if (other == null || other == this || other.team != team) continue;
            Vector3 away = transform.position - other.transform.position;
            away.y = 0f;
            float sqr = away.sqrMagnitude;
            if (sqr < 0.001f) continue;
            force += away.normalized / Mathf.Max(0.2f, Mathf.Sqrt(sqr));
            count++;
        }
        return count > 0 ? (force / count) * separationStrength : Vector3.zero;
    }

    static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;
        return 0.5f * ((2f * p1) + (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
    }

    Minion FindEnemyMinionInRange()
    {
        Minion[] allMinions = FindObjectsByType<Minion>(FindObjectsSortMode.None);
        Minion closest = null;
        float closestDistance = Mathf.Infinity;
        foreach (Minion minion in allMinions)
        {
            if (minion == null || minion == this || !minion.gameObject.activeInHierarchy || minion.hp <= 0f || minion.team == team) continue;
            float d = Vector3.Distance(transform.position, minion.transform.position);
            if (d <= aggroRange && d < closestDistance) { closest = minion; closestDistance = d; }
        }
        return closest;
    }

    TowerHealth FindEnemyTowerInRange()
    {
        TowerHealth[] towers = FindObjectsByType<TowerHealth>(FindObjectsSortMode.None);
        TowerHealth closest = null;
        float closestDistance = Mathf.Infinity;
        foreach (TowerHealth tower in towers)
        {
            if (tower == null || !tower.gameObject.activeInHierarchy || tower.hp <= 0f || tower.towerTeam == team) continue;
            float d = Vector3.Distance(transform.position, tower.transform.position);
            if (d <= towerAggroRange && d < closestDistance) { closest = tower; closestDistance = d; }
        }
        return closest;
    }

    void AttackMinion(Minion target)
    {
        if (target == null || Time.time < nextAttackTime) return;
        nextAttackTime = Time.time + attackRate;
        if (animator != null) animator.SetTrigger("Attack");
        target.TakeDamage(damage, gameObject);
    }

    void AttackTower(TowerHealth tower)
    {
        if (tower == null || Time.time < nextAttackTime) return;
        nextAttackTime = Time.time + attackRate;
        if (animator != null) animator.SetTrigger("Attack");
        tower.TakeDamage(damage);
    }

    public void TakeDamage(float amount) => TakeDamage(amount, null);

    public void TakeDamage(float amount, GameObject attacker)
    {
        hp = Mathf.Clamp(hp - amount, 0f, maxHp);
        if (hp <= 0f) Destroy(gameObject);
    }

    void FaceTarget(Vector3 target)
    {
        Vector3 direction = target - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f) return;
        Quaternion lookRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, rotationSpeed * Time.deltaTime);
    }
}
