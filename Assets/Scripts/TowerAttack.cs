using UnityEngine;

public class TowerAttack : MonoBehaviour
{
    public float attackRange = 22f;
    public float attackDamage = 30f;
    public float attackRate = 1.2f;
    public float championDamageRamp = 0.22f;
    public int maxChampionRampStacks = 4;

    [Header("Projectile Visual")]
    public float projectileSize = 0.35f;
    public float projectileSpeed = 20f;
    public float shootHeight = 6.5f;

    private TowerHealth towerHealth;
    private float nextAttackTime;
    private Minion lockedMinion;
    private Champion lockedChampion;
    private int championRampStacks;

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
        if (towerHealth == null || towerHealth.hp <= 0f || Time.time < nextAttackTime)
            return;

        ValidateLocks();

        if (lockedMinion == null)
            lockedMinion = FindClosestEnemyMinion();

        if (lockedMinion != null)
        {
            championRampStacks = 0;
            nextAttackTime = Time.time + attackRate;
            ShootProjectile(lockedMinion, null, attackDamage);
            return;
        }

        if (lockedChampion == null)
            lockedChampion = FindClosestEnemyChampion();

        if (lockedChampion != null)
        {
            championRampStacks = Mathf.Min(championRampStacks + 1, maxChampionRampStacks);
            float rampedDamage = attackDamage * (1f + championDamageRamp * (championRampStacks - 1));
            nextAttackTime = Time.time + attackRate;
            ShootProjectile(null, lockedChampion, rampedDamage);
        }
    }

    void AutoAssignTeam()
    {
        string n = gameObject.name.ToLower();
        if (n.StartsWith("blue_")) towerHealth.towerTeam = MinionTeam.Blue;
        else if (n.StartsWith("red_")) towerHealth.towerTeam = MinionTeam.Red;
    }

    void ValidateLocks()
    {
        if (lockedMinion != null)
        {
            if (!lockedMinion.gameObject.activeInHierarchy || lockedMinion.hp <= 0f || lockedMinion.team == towerHealth.towerTeam || FlatDistance(transform.position, lockedMinion.transform.position) > attackRange)
                lockedMinion = null;
        }

        if (lockedChampion != null)
        {
            if (!lockedChampion.IsAlive || ToMinionTeam(lockedChampion.Team) == towerHealth.towerTeam || FlatDistance(transform.position, lockedChampion.transform.position) > attackRange)
            {
                lockedChampion = null;
                championRampStacks = 0;
            }
        }
    }

    Minion FindClosestEnemyMinion()
    {
        Minion closest = null;
        float closestDistance = Mathf.Infinity;
        foreach (Minion m in FindObjectsByType<Minion>(FindObjectsSortMode.None))
        {
            if (m == null || !m.gameObject.activeInHierarchy || m.hp <= 0f || m.team == towerHealth.towerTeam)
                continue;

            float d = FlatDistance(transform.position, m.transform.position);
            if (d <= attackRange && d < closestDistance)
            {
                closest = m;
                closestDistance = d;
            }
        }
        return closest;
    }

    Champion FindClosestEnemyChampion()
    {
        Champion closest = null;
        float closestDistance = Mathf.Infinity;
        foreach (Champion champion in FindObjectsByType<Champion>(FindObjectsSortMode.None))
        {
            if (champion == null || !champion.IsAlive)
                continue;
            if (ToMinionTeam(champion.Team) == towerHealth.towerTeam)
                continue;

            float d = FlatDistance(transform.position, champion.transform.position);
            if (d <= attackRange && d < closestDistance)
            {
                closest = champion;
                closestDistance = d;
            }
        }
        return closest;
    }

    void ShootProjectile(Minion minionTarget, Champion championTarget, float damage)
    {
        Vector3 spawnPos = transform.position + Vector3.up * shootHeight;
        AOGObjectPoolRuntime pool = AOGObjectPoolRuntime.Instance;
        GameObject projectile = pool != null
            ? pool.Get("TowerBolt", CreateTowerBoltObject, spawnPos, Quaternion.identity)
            : CreateTowerBoltObject();

        projectile.transform.position = spawnPos;
        projectile.transform.localScale = Vector3.one * projectileSize;

        Renderer renderer = projectile.GetComponent<Renderer>();
        if (renderer != null)
        {
            Color color = towerHealth.towerTeam == MinionTeam.Blue ? new Color(0.2f, 0.7f, 1f) : new Color(1f, 0.1f, 0.05f);
            Material mat = renderer.material;
            mat.color = color;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", color * 2.5f);
            }
        }

        TowerBolt bolt = projectile.GetComponent<TowerBolt>();
        bolt.minionTarget = minionTarget;
        bolt.championTarget = championTarget;
        bolt.damage = damage;
        bolt.speed = projectileSpeed;

        AOGAudioDirectorRuntime.Instance?.PlayCue(AOGAudioCue.TowerShot, spawnPos);
    }

    private GameObject CreateTowerBoltObject()
    {
        GameObject projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        projectile.name = "Tower_Energy_Bolt";

        Renderer renderer = projectile.GetComponent<Renderer>();
        if (renderer != null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            renderer.material = new Material(shader);
        }

        Collider col = projectile.GetComponent<Collider>();
        if (col != null) Destroy(col);

        if (projectile.GetComponent<TowerBolt>() == null)
            projectile.AddComponent<TowerBolt>();
        return projectile;
    }

    static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    static MinionTeam ToMinionTeam(TeamType team)
    {
        return team == TeamType.Red ? MinionTeam.Red : MinionTeam.Blue;
    }
}
