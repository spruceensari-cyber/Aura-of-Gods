using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class LeagueOfGodsMapGenerator : MonoBehaviour
{
    [Header("Aura of Gods Visual Map")]
    public bool generateOnStart = false;

    Transform root;

    Material groundMat;
    Material darkGroundMat;
    Material roadMat;
    Material roadEdgeMat;
    Material riverMat;
    Material blueMat;
    Material redMat;
    Material goldMat;
    Material stoneMat;
    Material darkStoneMat;
    Material treeMat;
    Material trunkMat;
    Material voidMat;

    void Start()
    {
        if (generateOnStart)
            GenerateMap();
    }

    [ContextMenu("Generate Dark Mythology MOBA Map")]
    public void GenerateMap()
    {
        ClearMap();

        GameObject rootObj = new GameObject("AOG_VisualMap");
        root = rootObj.transform;
        root.SetParent(transform);

        CreateMaterials();

        CreateGround();
        CreateRoads();
        CreateRiver();
        CreateJungleZones();
        CreateWallsAndCliffs();
        CreateBases();
        CreateObjectives();
        CreateDecorativeTowers();
        CreateOuterBorder();
        CreateAmbientDetails();
    }

    [ContextMenu("Clear Dark Mythology MOBA Map")]
    public void ClearMap()
    {
        Transform old = transform.Find("AOG_VisualMap");
        if (old != null)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(old.gameObject);
            else
                Destroy(old.gameObject);
#else
            Destroy(old.gameObject);
#endif
        }
    }

    void CreateMaterials()
    {
        groundMat = MakeMat("AOG_Dark_Grass", new Color(0.055f, 0.18f, 0.075f));
        darkGroundMat = MakeMat("AOG_Deep_Jungle_Green", new Color(0.025f, 0.09f, 0.04f));
        roadMat = MakeMat("AOG_Ancient_Stone_Road", new Color(0.42f, 0.36f, 0.27f));
        roadEdgeMat = MakeMat("AOG_Road_Dark_Edge", new Color(0.18f, 0.15f, 0.12f));
        riverMat = MakeMat("AOG_Mystic_River", new Color(0.02f, 0.18f, 0.24f));
        blueMat = MakeMat("AOG_Celestial_Blue", new Color(0.05f, 0.35f, 1.0f));
        redMat = MakeMat("AOG_Fallen_Red", new Color(0.85f, 0.05f, 0.04f));
        goldMat = MakeMat("AOG_Old_Gold", new Color(0.95f, 0.63f, 0.16f));
        stoneMat = MakeMat("AOG_Sacred_Stone", new Color(0.32f, 0.30f, 0.28f));
        darkStoneMat = MakeMat("AOG_Dark_Cliff_Stone", new Color(0.08f, 0.075f, 0.08f));
        treeMat = MakeMat("AOG_Dark_Tree_Crown", new Color(0.015f, 0.08f, 0.035f));
        trunkMat = MakeMat("AOG_Old_Trunk", new Color(0.16f, 0.09f, 0.04f));
        voidMat = MakeMat("AOG_Void_Purple", new Color(0.28f, 0.03f, 0.45f));
    }

    Material MakeMat(string name, Color color)
    {
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.name = name;
        mat.color = color;
        return mat;
    }

    void CreateGround()
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Dark_Mythology_Ground";
        ground.transform.SetParent(root);
        ground.transform.position = new Vector3(0, -0.08f, 0);
        ground.transform.localScale = new Vector3(32, 1, 22);
        ground.GetComponent<Renderer>().sharedMaterial = groundMat;

        CreateFlatDisk("Blue_Base_Ground_Aura", new Vector3(-110, 0.01f, -75), new Vector3(30, 0.12f, 30), blueMat);
        CreateFlatDisk("Red_Base_Ground_Aura", new Vector3(110, 0.01f, 75), new Vector3(30, 0.12f, 30), redMat);
    }

    void CreateRoads()
    {
        // Mid lane biraz yukarı kaydırılmış, özgün diagonal.
        CreateRoadPath("Mid_Lane", roadMat, 12,
            new Vector3(-105, 0.04f, -28),
            new Vector3(-55, 0.04f, -10),
            new Vector3(0, 0.04f, 6),
            new Vector3(55, 0.04f, 22),
            new Vector3(105, 0.04f, 42)
        );

        // Top lane dıştan döner.
        CreateRoadPath("Top_Lane", roadMat, 11,
            new Vector3(-110, 0.04f, -55),
            new Vector3(-112, 0.04f, 38),
            new Vector3(-82, 0.04f, 86),
            new Vector3(-20, 0.04f, 96),
            new Vector3(55, 0.04f, 96),
            new Vector3(108, 0.04f, 72)
        );

        // Bot lane alttan döner.
        CreateRoadPath("Bot_Lane", roadMat, 11,
            new Vector3(-105, 0.04f, -72),
            new Vector3(-55, 0.04f, -92),
            new Vector3(20, 0.04f, -92),
            new Vector3(92, 0.04f, -65),
            new Vector3(110, 0.04f, 40)
        );

        // Base çıkışları
        CreateRoadSegment("Blue_Base_Exit_Mid", new Vector3(-110, 0.05f, -75), new Vector3(-105, 0.05f, -28), 14, roadMat);
        CreateRoadSegment("Red_Base_Exit_Mid", new Vector3(110, 0.05f, 75), new Vector3(105, 0.05f, 42), 14, roadMat);
    }

    void CreateRiver()
    {
        // Üst river / void objective tarafı
        CreateRoadPath("Upper_Mystic_River", riverMat, 18,
            new Vector3(-72, 0.025f, 38),
            new Vector3(-48, 0.025f, 26),
            new Vector3(-20, 0.025f, 26),
            new Vector3(10, 0.025f, 36)
        );

        // Alt river / dragon objective tarafı
        CreateRoadPath("Lower_Mystic_River", riverMat, 18,
            new Vector3(5, 0.025f, -48),
            new Vector3(36, 0.025f, -60),
            new Vector3(68, 0.025f, -56),
            new Vector3(92, 0.025f, -38)
        );

        CreateFlatDisk("Upper_River_Pool", new Vector3(-42, 0.03f, 30), new Vector3(17, 0.08f, 12), riverMat);
        CreateFlatDisk("Lower_River_Pool", new Vector3(58, 0.03f, -56), new Vector3(20, 0.08f, 14), riverMat);
    }

    void CreateJungleZones()
    {
        CreateJungleIsland("Blue_Upper_Jungle", new Vector3(-70, 0.02f, 30), new Vector3(18, 0.12f, 14));
        CreateJungleIsland("Blue_Mid_Jungle", new Vector3(-52, 0.02f, -35), new Vector3(18, 0.12f, 15));
        CreateJungleIsland("Blue_Lower_Jungle", new Vector3(-18, 0.02f, -65), new Vector3(20, 0.12f, 16));

        CreateJungleIsland("Center_Upper_Jungle", new Vector3(15, 0.02f, 52), new Vector3(22, 0.12f, 16));
        CreateJungleIsland("Center_Lower_Jungle", new Vector3(18, 0.02f, -25), new Vector3(20, 0.12f, 14));

        CreateJungleIsland("Red_Upper_Jungle", new Vector3(60, 0.02f, 48), new Vector3(18, 0.12f, 15));
        CreateJungleIsland("Red_Mid_Jungle", new Vector3(58, 0.02f, -8), new Vector3(20, 0.12f, 15));
        CreateJungleIsland("Red_Lower_Jungle", new Vector3(86, 0.02f, -36), new Vector3(16, 0.12f, 13));
    }

    void CreateWallsAndCliffs()
    {
        // Jungle çevresinde koyu taş sınırlar.
        CreateWallRing("Blue_Upper_Jungle_Wall", new Vector3(-70, 0.6f, 30), 24, 18);
        CreateWallRing("Blue_Mid_Jungle_Wall", new Vector3(-52, 0.6f, -35), 24, 18);
        CreateWallRing("Blue_Lower_Jungle_Wall", new Vector3(-18, 0.6f, -65), 26, 18);

        CreateWallRing("Center_Upper_Jungle_Wall", new Vector3(15, 0.6f, 52), 28, 18);
        CreateWallRing("Center_Lower_Jungle_Wall", new Vector3(18, 0.6f, -25), 26, 17);

        CreateWallRing("Red_Upper_Jungle_Wall", new Vector3(60, 0.6f, 48), 24, 18);
        CreateWallRing("Red_Mid_Jungle_Wall", new Vector3(58, 0.6f, -8), 26, 18);
        CreateWallRing("Red_Lower_Jungle_Wall", new Vector3(86, 0.6f, -36), 22, 15);
    }

    void CreateBases()
    {
        CreateBase("Blue_Celestial_Base", new Vector3(-110, 0.08f, -75), blueMat, true);
        CreateBase("Red_Fallen_Base", new Vector3(110, 0.08f, 75), redMat, false);
    }

    void CreateBase(string name, Vector3 pos, Material teamMat, bool blue)
    {
        GameObject group = new GameObject(name);
        group.transform.SetParent(root);

        GameObject baseDisk = CreateFlatDisk(name + "_Stone_Platform", pos, new Vector3(32, 0.25f, 32), stoneMat);
        baseDisk.transform.SetParent(group.transform);

        GameObject innerDisk = CreateFlatDisk(name + "_Inner_Rune_Ring", pos + new Vector3(0, 0.08f, 0), new Vector3(21, 0.18f, 21), teamMat);
        innerDisk.transform.SetParent(group.transform);

        GameObject nexusBase = CreateFlatDisk(name + "_Nexus_Altar", pos + new Vector3(0, 0.22f, 0), new Vector3(10, 0.6f, 10), darkStoneMat);
        nexusBase.transform.SetParent(group.transform);

        GameObject crystal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        crystal.name = name + "_Nexus_Crystal";
        crystal.transform.SetParent(group.transform);
        crystal.transform.position = pos + new Vector3(0, 4.3f, 0);
        crystal.transform.localScale = new Vector3(3.2f, 4.0f, 3.2f);
        crystal.GetComponent<Renderer>().sharedMaterial = teamMat;

        // iki guardian
        Vector3 offsetA = blue ? new Vector3(-8, 0, 8) : new Vector3(8, 0, -8);
        Vector3 offsetB = blue ? new Vector3(8, 0, -8) : new Vector3(-8, 0, 8);

        CreateGuardianStatue(name + "_Guardian_A", pos + offsetA + new Vector3(0, 2.3f, 0), teamMat, group.transform);
        CreateGuardianStatue(name + "_Guardian_B", pos + offsetB + new Vector3(0, 2.3f, 0), teamMat, group.transform);
    }

    void CreateObjectives()
    {
        // Üst objective: Nephilim / Void
        GameObject upperPit = CreateFlatDisk("Nephilim_Behemoth_Pit", new Vector3(-38, 0.08f, 33), new Vector3(16, 0.25f, 16), darkStoneMat);
        GameObject voidCore = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        voidCore.name = "Nephilim_Behemoth_Placeholder";
        voidCore.transform.SetParent(root);
        voidCore.transform.position = new Vector3(-38, 2.2f, 33);
        voidCore.transform.localScale = new Vector3(5, 3.2f, 5);
        voidCore.GetComponent<Renderer>().sharedMaterial = voidMat;

        // Alt objective: Celestial Drake / Leviathan
        GameObject lowerPit = CreateFlatDisk("Celestial_Drake_Pit", new Vector3(58, 0.08f, -58), new Vector3(18, 0.25f, 18), darkStoneMat);
        GameObject drake = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        drake.name = "Celestial_Drake_Placeholder";
        drake.transform.SetParent(root);
        drake.transform.position = new Vector3(58, 2.2f, -58);
        drake.transform.localScale = new Vector3(6, 2.8f, 4);
        drake.GetComponent<Renderer>().sharedMaterial = riverMat;
    }

    void CreateDecorativeTowers()
    {
        // Blue side lane guardians
        CreateGuardianTower("Blue_Top_Outer_Visual", new Vector3(-105, 2.5f, 35), blueMat);
        CreateGuardianTower("Blue_Top_Inner_Visual", new Vector3(-108, 2.5f, -10), blueMat);
        CreateGuardianTower("Blue_Mid_Outer_Visual", new Vector3(-52, 2.5f, -12), blueMat);
        CreateGuardianTower("Blue_Mid_Inner_Visual", new Vector3(-82, 2.5f, -48), blueMat);
        CreateGuardianTower("Blue_Bot_Outer_Visual", new Vector3(-25, 2.5f, -88), blueMat);
        CreateGuardianTower("Blue_Bot_Inner_Visual", new Vector3(-78, 2.5f, -78), blueMat);

        // Red side lane guardians
        CreateGuardianTower("Red_Top_Outer_Visual", new Vector3(42, 2.5f, 96), redMat);
        CreateGuardianTower("Red_Top_Inner_Visual", new Vector3(82, 2.5f, 85), redMat);
        CreateGuardianTower("Red_Mid_Outer_Visual", new Vector3(50, 2.5f, 22), redMat);
        CreateGuardianTower("Red_Mid_Inner_Visual", new Vector3(82, 2.5f, 48), redMat);
        CreateGuardianTower("Red_Bot_Outer_Visual", new Vector3(92, 2.5f, -35), redMat);
        CreateGuardianTower("Red_Bot_Inner_Visual", new Vector3(108, 2.5f, 20), redMat);
    }

    void CreateOuterBorder()
    {
        // Dış karanlık sur / cliff hissi
        CreateRoadSegment("North_Dark_Wall", new Vector3(-135, 2f, 112), new Vector3(135, 2f, 112), 8, darkStoneMat);
        CreateRoadSegment("South_Dark_Wall", new Vector3(-135, 2f, -112), new Vector3(135, 2f, -112), 8, darkStoneMat);
        CreateRoadSegment("West_Dark_Wall", new Vector3(-135, 2f, -112), new Vector3(-135, 2f, 112), 8, darkStoneMat);
        CreateRoadSegment("East_Dark_Wall", new Vector3(135, 2f, -112), new Vector3(135, 2f, 112), 8, darkStoneMat);
    }

    void CreateAmbientDetails()
    {
        // Haritanın boş kalmaması için ağaç kümeleri
        CreateTreeCluster(new Vector3(-120, 0, 80), 14);
        CreateTreeCluster(new Vector3(-120, 0, -20), 10);
        CreateTreeCluster(new Vector3(-95, 0, -100), 12);
        CreateTreeCluster(new Vector3(-5, 0, 88), 9);
        CreateTreeCluster(new Vector3(10, 0, -105), 12);
        CreateTreeCluster(new Vector3(104, 0, -75), 9);
        CreateTreeCluster(new Vector3(120, 0, 15), 10);
        CreateTreeCluster(new Vector3(120, 0, 100), 12);

        // Minik altın kutsal işaretler
        CreateRune("Center_Gold_Rune", new Vector3(0, 0.12f, 4), goldMat);
        CreateRune("Blue_Rune", new Vector3(-90, 0.12f, -54), blueMat);
        CreateRune("Red_Rune", new Vector3(90, 0.12f, 58), redMat);
    }

    void CreateRoadPath(string name, Material mat, float width, params Vector3[] points)
    {
        GameObject group = new GameObject(name);
        group.transform.SetParent(root);

        for (int i = 0; i < points.Length - 1; i++)
        {
            GameObject edge = CreateRoadSegment(name + "_Dark_Edge_" + i, points[i], points[i + 1], width + 3f, roadEdgeMat);
            edge.transform.SetParent(group.transform);

            GameObject road = CreateRoadSegment(name + "_Stone_" + i, points[i] + Vector3.up * 0.02f, points[i + 1] + Vector3.up * 0.02f, width, mat);
            road.transform.SetParent(group.transform);
        }
    }

    GameObject CreateRoadSegment(string name, Vector3 start, Vector3 end, float width, Material mat)
    {
        Vector3 mid = (start + end) * 0.5f;
        Vector3 dir = end - start;
        float length = dir.magnitude;

        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = name;
        obj.transform.SetParent(root);
        obj.transform.position = mid;
        obj.transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
        obj.transform.localScale = new Vector3(width, 0.16f, length);
        obj.GetComponent<Renderer>().sharedMaterial = mat;
        return obj;
    }

    GameObject CreateFlatDisk(string name, Vector3 pos, Vector3 scale, Material mat)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        obj.name = name;
        obj.transform.SetParent(root);
        obj.transform.position = pos;
        obj.transform.localScale = scale;
        obj.GetComponent<Renderer>().sharedMaterial = mat;
        return obj;
    }

    void CreateJungleIsland(string name, Vector3 pos, Vector3 scale)
    {
        GameObject island = CreateFlatDisk(name, pos, scale, darkGroundMat);

        for (int i = 0; i < 10; i++)
        {
            float angle = i * Mathf.PI * 2f / 10f;
            float rx = Mathf.Cos(angle) * scale.x * 0.35f;
            float rz = Mathf.Sin(angle) * scale.z * 0.35f;
            CreateSimpleTree(name + "_Tree_" + i, pos + new Vector3(rx, 0, rz));
        }

        CreateRune(name + "_Camp_Rune", pos + new Vector3(0, 0.18f, 0), goldMat);
    }

    void CreateWallRing(string name, Vector3 center, float radiusX, float radiusZ)
    {
        GameObject group = new GameObject(name);
        group.transform.SetParent(root);

        int count = 12;
        Vector3 prev = Vector3.zero;
        Vector3 first = Vector3.zero;

        for (int i = 0; i <= count; i++)
        {
            float t = i / (float)count * Mathf.PI * 2f;
            Vector3 p = center + new Vector3(Mathf.Cos(t) * radiusX * 0.5f, 0, Mathf.Sin(t) * radiusZ * 0.5f);

            if (i == 0)
            {
                first = p;
                prev = p;
                continue;
            }

            GameObject seg = CreateRoadSegment(name + "_Segment_" + i, prev, p, 2.2f, darkStoneMat);
            seg.transform.position += Vector3.up * 0.6f;
            seg.transform.localScale = new Vector3(2.2f, 1.2f, Vector3.Distance(prev, p));
            seg.transform.SetParent(group.transform);
            prev = p;
        }
    }

    void CreateTreeCluster(Vector3 center, int count)
    {
        for (int i = 0; i < count; i++)
        {
            Vector3 random = new Vector3(Random.Range(-12f, 12f), 0, Random.Range(-10f, 10f));
            CreateSimpleTree("Dark_Forest_Tree", center + random);
        }
    }

    void CreateSimpleTree(string name, Vector3 pos)
    {
        GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.name = name + "_Trunk";
        trunk.transform.SetParent(root);
        trunk.transform.position = pos + new Vector3(0, 1.1f, 0);
        trunk.transform.localScale = new Vector3(0.5f, 1.2f, 0.5f);
        trunk.GetComponent<Renderer>().sharedMaterial = trunkMat;

        GameObject crown = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        crown.name = name + "_Crown";
        crown.transform.SetParent(root);
        crown.transform.position = pos + new Vector3(0, 2.8f, 0);
        crown.transform.localScale = new Vector3(2.3f, 2.5f, 2.3f);
        crown.GetComponent<Renderer>().sharedMaterial = treeMat;
    }

    void CreateGuardianTower(string name, Vector3 pos, Material teamMat)
    {
        GameObject group = new GameObject(name);
        group.transform.SetParent(root);

        GameObject baseDisk = CreateFlatDisk(name + "_Base", pos + new Vector3(0, -1.9f, 0), new Vector3(3.8f, 0.35f, 3.8f), darkStoneMat);
        baseDisk.transform.SetParent(group.transform);

        GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pillar.name = name + "_Pillar";
        pillar.transform.SetParent(group.transform);
        pillar.transform.position = pos;
        pillar.transform.localScale = new Vector3(1.5f, 2.8f, 1.5f);
        pillar.GetComponent<Renderer>().sharedMaterial = stoneMat;

        GameObject core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        core.name = name + "_Energy_Core";
        core.transform.SetParent(group.transform);
        core.transform.position = pos + new Vector3(0, 3.2f, 0);
        core.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
        core.GetComponent<Renderer>().sharedMaterial = teamMat;

        GameObject wingA = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wingA.name = name + "_Wing_A";
        wingA.transform.SetParent(group.transform);
        wingA.transform.position = pos + new Vector3(-1.8f, 2.5f, 0);
        wingA.transform.localScale = new Vector3(0.25f, 1.8f, 2.5f);
        wingA.transform.rotation = Quaternion.Euler(0, 0, 25);
        wingA.GetComponent<Renderer>().sharedMaterial = goldMat;

        GameObject wingB = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wingB.name = name + "_Wing_B";
        wingB.transform.SetParent(group.transform);
        wingB.transform.position = pos + new Vector3(1.8f, 2.5f, 0);
        wingB.transform.localScale = new Vector3(0.25f, 1.8f, 2.5f);
        wingB.transform.rotation = Quaternion.Euler(0, 0, -25);
        wingB.GetComponent<Renderer>().sharedMaterial = goldMat;
    }

    void CreateGuardianStatue(string name, Vector3 pos, Material teamMat, Transform parent)
    {
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = name;
        body.transform.SetParent(parent);
        body.transform.position = pos;
        body.transform.localScale = new Vector3(1.4f, 2.2f, 1.4f);
        body.GetComponent<Renderer>().sharedMaterial = teamMat;
    }

    void CreateRune(string name, Vector3 pos, Material mat)
    {
        GameObject rune = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        rune.name = name;
        rune.transform.SetParent(root);
        rune.transform.position = pos;
        rune.transform.localScale = new Vector3(2.3f, 0.05f, 2.3f);
        rune.GetComponent<Renderer>().sharedMaterial = mat;
    }
}