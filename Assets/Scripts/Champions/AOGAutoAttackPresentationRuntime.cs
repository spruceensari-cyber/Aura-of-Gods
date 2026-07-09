using UnityEngine;

[DefaultExecutionOrder(25)]
public class AOGAutoAttackPresentationRuntime : MonoBehaviour
{
    private Vector3 lastPosition;
    private Quaternion baseRotation;
    private Vector3 baseScale;
    private float attackTimer;
    private float hitTimer;
    private int attackSide = 1;
    private AOGCharacterStats stats;

    private void Awake()
    {
        stats = GetComponent<AOGCharacterStats>();
        lastPosition = transform.position;
        baseRotation = transform.localRotation;
        baseScale = transform.localScale;
    }

    public void PlayAttack()
    {
        attackSide *= -1;
        attackTimer = 0.34f;
    }

    public void PlayHit()
    {
        hitTimer = 0.16f;
    }

    private void Update()
    {
        if (stats != null && stats.IsDead) return;

        Vector3 planar = transform.position - lastPosition;
        planar.y = 0f;
        lastPosition = transform.position;

        Quaternion targetRot = baseRotation;
        Vector3 targetScale = baseScale;

        if (attackTimer > 0f)
        {
            attackTimer -= Time.deltaTime;
            float t = 1f - attackTimer / 0.34f;
            float arc = Mathf.Sin(t * Mathf.PI);
            targetRot *= Quaternion.Euler(-arc * 12f, arc * 34f * attackSide, -arc * 9f * attackSide);
            targetScale = Vector3.Scale(baseScale, new Vector3(1f + arc * 0.04f, 1f - arc * 0.03f, 1f + arc * 0.10f));
        }
        else if (planar.sqrMagnitude > 0.0004f)
        {
            float bob = Mathf.Sin(Time.time * 11f) * 2.2f;
            targetRot *= Quaternion.Euler(bob, 0f, 0f);
        }

        if (hitTimer > 0f)
        {
            hitTimer -= Time.deltaTime;
            float shake = Mathf.Sin(hitTimer * 90f) * 5f;
            targetRot *= Quaternion.Euler(0f, 0f, shake);
        }

        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRot, 16f * Time.deltaTime);
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, 18f * Time.deltaTime);
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
