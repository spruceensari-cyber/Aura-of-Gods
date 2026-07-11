using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// Owns the final readable, original Aetherfront map presentation. Gameplay routes, combat
/// colliders, spawn points and objective locations remain in their existing systems.
/// </summary>
[DefaultExecutionOrder(-9000)]
public class AOGAetherfrontMapVisualAuthorityRuntime : MonoBehaviour
{
    private readonly Dictionary<string, Material> materials = new Dictionary<string, Material>();
    private Transform root;
    private bool built;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindFirstObjectByType<AOGAetherfrontMapVisualAuthorityRuntime>() != null)
            return;

        GameObject host = new GameObject("AOG_Aetherfront_Map_Visual_Authority");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGAetherfrontMapVisualAuthorityRuntime>();
    }

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
        QueueBuild();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        QueueBuild();
    }

    private void QueueBuild()
    {
        StopAllCoroutines();
        built = false;
        root = null;
        DisableLegacyPresentationPasses();
        StartCoroutine(BuildWhenMapIsReady());
    }

    private IEnumerator BuildWhenMapIsReady()
    {
        for (int attempt = 0; attempt < 30; attempt++)
        {
            GameObject map = GameObject.Find("AOG_Symmetric_Reference_Map");
            if (map != null)
            {
                Build(map.transform);
                yield break;
            }

            yield return new WaitForSecondsRealtime(0.15f);
        }

        Debug.LogWarning("AOG Aetherfront map pass could not find the gameplay map.");
    }

    private void Build(Transform map)
    {
        if (built || map == null)
            return;

        built = true;
        RemoveLegacyVisualRoots();

        Transform old = map.Find("Aetherfront_Visual_Pass");
        if (old != null)
            Destroy(old.gameObject);

        root = new GameObject("Aetherfront_Visual_Pass").transform;
        root.SetParent(map, false);

        ApplyLighting();
        BuildLaneSurfaces();
        BuildAetherRiver();
        BuildForestAndRuinSilhouettes();
        BuildFactionLandmarks();
        BuildTowerAdornments();
    }

    private void DisableLegacyPresentationPasses()
    {
        Disable<AOGPremiumMobaLookRuntime>();
        Disable<AOGPremiumMapVisualRuntime>();
        Disable<AOGWorldArtDirectorRuntime>();
        Disable<AOGMapSpatialIdentityRuntime>();
        Disable<AOGMapBeautyReadabilityRuntime>();
        Disable<AOGFinalMapArtPassRuntime>();
        Disable<AOGBenchmarkMapArtDirectionRuntime>();
        Disable<AOGDistinctJungleFlowRuntime>();
    }

    private static void Disable<T>() where T : Behaviour
    {
        foreach (T runtime in FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (runtime != null)
                runtime.enabled = false;
        }
    }

    private static void RemoveLegacyVisualRoots()
    {
        string[] names =
        {
            "AOG_Premium_Moba_Art",
            "AOG_Map_Spatial_Identity_Pass",
            "11_Distinct_Jungle_Flow",
            "12_Map_Beauty_Readability_Pass",
            "12_Final_Map_Art_Pass"
        };

        foreach (string name in names)
        {
            GameObject existing = GameObject.Find(name);
            if (existing != null)
                Destroy(existing);
        }
    }

    private void ApplyLighting()
    {
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.16f, 0.23f, 0.27f);
        RenderSettings.ambientEquatorColor = new Color(0.085f, 0.13f, 0.105f);
        RenderSettings.ambientGroundColor = new Color(0.018f, 0.028f, 0.025f);
        RenderSettings.ambientIntensity = 1.0f;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.025f, 0.045f, 0.050f);
        RenderSettings.fogDensity = 0.0034f;

        foreach (Light light in FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (light == null || light.type != LightType.Directional)
                continue;

            light.color = new Color(0.93f, 0.92f, 0.82f);
            light.intensity = 1.28f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.78f;
            light.transform.rotation = Quaternion.Euler(48f, -42f, 8f);
        }
    }

    private void BuildLaneSurfaces()
    {
        MinionSpawner spawner = FindFirstObjectByType<MinionSpawner>();
        if (spawner == null)
            return;

        BuildLane("Top", spawner.topLaneWaypoints, 8.8f, new Color(0.19f, 0.17f, 0.13f), new Color(0.40f, 0.31f, 0.17f));
        BuildLane("Mid", spawner.midLaneWaypoints, 9.6f, new Color(0.16f, 0.17f, 0.18f), new Color(0.29f, 0.36f, 0.40f));
        BuildLane("Bot", spawner.botLaneWaypoints, 8.8f, new Color(0.12f, 0.18f, 0.15f), new Color(0.16f, 0.42f, 0.30f));
    }

    private void BuildLane(string laneName, Transform[] waypoints, float width, Color stoneColor, Color seamColor)
    {
        if (waypoints == null || waypoints.Length < 2)
            return;

        Transform laneRoot = new GameObject("Aetherfront_" + laneName + "_Lane").transform;
        laneRoot.SetParent(root, false);
        Material stone = Lit(laneName + "_Lane_Stone", stoneColor, 0.30f, 0.10f);
        Material seam = Emissive(laneName + "_Lane_Seam", seamColor, laneName == "Mid" ? 1.05f : 0.55f);
        Material edge = Lit(laneName + "_Lane_Edge", Color.Lerp(stoneColor, Color.black, 0.50f), 0.20f, 0.18f);

        for (int segment = 0; segment < waypoints.Length - 1; segment++)
        {
            if (waypoints[segment] == null || waypoints[segment + 1] == null)
                continue;

            Vector3 a = waypoints[segment].position + Vector3.up * 0.095f;
            Vector3 b = waypoints[segment + 1].position + Vector3.up * 0.095f;
            Vector3 delta = b - a;
            delta.y = 0f;
            float length = delta.magnitude;
            if (length < 1f)
                continue;

            Vector3 direction = delta / length;
            Vector3 side = new Vector3(-direction.z, 0f, direction.x);
            int slabCount = Mathf.Max(1, Mathf.CeilToInt(length / 4.6f));
            for (int slab = 0; slab < slabCount; slab++)
            {
                float t = (slab + 0.5f) / slabCount;
                Vector3 center = Vector3.Lerp(a, b, t);
                float stagger = ((segment + slab) % 2 == 0 ? -0.32f : 0.32f);
                center += side * stagger;
                CreateCube(laneName + "_Paver_" + segment + "_" + slab, laneRoot, center,
                    Quaternion.LookRotation(direction), new Vector3(width, 0.09f, length / slabCount * 0.84f), stone);

                if (slab % 2 == 0)
                {
                    CreateCube(laneName + "_Paver_Seam_" + segment + "_" + slab, laneRoot, center + Vector3.up * 0.055f,
                        Quaternion.LookRotation(direction), new Vector3(0.10f, 0.018f, length / slabCount * 0.72f), seam);
                }
            }

            if (segment % 2 == 0)
            {
                Vector3 center = Vector3.Lerp(a, b, 0.5f);
                CreateCube(laneName + "_Edge_Left_" + segment, laneRoot, center + side * (width * 0.56f),
                    Quaternion.LookRotation(direction), new Vector3(0.38f, 0.22f, length * 0.92f), edge);
                CreateCube(laneName + "_Edge_Right_" + segment, laneRoot, center - side * (width * 0.56f),
                    Quaternion.LookRotation(direction), new Vector3(0.38f, 0.22f, length * 0.92f), edge);
            }
        }
    }

    private void BuildAetherRiver()
    {
        Vector3[] points =
        {
            new Vector3(-88f, 0.075f, 52f),
            new Vector3(-56f, 0.075f, 36f),
            new Vector3(-20f, 0.075f, 18f),
            new Vector3(12f, 0.075f, -6f),
            new Vector3(44f, 0.075f, -24f),
            new Vector3(80f, 0.075f, -46f)
        };

        Transform riverRoot = new GameObject("Aetherfront_River").transform;
        riverRoot.SetParent(root, false);
        Material water = Lit("Aetherfront_River_Water", new Color(0.025f, 0.14f, 0.17f), 0.82f, 0.08f);
        Material flow = Emissive("Aetherfront_River_Flow", new Color(0.08f, 0.56f, 0.74f), 1.65f);

        for (int i = 0; i < points.Length - 1; i++)
        {
            Vector3 delta = points[i + 1] - points[i];
            delta.y = 0f;
            float length = delta.magnitude;
            Vector3 direction = delta.normalized;
            Vector3 center = (points[i] + points[i + 1]) * 0.5f;
            CreateCube("River_Surface_" + i, riverRoot, center, Quaternion.LookRotation(direction), new Vector3(10.2f, 0.045f, length), water);
            CreateCube("River_Flow_" + i, riverRoot, center + Vector3.up * 0.035f, Quaternion.LookRotation(direction), new Vector3(0.16f, 0.018f, length * 0.82f), flow);
        }
    }

    private void BuildForestAndRuinSilhouettes()
    {
        Transform forest = new GameObject("Aetherfront_Forest_And_Ruins").transform;
        forest.SetParent(root, false);
        Material trunk = Lit("Aetherfront_Tree_Trunk", new Color(0.09f, 0.045f, 0.025f), 0.16f, 0.02f);
        Material canopy = Lit("Aetherfront_Canopy", new Color(0.012f, 0.105f, 0.050f), 0.12f, 0f);
        Material stone = Lit("Aetherfront_Ruin_Stone", new Color(0.10f, 0.12f, 0.12f), 0.24f, 0.22f);

        Random.State state = Random.state;
        Random.InitState(9137);
        for (int i = 0; i < 120; i++)
        {
            Vector3 point = new Vector3(Random.Range(-145f, 145f), 0f, Random.Range(-138f, 138f));
            if (NearCentralLanes(point) || point.magnitude < 20f)
                continue;

            BuildTreeCluster(forest, point, trunk, canopy, Random.Range(0.75f, 1.3f));
            if (i % 5 == 0)
                BuildRuin(forest, point + new Vector3(Random.Range(-3f, 3f), 0f, Random.Range(-3f, 3f)), stone);
        }
        Random.state = state;
    }

    private static bool NearCentralLanes(Vector3 point)
    {
        Vector3[] mid = { new Vector3(-105f, 0f, -78f), Vector3.zero, new Vector3(105f, 0f, 78f) };
        Vector3[] top = { new Vector3(-105f, 0f, -78f), new Vector3(-92f, 0f, 52f), new Vector3(105f, 0f, 78f) };
        Vector3[] bot = { new Vector3(-105f, 0f, -78f), new Vector3(12f, 0f, -98f), new Vector3(105f, 0f, 78f) };
        return DistanceToPolyline(point, mid) < 16f || DistanceToPolyline(point, top) < 15f || DistanceToPolyline(point, bot) < 15f;
    }

    private static float DistanceToPolyline(Vector3 point, Vector3[] points)
    {
        float best = float.MaxValue;
        for (int i = 0; i < points.Length - 1; i++)
        {
            Vector3 a = points[i];
            Vector3 b = points[i + 1];
            Vector3 ab = b - a;
            float t = Mathf.Clamp01(Vector3.Dot(point - a, ab) / Mathf.Max(0.001f, ab.sqrMagnitude));
            best = Mathf.Min(best, Vector3.Distance(point, a + ab * t));
        }
        return best;
    }

    private void BuildTreeCluster(Transform parent, Vector3 center, Material trunk, Material canopy, float scale)
    {
        GameObject cluster = new GameObject("Aetherwood_Cluster");
        cluster.transform.SetParent(parent, false);
        cluster.transform.position = center;

        CreateCylinder("Trunk", cluster.transform, new Vector3(0f, 1.4f * scale, 0f), new Vector3(0.28f * scale, 1.4f * scale, 0.28f * scale), trunk);
        CreateSphere("Canopy_Low", cluster.transform, new Vector3(-0.35f * scale, 3.2f * scale, 0f), new Vector3(1.5f * scale, 1.7f * scale, 1.45f * scale), canopy);
        CreateSphere("Canopy_High", cluster.transform, new Vector3(0.32f * scale, 4.05f * scale, 0.15f * scale), new Vector3(1.25f * scale, 1.5f * scale, 1.2f * scale), canopy);
    }

    private void BuildRuin(Transform parent, Vector3 center, Material stone)
    {
        GameObject ruin = new GameObject("Aetherfront_Ruin");
        ruin.transform.SetParent(parent, false);
        ruin.transform.position = center;
        CreateCube("Ruin_Column", ruin.transform, new Vector3(-0.72f, 1.15f, 0f), Quaternion.Euler(7f, 18f, -5f), new Vector3(0.42f, 2.3f, 0.50f), stone);
        CreateCube("Ruin_Slab", ruin.transform, new Vector3(0.55f, 0.34f, 0.38f), Quaternion.Euler(0f, 34f, 8f), new Vector3(1.6f, 0.25f, 0.8f), stone);
    }

    private void BuildFactionLandmarks()
    {
        BuildBaseLandmark(MinionTeam.Blue, new Color(0.16f, 0.62f, 1f));
        BuildBaseLandmark(MinionTeam.Red, new Color(1f, 0.16f, 0.22f));
    }

    private void BuildBaseLandmark(MinionTeam team, Color accent)
    {
        Transform spawn = FindBaseSpawn(team);
        if (spawn == null)
            return;

        Transform baseRoot = new GameObject(team + "_Aetherfront_Base_Landmark").transform;
        baseRoot.SetParent(root, false);
        baseRoot.position = spawn.position;
        Material stone = Lit(team + "_Base_Stone", Color.Lerp(new Color(0.055f, 0.06f, 0.075f), accent, 0.18f), 0.32f, 0.24f);
        Material energy = Emissive(team + "_Base_Energy", accent, 3.4f);

        for (int i = 0; i < 4; i++)
        {
            float angle = i * Mathf.PI * 2f / 4f + Mathf.PI * 0.25f;
            Vector3 point = new Vector3(Mathf.Cos(angle) * 9.5f, 2.6f, Mathf.Sin(angle) * 9.5f);
            CreateCube("Base_Obelisk_" + i, baseRoot, point, Quaternion.Euler(0f, -angle * Mathf.Rad2Deg, i % 2 == 0 ? 10f : -10f), new Vector3(0.68f, 5.2f, 0.92f), stone);
            CreateSphere("Base_Flame_" + i, baseRoot, point + Vector3.up * 3.0f, Vector3.one * 0.42f, energy);
        }

        CreateRing(baseRoot, 7.0f, accent, 0.13f, "Base_Aether_Ring");
    }

    private static Transform FindBaseSpawn(MinionTeam team)
    {
        string[] names = team == MinionTeam.Blue
            ? new[] { "BlueBaseSpawn", "BlueSpawn", "Blue_Spawn" }
            : new[] { "RedBaseSpawn", "RedSpawn", "Red_Spawn" };

        foreach (GameObject candidate in FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (candidate == null)
                continue;

            foreach (string name in names)
            {
                if (string.Equals(candidate.name, name, System.StringComparison.OrdinalIgnoreCase))
                    return candidate.transform;
            }
        }

        return null;
    }

    private void BuildTowerAdornments()
    {
        foreach (TowerHealth tower in FindObjectsByType<TowerHealth>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (tower == null || tower.transform.Find("Aetherfront_Tower_Adornment") != null)
                continue;

            Color accent = tower.towerTeam == MinionTeam.Blue ? new Color(0.16f, 0.62f, 1f) : new Color(1f, 0.16f, 0.22f);
            Transform towerRoot = new GameObject("Aetherfront_Tower_Adornment").transform;
            towerRoot.SetParent(tower.transform, false);
            Material energy = Emissive("Tower_Core_" + tower.towerTeam, accent, 4.0f);
            CreateSphere("Tower_Core", towerRoot, new Vector3(0f, 4.75f, 0f), Vector3.one * 0.50f, energy);
            CreateRing(towerRoot, 2.0f, accent, 0.075f, "Tower_Rune_Ring");
        }
    }

    private void CreateRing(Transform parent, float radius, Color color, float width, string name)
    {
        GameObject ring = new GameObject(name);
        ring.transform.SetParent(parent, false);
        ring.transform.localPosition = new Vector3(0f, 0.12f, 0f);
        LineRenderer line = ring.AddComponent<LineRenderer>();
        line.loop = true;
        line.useWorldSpace = false;
        line.positionCount = 64;
        line.startWidth = width;
        line.endWidth = width;
        line.sharedMaterial = Emissive(name + "_Material", color, 2.2f);
        line.shadowCastingMode = ShadowCastingMode.Off;
        for (int i = 0; i < line.positionCount; i++)
        {
            float angle = i * Mathf.PI * 2f / line.positionCount;
            line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
        }
    }

    private void CreateCube(string name, Transform parent, Vector3 position, Quaternion rotation, Vector3 scale, Material material)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = parent == root ? position : position;
        go.transform.localRotation = rotation;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = material;
        RemoveCollider(go);
    }

    private void CreateCylinder(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = material;
        RemoveCollider(go);
    }

    private void CreateSphere(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = material;
        RemoveCollider(go);
    }

    private Material Lit(string key, Color color, float smoothness, float metallic)
    {
        if (materials.TryGetValue(key, out Material existing) && existing != null)
            return existing;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");
        Material material = new Material(shader) { name = key, color = color, enableInstancing = true };
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
        materials[key] = material;
        return material;
    }

    private Material Emissive(string key, Color color, float strength)
    {
        Material material = Lit(key, color, 0.42f, 0.10f);
        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * strength);
        }
        return material;
    }

    private static void RemoveCollider(GameObject go)
    {
        Collider collider = go.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);
    }
}
