using UnityEngine;

public class AOGTowerBeamAttack : MonoBehaviour
{
    public float attackRange = 8f;
    public float attackCooldown = 1.2f;
    public float damage = 28f;
    public float beamDuration = 0.12f;

    public Vector3 beamOriginOffset = new Vector3(0, 4f, 0);

    private AOGCombatUnit unit;
    private LineRenderer line;
    private float nextAttackTime;
    private float beamHideTime;

    void Awake()
    {
        unit = GetComponent<AOGCombatUnit>();
        CreateLine();
    }

    void Update()
    {
        if (line != null && Time.time > beamHideTime)
            line.enabled = false;

        if (Time.time < nextAttackTime)
            return;

        AOGDamageable target = FindNearestEnemy();

        if (target != null)
        {
            nextAttackTime = Time.time + attackCooldown;
            FireBeam(target);
        }
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

            if (otherUnit.unitType == AOGUnitType.Nexus)
                continue;

            float dist = Vector3.Distance(transform.position, d.transform.position);

            if (dist <= attackRange && dist < bestDist)
            {
                best = d;
                bestDist = dist;
            }
        }

        return best;
    }

    void FireBeam(AOGDamageable target)
    {
        if (target == null)
            return;

        target.TakeDamage(damage);

        if (line != null)
        {
            Vector3 start = transform.position + beamOriginOffset;
            Vector3 end = target.transform.position + Vector3.up * 1.2f;

            line.SetPosition(0, start);
            line.SetPosition(1, end);
            line.enabled = true;
            beamHideTime = Time.time + beamDuration;
        }
    }

    void CreateLine()
    {
        line = gameObject.AddComponent<LineRenderer>();
        line.positionCount = 2;
        line.startWidth = 0.12f;
        line.endWidth = 0.04f;
        line.enabled = false;

        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = Color.cyan;
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", Color.cyan * 2f);

        line.material = mat;
    }
}