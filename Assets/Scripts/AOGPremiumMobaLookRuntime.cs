using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class AOGPremiumMobaLookRuntime : MonoBehaviour
{
    private const string ManagerName = "AOG_Premium_Moba_Look_Runtime";
    private const string ArtRootName = "AOG_Premium_Moba_Art";

    private readonly System.Random rng = new System.Random(7301);
    private Material grassMat;
    private Material darkGrassMat;
    private Material laneDirtMat;
    private Material stoneMat;
    private Material stoneEdgeMat;
    private Material cliffMat;
    private Material brushMat;
    private Material flowerMat;
    private Material goldRuneMat;
    private Material blueGlowMat;
    private Material redGlowMat;
    private Material shadowMat;

    private static readonly Vector3[] MidLane =
    {
        new Vector3(-105f, 0f, -78f),
        new Vector3(-62f, 0f, -45f),
        new Vector3(-25f, 0f, -18f),
        new Vector3(0f, 0f, 0f),
        new Vector3(25f, 0f, 18f),
        new Vector3(62f, 0f, 45f),
        new Vector3(105f, 0f, 78f)
    };

    private static readonly Vector3[] TopLane =
    {
        new Vector3(-105f, 0f, -78f),
        new Vector3(-119f, 0f, -48f),
        new Vector3(-112f, 0f, 18f),
        new Vector3(-82f, 0f, 64f),
        new Vector3(-34f, 0f, 91f),
        new Vector3(28f, 0f, 98f),
        new Vector3(78f, 0f, 92f),
        new Vector3(105f, 0f, 78f)
    };

    private static readonly Vector3[] BotLane =
    {
        new Vector3(-105f, 0f, -78f),
        new Vector3(-78f, 0f, -92f),
        new Vector3(-28f, 0f, -98f),
        new Vector3(34f, 0f, -91f),
        new Vector3(82f, 0f, -64f),
        new Vector3(112f, 0f, -18f),
        new Vector3(119f, 0f, 48f),
        new Vector3(105f, 0f, 78f)
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
        if (Object.FindAnyObjectByType<AOGPremiumMobaLookRuntime>() != null)
            return;

        GameObject manager = new GameObject(ManagerName);
        manager.AddComponent<AOGPremiumMobaLookRuntime>();
    }

    private void Start()
    {
        ApplyPremiumLook();
    }

    [ContextMenu("Apply Premium MOBA Look")]
    public void ApplyPremiumLook()
    {
        CreateMaterials();
        ApplyLighting();
        BuildMapArt();
        AddTowerLights();
        InstallUnitAnimators();
    }

    private void CreateMaterials()
    {
        grassMat = MakeMat("AOG_Premium_Grass", new Color32(56, 105, 48, 255), 0f);
        darkGrassMat = MakeMat("AOG_Premium_Dark_Grass", new Color32(22, 66, 38, 255), 0f);
        laneDirtMat = MakeMat("AOG_Premium_Worn_Lane_Dirt", new Color32(116, 102, 78, 255), 0f);
        stoneMat = MakeMat("AOG_Premium_Painted_Stone", new Color32(126, 122, 108, 255), 0f);
        stoneEdgeMat = MakeMat("AOG_Premium_Cool_Edge_Stone", new Color32(77, 82, 82, 255), 0f);
        cliffMat = MakeMat("AOG_Premium_Cliff_Rock", new Color32(62, 58, 62, 255), 0f);
        brushMat = MakeMat("AOG_Premium_Readable_Brush", new Color32(30, 91, 45, 255), 0f);
        flowerMat = MakeMat("AOG_Premium_Field_Accent", new Color32(211, 181, 74, 255), 0f);
        goldRuneMat = MakeMat("AOG_Premium_Gold_Rune", new Color32(219, 161, 59, 255), 0.65f);
        blueGlowMat = MakeMat("AOG_Premium_Blue_Team_Glow", new Color32(62, 166, 255, 255), 1.3f);
        redGlowMat = MakeMat("AOG_Premium_Red_Team_Glow", new Color32(255, 74, 56, 255), 1.3f);
        shadowMat = MakeMat("AOG_Premium_Soft_Ground_Shadow", new Color32(18, 20, 18, 160), 0f);
    }

    private Material MakeMat(string name, Color color, float emission)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        Material mat = new Material(shader);
        mat.name = name;
        mat.color = color;

        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);

        if (emission > 0f && mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * emission);
        }

        return mat;
    }

    private void ApplyLighting()
    {
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.28f, 0.34f, 0.35f);
        RenderSettings.ambientEquatorColor = new Color(0.22f, 0.24f, 0.19f);
        RenderSettings.ambientGroundColor = new Color(0.08f, 0.075f, 0.07f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.13f, 0.16f, 0.15f);
        RenderSettings.fogDensity = 0.0065f;

        Light sun = FindMainDirectionalLight();
        sun.name = "AOG_Premium_Key_Light";
        sun.type = LightType.Directional;
        sun.transform.rotation = Quaternion.Euler(48f, -38f, 14f);
        sun.color = new Color(1f, 0.91f, 0.78f);
        sun.intensity = 1.45f;
        sun.shadows = LightShadows.Soft;
        sun.shadowStrength = 0.72f;

        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.fieldOfView = Mathf.Clamp(cam.fieldOfView, 38f, 50f);
            cam.backgroundColor = new Color(0.12f, 0.15f, 0.16f);
        }
    }

    private Light FindMainDirectionalLight()
    {
        Light[] lights = Object.FindObjectsByType<Light>(FindObjectsInactive.Include);
        foreach (Light light in lights)
        {
            if (light.type == LightType.Directional)
                return light;
        }

        GameObject lightObj = new GameObject("AOG_Premium_Key_Light");
        return lightObj.AddComponent<Light>();
    }

    private void BuildMapArt()
    {
        Transform old = transform.Find(ArtRootName);
        if (old != null)
            Destroy(old.gameObject);

        GameObject artRoot = new GameObject(ArtRootName);
        artRoot.transform.SetParent(transform);

        BuildGrassBase(artRoot.transform);
        BuildLane("Mid", MidLane, artRoot.transform);
        BuildLane("Top", TopLane, artRoot.transform);
        BuildLane("Bot", BotLane, artRoot.transform);
        BuildObjectivePits(artRoot.transform);
        BuildBaseColorLanguage(artRoot.transform);
        BuildMapEdges(artRoot.transform);
    }

    private void BuildGrassBase(Transform parent)
    {
        GameObject baseGrass = CreateCube("Painted_Grass_Playfield", Vector3.zero, new Vector3(252f, 0.06f, 206f), Quaternion.identity, grassMat, parent);
        baseGrass.transform.position = new Vector3(0f, -0.04f, 0f);

        for (int i = 0; i < 70; i++)
        {
            Vector3 p = new Vector3(Range(-118f, 118f), 0.02f, Range(-94f, 94f));
            if (IsNearLane(p, 13f))
                continue;

            CreateBrushCluster("Ambient_Brush_" + i, p, darkGrassMat, parent, Range(0.75f, 1.25f));
        }
    }

    private void BuildLane(string laneName, Vector3[] points, Transform parent)
    {
        GameObject laneRoot = new GameObject(laneName + "_Premium_Lane");
        laneRoot.transform.SetParent(parent);

        for (int i = 0; i < points.Length - 1; i++)
        {
            Vector3 a = points[i];
            Vector3 b = points[i + 1];
            Vector3 dir = (b - a).normalized;
            Vector3 side = new Vector3(-dir.z, 0f, dir.x);
            float length = Vector3.Distance(a, b);
            Vector3 center = Vector3.Lerp(a, b, 0.5f) + Vector3.up * 0.02f;
            Quaternion rot = Quaternion.LookRotation(dir, Vector3.up);

            CreateCube(laneName + "_Lane_Dirt_" + i, center, new Vector3(12.5f, 0.08f, length + 2f), rot, laneDirtMat, laneRoot.transform);

            int slabCount = Mathf.Max(5, Mathf.RoundToInt(length / 8f));
            for (int s = 0; s < slabCount; s++)
            {
                float t = (s + 0.5f) / slabCount;
                Vector3 p = Vector3.Lerp(a, b, t);
                p += side * Range(-3.4f, 3.4f);
                p.y = 0.11f;
                Quaternion slabRot = rot * Quaternion.Euler(0f, Range(-18f, 18f), 0f);
                Vector3 slabScale = new Vector3(Range(1.6f, 3.7f), 0.12f, Range(1.0f, 3.2f));
                CreateCube(laneName + "_Painted_Slab_" + i + "_" + s, p, slabScale, slabRot, stoneMat, laneRoot.transform);
            }

            int edgeCount = Mathf.Max(4, Mathf.RoundToInt(length / 7f));
            for (int e = 0; e <= edgeCount; e++)
            {
                float t = (float)e / edgeCount;
                Vector3 p = Vector3.Lerp(a, b, t);
                CreateLaneEdgeStone(laneName, i, e, p + side * 7.2f, rot, laneRoot.transform);
                CreateLaneEdgeStone(laneName, i, e, p - side * 7.2f, rot, laneRoot.transform);
            }

            if (i % 2 == 0)
            {
                CreateRuneDisk(laneName + "_Gold_Rune_" + i, center + Vector3.up * 0.11f, laneRoot.transform);
                CreateBrushCluster(laneName + "_Brush_Left_" + i, center + side * 12.5f, brushMat, laneRoot.transform, 1.15f);
                CreateBrushCluster(laneName + "_Brush_Right_" + i, center - side * 12.5f, brushMat, laneRoot.transform, 1.15f);
            }

            if (laneName != "Mid" && i % 2 == 1)
            {
                CreateRockCluster(laneName + "_Cliff_Rock_" + i, center + side * 16f, laneRoot.transform, 1.2f);
            }
        }
    }

    private void CreateLaneEdgeStone(string laneName, int segment, int index, Vector3 pos, Quaternion rot, Transform parent)
    {
        pos.y = 0.16f;
        Vector3 scale = new Vector3(Range(0.8f, 2.0f), 0.22f, Range(0.8f, 2.5f));
        CreateCube(laneName + "_Edge_Stone_" + segment + "_" + index, pos, scale, rot * Quaternion.Euler(0f, Range(-25f, 25f), 0f), stoneEdgeMat, parent);
    }

    private void CreateRuneDisk(string name, Vector3 pos, Transform parent)
    {
        GameObject rune = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        rune.name = name;
        rune.transform.SetParent(parent);
        rune.transform.position = pos;
        rune.transform.localScale = new Vector3(1.7f, 0.025f, 1.7f);
        rune.GetComponent<Renderer>().sharedMaterial = goldRuneMat;
    }

    private void CreateBrushCluster(string name, Vector3 center, Material mat, Transform parent, float scale)
    {
        GameObject group = new GameObject(name);
        group.transform.SetParent(parent);
        group.transform.position = center;

        int blades = Mathf.RoundToInt(8 * scale);
        for (int i = 0; i < blades; i++)
        {
            Vector3 offset = RandomCircle(Range(0.6f, 2.1f) * scale);
            GameObject blade = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            blade.name = name + "_Blade_" + i;
            blade.transform.SetParent(group.transform);
            blade.transform.position = center + offset + Vector3.up * Range(0.35f, 0.7f);
            blade.transform.rotation = Quaternion.Euler(Range(-8f, 8f), Range(0f, 360f), Range(-10f, 10f));
            blade.transform.localScale = new Vector3(Range(0.22f, 0.42f), Range(0.7f, 1.35f), Range(0.22f, 0.42f)) * scale;
            blade.GetComponent<Renderer>().sharedMaterial = mat;
        }

        if (Range(0f, 1f) > 0.45f)
            CreateFlower(name + "_Flower", center + RandomCircle(1.6f * scale) + Vector3.up * 0.18f, group.transform);
    }

    private void CreateFlower(string name, Vector3 pos, Transform parent)
    {
        GameObject flower = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        flower.name = name;
        flower.transform.SetParent(parent);
        flower.transform.position = pos;
        flower.transform.localScale = Vector3.one * Range(0.18f, 0.32f);
        flower.GetComponent<Renderer>().sharedMaterial = flowerMat;
    }

    private void CreateRockCluster(string name, Vector3 center, Transform parent, float scale)
    {
        GameObject group = new GameObject(name);
        group.transform.SetParent(parent);
        group.transform.position = center;

        int rocks = Mathf.RoundToInt(5 * scale);
        for (int i = 0; i < rocks; i++)
        {
            Vector3 p = center + RandomCircle(Range(0.5f, 2.8f) * scale);
            p.y = Range(0.35f, 1.4f) * scale;
            Quaternion rot = Quaternion.Euler(Range(-8f, 8f), Range(0f, 360f), Range(-8f, 8f));
            Vector3 s = new Vector3(Range(0.8f, 2.1f), Range(0.7f, 2.6f), Range(0.8f, 2.2f)) * scale;
            CreateCube(name + "_Piece_" + i, p, s, rot, cliffMat, group.transform);
        }
    }

    private void BuildObjectivePits(Transform parent)
    {
        CreateObjectivePit("Dragon_Pit_Premium", new Vector3(-34f, 0f, 28f), redGlowMat, parent);
        CreateObjectivePit("Elder_Pit_Premium", new Vector3(34f, 0f, -28f), blueGlowMat, parent);
    }

    private void CreateObjectivePit(string name, Vector3 center, Material glowMat, Transform parent)
    {
        GameObject group = new GameObject(name);
        group.transform.SetParent(parent);

        GameObject pit = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pit.name = name + "_Rune_Floor";
        pit.transform.SetParent(group.transform);
        pit.transform.position = center + Vector3.up * 0.08f;
        pit.transform.localScale = new Vector3(7f, 0.05f, 7f);
        pit.GetComponent<Renderer>().sharedMaterial = glowMat;

        for (int i = 0; i < 12; i++)
        {
            float a = Mathf.PI * 2f * i / 12f;
            Vector3 p = center + new Vector3(Mathf.Cos(a) * 9f, 0f, Mathf.Sin(a) * 9f);
            CreateRockCluster(name + "_Ring_Rock_" + i, p, group.transform, 0.75f);
        }
    }

    private void BuildBaseColorLanguage(Transform parent)
    {
        CreateBaseGlow("Blue_Base_Premium_Read", new Vector3(-105f, 0f, -78f), blueGlowMat, parent);
        CreateBaseGlow("Red_Base_Premium_Read", new Vector3(105f, 0f, 78f), redGlowMat, parent);
    }

    private void CreateBaseGlow(string name, Vector3 center, Material glowMat, Transform parent)
    {
        GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.name = name;
        ring.transform.SetParent(parent);
        ring.transform.position = center + Vector3.up * 0.12f;
        ring.transform.localScale = new Vector3(12f, 0.035f, 12f);
        ring.GetComponent<Renderer>().sharedMaterial = glowMat;

        for (int i = 0; i < 8; i++)
        {
            float a = Mathf.PI * 2f * i / 8f;
            Vector3 p = center + new Vector3(Mathf.Cos(a) * 12f, 0.5f, Mathf.Sin(a) * 12f);
            CreateTorch(name + "_Torch_" + i, p, glowMat, parent);
        }
    }

    private void BuildMapEdges(Transform parent)
    {
        for (int i = 0; i < 34; i++)
        {
            float x = Range(-126f, 126f);
            float z = i % 2 == 0 ? Range(101f, 112f) : Range(-112f, -101f);
            CreateRockCluster("Outer_Cliff_Read_" + i, new Vector3(x, 0f, z), parent, Range(0.8f, 1.45f));
        }
    }

    private void AddTowerLights()
    {
        TowerHealth[] towers = Object.FindObjectsByType<TowerHealth>(FindObjectsInactive.Exclude);
        foreach (TowerHealth tower in towers)
        {
            if (tower.transform.Find("AOG_Premium_Tower_Light") != null)
                continue;

            bool blue = tower.towerTeam == MinionTeam.Blue || tower.transform.position.x < 0f;
            Material mat = blue ? blueGlowMat : redGlowMat;
            CreateTorch("AOG_Premium_Tower_Light", tower.transform.position + Vector3.up * 4.8f, mat, tower.transform);
        }
    }

    private void CreateTorch(string name, Vector3 pos, Material mat, Transform parent)
    {
        GameObject flame = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        flame.name = name;
        flame.transform.SetParent(parent);
        flame.transform.position = pos;
        flame.transform.localScale = Vector3.one * 0.75f;
        flame.GetComponent<Renderer>().sharedMaterial = mat;

        Light light = flame.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = mat.color;
        light.range = 10f;
        light.intensity = 1.6f;
        light.shadows = LightShadows.None;
    }

    private void InstallUnitAnimators()
    {
        HashSet<GameObject> units = new HashSet<GameObject>();
        foreach (Champion c in Object.FindObjectsByType<Champion>(FindObjectsInactive.Exclude))
            units.Add(c.gameObject);
        foreach (AOGCharacterStats c in Object.FindObjectsByType<AOGCharacterStats>(FindObjectsInactive.Exclude))
            units.Add(c.gameObject);
        foreach (Minion m in Object.FindObjectsByType<Minion>(FindObjectsInactive.Exclude))
            units.Add(m.gameObject);
        foreach (CombatUnit unit in Object.FindObjectsByType<CombatUnit>(FindObjectsInactive.Exclude))
            units.Add(unit.gameObject);

        foreach (GameObject unit in units)
        {
            if (unit.GetComponentInChildren<Renderer>() == null)
                continue;

            AOGPremiumUnitAnimator animator = unit.GetComponent<AOGPremiumUnitAnimator>();
            if (animator == null)
                animator = unit.AddComponent<AOGPremiumUnitAnimator>();

            animator.SetMaterials(blueGlowMat, redGlowMat, shadowMat);
        }
    }

    private GameObject CreateCube(string name, Vector3 pos, Vector3 scale, Quaternion rot, Material mat, Transform parent)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = name;
        obj.transform.SetParent(parent);
        obj.transform.position = pos;
        obj.transform.rotation = rot;
        obj.transform.localScale = scale;
        obj.GetComponent<Renderer>().sharedMaterial = mat;
        return obj;
    }

    private bool IsNearLane(Vector3 pos, float distance)
    {
        return DistanceToPolyline(pos, MidLane) < distance ||
               DistanceToPolyline(pos, TopLane) < distance ||
               DistanceToPolyline(pos, BotLane) < distance;
    }

    private float DistanceToPolyline(Vector3 pos, Vector3[] points)
    {
        float best = float.MaxValue;
        for (int i = 0; i < points.Length - 1; i++)
            best = Mathf.Min(best, DistancePointToSegment(pos, points[i], points[i + 1]));
        return best;
    }

    private float DistancePointToSegment(Vector3 p, Vector3 a, Vector3 b)
    {
        Vector3 ap = p - a;
        Vector3 ab = b - a;
        float t = Mathf.Clamp01(Vector3.Dot(ap, ab) / Mathf.Max(0.001f, ab.sqrMagnitude));
        return Vector3.Distance(p, a + ab * t);
    }

    private Vector3 RandomCircle(float radius)
    {
        float a = Range(0f, Mathf.PI * 2f);
        float r = Range(0f, radius);
        return new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
    }

    private float Range(float min, float max)
    {
        return min + (float)rng.NextDouble() * (max - min);
    }
}

public class AOGPremiumUnitAnimator : MonoBehaviour
{
    private Material blueMat;
    private Material redMat;
    private Material shadowMat;
    private Transform ring;
    private Transform shadow;
    private Vector3 lastPosition;
    private float pulse;
    private float baseRingScale = 1f;

    public void SetMaterials(Material blue, Material red, Material shadowMaterial)
    {
        blueMat = blue;
        redMat = red;
        shadowMat = shadowMaterial;
        EnsureReadabilityObjects();
    }

    private void Start()
    {
        lastPosition = transform.position;
        EnsureReadabilityObjects();
    }

    private void Update()
    {
        EnsureReadabilityObjects();

        float speed = (transform.position - lastPosition).magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
        lastPosition = transform.position;

        pulse += Time.deltaTime * (speed > 0.08f ? 7.5f : 2.2f);
        float ringPulse = 1f + Mathf.Sin(pulse) * (speed > 0.08f ? 0.065f : 0.025f);

        if (ring != null)
        {
            ring.localScale = new Vector3(baseRingScale * ringPulse, 0.025f, baseRingScale * ringPulse);
            ring.Rotate(Vector3.up, Time.deltaTime * 34f, Space.World);
        }

        if (shadow != null)
        {
            float shadowPulse = 1f + Mathf.Sin(pulse * 0.55f) * 0.035f;
            shadow.localScale = new Vector3(baseRingScale * 1.1f * shadowPulse, 0.018f, baseRingScale * 0.78f * shadowPulse);
        }
    }

    private void EnsureReadabilityObjects()
    {
        if (blueMat == null || redMat == null)
            return;

        if (ring == null)
        {
            Transform oldRing = transform.Find("AOG_Premium_Readability_Ring");
            if (oldRing != null)
                ring = oldRing;
            else
                ring = CreateDisk("AOG_Premium_Readability_Ring", ResolveTeamMaterial(), 0.045f);

            baseRingScale = EstimateRingScale();
        }

        if (shadow == null && shadowMat != null)
        {
            Transform oldShadow = transform.Find("AOG_Premium_Ground_Shadow");
            if (oldShadow != null)
                shadow = oldShadow;
            else
                shadow = CreateDisk("AOG_Premium_Ground_Shadow", shadowMat, 0.02f);
        }
    }

    private Transform CreateDisk(string name, Material mat, float y)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        obj.name = name;
        obj.transform.SetParent(transform);
        obj.transform.localPosition = new Vector3(0f, y, 0f);
        obj.transform.localRotation = Quaternion.identity;
        obj.transform.localScale = new Vector3(baseRingScale, 0.02f, baseRingScale);
        obj.GetComponent<Renderer>().sharedMaterial = mat;

        Collider col = obj.GetComponent<Collider>();
        if (col != null)
            Destroy(col);

        return obj.transform;
    }

    private Material ResolveTeamMaterial()
    {
        Champion champion = GetComponent<Champion>();
        if (champion != null)
            return champion.Team == TeamType.Red ? redMat : blueMat;

        CombatUnit combatUnit = GetComponent<CombatUnit>();
        if (combatUnit != null)
            return combatUnit.UnitTeam == TeamType.Red ? redMat : blueMat;

        AOGCharacterStats stats = GetComponent<AOGCharacterStats>();
        if (stats != null)
            return stats.team == MinionTeam.Red ? redMat : blueMat;

        Minion minion = GetComponent<Minion>();
        if (minion != null)
            return minion.team == MinionTeam.Red ? redMat : blueMat;

        return transform.position.x > 0f ? redMat : blueMat;
    }

    private float EstimateRingScale()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        Bounds bounds = new Bounds(transform.position, Vector3.one);
        bool hasBounds = false;

        foreach (Renderer r in renderers)
        {
            if (r.transform == ring || r.transform == shadow)
                continue;

            if (!hasBounds)
            {
                bounds = r.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(r.bounds);
            }
        }

        float size = hasBounds ? Mathf.Max(bounds.extents.x, bounds.extents.z) : 1f;
        return Mathf.Clamp(size * 1.35f, 0.85f, 3.2f);
    }
}
