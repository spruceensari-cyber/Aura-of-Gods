using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Premium combat feel layer built on top of the existing combat event architecture.
/// It does not apply damage and does not replace attack/ability controllers.
/// </summary>
public class AOGAdvancedCombatSpectacleRuntime : MonoBehaviour
{
    private static AOGAdvancedCombatSpectacleRuntime instance;
    private readonly Queue<float> hitStopQueue = new Queue<float>();
    private bool runningHitStop;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindFirstObjectByType<AOGAdvancedCombatSpectacleRuntime>() != null) return;
        GameObject host = new GameObject("AOG_Advanced_Combat_Spectacle_Runtime");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGAdvancedCombatSpectacleRuntime>();
    }

    private void Awake()
    {
        instance = this;
    }

    private void OnEnable()
    {
        AOGCombatEvents.BasicAttackHit += OnBasicHit;
        AOGCombatEvents.AbilityHit += OnAbilityHit;
        AOGCombatEvents.ChampionDeath += OnChampionDeath;
    }

    private void OnDisable()
    {
        AOGCombatEvents.BasicAttackHit -= OnBasicHit;
        AOGCombatEvents.AbilityHit -= OnAbilityHit;
        AOGCombatEvents.ChampionDeath -= OnChampionDeath;
    }

    private void OnBasicHit(AOGCombatHitEvent hit)
    {
        if (hit.target == null) return;
        Vector3 p = hit.target.transform.position + Vector3.up * 0.75f;
        Color c = ResolveAccent(hit.source, false);
        SpawnImpactBurst("Basic", p, c, Mathf.Clamp(hit.damage / 90f, 0.8f, 1.8f), false);
        QueueHitStop(0.018f);
        CameraImpulse(0.08f);
    }

    private void OnAbilityHit(AOGCombatHitEvent hit)
    {
        if (hit.target == null) return;
        Vector3 p = hit.target.transform.position + Vector3.up * 0.85f;
        Color c = ResolveAccent(hit.source, true);
        SpawnImpactBurst("Ability", p, c, Mathf.Clamp(hit.damage / 130f, 1.0f, 2.8f), true);
        QueueHitStop(hit.damage > 180f ? 0.040f : 0.026f);
        CameraImpulse(Mathf.Clamp(hit.damage / 650f, 0.10f, 0.34f));
    }

    private void OnChampionDeath(AOGChampionDeathEvent death)
    {
        if (death.victim == null) return;
        AOGCharacterStats stats = death.victim;
        StartCoroutine(DeathDissolve(stats.gameObject));
        CameraImpulse(0.25f);
    }

    private IEnumerator DeathDissolve(GameObject target)
    {
        if (target == null) yield break;
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
        Vector3 startScale = target.transform.localScale;
        Color teamColor = Color.white;
        AOGCharacterStats stats = target.GetComponent<AOGCharacterStats>();
        if (stats != null) teamColor = stats.team == MinionTeam.Blue ? new Color(0.24f,0.70f,1f) : new Color(1f,0.22f,0.30f);

        GameObject ring = AOGAbilityVisuals.CreateRing("Champion_Spirit_Dissolve", target.transform.position + Vector3.up * 0.05f, 2.1f, teamColor, 0.12f);
        Destroy(ring, 1.1f);

        float duration = 0.85f;
        for (float t = 0f; t < duration && target != null; t += Time.deltaTime)
        {
            float k = Mathf.Clamp01(t / duration);
            target.transform.localScale = Vector3.Lerp(startScale, startScale * 0.84f, k);
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null) continue;
                foreach (Material mat in renderer.materials)
                {
                    if (mat == null) continue;
                    if (mat.HasProperty("_EmissionColor"))
                    {
                        mat.EnableKeyword("_EMISSION");
                        mat.SetColor("_EmissionColor", teamColor * Mathf.Lerp(2.2f, 0.2f, k));
                    }
                }
            }
            yield return null;
        }
    }

    private static Color ResolveAccent(GameObject source, bool ability)
    {
        AOGActiveChampion champion = source != null ? source.GetComponentInParent<AOGActiveChampion>() : null;
        if (champion != null) return champion.accentColor;
        AOGCharacterStats stats = source != null ? source.GetComponentInParent<AOGCharacterStats>() : null;
        if (stats != null) return stats.team == MinionTeam.Blue ? new Color(0.22f,0.62f,1f) : new Color(1f,0.20f,0.26f);
        return ability ? new Color(0.65f,0.35f,1f) : Color.white;
    }

    private static void SpawnImpactBurst(string name, Vector3 point, Color color, float scale, bool ability)
    {
        GameObject ring = AOGAbilityVisuals.CreateRing("AOG_Advanced_" + name + "_Impact", point, ability ? 1.55f * scale : 0.95f * scale, color, ability ? 0.12f : 0.065f);
        Destroy(ring, ability ? 0.42f : 0.26f);

        int shardCount = ability ? 7 : 3;
        for (int i = 0; i < shardCount; i++)
        {
            float angle = i * Mathf.PI * 2f / shardCount + Random.Range(-0.18f, 0.18f);
            GameObject shard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shard.name = "AOG_Advanced_Impact_Shard";
            shard.transform.position = point + new Vector3(Mathf.Cos(angle), Random.Range(-0.1f, 0.5f), Mathf.Sin(angle)) * (0.18f * scale);
            shard.transform.rotation = Quaternion.Euler(Random.Range(-35f,35f), angle*Mathf.Rad2Deg, Random.Range(-45f,45f));
            shard.transform.localScale = new Vector3(0.055f, Random.Range(0.45f,0.9f) * scale, 0.055f);
            shard.GetComponent<Renderer>().sharedMaterial = Emissive(color, ability ? 4.8f : 3.2f);
            Collider c = shard.GetComponent<Collider>(); if (c != null) Destroy(c);
            Destroy(shard, ability ? 0.38f : 0.22f);
        }
    }

    private void QueueHitStop(float duration)
    {
        hitStopQueue.Enqueue(Mathf.Clamp(duration, 0.01f, 0.05f));
        if (!runningHitStop)
            StartCoroutine(RunHitStop());
    }

    private IEnumerator RunHitStop()
    {
        runningHitStop = true;
        while (hitStopQueue.Count > 0)
        {
            float duration = hitStopQueue.Dequeue();
            float previousScale = Time.timeScale;
            Time.timeScale = Mathf.Min(Time.timeScale, 0.18f);
            yield return new WaitForSecondsRealtime(duration);
            Time.timeScale = previousScale;
            yield return null;
        }
        runningHitStop = false;
    }

    private static void CameraImpulse(float amount)
    {
        if (Camera.main == null) return;
        AOGMobaCameraController cam = Camera.main.GetComponent<AOGMobaCameraController>();
        if (cam != null) cam.AddRandomImpulse(amount);
    }

    private static Material Emissive(Color color, float strength)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        Material mat = new Material(shader) { color = color };
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * strength);
        }
        return mat;
    }
}

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
        if (hit.source == null || !hit.source.transform.IsChildOf(transform) && hit.source != gameObject) return;
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
