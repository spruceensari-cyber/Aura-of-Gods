using UnityEngine;

[DefaultExecutionOrder(25)]
public class AOGAutoAttackPresentationRuntime : MonoBehaviour
{
    private Transform visualRoot;
    private Vector3 lastPosition;
    private Vector3 basePosition;
    private Quaternion baseRotation;
    private Vector3 baseScale;
    private float attackTimer;
    private float hitTimer;
    private int attackSide = 1;
    private AOGCharacterStats stats;

    private void Awake()
    {
        stats = GetComponent<AOGCharacterStats>();
        visualRoot = ResolveVisualRoot();
        lastPosition = transform.position;
        if (visualRoot != null)
        {
            basePosition = visualRoot.localPosition;
            baseRotation = visualRoot.localRotation;
            baseScale = visualRoot.localScale;
        }
    }

    public void PlayAttack()
    {
        attackSide *= -1;
        attackTimer = 0.34f;

        AOGPremiumMeleeSwingRuntime swing = GetComponent<AOGPremiumMeleeSwingRuntime>();
        if (swing != null)
            swing.PlaySwing();
    }

    public void PlayHit()
    {
        hitTimer = 0.16f;
    }

    private void Update()
    {
        if (stats != null && stats.IsDead) return;
        if (visualRoot == null)
        {
            visualRoot = ResolveVisualRoot();
            if (visualRoot == null) return;
            basePosition = visualRoot.localPosition;
            baseRotation = visualRoot.localRotation;
            baseScale = visualRoot.localScale;
        }

        Vector3 planar = transform.position - lastPosition;
        planar.y = 0f;
        lastPosition = transform.position;

        Quaternion targetRot = baseRotation;
        Vector3 targetScale = baseScale;
        Vector3 targetPos = basePosition;

        if (attackTimer > 0f)
        {
            attackTimer -= Time.deltaTime;
            float t = 1f - attackTimer / 0.34f;
            float arc = Mathf.Sin(t * Mathf.PI);
            targetRot *= Quaternion.Euler(-arc * 12f, arc * 34f * attackSide, -arc * 9f * attackSide);
            targetScale = Vector3.Scale(baseScale, new Vector3(1f + arc * 0.04f, 1f - arc * 0.03f, 1f + arc * 0.10f));
            targetPos += Vector3.forward * arc * 0.16f;
        }
        else if (planar.sqrMagnitude > 0.0004f)
        {
            float bob = Mathf.Sin(Time.time * 11f);
            targetRot *= Quaternion.Euler(bob * 2.2f, 0f, 0f);
            targetPos += Vector3.up * Mathf.Abs(bob) * 0.035f;
        }

        if (hitTimer > 0f)
        {
            hitTimer -= Time.deltaTime;
            float shake = Mathf.Sin(hitTimer * 90f) * 5f;
            targetRot *= Quaternion.Euler(0f, 0f, shake);
        }

        visualRoot.localPosition = Vector3.Lerp(visualRoot.localPosition, targetPos, 18f * Time.deltaTime);
        visualRoot.localRotation = Quaternion.Slerp(visualRoot.localRotation, targetRot, 16f * Time.deltaTime);
        visualRoot.localScale = Vector3.Lerp(visualRoot.localScale, targetScale, 18f * Time.deltaTime);
    }

    private Transform ResolveVisualRoot()
    {
        Animator animator = GetComponentInChildren<Animator>(true);
        if (animator != null && animator.transform != transform) return animator.transform;
        foreach (Transform child in transform)
        {
            if (child.GetComponentInChildren<Renderer>(true) != null && !child.name.ToLowerInvariant().Contains("hp"))
                return child;
        }
        return null;
    }
}

public class AOGAutoAttackPresentationBootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        GameObject host = new GameObject("AOG_AutoAttack_Presentation_Bootstrap");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGAutoAttackPresentationBootstrap>();
    }

    private void Update()
    {
        foreach (AOGCharacterStats hero in FindObjectsByType<AOGCharacterStats>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (hero != null && hero.GetComponent<AOGAutoAttackPresentationRuntime>() == null)
                hero.gameObject.AddComponent<AOGAutoAttackPresentationRuntime>();
        }
    }
}
