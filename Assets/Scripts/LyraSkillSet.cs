using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LyraSkillSet : MonoBehaviour, IChampionBasicAttackModifier
{
    [Header("Team")]
    public MinionTeam team = MinionTeam.Blue;

    [Header("References")]
    public Animator animator;
    public ChampionPresentationController presentation;

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
    [Range(0.05f, 1f)] public float vanishedAlpha = 0.28f;

    [Header("E - Hunter's Net")]
    public float eRange = 10f;
    public float eDamage = 45f;
    public float eSlowDuration = 2f;
    [Range(0.1f, 1f)] public float eSlowMultiplier = 0.45f;

    [Header("R - Blood Moon Execution")]
    public float rRange = 9f;
    public float rDamage = 220f;
    [Range(0.05f, 0.95f)] public float executeThreshold = 0.35f;

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
    private readonly Dictionary<Renderer, Color[]> originalRendererColors = new Dictionary<Renderer, Color[]>();

    public bool IsVanished => vanished;

    private void Awake()
    {
        if (presentation == null)
            presentation = GetComponent<ChampionPresentationController>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        AOGCharacterStats stats = GetComponent<AOGCharacterStats>();
        if (stats != null)
            team = stats.team;
    }

    private void Update()
    {
        if (vanished && Time.time >= vanishEndTime)
        {
            vanished = false;
            RestoreRendererOpacity();
        }

        if (Input.GetKeyDown(KeyCode.Q)) CastQ();
        if (Input.GetKeyDown(KeyCode.W)) CastW();
        if (Input.GetKeyDown(KeyCode.E)) CastE();
        if (Input.GetKeyDown(KeyCode.R)) CastR();
    }

    private void CastQ()
    {
        if (Time.time < nextQ)
            return;

        Minion target = FindClosestEnemyMinion(qRange);
        if (target == null)
            return;

        nextQ = Time.time + qCooldown;
        PlayAbilityPresentation(0);
        CreateProjectile(target, qDamage, qProjectileSpeed, "Lyra_Q_Neon_Dagger", 0);
    }

    private void CastW()
    {
        if (Time.time < nextW)
            return;

        nextW = Time.time + wCooldown;
        PlayAbilityPresentation(1);

        transform.position += transform.forward * wDashDistance;
        vanished = true;
        vanishEndTime = Time.time + wBuffDuration;
        nextAttackEmpowered = true;

        SetRenderersTransparent(vanishedAlpha);
        CreateCircleVisual(transform.position, 2.2f, "Lyra_W_Vanish");
        presentation?.SpawnAbilityImpactVfx(transform.position + Vector3.up * 0.2f, 1);
    }

    private void CastE()
    {
        if (Time.time < nextE)
            return;

        Minion target = FindClosestEnemyMinion(eRange);
        if (target == null)
            return;

        nextE = Time.time + eCooldown;
        PlayAbilityPresentation(2);

        target.TakeDamage(eDamage, gameObject);
        StartCoroutine(ApplySlow(target, eSlowDuration));
        CreateCircleVisual(target.transform.position, 2.8f, "Lyra_E_Hunters_Net");
        presentation?.SpawnAbilityImpactVfx(target.transform.position + Vector3.up * 0.8f, 2);
    }

    private void CastR()
    {
        if (Time.time < nextR)
            return;

        Minion target = FindClosestEnemyMinion(rRange);
        if (target == null)
            return;

        nextR = Time.time + rCooldown;
        PlayAbilityPresentation(3);

        Vector3 behindTarget = target.transform.position - target.transform.forward * 1.7f;
        behindTarget.y = transform.position.y;
        transform.position = behindTarget;
        FaceTarget(target.transform.position);

        float finalDamage = target.hp <= target.maxHp * executeThreshold ? target.hp + 999f : rDamage;
        target.TakeDamage(finalDamage, gameObject);
        CreateCircleVisual(target.transform.position, 4f, "Lyra_R_Blood_Moon");
        presentation?.SpawnAbilityImpactVfx(target.transform.position + Vector3.up * 1f, 3);
        CreateProjectile(target, 0f, 35f, "Lyra_R_Blood_Slash", 3);
    }

    public void OnBasicAttackHit(Minion target)
    {
        if (!nextAttackEmpowered || target == null)
            return;

        target.TakeDamage(wBonusDamage, gameObject);
        nextAttackEmpowered = false;
        presentation?.SpawnImpactVfx(target.transform.position + Vector3.up * 0.8f, true, false);
    }

    private Minion FindClosestEnemyMinion(float range)
    {
        Minion[] minions = FindObjectsByType<Minion>(FindObjectsSortMode.None);
        Minion closest = null;
        float closestDistance = Mathf.Infinity;

        foreach (Minion minion in minions)
        {
            if (minion == null || !minion.gameObject.activeInHierarchy || minion.hp <= 0f || minion.team == team)
                continue;

            float distance = FlatDistance(transform.position, minion.transform.position);
            if (distance <= range && distance < closestDistance)
            {
                closest = minion;
                closestDistance = distance;
            }
        }

        return closest;
    }

    private void CreateProjectile(Minion target, float damage, float speed, string projectileName, int abilitySlot)
    {
        if (target == null)
            return;

        GameObject projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        projectile.name = projectileName;
        projectile.transform.position = transform.position + Vector3.up * 1.4f + transform.forward * 0.8f;
        projectile.transform.localScale = Vector3.one * 0.35f;

        Renderer projectileRenderer = projectile.GetComponent<Renderer>();
        if (projectileRenderer != null)
            projectileRenderer.material = CreateGlowMaterial(lyraColor, 3f);

        Collider projectileCollider = projectile.GetComponent<Collider>();
        if (projectileCollider != null)
            Destroy(projectileCollider);

        LyraProjectile projectileLogic = projectile.AddComponent<LyraProjectile>();
        projectileLogic.target = target;
        projectileLogic.owner = gameObject;
        projectileLogic.presentation = presentation;
        projectileLogic.abilitySlot = abilitySlot;
        projectileLogic.damage = damage;
        projectileLogic.speed = speed;
        projectileLogic.color = lyraColor;
    }

    private void CreateCircleVisual(Vector3 position, float size, string visualName)
    {
        GameObject circle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        circle.name = visualName;
        circle.transform.position = position + Vector3.up * 0.05f;
        circle.transform.localScale = new Vector3(size, 0.03f, size);

        Renderer circleRenderer = circle.GetComponent<Renderer>();
        if (circleRenderer != null)
            circleRenderer.material = CreateGlowMaterial(lyraColor, 2.5f);

        Collider circleCollider = circle.GetComponent<Collider>();
        if (circleCollider != null)
            Destroy(circleCollider);

        Destroy(circle, visualLife);
    }

    private Material CreateGlowMaterial(Color color, float emissionStrength)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        Material material = new Material(shader);
        material.color = color;

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);

        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * emissionStrength);
        }

        return material;
    }

    private IEnumerator ApplySlow(Minion target, float delay)
    {
        if (target == null)
            yield break;

        float originalSpeed = target.speed;
        target.speed = originalSpeed * eSlowMultiplier;

        yield return new WaitForSeconds(delay);

        if (target != null)
            target.speed = originalSpeed;
    }

    private void SetRenderersTransparent(float alpha)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);

        foreach (Renderer targetRenderer in renderers)
        {
            if (targetRenderer == null)
                continue;

            string objectName = targetRenderer.gameObject.name;
            if (objectName.Contains("AOG_HP_Bar") || objectName.Contains("Readability_Ring") || objectName.Contains("Ground_Shadow"))
                continue;

            Material[] materials = targetRenderer.materials;
            if (!originalRendererColors.ContainsKey(targetRenderer))
            {
                Color[] colors = new Color[materials.Length];
                for (int i = 0; i < materials.Length; i++)
                    colors[i] = materials[i].color;
                originalRendererColors[targetRenderer] = colors;
            }

            foreach (Material material in materials)
            {
                Color color = material.color;
                color.a = alpha;
                material.color = color;

                if (material.HasProperty("_BaseColor"))
                {
                    Color baseColor = material.GetColor("_BaseColor");
                    baseColor.a = alpha;
                    material.SetColor("_BaseColor", baseColor);
                }

                if (material.HasProperty("_Surface"))
                    material.SetFloat("_Surface", 1f);

                material.renderQueue = 3000;
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.EnableKeyword("_ALPHABLEND_ON");
            }
        }
    }

    private void RestoreRendererOpacity()
    {
        foreach (KeyValuePair<Renderer, Color[]> pair in originalRendererColors)
        {
            Renderer targetRenderer = pair.Key;
            if (targetRenderer == null)
                continue;

            Material[] materials = targetRenderer.materials;
            Color[] originalColors = pair.Value;

            for (int i = 0; i < materials.Length && i < originalColors.Length; i++)
            {
                Color color = originalColors[i];
                color.a = 1f;
                materials[i].color = color;

                if (materials[i].HasProperty("_BaseColor"))
                    materials[i].SetColor("_BaseColor", color);

                if (materials[i].HasProperty("_Surface"))
                    materials[i].SetFloat("_Surface", 0f);

                materials[i].renderQueue = 2000;
            }
        }
    }

    private void PlayAbilityPresentation(int slot)
    {
        if (presentation == null)
            presentation = GetComponent<ChampionPresentationController>();

        if (presentation != null)
        {
            presentation.PlayAbility(slot);
            return;
        }

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (animator == null)
            return;

        string trigger = slot == 0 ? "SkillQ" : slot == 1 ? "SkillW" : slot == 2 ? "SkillE" : "SkillR";
        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.name == trigger && parameter.type == AnimatorControllerParameterType.Trigger)
            {
                animator.SetTrigger(trigger);
                break;
            }
        }
    }

    private void FaceTarget(Vector3 target)
    {
        Vector3 direction = target - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(direction);
    }

    private static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    public float GetQCooldownRatio() => qCooldown <= 0f ? 0f : Mathf.Clamp01((nextQ - Time.time) / qCooldown);
    public float GetWCooldownRatio() => wCooldown <= 0f ? 0f : Mathf.Clamp01((nextW - Time.time) / wCooldown);
    public float GetECooldownRatio() => eCooldown <= 0f ? 0f : Mathf.Clamp01((nextE - Time.time) / eCooldown);
    public float GetRCooldownRatio() => rCooldown <= 0f ? 0f : Mathf.Clamp01((nextR - Time.time) / rCooldown);
}
