using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum AOGNeutralBossType
{
    Dragon,
    Medusa
}

public class AOGNeutralBossAI : MonoBehaviour
{
    public AOGNeutralBossType bossType = AOGNeutralBossType.Dragon;
    public float maxHp = 5000f;
    public float hp = 5000f;
    public float detectionRange = 13f;
    public float attackRange = 4.8f;
    public float leashRange = 16f;
    public float moveSpeed = 3.4f;
    public float attackDamage = 85f;
    public float attackCooldown = 2.2f;

    public bool IsDead => hp <= 0f;

    private Vector3 homePosition;
    private Quaternion homeRotation;
    private float nextAttackTime;
    private float idleTime;
    private Transform currentTarget;
    private Transform visualRoot;
    private Vector3 visualBaseLocalPosition;
    private Quaternion visualBaseRotation;
    private Coroutine attackRoutine;
    private Material energyMaterial;

    private void Start()
    {
        hp = hp <= 0f ? maxHp : Mathf.Clamp(hp, 0f, maxHp);
        homePosition = transform.position;
        homeRotation = transform.rotation;
        visualRoot = FindVisualRoot();
        if (visualRoot != null)
        {
            visualBaseLocalPosition = visualRoot.localPosition;
            visualBaseRotation = visualRoot.localRotation;
        }

        BuildBossAura();
    }

    private void Update()
    {
        if (IsDead)
            return;

        idleTime += Time.deltaTime;
        AnimateIdle();

        if (currentTarget == null || !IsValidTarget(currentTarget) || Vector3.Distance(homePosition, currentTarget.position) > leashRange * 1.35f)
            currentTarget = FindTarget();

        if (currentTarget == null)
        {
            ReturnHome();
            return;
        }

        float distanceFromHome = Vector3.Distance(transform.position, homePosition);
        if (distanceFromHome > leashRange)
        {
            currentTarget = null;
            ReturnHome();
            return;
        }

        float distance = FlatDistance(transform.position, currentTarget.position);
        FaceTarget(currentTarget.position);

        if (distance > attackRange)
        {
            MoveToward(currentTarget.position);
            return;
        }

        if (Time.time >= nextAttackTime && attackRoutine == null)
        {
            nextAttackTime = Time.time + attackCooldown;
            attackRoutine = StartCoroutine(AttackSequence());
        }
    }

    public void TakeDamage(float amount, GameObject attacker)
    {
        if (IsDead || amount <= 0f)
            return;

        hp = Mathf.Clamp(hp - amount, 0f, maxHp);
        if (attacker != null)
            currentTarget = attacker.transform;

        if (hp <= 0f)
            StartCoroutine(DeathSequence(attacker));
    }

    private IEnumerator AttackSequence()
    {
        Transform lockedTarget = currentTarget;
        if (lockedTarget == null)
        {
            attackRoutine = null;
            yield break;
        }

        Vector3 startScale = visualRoot != null ? visualRoot.localScale : Vector3.one;
        float windup = bossType == AOGNeutralBossType.Dragon ? 0.55f : 0.42f;

        for (float t = 0f; t < windup; t += Time.deltaTime)
        {
            float k = t / windup;
            if (visualRoot != null)
                visualRoot.localScale = Vector3.Lerp(startScale, startScale * 1.08f, k);
            yield return null;
        }

        if (lockedTarget != null && FlatDistance(transform.position, lockedTarget.position) <= attackRange + 2f)
        {
            if (bossType == AOGNeutralBossType.Dragon)
                DragonBreath(lockedTarget.position);
            else
                MedusaGaze(lockedTarget.position);
        }

        if (visualRoot != null)
            visualRoot.localScale = startScale;

        yield return new WaitForSeconds(0.25f);
        attackRoutine = null;
    }

    private void DragonBreath(Vector3 targetPoint)
    {
        Vector3 forward = targetPoint - transform.position;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.01f)
            forward = transform.forward;
        forward.Normalize();

        CreateConeTelegraph(forward, new Color(1f, 0.27f, 0.06f, 0.76f), 7.5f, 55f);

        Collider[] hits = Physics.OverlapSphere(transform.position + forward * 3.2f, 5.5f, ~0, QueryTriggerInteraction.Ignore);
        foreach (Collider hit in hits)
        {
            if (hit == null)
                continue;

            Vector3 toTarget = hit.transform.position - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 0.01f)
                continue;

            if (Vector3.Angle(forward, toTarget.normalized) > 32f)
                continue;

            DamageColliderTarget(hit, attackDamage);
        }

        AOGMobaCameraController camera = Camera.main != null ? Camera.main.GetComponent<AOGMobaCameraController>() : null;
        camera?.AddRandomImpulse(0.34f);
    }

    private void MedusaGaze(Vector3 targetPoint)
    {
        CreateRadialTelegraph(new Color(0.46f, 0.18f, 0.76f, 0.78f), 5.8f);

        Collider[] hits = Physics.OverlapSphere(transform.position, 5.8f, ~0, QueryTriggerInteraction.Ignore);
        foreach (Collider hit in hits)
            DamageColliderTarget(hit, attackDamage * 0.82f);

        AOGMobaCameraController camera = Camera.main != null ? Camera.main.GetComponent<AOGMobaCameraController>() : null;
        camera?.AddRandomImpulse(0.25f);
    }

    private void DamageColliderTarget(Collider hit, float damage)
    {
        AOGCharacterStats hero = hit.GetComponentInParent<AOGCharacterStats>();
        if (hero != null)
        {
            hero.TakeDamage(damage);
            return;
        }

        Minion minion = hit.GetComponentInParent<Minion>();
        if (minion != null)
            minion.TakeDamage(damage, gameObject);
    }

    private void CreateConeTelegraph(Vector3 forward, Color color, float length, float angle)
    {
        GameObject telegraph = new GameObject(bossType + "_Attack_Telegraph");
        telegraph.transform.position = transform.position + Vector3.up * 0.12f;
        telegraph.transform.rotation = Quaternion.LookRotation(forward);

        LineRenderer line = telegraph.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.positionCount = 18;
        line.loop = true;
        line.startWidth = 0.09f;
        line.endWidth = 0.09f;
        line.sharedMaterial = BuildUnlit(color);

        float half = angle * 0.5f * Mathf.Deg2Rad;
        line.SetPosition(0, Vector3.zero);
        for (int i = 0; i <= 15; i++)
        {
            float a = Mathf.Lerp(-half, half, i / 15f);
            line.SetPosition(i + 1, new Vector3(Mathf.Sin(a) * length, 0f, Mathf.Cos(a) * length));
        }
        line.SetPosition(17, Vector3.zero);

        AOGTelegraphFade fade = telegraph.AddComponent<AOGTelegraphFade>();
        fade.life = 0.65f;
        fade.line = line;
    }

    private void CreateRadialTelegraph(Color color, float radius)
    {
        GameObject telegraph = new GameObject(bossType + "_Radial_Telegraph");
        telegraph.transform.position = transform.position + Vector3.up * 0.12f;

        LineRenderer line = telegraph.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.loop = true;
        line.positionCount = 64;
        line.startWidth = 0.12f;
        line.endWidth = 0.12f;
        line.sharedMaterial = BuildUnlit(color);

        for (int i = 0; i < line.positionCount; i++)
        {
            float a = i * Mathf.PI * 2f / line.positionCount;
            line.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius));
        }

        AOGTelegraphFade fade = telegraph.AddComponent<AOGTelegraphFade>();
        fade.life = 0.72f;
        fade.line = line;
    }

    private void AnimateIdle()
    {
        if (visualRoot == null || attackRoutine != null)
            return;

        float hover = Mathf.Sin(idleTime * (bossType == AOGNeutralBossType.Dragon ? 1.8f : 1.35f)) * 0.16f;
        float yaw = Mathf.Sin(idleTime * 0.65f) * 7f;
        visualRoot.localPosition = visualBaseLocalPosition + Vector3.up * hover;
        visualRoot.localRotation = visualBaseRotation * Quaternion.Euler(0f, yaw, Mathf.Sin(idleTime * 1.1f) * 2.5f);
    }

    private void MoveToward(Vector3 position)
    {
        Vector3 direction = position - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.01f)
            return;

        transform.position += direction.normalized * moveSpeed * Time.deltaTime;
        FaceTarget(position);
    }

    private void ReturnHome()
    {
        float distance = FlatDistance(transform.position, homePosition);
        if (distance <= 0.25f)
        {
            transform.position = Vector3.Lerp(transform.position, homePosition, 4f * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, homeRotation, 4f * Time.deltaTime);
            if (hp < maxHp)
                hp = Mathf.Min(maxHp, hp + maxHp * 0.035f * Time.deltaTime);
            return;
        }

        MoveToward(homePosition);
    }

    private Transform FindTarget()
    {
        AOGCharacterStats[] heroes = Object.FindObjectsByType<AOGCharacterStats>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        Transform closest = null;
        float closestDistance = Mathf.Infinity;

        foreach (AOGCharacterStats hero in heroes)
        {
            if (hero == null || hero.IsDead)
                continue;

            float distance = FlatDistance(transform.position, hero.transform.position);
            if (distance <= detectionRange && distance < closestDistance)
            {
                closest = hero.transform;
                closestDistance = distance;
            }
        }

        return closest;
    }

    private static bool IsValidTarget(Transform target)
    {
        if (target == null || !target.gameObject.activeInHierarchy)
            return false;

        AOGCharacterStats hero = target.GetComponentInParent<AOGCharacterStats>();
        return hero == null || !hero.IsDead;
    }

    private void FaceTarget(Vector3 point)
    {
        Vector3 direction = point - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.01f)
            return;

        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction.normalized), 5.5f * Time.deltaTime);
    }

    private Transform FindVisualRoot()
    {
        Animator animator = GetComponentInChildren<Animator>(true);
        if (animator != null)
            return animator.transform;

        foreach (Transform child in transform)
        {
            if (child.GetComponentInChildren<Renderer>(true) != null)
                return child;
        }

        return transform;
    }

    private void BuildBossAura()
    {
        Color color = bossType == AOGNeutralBossType.Dragon
            ? new Color(1f, 0.22f, 0.05f, 1f)
            : new Color(0.55f, 0.20f, 0.90f, 1f);

        energyMaterial = BuildEmission(color, 3.5f);

        GameObject ringObject = new GameObject(bossType + "_Boss_Aura");
        ringObject.transform.SetParent(transform, false);
        ringObject.transform.localPosition = new Vector3(0f, 0.10f, 0f);

        LineRenderer ring = ringObject.AddComponent<LineRenderer>();
        ring.loop = true;
        ring.useWorldSpace = false;
        ring.positionCount = 64;
        ring.startWidth = 0.08f;
        ring.endWidth = 0.08f;
        ring.sharedMaterial = energyMaterial;
        float radius = bossType == AOGNeutralBossType.Dragon ? 4.2f : 3.6f;
        for (int i = 0; i < ring.positionCount; i++)
        {
            float a = i * Mathf.PI * 2f / ring.positionCount;
            float ripple = 1f + Mathf.Sin(a * 6f) * 0.04f;
            ring.SetPosition(i, new Vector3(Mathf.Cos(a) * radius * ripple, 0f, Mathf.Sin(a) * radius * ripple));
        }

        AOGOrbitAnimator orbit = ringObject.AddComponent<AOGOrbitAnimator>();
        orbit.speed = bossType == AOGNeutralBossType.Dragon ? 9f : -13f;
    }

    private IEnumerator DeathSequence(GameObject killer)
    {
        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }

        AOGPlayerEconomy economy = killer != null ? killer.GetComponentInParent<AOGPlayerEconomy>() : null;
        if (economy != null)
            economy.AddGold(bossType == AOGNeutralBossType.Dragon ? 450 : 350);

        Vector3 startScale = transform.localScale;
        Quaternion startRotation = transform.rotation;
        for (float t = 0f; t < 1.4f; t += Time.deltaTime)
        {
            float k = t / 1.4f;
            transform.localScale = Vector3.Lerp(startScale, startScale * 0.15f, k);
            transform.rotation = startRotation * Quaternion.Euler(0f, k * 420f, k * 35f);
            yield return null;
        }

        gameObject.SetActive(false);
    }

    private static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private static Material BuildUnlit(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        Material material = new Material(shader) { color = color };
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        return material;
    }

    private static Material BuildEmission(Color color, float strength)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        Material material = new Material(shader) { color = color };
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * strength);
        }
        return material;
    }
}

public class AOGTelegraphFade : MonoBehaviour
{
    public LineRenderer line;
    public float life = 0.7f;
    private float elapsed;
    private Color initialColor = Color.white;

    private void Start()
    {
        if (line != null && line.sharedMaterial != null)
            initialColor = line.sharedMaterial.color;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, life));
        if (line != null)
        {
            Color color = initialColor;
            color.a *= 1f - t;
            line.startColor = color;
            line.endColor = color;
            transform.localScale = Vector3.one * Mathf.Lerp(0.92f, 1.08f, t);
        }

        if (elapsed >= life)
            Destroy(gameObject);
    }
}

public class AOGNeutralBossBootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        GameObject host = new GameObject("AOG_Neutral_Boss_Bootstrap");
        Object.DontDestroyOnLoad(host);
        host.AddComponent<AOGNeutralBossBootstrap>();
    }

    private void Start()
    {
        StartCoroutine(AttachBosses());
    }

    private IEnumerator AttachBosses()
    {
        for (int attempt = 0; attempt < 15; attempt++)
        {
            GameObject[] all = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            bool foundAny = false;

            foreach (GameObject obj in all)
            {
                if (obj == null)
                    continue;

                string lower = obj.name.ToLowerInvariant();
                AOGNeutralBossType? type = null;

                if (lower.Contains("dragon") && IsLikelyBossRoot(obj))
                    type = AOGNeutralBossType.Dragon;
                else if (lower.Contains("medusa") && IsLikelyBossRoot(obj))
                    type = AOGNeutralBossType.Medusa;

                if (!type.HasValue)
                    continue;

                foundAny = true;
                AOGNeutralBossAI ai = obj.GetComponent<AOGNeutralBossAI>();
                if (ai == null)
                    ai = obj.AddComponent<AOGNeutralBossAI>();

                ai.bossType = type.Value;
                ai.maxHp = type == AOGNeutralBossType.Dragon ? 6500f : 5200f;
                ai.hp = ai.maxHp;
                ai.attackDamage = type == AOGNeutralBossType.Dragon ? 115f : 92f;
                ai.attackRange = type == AOGNeutralBossType.Dragon ? 5.4f : 4.8f;

                if (obj.GetComponent<Collider>() == null)
                {
                    CapsuleCollider collider = obj.AddComponent<CapsuleCollider>();
                    collider.center = new Vector3(0f, 2.2f, 0f);
                    collider.height = type == AOGNeutralBossType.Dragon ? 7f : 5.5f;
                    collider.radius = type == AOGNeutralBossType.Dragon ? 3f : 2.4f;
                }

                AOGObjectiveWorldBar bar = obj.GetComponent<AOGObjectiveWorldBar>();
                if (bar == null)
                    bar = obj.AddComponent<AOGObjectiveWorldBar>();
                bar.offset = new Vector3(0f, type == AOGNeutralBossType.Dragon ? 8.2f : 6.6f, 0f);
                bar.width = type == AOGNeutralBossType.Dragon ? 5.2f : 4.5f;
                bar.height = 0.32f;
            }

            if (foundAny)
                yield break;

            yield return new WaitForSecondsRealtime(0.25f);
        }
    }

    private static bool IsLikelyBossRoot(GameObject obj)
    {
        if (obj.transform.parent == null)
            return true;

        string parentName = obj.transform.parent.name.ToLowerInvariant();
        string name = obj.name.ToLowerInvariant();
        return !parentName.Contains("dragon") && !parentName.Contains("medusa") &&
               (name.Contains("model") || obj.GetComponentInChildren<Renderer>(true) != null);
    }
}
