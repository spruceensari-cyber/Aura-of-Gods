using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class AOGPrefabVisualSystem : MonoBehaviour
{
    [Header("Optional Real Prefabs")]
    public GameObject blueTowerPrefab;
    public GameObject redTowerPrefab;
    public GameObject blueNexusPrefab;
    public GameObject redNexusPrefab;
    public GameObject treePrefab;
    public GameObject rockPrefab;
    public GameObject jungleCampPrefab;
    public GameObject blueMinionStatuePrefab;
    public GameObject redMinionStatuePrefab;

    [Header("Scale Settings")]
    public float towerScale = 1.0f;
    public float nexusScale = 1.0f;
    public float treeScale = 1.0f;
    public float rockScale = 1.0f;
    public float campScale = 1.0f;

    private Transform root;

    private Material blueMat;
    private Material redMat;
    private Material darkMat;
    private Material stoneMat;
    private Material goldMat;
    private Material treeMat;
    private Material rockMat;
    private Material purpleMat;

    [ContextMenu("Build Prefab Visual System")]
    public void Build()
    {
        Clear();

        GameObject rootObj = new GameObject("AOG_Prefab_Visual_System");
        root = rootObj.transform;
        root.SetParent(transform);

        CreateMaterials();

        BuildNexus();
        BuildTowers();
        BuildJungleCamps();
        BuildForestAndRocks();
        BuildLaneStatues();
        BuildAtmosphereProps();
    }

    [ContextMenu("Clear Prefab Visual System")]
    public void Clear()
    {
        Transform old = transform.Find("AOG_Prefab_Visual_System");
        if (old != null)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) DestroyImmediate(old.gameObject);
            else Destroy(old.gameObject);
#else
            Destroy(old.gameObject);
#endif
        }
    }

    void CreateMaterials()
    {
        blueMat = MakeMat("AOG_PVS_Blue_Energy", new Color32(40, 130, 255, 255));
        redMat = MakeMat("AOG_PVS_Red_Energy", new Color32(220, 35, 25, 255));
        darkMat = MakeMat("AOG_PVS_Dark_Stone", new Color32(22, 22, 26, 255));
        stoneMat = MakeMat("AOG_PVS_Ancient_Stone", new Color32(90, 82, 70, 255));
        goldMat = MakeMat("AOG_PVS_Old_Gold", new Color32(220, 160, 50, 255));
        treeMat = MakeMat("AOG_PVS_Dark_Tree", new Color32(8, 45, 20, 255));
        rockMat = MakeMat("AOG_PVS_Rock", new Color32(55, 55, 58, 255));
        purpleMat = MakeMat("AOG_PVS_Void_Purple", new Color32(105, 25, 160, 255));
    }

    Material MakeMat(string name, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        Material mat = new Material(shader);
        mat.name = name;
        mat.color = color;
        return mat;
    }

    void BuildNexus()
    {
        GameObject group = NewGroup("01_Nexus");

        SpawnOrFallbackNexus(
            "Blue_Nexus_Final",
            blueNexusPrefab,
            new Vector3(-105, 0, -78),
            blueMat,
            group.transform,
            true
        );

        SpawnOrFallbackNexus(
            "Red_Nexus_Final",
            redNexusPrefab,
            new Vector3(105, 0, 78),
            redMat,
            group.transform,
            false
        );
    }

    void BuildTowers()
    {
        GameObject group = NewGroup("02_Towers_Guardians");

        Vector3[] blueTowerPositions =
        {
            new Vector3(-52, 0, -38),
            new Vector3(-82, 0, -60),
            new Vector3(-118, 0, 18),
            new Vector3(-108, 0, -34),
            new Vector3(-18, 0, -118),
            new Vector3(-68, 0, -92)
        };

        string[] names =
        {
            "Mid_Outer",
            "Mid_Inner",
            "Top_Outer",
            "Top_Inner",
            "Bot_Outer",
            "Bot_Inner"
        };

        for (int i = 0; i < blueTowerPositions.Length; i++)
        {
            Vector3 bluePos = blueTowerPositions[i];
            Vector3 redPos = Mirror(bluePos);

            SpawnOrFallbackTower(
                "Blue_" + names[i],
                blueTowerPrefab,
                bluePos,
                blueMat,
                true,
                group.transform
            );

            SpawnOrFallbackTower(
                "Red_" + names[i],
                redTowerPrefab,
                redPos,
                redMat,
                false,
                group.transform
            );
        }
    }

    void BuildJungleCamps()
    {
        GameObject group = NewGroup("03_Jungle_Camps");

        Vector3[] blueCamps =
        {
            new Vector3(-78, 0, 34),
            new Vector3(-70, 0, -14),
            new Vector3(-42, 0, -66),
            new Vector3(-12, 0, 58),
            new Vector3(-20, 0, -22)
        };

        for (int i = 0; i < blueCamps.Length; i++)
        {
            SpawnOrFallbackCamp("Blue_Camp_" + i, blueCamps[i], blueMat, group.transform);
            SpawnOrFallbackCamp("Red_Camp_" + i, Mirror(blueCamps[i]), redMat, group.transform);
        }

        SpawnOrFallbackObjective("Upper_Void_Boss_Pit", new Vector3(-38, 0, 36), purpleMat, group.transform);
        SpawnOrFallbackObjective("Lower_Leviathan_Pit", new Vector3(38, 0, -36), blueMat, group.transform);
    }

    void BuildForestAndRocks()
    {
        GameObject group = NewGroup("04_Forest_Rocks_Border");

        Random.InitState(999);

        for (int i = 0; i < 230; i++)
        {
            Vector3 pos = RandomBorderPosition();
            SpawnTree(pos, group.transform);
        }

        for (int i = 0; i < 90; i++)
        {
            Vector3 pos = RandomBorderPosition();
            SpawnRock(pos, group.transform);
        }

        Vector3[] innerForestClusters =
        {
            new Vector3(-90,0,70),
            new Vector3(-88,0,-5),
            new Vector3(-58,0,-80),
            new Vector3(-8,0,78),
            new Vector3(12,0,-70),
            new Vector3(70,0,50),
            new Vector3(88,0,-15),
            new Vector3(80,0,-78)
        };

        foreach (Vector3 cluster in innerForestClusters)
        {
            for (int i = 0; i < 14; i++)
            {
                Vector3 p = cluster + new Vector3(Random.Range(-10f, 10f), 0, Random.Range(-8f, 8f));
                SpawnTree(p, group.transform);
            }

            for (int i = 0; i < 5; i++)
            {
                Vector3 p = cluster + new Vector3(Random.Range(-8f, 8f), 0, Random.Range(-6f, 6f));
                SpawnRock(p, group.transform);
            }
        }
    }

    void BuildLaneStatues()
    {
        GameObject group = NewGroup("05_Lane_Small_Statues");

        Vector3[] blueStatues =
        {
            new Vector3(-74,0,-54),
            new Vector3(-36,0,-28),
            new Vector3(-112,0,46),
            new Vector3(-85,0,82),
            new Vector3(-46,0,-103),
            new Vector3(-12,0,-112)
        };

        for (int i = 0; i < blueStatues.Length; i++)
        {
            SpawnSmallStatue("Blue_Lane_Statue_" + i, blueMinionStatuePrefab, blueStatues[i], blueMat, group.transform);
            SpawnSmallStatue("Red_Lane_Statue_" + i, redMinionStatuePrefab, Mirror(blueStatues[i]), redMat, group.transform);
        }
    }

    void BuildAtmosphereProps()
    {
        GameObject group = NewGroup("06_Atmosphere_Runes_Torches");

        Vector3[] torchPoints =
        {
            new Vector3(-85,0,-55),
            new Vector3(-45,0,-25),
            new Vector3(0,0,0),
            new Vector3(45,0,25),
            new Vector3(85,0,55),

            new Vector3(-112,0,25),
            new Vector3(-70,0,92),
            new Vector3(0,0,102),
            new Vector3(70,0,92),
            new Vector3(112,0,25),

            new Vector3(-28,0,-112),
            new Vector3(28,0,-104),
            new Vector3(80,0,-70),
            new Vector3(112,0,-25)
        };

        for (int i = 0; i < torchPoints.Length; i++)
        {
            Material m = i % 2 == 0 ? goldMat : purpleMat;
            CreateTorch("Torch_" + i, torchPoints[i], m, group.transform);
        }
    }

    void SpawnOrFallbackNexus(string name, GameObject prefab, Vector3 pos, Material mat, Transform parent, bool blueSide)
    {
        if (prefab != null)
        {
            GameObject obj = Instantiate(prefab, pos, Quaternion.identity, parent);
            obj.name = name;
            obj.transform.localScale *= nexusScale;
            return;
        }

        GameObject group = NewChildGroup(name, parent);

        CreateCylinder(name + "_Base", pos + new Vector3(0, 0.3f, 0), new Vector3(13, 0.6f, 13), darkMat, group.transform);
        CreateCylinder(name + "_Ring", pos + new Vector3(0, 0.9f, 0), new Vector3(9, 0.35f, 9), stoneMat, group.transform);
        CreateCylinder(name + "_Energy_Ring", pos + new Vector3(0, 1.2f, 0), new Vector3(6, 0.12f, 6), mat, group.transform);

        GameObject crystal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        crystal.name = name + "_Crystal_Core";
        crystal.transform.SetParent(group.transform);
        crystal.transform.position = pos + new Vector3(0, 6.2f, 0);
        crystal.transform.localScale = new Vector3(3, 6, 3);
        crystal.GetComponent<Renderer>().sharedMaterial = mat;

        GameObject orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        orb.name = name + "_Orb";
        orb.transform.SetParent(group.transform);
        orb.transform.position = pos + new Vector3(0, 13f, 0);
        orb.transform.localScale = new Vector3(3.3f, 3.3f, 3.3f);
        orb.GetComponent<Renderer>().sharedMaterial = mat;

        for (int i = 0; i < 8; i++)
        {
            float a = i * Mathf.PI * 2f / 8f;
            Vector3 p = pos + new Vector3(Mathf.Cos(a) * 10, 2.1f, Mathf.Sin(a) * 10);
            CreateObelisk(name + "_Obelisk_" + i, p, mat, group.transform);
        }
    }

    void SpawnOrFallbackTower(string name, GameObject prefab, Vector3 pos, Material mat, bool blueSide, Transform parent)
    {
        if (prefab != null)
        {
            GameObject obj = Instantiate(prefab, pos, Quaternion.identity, parent);
            obj.name = name;
            obj.transform.localScale *= towerScale;
            return;
        }

        GameObject group = NewChildGroup(name, parent);

        CreateCylinder(name + "_Base", pos + new Vector3(0, 0.25f, 0), new Vector3(4.8f, 0.5f, 4.8f), darkMat, group.transform);
        CreateCylinder(name + "_Inner", pos + new Vector3(0, 0.75f, 0), new Vector3(3.2f, 0.3f, 3.2f), stoneMat, group.transform);

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = name + "_Guardian_Body";
        body.transform.SetParent(group.transform);
        body.transform.position = pos + new Vector3(0, 3.0f, 0);
        body.transform.localScale = new Vector3(1.2f, 2.2f, 1.2f);
        body.GetComponent<Renderer>().sharedMaterial = blueSide ? stoneMat : darkMat;

        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = name + "_Energy_Head";
        head.transform.SetParent(group.transform);
        head.transform.position = pos + new Vector3(0, 5.5f, 0);
        head.transform.localScale = new Vector3(1.1f, 1.1f, 1.1f);
        head.GetComponent<Renderer>().sharedMaterial = mat;

        CreateWing(name + "_Wing_L", pos + new Vector3(-1.35f, 4.1f, 0), true, blueSide ? goldMat : darkMat, group.transform);
        CreateWing(name + "_Wing_R", pos + new Vector3(1.35f, 4.1f, 0), false, blueSide ? goldMat : darkMat, group.transform);

        CreateObelisk(name + "_Attack_Core", pos + new Vector3(0, 6.8f, 0), mat, group.transform);
    }

    void SpawnOrFallbackCamp(string name, Vector3 pos, Material mat, Transform parent)
    {
        if (jungleCampPrefab != null)
        {
            GameObject obj = Instantiate(jungleCampPrefab, pos, Quaternion.identity, parent);
            obj.name = name;
            obj.transform.localScale *= campScale;
            return;
        }

        GameObject group = NewChildGroup(name, parent);

        CreateCylinder(name + "_Pit", pos + new Vector3(0, 0.2f, 0), new Vector3(7.5f, 0.2f, 7.5f), darkMat, group.transform);
        CreateCylinder(name + "_Rune", pos + new Vector3(0, 0.45f, 0), new Vector3(5, 0.08f, 5), mat, group.transform);

        GameObject creature = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        creature.name = name + "_Creature";
        creature.transform.SetParent(group.transform);
        creature.transform.position = pos + new Vector3(0, 2.2f, 0);
        creature.transform.localScale = new Vector3(1.5f, 2.0f, 1.5f);
        creature.GetComponent<Renderer>().sharedMaterial = mat;

        for (int i = 0; i < 6; i++)
        {
            float a = i * Mathf.PI * 2f / 6f;
            Vector3 p = pos + new Vector3(Mathf.Cos(a) * 5.2f, 1.0f, Mathf.Sin(a) * 5.2f);
            CreateObelisk(name + "_Bone_Obelisk_" + i, p, stoneMat, group.transform);
        }
    }

    void SpawnOrFallbackObjective(string name, Vector3 pos, Material mat, Transform parent)
    {
        GameObject group = NewChildGroup(name, parent);

        CreateCylinder(name + "_Outer_Pit", pos + new Vector3(0, 0.35f, 0), new Vector3(14, 0.35f, 14), darkMat, group.transform);
        CreateCylinder(name + "_Energy_Circle", pos + new Vector3(0, 0.72f, 0), new Vector3(9, 0.12f, 9), mat, group.transform);

        GameObject boss = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        boss.name = name + "_Boss";
        boss.transform.SetParent(group.transform);
        boss.transform.position = pos + new Vector3(0, 3.8f, 0);
        boss.transform.localScale = new Vector3(5.2f, 3.0f, 5.2f);
        boss.GetComponent<Renderer>().sharedMaterial = mat;

        for (int i = 0; i < 10; i++)
        {
            float a = i * Mathf.PI * 2f / 10f;
            Vector3 p = pos + new Vector3(Mathf.Cos(a) * 10, 1.4f, Mathf.Sin(a) * 10);
            CreateTorch(name + "_Ritual_Torch_" + i, p, mat, group.transform);
        }
    }

    void SpawnTree(Vector3 pos, Transform parent)
    {
        if (treePrefab != null)
        {
            GameObject obj = Instantiate(treePrefab, pos, Quaternion.Euler(0, Random.Range(0, 360), 0), parent);
            obj.name = "Tree_Prefab";
            obj.transform.localScale *= treeScale * Random.Range(0.8f, 1.3f);
            return;
        }

        GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.name = "Tree_Trunk";
        trunk.transform.SetParent(parent);
        trunk.transform.position = pos + new Vector3(0, 1.0f, 0);
        trunk.transform.localScale = new Vector3(0.4f, 1.1f, 0.4f);
        trunk.GetComponent<Renderer>().sharedMaterial = stoneMat;

        GameObject crown = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        crown.name = "Tree_Crown";
        crown.transform.SetParent(parent);
        crown.transform.position = pos + new Vector3(0, 2.7f, 0);
        crown.transform.localScale = new Vector3(Random.Range(1.6f, 2.4f), Random.Range(2.0f, 3.0f), Random.Range(1.6f, 2.4f));
        crown.GetComponent<Renderer>().sharedMaterial = treeMat;
    }

    void SpawnRock(Vector3 pos, Transform parent)
    {
        if (rockPrefab != null)
        {
            GameObject obj = Instantiate(rockPrefab, pos, Quaternion.Euler(0, Random.Range(0, 360), 0), parent);
            obj.name = "Rock_Prefab";
            obj.transform.localScale *= rockScale * Random.Range(0.7f, 1.4f);
            return;
        }

        GameObject rock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        rock.name = "Rock_Fallback";
        rock.transform.SetParent(parent);
        rock.transform.position = pos + new Vector3(0, 0.35f, 0);
        rock.transform.localScale = new Vector3(Random.Range(0.8f, 2.2f), Random.Range(0.5f, 1.2f), Random.Range(0.8f, 2.2f));
        rock.GetComponent<Renderer>().sharedMaterial = rockMat;
    }

    void SpawnSmallStatue(string name, GameObject prefab, Vector3 pos, Material mat, Transform parent)
    {
        if (prefab != null)
        {
            GameObject obj = Instantiate(prefab, pos, Quaternion.identity, parent);
            obj.name = name;
            return;
        }

        GameObject group = NewChildGroup(name, parent);

        CreateCylinder(name + "_Base", pos + new Vector3(0, 0.2f, 0), new Vector3(1.6f, 0.25f, 1.6f), darkMat, group.transform);

        GameObject statue = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        statue.name = name + "_Small_Guardian";
        statue.transform.SetParent(group.transform);
        statue.transform.position = pos + new Vector3(0, 1.4f, 0);
        statue.transform.localScale = new Vector3(0.5f, 1.1f, 0.5f);
        statue.GetComponent<Renderer>().sharedMaterial = mat;
    }

    Vector3 RandomBorderPosition()
    {
        bool horizontal = Random.value > 0.5f;

        if (horizontal)
        {
            float x = Random.Range(-135f, 135f);
            float z = Random.value > 0.5f ? Random.Range(92f, 110f) : Random.Range(-110f, -92f);
            return new Vector3(x, 0, z);
        }
        else
        {
            float x = Random.value > 0.5f ? Random.Range(128f, 142f) : Random.Range(-142f, -128f);
            float z = Random.Range(-100f, 100f);
            return new Vector3(x, 0, z);
        }
    }

    Vector3 Mirror(Vector3 p)
    {
        return new Vector3(-p.x, p.y, -p.z);
    }

    void CreateCylinder(string name, Vector3 pos, Vector3 scale, Material mat, Transform parent)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        obj.name = name;
        obj.transform.SetParent(parent);
        obj.transform.position = pos;
        obj.transform.localScale = scale;
        obj.GetComponent<Renderer>().sharedMaterial = mat;
    }

    void CreateObelisk(string name, Vector3 pos, Material mat, Transform parent)
    {
        GameObject obelisk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        obelisk.name = name;
        obelisk.transform.SetParent(parent);
        obelisk.transform.position = pos;
        obelisk.transform.localScale = new Vector3(0.35f, 1.3f, 0.35f);
        obelisk.GetComponent<Renderer>().sharedMaterial = mat;

        GameObject top = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        top.name = name + "_Top";
        top.transform.SetParent(parent);
        top.transform.position = pos + new Vector3(0, 1.6f, 0);
        top.transform.localScale = new Vector3(0.65f, 0.65f, 0.65f);
        top.GetComponent<Renderer>().sharedMaterial = mat;
    }

    void CreateWing(string name, Vector3 pos, bool left, Material mat, Transform parent)
    {
        GameObject wing = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wing.name = name;
        wing.transform.SetParent(parent);
        wing.transform.position = pos;
        wing.transform.localScale = new Vector3(0.22f, 2.1f, 1.1f);
        wing.transform.rotation = Quaternion.Euler(0, 0, left ? 35 : -35);
        wing.GetComponent<Renderer>().sharedMaterial = mat;
    }

    void CreateTorch(string name, Vector3 pos, Material mat, Transform parent)
    {
        GameObject stand = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        stand.name = name + "_Stand";
        stand.transform.SetParent(parent);
        stand.transform.position = pos + new Vector3(0, 0.7f, 0);
        stand.transform.localScale = new Vector3(0.18f, 0.7f, 0.18f);
        stand.GetComponent<Renderer>().sharedMaterial = darkMat;

        GameObject glow = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        glow.name = name + "_Glow";
        glow.transform.SetParent(parent);
        glow.transform.position = pos + new Vector3(0, 1.55f, 0);
        glow.transform.localScale = new Vector3(0.45f, 0.45f, 0.45f);
        glow.GetComponent<Renderer>().sharedMaterial = mat;
    }

    GameObject NewGroup(string name)
    {
        GameObject g = new GameObject(name);
        g.transform.SetParent(root);
        return g;
    }

    GameObject NewChildGroup(string name, Transform parent)
    {
        GameObject g = new GameObject(name);
        g.transform.SetParent(parent);
        return g;
    }
}