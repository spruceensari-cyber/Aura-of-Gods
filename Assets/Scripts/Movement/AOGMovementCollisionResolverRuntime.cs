using UnityEngine;

/// <summary>
/// Shared movement safety helper for transform-driven MOBA actors. Performs capsule sweeps against
/// world/structure blockers, slides along surfaces and applies small depenetration corrections.
/// Other combat units are intentionally ignored as hard blockers so lane crowds do not deadlock.
/// </summary>
public static class AOGMovementCollisionResolver
{
    private static readonly RaycastHit[] sweepHits = new RaycastHit[24];
    private static readonly Collider[] overlapHits = new Collider[24];

    public static Vector3 ResolveStep(Transform actor, Vector3 startPosition, Vector3 desiredDelta, float radius, float height)
    {
        if (actor == null || desiredDelta.sqrMagnitude <= 0.0000001f) return Vector3.zero;

        desiredDelta.y = 0f;
        float distance = desiredDelta.magnitude;
        if (distance <= 0.0001f) return Vector3.zero;

        Vector3 direction = desiredDelta / distance;
        Capsule(startPosition,radius,height,out Vector3 bottom,out Vector3 top);
        int count = Physics.CapsuleCastNonAlloc(bottom,top,radius,direction,sweepHits,distance+0.08f,~0,QueryTriggerInteraction.Ignore);

        RaycastHit nearest = default;
        bool blocked = false;
        float nearestDistance = float.PositiveInfinity;
        for (int i=0;i<count;i++)
        {
            Collider collider = sweepHits[i].collider;
            if (!IsWorldBlocker(actor,collider)) continue;
            if (sweepHits[i].distance < nearestDistance)
            {
                nearestDistance = sweepHits[i].distance;
                nearest = sweepHits[i];
                blocked = true;
            }
        }

        if (!blocked) return desiredDelta;

        float travel = Mathf.Clamp(nearestDistance-0.04f,0f,distance);
        Vector3 first = direction*travel;
        Vector3 remaining = desiredDelta-first;
        Vector3 slide = Vector3.ProjectOnPlane(remaining,nearest.normal);
        slide.y = 0f;
        if (slide.sqrMagnitude <= 0.0001f) return first;

        Vector3 slideStart = startPosition+first;
        Capsule(slideStart,radius,height,out bottom,out top);
        float slideDistance = slide.magnitude;
        Vector3 slideDirection = slide/slideDistance;
        count = Physics.CapsuleCastNonAlloc(bottom,top,radius,slideDirection,sweepHits,slideDistance+0.04f,~0,QueryTriggerInteraction.Ignore);
        float allowedSlide = slideDistance;
        for (int i=0;i<count;i++)
        {
            if (!IsWorldBlocker(actor,sweepHits[i].collider)) continue;
            allowedSlide = Mathf.Min(allowedSlide,Mathf.Max(0f,sweepHits[i].distance-0.03f));
        }
        return first+slideDirection*allowedSlide;
    }

    public static Vector3 ComputeDepenetration(Transform actor,float radius,float height,float maxCorrection=0.32f)
    {
        if (actor == null) return Vector3.zero;
        Capsule(actor.position,radius,height,out Vector3 bottom,out Vector3 top);
        int count = Physics.OverlapCapsuleNonAlloc(bottom,top,radius,overlapHits,~0,QueryTriggerInteraction.Ignore);
        Vector3 correction = Vector3.zero;
        int blockers = 0;

        CapsuleCollider probe = actor.GetComponent<CapsuleCollider>();
        if (probe == null) return Vector3.zero;

        for (int i=0;i<count;i++)
        {
            Collider other = overlapHits[i];
            if (!IsWorldBlocker(actor,other)) continue;
            if (Physics.ComputePenetration(probe,actor.position,actor.rotation,other,other.transform.position,other.transform.rotation,out Vector3 dir,out float dist))
            {
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.0001f)
                {
                    correction += dir.normalized*Mathf.Min(dist,maxCorrection);
                    blockers++;
                }
            }
        }

        if (blockers > 1) correction /= blockers;
        return Vector3.ClampMagnitude(correction,maxCorrection);
    }

    private static bool IsWorldBlocker(Transform actor,Collider collider)
    {
        if (collider == null || collider.isTrigger) return false;
        Transform t = collider.transform;
        if (t == actor || t.IsChildOf(actor) || actor.IsChildOf(t)) return false;

        if (collider.GetComponentInParent<AOGCharacterStats>() != null) return false;
        if (collider.GetComponentInParent<Minion>() != null) return false;
        if (collider.GetComponentInParent<AOGNeutralMonsterRuntime>() != null) return false;
        if (collider.GetComponentInParent<AOGNeutralBossAI>() != null) return false;

        string n = collider.gameObject.name.ToLowerInvariant();
        if (n.Contains("ground") || n.Contains("floor") || n.Contains("lane") || n.Contains("river") || n.Contains("road")) return false;
        if (n.Contains("telegraph") || n.Contains("ring") || n.Contains("aura") || n.Contains("click") || n.Contains("healthbar") || n.Contains("hp_bar")) return false;

        return true;
    }

    private static void Capsule(Vector3 position,float radius,float height,out Vector3 bottom,out Vector3 top)
    {
        float safeHeight = Mathf.Max(height,radius*2f+0.05f);
        float halfSegment = Mathf.Max(0f,safeHeight*0.5f-radius);
        Vector3 center = position+Vector3.up*(safeHeight*0.5f);
        bottom = center-Vector3.up*halfSegment;
        top = center+Vector3.up*halfSegment;
    }
}

/// <summary>
/// Late-frame guard for every transform-driven movement authority. It records the frame start,
/// inspects the final attempted displacement and rewrites that displacement through the resolver.
/// </summary>
[DefaultExecutionOrder(9000)]
public class AOGMovementFrameGuardRuntime : MonoBehaviour
{
    public float radius = 0.58f;
    public float height = 2.2f;
    public float maxFrameDistance = 1.25f;

    private Vector3 frameStart;
    private bool initialized;

    private void OnEnable()
    {
        frameStart = transform.position;
        initialized = true;
    }

    private void Update()
    {
        if (!initialized)
        {
            frameStart = transform.position;
            initialized = true;
        }
    }

    private void LateUpdate()
    {
        Vector3 attempted = transform.position-frameStart;
        float vertical = attempted.y;
        attempted.y = 0f;

        if (attempted.magnitude > maxFrameDistance)
        {
            frameStart = transform.position;
            return;
        }

        if (attempted.sqrMagnitude > 0.0000001f)
        {
            Vector3 resolved = AOGMovementCollisionResolver.ResolveStep(transform,frameStart,attempted,radius,height);
            Vector3 final = frameStart+resolved;
            final.y = frameStart.y+vertical;
            transform.position = final;
        }

        Vector3 correction = AOGMovementCollisionResolver.ComputeDepenetration(transform,radius,height,0.10f);
        if (correction.sqrMagnitude > 0.0001f)
            transform.position += correction;

        frameStart = transform.position;
    }
}

/// <summary>
/// Low-frequency stuck detector. It never owns path choice; it applies a small depenetration nudge
/// and a deterministic lateral escape offset when the actor remains nearly stationary too long.
/// </summary>
public class AOGMovementStuckRecoveryRuntime : MonoBehaviour
{
    public float radius = 0.58f;
    public float height = 2.2f;
    public float sampleInterval = 0.35f;
    public float stuckDistance = 0.055f;
    public int samplesBeforeRecovery = 4;

    private Vector3 lastPosition;
    private float nextSample;
    private int stuckSamples;
    private int escapeSign = 1;

    private void OnEnable()
    {
        lastPosition = transform.position;
        stuckSamples = 0;
    }

    private void Update()
    {
        if (Time.unscaledTime < nextSample) return;
        nextSample = Time.unscaledTime+sampleInterval;

        Vector3 delta = transform.position-lastPosition;
        delta.y = 0f;
        if (delta.magnitude <= stuckDistance) stuckSamples++;
        else stuckSamples = 0;
        lastPosition = transform.position;

        if (stuckSamples < samplesBeforeRecovery) return;

        Vector3 correction = AOGMovementCollisionResolver.ComputeDepenetration(transform,radius,height,0.28f);
        if (correction.sqrMagnitude <= 0.0001f)
        {
            Vector3 lateral = Vector3.Cross(Vector3.up,transform.forward).normalized*escapeSign*0.22f;
            correction = AOGMovementCollisionResolver.ResolveStep(transform,transform.position,lateral,radius,height);
            escapeSign *= -1;
        }

        if (correction.sqrMagnitude > 0.0001f)
            transform.position += correction;
        stuckSamples = 0;
    }
}

[DefaultExecutionOrder(-580)]
public class AOGMovementReliabilityBootstrap : MonoBehaviour
{
    private float nextAttach;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindFirstObjectByType<AOGMovementReliabilityBootstrap>() != null) return;
        GameObject host = new GameObject("AOG_Movement_Reliability_Bootstrap");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGMovementReliabilityBootstrap>();
    }

    private void Update()
    {
        if (Time.unscaledTime < nextAttach) return;
        nextAttach = Time.unscaledTime+1.0f;

        foreach (AOGCharacterStats hero in AOGWorldRegistry.Characters)
            Attach(hero != null ? hero.gameObject : null);

        foreach (Minion minion in Minion.Active)
            Attach(minion != null ? minion.gameObject : null);
    }

    private static void Attach(GameObject target)
    {
        if (target == null) return;
        CapsuleCollider capsule = target.GetComponent<CapsuleCollider>();
        float radius = capsule != null ? capsule.radius : 0.58f;
        float height = capsule != null ? capsule.height : 2.2f;

        AOGMovementFrameGuardRuntime guard = target.GetComponent<AOGMovementFrameGuardRuntime>();
        if (guard == null) guard = target.AddComponent<AOGMovementFrameGuardRuntime>();
        guard.radius = radius;
        guard.height = height;

        AOGMovementStuckRecoveryRuntime recovery = target.GetComponent<AOGMovementStuckRecoveryRuntime>();
        if (recovery == null) recovery = target.AddComponent<AOGMovementStuckRecoveryRuntime>();
        recovery.radius = radius;
        recovery.height = height;
    }
}
