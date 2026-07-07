using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class AOGStructureUpgrade : MonoBehaviour
{
    Transform root;

    Material blueMat;
    Material redMat;
    Material goldMat;
    Material darkStoneMat;
    Material stoneMat;
    Material crystalMat;
    Material voidMat;
    Material fireMat;

    [ContextMenu("Build Structure Visual Upgrade")]
    public void BuildUpgrade()
    {
        ClearUpgrade();

        GameObject rootObj = new GameObject("AOG_Structure_Visual_Upgrade");
        root = rootObj.transform;
        root.SetParent(transform);

        CreateMaterials();

        BuildNexusUpgrades();
        BuildGuardianTowerUpgrades();
        BuildJungleCampUpgrades();
        BuildObjectiveUpgrades();
    }

    [ContextMenu("Clear Structure Visual Upgrade")]
    public void ClearUpgrade()
    {
        Transform old = transform.Find("AOG_Structure_Visual_Upgrade");
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
        blueMat = MakeMat("AOG_SU_Celestial_Blue", new Color32(30, 110, 255, 255));
        redMat = MakeMat("AOG_SU_Fallen_Red", new Color32(210, 30, 20, 255));
        goldMat = MakeMat("AOG_SU_Old_Gold", new Color32(220, 160, 45, 255));
        darkStoneMat = MakeMat("AOG_SU_Dark_Stone", new Color32(22, 20, 24, 255));
        stoneMat = MakeMat("AOG_SU_Old_Stone", new Color32(90, 82, 72, 255));
        crystalMat = MakeMat("AOG_SU_Crystal_WhiteBlue", new Color32(120, 190, 255, 255));
        voidMat = MakeMat("AOG_SU_Void_Purple", new Color32(110, 20, 160, 255));
        fireMat = MakeMat("AOG_SU_Fire_Orange", new Color32(255, 95, 20, 255));
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

    void BuildNexusUpgrades()
    {
        GameObject group = NewGroup("01_Nexus_Final_Visuals");

        CreateFinalNexus("Blue_Final_Nexus", new Vector3(-105, 0, -78), blueMat, true, group.transform);
        CreateFinalNexus("Red_Final_Nexus", new Vector3(105, 0, 78), redMat, false, group.transform);
    }

    void CreateFinalNexus(string name, Vector3 center, Material teamMat, bool blueSide, Transform parent)
    {
        GameObject group = NewChildGroup(name, parent);

        CreateCylinder(name + "_Massive_Base", center + new Vector3(0, 0.35f, 0), new Vector3(14, 0.7f, 14), darkStoneMat, group.transform);
        CreateCylinder(name + "_Second_Ring", center + new Vector3(0, 1.0f, 0), new Vector3(10, 0.45f, 10), stoneMat, group.transform);
        CreateCylinder(name + "_Energy_Ring", center + new Vector3(0, 1.35f, 0), new Vector3(7, 0.16f, 7), teamMat, group.transform);

        GameObject core = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        core.name = name + "_Tall_Crystal_Core";
        core.transform.SetParent(group.transform);
        core.transform.position = center + new Vector3(0, 6.4f, 0);
        core.transform.localScale = new Vector3(3.0f, 6.2f, 3.0f);
        core.GetComponent<Renderer>().sharedMaterial = teamMat;

        GameObject orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        orb.name = name + "_Floating_Orb";
        orb.transform.SetParent(group.transform);
        orb.transform.position = center + new Vector3(0, 13.2f, 0);
        orb.transform.localScale = new Vector3(3.2f, 3.2f, 3.2f);
        orb.GetComponent<Renderer>().sharedMaterial = teamMat;

        for (int i = 0; i < 8; i++)
        {
            float a = i * Mathf.PI * 2f / 8f;
            Vector3 p = center + new Vector3(Mathf.Cos(a) * 9.5f, 2.6f, Mathf.Sin(a) * 9.5f);
            CreateObelisk(name + "_Nexus_Obelisk_" + i, p, teamMat, group.transform);
        }

        if (blueSide)
        {
            CreateWingedStatue(name + "_Left_Angel", center + new Vector3(-11, 3, 7), blueMat, true, group.transform);
            CreateWingedStatue(name + "_Right_Angel", center + new Vector3(11, 3, -7), blueMat, true, group.transform);
        }
        else
        {
            CreateWingedStatue(name + "_Left_Fallen", center + new Vector3(-11, 3, 7), redMat, false, group.transform);
            CreateWingedStatue(name + "_Right_Fallen", center + new Vector3(11, 3, -7), redMat, false, group.transform);
        }
    }

    void BuildGuardianTowerUpgrades()
    {
        GameObject group = NewGroup("02_Guardian_Tower_Final_Visuals");

        Vector3[] blue =
        {
            new Vector3(-52,0,-38),
            new Vector3(-82,0,-60),
            new Vector3(-118,0,18),
            new Vector3(-108,0,-34),
            new Vector3(-18,0,-118),
            new Vector3(-68,0,-92)
        };

        string[] blueNames =
        {
            "Blue_Mid_Outer",
            "Blue_Mid_Inner",
            "Blue_Top_Outer",
            "Blue_Top_Inner",
            "Blue_Bot_Outer",
            "Blue_Bot_Inner"
        };

        for (int i = 0; i < blue.Length; i++)
        {
            CreateFinalGuardianTower(blueNames[i] + "_Final", blue[i], blueMat, true, group.transform);
            CreateFinalGuardianTower(blueNames[i].Replace("Blue", "Red") + "_Final", Mirror(blue[i]), redMat, false, group.transform);
        }
    }

    void CreateFinalGuardianTower(string name, Vector3 pos, Material teamMat, bool angelic, Transform parent)
    {
        GameObject group = NewChildGroup(name, parent);

        CreateCylinder(name + "_Base_Ring", pos + new Vector3(0, 0.35f, 0), new Vector3(5.6f, 0.55f, 5.6f), darkStoneMat, group.transform);
        CreateCylinder(name + "_Inner_Ring", pos + new Vector3(0, 0.85f, 0), new Vector3(3.8f, 0.35f, 3.8f), stoneMat, group.transform);

        CreateWingedStatue(name + "_Guardian_Form", pos + new Vector3(0, 3.3f, 0), teamMat, angelic, group.transform);

        GameObject energy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        energy.name = name + "_Attack_Energy_Core";
        energy.transform.SetParent(group.transform);
        energy.transform.position = pos + new Vector3(0, 7.1f, 0);
        energy.transform.localScale = new Vector3(1.4f, 1.4f, 1.4f);
        energy.GetComponent<Renderer>().sharedMaterial = teamMat;

        for (int i = 0; i < 4; i++)
        {
            float a = i * Mathf.PI * 2f / 4f + Mathf.PI / 4f;
            Vector3 p = pos + new Vector3(Mathf.Cos(a) * 3.5f, 1.8f, Mathf.Sin(a) * 3.5f);
            CreateSmallFlame(name + "_Small_Flame_" + i, p, teamMat, group.transform);
        }
    }

    void CreateWingedStatue(string name, Vector3 pos, Material teamMat, bool angelic, Transform parent)
    {
        GameObject group = NewChildGroup(name, parent);

        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = name + "_Body";
        body.transform.SetParent(group.transform);
        body.transform.position = pos;
        body.transform.localScale = angelic ? new Vector3(1.15f, 2.2f, 1.15f) : new Vector3(1.35f, 2.3f, 1.35f);
        body.GetComponent<Renderer>().sharedMaterial = angelic ? stoneMat : darkStoneMat;

        GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.name = name + "_Head";
        head.transform.SetParent(group.transform);
        head.transform.position = pos + new Vector3(0, 2.55f, 0);
        head.transform.localScale = new Vector3(0.85f, 0.85f, 0.85f);
        head.GetComponent<Renderer>().sharedMaterial = teamMat;

        CreateWing(name + "_Wing_L", pos + new Vector3(-1.45f, 1.15f, 0), true, angelic ? goldMat : darkStoneMat, group.transform);
        CreateWing(name + "_Wing_R", pos + new Vector3(1.45f, 1.15f, 0), false, angelic ? goldMat : darkStoneMat, group.transform);

        GameObject weapon = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        weapon.name = name + "_Spear";
        weapon.transform.SetParent(group.transform);
        weapon.transform.position = pos + new Vector3(1.0f, 0.35f, 0.2f);
        weapon.transform.rotation = Quaternion.Euler(20, 0, 8);
        weapon.transform.localScale = new Vector3(0.12f, 2.8f, 0.12f);
        weapon.GetComponent<Renderer>().sharedMaterial = goldMat;

        if (!angelic)
        {
            GameObject hornA = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            hornA.name = name + "_Horn_A";
            hornA.transform.SetParent(group.transform);
            hornA.transform.position = pos + new Vector3(-0.35f, 3.15f, 0);
            hornA.transform.rotation = Quaternion.Euler(25, 0, 25);
            hornA.transform.localScale = new Vector3(0.12f, 0.75f, 0.12f);
            hornA.GetComponent<Renderer>().sharedMaterial = teamMat;

            GameObject hornB = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            hornB.name = name + "_Horn_B";
            hornB.transform.SetParent(group.transform);
            hornB.transform.position = pos + new Vector3(0.35f, 3.15f, 0);
            hornB.transform.rotation = Quaternion.Euler(25, 0, -25);
            hornB.transform.localScale = new Vector3(0.12f, 0.75f, 0.12f);
            hornB.GetComponent<Renderer>().sharedMaterial = teamMat;
        }
    }

    void BuildJungleCampUpgrades()
    {
        GameObject group = NewGroup("03_Jungle_Camp_Final_Visuals");

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
            CreateFinalJungleCamp("Blue_Ghoul_Camp", p, blueMat, group.transform);
            CreateFinalJungleCamp("Red_Ghoul_Camp", Mirror(p), redMat, group.transform);
        }
    }

    void CreateFinalJungleCamp(string name, Vector3 center, Material teamMat, Transform parent)
    {
        GameObject group = NewChildGroup(name + "_" + Mathf.RoundToInt(center.x) + "_" + Mathf.RoundToInt(center.z), parent);

        CreateCylinder(name + "_Dark_Pit", center + new Vector3(0, 0.25f, 0), new Vector3(8, 0.25f, 8), darkStoneMat, group.transform);
        CreateCylinder(name + "_Inner_Rune", center + new Vector3(0, 0.5f, 0), new Vector3(5.5f, 0.08f, 5.5f), teamMat, group.transform);

        GameObject creature = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        creature.name = name + "_Creature_Placeholder";
        creature.transform.SetParent(group.transform);
        creature.transform.position = center + new Vector3(0, 2.2f, 0);
        creature.transform.localScale = new Vector3(1.7f, 2.2f, 1.7f);
        creature.GetComponent<Renderer>().sharedMaterial = teamMat;

        for (int i = 0; i < 7; i++)
        {
            float a = i * Mathf.PI * 2f / 7f;
            Vector3 p = center + new Vector3(Mathf.Cos(a) * 5.3f, 1.2f, Mathf.Sin(a) * 5.3f);
            CreateObelisk(name + "_Camp_Obelisk_" + i, p, darkStoneMat, group.transform);
        }
    }

    void BuildObjectiveUpgrades()
    {
        GameObject group = NewGroup("04_Objective_Final_Visuals");

        CreateFinalObjective("Void_Nephilim_Behemoth", new Vector3(-38, 0, 36), voidMat, group.transform);
        CreateFinalObjective("Celestial_Leviathan", new Vector3(38, 0, -36), crystalMat, group.transform);
    }

    void CreateFinalObjective(string name, Vector3 center, Material coreMat, Transform parent)
    {
        GameObject group = NewChildGroup(name, parent);

        CreateCylinder(name + "_Massive_Pit", center + new Vector3(0, 0.4f, 0), new Vector3(15, 0.35f, 15), darkStoneMat, group.transform);
        CreateCylinder(name + "_Energy_Circle", center + new Vector3(0, 0.8f, 0), new Vector3(10, 0.1f, 10), coreMat, group.transform);

        GameObject boss = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        boss.name = name + "_Boss_Shape";
        boss.transform.SetParent(group.transform);
        boss.transform.position = center + new Vector3(0, 4, 0);
        boss.transform.localScale = new Vector3(5.5f, 3.2f, 5.5f);
        boss.GetComponent<Renderer>().sharedMaterial = coreMat;

        for (int i = 0; i < 10; i++)
        {
            float a = i * Mathf.PI * 2f / 10f;
            Vector3 p = center + new Vector3(Mathf.Cos(a) * 10.5f, 1.8f, Mathf.Sin(a) * 10.5f);
            CreateSmallFlame(name + "_Ritual_Flame_" + i, p, coreMat, group.transform);
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

    void CreateWing(string name, Vector3 pos, bool left, Material mat, Transform parent)
    {
        GameObject wing = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wing.name = name;
        wing.transform.SetParent(parent);
        wing.transform.position = pos;
        wing.transform.localScale = new Vector3(0.22f, 2.3f, 1.2f);
        wing.transform.rotation = Quaternion.Euler(0, 0, left ? 35 : -35);
        wing.GetComponent<Renderer>().sharedMaterial = mat;
    }

    void CreateObelisk(string name, Vector3 pos, Material mat, Transform parent)
    {
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        obj.name = name;
        obj.transform.SetParent(parent);
        obj.transform.position = pos;
        obj.transform.localScale = new Vector3(0.45f, 1.5f, 0.45f);
        obj.GetComponent<Renderer>().sharedMaterial = mat;

        GameObject top = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        top.name = name + "_Top";
        top.transform.SetParent(parent);
        top.transform.position = pos + new Vector3(0, 1.8f, 0);
        top.transform.localScale = new Vector3(0.75f, 0.75f, 0.75f);
        top.GetComponent<Renderer>().sharedMaterial = mat;
    }

    void CreateSmallFlame(string name, Vector3 pos, Material mat, Transform parent)
    {
        GameObject stand = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        stand.name = name + "_Stand";
        stand.transform.SetParent(parent);
        stand.transform.position = pos;
        stand.transform.localScale = new Vector3(0.25f, 0.8f, 0.25f);
        stand.GetComponent<Renderer>().sharedMaterial = darkStoneMat;

        GameObject flame = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        flame.name = name + "_Glow";
        flame.transform.SetParent(parent);
        flame.transform.position = pos + new Vector3(0, 1.0f, 0);
        flame.transform.localScale = new Vector3(0.65f, 0.65f, 0.65f);
        flame.GetComponent<Renderer>().sharedMaterial = mat;
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