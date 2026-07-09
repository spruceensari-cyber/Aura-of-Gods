using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public static class AOGMinionVisualFactory
{
    private static readonly Dictionary<string, Material> MaterialCache = new Dictionary<string, Material>();

    public static AOGMinionProceduralAnimator Build(Minion minion)
    {
        if (minion == null)
            return null;

        Transform existing = minion.transform.Find("AOG_Minon_Visual");
        if (existing != null)
        {
            AOGMinionProceduralAnimator existingAnimator = minion.GetComponent<AOGMinionProceduralAnimator>();
            if (existingAnimator == null)
                existingAnimator = minion.gameObject.AddComponent<AOGMinionProceduralAnimator>();
            existingAnimator.ResolveParts(existing);
            return existingAnimator;
        }

        HideLegacyModelRenderers(minion.transform);

        Color teamColor = minion.team == MinionTeam.Blue
            ? new Color(0.12f, 0.47f, 1f, 1f)
            : new Color(1f, 0.16f, 0.22f, 1f);
        Color armorColor = minion.team == MinionTeam.Blue
            ? new Color(0.06f, 0.11f, 0.20f, 1f)
            : new Color(0.20f, 0.055f, 0.07f, 1f);
        Color neutralMetal = new Color(0.20f, 0.23f, 0.26f, 1f);

        Material armor = GetMaterial("MinionArmor_" + minion.team, armorColor, 0.46f, 0.48f, false);
        Material metal = GetMaterial("MinionMetal", neutralMetal, 0.62f, 0.62f, false);
        Material energy = GetMaterial("MinionEnergy_" + minion.team, teamColor, 0.32f, 0.08f, true);

        GameObject visualObject = new GameObject("AOG_Minon_Visual");
        visualObject.transform.SetParent(minion.transform, false);
        visualObject.transform.localPosition = Vector3.zero;
        Transform visual = visualObject.transform;

        AOGMinionProceduralAnimator animator = minion.GetComponent<AOGMinionProceduralAnimator>();
        if (animator == null)
            animator = minion.gameObject.AddComponent<AOGMinionProceduralAnimator>();

        switch (minion.role)
        {
            case MinionRole.Ranged:
                BuildRanged(visual, armor, metal, energy, animator);
                break;
            case MinionRole.Cannon:
                BuildCannon(visual, armor, metal, energy, animator);
                break;
            default:
                BuildMelee(visual, armor, metal, energy, animator);
                break;
        }

        animator.ResolveParts(visual);
        return animator;
    }

    private static void BuildMelee(Transform root, Material armor, Material metal, Material energy, AOGMinionProceduralAnimator animator)
    {
        Transform body = Create(PrimitiveType.Capsule, "Body", root, new Vector3(0f, 0.92f, 0f), new Vector3(0.56f, 0.72f, 0.48f), armor).transform;
        Transform head = Create(PrimitiveType.Sphere, "Head", root, new Vector3(0f, 1.82f, 0f), new Vector3(0.50f, 0.44f, 0.50f), armor).transform;
        Create(PrimitiveType.Cube, "Visor", head, new Vector3(0f, 0.03f, 0.42f), new Vector3(0.58f, 0.13f, 0.07f), energy);

        Transform armL = Create(PrimitiveType.Capsule, "Arm_L", root, new Vector3(-0.48f, 1.04f, 0.02f), new Vector3(0.18f, 0.52f, 0.18f), metal).transform;
        armL.localRotation = Quaternion.Euler(10f, 0f, 18f);
        Transform armR = Create(PrimitiveType.Capsule, "Arm_R", root, new Vector3(0.48f, 1.04f, 0.02f), new Vector3(0.18f, 0.52f, 0.18f), metal).transform;
        armR.localRotation = Quaternion.Euler(10f, 0f, -18f);

        Transform sword = Create(PrimitiveType.Cube, "Weapon", armR, new Vector3(0f, -0.52f, 0.26f), new Vector3(0.11f, 0.82f, 0.12f), energy).transform;
        sword.localRotation = Quaternion.Euler(18f, 0f, 0f);
        Transform shield = Create(PrimitiveType.Cylinder, "Shield", armL, new Vector3(0f, -0.28f, 0.22f), new Vector3(0.36f, 0.08f, 0.36f), armor).transform;
        shield.localRotation = Quaternion.Euler(90f, 0f, 0f);

        animator.body = body;
        animator.leftArm = armL;
        animator.rightArm = armR;
        animator.weapon = sword;
    }

    private static void BuildRanged(Transform root, Material armor, Material metal, Material energy, AOGMinionProceduralAnimator animator)
    {
        Transform body = Create(PrimitiveType.Capsule, "Body", root, new Vector3(0f, 0.90f, 0f), new Vector3(0.46f, 0.76f, 0.42f), armor).transform;
        Transform hood = Create(PrimitiveType.Sphere, "Head", root, new Vector3(0f, 1.82f, -0.02f), new Vector3(0.46f, 0.50f, 0.46f), armor).transform;
        Create(PrimitiveType.Sphere, "Eye", hood, new Vector3(0f, 0.02f, 0.38f), new Vector3(0.20f, 0.11f, 0.08f), energy);

        Transform armL = Create(PrimitiveType.Capsule, "Arm_L", root, new Vector3(-0.42f, 1.03f, 0.02f), new Vector3(0.15f, 0.50f, 0.15f), metal).transform;
        Transform armR = Create(PrimitiveType.Capsule, "Arm_R", root, new Vector3(0.42f, 1.03f, 0.02f), new Vector3(0.15f, 0.50f, 0.15f), metal).transform;

        Transform staff = Create(PrimitiveType.Cylinder, "Weapon", armR, new Vector3(0.08f, -0.42f, 0.18f), new Vector3(0.07f, 0.72f, 0.07f), metal).transform;
        staff.localRotation = Quaternion.Euler(20f, 0f, 0f);
        Create(PrimitiveType.Sphere, "Staff_Core", staff, new Vector3(0f, 1.05f, 0f), Vector3.one * 0.24f, energy);

        animator.body = body;
        animator.leftArm = armL;
        animator.rightArm = armR;
        animator.weapon = staff;
    }

    private static void BuildCannon(Transform root, Material armor, Material metal, Material energy, AOGMinionProceduralAnimator animator)
    {
        Transform chassis = Create(PrimitiveType.Cube, "Body", root, new Vector3(0f, 0.72f, 0f), new Vector3(1.15f, 0.62f, 1.35f), armor).transform;
        Transform turret = Create(PrimitiveType.Cylinder, "Turret", root, new Vector3(0f, 1.22f, 0f), new Vector3(0.58f, 0.24f, 0.58f), metal).transform;
        Transform barrel = Create(PrimitiveType.Cylinder, "Weapon", turret, new Vector3(0f, 0.10f, 0.68f), new Vector3(0.12f, 0.72f, 0.12f), metal).transform;
        barrel.localRotation = Quaternion.Euler(90f, 0f, 0f);
        Create(PrimitiveType.Sphere, "Core", turret, new Vector3(0f, 0.36f, 0f), Vector3.one * 0.34f, energy);

        Transform wheelL = Create(PrimitiveType.Cylinder, "Wheel_L", root, new Vector3(-0.70f, 0.42f, 0f), new Vector3(0.45f, 0.20f, 0.45f), metal).transform;
        wheelL.localRotation = Quaternion.Euler(0f, 0f, 90f);
        Transform wheelR = Create(PrimitiveType.Cylinder, "Wheel_R", root, new Vector3(0.70f, 0.42f, 0f), new Vector3(0.45f, 0.20f, 0.45f), metal).transform;
        wheelR.localRotation = Quaternion.Euler(0f, 0f, 90f);

        animator.body = chassis;
        animator.weapon = barrel;
        animator.leftArm = wheelL;
        animator.rightArm = wheelR;
        animator.isCannon = true;
    }

    private static GameObject Create(PrimitiveType type, string name, Transform parent, Vector3 localPosition, Vector3 localScale, Material material)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localScale = localScale;
        Renderer renderer = go.GetComponent<Renderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = ShadowCastingMode.On;
        renderer.receiveShadows = true;
        Collider collider = go.GetComponent<Collider>();
        if (collider != null)
            Object.Destroy(collider);
        return go;
    }

    private static Material GetMaterial(string key, Color color, float smoothness, float metallic, bool emission)
    {
        if (MaterialCache.TryGetValue(key, out Material cached) && cached != null)
            return cached;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        Material material = new Material(shader)
        {
            name = key,
            color = color,
            enableInstancing = true
        };
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
        if (emission && material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * 4.5f);
        }

        MaterialCache[key] = material;
        return material;
    }

    private static void HideLegacyModelRenderers(Transform root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
                continue;

            string lower = renderer.gameObject.name.ToLowerInvariant();
            if (lower.Contains("aog_hp_bar") || lower.Contains("objective_hp_bar") || lower.Contains("click_indicator"))
                continue;

            renderer.enabled = false;
        }
    }
}

public class AOGMinionProceduralAnimator : MonoBehaviour
{
    public Transform body;
    public Transform leftArm;
    public Transform rightArm;
    public Transform weapon;
    public bool isCannon;

    private Transform visualRoot;
    private Vector3 visualBasePosition;
    private Quaternion bodyBaseRotation;
    private Quaternion leftBaseRotation;
    private Quaternion rightBaseRotation;
    private Quaternion weaponBaseRotation;
    private float moveBlend;
    private float attackTimer;
    private float attackDuration = 0.45f;
    private float hitTimer;
    private float deathTimer;
    private bool dying;
    private int attackVariant;

    public void ResolveParts(Transform root)
    {
        visualRoot = root;
        visualBasePosition = root.localPosition;
        if (body == null) body = Find(root, "Body");
        if (leftArm == null) leftArm = Find(root, "Arm_L") ?? Find(root, "Wheel_L");
        if (rightArm == null) rightArm = Find(root, "Arm_R") ?? Find(root, "Wheel_R");
        if (weapon == null) weapon = Find(root, "Weapon");

        if (body != null) bodyBaseRotation = body.localRotation;
        if (leftArm != null) leftBaseRotation = leftArm.localRotation;
        if (rightArm != null) rightBaseRotation = rightArm.localRotation;
        if (weapon != null) weaponBaseRotation = weapon.localRotation;
    }

    public void SetMoving(bool moving, float normalizedSpeed)
    {
        float target = moving ? Mathf.Clamp01(normalizedSpeed) : 0f;
        moveBlend = Mathf.Lerp(moveBlend, target, 10f * Time.deltaTime);
    }

    public void PlayAttack()
    {
        attackVariant++;
        attackDuration = isCannon ? 0.58f : 0.44f;
        attackTimer = attackDuration;
    }

    public void PlayHit()
    {
        hitTimer = 0.18f;
    }

    public void PlayDeath()
    {
        if (dying)
            return;

        dying = true;
        deathTimer = 0f;
    }

    private void Update()
    {
        if (visualRoot == null)
            return;

        if (dying)
        {
            deathTimer += Time.deltaTime;
            float t = Mathf.Clamp01(deathTimer / 0.55f);
            visualRoot.localPosition = visualBasePosition + Vector3.down * t * 0.55f;
            visualRoot.localRotation = Quaternion.Euler(75f * t, 120f * t, 30f * t);
            visualRoot.localScale = Vector3.one * Mathf.Lerp(1f, 0.15f, t);
            return;
        }

        float cycle = Time.time * (isCannon ? 5f : 8.5f);
        visualRoot.localPosition = visualBasePosition + Vector3.up * Mathf.Abs(Mathf.Sin(cycle)) * 0.055f * moveBlend;

        if (body != null)
            body.localRotation = bodyBaseRotation * Quaternion.Euler(Mathf.Sin(cycle) * 3.5f * moveBlend, 0f, Mathf.Cos(cycle * 0.5f) * 2f * moveBlend);

        if (isCannon)
        {
            if (leftArm != null) leftArm.Rotate(Vector3.up, 240f * moveBlend * Time.deltaTime, Space.Self);
            if (rightArm != null) rightArm.Rotate(Vector3.up, -240f * moveBlend * Time.deltaTime, Space.Self);
        }
        else
        {
            float armSwing = Mathf.Sin(cycle) * 28f * moveBlend;
            if (leftArm != null) leftArm.localRotation = leftBaseRotation * Quaternion.Euler(armSwing, 0f, 0f);
            if (rightArm != null) rightArm.localRotation = rightBaseRotation * Quaternion.Euler(-armSwing, 0f, 0f);
        }

        if (attackTimer > 0f)
        {
            attackTimer -= Time.deltaTime;
            float t = 1f - attackTimer / Mathf.Max(0.01f, attackDuration);
            float arc = Mathf.Sin(t * Mathf.PI);

            if (isCannon)
            {
                if (weapon != null)
                    weapon.localPosition = new Vector3(weapon.localPosition.x, weapon.localPosition.y, Mathf.Lerp(0.68f, 0.42f, arc));
            }
            else
            {
                float sign = attackVariant % 2 == 0 ? 1f : -1f;
                if (rightArm != null)
                    rightArm.localRotation = rightBaseRotation * Quaternion.Euler(-arc * 70f, sign * arc * 20f, 0f);
                if (weapon != null)
                    weapon.localRotation = weaponBaseRotation * Quaternion.Euler(-arc * 55f, sign * arc * 25f, 0f);
            }
        }

        if (hitTimer > 0f)
        {
            hitTimer -= Time.deltaTime;
            float shake = Mathf.Sin(hitTimer * 80f) * 0.08f;
            visualRoot.localPosition += new Vector3(shake, 0f, 0f);
        }
    }

    private static Transform Find(Transform root, string name)
    {
        if (root == null) return null;
        if (root.name == name) return root;
        foreach (Transform child in root)
        {
            Transform result = Find(child, name);
            if (result != null) return result;
        }
        return null;
    }
}
