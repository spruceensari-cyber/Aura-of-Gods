using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class AOGVisualArtPass : MonoBehaviour
{
    [Header("Aura of Gods Visual Art Pass")]
    public bool buildOnStart = false;

    private Transform root;

    private Material matDarkStone;
    private Material matStone;
    private Material matOldGold;
    private Material matBlue;
    private Material matRed;
    private Material matTree;
    private Material matTrunk;
    private Material matBone;
    private Material matVoid;
    private Material matFire;
    private Material matWaterGlow;

    void Start()
    {
        if (buildOnStart)
            BuildArtPass();
    }

    [ContextMenu("Build AOG Visual Art Pass")]
    public void BuildArtPass()
    {
        ClearArtPass();

        GameObject artRoot = new GameObject("AOG_ArtPass_RealGameLook");
        root = artRoot.transform;
        root.SetParent(transform);

        CreateMaterials();

        BuildOuterCliffs();
        BuildBlueBaseArchitecture();
        BuildRedBaseArchitecture();
        BuildLaneDetails();
        BuildJungleCamps();
        BuildObjectivePits();
        BuildMythicGuardianTowers();
        BuildForestDepth();
        BuildSmallAtmosphereDetails();
    }

    [ContextMenu("Clear AOG Visual Art Pass")]
    public void ClearArtPass()
    {
        Transform old = transform.Find("AOG_ArtPass_RealGameLook");
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
        matDarkStone = MakeMat("AOG_AP_Dark_Obsidian_Stone", new Color(0.055f, 0.052f, 0.06f));
        matStone = MakeMat("AOG_AP_Old_Ruined_Stone", new Color(0.30f, 0.27f, 0.23f));
        matOldGold = MakeMat("AOG_AP_Old_Gold", new Color(0.85f, 0.55f, 0.16f));
        matBlue = MakeMat("AOG_AP_Celestial_Blue", new Color(0.05f, 0.32f, 1.0f));
        matRed = MakeMat("AOG_AP_Fallen_Red", new Color(0.9f, 0.04f, 0.025f));
        matTree = MakeMat("AOG_AP_Dark_Forest_Crown", new Color(0.012f, 0.07f, 0.03f));
        matTrunk = MakeMat("AOG_AP_Ancient_Trunk", new Color(0.13f, 0.075f, 0.035f));
        matBone = MakeMat("AOG_AP_Bone_Relic", new Color(0.72f, 0.66f, 0.52f));
        matVoid = MakeMat("AOG_AP_Void_Purple", new Color(0.30f, 0.02f, 0.45f));
        matFire = MakeMat("AOG_AP_Fire_Orange", new Color(1.0f, 0.34f, 0.04f));
        matWaterGlow = MakeMat("AOG_AP_Mystic_Water_Glow", new Color(0.0f, 0.42f, 0.65f));
    }

    Material MakeMat(string name, Color color)
    {
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.name = name;
        mat.color = color;
        return mat;
    }

    // =========================================================
    // OUTER CLIFF / BORDER
    // =========================================================

    void BuildOuterCliffs()
    {
        GameObject group = NewGroup("01_Outer_Cliffs_And_Walls");

        CreateWallSegment("North_Cliff_Wall", new Vector3(0, 3, 113), new Vector3(285, 6, 6), group.transform);
        CreateWallSegment("South_Cliff_Wall", new Vector3(0, 3, -113), new Vector3(285, 6, 6), group.transform);
        CreateWallSegment("West_Cliff_Wall", new Vector3(-143, 3, 0), new Vector3(6, 6, 230), group.transform);
        CreateWallSegment("East_Cliff_Wall", new Vector3(143, 3, 0), new Vector3(6, 6, 230), group.transform);

        for (int i = 0; i < 26; i++)
        {
            float x = Random.Range(-135f, 135f);
            float z = Random.value > 0.5f ? Random.Range(105f, 118f) : Random.Range(-118f, -105f);
            CreateRockCluster("Outer_RockCluster", new Vector3(x, 0, z), group.transform);
        }

        for (int i = 0; i < 20; i++)
        {
            float z = Random.Range(-105f, 105f);
            float x = Random.value > 0.5f ? Random.Range(132f, 146f) : Random.Range(-146f, -132f);
            CreateRockCluster("Side_RockCluster", new Vector3(x, 0, z), group.transform);
        }
    }

    void CreateWallSegment(string name, Vector3 pos, Vector3 scale, Transform parent)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.transform.SetParent(parent);
        wall.transform.position = pos;
        wall.transform.localScale = scale;
        wall.GetComponent<Renderer>().sharedMaterial = matDarkStone;
    }

    // =========================================================
    // BASE ARCHITECTURE
    // =========================================================

    void BuildBlueBaseArchitecture()
    {
        GameObject group = NewGroup("02_Blue_Celestial_Base_Architecture");
        Vector3 center = new Vector3(-110, 0, -75);

        CreateBaseRing("Blue_Base_Outer_Ring", center, 34, matStone, group.transform);
        CreateBaseRing("Blue_Base_Inner_Ring", center + Vector3.up * 0.12f, 22, matDarkStone, group.transform);
        CreateCrystalNexus("Blue_Divine_Nexus", center + new Vector3(0, 5, 0), matBlue, group.transform);

        CreateAngelStatue("Blue_Angel_Guardian_Left", center + new Vector3(-14, 3, 12), matBlue, group.transform);
        CreateAngelStatue("Blue_Angel_Guardian_Right", center + new Vector3(14, 3, -12), matBlue, group.transform);

        CreateBanner("Blue_Banner_1", center + new Vector3(-24, 4, 8), matBlue, group.transform);
        CreateBanner("Blue_Banner_2", center + new Vector3(5, 4, -24), matBlue, group.transform);
        CreateSacredLamp(center + new Vector3(-18, 2, -18), matBlue, group.transform);
        CreateSacredLamp(center + new Vector3(18, 2, 18), matBlue, group.transform);
    }

    void BuildRedBaseArchitecture()
    {
        GameObject group = NewGroup("03_Red_Fallen_Base_Architecture");
        Vector3 center = new Vector3(110, 0, 75);

        CreateBaseRing("Red_Base_Outer_Ring", center, 34, matDarkStone, group.transform);
        CreateBaseRing("Red_Base_Inner_Ring", center + Vector3.up * 0.12f, 22, matStone, group.transform);
        CreateCrystalNexus("Red_Abyss_Nexus", center + new Vector3(0, 5, 0), matRed, group.transform);

        CreateFallenStatue("Red_Fallen_Guardian_Left", center + new Vector3(-14, 3, 12), matRed, group.transform);
        CreateFallenStatue("Red_Fallen_Guardian_Right", center + new Vector3(14, 3, -12), matRed, group.transform);

        CreateBanner("Red_Banner_1", center + new Vector3(-24, 4, 8), matRed, group.transform);
        CreateBanner("Red_Banner_2", center + new Vector3(5, 4, -24), matRed, group.transform);
        CreateSacredLamp(center + new Vector3(-18, 2, -18), matRed, group.transform);
        CreateSacredLamp(center + new Vector3(18, 2, 18), matRed, group.transform);
    }

    void CreateBaseRing(string name, Vector3 pos, float radius, Material mat, Transform parent)
    {
        GameObject disk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        disk.name = name;
        disk.transform.SetParent(parent);
        disk.transform.position = pos + new Vector3(0, 0.05f, 0);
        disk.transform.localScale = new Vector3(radius, 0.22f, radius);
        disk.GetComponent<Renderer>().sharedMaterial = mat;

        for (int i = 0; i < 16; i++)
        {
            float a = i * Mathf.PI * 2f / 16f;
            Vector3 p = pos + new Vector3(Mathf.Cos(a) * radius * 0.52f, 0.55f, Mathf.Sin(a) * radius * 0.52f);
            GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pillar.name = name + "_Small_Pillar_" + i;
            pillar.transform.SetParent(parent);
            pillar.transform.position = p;
            pillar.transform.localScale = new Vector3(0.7f, 1.2f, 0.7f);
            pillar.GetComponent<Renderer>().sharedMaterial = matOldGold;
        }
    }

    void CreateCrystalNexus(string name, Vector3 pos, Material teamMat, Transform parent)
    {
        GameObject baseObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        baseObj.name = name + "_Altar";
        baseObj.transform.SetParent(parent);
        baseObj.transform.position = pos + new Vector3(0, -3.8f, 0);
        baseObj.transform.localScale = new Vector3(8, 0.8f, 8);
        baseObj.GetComponent<Renderer>().sharedMaterial = matDarkStone;

        GameObject crystal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        crystal.name = name + "_Crystal_Core";
        crystal.transform.SetParent(parent);
        crystal.transform.position = pos;
        crystal.transform.localScale = new Vector3(3.2f, 5.5f, 3.2f);
        crystal.GetComponent<Renderer>().sharedMaterial = teamMat;

        GameObject top = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        top.name = name + "_Energy_Orb";
        top.transform.SetParent(parent);
        top.transform.position = pos + new Vector3(0, 6.2f, 0);
        top.transform.localScale = new Vector3(3.5f, 3.5f, 3.5f);
        top.GetComponent<Renderer>().sharedMaterial = teamMat;
    }

    // =========================================================
    // LANE DETAILS
    // =========================================================

    void BuildLaneDetails()
    {
        GameObject group = NewGroup("04_Lane_Details_Broken_Stones_Runes");

        Vector3[] mid = {
            new Vector3(-95,0.18f,-25),
            new Vector3(-55,0.18f,-10),
            new Vector3(-10,0.18f,3),
            new Vector3(35,0.18f,16),
            new Vector3(85,0.18f,34)
        };

        Vector3[] top = {
            new Vector3(-110,0.18f,-45),
            new Vector3(-105,0.18f,35),
            new Vector3(-70,0.18f,82),
            new Vector3(-15,0.18f,94),
            new Vector3(50,0.18f,92),
            new Vector3(100,0.18f,70)
        };

        Vector3[] bot = {
            new Vector3(-96,0.18f,-70),
            new Vector3(-55,0.18f,-88),
            new Vector3(10,0.18f,-90),
            new Vector3(70,0.18f,-65),
            new Vector3(105,0.18f,35)
        };

        AddLaneStones("Mid_Lane_Detail", mid, group.transform);
        AddLaneStones("Top_Lane_Detail", top, group.transform);
        AddLaneStones("Bot_Lane_Detail", bot, group.transform);
    }

    void AddLaneStones(string name, Vector3[] points, Transform parent)
    {
        GameObject laneGroup = NewChildGroup(name, parent);

        for (int i = 0; i < points.Length; i++)
        {
            CreateRunePlate(name + "_Rune_" + i, points[i] + Vector3.up * 0.03f, matOldGold, laneGroup.transform);

            for (int j = 0; j < 5; j++)
            {
                Vector3 offset = new Vector3(Random.Range(-7f, 7f), 0, Random.Range(-4f, 4f));
                CreateBrokenStone(name + "_BrokenStone_" + i + "_" + j, points[i] + offset, laneGroup.transform);
            }
        }
    }

    void CreateBrokenStone(string name, Vector3 pos, Transform parent)
    {
        GameObject stone = GameObject.CreatePrimitive(PrimitiveType.Cube);
        stone.name = name;
        stone.transform.SetParent(parent);
        stone.transform.position = pos + new Vector3(0, 0.22f, 0);
        stone.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
        stone.transform.localScale = new Vector3(Random.Range(1.0f, 2.4f), 0.18f, Random.Range(0.6f, 1.8f));
        stone.GetComponent<Renderer>().sharedMaterial = matStone;
    }

    void CreateRunePlate(string name, Vector3 pos, Material mat, Transform parent)
    {
        GameObject rune = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        rune.name = name;
        rune.transform.SetParent(parent);
        rune.transform.position = pos;
        rune.transform.localScale = new Vector3(1.6f, 0.04f, 1.6f);
        rune.GetComponent<Renderer>().sharedMaterial = mat;
    }

    // =========================================================
    // JUNGLE / CAMPS
    // =========================================================

    void BuildJungleCamps()
    {
        GameObject group = NewGroup("05_Jungle_Camps_Ghouls_Nephilim_Ruins");

        CreateGhoulNest("Blue_Ghoul_Nest_1", new Vector3(-72, 0, 30), group.transform);
        CreateGhoulNest("Blue_Ghoul_Nest_2", new Vector3(-52, 0, -35), group.transform);
        CreateNephilimRuin("Blue_Nephilim_Ruin", new Vector3(-18, 0, -65), matBlue, group.transform);

        CreateGhoulNest("Center_Cursed_Camp", new Vector3(18, 0, -25), group.transform);
        CreateNephilimRuin("Center_Upper_Ruin", new Vector3(15, 0, 52), matVoid, group.transform);

        CreateGhoulNest("Red_Ghoul_Nest_1", new Vector3(60, 0, 48), group.transform);
        CreateGhoulNest("Red_Ghoul_Nest_2", new Vector3(58, 0, -8), group.transform);
        CreateNephilimRuin("Red_Nephilim_Ruin", new Vector3(86, 0, -36), matRed, group.transform);
    }

    void CreateGhoulNest(string name, Vector3 center, Transform parent)
    {
        GameObject group = NewChildGroup(name, parent);

        GameObject pit = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pit.name = name + "_Dark_Pit";
        pit.transform.SetParent(group.transform);
        pit.transform.position = center + new Vector3(0, 0.18f, 0);
        pit.transform.localScale = new Vector3(7, 0.18f, 7);
        pit.GetComponent<Renderer>().sharedMaterial = matDarkStone;

        for (int i = 0; i < 6; i++)
        {
            float a = i * Mathf.PI * 2f / 6f;
            Vector3 p = center + new Vector3(Mathf.Cos(a) * 5f, 1.0f, Mathf.Sin(a) * 5f);
            GameObject bone = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            bone.name = name + "_Bone_Totem_" + i;
            bone.transform.SetParent(group.transform);
            bone.transform.position = p;
            bone.transform.localScale = new Vector3(0.4f, 1.2f, 0.4f);
            bone.transform.rotation = Quaternion.Euler(Random.Range(-20, 20), Random.Range(0, 360), Random.Range(-20, 20));
            bone.GetComponent<Renderer>().sharedMaterial = matBone;
        }

        CreateSacredLamp(center + new Vector3(0, 1.2f, 0), matFire, group.transform);
    }

    void CreateNephilimRuin(string name, Vector3 center, Material glowMat, Transform parent)
    {
        GameObject group = NewChildGroup(name, parent);

        GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        platform.name = name + "_Ruin_Platform";
        platform.transform.SetParent(group.transform);
        platform.transform.position = center + new Vector3(0, 0.15f, 0);
        platform.transform.localScale = new Vector3(8, 0.25f, 8);
        platform.GetComponent<Renderer>().sharedMaterial = matStone;

        for (int i = 0; i < 4; i++)
        {
            float a = i * Mathf.PI * 2f / 4f + Mathf.PI * 0.25f;
            Vector3 p = center + new Vector3(Mathf.Cos(a) * 5.5f, 2, Mathf.Sin(a) * 5.5f);

            GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pillar.name = name + "_Broken_Pillar_" + i;
            pillar.transform.SetParent(group.transform);
            pillar.transform.position = p;
            pillar.transform.localScale = new Vector3(0.7f, Random.Range(1.8f, 3.2f), 0.7f);
            pillar.transform.rotation = Quaternion.Euler(Random.Range(-8, 8), Random.Range(0, 360), Random.Range(-8, 8));
            pillar.GetComponent<Renderer>().sharedMaterial = matDarkStone;
        }

        GameObject orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        orb.name = name + "_Core";
        orb.transform.SetParent(group.transform);
        orb.transform.position = center + new Vector3(0, 2.2f, 0);
        orb.transform.localScale = new Vector3(2.2f, 2.2f, 2.2f);
        orb.GetComponent<Renderer>().sharedMaterial = glowMat;
    }

    // =========================================================
    // OBJECTIVES
    // =========================================================

    void BuildObjectivePits()
    {
        GameObject group = NewGroup("06_Major_Objectives_Pits");

        CreateMajorPit(
            "Upper_Nephilim_Behemoth_Altar",
            new Vector3(-38, 0, 33),
            matVoid,
            group.transform
        );

        CreateMajorPit(
            "Lower_Celestial_Leviathan_Pool",
            new Vector3(58, 0, -58),
            matWaterGlow,
            group.transform
        );
    }

    void CreateMajorPit(string name, Vector3 center, Material coreMat, Transform parent)
    {
        GameObject group = NewChildGroup(name, parent);

        GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.name = name + "_Outer_Ring";
        ring.transform.SetParent(group.transform);
        ring.transform.position = center + new Vector3(0, 0.3f, 0);
        ring.transform.localScale = new Vector3(15, 0.35f, 15);
        ring.GetComponent<Renderer>().sharedMaterial = matDarkStone;

        GameObject inner = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        inner.name = name + "_Inner_Energy";
        inner.transform.SetParent(group.transform);
        inner.transform.position = center + new Vector3(0, 0.55f, 0);
        inner.transform.localScale = new Vector3(9, 0.18f, 9);
        inner.GetComponent<Renderer>().sharedMaterial = coreMat;

        for (int i = 0; i < 8; i++)
        {
            float a = i * Mathf.PI * 2f / 8f;
            Vector3 p = center + new Vector3(Mathf.Cos(a) * 9.5f, 1.5f, Mathf.Sin(a) * 9.5f);
            CreateSacredLamp(p, coreMat, group.transform);
        }

        GameObject beast = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        beast.name = name + "_Creature_Placeholder";
        beast.transform.SetParent(group.transform);
        beast.transform.position = center + new Vector3(0, 3.5f, 0);
        beast.transform.localScale = new Vector3(5, 3, 5);
        beast.GetComponent<Renderer>().sharedMaterial = coreMat;
    }

    // =========================================================
    // GUARDIAN TOWERS AS MYTHIC FIGURES
    // =========================================================

    void BuildMythicGuardianTowers()
    {
        GameObject group = NewGroup("07_Mythic_Guardian_Towers");

        // Blue guardian positions
        CreateAngelTower("Blue_Top_Guardian_Outer", new Vector3(-105, 0, 35), matBlue, group.transform);
        CreateAngelTower("Blue_Top_Guardian_Inner", new Vector3(-108, 0, -10), matBlue, group.transform);
        CreateAngelTower("Blue_Mid_Guardian_Outer", new Vector3(-52, 0, -12), matBlue, group.transform);
        CreateAngelTower("Blue_Mid_Guardian_Inner", new Vector3(-82, 0, -48), matBlue, group.transform);
        CreateAngelTower("Blue_Bot_Guardian_Outer", new Vector3(-25, 0, -88), matBlue, group.transform);
        CreateAngelTower("Blue_Bot_Guardian_Inner", new Vector3(-78, 0, -78), matBlue, group.transform);

        // Red guardian positions
        CreateFallenTower("Red_Top_Guardian_Outer", new Vector3(42, 0, 96), matRed, group.transform);
        CreateFallenTower("Red_Top_Guardian_Inner", new Vector3(82, 0, 85), matRed, group.transform);
        CreateFallenTower("Red_Mid_Guardian_Outer", new Vector3(50, 0, 22), matRed, group.transform);
        CreateFallenTower("Red_Mid_Guardian_Inner", new Vector3(82, 0, 48), matRed, group.transform);
        CreateFallenTower("Red_Bot_Guardian_Outer", new Vector3(92, 0, -35), matRed, group.transform);
        CreateFallenTower("Red_Bot_Guardian_Inner", new Vector3(108, 0, 20), matRed, group.transform);
    }

    void CreateAngelTower(string name, Vector3 basePos, Material teamMat, Transform parent)
    {
        GameObject group = NewChildGroup(name, parent);

        GameObject baseDisk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        baseDisk.name = name + "_Base";
        baseDisk.transform.SetParent(group.transform);
        baseDisk.transform.position = basePos + new Vector3(0, 0.4f, 0);
        baseDisk.transform.localScale = new Vector3(4.5f, 0.45f, 4.5f);
        baseDisk.GetComponent<Renderer>().sharedMaterial = matDarkStone;

        CreateAngelStatue(name + "_Angel_Form", basePos + new Vector3(0, 3.4f, 0), teamMat, group.transform);
        CreateSacredLamp(basePos + new Vector3(0, 5.6f, 0), teamMat, group.transform);
    }

    void CreateFallenTower(string name, Vector3 basePos, Material teamMat, Transform parent)
    {
        GameObject group = NewChildGroup(name, parent);

        GameObject baseDisk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        baseDisk.name = name + "_Base";
        baseDisk.transform.SetParent(group.transform);
        baseDisk.transform.position = basePos + new Vector3(0, 0.4f, 0);
        baseDisk.transform.localScale = new Vector3(4.8f, 0.45f, 4.8f);
        baseDisk.GetComponent<Renderer>().sharedMaterial = matDarkStone;

        CreateFallenStatue(name + "_Fallen_Form", basePos + new Vector3(0, 3.4f, 0), teamMat, group.transform);
        CreateSacredLamp(basePos + new Vector3(0, 5.6f, 0), teamMat, group.transform);
    }

    void CreateAngelStatue(string name, Vector3 pos, Material teamMat, Transform parent)
    {
        GameObject group = NewChildGroup(name, parent);

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = name + "_Body";
        body.transform.SetParent(group.transform);
        body.transform.position = pos;
        body.transform.localScale = new Vector3(1.2f, 2.2f, 1.2f);
        body.GetComponent<Renderer>().sharedMaterial = matStone;

        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = name + "_Head";
        head.transform.SetParent(group.transform);
        head.transform.position = pos + new Vector3(0, 2.5f, 0);
        head.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
        head.GetComponent<Renderer>().sharedMaterial = teamMat;

        CreateWing(name + "_Wing_L", pos + new Vector3(-1.25f, 1.2f, 0), true, matOldGold, group.transform);
        CreateWing(name + "_Wing_R", pos + new Vector3(1.25f, 1.2f, 0), false, matOldGold, group.transform);

        GameObject spear = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        spear.name = name + "_Spear";
        spear.transform.SetParent(group.transform);
        spear.transform.position = pos + new Vector3(0.9f, 0.4f, 0.2f);
        spear.transform.rotation = Quaternion.Euler(20, 0, 0);
        spear.transform.localScale = new Vector3(0.12f, 2.4f, 0.12f);
        spear.GetComponent<Renderer>().sharedMaterial = matOldGold;
    }

    void CreateFallenStatue(string name, Vector3 pos, Material teamMat, Transform parent)
    {
        GameObject group = NewChildGroup(name, parent);

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = name + "_Body";
        body.transform.SetParent(group.transform);
        body.transform.position = pos;
        body.transform.localScale = new Vector3(1.35f, 2.35f, 1.35f);
        body.GetComponent<Renderer>().sharedMaterial = matDarkStone;

        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = name + "_Head";
        head.transform.SetParent(group.transform);
        head.transform.position = pos + new Vector3(0, 2.5f, 0);
        head.transform.localScale = new Vector3(0.9f, 0.9f, 0.9f);
        head.GetComponent<Renderer>().sharedMaterial = teamMat;

        CreateWing(name + "_Broken_Wing_L", pos + new Vector3(-1.35f, 1.1f, 0), true, matDarkStone, group.transform);
        CreateWing(name + "_Broken_Wing_R", pos + new Vector3(1.35f, 1.1f, 0), false, matDarkStone, group.transform);

        GameObject hornA = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        hornA.name = name + "_Horn_A";
        hornA.transform.SetParent(group.transform);
        hornA.transform.position = pos + new Vector3(-0.35f, 3.15f, 0);
        hornA.transform.rotation = Quaternion.Euler(25, 0, 20);
        hornA.transform.localScale = new Vector3(0.12f, 0.8f, 0.12f);
        hornA.GetComponent<Renderer>().sharedMaterial = matRed;

        GameObject hornB = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        hornB.name = name + "_Horn_B";
        hornB.transform.SetParent(group.transform);
        hornB.transform.position = pos + new Vector3(0.35f, 3.15f, 0);
        hornB.transform.rotation = Quaternion.Euler(25, 0, -20);
        hornB.transform.localScale = new Vector3(0.12f, 0.8f, 0.12f);
        hornB.GetComponent<Renderer>().sharedMaterial = matRed;
    }

    void CreateWing(string name, Vector3 pos, bool left, Material mat, Transform parent)
    {
        GameObject wing = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wing.name = name;
        wing.transform.SetParent(parent);
        wing.transform.position = pos;
        wing.transform.localScale = new Vector3(0.25f, 2.2f, 1.2f);
        wing.transform.rotation = Quaternion.Euler(0, 0, left ? 35 : -35);
        wing.GetComponent<Renderer>().sharedMaterial = mat;
    }

    // =========================================================
    // FOREST DEPTH
    // =========================================================

    void BuildForestDepth()
    {
        GameObject group = NewGroup("08_Forest_Depth_Trees_Rocks");

        Vector3[] clusters =
        {
            new Vector3(-120,0,80),
            new Vector3(-120,0,-20),
            new Vector3(-100,0,-95),
            new Vector3(-55,0,70),
            new Vector3(-5,0,86),
            new Vector3(0,0,-105),
            new Vector3(55,0,-95),
            new Vector3(105,0,-72),
            new Vector3(120,0,10),
            new Vector3(120,0,92)
        };

        foreach (Vector3 c in clusters)
        {
            for (int i = 0; i < 14; i++)
            {
                Vector3 p = c + new Vector3(Random.Range(-12f, 12f), 0, Random.Range(-10f, 10f));
                CreateStylizedTree("Forest_Tree", p, group.transform);
            }

            for (int i = 0; i < 5; i++)
            {
                Vector3 p = c + new Vector3(Random.Range(-10f, 10f), 0, Random.Range(-8f, 8f));
                CreateRockCluster("Forest_Rock", p, group.transform);
            }
        }
    }

    void CreateStylizedTree(string name, Vector3 pos, Transform parent)
    {
        GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.name = name + "_Trunk";
        trunk.transform.SetParent(parent);
        trunk.transform.position = pos + new Vector3(0, 1.1f, 0);
        trunk.transform.localScale = new Vector3(0.45f, 1.2f, 0.45f);
        trunk.GetComponent<Renderer>().sharedMaterial = matTrunk;

        GameObject crown = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        crown.name = name + "_Crown";
        crown.transform.SetParent(parent);
        crown.transform.position = pos + new Vector3(0, 2.6f, 0);
        crown.transform.localScale = new Vector3(Random.Range(1.8f, 2.7f), Random.Range(2.0f, 3.0f), Random.Range(1.8f, 2.7f));
        crown.GetComponent<Renderer>().sharedMaterial = matTree;
    }

    void CreateRockCluster(string name, Vector3 center, Transform parent)
    {
        int count = Random.Range(3, 6);
        for (int i = 0; i < count; i++)
        {
            GameObject rock = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rock.name = name + "_" + i;
            rock.transform.SetParent(parent);
            rock.transform.position = center + new Vector3(Random.Range(-2f, 2f), 0.45f, Random.Range(-2f, 2f));
            rock.transform.localScale = new Vector3(Random.Range(1.0f, 2.8f), Random.Range(0.8f, 1.8f), Random.Range(1.0f, 2.8f));
            rock.GetComponent<Renderer>().sharedMaterial = matDarkStone;
        }
    }

    // =========================================================
    // ATMOSPHERE SMALL DETAILS
    // =========================================================

    void BuildSmallAtmosphereDetails()
    {
        GameObject group = NewGroup("09_Atmosphere_Torches_Runes_Relics");

        Vector3[] torchPositions =
        {
            new Vector3(-95,0, -28), new Vector3(-55,0,-10), new Vector3(0,0,6), new Vector3(55,0,22),
            new Vector3(-105,0,35), new Vector3(-70,0,82), new Vector3(45,0,92),
            new Vector3(-55,0,-88), new Vector3(20,0,-90), new Vector3(90,0,-55)
        };

        foreach (Vector3 p in torchPositions)
        {
            CreateSacredLamp(p + new Vector3(0, 1.3f, 0), matFire, group.transform);
        }

        CreateBanner("Central_Blue_Banner_Relic", new Vector3(-8, 2.8f, 0), matBlue, group.transform);
        CreateBanner("Central_Red_Banner_Relic", new Vector3(18, 2.8f, 12), matRed, group.transform);

        CreateRunePlate("Golden_Center_Rune", new Vector3(0, 0.35f, 5), matOldGold, group.transform);
        CreateRunePlate("Void_Rune_Upper", new Vector3(-38, 0.35f, 33), matVoid, group.transform);
        CreateRunePlate("Water_Rune_Lower", new Vector3(58, 0.35f, -58), matWaterGlow, group.transform);
    }

    void CreateSacredLamp(Vector3 pos, Material glowMat, Transform parent)
    {
        GameObject stand = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        stand.name = "Sacred_Lamp_Stand";
        stand.transform.SetParent(parent);
        stand.transform.position = pos;
        stand.transform.localScale = new Vector3(0.28f, 1.0f, 0.28f);
        stand.GetComponent<Renderer>().sharedMaterial = matDarkStone;

        GameObject flame = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        flame.name = "Sacred_Lamp_Glow";
        flame.transform.SetParent(parent);
        flame.transform.position = pos + new Vector3(0, 1.2f, 0);
        flame.transform.localScale = new Vector3(0.75f, 0.75f, 0.75f);
        flame.GetComponent<Renderer>().sharedMaterial = glowMat;
    }

    void CreateBanner(string name, Vector3 pos, Material bannerMat, Transform parent)
    {
        GameObject pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pole.name = name + "_Pole";
        pole.transform.SetParent(parent);
        pole.transform.position = pos;
        pole.transform.localScale = new Vector3(0.14f, 2.2f, 0.14f);
        pole.GetComponent<Renderer>().sharedMaterial = matOldGold;

        GameObject banner = GameObject.CreatePrimitive(PrimitiveType.Cube);
        banner.name = name + "_Cloth";
        banner.transform.SetParent(parent);
        banner.transform.position = pos + new Vector3(0.65f, 0.7f, 0);
        banner.transform.localScale = new Vector3(1.1f, 1.4f, 0.08f);
        banner.GetComponent<Renderer>().sharedMaterial = bannerMat;
    }

    // =========================================================
    // HELPERS
    // =========================================================

    GameObject NewGroup(string name)
    {
        GameObject group = new GameObject(name);
        group.transform.SetParent(root);
        return group;
    }

    GameObject NewChildGroup(string name, Transform parent)
    {
        GameObject group = new GameObject(name);
        group.transform.SetParent(parent);
        return group;
    }
}