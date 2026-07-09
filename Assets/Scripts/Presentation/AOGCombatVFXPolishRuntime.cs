using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// More natural combat presentation layer using particle bursts, short-lived lights and eased motion.
/// Designed as a bridge toward authored VFX Graph assets without cube/sphere placeholder readability artifacts.
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
            champion.OnDamaged += (damage, type) => SpawnImpact(champion.transform.position + Vector3.up * 1.0f, DamageColor(type), damage);
            champion.OnDeath += () => StartCoroutine(DeathDissolveBurst(champion.transform.position));
        }

        foreach (ChampionController controller in Resources.FindObjectsOfTypeAll<ChampionController>())
        {
            if (controller == null || !controller.gameObject.scene.IsValid() || !boundControllers.Add(controller)) continue;
            controller.OnBasicAttackWindup += () => StartCoroutine(AttackArc(controller.transform));
        }
    }

    IEnumerator AttackArc(Transform source)
    {
        if (source == null) yield break;

        Vector3 origin = source.position + Vector3.up * 1.05f;
        TrailRenderer trail = CreateTrail("AOG_Attack_Arc", new Color(0.12f, 0.72f, 1f, 0.75f), 0.11f, 0.018f);
        trail.transform.position = origin;

        float elapsed = 0f;
        float duration = 0.14f;
        while (source != null && elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            float angle = Mathf.Lerp(-48f, 58f, eased) * Mathf.Deg2Rad;
            Vector3 local = new Vector3(Mathf.Sin(angle) * 1.35f, 0.18f * Mathf.Sin(t * Mathf.PI), Mathf.Cos(angle) * 1.35f);
            trail.transform.position = origin + source.TransformDirection(local);
            yield return null;
        }

        Destroy(trail.gameObject, trail.time + 0.05f);
    }

    void SpawnImpact(Vector3 position, Color color, float damage)
    {
        float intensity = Mathf.Clamp01(damage / 240f);
        ParticleSystem ps = CreateImpactParticles(position, color, intensity);
        ps.Play();
        StartCoroutine(ImpactLight(position, color, Mathf.Lerp(1.2f, 3.2f, intensity), Mathf.Lerp(0.07f, 0.16f, intensity)));
        Destroy(ps.gameObject, 1.2f);
    }

    ParticleSystem CreateImpactParticles(Vector3 position, Color color, float intensity)
    {
        GameObject obj = new GameObject("AOG_Impact_Particles");
        obj.transform.position = position;
        ParticleSystem ps = obj.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule main = ps.main;
        main.duration = 0.25f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.12f, 0.34f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2.0f, 5.2f + intensity * 2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.13f + intensity * 0.05f);
        main.startColor = color;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 36;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)Mathf.RoundToInt(Mathf.Lerp(10f, 26f, intensity))) });

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.18f;

        ParticleSystem.ColorOverLifetimeModule colorLife = ps.colorOverLifetime;
        colorLife.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(color, 0f), new GradientColorKey(color * 0.5f, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        colorLife.color = gradient;

        ParticleSystem.LimitVelocityOverLifetimeModule limit = ps.limitVelocityOverLifetime;
        limit.enabled = true;
        limit.drag = 3.5f;

        return ps;
    }

    IEnumerator ImpactLight(Vector3 position, Color color, float range, float duration)
    {
        GameObject obj = new GameObject("AOG_Impact_Light");
        obj.transform.position = position;
        Light light = obj.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.range = range;
        light.intensity = 2.2f;
        light.shadows = LightShadows.None;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            light.intensity = Mathf.Lerp(2.2f, 0f, t * t);
            yield return null;
        }
        Destroy(obj);
    }

    IEnumerator DeathDissolveBurst(Vector3 position)
    {
        for (int layer = 0; layer < 3; layer++)
        {
            float radius = 0.8f + layer * 0.55f;
            GameObject ring = new GameObject("AOG_Death_Energy_Ring");
            LineRenderer lr = ring.AddComponent<LineRenderer>();
            lr.loop = true;
            lr.useWorldSpace = true;
            lr.positionCount = 48;
            lr.widthMultiplier = 0.045f;
            lr.material = CreateMaterial(new Color(0.38f, 0.12f, 1f, 0.65f), 2.5f);

            float elapsed = 0f;
            float duration = 0.42f + layer * 0.08f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float easedRadius = Mathf.Lerp(radius, radius * 3.8f, 1f - Mathf.Pow(1f - t, 2f));
                for (int i = 0; i < lr.positionCount; i++)
                {
                    float a = i / (float)lr.positionCount * Mathf.PI * 2f;
                    Vector3 p = position + new Vector3(Mathf.Cos(a), 0.05f + Mathf.Sin(a * 3f + t * 7f) * 0.04f, Mathf.Sin(a)) * easedRadius;
                    lr.SetPosition(i, p);
                }
                Color c = lr.material.color;
                c.a = 1f - t;
                lr.material.color = c;
                yield return null;
            }
            Destroy(ring);
            yield return new WaitForSecondsRealtime(0.06f);
        }
    }

    TrailRenderer CreateTrail(string name, Color color, float time, float width)
    {
        GameObject obj = new GameObject(name);
        TrailRenderer trail = obj.AddComponent<TrailRenderer>();
        trail.time = time;
        trail.minVertexDistance = 0.02f;
        trail.widthCurve = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.18f, 1f),
            new Keyframe(1f, 0f));
        trail.widthMultiplier = width;
        trail.material = CreateMaterial(color, 2.3f);
        return trail;
    }

    static Color DamageColor(DamageType type) => type switch
    {
        DamageType.Magical => new Color(0.45f, 0.18f, 1f),
        DamageType.True => new Color(0.95f, 0.98f, 1f),
        _ => new Color(1f, 0.28f, 0.08f)
    };

    static Material CreateMaterial(Color color, float emission)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
            ?? Shader.Find("Universal Render Pipeline/Unlit")
            ?? Shader.Find("Unlit/Color")
            ?? Shader.Find("Standard");
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
