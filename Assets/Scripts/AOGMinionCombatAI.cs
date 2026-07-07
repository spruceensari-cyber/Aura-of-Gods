using UnityEngine;

public class AOGMinionCombatAI : MonoBehaviour
{
    public float moveSpeed = 2.6f;
    public float rotateSpeed = 8f;

    public float attackRange = 2.2f;
    public float aggroRange = 5.5f;
    public float attackCooldown = 1.1f;
    public float damage = 12f;

    public string blueTargetName = "Red_Target";
    public string redTargetName = "Blue_Target";

    private AOGCombatUnit unit;
    private AOGDamageable currentTarget;
    private float nextAttackTime;

    void Awake()
    {
        unit = GetComponent<AOGCombatUnit>();
    }

    void Update()
    {
        if (unit == null)
            return;

        currentTarget = FindNearestEnemy();

        if (currentTarget != null)
        {
            float dist = Vector3.Distance(transform.position, currentTarget.transform.position);

            if (dist <= attackRange)
            {
                AttackTarget();
                Face(currentTarget.transform.position);
                return;
            }
        }

        MoveForwardToEnemyBase();
    }

    AOGDamageable FindNearestEnemy()
    {
        AOGDamageable[] all = FindObjectsByType<AOGDamageable>(FindObjectsSortMode.None);

        AOGDamageable best = null;
        float bestDist = Mathf.Infinity;

        foreach (AOGDamageable d in all)
        {
            if (d == null || !d.gameObject.activeInHierarchy)
                continue;

            AOGCombatUnit otherUnit = d.GetComponent<AOGCombatUnit>();
            if (otherUnit == null)
                continue;

            if (otherUnit.team == unit.team || otherUnit.team == AOGTeam.Neutral)
                continue;

            float dist = Vector3.Distance(transform.position, d.transform.position);

            if (dist <= aggroRange && dist < bestDist)
            {
                best = d;
                bestDist = dist;
            }
        }

        return best;
    }

    void AttackTarget()
    {
        if (Time.time < nextAttackTime)
            return;

        nextAttackTime = Time.time + attackCooldown;

        if (currentTarget != null)
            currentTarget.TakeDamage(damage);
    }

    void MoveForwardToEnemyBase()
    {
        string targetName = unit.team == AOGTeam.Blue ? blueTargetName : redTargetName;
        GameObject targetObj = GameObject.Find(targetName);

        if (targetObj == null)
            return;

        Vector3 targetPos = targetObj.transform.position;
        targetPos.y = transform.position.y;

        Vector3 dir = targetPos - transform.position;

        if (dir.magnitude < 1f)
            return;

        Vector3 moveDir = dir.normalized;
        transform.position += moveDir * moveSpeed * Time.deltaTime;

        Face(targetPos);
    }

    void Face(Vector3 pos)
    {
        Vector3 dir = pos - transform.position;
        dir.y = 0;

        if (dir == Vector3.zero)
            return;

        Quaternion targetRot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotateSpeed * Time.deltaTime);
    }
}