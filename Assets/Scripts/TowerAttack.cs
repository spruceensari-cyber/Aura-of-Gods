using UnityEngine;

public class TowerAttack : MonoBehaviour
{
    public float attackRange = 22f;
    public float attackDamage = 30f;
    public float attackRate = 1.2f;

    [Header("Projectile Visual")]
    public float projectileSize = 0.35f;
    public float projectileSpeed = 20f;
    public float shootHeight = 6.5f;

    private TowerHealth towerHealth;
    private float nextAttackTime;

    void Start()
    {
        towerHealth = GetComponent<TowerHealth>();

        if (towerHealth == null)
        {
            towerHealth = gameObject.AddComponent<TowerHealth>();
            AutoAssignTeam();
        }
    }

    void Update()
    {
        if (towerHealth == null)
            towerHealth = GetComponent<TowerHealth>();

        if (towerHealth == null)
            return;

        if (towerHealth.hp <= 0f)
            return;

        if (Time.time < nextAttackTime)
            return;

        Minion target = FindClosestEnemyMinion();

        if (target == null)
            return;

        nextAttackTime = Time.time + attackRate;

        ShootProjectile(target);

        Debug.Log(name + " küçük kule mermisi attı -> " + target.name);
    }

    void AutoAssignTeam()
    {
        string n = gameObject.name.ToLower();

        if (n.StartsWith("blue_"))
            towerHealth.towerTeam = MinionTeam.Blue;
        else if (n.StartsWith("red_"))
            towerHealth.towerTeam = MinionTeam.Red;
    }

    Minion FindClosestEnemyMinion()
    {
        Minion[] minions = FindObjectsByType<Minion>(FindObjectsSortMode.None);

        Minion closest = null;
        float closestDistance = Mathf.Infinity;

        foreach (Minion m in minions)
        {
            if (m == null)
                continue;

            if (!m.gameObject.activeInHierarchy)
                continue;

            if (m.hp <= 0f)
                continue;

            if (m.team == towerHealth.towerTeam)
                continue;

            Vector3 towerPos = transform.position;
            Vector3 minionPos = m.transform.position;

            towerPos.y = 0f;
            minionPos.y = 0f;

            float distance = Vector3.Distance(towerPos, minionPos);

            if (distance <= attackRange && distance < closestDistance)
            {
                closest = m;
                closestDistance = distance;
            }
        }

        return closest;
    }

    void ShootProjectile(Minion target)
    {
        Vector3 spawnPos = transform.position + Vector3.up * shootHeight;

        GameObject projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        projectile.name = "Tower_Red_Energy_Bolt";

        projectile.transform.position = spawnPos;
        projectile.transform.localScale = Vector3.one * projectileSize;

        Renderer renderer = projectile.GetComponent<Renderer>();

        if (renderer != null)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));

            Color color = Color.red;

            if (towerHealth != null && towerHealth.towerTeam == MinionTeam.Blue)
                color = new Color(0.2f, 0.7f, 1f);

            if (towerHealth != null && towerHealth.towerTeam == MinionTeam.Red)
                color = new Color(1f, 0.1f, 0.05f);

            mat.color = color;

            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);

            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", color * 2.5f);
            }

            renderer.material = mat;
        }

        Collider col = projectile.GetComponent<Collider>();
        if (col != null)
            Destroy(col);

        TowerBolt bolt = projectile.AddComponent<TowerBolt>();
        bolt.target = target;
        bolt.damage = attackDamage;
        bolt.speed = projectileSpeed;
    }
}