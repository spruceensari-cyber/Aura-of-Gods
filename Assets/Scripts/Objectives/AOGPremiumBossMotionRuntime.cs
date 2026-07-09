using UnityEngine;

[DefaultExecutionOrder(15)]
public class AOGPremiumBossMotionRuntime : MonoBehaviour
{
    private AOGNeutralBossAI boss;
    private Transform visualRoot;
    private Vector3 baseLocalPosition;
    private Quaternion baseLocalRotation;
    private Vector3 lastWorldPosition;
    private float moveBlend;
    private float attackPulse;

    private void Awake()
    {
        boss = GetComponent<AOGNeutralBossAI>();
        visualRoot = ResolveVisualRoot();
        if (visualRoot != null)
        {
            baseLocalPosition = visualRoot.localPosition;
            baseLocalRotation = visualRoot.localRotation;
        }
        lastWorldPosition = transform.position;
    }

    private void Update()
    {
        if (boss == null || visualRoot == null || boss.IsDead)
            return;

        Vector3 velocity = (transform.position - lastWorldPosition) / Mathf.Max(Time.deltaTime, 0.0001f);
        velocity.y = 0f;
        lastWorldPosition = transform.position;
        moveBlend = Mathf.Lerp(moveBlend, Mathf.Clamp01(velocity.magnitude / Mathf.Max(0.1f, boss.moveSpeed)), 6f * Time.deltaTime);

        float time = Time.time;
        if (boss.bossType == AOGNeutralBossType.Dragon)
            AnimateDragon(time);
        else
            AnimateMedusa(time);

        attackPulse = Mathf.MoveTowards(attackPulse, 0f, Time.deltaTime * 2.5f);
    }

    public void PulseAttack()
    {
        attackPulse = 1f;
    }

    private void AnimateDragon(float t)
    {
        float hover = Mathf.Sin(t * 1.65f) * 0.22f;
        float breathing = Mathf.Sin(t * 2.1f) * 0.035f;
        float bank = Mathf.Sin(t * 0.82f) * 5f;
        float stride = Mathf.Sin(t * 6.5f) * moveBlend;

        visualRoot.localPosition = baseLocalPosition + Vector3.up * (hover + Mathf.Abs(stride) * 0.08f) + Vector3.forward * attackPulse * 0.28f;
        visualRoot.localRotation = baseLocalRotation * Quaternion.Euler(-4f - attackPulse * 14f + stride * 3f, Mathf.Sin(t * 0.55f) * 8f, bank);
        visualRoot.localScale = Vector3.one * (1f + breathing + attackPulse * 0.06f);
    }

    private void AnimateMedusa(float t)
    {
        float sway = Mathf.Sin(t * 1.25f);
        float hover = Mathf.Sin(t * 1.8f) * 0.12f;
        float stride = Mathf.Sin(t * 7.2f) * moveBlend;

        visualRoot.localPosition = baseLocalPosition + new Vector3(sway * 0.06f, hover + Mathf.Abs(stride) * 0.05f, attackPulse * 0.16f);
        visualRoot.localRotation = baseLocalRotation * Quaternion.Euler(stride * 2f, sway * 11f + attackPulse * 18f, -sway * 4f);
        visualRoot.localScale = Vector3.one * (1f + Mathf.Sin(t * 2.4f) * 0.018f + attackPulse * 0.04f);
    }

    private Transform ResolveVisualRoot()
    {
        Animator animator = GetComponentInChildren<Animator>(true);
        if (animator != null && animator.transform != transform)
            return animator.transform;

        foreach (Transform child in transform)
        {
            if (child.GetComponentInChildren<Renderer>(true) != null && !child.name.ToLowerInvariant().Contains("aura"))
                return child;
        }

        return transform;
    }
}

public class AOGPremiumBossMotionBootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        GameObject host = new GameObject("AOG_Premium_Boss_Motion_Bootstrap");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGPremiumBossMotionBootstrap>();
    }

    private void Update()
    {
        foreach (AOGNeutralBossAI boss in FindObjectsByType<AOGNeutralBossAI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (boss != null && boss.GetComponent<AOGPremiumBossMotionRuntime>() == null)
                boss.gameObject.AddComponent<AOGPremiumBossMotionRuntime>();
        }
    }
}
