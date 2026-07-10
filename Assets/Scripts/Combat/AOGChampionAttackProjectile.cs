using System;
using UnityEngine;

public enum AOGChampionProjectileStyle
{
    MoonDagger,
    VoidArrow,
    SpiritOrb,
    FlameOrb,
    AstralLance,
    GenericArcane
}

/// <summary>
/// Authoritative ranged basic-attack projectile. Applies damage exactly once on impact
/// and raises a unified combat event after confirmed resolution.
/// </summary>
public class AOGChampionAttackProjectile : MonoBehaviour
{
    public GameObject owner;
    public Transform target;
    public float damage;
    public float speed = 24f;
    public float impactRadius;
    public AOGChampionProjectileStyle style;
    public Color accentColor = Color.white;
    public AOGCombatTargetKind targetKind;
    public Action<bool> completed;

    private bool resolved;
    private float deadline;
    private LineRenderer trail;

    public static AOGChampionAttackProjectile Launch(
        GameObject owner,
        Transform target,
        float damage,
        float speed,
        AOGChampionProjectileStyle style,
        Color accent,
        AOGCombatTargetKind targetKind,
        Action<bool> completed = null)
    {
        if (owner == null || target == null)
            return null;

        GameObject projectile = new GameObject(owner.name + "_AttackProjectile");
        projectile.transform.position = owner.transform.position + Vector3.up * 1.25f + owner.transform.forward * 0.55f;

        AOGChampionAttackProjectile resolver = projectile.AddComponent<AOGChampionAttackProjectile>();
        resolver.owner = owner;
        resolver.target = target;
        resolver.damage = damage;
        resolver.speed = Mathf.Max(4f, speed);
        resolver.style = style;
        resolver.accentColor = accent;
        resolver.targetKind = targetKind;
        resolver.completed = completed;
        resolver.BuildVisual();
        return resolver;
    }

    private void Start()
    {
        deadline = Time.time + 2.5f;
    }

    private void Update()
    {
        if (resolved)
            return;

        if (target == null || !target.gameObject.activeInHierarchy || Time.time >= deadline)
        {
            Resolve(false);
            return;
        }

        Vector3 destination = target.position + Vector3.up * TargetHeight(targetKind);
        Vector3 previous = transform.position;
        transform.position = Vector3.MoveTowards(transform.position, destination, speed * Time.deltaTime);
        transform.forward = (destination - transform.position).sqrMagnitude > 0.001f
            ? (destination - transform.position).normalized
            : transform.forward;

        if (trail != null)
        {
            trail.SetPosition(0, previous);
            trail.SetPosition(1, transform.position);
        }

        if (Vector3.Distance(transform.position, destination) <= 0.16f)
        {
            bool hit = ApplyDamage();
            Resolve(hit);
        }
    }

    private bool ApplyDamage()
    {
        if (target == null)
            return false;

        switch (targetKind)
        {
            case AOGCombatTargetKind.Champion:
            {
                AOGCharacterStats hero = target.GetComponentInParent<AOGCharacterStats>();
                if (hero == null || hero.IsDead) return false;
                hero.TakeDamage(damage, owner);
                break;
            }
            case AOGCombatTargetKind.Minion:
            {
                Minion minion = target.GetComponentInParent<Minion>();
                if (minion == null || minion.hp <= 0f) return false;
                minion.TakeDamage(damage, owner);
                break;
            }
            case AOGCombatTargetKind.NeutralMonster:
            {
                AOGNeutralMonsterRuntime monster = target.GetComponentInParent<AOGNeutralMonsterRuntime>();
                if (monster == null || monster.IsDead) return false;
                monster.TakeDamage(damage, owner);
                break;
            }
            case AOGCombatTargetKind.Tower:
            {
                TowerHealth tower = target.GetComponentInParent<TowerHealth>();
                if (tower == null || tower.hp <= 0f) return false;
                tower.TakeDamage(damage);
                break;
            }
            case AOGCombatTargetKind.Nexus:
            {
                AOGNexusCore nexus = target.GetComponentInParent<AOGNexusCore>();
                if (nexus == null || nexus.IsDestroyed) return false;
                nexus.TakeDamage(damage);
                break;
            }
            case AOGCombatTargetKind.Boss:
            {
                AOGNeutralBossAI boss = target.GetComponentInParent<AOGNeutralBossAI>();
                if (boss == null || boss.IsDead) return false;
                boss.TakeDamage(damage, owner);
                break;
            }
        }

        AOGCombatEvents.RaiseBasicAttackHit(new AOGCombatHitEvent
        {
            source = owner,
            target = target.gameObject,
            damage = damage,
            basicAttack = true,
            abilityId = "basic_attack",
            targetKind = targetKind
        });

        SpawnImpact();
        return true;
    }

    private void Resolve(bool hit)
    {
        if (resolved)
            return;
        resolved = true;
        completed?.Invoke(hit);
        Destroy(gameObject);
    }

    private void BuildVisual()
    {
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visual.name = "Visual";
        visual.transform.SetParent(transform, false);

        switch (style)
        {
            case AOGChampionProjectileStyle.MoonDagger:
                visual.transform.localScale = new Vector3(0.08f, 0.08f, 0.72f);
                speed = Mathf.Max(speed, 28f);
                break;
            case AOGChampionProjectileStyle.VoidArrow:
                visual.transform.localScale = new Vector3(0.05f, 0.05f, 0.95f);
                speed = Mathf.Max(speed, 34f);
                break;
            case AOGChampionProjectileStyle.SpiritOrb:
                visual.transform.localScale = Vector3.one * 0.24f;
                break;
            case AOGChampionProjectileStyle.FlameOrb:
                visual.transform.localScale = Vector3.one * 0.32f;
                speed = Mathf.Min(speed, 19f);
                break;
            case AOGChampionProjectileStyle.AstralLance:
                visual.transform.localScale = new Vector3(0.07f, 0.07f, 0.82f);
                speed = Mathf.Max(speed, 30f);
                break;
            default:
                visual.transform.localScale = Vector3.one * 0.20f;
                break;
        }

        Collider collider = visual.GetComponent<Collider>();
        if (collider != null) Destroy(collider);

        Renderer renderer = visual.GetComponent<Renderer>();
        if (renderer != null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            Material material = new Material(shader) { color = accentColor };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", accentColor);
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", accentColor * 4f);
            }
            renderer.sharedMaterial = material;
        }

        trail = gameObject.AddComponent<LineRenderer>();
        trail.positionCount = 2;
        trail.startWidth = style == AOGChampionProjectileStyle.FlameOrb ? 0.16f : 0.10f;
        trail.endWidth = 0.015f;
        trail.startColor = accentColor;
        trail.endColor = new Color(accentColor.r, accentColor.g, accentColor.b, 0f);
        Shader trailShader = Shader.Find("Universal Render Pipeline/Unlit");
        if (trailShader == null) trailShader = Shader.Find("Unlit/Color");
        if (trailShader != null)
            trail.material = new Material(trailShader) { color = accentColor };
    }

    private void SpawnImpact()
    {
        AOGAbilityVisuals.CreateRing("Champion_Projectile_Impact", transform.position + Vector3.up * 0.03f, impactRadius > 0f ? impactRadius : 0.65f, accentColor, 0.05f);
    }

    private static float TargetHeight(AOGCombatTargetKind kind)
    {
        if (kind == AOGCombatTargetKind.Tower) return 1.8f;
        if (kind == AOGCombatTargetKind.Nexus) return 2.3f;
        if (kind == AOGCombatTargetKind.Boss) return 1.5f;
        return 0.9f;
    }
}
