using System.Collections;
using UnityEngine;

public class AOGPremiumMeleeSwingRuntime : MonoBehaviour
{
    public Transform weaponRoot;
    public Transform visualRoot;
    public float anticipationDuration = 0.075f;
    public float swingDuration = 0.24f;
    public float recoveryDuration = 0.13f;
    public float lungeDistance = 0.24f;

    private TrailRenderer trail;
    private Quaternion weaponBaseRotation;
    private Vector3 visualBasePosition;
    private Quaternion visualBaseRotation;
    private int comboIndex;
    private bool swinging;
    private Color accent = new Color(0.52f,0.34f,1f,0.90f);

    private void Awake()
    {
        AOGActiveChampion active = GetComponent<AOGActiveChampion>();
        if (active != null)
            accent = active.accentColor;
        ResolveReferences();
    }

    public void PlaySwing()
    {
        if (!isActiveAndEnabled || swinging)
            return;

        ResolveReferences();
        comboIndex = (comboIndex + 1) % 3;
        StartCoroutine(SwingRoutine(comboIndex));
    }

    private void ResolveReferences()
    {
        if (visualRoot == null)
        {
            Animator animator = GetComponentInChildren<Animator>(true);
            if (animator != null && animator.transform != transform)
                visualRoot = animator.transform;

            if (visualRoot == null)
            {
                foreach (Transform child in transform)
                {
                    if (child.GetComponentInChildren<Renderer>(true) != null)
                    {
                        visualRoot = child;
                        break;
                    }
                }
            }
        }

        if (weaponRoot == null)
            weaponRoot = FindLikelyWeapon(transform);

        if (weaponRoot != null)
        {
            weaponBaseRotation = weaponRoot.localRotation;
            trail = weaponRoot.GetComponent<TrailRenderer>();
            if (trail == null)
                trail = BuildTrail(weaponRoot,accent);
        }

        if (visualRoot != null)
        {
            visualBasePosition = visualRoot.localPosition;
            visualBaseRotation = visualRoot.localRotation;
        }
    }

    private IEnumerator SwingRoutine(int combo)
    {
        swinging = true;

        Quaternion weaponStart = weaponRoot != null ? weaponRoot.localRotation : Quaternion.identity;
        Quaternion windupRotation = weaponBaseRotation;
        Quaternion endRotation;
        float torsoYaw;
        float torsoRoll;

        if (combo == 0)
        {
            windupRotation = weaponBaseRotation * Quaternion.Euler(18f,-32f,-42f);
            endRotation = weaponBaseRotation * Quaternion.Euler(-25f, 55f, 110f);
            torsoYaw = 26f;
            torsoRoll = -8f;
        }
        else if (combo == 1)
        {
            windupRotation = weaponBaseRotation * Quaternion.Euler(-16f,30f,38f);
            endRotation = weaponBaseRotation * Quaternion.Euler(35f, -45f, -125f);
            torsoYaw = -30f;
            torsoRoll = 10f;
        }
        else
        {
            windupRotation = weaponBaseRotation * Quaternion.Euler(42f,-10f,-28f);
            endRotation = weaponBaseRotation * Quaternion.Euler(-60f, 18f, 155f);
            torsoYaw = 12f;
            torsoRoll = -16f;
        }

        Quaternion visualStartRot = visualRoot != null ? visualRoot.localRotation : Quaternion.identity;
        Quaternion anticipationRot = visualStartRot * Quaternion.Euler(4f,-torsoYaw*0.45f,-torsoRoll*0.35f);
        Quaternion visualEndRot = visualStartRot * Quaternion.Euler(-8f, torsoYaw, torsoRoll);
        Vector3 startPos = visualRoot != null ? visualRoot.localPosition : Vector3.zero;
        Vector3 anticipationPos = startPos - Vector3.forward * lungeDistance * 0.22f;
        Vector3 endPos = startPos + Vector3.forward * lungeDistance;

        float elapsed = 0f;
        while (elapsed < anticipationDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f,anticipationDuration));
            float eased = t*t;

            if (weaponRoot != null)
                weaponRoot.localRotation = Quaternion.Slerp(weaponStart,windupRotation,eased);
            if (visualRoot != null)
            {
                visualRoot.localRotation = Quaternion.Slerp(visualStartRot,anticipationRot,eased);
                visualRoot.localPosition = Vector3.Lerp(startPos,anticipationPos,eased);
            }
            yield return null;
        }

        if (trail != null)
        {
            trail.Clear();
            trail.emitting = true;
        }

        elapsed = 0f;
        while (elapsed < swingDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f,swingDuration));
            float eased = Mathf.Sin(t * Mathf.PI * 0.5f);

            if (weaponRoot != null)
                weaponRoot.localRotation = Quaternion.Slerp(windupRotation, endRotation, eased);

            if (visualRoot != null)
            {
                visualRoot.localRotation = Quaternion.Slerp(anticipationRot, visualEndRot, eased);
                visualRoot.localPosition = Vector3.Lerp(anticipationPos, endPos, eased);
            }

            yield return null;
        }

        if (trail != null)
            trail.emitting = false;

        elapsed = 0f;
        while (elapsed < recoveryDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f,recoveryDuration));
            float eased = 1f - Mathf.Pow(1f-t,3f);

            if (weaponRoot != null)
                weaponRoot.localRotation = Quaternion.Slerp(endRotation, weaponBaseRotation, eased);
            if (visualRoot != null)
            {
                visualRoot.localRotation = Quaternion.Slerp(visualEndRot, visualBaseRotation, eased);
                visualRoot.localPosition = Vector3.Lerp(endPos, visualBasePosition, eased);
            }

            yield return null;
        }

        if (weaponRoot != null)
            weaponRoot.localRotation = weaponBaseRotation;
        if (visualRoot != null)
        {
            visualRoot.localRotation = visualBaseRotation;
            visualRoot.localPosition = visualBasePosition;
        }

        swinging = false;
    }

    private static Transform FindLikelyWeapon(Transform root)
    {
        string[] tokens = { "sword", "blade", "weapon", "katana", "sabre", "axe", "dagger", "staff", "lance", "scythe" };
        Transform[] all = root.GetComponentsInChildren<Transform>(true);
        foreach (Transform t in all)
        {
            string lower = t.name.ToLowerInvariant();
            foreach (string token in tokens)
                if (lower.Contains(token))
                    return t;
        }
        return null;
    }

    private static TrailRenderer BuildTrail(Transform weapon,Color accent)
    {
        TrailRenderer trail = weapon.gameObject.AddComponent<TrailRenderer>();
        trail.time = 0.22f;
        trail.startWidth = 0.30f;
        trail.endWidth = 0.015f;
        trail.minVertexDistance = 0.035f;
        trail.emitting = false;
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        trail.receiveShadows = false;
        trail.alignment = LineAlignment.TransformZ;

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");

        Material material = new Material(shader);
        Color c = new Color(accent.r,accent.g,accent.b,0.90f);
        material.color = c;
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", c);
        trail.material = material;
        trail.startColor = Color.Lerp(c,Color.white,0.22f);
        trail.endColor = new Color(c.r, c.g, c.b, 0f);
        return trail;
    }
}

public class AOGPremiumMeleeSwingBootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindFirstObjectByType<AOGPremiumMeleeSwingBootstrap>() != null)
            return;
        GameObject host = new GameObject("AOG_Premium_Melee_Swing_Bootstrap");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGPremiumMeleeSwingBootstrap>();
    }

    private float nextScan;
    private void Update()
    {
        if (Time.unscaledTime < nextScan)
            return;
        nextScan = Time.unscaledTime + 0.75f;

        foreach (AOGCharacterStats hero in FindObjectsByType<AOGCharacterStats>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (hero == null)
                continue;

            bool melee = hero.attackRange <= 3.5f;
            if (melee && hero.GetComponent<AOGPremiumMeleeSwingRuntime>() == null)
                hero.gameObject.AddComponent<AOGPremiumMeleeSwingRuntime>();
        }
    }
}
