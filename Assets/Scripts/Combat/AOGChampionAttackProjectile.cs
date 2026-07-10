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
    private TrailRenderer trail;
    private Transform visualRoot;
    private float pulseOffset;

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
        pulseOffset = UnityEngine.Random.Range(0f,10f);
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
        Vector3 toTarget = destination - transform.position;
        if (toTarget.sqrMagnitude > 0.001f)
            transform.forward = Vector3.Slerp(transform.forward,toTarget.normalized,Mathf.Clamp01(Time.deltaTime*18f));

        transform.position = Vector3.MoveTowards(transform.position, destination, speed * Time.deltaTime);

        if (visualRoot != null)
        {
            if (style == AOGChampionProjectileStyle.SpiritOrb)
                visualRoot.localPosition = new Vector3(Mathf.Sin((Time.time+pulseOffset)*14f)*0.10f,Mathf.Cos((Time.time+pulseOffset)*11f)*0.08f,0f);
            else if (style == AOGChampionProjectileStyle.FlameOrb)
            {
                float pulse = 1f + Mathf.Sin((Time.time+pulseOffset)*18f)*0.10f;
                visualRoot.localScale = Vector3.one * pulse;
            }
            else if (style == AOGChampionProjectileStyle.AstralLance)
                visualRoot.Rotate(Vector3.forward,220f*Time.deltaTime,Space.Self);
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
        visualRoot = new GameObject("VisualRoot").transform;
        visualRoot.SetParent(transform,false);

        GameObject core;
        switch (style)
        {
            case AOGChampionProjectileStyle.MoonDagger:
                core = GameObject.CreatePrimitive(PrimitiveType.Cube);
                core.transform.localScale = new Vector3(0.07f,0.28f,0.82f);
                core.transform.localRotation = Quaternion.Euler(0f,0f,24f);
                speed = Mathf.Max(speed,28f);
                AddSideShard(new Vector3(0.16f,0f,-0.05f),new Vector3(0.035f,0.18f,0.46f),-18f);
                break;
            case AOGChampionProjectileStyle.VoidArrow:
                core = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                core.transform.localScale = new Vector3(0.055f,0.48f,0.055f);
                core.transform.localRotation = Quaternion.Euler(90f,0f,0f);
                speed = Mathf.Max(speed,34f);
                AddSideShard(new Vector3(-0.10f,0f,-0.26f),new Vector3(0.025f,0.13f,0.30f),22f);
                AddSideShard(new Vector3(0.10f,0f,-0.26f),new Vector3(0.025f,0.13f,0.30f),-22f);
                break;
            case AOGChampionProjectileStyle.SpiritOrb:
                core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                core.transform.localScale = Vector3.one*0.28f;
                AddSpiritWisp(new Vector3(0.28f,0.06f,0f),0.11f);
                AddSpiritWisp(new Vector3(-0.22f,-0.08f,0.05f),0.09f);
                break;
            case AOGChampionProjectileStyle.FlameOrb:
                core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                core.transform.localScale = Vector3.one*0.36f;
                speed = Mathf.Min(speed,19f);
                AddFlameFin(new Vector3(0f,0.18f,-0.18f),18f);
                AddFlameFin(new Vector3(0f,-0.18f,-0.18f),-18f);
                break;
            case AOGChampionProjectileStyle.AstralLance:
                core = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                core.transform.localScale = new Vector3(0.065f,0.56f,0.065f);
                core.transform.localRotation = Quaternion.Euler(90f,0f,0f);
                speed = Mathf.Max(speed,30f);
                BuildAstralCross();
                break;
            default:
                core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                core.transform.localScale = Vector3.one*0.22f;
                break;
        }

        core.name = style + "_Core";
        core.transform.SetParent(visualRoot,false);
        RemoveCollider(core);
        ApplyMaterial(core,accentColor,4.2f);

        trail = gameObject.AddComponent<TrailRenderer>();
        trail.time = TrailTime();
        trail.startWidth = TrailWidth();
        trail.endWidth = 0.01f;
        trail.minVertexDistance = 0.035f;
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        trail.receiveShadows = false;
        trail.startColor = Color.Lerp(accentColor,Color.white,0.20f);
        trail.endColor = new Color(accentColor.r,accentColor.g,accentColor.b,0f);
        Shader trailShader = Shader.Find("Universal Render Pipeline/Unlit");
        if (trailShader == null) trailShader = Shader.Find("Unlit/Color");
        if (trailShader != null)
        {
            Material trailMat = new Material(trailShader) { color=accentColor };
            if (trailMat.HasProperty("_BaseColor")) trailMat.SetColor("_BaseColor",accentColor);
            trail.sharedMaterial = trailMat;
        }
    }

    private void AddSideShard(Vector3 localPosition,Vector3 scale,float roll)
    {
        GameObject shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
        shard.name = "Projectile_Side_Shard";
        shard.transform.SetParent(visualRoot,false);
        shard.transform.localPosition = localPosition;
        shard.transform.localRotation = Quaternion.Euler(0f,0f,roll);
        shard.transform.localScale = scale;
        RemoveCollider(shard);
        ApplyMaterial(shard,Color.Lerp(accentColor,Color.white,0.18f),3.5f);
    }

    private void AddSpiritWisp(Vector3 localPosition,float size)
    {
        GameObject wisp = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        wisp.name = "Spirit_Wisp";
        wisp.transform.SetParent(visualRoot,false);
        wisp.transform.localPosition = localPosition;
        wisp.transform.localScale = Vector3.one*size;
        RemoveCollider(wisp);
        ApplyMaterial(wisp,Color.Lerp(accentColor,Color.white,0.38f),4.8f);
    }

    private void AddFlameFin(Vector3 localPosition,float roll)
    {
        GameObject fin = GameObject.CreatePrimitive(PrimitiveType.Cube);
        fin.name = "Flame_Fin";
        fin.transform.SetParent(visualRoot,false);
        fin.transform.localPosition = localPosition;
        fin.transform.localRotation = Quaternion.Euler(0f,0f,roll);
        fin.transform.localScale = new Vector3(0.06f,0.34f,0.28f);
        RemoveCollider(fin);
        ApplyMaterial(fin,Color.Lerp(accentColor,new Color(1f,0.55f,0.08f),0.45f),4.6f);
    }

    private void BuildAstralCross()
    {
        for (int i=0;i<2;i++)
        {
            GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cube);
            line.name = "Astral_Lance_Fin";
            line.transform.SetParent(visualRoot,false);
            line.transform.localRotation = Quaternion.Euler(0f,0f,i==0?45f:-45f);
            line.transform.localScale = new Vector3(0.035f,0.34f,0.035f);
            RemoveCollider(line);
            ApplyMaterial(line,Color.Lerp(accentColor,Color.white,0.52f),4.2f);
        }
    }

    private float TrailWidth()
    {
        switch(style)
        {
            case AOGChampionProjectileStyle.FlameOrb: return 0.22f;
            case AOGChampionProjectileStyle.SpiritOrb: return 0.17f;
            case AOGChampionProjectileStyle.AstralLance: return 0.09f;
            case AOGChampionProjectileStyle.VoidArrow: return 0.07f;
            default: return 0.11f;
        }
    }

    private float TrailTime()
    {
        switch(style)
        {
            case AOGChampionProjectileStyle.SpiritOrb: return 0.42f;
            case AOGChampionProjectileStyle.FlameOrb: return 0.34f;
            case AOGChampionProjectileStyle.VoidArrow: return 0.16f;
            default: return 0.24f;
        }
    }

    private void SpawnImpact()
    {
        float radius = impactRadius > 0f ? impactRadius : 0.65f;
        GameObject ring = AOGAbilityVisuals.CreateRing("Champion_Projectile_Impact",transform.position+Vector3.up*0.03f,radius,accentColor,style==AOGChampionProjectileStyle.FlameOrb?0.12f:0.06f);
        Destroy(ring,style==AOGChampionProjectileStyle.FlameOrb?0.52f:0.32f);

        if (style == AOGChampionProjectileStyle.SpiritOrb)
        {
            for (int i=0;i<3;i++)
                SpawnImpactOrb(transform.position+Random.insideUnitSphere*0.35f,0.11f+0.03f*i,Color.Lerp(accentColor,Color.white,0.35f),0.36f);
        }
        else if (style == AOGChampionProjectileStyle.FlameOrb)
        {
            for (int i=0;i<4;i++)
                SpawnImpactShard(transform.position,Quaternion.Euler(Random.Range(-30f,30f),i*90f,Random.Range(-40f,40f)),new Vector3(0.05f,0.72f,0.05f),Color.Lerp(accentColor,new Color(1f,0.55f,0.08f),0.45f),0.40f);
        }
        else if (style == AOGChampionProjectileStyle.AstralLance)
        {
            for (int i=0;i<4;i++)
                SpawnImpactShard(transform.position,Quaternion.Euler(0f,i*45f,45f),new Vector3(0.035f,0.65f,0.035f),Color.Lerp(accentColor,Color.white,0.45f),0.34f);
        }
    }

    private static void SpawnImpactOrb(Vector3 position,float size,Color color,float life)
    {
        GameObject go=GameObject.CreatePrimitive(PrimitiveType.Sphere);go.name="Projectile_Impact_Wisp";go.transform.position=position;go.transform.localScale=Vector3.one*size;RemoveCollider(go);ApplyMaterial(go,color,4f);Destroy(go,life);
    }

    private static void SpawnImpactShard(Vector3 position,Quaternion rotation,Vector3 scale,Color color,float life)
    {
        GameObject go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name="Projectile_Impact_Shard";go.transform.position=position;go.transform.rotation=rotation;go.transform.localScale=scale;RemoveCollider(go);ApplyMaterial(go,color,4f);Destroy(go,life);
    }

    private static void ApplyMaterial(GameObject go,Color color,float emission)
    {
        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer == null) return;
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        Material material = new Material(shader) { color=color };
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor",color);
        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor",color*emission);
        }
        renderer.sharedMaterial = material;
    }

    private static void RemoveCollider(GameObject go)
    {
        Collider collider = go.GetComponent<Collider>();
        if (collider != null) Destroy(collider);
    }

    private static float TargetHeight(AOGCombatTargetKind kind)
    {
        if (kind == AOGCombatTargetKind.Tower) return 1.8f;
        if (kind == AOGCombatTargetKind.Nexus) return 2.3f;
        if (kind == AOGCombatTargetKind.Boss) return 1.5f;
        return 0.9f;
    }
}
