using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Additive objective-arena polish: layered rings, pylons and animated energy accents for Dragon and Medusa pits.
/// Does not replace authored geometry.
/// </summary>
public class AOGObjectiveArenaPolishRuntime : MonoBehaviour
{
    readonly List<Transform> animated = new();
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
        dragonMat = CreateMaterial("DragonArena", new Color(1f, 0.12f, 0.03f), 2.8f);
        medusaMat = CreateMaterial("MedusaArena", new Color(0.18f, 1f, 0.42f), 2.2f);
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

        BuildArena(manager.DragonObject.transform.position, dragonMat, "Dragon");
        BuildArena(manager.MedusaObject.transform.position, medusaMat, "Medusa");
        built = true;
    }

    void BuildArena(Vector3 center, Material mat, string prefix)
    {
        CreateRing(prefix + "_Outer", center, 10f, 0.16f, mat);
        CreateRing(prefix + "_Inner", center, 6.8f, 0.10f, mat);

        for (int i = 0; i < 8; i++)
        {
            float angle = i / 8f * Mathf.PI * 2f;
            Vector3 pos = center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 8.6f;
            GameObject pylon = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pylon.name = prefix + "_Energy_Pylon";
            pylon.transform.position = pos + Vector3.up * 1.1f;
            pylon.transform.localScale = new Vector3(0.22f, 1.1f, 0.22f);
            pylon.GetComponent<Renderer>().sharedMaterial = mat;
            Collider col = pylon.GetComponent<Collider>();
            if (col != null) Destroy(col);
            animated.Add(pylon.transform);
        }
    }

    void CreateRing(string name, Vector3 center, float radius, float thickness, Material mat)
    {
        const int segments = 36;
        for (int i = 0; i < segments; i++)
        {
            float a0 = i / (float)segments * Mathf.PI * 2f;
            float a1 = (i + 1) / (float)segments * Mathf.PI * 2f;
            Vector3 p0 = center + new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0)) * radius;
            Vector3 p1 = center + new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1)) * radius;
            Vector3 dir = p1 - p0;

            GameObject segment = GameObject.CreatePrimitive(PrimitiveType.Cube);
            segment.name = name;
            segment.transform.position = Vector3.Lerp(p0, p1, 0.5f) + Vector3.up * 0.08f;
            segment.transform.rotation = Quaternion.LookRotation(dir.normalized);
            segment.transform.localScale = new Vector3(thickness, 0.05f, dir.magnitude);
            segment.GetComponent<Renderer>().sharedMaterial = mat;
            Collider col = segment.GetComponent<Collider>();
            if (col != null) Destroy(col);
            animated.Add(segment.transform);
        }
    }

    void Animate()
    {
        float pulse = 1f + Mathf.Sin(Time.unscaledTime * 2.2f) * 0.08f;
        for (int i = 0; i < animated.Count; i++)
        {
            Transform t = animated[i];
            if (t == null) continue;
            if (t.name.Contains("Pylon"))
                t.localScale = new Vector3(0.22f * pulse, 1.1f + Mathf.Sin(Time.unscaledTime * 3f + i) * 0.12f, 0.22f * pulse);
            else
                t.Rotate(Vector3.up, Time.unscaledDeltaTime * 3f, Space.World);
        }
    }

    static Material CreateMaterial(string name, Color color, float emission)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
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
