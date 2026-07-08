using UnityEngine;
using UnityEngine.SceneManagement;

public class AOGTowerTestScenePolishRuntime : MonoBehaviour
{
    private const string ManagerName = "AOG_TowerTest_Scene_Polish_Runtime";
    private const string RootName = "AOG_TowerTest_Premium_Polish";

    private Material laneTrimMat;
    private Material laneStoneMat;
    private Material brushMat;
    private Material flowerBlueMat;
    private Material flowerRedMat;
    private Material crystalBlueMat;
    private Material crystalRedMat;
    private Material shadowMat;
    private Material goldMat;

    private readonly System.Random random = new System.Random(92026);

    private static readonly Vector3[] MidLane =
    {
        new Vector3(-105f, 0.05f, -78f),
        new Vector3(-60f, 0.05f, -44f),
        new Vector3(-25f, 0.05f, -18f),
        new Vector3(0f, 0.05f, 0f),
        new Vector3(25f, 0.05f, 18f),
        new Vector3(60f, 0.05f, 44f),
        new Vector3(105f, 0.05f, 78f)
    };

    private static readonly Vector3[] TopLane =
    {
        new Vector3(-105f, 0.05f, -78f),
        new Vector3(-118f, 0.05f, -52f),
        new Vector3(-112f, 0.05f, 10f),
        new Vector3(-92f, 0.05f, 50f),
        new Vector3(-48f, 0.05f, 88f),
        new Vector3(10f, 0.05f, 96f),
        new Vector3(58f, 0.05f, 108f),
        new Vector3(105f, 0.05f, 78f)
    };

    private static readonly Vector3[] BotLane =
    {
        new Vector3(-105f, 0.05f, -78f),
        new Vector3(-58f, 0.05f, -108f),
        new Vector3(-10f, 0.05f, -96f),
        new Vector3(48f, 0.05f, -88f),
        new Vector3(92f, 0.05f, -50f),
        new Vector3(112f, 0.05f, 10f),
        new Vector3(118f, 0.05f, 52f),
        new Vector3(105f, 0.05f, 78f)
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        EnsureManager();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureManager();
    }

    private static void EnsureManager()
    {
        if (Object.FindAnyObjectByType<AOGTowerTestScenePolishRuntime>() != null)
            return;

        GameObject manager = new GameObject(ManagerName);
        manager.AddComponent<AOGTowerTestScenePolishRuntime>();
    }

    private void Start()
    {
        if (!ShouldPolish(SceneManager.GetActiveScene()))
            return;

        BuildPolish();
    }

    private bool ShouldPolish(Scene scene)
    {
        return scene.name.Contains("AOGSymmetricReferenceMap");
    }

    private void BuildPolish()
    {
        GameObject oldRoot = GameObject.Find(RootName);
        if (oldRoot != null)
            Destroy(oldRoot);

        CreateMaterials();
        ConfigureAtmosphere();
        ConfigureCameraAndPlayer();

        GameObject root = new GameObject(RootName);
        CreateLaneTrim(root.transform, MidLane, 14f);
        CreateLaneTrim(root.transform, TopLane, 13f);
        CreateLaneTrim(root.transform, BotLane, 13f);

        CreateLaneStones(root.transform, MidLane, 11f, 44);
        CreateLaneStones(root.transform, TopLane, 10f, 46);
        CreateLaneStones(root.transform, BotLane, 10f, 46);

        CreateJungleDressing(root.transform);
        CreateBaseAccents(root.transform, new Vector3(-105f, 0f, -78f), true);
        CreateBaseAccents(root.transform, new Vector3(105f, 0f, 78f), false);
        CreateObjectiveAccents(root.transform);
        CreateMapDepthShadows(root.transform);
    }

    private void CreateMaterials()
    {
        laneTrimMat = MakeMat("AOG_Polish_Lane_Trim", new Color32(145, 122, 78, 255), new Color(0.35f, 0.22f, 0.08f));
        laneStoneMat = MakeMat("AOG_Polish_Lane_Stones", new Color32(103, 98, 88, 255), new Color(0.04f, 0.035f, 0.025f));
        brushMat = MakeMat("AOG_Polish_Brush", new Color32(16, 82, 38, 255), new Color(0.02f, 0.12f, 0.04f));
        flowerBlueMat = MakeMat("AOG_Polish_Blue_Flowers", new Color32(54, 130, 255, 255), new Color(0.05f, 0.25f, 0.7f));
        flowerRedMat = MakeMat("AOG_Polish_Red_Flowers", new Color32(255, 62, 72, 255), new Color(0.55f, 0.08f, 0.06f));
        crystalBlueMat = MakeMat("AOG_Polish_Blue_Crystal", new Color32(80, 155, 255, 255), new Color(0.08f, 0.32f, 1f));
        crystalRedMat = MakeMat("AOG_Polish_Red_Crystal", new Color32(255, 72, 64, 255), new Color(1f, 0.1f, 0.06f));
        shadowMat = MakeMat("AOG_Polish_Depth_Shadow", new Color32(7, 9, 12, 255), Color.black);
        goldMat = MakeMat("AOG_Polish_Gold_Runes", new Color32(230, 178, 78, 255), new Color(0.8f, 0.38f, 0.08f));
    }

    private Material MakeMat(string name, Color color, Color emission)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        Material material = new Material(shader);
        material.name = name;
        material.color = color;

        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", emission);
        }

        return material;
    }

    private void ConfigureAtmosphere()
    {
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.045f, 0.065f, 0.075f);
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.0065f;
        RenderSettings.ambientLight = new Color(0.28f, 0.32f, 0.34f);

        Light light = Object.FindAnyObjectByType<Light>();
        if (light == null)
        {
            GameObject lightObject = new GameObject("AOG_Premium_Key_Light");
            light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
        }

        light.transform.rotation = Quaternion.Euler(48f, -35f, 18f);
        light.color = new Color(1f, 0.92f, 0.78f);
        light.intensity = 1.25f;
        light.shadows = LightShadows.Soft;
    }

    private void ConfigureCameraAndPlayer()
    {
        GameObject player = AOGChampionVisualApplier.FindPlayerObject();
        if (player == null)
            return;

        AOGMobaCameraController[] controllers = Object.FindObjectsByType<AOGMobaCameraController>(FindObjectsInactive.Exclude);
        foreach (AOGMobaCameraController controller in controllers)
        {
            controller.target = player.transform;
            controller.useOrthographic = true;
            controller.orthographicSize = 25f;
            controller.minZoom = 18f;
            controller.maxZoom = 32f;
            controller.offset = new Vector3(0f, 36f, -30f);
            controller.rotation = new Vector3(60f, 0f, 0f);
        }

        Camera camera = Camera.main;
        if (camera != null)
        {
            camera.orthographic = true;
            camera.orthographicSize = 25f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 600f;
        }
    }

    private void CreateLaneTrim(Transform root, Vector3[] path, float width)
    {
        for (int i = 0; i < path.Length - 1; i++)
        {
            Vector3 a = path[i];
            Vector3 b = path[i + 1];
            Vector3 dir = (b - a).normalized;
            Vector3 side = new Vector3(-dir.z, 0f, dir.x);

            CreateSegment("Lane_Gold_Edge_A", root, a + side * width * 0.52f, b + side * width * 0.52f, 1.15f, 0.12f, laneTrimMat, 0.24f);
            CreateSegment("Lane_Gold_Edge_B", root, a - side * width * 0.52f, b - side * width * 0.52f, 1.15f, 0.12f, laneTrimMat, 0.24f);
        }
    }

    private void CreateLaneStones(Transform root, Vector3[] path, float width, int count)
    {
        float pathLength = PathLength(path);
        for (int i = 0; i < count; i++)
        {
            float t = (i + 0.5f) / count;
            Vector3 point = PointOnPath(path, t);
            Vector3 tangent = TangentOnPath(path, t);
            Vector3 side = new Vector3(-tangent.z, 0f, tangent.x);
            float laneOffset = RandomRange(-width * 0.32f, width * 0.32f);
            float longScale = RandomRange(1.4f, 3.6f);
            float sideScale = RandomRange(0.7f, 1.8f);

            GameObject stone = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stone.name = "Polished_Lane_Slab";
            stone.transform.SetParent(root);
            stone.transform.position = point + side * laneOffset + Vector3.up * 0.34f;
            stone.transform.rotation = Quaternion.LookRotation(tangent) * Quaternion.Euler(0f, RandomRange(-8f, 8f), 0f);
            stone.transform.localScale = new Vector3(sideScale, 0.11f, longScale + pathLength * 0.002f);
            stone.GetComponent<Renderer>().sharedMaterial = laneStoneMat;
        }
    }

    private void CreateJungleDressing(Transform root)
    {
        Vector3[] clusters =
        {
            new Vector3(-84f, 0f, 22f), new Vector3(-62f, 0f, 62f), new Vector3(-20f, 0f, 68f),
            new Vector3(42f, 0f, 58f), new Vector3(82f, 0f, 24f), new Vector3(-42f, 0f, -58f),
            new Vector3(-82f, 0f, -24f), new Vector3(20f, 0f, -68f), new Vector3(62f, 0f, -62f),
            new Vector3(84f, 0f, -22f), new Vector3(-12f, 0f, 28f), new Vector3(12f, 0f, -28f)
        };

        for (int i = 0; i < clusters.Length; i++)
        {
            CreateBrushCluster(root, clusters[i], i % 2 == 0 ? flowerBlueMat : flowerRedMat);
        }
    }

    private void CreateBrushCluster(Transform root, Vector3 center, Material flowerMat)
    {
        GameObject group = new GameObject("Premium_Brush_Cluster");
        group.transform.SetParent(root);
        group.transform.position = center;

        for (int i = 0; i < 16; i++)
        {
            float angle = RandomRange(0f, Mathf.PI * 2f);
            float radius = RandomRange(1.2f, 6.2f);
            Vector3 offset = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);

            GameObject brush = GameObject.CreatePrimitive(i % 3 == 0 ? PrimitiveType.Capsule : PrimitiveType.Sphere);
            brush.name = "Brush_Leaf_Mass";
            brush.transform.SetParent(group.transform);
            brush.transform.localPosition = offset + Vector3.up * RandomRange(0.5f, 1.5f);
            brush.transform.localScale = new Vector3(RandomRange(1.0f, 2.2f), RandomRange(1.2f, 3.2f), RandomRange(1.0f, 2.2f));
            brush.GetComponent<Renderer>().sharedMaterial = brushMat;
        }

        for (int i = 0; i < 8; i++)
        {
            GameObject flower = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            flower.name = "Brush_Flower_Glow";
            flower.transform.SetParent(group.transform);
            flower.transform.localPosition = new Vector3(RandomRange(-5.5f, 5.5f), 0.65f, RandomRange(-5.5f, 5.5f));
            flower.transform.localScale = Vector3.one * RandomRange(0.22f, 0.46f);
            flower.GetComponent<Renderer>().sharedMaterial = flowerMat;
        }
    }

    private void CreateBaseAccents(Transform root, Vector3 center, bool blue)
    {
        Material crystal = blue ? crystalBlueMat : crystalRedMat;
        Color lightColor = blue ? new Color(0.2f, 0.55f, 1f) : new Color(1f, 0.25f, 0.16f);

        CreateRing(root, "Base_Rune_Ring", center + Vector3.up * 0.7f, 27f, 0.1f, crystal);
        CreatePointLight(root, "Base_Aura_Light", center + Vector3.up * 8f, lightColor, 4.8f, 34f);

        for (int i = 0; i < 6; i++)
        {
            float angle = i * Mathf.PI * 2f / 6f;
            Vector3 pos = center + new Vector3(Mathf.Cos(angle) * 22f, 1.2f, Mathf.Sin(angle) * 22f);
            CreateCrystal(root, pos, crystal, lightColor);
        }
    }

    private void CreateObjectiveAccents(Transform root)
    {
        CreateRing(root, "Upper_Objective_Ring", new Vector3(-38f, 0.75f, 36f), 18f, 0.1f, goldMat);
        CreateRing(root, "Lower_Objective_Ring", new Vector3(38f, 0.75f, -36f), 18f, 0.1f, goldMat);
        CreatePointLight(root, "Upper_Objective_Light", new Vector3(-38f, 8f, 36f), new Color(0.95f, 0.64f, 0.22f), 2.3f, 24f);
        CreatePointLight(root, "Lower_Objective_Light", new Vector3(38f, 8f, -36f), new Color(0.7f, 0.28f, 1f), 2.1f, 24f);
    }

    private void CreateMapDepthShadows(Transform root)
    {
        CreateSegment("Outer_Shadow_North", root, new Vector3(-135f, 0.2f, 116f), new Vector3(135f, 0.2f, 116f), 5f, 0.16f, shadowMat, 0.2f);
        CreateSegment("Outer_Shadow_South", root, new Vector3(-135f, 0.2f, -116f), new Vector3(135f, 0.2f, -116f), 5f, 0.16f, shadowMat, 0.2f);
        CreateSegment("Outer_Shadow_West", root, new Vector3(-135f, 0.2f, -116f), new Vector3(-135f, 0.2f, 116f), 5f, 0.16f, shadowMat, 0.2f);
        CreateSegment("Outer_Shadow_East", root, new Vector3(135f, 0.2f, -116f), new Vector3(135f, 0.2f, 116f), 5f, 0.16f, shadowMat, 0.2f);
    }

    private void CreateSegment(string name, Transform parent, Vector3 a, Vector3 b, float width, float height, Material material, float y)
    {
        Vector3 mid = (a + b) * 0.5f;
        Vector3 dir = b - a;
        float length = dir.magnitude;
        if (length < 0.01f)
            return;

        GameObject segment = GameObject.CreatePrimitive(PrimitiveType.Cube);
        segment.name = name;
        segment.transform.SetParent(parent);
        segment.transform.position = new Vector3(mid.x, y, mid.z);
        segment.transform.rotation = Quaternion.LookRotation(new Vector3(dir.x, 0f, dir.z).normalized);
        segment.transform.localScale = new Vector3(width, height, length);
        segment.GetComponent<Renderer>().sharedMaterial = material;
    }

    private void CreateRing(Transform parent, string name, Vector3 position, float radius, float height, Material material)
    {
        GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.name = name;
        ring.transform.SetParent(parent);
        ring.transform.position = position;
        ring.transform.localScale = new Vector3(radius, height, radius);
        ring.GetComponent<Renderer>().sharedMaterial = material;
    }

    private void CreateCrystal(Transform parent, Vector3 position, Material material, Color lightColor)
    {
        GameObject crystal = GameObject.CreatePrimitive(PrimitiveType.Cube);
        crystal.name = "Base_Crystal_Accent";
        crystal.transform.SetParent(parent);
        crystal.transform.position = position;
        crystal.transform.rotation = Quaternion.Euler(0f, 45f, 0f);
        crystal.transform.localScale = new Vector3(1.2f, 2.4f, 1.2f);
        crystal.GetComponent<Renderer>().sharedMaterial = material;
        CreatePointLight(parent, "Crystal_Glow", position + Vector3.up * 1.5f, lightColor, 1.2f, 9f);
    }

    private void CreatePointLight(Transform parent, string name, Vector3 position, Color color, float intensity, float range)
    {
        GameObject lightObject = new GameObject(name);
        lightObject.transform.SetParent(parent);
        lightObject.transform.position = position;
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
        light.shadows = LightShadows.None;
    }

    private float PathLength(Vector3[] path)
    {
        float length = 0f;
        for (int i = 0; i < path.Length - 1; i++)
        {
            length += Vector3.Distance(path[i], path[i + 1]);
        }

        return length;
    }

    private Vector3 PointOnPath(Vector3[] path, float t)
    {
        float totalLength = PathLength(path);
        float target = Mathf.Clamp01(t) * totalLength;
        float walked = 0f;

        for (int i = 0; i < path.Length - 1; i++)
        {
            float segmentLength = Vector3.Distance(path[i], path[i + 1]);
            if (walked + segmentLength >= target)
            {
                float segmentT = (target - walked) / Mathf.Max(0.001f, segmentLength);
                return Vector3.Lerp(path[i], path[i + 1], segmentT);
            }

            walked += segmentLength;
        }

        return path[path.Length - 1];
    }

    private Vector3 TangentOnPath(Vector3[] path, float t)
    {
        float totalLength = PathLength(path);
        float target = Mathf.Clamp01(t) * totalLength;
        float walked = 0f;

        for (int i = 0; i < path.Length - 1; i++)
        {
            float segmentLength = Vector3.Distance(path[i], path[i + 1]);
            if (walked + segmentLength >= target)
                return (path[i + 1] - path[i]).normalized;

            walked += segmentLength;
        }

        return (path[path.Length - 1] - path[path.Length - 2]).normalized;
    }

    private float RandomRange(float min, float max)
    {
        return min + (float)random.NextDouble() * (max - min);
    }
}
