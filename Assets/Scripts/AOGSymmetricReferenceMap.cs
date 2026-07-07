using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class AOGSymmetricReferenceMap : MonoBehaviour
{
    Transform root;

    Material groundMat;
    Material roadMat;
    Material roadEdgeMat;
    Material riverMat;
    Material darkStoneMat;
    Material stoneMat;
    Material blueMat;
    Material redMat;
    Material goldMat;
    Material treeMat;
    Material trunkMat;
    Material voidMat;

    [ContextMenu("Build Symmetric Aura Of Gods Map")]
    public void BuildMap()
    {
        ClearMap();

        Random.InitState(777);

        GameObject rootObj = new GameObject("AOG_Symmetric_Reference_Map");
        root = rootObj.transform;
        root.SetParent(transform);

        CreateMaterials();

        CreateGround();
        CreateMainLanes();
        CreateRiver();
        CreateBases();
        CreateSymmetricTowers();
        CreateJungleIslands();
        CreateObjectivePits();
        CreateOuterWalls();
        CreateForestBorder();
        CreateLaneDetails();
    }

    [ContextMenu("Clear Symmetric Aura Of Gods Map")]
    public void ClearMap()
    {
        string[] names =
        {
            "AOG_Symmetric_Reference_Map",
            "AOG_VisualMap",
            "AOG_ArtPass_RealGameLook"
        };

        foreach (string n in names)
        {
            Transform old = transform.Find(n);
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
    }

    void CreateMaterials()
    {
        groundMat = MakeMat("AOG_SR_Dark_Ground", new Color32(10, 31, 15, 255));
        roadMat = MakeMat("AOG_SR_Ancient_Road", new Color32(92, 78, 58, 255));
        roadEdgeMat = MakeMat("AOG_SR_Road_Edge", new Color32(30, 25, 22, 255));
        riverMat = MakeMat("AOG_SR_Mystic_River", new Color32(5, 42, 58, 255));
        darkStoneMat = MakeMat("AOG_SR_Dark_Stone", new Color32(18, 17, 20, 255));
        stoneMat = MakeMat("AOG_SR_Old_Stone", new Color32(72, 68, 62, 255));
        blueMat = MakeMat("AOG_SR_Celestial_Blue", new Color32(25, 95, 255, 255));
        redMat = MakeMat("AOG_SR_Fallen_Red", new Color32(190, 25, 18, 255));
        goldMat = MakeMat("AOG_SR_Old_Gold", new Color32(210, 150, 45, 255));
        treeMat = MakeMat("AOG_SR_Dark_Tree", new Color32(6, 38, 18, 255));
        trunkMat = MakeMat("AOG_SR_Trunk", new Color32(55, 32, 18, 255));
        voidMat = MakeMat("AOG_SR_Void_Purple", new Color32(90, 15, 140, 255));
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

    void CreateGround()
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Main_Dark_Ground";
        ground.transform.SetParent(root);
        ground.transform.position = new Vector3(0, -0.08f, 0);
        ground.transform.localScale = new Vector3(32, 1, 24);
        ground.GetComponent<Renderer>().sharedMaterial = groundMat;
    }

    void CreateMainLanes()
    {
        GameObject lanes = NewGroup("01_Symmetric_Lanes");

        Vector3 blueBase = new Vector3(-105, 0.05f, -78);
        Vector3 redBase = new Vector3(105, 0.05f, 78);

        // MID: tam merkezden geçer.
        CreateRoadPath("MID_LANE_CENTERED", lanes.transform, 12,
            blueBase,
            new Vector3(-60, 0.05f, -44),
            new Vector3(-25, 0.05f, -18),
            new Vector3(0, 0.05f, 0),
            new Vector3(25, 0.05f, 18),
            new Vector3(60, 0.05f, 44),
            redBase
        );

// TOP: üstten döner, bot lane ile daha dengeli.
CreateRoadPath("TOP_LANE_OUTER", lanes.transform, 11,
    blueBase,
    new Vector3(-118, 0.05f, -52),
    new Vector3(-112, 0.05f, 10),
    new Vector3(-92, 0.05f, 50),
    new Vector3(-48, 0.05f, 88),
    new Vector3(10, 0.05f, 96),
    new Vector3(58, 0.05f, 108),
    redBase
);

// BOT: alt taraftan döner ama artık harita dışına taşmaz.
CreateRoadPath("BOT_LANE_OUTER", lanes.transform, 11,
    blueBase,
    new Vector3(-58, 0.05f, -108),
    new Vector3(-10, 0.05f, -96),
    new Vector3(48, 0.05f, -88),
    new Vector3(92, 0.05f, -50),
    new Vector3(112, 0.05f, 10),
    new Vector3(118, 0.05f, 52),
    redBase
);
    }

    void CreateRiver()
    {
        GameObject river = NewGroup("02_Symmetric_River");

        // Referanstaki gibi iki parçalı ama merkez simetrisine yakın.
        CreateRoadPath("Upper_River_Bend", river.transform, 18,
            new Vector3(-78, 0.02f, 24),
            new Vector3(-55, 0.02f, 42),
            new Vector3(-25, 0.02f, 39),
            new Vector3(0, 0.02f, 24),
            new Vector3(25, 0.02f, 20)
        );

        CreateRoadPath("Lower_River_Bend", river.transform, 18,
            new Vector3(-25, 0.02f, -20),
            new Vector3(0, 0.02f, -24),
            new Vector3(25, 0.02f, -39),
            new Vector3(55, 0.02f, -42),
            new Vector3(78, 0.02f, -24)
        );

        CreateDisk("Upper_River_Pool", new Vector3(-38, 0.03f, 36), new Vector3(20, 0.08f, 14), riverMat, river.transform);
        CreateDisk("Lower_River_Pool", new Vector3(38, 0.03f, -36), new Vector3(20, 0.08f, 14), riverMat, river.transform);
    }

    void CreateBases()
    {
        GameObject bases = NewGroup("03_Bases");

        CreateBase("Blue_Celestial_Base", new Vector3(-105, 0, -78), blueMat, bases.transform);
        CreateBase("Red_Fallen_Base", new Vector3(105, 0, 78), redMat, bases.transform);
    }

    void CreateBase(string name, Vector3 center, Material teamMat, Transform parent)
    {
        GameObject group = NewChildGroup(name, parent);

        CreateDisk(name + "_Outer_Platform", center + new Vector3(0, 0.05f, 0), new Vector3(34, 0.25f, 34), stoneMat, group.transform);
        CreateDisk(name + "_Inner_Platform", center + new Vector3(0, 0.22f, 0), new Vector3(22, 0.25f, 22), darkStoneMat, group.transform);
        CreateDisk(name + "_Rune_Aura", center + new Vector3(0, 0.42f, 0), new Vector3(14, 0.08f, 14), teamMat, group.transform);

        GameObject nexus = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        nexus.name = name + "_Nexus_Crystal";
        nexus.transform.SetParent(group.transform);
        nexus.transform.position = center + new Vector3(0, 5.0f, 0);
        nexus.transform.localScale = new Vector3(4, 5, 4);
        nexus.GetComponent<Renderer>().sharedMaterial = teamMat;

        GameObject orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        orb.name = name + "_Nexus_Orb";
        orb.transform.SetParent(group.transform);
        orb.transform.position = center + new Vector3(0, 10.2f, 0);
        orb.transform.localScale = new Vector3(4.5f, 4.5f, 4.5f);
        orb.GetComponent<Renderer>().sharedMaterial = teamMat;

        for (int i = 0; i < 12; i++)
        {
            float a = i * Mathf.PI * 2f / 12f;
            Vector3 p = center + new Vector3(Mathf.Cos(a) * 18, 1.5f, Mathf.Sin(a) * 18);
            CreateSmallPillar(name + "_Base_Pillar_" + i, p, teamMat, group.transform);
        }
    }

    void CreateSymmetricTowers()
    {
        GameObject towers = NewGroup("04_Symmetric_Mythic_Guardians");

        // Blue towers
        CreateTower("Blue_Mid_Outer", new Vector3(-52, 0, -38), blueMat, true, towers.transform);
        CreateTower("Blue_Mid_Inner", new Vector3(-82, 0, -60), blueMat, true, towers.transform);

        CreateTower("Blue_Top_Outer", new Vector3(-118, 0, 18), blueMat, true, towers.transform);
        CreateTower("Blue_Top_Inner", new Vector3(-108, 0, -34), blueMat, true, towers.transform);

        CreateTower("Blue_Bot_Outer", new Vector3(-18, 0, -118), blueMat, true, towers.transform);
        CreateTower("Blue_Bot_Inner", new Vector3(-68, 0, -92), blueMat, true, towers.transform);

        // Red towers = blue towers’ın merkez simetrisi.
        CreateTower("Red_Mid_Outer", Mirror(new Vector3(-52, 0, -38)), redMat, false, towers.transform);
        CreateTower("Red_Mid_Inner", Mirror(new Vector3(-82, 0, -60)), redMat, false, towers.transform);

        CreateTower("Red_Top_Outer", Mirror(new Vector3(-18, 0, -118)), redMat, false, towers.transform);
        CreateTower("Red_Top_Inner", Mirror(new Vector3(-68, 0, -92)), redMat, false, towers.transform);

        CreateTower("Red_Bot_Outer", Mirror(new Vector3(-118, 0, 18)), redMat, false, towers.transform);
        CreateTower("Red_Bot_Inner", Mirror(new Vector3(-108, 0, -34)), redMat, false, towers.transform);
    }

    Vector3 Mirror(Vector3 v)
    {
        return new Vector3(-v.x, v.y, -v.z);
    }

    void CreateTower(string name, Vector3 pos, Material teamMat, bool angelic, Transform parent)
    {
        GameObject group = NewChildGroup(name, parent);

        CreateDisk(name + "_Base", pos + new Vector3(0, 0.35f, 0), new Vector3(5, 0.45f, 5), darkStoneMat, group.transform);

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = name + "_Guardian_Body";
        body.transform.SetParent(group.transform);
        body.transform.position = pos + new Vector3(0, 3.2f, 0);
        body.transform.localScale = new Vector3(1.4f, 2.5f, 1.4f);
        body.GetComponent<Renderer>().sharedMaterial = angelic ? stoneMat : darkStoneMat;

        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = name + "_Energy_Head";
        head.transform.SetParent(group.transform);
        head.transform.position = pos + new Vector3(0, 6.0f, 0);
        head.transform.localScale = new Vector3(1.4f, 1.4f, 1.4f);
        head.GetComponent<Renderer>().sharedMaterial = teamMat;

        CreateWing(name + "_Wing_L", pos + new Vector3(-1.6f, 4.3f, 0), true, angelic ? goldMat : darkStoneMat, group.transform);
        CreateWing(name + "_Wing_R", pos + new Vector3(1.6f, 4.3f, 0), false, angelic ? goldMat : darkStoneMat, group.transform);

        CreateSmallPillar(name + "_Attack_Core", pos + new Vector3(0, 7.3f, 0), teamMat, group.transform);
    }

    void CreateJungleIslands()
    {
        GameObject jungle = NewGroup("05_Symmetric_Jungle_Camps");

        Vector3[] blueCamps =
        {
            new Vector3(-78,0,34),
            new Vector3(-70,0,-14),
            new Vector3(-42,0,-66),
            new Vector3(-12,0,58),
            new Vector3(-20,0,-22)
        };

        foreach (Vector3 p in blueCamps)
        {
            CreateJungleCamp("Blue_Camp", p, blueMat, jungle.transform);
            CreateJungleCamp("Red_Camp", Mirror(p), redMat, jungle.transform);
        }
    }

    void CreateJungleCamp(string name, Vector3 center, Material coreMat, Transform parent)
    {
        GameObject group = NewChildGroup(name + "_" + Mathf.RoundToInt(center.x) + "_" + Mathf.RoundToInt(center.z), parent);

        CreateDisk("Camp_Dark_Ground", center + new Vector3(0, 0.1f, 0), new Vector3(10, 0.15f, 8), darkStoneMat, group.transform);

        for (int i = 0; i < 8; i++)
        {
            float a = i * Mathf.PI * 2f / 8f;
            Vector3 p = center + new Vector3(Mathf.Cos(a) * 6, 1.2f, Mathf.Sin(a) * 4.8f);
            CreateSmallPillar("Camp_Ruin_Pillar_" + i, p, stoneMat, group.transform);
        }

        GameObject core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        core.name = "Camp_Creature_Core";
        core.transform.SetParent(group.transform);
        core.transform.position = center + new Vector3(0, 2.2f, 0);
        core.transform.localScale = new Vector3(2.2f, 2.2f, 2.2f);
        core.GetComponent<Renderer>().sharedMaterial = coreMat;

        for (int i = 0; i < 12; i++)
        {
            Vector3 t = center + new Vector3(Random.Range(-9f, 9f), 0, Random.Range(-7f, 7f));
            CreateTree(t, group.transform);
        }
    }

    void CreateObjectivePits()
    {
        GameObject obj = NewGroup("06_Symmetric_Major_Objectives");

        CreateMajorObjective("Upper_Nephilim_Behemoth", new Vector3(-38, 0, 36), voidMat, obj.transform);
        CreateMajorObjective("Lower_Celestial_Leviathan", new Vector3(38, 0, -36), riverMat, obj.transform);
    }

    void CreateMajorObjective(string name, Vector3 center, Material coreMat, Transform parent)
    {
        GameObject group = NewChildGroup(name, parent);

        CreateDisk(name + "_Outer_Ring", center + new Vector3(0, 0.35f, 0), new Vector3(17, 0.35f, 17), darkStoneMat, group.transform);
        CreateDisk(name + "_Inner_Energy", center + new Vector3(0, 0.7f, 0), new Vector3(10, 0.18f, 10), coreMat, group.transform);

        GameObject beast = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        beast.name = name + "_Creature";
        beast.transform.SetParent(group.transform);
        beast.transform.position = center + new Vector3(0, 4, 0);
        beast.transform.localScale = new Vector3(5.5f, 3.5f, 5.5f);
        beast.GetComponent<Renderer>().sharedMaterial = coreMat;

        for (int i = 0; i < 10; i++)
        {
            float a = i * Mathf.PI * 2f / 10f;
            Vector3 p = center + new Vector3(Mathf.Cos(a) * 12, 1.2f, Mathf.Sin(a) * 12);
            CreateSmallPillar(name + "_Torch_" + i, p, goldMat, group.transform);
        }
    }

    void CreateOuterWalls()
    {
        GameObject walls = NewGroup("07_Outer_Dark_Fortress_Walls");

        CreateCube("North_Wall", new Vector3(0, 3, 116), new Vector3(290, 6, 6), darkStoneMat, walls.transform);
        CreateCube("South_Wall", new Vector3(0, 3, -116), new Vector3(290, 6, 6), darkStoneMat, walls.transform);
        CreateCube("West_Wall", new Vector3(-146, 3, 0), new Vector3(6, 6, 235), darkStoneMat, walls.transform);
        CreateCube("East_Wall", new Vector3(146, 3, 0), new Vector3(6, 6, 235), darkStoneMat, walls.transform);
    }

    void CreateForestBorder()
    {
        GameObject forest = NewGroup("08_Dark_Forest_Border");

        for (int i = 0; i < 160; i++)
        {
            bool horizontal = Random.value > 0.5f;
            float x;
            float z;

            if (horizontal)
            {
                x = Random.Range(-138f, 138f);
                z = Random.value > 0.5f ? Random.Range(96f, 112f) : Random.Range(-112f, -96f);
            }
            else
            {
                x = Random.value > 0.5f ? Random.Range(126f, 142f) : Random.Range(-142f, -126f);
                z = Random.Range(-104f, 104f);
            }

            CreateTree(new Vector3(x, 0, z), forest.transform);
        }
    }

    void CreateLaneDetails()
    {
        GameObject details = NewGroup("09_Lane_Runes_And_Broken_Stones");

        Vector3[] points =
        {
            new Vector3(-80,0,-58),
            new Vector3(-50,0,-38),
            new Vector3(-22,0,-16),
            new Vector3(0,0,0),
            new Vector3(22,0,16),
            new Vector3(50,0,38),
            new Vector3(80,0,58),

            new Vector3(-112,0,30),
            new Vector3(-85,0,84),
            new Vector3(0,0,102),
            new Vector3(85,0,84),
            new Vector3(112,0,30),

            new Vector3(-30,0,-112),
            new Vector3(30,0,-104),
            new Vector3(88,0,-70),
            new Vector3(116,0,-20)
        };

        foreach (Vector3 p in points)
        {
            CreateDisk("Small_Gold_Rune", p + new Vector3(0, 0.23f, 0), new Vector3(1.5f, 0.04f, 1.5f), goldMat, details.transform);

            for (int i = 0; i < 3; i++)
            {
                CreateCube("Broken_Road_Stone", p + new Vector3(Random.Range(-4f, 4f), 0.25f, Random.Range(-3f, 3f)), new Vector3(Random.Range(1f, 2.2f), 0.16f, Random.Range(0.6f, 1.8f)), stoneMat, details.transform);
            }
        }
    }

    void CreateRoadPath(string name, Transform parent, float width, params Vector3[] points)
    {
        GameObject group = NewChildGroup(name, parent);

        for (int i = 0; i < points.Length - 1; i++)
        {
            CreateSegment(name + "_Edge_" + i, points[i], points[i + 1], width + 3.5f, 0.13f, roadEdgeMat, group.transform);
            CreateSegment(name + "_Stone_" + i, points[i] + Vector3.up * 0.035f, points[i + 1] + Vector3.up * 0.035f, width, 0.16f, roadMat, group.transform);
        }
    }

    void CreateSegment(string name, Vector3 a, Vector3 b, float width, float height, Material mat, Transform parent)
    {
        Vector3 mid = (a + b) * 0.5f;
        Vector3 dir = b - a;
        float len = dir.magnitude;

        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = name;
        obj.transform.SetParent(parent);
        obj.transform.position = mid;
        obj.transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
        obj.transform.localScale = new Vector3(width, height, len);
        obj.GetComponent<Renderer>().sharedMaterial = mat;
    }

    GameObject CreateDisk(string name, Vector3 pos, Vector3 scale, Material mat, Transform parent)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        obj.name = name;
        obj.transform.SetParent(parent);
        obj.transform.position = pos;
        obj.transform.localScale = scale;
        obj.GetComponent<Renderer>().sharedMaterial = mat;
        return obj;
    }

    void CreateSmallPillar(string name, Vector3 pos, Material mat, Transform parent)
    {
        GameObject p = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        p.name = name;
        p.transform.SetParent(parent);
        p.transform.position = pos;
        p.transform.localScale = new Vector3(0.55f, 1.5f, 0.55f);
        p.GetComponent<Renderer>().sharedMaterial = mat;
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

    void CreateTree(Vector3 pos, Transform parent)
    {
        GameObject trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.name = "Dark_Tree_Trunk";
        trunk.transform.SetParent(parent);
        trunk.transform.position = pos + new Vector3(0, 1.0f, 0);
        trunk.transform.localScale = new Vector3(0.45f, 1.1f, 0.45f);
        trunk.GetComponent<Renderer>().sharedMaterial = trunkMat;

        GameObject crown = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        crown.name = "Dark_Tree_Crown";
        crown.transform.SetParent(parent);
        crown.transform.position = pos + new Vector3(0, 2.6f, 0);
        crown.transform.localScale = new Vector3(Random.Range(1.8f, 2.8f), Random.Range(2.0f, 3.2f), Random.Range(1.8f, 2.8f));
        crown.GetComponent<Renderer>().sharedMaterial = treeMat;
    }

    void CreateCube(string name, Vector3 pos, Vector3 scale, Material mat, Transform parent)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = name;
        obj.transform.SetParent(parent);
        obj.transform.position = pos;
        obj.transform.localScale = scale;
        obj.GetComponent<Renderer>().sharedMaterial = mat;
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