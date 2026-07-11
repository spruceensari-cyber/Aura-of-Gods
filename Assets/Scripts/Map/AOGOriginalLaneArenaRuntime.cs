using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Replaces late legacy presentation clutter with a clean, original three-lane battlefield layer.
/// Existing gameplay waypoints, towers, nexuses, minion paths and objective AI remain authoritative.
/// Decorative pieces are non-blocking; only the main ground keeps collision.
/// </summary>
[DefaultExecutionOrder(16350)]
public class AOGOriginalLaneArenaRuntime : MonoBehaviour
{
    private static readonly string[] LegacyRoots =
    {
        "AOG_Runtime_World_Art_Layer",
        "12_Map_Beauty_Readability_Pass",
        "AOG_Premium_Map_Pass",
        "10_Expanded_Objective_Pocket",
        "08_Dark_Forest_Border",
        "AOG_Benchmark_Map_Art_Direction_Runtime",
        "AOG_World_Art_Director"
    };

    private readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
    private Transform root;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindFirstObjectByType<AOGOriginalLaneArenaRuntime>() != null) return;
        GameObject host = new GameObject("AOG_Original_Lane_Arena_Runtime");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGOriginalLaneArenaRuntime>();
    }

    private IEnumerator Start()
    {
        yield return new WaitForSecondsRealtime(1.8f);
        Build();
    }

    private void Build()
    {
        RemoveLegacyPresentation();
        if (root != null) Destroy(root.gameObject);
        root = new GameObject("AOG_Original_Competitive_Arena").transform;

        BuildGround();
        BuildRiver();
        BuildLanes();
        BuildObjectivePits();
        BuildNonBlockingLaneEdges();
        ImproveLighting();
    }

    private static void RemoveLegacyPresentation()
    {
        foreach (Transform t in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t == null) continue;
            string n = t.name;
            bool exact = false;
            for (int i = 0; i < LegacyRoots.Length; i++)
                if (n == LegacyRoots[i]) { exact = true; break; }

            bool clutter = n.Contains("Jungle") || n.Contains("Forest") || n.Contains("Grass_Tuft") ||
                           n.Contains("Lane_Rock") || n.Contains("Aether_Lantern") ||
                           n.Contains("Readable_Boundary") || n.Contains("Terrain_Grade") ||
                           n.Contains("Camp_Ruin") || n.Contains("Expanded_Border_Tree");

            if (exact || clutter)
                t.gameObject.SetActive(false);
        }
    }

    private void BuildGround()
    {
        Material ground = Lit("Arena_Ground", new Color(0.030f, 0.090f, 0.050f), 0.12f, 0.01f);
        GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Cube);
        plane.name = "Arena_Main_Ground";
        plane.transform.SetParent(root, false);
        plane.transform.position = new Vector3(0f, -0.35f, 0f);
        plane.transform.localScale = new Vector3(330f, 0.7f, 300f);
        plane.GetComponent<Renderer>().sharedMaterial = ground;
    }

    private void BuildRiver()
    {
        Material water = Emissive("Arena_River", new Color(0.025f, 0.20f, 0.28f), 1.35f);
        GameObject river = GameObject.CreatePrimitive(PrimitiveType.Cube);
        river.name = "Arena_Central_River";
        river.transform.SetParent(root, false);
        river.transform.position = new Vector3(0f, 0.02f, 0f);
        river.transform.rotation = Quaternion.Euler(0f, 45f, 0f);
        river.transform.localScale = new Vector3(18f, 0.10f, 220f);
        river.GetComponent<Renderer>().sharedMaterial = water;
        Collider c = river.GetComponent<Collider>(); if (c != null) Destroy(c);
    }

    private void BuildLanes()
    {
        Material stone = Lit("Arena_Lane_Stone", new Color(0.22f, 0.215f, 0.19f), 0.30f, 0.08f);
        Material edge = Lit("Arena_Lane_Edge", new Color(0.080f, 0.075f, 0.070f), 0.22f, 0.14f);

        MinionSpawner spawner = FindFirstObjectByType<MinionSpawner>();
        if (spawner == null) return;

        BuildLaneFromWaypoints("Top", spawner.topLaneWaypoints, spawner.blueBaseSpawn, spawner.redBaseSpawn, 9.5f, stone, edge);
        BuildLaneFromWaypoints("Mid", spawner.midLaneWaypoints, spawner.blueBaseSpawn, spawner.redBaseSpawn, 10.5f, stone, edge);
        BuildLaneFromWaypoints("Bot", spawner.botLaneWaypoints, spawner.blueBaseSpawn, spawner.redBaseSpawn, 9.5f, stone, edge);
    }

    private void BuildLaneFromWaypoints(string laneName, Transform[] waypoints, Transform blueSpawn, Transform redSpawn, float width, Material stone, Material edge)
    {
        List<Vector3> points = new List<Vector3>();
        if (blueSpawn != null) points.Add(blueSpawn.position);
        if (waypoints != null)
            foreach (Transform t in waypoints) if (t != null) points.Add(t.position);
        if (redSpawn != null) points.Add(redSpawn.position);
        if (points.Count < 2) return;

        Transform laneRoot = new GameObject("Arena_" + laneName + "_Lane").transform;
        laneRoot.SetParent(root, false);
        for (int i = 0; i < points.Count - 1; i++)
        {
            CreateSegment(laneRoot, laneName + "_Lane_" + i, points[i], points[i + 1], width, 0.12f, stone);
            CreateSegment(laneRoot, laneName + "_Edge_L_" + i, Offset(points[i], points[i + 1], width * 0.56f), Offset(points[i + 1], points[i], -width * 0.56f), 0.55f, 0.20f, edge);
            CreateSegment(laneRoot, laneName + "_Edge_R_" + i, Offset(points[i], points[i + 1], -width * 0.56f), Offset(points[i + 1], points[i], width * 0.56f), 0.55f, 0.20f, edge);
        }
    }

    private static Vector3 Offset(Vector3 from, Vector3 to, float amount)
    {
        Vector3 f = to - from; f.y = 0f;
        if (f.sqrMagnitude < 0.001f) return from;
        f.Normalize();
        Vector3 right = new Vector3(f.z, 0f, -f.x);
        Vector3 result = from + right * amount;
        result.y = 0.03f;
        return result;
    }

    private static void CreateSegment(Transform parent, string name, Vector3 a, Vector3 b, float width, float height, Material mat)
    {
        Vector3 delta = b - a; delta.y = 0f;
        if (delta.sqrMagnitude < 0.01f) return;
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.position = (a + b) * 0.5f + Vector3.up * 0.02f;
        go.transform.rotation = Quaternion.LookRotation(delta.normalized);
        go.transform.localScale = new Vector3(width, height, delta.magnitude);
        go.GetComponent<Renderer>().sharedMaterial = mat;
        Collider c = go.GetComponent<Collider>(); if (c != null) Destroy(c);
    }

    private void BuildObjectivePits()
    {
        BuildPit(new Vector3(-38f, 0.05f, 36f), 13f, new Color(0.86f, 0.22f, 0.06f), "Dragon_Pit");
        BuildPit(new Vector3(38f, 0.05f, -36f), 13f, new Color(0.42f, 0.16f, 0.76f), "Medusa_Pit");
    }

    private void BuildPit(Vector3 center, float radius, Color accent, string name)
    {
        Material basin = Lit(name + "_Basin", new Color(0.055f, 0.055f, 0.065f), 0.26f, 0.18f);
        Material energy = Emissive(name + "_Energy", accent, 2.4f);

        GameObject disk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        disk.name = name + "_Floor";
        disk.transform.SetParent(root, false);
        disk.transform.position = center;
        disk.transform.localScale = new Vector3(radius, 0.12f, radius);
        disk.GetComponent<Renderer>().sharedMaterial = basin;
        Collider dc = disk.GetComponent<Collider>(); if (dc != null) Destroy(dc);

        GameObject ring = new GameObject(name + "_Ring");
        ring.transform.SetParent(root, false);
        ring.transform.position = center + Vector3.up * 0.15f;
        LineRenderer line = ring.AddComponent<LineRenderer>();
        line.loop = true;
        line.useWorldSpace = false;
        line.positionCount = 64;
        line.startWidth = 0.16f;
        line.endWidth = 0.16f;
        line.sharedMaterial = energy;
        line.shadowCastingMode = ShadowCastingMode.Off;
        for (int i = 0; i < line.positionCount; i++)
        {
            float a = i * Mathf.PI * 2f / line.positionCount;
            line.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius));
        }
    }

    private void BuildNonBlockingLaneEdges()
    {
        foreach (Collider c in root.GetComponentsInChildren<Collider>(true))
        {
            if (c == null) continue;
            if (c.gameObject.name == "Arena_Main_Ground") continue;
            c.enabled = false;
        }
    }

    private static void ImproveLighting()
    {
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.18f, 0.22f, 0.27f);
        RenderSettings.ambientEquatorColor = new Color(0.10f, 0.14f, 0.13f);
        RenderSettings.ambientGroundColor = new Color(0.025f, 0.04f, 0.03f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.035f, 0.050f, 0.060f);
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.0032f;
    }

    private Material Lit(string key, Color color, float smoothness, float metallic)
    {
        if (materials.TryGetValue(key, out Material cached) && cached != null) return cached;
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        Material mat = new Material(shader) { name = key, color = color, enableInstancing = true };
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
        materials[key] = mat;
        return mat;
    }

    private Material Emissive(string key, Color color, float strength)
    {
        Material mat = Lit(key, color, 0.42f, 0.12f);
        if (mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * strength);
        }
        return mat;
    }
}
