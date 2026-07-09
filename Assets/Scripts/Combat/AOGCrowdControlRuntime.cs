using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum AOGCrowdControlType
{
    Slow,
    Root,
    Stun,
    Silence,
    Knockback,
    Airborne
}

public class AOGCrowdControlRuntime : MonoBehaviour
{
    private Champion champion;
    private readonly Dictionary<AOGCrowdControlType, float> expiresAt = new();
    private float slowMultiplier = 1f;
    private StatModifier slowModifier;

    public bool IsRooted => Active(AOGCrowdControlType.Root);
    public bool IsSilenced => Active(AOGCrowdControlType.Silence);
    public bool IsAirborne => Active(AOGCrowdControlType.Airborne);

    void Awake()
    {
        champion = GetComponent<Champion>();
    }

    void Update()
    {
        if (champion == null)
            return;

        if (!Active(AOGCrowdControlType.Slow) && slowModifier != null)
        {
            champion.RemoveStatModifier(slowModifier);
            slowModifier = null;
            slowMultiplier = 1f;
        }
    }

    public void Apply(AOGCrowdControlType type, float duration, float magnitude = 0.35f, Vector3 source = default)
    {
        duration = Mathf.Max(0f, duration);
        expiresAt[type] = Mathf.Max(expiresAt.TryGetValue(type, out float old) ? old : 0f, Time.time + duration);

        switch (type)
        {
            case AOGCrowdControlType.Stun:
                champion?.Stun(duration);
                break;
            case AOGCrowdControlType.Slow:
                ApplySlow(magnitude);
                break;
            case AOGCrowdControlType.Knockback:
                StartCoroutine(KnockbackRoutine(source, Mathf.Max(1f, magnitude), duration <= 0f ? 0.18f : duration));
                break;
            case AOGCrowdControlType.Airborne:
                StartCoroutine(AirborneRoutine(Mathf.Max(0.4f, duration), Mathf.Max(0.8f, magnitude)));
                break;
        }
    }

    public bool Active(AOGCrowdControlType type)
    {
        return expiresAt.TryGetValue(type, out float t) && Time.time < t;
    }

    public void Cleanse(bool removeHardCC = false)
    {
        List<AOGCrowdControlType> remove = new();
        foreach (var pair in expiresAt)
        {
            bool hard = pair.Key == AOGCrowdControlType.Stun || pair.Key == AOGCrowdControlType.Airborne;
            if (!hard || removeHardCC)
                remove.Add(pair.Key);
        }

        foreach (AOGCrowdControlType type in remove)
            expiresAt.Remove(type);

        if (slowModifier != null)
        {
            champion?.RemoveStatModifier(slowModifier);
            slowModifier = null;
        }
    }

    private void ApplySlow(float magnitude)
    {
        float nextMultiplier = Mathf.Clamp01(1f - Mathf.Clamp01(magnitude));
        if (slowModifier != null && nextMultiplier >= slowMultiplier)
            return;

        if (slowModifier != null)
            champion.RemoveStatModifier(slowModifier);

        slowMultiplier = nextMultiplier;
        float movementPenalty = -champion.MovementSpeed * (1f - slowMultiplier);
        slowModifier = new StatModifier { MovementSpeedBonus = movementPenalty };
        champion.AddStatModifier(slowModifier);
    }

    private IEnumerator KnockbackRoutine(Vector3 source, float distance, float duration)
    {
        Vector3 start = transform.position;
        Vector3 dir = transform.position - source;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f)
            dir = -transform.forward;
        Vector3 end = start + dir.normalized * distance;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(start, end, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
    }

    private IEnumerator AirborneRoutine(float duration, float height)
    {
        Vector3 start = transform.position;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            Vector3 p = start;
            p.y += Mathf.Sin(t * Mathf.PI) * height;
            transform.position = p;
            yield return null;
        }
        transform.position = start;
    }

    public static AOGCrowdControlRuntime Ensure(Champion target)
    {
        if (target == null) return null;
        AOGCrowdControlRuntime cc = target.GetComponent<AOGCrowdControlRuntime>();
        if (cc == null) cc = target.gameObject.AddComponent<AOGCrowdControlRuntime>();
        return cc;
    }
}
