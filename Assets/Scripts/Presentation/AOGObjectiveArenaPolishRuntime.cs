using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Realism-oriented objective arena polish: continuous energy rings, low mist, selective local lighting and subtle motion.
/// Keeps authored geometry untouched and avoids visible primitive-segment construction.
/// </summary>
public class AOGObjectiveArenaPolishRuntime : MonoBehaviour
{
    readonly List<LineRenderer> rings = new();
    readonly List<ParticleSystem> mists = new();
    readonly List<Light> localLights = new();
    Material dragonMat;
    Material medusaMat;
    bool built;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        if (FindObjectOfType<AOGObjectiveArenaPolishRuntime>() != null) return;
        new GameObject("AOG_Objective_Arena_Polish_Runtime").AddComponent<AOGObjectiveArenaPolishRuntime>();
    }

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        dragonMat = CreateMaterial("DragonArena", new Color(1f, 0.10f, 0.025f, 0.62f), 2.5f);
        medusaMat = CreateMaterial("MedusaArena", new Color(0.15f, 0.88f, 0.38f, 0.58f), 2.0f);
    }

    void Update()
    {
        if (!built) TryBuild();
        Animate();
    }

    void TryBuild()
    {
        ObjectiveManager manager = FindObjectOfType<ObjectiveManager>();
        if (manager == null || manager.DragonObject == null || manager.MedusaObject == null) return;

        BuildArena(manager.DragonObject.transform.position, dragonMat, new Color(1f, 0.18f, 0.04f), "Dragon");
        BuildArena(manager.MedusaObject.transform.position, medusaMat, new Color(0.18f, 1f, 0.42f), "Medusa");
        built = true;
    }

    void BuildArena(Vector3 center, Material mat, Color lightColor, string prefix)
    {
        rings.Add(CreateRing(prefix + "_OuterRing", center, 10f, 0.07f, mat, 0.06f));
        rings.Add(CreateRing(prefix + "_InnerRing", center, 6.8f, 0.045f, mat, 0.11f));
        mists.Add(CreateGroundMist(prefix + "_GroundMist", center, lightColor));

        for (int i = 0; i < 4; i++)
        {
            float angle = i / 4f * Mathf.PI * 2f + Mathf.PI * 0.25f;
            Vector3 pos = center + new Vector3(Mathf.Cos(angle), 1.1f, Mathf.Sin(angle)) * 7.8f;
            GameObject lightObj = new GameObject(prefix + "_AccentLight");
            lightObj.transform.position = pos;
            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = lightColor;
            light.intensity = 0.85f;
            light.range = 4.8f;
            light.shadows = LightShadows.None;
            localLights.Add(light);
        }
    }

    LineRenderer CreateRing(string name, Vector3 center, float radius, float width, Material mat, float waviness)
    {
        GameObject obj = new GameObject(name);
        LineRenderer lr = obj.AddComponent<LineRenderer>();
        lr.loop = true;
        lr.useWorldSpace = true;
        lr.positionCount = 96;
        lr.widthMultiplier = width;
        lr.numCornerVertices = 4;
        lr.numCapVertices = 4;
        lr.material = mat;

        for (int i = 0; i < lr.positionCount; i++)
        {
            float a = i / (float)lr.positionCount * Mathf.PI * 2f;
            float r = radius + Mathf.Sin(a * 5f) * waviness;
            lr.SetPosition(i, center + new Vector3(Mathf.Cos(a) * r, 0.075f, Mathf.Sin(a) * r));
        }

        return lr;
    }

    ParticleSystem CreateGroundMist(string name, Vector3 center, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.position = center + Vector3.up * 0.05f;
        ParticleSystem ps = obj.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule main = ps.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(2.8f, 5.2f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.25f);
        main.startSize = new ParticleSystem.MinMaxCurve(1.2f, 2.8f);
        main.startColor = new Color(color.r, color.g, color.b, 0.08f);
        main.maxParticles = 60;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 7f;

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Donut;
        shape.radius = 7.2f;
        shape.donutRadius = 2.8f;
        shape.rotation = new Vector3(90f, 0f, 0f);

        ParticleSystem.ColorOverLifetimeModule colorLife = ps.colorOverLifetime;
        colorLife.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(color, 0f), new GradientColorKey(color * 0.55f, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.09f, 0.35f), new GradientAlphaKey(0f, 1f) });
        colorLife.color = gradient;

        ParticleSystem.VelocityOverLifetimeModule velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.y = new ParticleSystem.MinMaxCurve(0.03f, 0.12f);

        ps.Play();
        return ps;
    }

    void Animate()
    {
        float pulse = 1f + Mathf.Sin(Time.unscaledTime * 1.2f) * 0.05f;

        for (int i = 0; i < rings.Count; i++)
        {
            LineRenderer ring = rings[i];
            if (ring == null) continue;
            ring.widthMultiplier = (i % 2 == 0 ? 0.07f : 0.045f) * pulse;
            ring.transform.Rotate(Vector3.up, Time.unscaledDeltaTime * (i % 2 == 0 ? 0.8f : -1.1f), Space.World);
        }

        for (int i = 0; i < localLights.Count; i++)
        {
            Light light = localLights[i];
            if (light == null) continue;
            light.intensity = 0.72f + Mathf.Sin(Time.unscaledTime * 1.6f + i * 1.7f) * 0.12f;
        }
    }

    static Material CreateMaterial(string name, Color color, float emission)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
            ?? Shader.Find("Universal Render Pipeline/Unlit")
            ?? Shader.Find("Unlit/Color")
            ?? Shader.Find("Standard");
        Material mat = new Material(shader) { name = name, color = color };
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * emission);
        }
        return mat;
    }
}
