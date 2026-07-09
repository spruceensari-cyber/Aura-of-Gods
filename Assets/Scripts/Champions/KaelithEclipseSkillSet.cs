using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IAOGAbilityCooldownProvider
{
    string ChampionDisplayName { get; }
    string ChampionRoleName { get; }
    string GetAbilityName(int slot);
    float GetAbilityCooldownRatio(int slot);
    float GetAbilityCooldownDuration(int slot);
}

public class KaelithEclipseSkillSet : MonoBehaviour, IAOGAbilityCooldownProvider, IChampionBasicAttackModifier
{
    [Header("Cooldowns")]
    public float qCooldown = 4.5f;
    public float wCooldown = 10f;
    public float eCooldown = 8f;
    public float rCooldown = 42f;

    [Header("Q - Void Lance")]
    public float qDamage = 125f;
    public float qRange = 16f;
    public float qSpeed = 22f;

    [Header("W - Eclipse Domain")]
    public float wRadius = 5.5f;
    public float wDuration = 4f;
    public float wDamagePerTick = 38f;

    [Header("E - Rift Dash")]
    public float eDistance = 7f;
    public float eDamage = 110f;
    public float eDuration = 0.22f;

    [Header("R - Total Eclipse")]
    public float rRadius = 8.5f;
    public float rDamage = 360f;
    public float rWindup = 0.85f;

    private float nextQ;
    private float nextW;
    private float nextE;
    private float nextR;
    private ChampionPresentationController presentation;
    private AOGChampionProceduralAnimator procedural;
    private AOGCharacterStats stats;
    private bool nextAttackEmpowered;

    public string ChampionDisplayName => "KAELITH";
    public string ChampionRoleName => "ECLIPSE REAVER";

    private void Awake()
    {
        presentation = GetComponent<ChampionPresentationController>();
        procedural = GetComponent<AOGChampionProceduralAnimator>();
        stats = GetComponent<AOGCharacterStats>();
    }

    private void Update()
    {
        AOGActiveChampion active = GetComponent<AOGActiveChampion>();
        if (active != null && !active.IsActiveChampion)
            return;

        if (AOGInputBridge.KeyPressedThisFrame(KeyCode.Q)) CastQ();
        if (AOGInputBridge.KeyPressedThisFrame(KeyCode.W)) CastW();
        if (AOGInputBridge.KeyPressedThisFrame(KeyCode.E)) CastE();
        if (AOGInputBridge.KeyPressedThisFrame(KeyCode.R)) CastR();
    }

    public void CastQ()
    {
        if (Time.time < nextQ)
            return;

        nextQ = Time.time + qCooldown;
        presentation?.PlayAbility(0);
        procedural?.PlaySkill(0);

        GameObject projectile = new GameObject("Kaelith_Q_Void_Lance");
        projectile.transform.position = transform.position + Vector3.up * 1.35f + transform.forward * 1.0f;
        projectile.transform.rotation = transform.rotation;

        AOGSkillProjectile logic = projectile.AddComponent<AOGSkillProjectile>();
        logic.owner = gameObject;
        logic.team = stats != null ? stats.team : MinionTeam.Blue;
        logic.direction = transform.forward;
        logic.speed = qSpeed;
        logic.range = qRange;
        logic.damage = qDamage;
        logic.radius = 0.42f;
        logic.color = new Color(0.46f, 0.16f, 0.94f, 1f);
        logic.pierceCount = 1;
        logic.BuildVisual(AOGSkillProjectile.Shape.Lance);
    }

    public void CastW()
    {
        if (Time.time < nextW)
            return;

        nextW = Time.time + wCooldown;
        presentation?.PlayAbility(1);
        procedural?.PlaySkill(1);

        GameObject zoneObject = new GameObject("Kaelith_W_Eclipse_Domain");
        zoneObject.transform.position = transform.position;
        AOGPersistentDamageZone zone = zoneObject.AddComponent<AOGPersistentDamageZone>();
        zone.owner = gameObject;
        zone.team = stats != null ? stats.team : MinionTeam.Blue;
        zone.radius = wRadius;
        zone.duration = wDuration;
        zone.damagePerTick = wDamagePerTick;
        zone.tickRate = 0.65f;
        zone.color = new Color(0.28f, 0.06f, 0.48f, 0.72f);
        zone.BuildVisual();

        nextAttackEmpowered = true;
    }

    public void CastE()
    {
        if (Time.time < nextE)
            return;

        nextE = Time.time + eCooldown;
        presentation?.PlayAbility(2);
        procedural?.PlaySkill(2);
        StartCoroutine(RiftDash());
    }

    public void CastR()
    {
        if (Time.time < nextR)
            return;

        nextR = Time.time + rCooldown;
        presentation?.PlayAbility(3);
        procedural?.PlaySkill(3);
        StartCoroutine(TotalEclipse());
    }

    private IEnumerator RiftDash()
    {
        Vector3 start = transform.position;
        Vector3 end = start + transform.forward * eDistance;
        HashSet<Object> hitTargets = new HashSet<Object>();

        GameObject trail = AOGAbilityVisuals.CreateBeam("Kaelith_E_Rift_Trail", start + Vector3.up * 0.15f, end + Vector3.up * 0.15f, new Color(0.48f, 0.12f, 0.95f, 0.8f), 0.45f);

        float elapsed = 0f;
        while (elapsed < eDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / eDuration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            transform.position = Vector3.Lerp(start, end, eased);

            DamageEnemiesInRadius(transform.position, 1.7f, eDamage, hitTargets);
            yield return null;
        }

        transform.position = end;
        presentation?.SpawnAbilityImpactVfx(end + Vector3.up * 0.6f, 2);
        if (trail != null) Destroy(trail, 0.35f);
    }

    private IEnumerator TotalEclipse()
    {
        GameObject telegraph = AOGAbilityVisuals.CreateRing("Kaelith_R_Eclipse_Telegraph", transform.position + Vector3.up * 0.08f, rRadius, new Color(0.58f, 0.12f, 0.92f, 0.92f), 0.14f);
        AOGScreenEclipsePulse screenPulse = gameObject.AddComponent<AOGScreenEclipsePulse>();
        screenPulse.duration = rWindup + 0.75f;

        float elapsed = 0f;
        Vector3 baseScale = telegraph != null ? telegraph.transform.localScale : Vector3.one;
        while (elapsed < rWindup)
        {
            elapsed += Time.deltaTime;
            if (telegraph != null)
            {
                float pulse = 1f + Mathf.Sin(elapsed * 18f) * 0.035f;
                telegraph.transform.localScale = baseScale * pulse;
            }
            yield return null;
        }

        HashSet<Object> targets = new HashSet<Object>();
        DamageEnemiesInRadius(transform.position, rRadius, rDamage, targets);
        presentation?.SpawnAbilityImpactVfx(transform.position + Vector3.up * 0.8f, 3);
        AOGMobaCameraController camera = Camera.main != null ? Camera.main.GetComponent<AOGMobaCameraController>() : null;
        camera?.AddRandomImpulse(0.44f);

        if (telegraph != null)
            Destroy(telegraph, 0.4f);
    }

    private void DamageEnemiesInRadius(Vector3 center, float radius, float damage, HashSet<Object> alreadyHit)
    {
        Collider[] hits = Physics.OverlapSphere(center, radius, ~0, QueryTriggerInteraction.Ignore);
        foreach (Collider hit in hits)
        {
            if (hit == null)
                continue;

            if (TryDamageTarget(hit, damage, alreadyHit))
                continue;
        }
    }

    private bool TryDamageTarget(Collider hit, float damage, HashSet<Object> alreadyHit)
    {
        Minion minion = hit.GetComponentInParent<Minion>();
        if (minion != null && stats != null && minion.team != stats.team)
        {
            if (alreadyHit.Add(minion))
                minion.TakeDamage(damage, gameObject);
            return true;
        }

        TowerHealth tower = hit.GetComponentInParent<TowerHealth>();
        if (tower != null && stats != null && tower.towerTeam != stats.team)
        {
            if (alreadyHit.Add(tower))
                tower.TakeDamage(damage * 0.55f);
            return true;
        }

        AOGNexusCore nexus = hit.GetComponentInParent<AOGNexusCore>();
        if (nexus != null && stats != null && nexus.team != stats.team)
        {
            if (alreadyHit.Add(nexus))
                nexus.TakeDamage(damage * 0.35f);
            return true;
        }

        AOGNeutralBossAI boss = hit.GetComponentInParent<AOGNeutralBossAI>();
        if (boss != null)
        {
            if (alreadyHit.Add(boss))
                boss.TakeDamage(damage, gameObject);
            return true;
        }

        return false;
    }

    public void OnBasicAttackHit(Minion target)
    {
        if (!nextAttackEmpowered || target == null)
            return;

        nextAttackEmpowered = false;
        target.TakeDamage(45f, gameObject);
        AOGAbilityVisuals.CreateRing("Kaelith_Empowered_Hit", target.transform.position + Vector3.up * 0.12f, 1.4f, new Color(0.70f, 0.20f, 1f, 0.92f), 0.10f);
        presentation?.SpawnImpactVfx(target.transform.position + Vector3.up * 0.8f, true, false);
    }

    public string GetAbilityName(int slot)
    {
        switch (slot)
        {
            case 0: return "VOID LANCE";
            case 1: return "ECLIPSE DOMAIN";
            case 2: return "RIFT DASH";
            default: return "TOTAL ECLIPSE";
        }
    }

    public float GetAbilityCooldownRatio(int slot)
    {
        switch (slot)
        {
            case 0: return CooldownRatio(nextQ, qCooldown);
            case 1: return CooldownRatio(nextW, wCooldown);
            case 2: return CooldownRatio(nextE, eCooldown);
            default: return CooldownRatio(nextR, rCooldown);
        }
    }

    public float GetAbilityCooldownDuration(int slot)
    {
        switch (slot)
        {
            case 0: return qCooldown;
            case 1: return wCooldown;
            case 2: return eCooldown;
            default: return rCooldown;
        }
    }

    private static float CooldownRatio(float nextTime, float duration)
    {
        if (duration <= 0f)
            return 0f;
        return Mathf.Clamp01((nextTime - Time.time) / duration);
    }
}

public class AOGSkillProjectile : MonoBehaviour
{
    public enum Shape { Orb, Lance }

    public GameObject owner;
    public MinionTeam team;
    public Vector3 direction = Vector3.forward;
    public float speed = 20f;
    public float range = 14f;
    public float damage = 100f;
    public float radius = 0.4f;
    public Color color = Color.magenta;
    public int pierceCount;

    private Vector3 startPosition;
    private int pierced;
    private readonly HashSet<Object> hitTargets = new HashSet<Object>();

    private void Start()
    {
        startPosition = transform.position;
    }

    private void Update()
    {
        float step = speed * Time.deltaTime;
        Vector3 from = transform.position;
        Vector3 to = from + direction.normalized * step;

        RaycastHit[] hits = Physics.SphereCastAll(from, radius, direction.normalized, step, ~0, QueryTriggerInteraction.Ignore);
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null || (owner != null && hit.collider.transform.IsChildOf(owner.transform)))
                continue;

            if (DamageTarget(hit.collider))
            {
                pierced++;
                AOGAbilityVisuals.CreateRing("Void_Lance_Impact", hit.point + Vector3.up * 0.08f, 1.0f, color, 0.08f);
                if (pierced > pierceCount)
                {
                    Destroy(gameObject);
                    return;
                }
            }
        }

        transform.position = to;
        if (Vector3.Distance(startPosition, transform.position) >= range)
            Destroy(gameObject);
    }

    public void BuildVisual(Shape shape)
    {
        GameObject visual = GameObject.CreatePrimitive(shape == Shape.Lance ? PrimitiveType.Capsule : PrimitiveType.Sphere);
        visual.name = "Projectile_Visual";
        visual.transform.SetParent(transform, false);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = shape == Shape.Lance ? Quaternion.Euler(90f, 0f, 0f) : Quaternion.identity;
        visual.transform.localScale = shape == Shape.Lance ? new Vector3(0.28f, 1.35f, 0.28f) : Vector3.one * 0.55f;
        Collider collider = visual.GetComponent<Collider>();
        if (collider != null) Destroy(collider);

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        Material material = new Material(shader) { color = color };
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * 5f);
        }
        visual.GetComponent<Renderer>().sharedMaterial = material;

        TrailRenderer trail = visual.AddComponent<TrailRenderer>();
        trail.time = 0.28f;
        trail.startWidth = shape == Shape.Lance ? 0.42f : 0.52f;
        trail.endWidth = 0f;
        trail.sharedMaterial = material;
        trail.startColor = color;
        trail.endColor = new Color(color.r, color.g, color.b, 0f);
    }

    private bool DamageTarget(Collider collider)
    {
        Minion minion = collider.GetComponentInParent<Minion>();
        if (minion != null && minion.team != team && hitTargets.Add(minion))
        {
            minion.TakeDamage(damage, owner);
            return true;
        }

        TowerHealth tower = collider.GetComponentInParent<TowerHealth>();
        if (tower != null && tower.towerTeam != team && hitTargets.Add(tower))
        {
            tower.TakeDamage(damage * 0.55f);
            return true;
        }

        AOGNexusCore nexus = collider.GetComponentInParent<AOGNexusCore>();
        if (nexus != null && nexus.team != team && hitTargets.Add(nexus))
        {
            nexus.TakeDamage(damage * 0.35f);
            return true;
        }

        AOGNeutralBossAI boss = collider.GetComponentInParent<AOGNeutralBossAI>();
        if (boss != null && hitTargets.Add(boss))
        {
            boss.TakeDamage(damage, owner);
            return true;
        }

        return false;
    }
}

public class AOGPersistentDamageZone : MonoBehaviour
{
    public GameObject owner;
    public MinionTeam team;
    public float radius = 5f;
    public float duration = 4f;
    public float damagePerTick = 35f;
    public float tickRate = 0.7f;
    public Color color = new Color(0.3f, 0.1f, 0.6f, 0.7f);

    private float elapsed;
    private float nextTick;
    private GameObject visual;

    public void BuildVisual()
    {
        visual = AOGAbilityVisuals.CreateDisc("Persistent_Zone_Visual", transform.position + Vector3.up * 0.07f, radius, color);
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        if (elapsed >= duration)
        {
            if (visual != null) Destroy(visual);
            Destroy(gameObject);
            return;
        }

        if (Time.time >= nextTick)
        {
            nextTick = Time.time + tickRate;
            TickDamage();
        }

        if (visual != null)
        {
            float pulse = 1f + Mathf.Sin(Time.time * 4.5f) * 0.035f;
            visual.transform.localScale = Vector3.one * pulse;
        }
    }

    private void TickDamage()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, radius, ~0, QueryTriggerInteraction.Ignore);
        HashSet<Object> damaged = new HashSet<Object>();
        foreach (Collider hit in hits)
        {
            Minion minion = hit.GetComponentInParent<Minion>();
            if (minion != null && minion.team != team && damaged.Add(minion))
            {
                minion.TakeDamage(damagePerTick, owner);
                continue;
            }

            AOGNeutralBossAI boss = hit.GetComponentInParent<AOGNeutralBossAI>();
            if (boss != null && damaged.Add(boss))
                boss.TakeDamage(damagePerTick, owner);
        }
    }
}

public static class AOGAbilityVisuals
{
    public static GameObject CreateRing(string name, Vector3 position, float radius, Color color, float width)
    {
        GameObject go = new GameObject(name);
        go.transform.position = position;
        LineRenderer line = go.AddComponent<LineRenderer>();
        line.loop = true;
        line.useWorldSpace = false;
        line.positionCount = 64;
        line.startWidth = width;
        line.endWidth = width;
        line.sharedMaterial = CreateUnlit(color);
        line.startColor = color;
        line.endColor = color;
        for (int i = 0; i < line.positionCount; i++)
        {
            float a = i * Mathf.PI * 2f / line.positionCount;
            line.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius));
        }
        return go;
    }

    public static GameObject CreateDisc(string name, Vector3 position, float radius, Color color)
    {
        GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        disc.name = name;
        disc.transform.position = position;
        disc.transform.localScale = new Vector3(radius, 0.025f, radius);
        Collider col = disc.GetComponent<Collider>();
        if (col != null) Object.Destroy(col);
        Material material = CreateLit(color);
        disc.GetComponent<Renderer>().sharedMaterial = material;
        return disc;
    }

    public static GameObject CreateBeam(string name, Vector3 start, Vector3 end, Color color, float width)
    {
        GameObject beam = new GameObject(name);
        LineRenderer line = beam.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.positionCount = 2;
        line.SetPosition(0, start);
        line.SetPosition(1, end);
        line.startWidth = width;
        line.endWidth = width * 0.15f;
        line.sharedMaterial = CreateUnlit(color);
        line.startColor = color;
        line.endColor = new Color(color.r, color.g, color.b, 0f);
        return beam;
    }

    private static Material CreateUnlit(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        Material mat = new Material(shader) { color = color };
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        return mat;
    }

    private static Material CreateLit(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        Material mat = new Material(shader) { color = color };
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * 2.5f);
        }
        return mat;
    }
}

public class AOGScreenEclipsePulse : MonoBehaviour
{
    public float duration = 1.5f;
    private float elapsed;
    private Color originalAmbient;

    private void Start()
    {
        originalAmbient = RenderSettings.ambientLight;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration));
        float pulse = Mathf.Sin(t * Mathf.PI);
        RenderSettings.ambientLight = Color.Lerp(originalAmbient, new Color(0.025f, 0.01f, 0.055f), pulse * 0.85f);

        if (elapsed >= duration)
        {
            RenderSettings.ambientLight = originalAmbient;
            Destroy(this);
        }
    }
}

public class AOGChampionProceduralAnimator : MonoBehaviour
{
    public float moveBobFrequency = 8f;
    public float moveBobHeight = 0.055f;
    public float moveLean = 5f;

    private Transform visualRoot;
    private Vector3 baseLocalPosition;
    private Quaternion baseLocalRotation;
    private Vector3 lastWorldPosition;
    private float moveBlend;
    private float attackTimer;
    private float attackDuration;
    private int attackVariant;
    private float skillTimer;
    private float skillDuration;
    private int skillSlot;
    private float hitTimer;
    private bool dead;

    private void Start()
    {
        visualRoot = FindVisualRoot();
        if (visualRoot != null)
        {
            baseLocalPosition = visualRoot.localPosition;
            baseLocalRotation = visualRoot.localRotation;
        }
        lastWorldPosition = transform.position;
    }

    private void Update()
    {
        if (visualRoot == null)
            return;

        Vector3 velocity = (transform.position - lastWorldPosition) / Mathf.Max(Time.deltaTime, 0.0001f);
        velocity.y = 0f;
        lastWorldPosition = transform.position;
        moveBlend = Mathf.Lerp(moveBlend, Mathf.Clamp01(velocity.magnitude / 6f), 10f * Time.deltaTime);

        if (dead)
        {
            visualRoot.localRotation = Quaternion.Slerp(visualRoot.localRotation, baseLocalRotation * Quaternion.Euler(82f, 0f, 24f), 4f * Time.deltaTime);
            visualRoot.localPosition = Vector3.Lerp(visualRoot.localPosition, baseLocalPosition + Vector3.down * 0.65f, 4f * Time.deltaTime);
            return;
        }

        Vector3 targetPosition = baseLocalPosition;
        Quaternion targetRotation = baseLocalRotation;

        if (moveBlend > 0.02f)
        {
            float cycle = Time.time * moveBobFrequency;
            targetPosition += Vector3.up * Mathf.Abs(Mathf.Sin(cycle)) * moveBobHeight * moveBlend;
            targetRotation *= Quaternion.Euler(Mathf.Sin(cycle) * moveLean * moveBlend, 0f, Mathf.Cos(cycle * 0.5f) * 1.8f * moveBlend);
        }

        if (attackTimer > 0f)
        {
            attackTimer -= Time.deltaTime;
            float t = 1f - attackTimer / Mathf.Max(0.01f, attackDuration);
            float arc = Mathf.Sin(t * Mathf.PI);
            float yaw = attackVariant % 2 == 0 ? 38f : -38f;
            targetRotation *= Quaternion.Euler(-arc * 14f, arc * yaw, -arc * yaw * 0.35f);
            targetPosition += Vector3.forward * arc * 0.22f;
        }

        if (skillTimer > 0f)
        {
            skillTimer -= Time.deltaTime;
            float t = 1f - skillTimer / Mathf.Max(0.01f, skillDuration);
            float pulse = Mathf.Sin(t * Mathf.PI);
            targetPosition += Vector3.up * pulse * (skillSlot == 3 ? 0.42f : 0.18f);
            targetRotation *= Quaternion.Euler(-pulse * (skillSlot == 2 ? 28f : 8f), pulse * (skillSlot == 3 ? 90f : 25f), 0f);
        }

        if (hitTimer > 0f)
        {
            hitTimer -= Time.deltaTime;
            float t = hitTimer / 0.18f;
            targetRotation *= Quaternion.Euler(0f, 0f, Mathf.Sin(t * Mathf.PI) * 12f);
        }

        visualRoot.localPosition = Vector3.Lerp(visualRoot.localPosition, targetPosition, 16f * Time.deltaTime);
        visualRoot.localRotation = Quaternion.Slerp(visualRoot.localRotation, targetRotation, 18f * Time.deltaTime);
    }

    public void PlayAttack(int variant = 0)
    {
        attackVariant = variant;
        attackDuration = 0.42f;
        attackTimer = attackDuration;
    }

    public void PlaySkill(int slot)
    {
        skillSlot = slot;
        skillDuration = slot == 3 ? 1.1f : slot == 2 ? 0.36f : 0.65f;
        skillTimer = skillDuration;
    }

    public void PlayHit()
    {
        hitTimer = 0.18f;
    }

    public void PlayDeath()
    {
        dead = true;
    }

    private Transform FindVisualRoot()
    {
        Animator animator = GetComponentInChildren<Animator>(true);
        if (animator != null && animator.transform != transform)
            return animator.transform;

        Transform procedural = transform.Find("Kaelith_Procedural_Visual");
        if (procedural != null)
            return procedural;

        foreach (Transform child in transform)
        {
            if (child.GetComponentInChildren<Renderer>(true) != null)
                return child;
        }

        return null;
    }
}
