using UnityEngine;

public enum MinionTeam
{
    Blue,
    Red
}

public enum MinionRole
{
    Melee,
    Ranged,
    Cannon
}

public class Minion : MonoBehaviour
{
    public MinionTeam team;
    public MinionRole role;

    [Header("Path / Lane")]
    public Vector3[] path;
    public int currentPathIndex = 1;
    public float waypointReachDistance = 0.9f;
    public float laneWidth = 1.0f;

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

    void Start()
    {
        if (hp <= 0f)
            hp = maxHp;

        animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        if (hp <= 0f)
            return;

        Minion enemyMinion = FindEnemyMinionInRange();

        if (enemyMinion != null)
        {
            FaceTarget(enemyMinion.transform.position);

            float minionDistance = Vector3.Distance(transform.position, enemyMinion.transform.position);

            if (minionDistance <= attackRange)
            {
                AttackMinion(enemyMinion);
                return;
            }
        }

        TowerHealth enemyTower = FindEnemyTowerInRange();

        if (enemyTower != null)
        {
            FaceTarget(enemyTower.transform.position);

            float towerDistance = Vector3.Distance(transform.position, enemyTower.transform.position);

            if (towerDistance <= towerAttackDistance)
            {
                AttackTower(enemyTower);
                return;
            }
        }

        MoveAlongPath();
    }

    void MoveAlongPath()
    {
        if (path == null || path.Length == 0)
            return;

        if (currentPathIndex >= path.Length)
            return;

        Vector3 target = path[currentPathIndex];
        target.y = transform.position.y;

        float distance = Vector3.Distance(transform.position, target);

        if (distance <= waypointReachDistance)
        {
            currentPathIndex++;

            if (currentPathIndex >= path.Length)
                return;

            target = path[currentPathIndex];
            target.y = transform.position.y;
        }

        MoveTo(target);
    }

    void MoveTo(Vector3 target)
    {
        target.y = transform.position.y;

        Vector3 direction = target - transform.position;
        direction.y = 0f;

        if (direction.magnitude <= 0.05f)
            return;

        Vector3 moveDirection = direction.normalized;
        transform.position += moveDirection * speed * Time.deltaTime;

        FaceTarget(target);
    }

    Minion FindEnemyMinionInRange()
    {
        Minion[] allMinions = FindObjectsByType<Minion>(FindObjectsSortMode.None);

        Minion closest = null;
        float closestDistance = Mathf.Infinity;

        foreach (Minion minion in allMinions)
        {
            if (minion == null)
                continue;

            if (minion == this)
                continue;

            if (!minion.gameObject.activeInHierarchy)
                continue;

            if (minion.hp <= 0f)
                continue;

            if (minion.team == team)
                continue;

            float distance = Vector3.Distance(transform.position, minion.transform.position);

            if (distance <= aggroRange && distance < closestDistance)
            {
                closest = minion;
                closestDistance = distance;
            }
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
            if (tower == null)
                continue;

            if (!tower.gameObject.activeInHierarchy)
                continue;

            if (tower.hp <= 0f)
                continue;

            if (tower.towerTeam == team)
                continue;

            float distance = Vector3.Distance(transform.position, tower.transform.position);

            if (distance <= towerAggroRange && distance < closestDistance)
            {
                closest = tower;
                closestDistance = distance;
            }
        }

        return closest;
    }

    void AttackMinion(Minion target)
    {
        if (target == null)
            return;

        if (Time.time < nextAttackTime)
            return;

        nextAttackTime = Time.time + attackRate;

        if (animator != null)
            animator.SetTrigger("Attack");

        target.TakeDamage(damage, gameObject);

        Debug.Log(name + " hit minion: " + target.name + " | Damage: " + damage);
    }

    void AttackTower(TowerHealth tower)
    {
        if (tower == null)
            return;

        if (Time.time < nextAttackTime)
            return;

        nextAttackTime = Time.time + attackRate;

        if (animator != null)
            animator.SetTrigger("Attack");

        tower.TakeDamage(damage);

        Debug.Log(name + " hit tower: " + tower.name + " | Damage: " + damage);
    }

    public void TakeDamage(float amount)
    {
        TakeDamage(amount, null);
    }

    public void TakeDamage(float amount, GameObject attacker)
    {
        if (hp <= 0f || amount <= 0f)
            return;

        float oldHp = hp;
        hp -= amount;
        hp = Mathf.Clamp(hp, 0f, maxHp);

        float appliedDamage = oldHp - hp;
        AOGFloatingCombatText.SpawnDamage(transform.position, appliedDamage, GetDamageTextColor(attacker));

        Debug.Log(name + " took damage. HP: " + hp + " / " + maxHp);

        if (hp <= 0f)
        {
            Destroy(gameObject);
        }
    }

    Color GetDamageTextColor(GameObject attacker)
    {
        if (attacker != null)
        {
            AOGCharacterStats attackerStats = attacker.GetComponent<AOGCharacterStats>();

            if (attackerStats != null)
                return attackerStats.team == MinionTeam.Blue ? new Color(0.35f, 0.85f, 1f, 1f) : new Color(1f, 0.25f, 0.18f, 1f);
        }

        return new Color(1f, 0.72f, 0.22f, 1f);
    }

    void FaceTarget(Vector3 target)
    {
        Vector3 direction = target - transform.position;
        direction.y = 0f;

        if (direction == Vector3.zero)
            return;

        Quaternion lookRotation = Quaternion.LookRotation(direction);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            lookRotation,
            rotationSpeed * Time.deltaTime
        );
    }
}
