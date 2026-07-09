using System.Collections;
using UnityEngine;

public class AOGPremiumMeleeSwingRuntime : MonoBehaviour
{
    public Transform weaponRoot;
    public Transform visualRoot;
    public float swingDuration = 0.26f;
    public float recoveryDuration = 0.12f;
    public float lungeDistance = 0.22f;

    private TrailRenderer trail;
    private Quaternion weaponBaseRotation;
    private Vector3 visualBasePosition;
    private int comboIndex;
    private bool swinging;

    private void Awake()
    {
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
                trail = BuildTrail(weaponRoot);
        }

        if (visualRoot != null)
            visualBasePosition = visualRoot.localPosition;
    }

    private IEnumerator SwingRoutine(int combo)
    {
        swinging = true;
        if (trail != null)
            trail.emitting = true;

        Quaternion startRotation = weaponRoot != null ? weaponRoot.localRotation : Quaternion.identity;
        Quaternion endRotation;
        float torsoYaw;
        float torsoRoll;

        if (combo == 0)
        {
            endRotation = weaponBaseRotation * Quaternion.Euler(-25f, 55f, 110f);
            torsoYaw = 26f;
            torsoRoll = -8f;
        }
        else if (combo == 1)
        {
            endRotation = weaponBaseRotation * Quaternion.Euler(35f, -45f, -125f);
            torsoYaw = -30f;
            torsoRoll = 10f;
        }
        else
        {
            endRotation = weaponBaseRotation * Quaternion.Euler(-60f, 18f, 155f);
            torsoYaw = 12f;
            torsoRoll = -16f;
        }

        Quaternion visualStartRot = visualRoot != null ? visualRoot.localRotation : Quaternion.identity;
        Quaternion visualEndRot = visualStartRot * Quaternion.Euler(-8f, torsoYaw, torsoRoll);
        Vector3 startPos = visualRoot != null ? visualRoot.localPosition : Vector3.zero;
        Vector3 endPos = startPos + Vector3.forward * lungeDistance;

        float elapsed = 0f;
        while (elapsed < swingDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / swingDuration);
            float eased = Mathf.Sin(t * Mathf.PI);

            if (weaponRoot != null)
                weaponRoot.localRotation = Quaternion.Slerp(startRotation, endRotation, eased);

            if (visualRoot != null)
            {
                visualRoot.localRotation = Quaternion.Slerp(visualStartRot, visualEndRot, eased);
                visualRoot.localPosition = Vector3.Lerp(startPos, endPos, eased);
            }

            yield return null;
        }

        elapsed = 0f;
        while (elapsed < recoveryDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / recoveryDuration);

            if (weaponRoot != null)
                weaponRoot.localRotation = Quaternion.Slerp(weaponRoot.localRotation, weaponBaseRotation, t);

            if (visualRoot != null)
            {
                visualRoot.localRotation = Quaternion.Slerp(visualRoot.localRotation, visualStartRot, t);
                visualRoot.localPosition = Vector3.Lerp(visualRoot.localPosition, visualBasePosition, t);
            }

            yield return null;
        }

        if (trail != null)
            trail.emitting = false;

        swinging = false;
    }

    private static Transform FindLikelyWeapon(Transform root)
    {
        string[] tokens = { "sword", "blade", "weapon", "katana", "sabre", "axe", "dagger", "staff" };
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

    private static TrailRenderer BuildTrail(Transform weapon)
    {
        TrailRenderer trail = weapon.gameObject.AddComponent<TrailRenderer>();
        trail.time = 0.20f;
        trail.startWidth = 0.28f;
        trail.endWidth = 0.02f;
        trail.minVertexDistance = 0.04f;
        trail.emitting = false;
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        trail.receiveShadows = false;

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");

        Material material = new Material(shader);
        Color c = new Color(0.52f, 0.34f, 1f, 0.90f);
        material.color = c;
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", c);
        trail.material = material;
        trail.startColor = c;
        trail.endColor = new Color(c.r, c.g, c.b, 0f);
        return trail;
    }
}

public class AOGPremiumMeleeSwingBootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        GameObject host = new GameObject("AOG_Premium_Melee_Swing_Bootstrap");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGPremiumMeleeSwingBootstrap>();
    }

    private void Update()
    {
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
