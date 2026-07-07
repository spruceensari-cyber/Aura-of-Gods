using UnityEngine;

public class TowerAttack : MonoBehaviour
{
    public float attackRange = 22f;
    public float attackDamage = 30f;
    public float attackRate = 1.2f;

    [Header("Targeting")]
    public bool prioritizeMinions = true;
    public bool attackEnemyHeroes = true;
    public float heroTargetHeight = 1.5f;
    public float minionTargetHeight = 1.2f;

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

        Transform target = FindPriorityTarget(out float targetHeight);

        if (target == null)
            return;

        nextAttackTime = Time.time + attackRate;

        ShootProjectile(target, targetHeight);

        Debug.Log(name + " tower bolt -> " + target.name);
    }

    void AutoAssignTeam()
    {
        string n = gameObject.name.ToLower();

        if (n.StartsWith("blue_"))
            towerHealth.towerTeam = MinionTeam.Blue;
        else if (n.StartsWith("red_"))
            towerHealth.towerTeam = MinionTeam.Red;
    }

    Transform FindPriorityTarget(out float targetHeight)
    {
        targetHeight = minionTargetHeight;

        if (prioritizeMinions)
        {
            Minion minion = FindClosestEnemyMinion();

            if (minion != null)
                return minion.transform;
        }

        if (attackEnemyHeroes)
        {
            AOGCharacterStats hero = FindClosestEnemyHero();

            if (hero != null)
            {
                targetHeight = heroTargetHeight;
                return hero.transform;
            }
        }

        if (!prioritizeMinions)
        {
            Minion minion = FindClosestEnemyMinion();

            if (minion != null)
            {
                targetHeight = minionTargetHeight;
                return minion.transform;
            }
        }

        return null;
    }

    Minion FindClosestEnemyMinion()
    {
        Minion[] minions = FindObjectsByType<Minion>(FindObjectsSortMode.None);

        Minion closest = null;
        float closestDistance = Mathf.Infinity;

        foreach (Minion minion in minions)
        {
            if (minion == null)
                continue;

            if (!minion.gameObject.activeInHierarchy)
                continue;

            if (minion.hp <= 0f)
                continue;

            if (minion.team == towerHealth.towerTeam)
                continue;

            float distance = FlatDistance(transform.position, minion.transform.position);

            if (distance <= attackRange && distance < closestDistance)
            {
                closest = minion;
                closestDistance = distance;
            }
        }

        return closest;
    }

    AOGCharacterStats FindClosestEnemyHero()
    {
        AOGCharacterStats[] heroes = FindObjectsByType<AOGCharacterStats>(FindObjectsSortMode.None);

        AOGCharacterStats closest = null;
        float closestDistance = Mathf.Infinity;

        foreach (AOGCharacterStats hero in heroes)
        {
            if (hero == null)
                continue;

            if (!hero.gameObject.activeInHierarchy)
                continue;

            if (hero.IsDead)
                continue;

            if (hero.team == towerHealth.towerTeam)
                continue;

            float distance = FlatDistance(transform.position, hero.transform.position);

            if (distance <= attackRange && distance < closestDistance)
            {
                closest = hero;
                closestDistance = distance;
            }
        }

        return closest;
    }

    void ShootProjectile(Transform target, float targetHeight)
    {
        Vector3 spawnPos = transform.position + Vector3.up * shootHeight;

        GameObject projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        projectile.name = "Tower_Energy_Bolt";

        projectile.transform.position = spawnPos;
        projectile.transform.localScale = Vector3.one * projectileSize;

        Renderer renderer = projectile.GetComponent<Renderer>();

        if (renderer != null)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            Color color = GetProjectileColor();

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
        bolt.targetHeight = targetHeight;
        bolt.damage = attackDamage;
        bolt.speed = projectileSpeed;
    }

    Color GetProjectileColor()
    {
        if (towerHealth != null && towerHealth.towerTeam == MinionTeam.Blue)
            return new Color(0.2f, 0.7f, 1f);

        if (towerHealth != null && towerHealth.towerTeam == MinionTeam.Red)
            return new Color(1f, 0.1f, 0.05f);

        return Color.red;
    }

    float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
