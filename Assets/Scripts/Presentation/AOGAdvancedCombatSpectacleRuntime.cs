using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Adds anticipation/recovery body motion to champions when existing systems expose animation events through presentation calls.
/// This is intentionally presentation-only and conservative.
/// </summary>
[DefaultExecutionOrder(15980)]
public class AOGChampionMotionJuiceRuntime : MonoBehaviour
{
    private readonly HashSet<AOGActiveChampion> processed = new HashSet<AOGActiveChampion>();
    private float nextScan;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindFirstObjectByType<AOGChampionMotionJuiceRuntime>() != null) return;
        GameObject host = new GameObject("AOG_Champion_Motion_Juice_Runtime");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGChampionMotionJuiceRuntime>();
    }

    private void Update()
    {
        if (Time.unscaledTime < nextScan) return;
        nextScan = Time.unscaledTime + 0.9f;
        foreach (AOGActiveChampion champion in FindObjectsByType<AOGActiveChampion>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (champion == null || processed.Contains(champion)) continue;
            if (champion.GetComponent<AOGChampionAttackPoseRuntime>() == null)
                champion.gameObject.AddComponent<AOGChampionAttackPoseRuntime>();
            processed.Add(champion);
        }
    }
}

public class AOGChampionAttackPoseRuntime : MonoBehaviour
{
    private Vector3 baseScale;
    private Quaternion baseRotation;
    private float pulse;

    private void Awake()
    {
        baseScale = transform.localScale;
        baseRotation = transform.localRotation;
    }

    private void OnEnable()
    {
        AOGCombatEvents.BasicAttackHit += OnHit;
        AOGCombatEvents.AbilityHit += OnHit;
    }

    private void OnDisable()
    {
        AOGCombatEvents.BasicAttackHit -= OnHit;
        AOGCombatEvents.AbilityHit -= OnHit;
    }

    private void OnHit(AOGCombatHitEvent hit)
    {
        if (hit.source == null || (!hit.source.transform.IsChildOf(transform) && hit.source != gameObject)) return;
        pulse = Mathf.Max(pulse, hit.basicAttack ? 0.18f : 0.28f);
    }

    private void LateUpdate()
    {
        if (pulse <= 0f)
        {
            transform.localScale = Vector3.Lerp(transform.localScale, baseScale, 12f * Time.deltaTime);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, baseRotation, 12f * Time.deltaTime);
            return;
        }

        pulse -= Time.deltaTime;
        float k = Mathf.Clamp01(pulse / 0.28f);
        transform.localScale = baseScale * (1f + 0.035f * k);
        transform.localRotation = baseRotation * Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 26f) * 1.6f * k);
    }
}
