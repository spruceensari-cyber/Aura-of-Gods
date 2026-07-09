using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lightweight procedural presentation rig for prototype champions.
/// It creates a readable original silhouette and combat motion without external animation clips.
/// A production rig/Animator can replace this component later without touching champion gameplay code.
/// </summary>
public class AOGProceduralChampionRig : MonoBehaviour
{
    private const string PresentationRootName = "AOG_Procedural_Champion_Presentation";

    private Champion champion;
    private ChampionController controller;
    private Transform presentationRoot;
    private Transform hips;
    private Transform torso;
    private Transform head;
    private Transform leftArm;
    private Transform rightArm;
    private Transform leftLeg;
    private Transform rightLeg;
    private Transform leftBlade;
    private Transform rightBlade;
    private Transform backVeilA;
    private Transform backVeilB;

    private Vector3 lastPosition;
    private float locomotionPhase;
    private float attackPhase;
    private float castPhase;
    private float abilityPulse;
    private readonly List<ChampionAbility> boundAbilities = new();

    private Quaternion leftArmBase;
    private Quaternion rightArmBase;
    private Quaternion leftLegBase;
    private Quaternion rightLegBase;

    public bool IsBuilt => presentationRoot != null;

    public void BuildNyxaraPresentation()
    {
        if (presentationRoot != null)
            return;

        champion = GetComponent<Champion>();
        controller = GetComponent<ChampionController>();

        Transform existing = transform.Find(PresentationRootName);
        if (existing != null)
        {
            presentationRoot = existing;
            CacheNamedParts();
            BindEvents();
            return;
        }

        Material obsidian = CreateMaterial("Nyxara_Obsidian", new Color(0.055f, 0.065f, 0.10f), 0f);
        Material violet = CreateMaterial("Nyxara_Violet", new Color(0.31f, 0.10f, 0.62f), 0.35f);
        Material cyan = CreateMaterial("Nyxara_Rift_Cyan", new Color(0.08f, 0.68f, 0.92f), 1.2f);
        Material skin = CreateMaterial("Nyxara_Skin", new Color(0.64f, 0.48f, 0.52f), 0f);
        Material steel = CreateMaterial("Nyxara_Rift_Steel", new Color(0.48f, 0.58f, 0.72f), 0.2f);

        presentationRoot = new GameObject(PresentationRootName).transform;
        presentationRoot.SetParent(transform, false);
        presentationRoot.localPosition = Vector3.zero;

        hips = CreatePart("Hips", PrimitiveType.Capsule, presentationRoot,
            new Vector3(0f, 1.02f, 0f), new Vector3(0.58f, 0.32f, 0.42f), Quaternion.identity, obsidian);

        torso = CreatePart("Torso", PrimitiveType.Capsule, hips,
            new Vector3(0f, 0.78f, 0f), new Vector3(0.68f, 0.74f, 0.46f), Quaternion.identity, obsidian);

        CreatePart("Chest_Rift_Core", PrimitiveType.Sphere, torso,
            new Vector3(0f, 0.18f, 0.35f), new Vector3(0.20f, 0.26f, 0.10f), Quaternion.identity, cyan);

        CreatePart("Shoulder_Left", PrimitiveType.Sphere, torso,
            new Vector3(-0.48f, 0.26f, 0f), new Vector3(0.26f, 0.22f, 0.30f), Quaternion.identity, violet);
        CreatePart("Shoulder_Right", PrimitiveType.Sphere, torso,
            new Vector3(0.48f, 0.26f, 0f), new Vector3(0.26f, 0.22f, 0.30f), Quaternion.identity, violet);

        head = CreatePart("Head", PrimitiveType.Sphere, torso,
            new Vector3(0f, 0.93f, 0f), new Vector3(0.40f, 0.48f, 0.38f), Quaternion.identity, skin);

        CreatePart("Mask", PrimitiveType.Cube, head,
            new Vector3(0f, 0.02f, 0.32f), new Vector3(0.48f, 0.15f, 0.08f), Quaternion.Euler(0f, 0f, 6f), obsidian);
        CreatePart("Eye_Rift", PrimitiveType.Cube, head,
            new Vector3(0.12f, 0.03f, 0.37f), new Vector3(0.08f, 0.04f, 0.03f), Quaternion.identity, cyan);

        leftArm = CreatePart("Arm_Left", PrimitiveType.Capsule, torso,
            new Vector3(-0.63f, -0.05f, 0f), new Vector3(0.20f, 0.62f, 0.20f), Quaternion.Euler(0f, 0f, -10f), obsidian);
        rightArm = CreatePart("Arm_Right", PrimitiveType.Capsule, torso,
            new Vector3(0.63f, -0.05f, 0f), new Vector3(0.20f, 0.62f, 0.20f), Quaternion.Euler(0f, 0f, 10f), obsidian);

        leftLeg = CreatePart("Leg_Left", PrimitiveType.Capsule, hips,
            new Vector3(-0.26f, -0.72f, 0f), new Vector3(0.24f, 0.75f, 0.25f), Quaternion.identity, obsidian);
        rightLeg = CreatePart("Leg_Right", PrimitiveType.Capsule, hips,
            new Vector3(0.26f, -0.72f, 0f), new Vector3(0.24f, 0.75f, 0.25f), Quaternion.identity, obsidian);

        CreatePart("Boot_Left", PrimitiveType.Cube, leftLeg,
            new Vector3(0f, -0.76f, 0.14f), new Vector3(0.28f, 0.18f, 0.46f), Quaternion.identity, violet);
        CreatePart("Boot_Right", PrimitiveType.Cube, rightLeg,
            new Vector3(0f, -0.76f, 0.14f), new Vector3(0.28f, 0.18f, 0.46f), Quaternion.identity, violet);

        leftBlade = CreateBlade("Rift_Blade_Left", leftArm, new Vector3(0f, -0.74f, 0.12f), steel, cyan, -16f);
        rightBlade = CreateBlade("Rift_Blade_Right", rightArm, new Vector3(0f, -0.74f, 0.12f), steel, cyan, 16f);

        backVeilA = CreatePart("Back_Veil_A", PrimitiveType.Cube, torso,
            new Vector3(-0.22f, -0.15f, -0.34f), new Vector3(0.18f, 1.15f, 0.05f), Quaternion.Euler(12f, 0f, 8f), violet);
        backVeilB = CreatePart("Back_Veil_B", PrimitiveType.Cube, torso,
            new Vector3(0.22f, -0.15f, -0.34f), new Vector3(0.18f, 1.15f, 0.05f), Quaternion.Euler(12f, 0f, -8f), violet);

        CreatePart("Crown_Rift_Left", PrimitiveType.Cube, head,
            new Vector3(-0.22f, 0.40f, -0.02f), new Vector3(0.07f, 0.44f, 0.07f), Quaternion.Euler(0f, 0f, -28f), cyan);
        CreatePart("Crown_Rift_Right", PrimitiveType.Cube, head,
            new Vector3(0.22f, 0.40f, -0.02f), new Vector3(0.07f, 0.44f, 0.07f), Quaternion.Euler(0f, 0f, 28f), cyan);

        leftArmBase = leftArm.localRotation;
        rightArmBase = rightArm.localRotation;
        leftLegBase = leftLeg.localRotation;
        rightLegBase = rightLeg.localRotation;
        lastPosition = transform.position;

        BindEvents();
    }

    private void CacheNamedParts()
    {
        hips = FindDeep(presentationRoot, "Hips");
        torso = FindDeep(presentationRoot, "Torso");
        head = FindDeep(presentationRoot, "Head");
        leftArm = FindDeep(presentationRoot, "Arm_Left");
        rightArm = FindDeep(presentationRoot, "Arm_Right");
        leftLeg = FindDeep(presentationRoot, "Leg_Left");
        rightLeg = FindDeep(presentationRoot, "Leg_Right");
        leftBlade = FindDeep(presentationRoot, "Rift_Blade_Left");
        rightBlade = FindDeep(presentationRoot, "Rift_Blade_Right");
        backVeilA = FindDeep(presentationRoot, "Back_Veil_A");
        backVeilB = FindDeep(presentationRoot, "Back_Veil_B");

        if (leftArm != null) leftArmBase = leftArm.localRotation;
        if (rightArm != null) rightArmBase = rightArm.localRotation;
        if (leftLeg != null) leftLegBase = leftLeg.localRotation;
        if (rightLeg != null) rightLegBase = rightLeg.localRotation;
        lastPosition = transform.position;
    }

    private void BindEvents()
    {
        controller = GetComponent<ChampionController>();
        if (controller != null)
        {
            controller.OnBasicAttackWindup -= HandleAttackWindup;
            controller.OnBasicAttackResolved -= HandleAttackResolved;
            controller.OnBasicAttackWindup += HandleAttackWindup;
            controller.OnBasicAttackResolved += HandleAttackResolved;
        }

        foreach (ChampionAbility ability in GetComponents<ChampionAbility>())
        {
            ability.OnCastStarted -= HandleCastStarted;
            ability.OnCastCompleted -= HandleCastCompleted;
            ability.OnCastStarted += HandleCastStarted;
            ability.OnCastCompleted += HandleCastCompleted;
            if (!boundAbilities.Contains(ability))
                boundAbilities.Add(ability);
        }
    }

    private void OnDestroy()
    {
        if (controller != null)
        {
            controller.OnBasicAttackWindup -= HandleAttackWindup;
            controller.OnBasicAttackResolved -= HandleAttackResolved;
        }

        foreach (ChampionAbility ability in boundAbilities)
        {
            if (ability == null)
                continue;
            ability.OnCastStarted -= HandleCastStarted;
            ability.OnCastCompleted -= HandleCastCompleted;
        }
    }

    private void Update()
    {
        if (presentationRoot == null)
            return;

        float speed = (transform.position - lastPosition).magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
        lastPosition = transform.position;
        float move01 = Mathf.Clamp01(speed / 5f);

        locomotionPhase += Time.deltaTime * Mathf.Lerp(2.2f, 9.5f, move01);
        attackPhase = Mathf.MoveTowards(attackPhase, 0f, Time.deltaTime * 3.8f);
        castPhase = Mathf.MoveTowards(castPhase, 0f, Time.deltaTime * 2.6f);
        abilityPulse = Mathf.MoveTowards(abilityPulse, 0f, Time.deltaTime * 1.8f);

        float stride = Mathf.Sin(locomotionPhase) * 28f * move01;
        float counterStride = Mathf.Sin(locomotionPhase + Mathf.PI) * 28f * move01;
        float idleBreath = Mathf.Sin(Time.time * 2.1f) * 0.025f;

        if (hips != null)
            hips.localPosition = new Vector3(0f, 1.02f + idleBreath + Mathf.Abs(Mathf.Sin(locomotionPhase)) * 0.035f * move01, 0f);

        if (leftLeg != null)
            leftLeg.localRotation = leftLegBase * Quaternion.Euler(stride, 0f, 0f);
        if (rightLeg != null)
            rightLeg.localRotation = rightLegBase * Quaternion.Euler(counterStride, 0f, 0f);

        float attackSlash = Mathf.Sin(attackPhase * Mathf.PI) * 92f;
        float castSpread = Mathf.Sin(castPhase * Mathf.PI) * 48f;

        if (leftArm != null)
            leftArm.localRotation = leftArmBase * Quaternion.Euler(counterStride * 0.55f - castSpread * 0.4f, 0f, attackSlash * 0.35f);
        if (rightArm != null)
            rightArm.localRotation = rightArmBase * Quaternion.Euler(stride * 0.55f - attackSlash - castSpread * 0.4f, 0f, -attackSlash * 0.25f);

        float veilSway = Mathf.Sin(Time.time * 3.4f + locomotionPhase * 0.25f) * (5f + move01 * 13f);
        if (backVeilA != null)
            backVeilA.localRotation = Quaternion.Euler(12f + move01 * 18f, 0f, 8f + veilSway);
        if (backVeilB != null)
            backVeilB.localRotation = Quaternion.Euler(12f + move01 * 18f, 0f, -8f - veilSway);

        float bladeSpin = Time.deltaTime * (50f + move01 * 120f + abilityPulse * 260f);
        leftBlade?.Rotate(Vector3.up, bladeSpin, Space.Self);
        rightBlade?.Rotate(Vector3.up, -bladeSpin, Space.Self);

        if (torso != null)
            torso.localRotation = Quaternion.Euler(0f, Mathf.Sin(Time.time * 1.4f) * 2f, -stride * 0.08f);
    }

    private void HandleAttackWindup()
    {
        attackPhase = 1f;
    }

    private void HandleAttackResolved()
    {
        abilityPulse = Mathf.Max(abilityPulse, 0.35f);
    }

    private void HandleCastStarted(ChampionAbility ability)
    {
        castPhase = 1f;
        abilityPulse = 1f;
    }

    private void HandleCastCompleted(ChampionAbility ability)
    {
        if (ability != null && ability.Key == AbilityKey.R)
            StartCoroutine(UltimateRigPulse());
    }

    private IEnumerator UltimateRigPulse()
    {
        Vector3 baseScale = presentationRoot.localScale;
        float elapsed = 0f;
        while (elapsed < 0.4f)
        {
            elapsed += Time.deltaTime;
            float pulse = 1f + Mathf.Sin((elapsed / 0.4f) * Mathf.PI) * 0.18f;
            presentationRoot.localScale = baseScale * pulse;
            yield return null;
        }
        presentationRoot.localScale = baseScale;
    }

    private Transform CreateBlade(string name, Transform parent, Vector3 localPosition, Material steel, Material glow, float angle)
    {
        Transform root = new GameObject(name).transform;
        root.SetParent(parent, false);
        root.localPosition = localPosition;
        root.localRotation = Quaternion.Euler(0f, 0f, angle);

        CreatePart("Blade_Core", PrimitiveType.Cube, root,
            new Vector3(0f, -0.40f, 0f), new Vector3(0.10f, 0.85f, 0.08f), Quaternion.Euler(0f, 0f, 12f), steel);
        CreatePart("Blade_Edge", PrimitiveType.Cube, root,
            new Vector3(0.10f, -0.42f, 0f), new Vector3(0.045f, 0.92f, 0.045f), Quaternion.Euler(0f, 0f, 12f), glow);
        CreatePart("Blade_Guard", PrimitiveType.Cube, root,
            new Vector3(0f, 0.02f, 0f), new Vector3(0.38f, 0.08f, 0.10f), Quaternion.Euler(0f, 0f, -10f), glow);

        return root;
    }

    private Transform CreatePart(string name, PrimitiveType type, Transform parent, Vector3 localPosition,
        Vector3 localScale, Quaternion localRotation, Material material)
    {
        GameObject obj = GameObject.CreatePrimitive(type);
        obj.name = name;
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = localPosition;
        obj.transform.localScale = localScale;
        obj.transform.localRotation = localRotation;

        Collider collider = obj.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);

        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = material;

        return obj.transform;
    }

    private Material CreateMaterial(string name, Color color, float emission)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material material = new Material(shader);
        material.name = name;
        material.color = color;

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);

        if (emission > 0f && material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * emission);
        }

        return material;
    }

    private Transform FindDeep(Transform root, string targetName)
    {
        if (root == null)
            return null;
        if (root.name == targetName)
            return root;

        foreach (Transform child in root)
        {
            Transform result = FindDeep(child, targetName);
            if (result != null)
                return result;
        }

        return null;
    }
}
