using UnityEngine;
using System.Collections.Generic;

public class LyraSkillSet : MonoBehaviour
{
    [Header("Team")]
    public MinionTeam team = MinionTeam.Blue;

    [Header("References")]
    public Animator animator;

    [Header("Cooldowns")]
    public float qCooldown = 4f;
    public float wCooldown = 8f;
    public float eCooldown = 10f;
    public float rCooldown = 35f;

    [Header("Q - Neon Dagger")]
    public float qRange = 12f;
    public float qDamage = 90f;
    public float qProjectileSpeed = 22f;

    [Header("W - Vanish Step")]
    public float wDashDistance = 6f;
    public float wBuffDuration = 2.5f;
    public float wBonusDamage = 35f;
    [Range(0.05f, 1f)]
    public float vanishedAlpha = 0.28f;

    [Header("E - Hunter's Net")]
    public float eRange = 10f;
    public float eDamage = 45f;
    public float eSlowDuration = 2f;

    [Header("R - Blood Moon Execution")]
    public float rRange = 9f;
    public float rDamage = 220f;
    public float executeThreshold = 0.35f;

    [Header("Visuals")]
    public Color lyraColor = new Color(1f, 0.05f, 0.65f);
    public float visualLife = 0.6f;

    private float nextQ;
    private float nextW;
    private float nextE;
    private float nextR;

    private bool vanished;
    private float vanishEndTime;
    private bool nextAttackEmpowered;

    private Dictionary<Renderer, Color[]> originalRendererColors = new Dictionary<Renderer, Color[]>();

    public bool IsVanished
    {
        get { return vanished; }
    }

    void Start()
    {
        FindValidAnimator();
    }

    void Update()
    {
        if (vanished && Time.time >= vanishEndTime)
        {
            vanished = false;
            RestoreRendererOpacity();
        }

        if (Input.GetKeyDown(KeyCode.Q))
            CastQ();

        if (Input.GetKeyDown(KeyCode.W))
            CastW();

        if (Input.GetKeyDown(KeyCode.E))
            CastE();

        if (Input.GetKeyDown(KeyCode.R))
            CastR();
    }

    void CastQ()
    {
        if (Time.time < nextQ)
        {
            Debug.Log("Q cooldown.");
            return;
        }

        Minion target = FindClosestEnemyMinion(qRange);

        if (target == null)
        {
            Debug.Log("Q için hedef yok.");
            return;
        }

        nextQ = Time.time + qCooldown;

        PlayAnim("SkillQ");

        CreateProjectile(target, qDamage, qProjectileSpeed, "Lyra_Q_Neon_Dagger");

        Debug.Log("Lyra Q kullandı: Neon Dagger -> " + target.name);
    }

    void CastW()
    {
        if (Time.time < nextW)
        {
            Debug.Log("W cooldown.");
            return;
        }

        nextW = Time.time + wCooldown;

        PlayAnim("SkillW");

        transform.position += transform.forward * wDashDistance;

        vanished = true;
        vanishEndTime = Time.time + wBuffDuration;
        nextAttackEmpowered = true;

        SetRenderersTransparent(vanishedAlpha);
        CreateCircleVisual(transform.position, 2.2f, "Lyra_W_Vanish");

        Debug.Log("Lyra W kullandı: Vanish Step. Lyra saydam oldu.");
    }

    void CastE()
    {
        if (Time.time < nextE)
        {
            Debug.Log("E cooldown.");
            return;
        }

        Minion target = FindClosestEnemyMinion(eRange);

        if (target == null)
        {
            Debug.Log("E için hedef yok.");
            return;
        }

        nextE = Time.time + eCooldown;

        PlayAnim("SkillE");

        target.TakeDamage(eDamage, gameObject);
        target.speed *= 0.45f;

        StartCoroutine(RemoveSlow(target, eSlowDuration));

        CreateCircleVisual(target.transform.position, 2.8f, "Lyra_E_Hunters_Net");

        Debug.Log("Lyra E kullandı: Hunter's Net -> " + target.name);
    }

    void CastR()
    {
        if (Time.time < nextR)
        {
            Debug.Log("R cooldown.");
            return;
        }

        Minion target = FindClosestEnemyMinion(rRange);

        if (target == null)
        {
            Debug.Log("R için hedef yok.");
            return;
        }

        nextR = Time.time + rCooldown;

        PlayAnim("SkillR");

        float finalDamage = rDamage;

        if (target.hp <= target.maxHp * executeThreshold)
            finalDamage = target.hp + 999f;

        Vector3 behindTarget = target.transform.position - target.transform.forward * 1.7f;
        behindTarget.y = transform.position.y;

        transform.position = behindTarget;
        FaceTarget(target.transform.position);

        target.TakeDamage(finalDamage, gameObject);

        CreateCircleVisual(target.transform.position, 4f, "Lyra_R_Blood_Moon");
        CreateProjectile(target, 0f, 35f, "Lyra_R_Blood_Slash");

        Debug.Log("Lyra R kullandı: Blood Moon Execution -> " + target.name);
    }

    public void EmpoweredBasicAttack(Minion target)
    {
        if (target == null)
            return;

        if (nextAttackEmpowered)
        {
            target.TakeDamage(wBonusDamage, gameObject);
            nextAttackEmpowered = false;

            Debug.Log("Lyra W empowered attack bonus damage verdi.");
        }
    }

    Minion FindClosestEnemyMinion(float range)
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

            if (m.team == team)
                continue;

            float distance = FlatDistance(transform.position, m.transform.position);

            if (distance <= range && distance < closestDistance)
            {
                closest = m;
                closestDistance = distance;
            }
        }

        return closest;
    }

    void CreateProjectile(Minion target, float damage, float speed, string projectileName)
    {
        if (target == null)
            return;

        GameObject projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        projectile.name = projectileName;

        projectile.transform.position = transform.position + Vector3.up * 1.4f + transform.forward * 0.8f;
        projectile.transform.localScale = Vector3.one * 0.35f;

        Renderer renderer = projectile.GetComponent<Renderer>();

        if (renderer != null)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));

            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", lyraColor);

            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", lyraColor * 3f);
            }

            renderer.material = mat;
        }

        Collider col = projectile.GetComponent<Collider>();

        if (col != null)
            Destroy(col);

        LyraProjectile p = projectile.AddComponent<LyraProjectile>();
        p.target = target;
        p.damage = damage;
        p.speed = speed;
        p.color = lyraColor;
    }

    void CreateCircleVisual(Vector3 position, float size, string visualName)
    {
        GameObject circle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        circle.name = visualName;

        circle.transform.position = position + Vector3.up * 0.05f;
        circle.transform.localScale = new Vector3(size, 0.03f, size);

        Renderer renderer = circle.GetComponent<Renderer>();

        if (renderer != null)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));

            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", lyraColor);

            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", lyraColor * 2.5f);
            }

            renderer.material = mat;
        }

        Collider col = circle.GetComponent<Collider>();

        if (col != null)
            Destroy(col);

        Destroy(circle, visualLife);
    }

    System.Collections.IEnumerator RemoveSlow(Minion target, float delay)
    {
        if (target == null)
            yield break;

        float originalSpeed = target.speed / 0.45f;

        yield return new WaitForSeconds(delay);

        if (target != null)
            target.speed = originalSpeed;
    }

    void SetRenderersTransparent(float alpha)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();

        foreach (Renderer r in renderers)
        {
            if (r == null)
                continue;

            if (r.gameObject.name.Contains("AOG_HP_Bar"))
                continue;

            Material[] mats = r.materials;

            if (!originalRendererColors.ContainsKey(r))
            {
                Color[] colors = new Color[mats.Length];

                for (int i = 0; i < mats.Length; i++)
                    colors[i] = mats[i].color;

                originalRendererColors.Add(r, colors);
            }

            for (int i = 0; i < mats.Length; i++)
            {
                Material mat = mats[i];

                Color c = mat.color;
                c.a = alpha;
                mat.color = c;

                if (mat.HasProperty("_BaseColor"))
                {
                    Color baseColor = mat.GetColor("_BaseColor");
                    baseColor.a = alpha;
                    mat.SetColor("_BaseColor", baseColor);
                }

                if (mat.HasProperty("_Surface"))
                    mat.SetFloat("_Surface", 1f);

                if (mat.HasProperty("_AlphaClip"))
                    mat.SetFloat("_AlphaClip", 0f);

                mat.renderQueue = 3000;
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.EnableKeyword("_ALPHABLEND_ON");
            }

            r.enabled = true;
        }
    }

    void RestoreRendererOpacity()
    {
        foreach (var pair in originalRendererColors)
        {
            Renderer r = pair.Key;

            if (r == null)
                continue;

            Material[] mats = r.materials;
            Color[] originalColors = pair.Value;

            for (int i = 0; i < mats.Length && i < originalColors.Length; i++)
            {
                Color c = originalColors[i];
                c.a = 1f;

                mats[i].color = c;

                if (mats[i].HasProperty("_BaseColor"))
                    mats[i].SetColor("_BaseColor", c);

                if (mats[i].HasProperty("_Surface"))
                    mats[i].SetFloat("_Surface", 0f);

                mats[i].renderQueue = 2000;
            }

            r.enabled = true;
        }
    }

    void FaceTarget(Vector3 target)
    {
        Vector3 direction = target - transform.position;
        direction.y = 0f;

        if (direction == Vector3.zero)
            return;

        transform.rotation = Quaternion.LookRotation(direction);
    }

    float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    void FindValidAnimator()
    {
        if (animator != null && animator.enabled && animator.runtimeAnimatorController != null)
            return;

        Animator[] animators = GetComponentsInChildren<Animator>();

        foreach (Animator a in animators)
        {
            if (a != null && a.enabled && a.runtimeAnimatorController != null)
            {
                animator = a;
                return;
            }
        }
    }

    void PlayAnim(string triggerName)
    {
        FindValidAnimator();

        if (animator != null)
        {
            animator.ResetTrigger("Attack");
            animator.ResetTrigger("SkillQ");
            animator.ResetTrigger("SkillW");
            animator.ResetTrigger("SkillE");
            animator.ResetTrigger("SkillR");

            animator.SetTrigger(triggerName);

            Debug.Log("Lyra animasyon tetiklendi: " + triggerName + " -> " + animator.gameObject.name);
        }
        else
        {
            Debug.LogWarning("Lyra animator bulunamadı: " + triggerName);
        }
    }

    public float GetQCooldownRatio()
    {
        return Mathf.Clamp01((nextQ - Time.time) / qCooldown);
    }

    public float GetWCooldownRatio()
    {
        return Mathf.Clamp01((nextW - Time.time) / wCooldown);
    }

    public float GetECooldownRatio()
    {
        return Mathf.Clamp01((nextE - Time.time) / eCooldown);
    }

    public float GetRCooldownRatio()
    {
        return Mathf.Clamp01((nextR - Time.time) / rCooldown);
    }
}