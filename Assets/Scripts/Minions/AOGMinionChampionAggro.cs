using System.Collections;
using UnityEngine;

[DefaultExecutionOrder(40)]
public class AOGMinionChampionAggro : MonoBehaviour
{
    public float heroAggroRange = 6.5f;
    public float disengageRange = 9f;
    public float attackRangePadding = 0.45f;

    private Minion minion;
    private AOGCharacterStats targetHero;
    private float originalSpeed;
    private float nextAttack;
    private Coroutine attackRoutine;
    private AOGMinionProceduralAnimator animator;

    private void Awake()
    {
        minion = GetComponent<Minion>();
        if (minion != null) originalSpeed = minion.speed;
        animator = GetComponent<AOGMinionProceduralAnimator>();
    }

    private void Start()
    {
        if (minion != null) originalSpeed = minion.speed;
    }

    private void Update()
    {
        if (minion == null || minion.hp <= 0f)
            return;

        if (targetHero == null || targetHero.IsDead || !targetHero.gameObject.activeInHierarchy || FlatDistance(transform.position, targetHero.transform.position) > disengageRange)
            targetHero = FindEnemyHero();

        if (targetHero == null)
        {
            RestoreSpeed();
            return;
        }

        float distance = FlatDistance(transform.position, targetHero.transform.position);
        float range = minion.role == MinionRole.Melee ? Mathf.Max(1.8f, minion.attackRange) : Mathf.Max(4.8f, minion.attackRange);

        if (distance > range + attackRangePadding)
        {
            minion.speed = originalSpeed;
            Vector3 dir = targetHero.transform.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.01f)
            {
                transform.position += dir.normalized * originalSpeed * 0.82f * Time.deltaTime;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir.normalized), 9f * Time.deltaTime);
            }
            return;
        }

        minion.speed = 0f;
        Face(targetHero.transform.position);
        if (Time.time >= nextAttack && attackRoutine == null)
        {
            nextAttack = Time.time + minion.attackRate;
            attackRoutine = StartCoroutine(AttackHero(targetHero));
        }
    }

    private AOGCharacterStats FindEnemyHero()
    {
        AOGCharacterStats best = null;
        float bestDistance = float.MaxValue;
        foreach (AOGCharacterStats hero in FindObjectsByType<AOGCharacterStats>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (hero == null || hero.IsDead || hero.team == minion.team)
                continue;

            float d = FlatDistance(transform.position, hero.transform.position);
            if (d <= heroAggroRange && d < bestDistance)
            {
                best = hero;
                bestDistance = d;
            }
        }
        return best;
    }

    private IEnumerator AttackHero(AOGCharacterStats locked)
    {
        animator = animator != null ? animator : GetComponent<AOGMinionProceduralAnimator>();
        animator?.PlayAttack();
        float windup = minion.role == MinionRole.Cannon ? 0.48f : minion.role == MinionRole.Ranged ? 0.34f : 0.26f;
        yield return new WaitForSeconds(windup);

        if (locked != null && !locked.IsDead)
        {
            float allowed = minion.role == MinionRole.Melee ? Mathf.Max(2.4f, minion.attackRange + 0.6f) : Mathf.Max(5.4f, minion.attackRange + 0.7f);
            if (FlatDistance(transform.position, locked.transform.position) <= allowed)
            {
                if (minion.role == MinionRole.Melee)
                {
                    locked.TakeDamage(minion.damage);
                    GameObject ring = AOGAbilityVisuals.CreateRing("Minion_Hero_Hit", locked.transform.position + Vector3.up * 0.1f, 0.62f, TeamColor(), 0.06f);
                    Destroy(ring, 0.28f);
                }
                else
                {
                    LaunchProjectile(locked);
                }
            }
        }

        attackRoutine = null;
    }

    private void LaunchProjectile(AOGCharacterStats hero)
    {
        GameObject projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        projectile.name = minion.team + "_Hero_Aggro_Projectile";
        projectile.transform.position = transform.position + Vector3.up * 1.35f + transform.forward * 0.45f;
        projectile.transform.localScale = Vector3.one * (minion.role == MinionRole.Cannon ? 0.42f : 0.24f);
        Collider col = projectile.GetComponent<Collider>();
        if (col != null) Destroy(col);

        Color c = TeamColor();
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        Material mat = new Material(shader) { color = c };
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
        if (mat.HasProperty("_EmissionColor")) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", c * 4f); }
        projectile.GetComponent<Renderer>().sharedMaterial = mat;

        TrailRenderer trail = projectile.AddComponent<TrailRenderer>();
        trail.time = 0.28f;
        trail.startWidth = minion.role == MinionRole.Cannon ? 0.38f : 0.20f;
        trail.endWidth = 0f;
        trail.sharedMaterial = mat;
        trail.startColor = c;
        trail.endColor = new Color(c.r, c.g, c.b, 0f);

        AOGHeroSeekingProjectile seeker = projectile.AddComponent<AOGHeroSeekingProjectile>();
        seeker.target = hero;
        seeker.damage = minion.damage;
        seeker.speed = minion.role == MinionRole.Cannon ? 14f : 18f;
        seeker.color = c;
    }

    private void RestoreSpeed()
    {
        if (minion != null && minion.speed <= 0.01f)
            minion.speed = originalSpeed;
    }

    private void Face(Vector3 point)
    {
        Vector3 d = point - transform.position;
        d.y = 0f;
        if (d.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d.normalized), 10f * Time.deltaTime);
    }

    private Color TeamColor()
    {
        return minion.team == MinionTeam.Blue ? new Color(0.16f, 0.58f, 1f) : new Color(1f, 0.18f, 0.22f);
    }

    private static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f; b.y = 0f; return Vector3.Distance(a, b);
    }
}

public class AOGHeroSeekingProjectile : MonoBehaviour
{
    public AOGCharacterStats target;
    public float damage;
    public float speed = 18f;
    public Color color;

    private void Start() { Destroy(gameObject, 4f); }

    private void Update()
    {
        if (target == null || target.IsDead) { Destroy(gameObject); return; }
        Vector3 point = target.transform.position + Vector3.up * 1.1f;
        Vector3 delta = point - transform.position;
        if (delta.magnitude <= Mathf.Max(0.25f, speed * Time.deltaTime))
        {
            target.TakeDamage(damage);
            GameObject ring = AOGAbilityVisuals.CreateRing("Ranged_Hero_Hit", point, 0.62f, color, 0.06f);
            Destroy(ring, 0.28f);
            Destroy(gameObject);
            return;
        }
        transform.position += delta.normalized * speed * Time.deltaTime;
        transform.rotation = Quaternion.LookRotation(delta.normalized);
    }
}

public class AOGMinionChampionAggroBootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        GameObject host = new GameObject("AOG_Minion_Champion_Aggro_Bootstrap");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGMinionChampionAggroBootstrap>();
    }

    private void Update()
    {
        foreach (Minion minion in Minion.Active)
        {
            if (minion != null && minion.GetComponent<AOGMinionChampionAggro>() == null)
                minion.gameObject.AddComponent<AOGMinionChampionAggro>();
        }
    }
}
