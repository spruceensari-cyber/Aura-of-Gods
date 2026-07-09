using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Combat readability layer: attack trails, hit bursts and death dissolving pulses.
/// Uses lightweight primitives as replaceable production hooks.
/// </summary>
public class AOGCombatVFXPolishRuntime : MonoBehaviour
{
    readonly HashSet<Champion> boundChampions = new();
    readonly HashSet<ChampionController> boundControllers = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        if (FindObjectOfType<AOGCombatVFXPolishRuntime>() != null) return;
        new GameObject("AOG_Combat_VFX_Polish_Runtime").AddComponent<AOGCombatVFXPolishRuntime>();
    }

    void Awake() => DontDestroyOnLoad(gameObject);

    void Update()
    {
        foreach (Champion champion in Resources.FindObjectsOfTypeAll<Champion>())
        {
            if (champion == null || !champion.gameObject.scene.IsValid() || !boundChampions.Add(champion)) continue;
            champion.OnDamaged += (damage, type) => SpawnImpact(champion.transform.position + Vector3.up * 1.0f, DamageColor(type), Mathf.Clamp(0.35f + damage / 250f, 0.35f, 1.1f));
            champion.OnDeath += () => StartCoroutine(DeathPulse(champion.transform.position));
        }

        foreach (ChampionController controller in Resources.FindObjectsOfTypeAll<ChampionController>())
        {
            if (controller == null || !controller.gameObject.scene.IsValid() || !boundControllers.Add(controller)) continue;
            controller.OnBasicAttackWindup += () => StartCoroutine(AttackTrail(controller.transform));
        }
    }

    IEnumerator AttackTrail(Transform source)
    {
        if (source == null) yield break;
        Vector3 start = source.position + Vector3.up * 1.0f + source.right * 0.35f;
        Vector3 end = start + source.forward * 1.9f + source.right * 0.8f;
        GameObject trail = GameObject.CreatePrimitive(PrimitiveType.Cube);
        trail.name = "AOG_Attack_Trail";
        Collider col = trail.GetComponent<Collider>();
        if (col != null) Destroy(col);
        trail.transform.localScale = new Vector3(0.08f, 0.08f, 1.4f);
        trail.GetComponent<Renderer>().material = CreateMaterial(new Color(0.10f, 0.78f, 1f), 2.8f);

        float elapsed = 0f;
        while (elapsed < 0.10f)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / 0.10f);
            trail.transform.position = Vector3.Lerp(start, end, t);
            trail.transform.rotation = Quaternion.LookRotation((end - start).normalized);
            trail.transform.localScale = new Vector3(0.08f * (1f - t), 0.08f * (1f - t), 1.4f);
            yield return null;
        }
        Destroy(trail);
    }

    void SpawnImpact(Vector3 position, Color color, float scale)
    {
        GameObject burst = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        burst.name = "AOG_Hit_Burst";
        burst.transform.position = position;
        burst.transform.localScale = Vector3.one * 0.12f;
        Collider col = burst.GetComponent<Collider>();
        if (col != null) Destroy(col);
        burst.GetComponent<Renderer>().material = CreateMaterial(color, 3.2f);
        StartCoroutine(ImpactRoutine(burst.transform, scale));
    }

    IEnumerator ImpactRoutine(Transform burst, float scale)
    {
        float elapsed = 0f;
        while (burst != null && elapsed < 0.16f)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / 0.16f);
            burst.localScale = Vector3.one * Mathf.Lerp(0.12f, scale, t);
            yield return null;
        }
        if (burst != null) Destroy(burst.gameObject);
    }

    IEnumerator DeathPulse(Vector3 position)
    {
        for (int i = 0; i < 3; i++)
        {
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "AOG_Death_Pulse";
            ring.transform.position = position + Vector3.up * 0.05f;
            ring.transform.localScale = new Vector3(0.4f, 0.02f, 0.4f);
            Collider col = ring.GetComponent<Collider>();
            if (col != null) Destroy(col);
            ring.GetComponent<Renderer>().material = CreateMaterial(new Color(0.48f, 0.16f, 1f), 3f);

            float elapsed = 0f;
            while (elapsed < 0.32f)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / 0.32f);
                ring.transform.localScale = new Vector3(Mathf.Lerp(0.4f, 3.8f, t), 0.02f, Mathf.Lerp(0.4f, 3.8f, t));
                yield return null;
            }
            Destroy(ring);
            yield return new WaitForSecondsRealtime(0.08f);
        }
    }

    static Color DamageColor(DamageType type) => type switch
    {
        DamageType.Magical => new Color(0.50f, 0.16f, 1f),
        DamageType.True => Color.white,
        _ => new Color(1f, 0.32f, 0.08f)
    };

    static Material CreateMaterial(Color color, float emission)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
        Material mat = new Material(shader) { color = color };
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * emission);
        }
        return mat;
    }
}
