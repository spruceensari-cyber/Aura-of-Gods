using UnityEngine;

/// <summary>
/// Extends the current symmetric map without replacing its lane/base authority.
/// Repositions the existing outer walls, enlarges the ground footprint, disables the old
/// forest border that would become an internal blocker, and builds a dedicated late-game
/// Void Titan sanctuary in the expanded south objective pocket.
/// </summary>
[DefaultExecutionOrder(-620)]
public class AOGExpandedObjectiveMapRuntime : MonoBehaviour
{
    private const float ExpandedHalfWidth = 170f;
    private const float ExpandedHalfDepth = 156f;
    private static readonly Vector3 TitanSanctuaryPosition = new Vector3(0f, 0.2f, -132f);

    private bool applied;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindFirstObjectByType<AOGExpandedObjectiveMapRuntime>() != null)
            return;

        GameObject host = new GameObject("AOG_Expanded_Objective_Map_Runtime");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGExpandedObjectiveMapRuntime>();
    }

    private void Update()
    {
        if (applied)
            return;

        GameObject mapRoot = GameObject.Find("AOG_Symmetric_Reference_Map");
        if (mapRoot == null)
            return;

        ApplyExpansion(mapRoot.transform);
        applied = true;
    }

    private void ApplyExpansion(Transform mapRoot)
    {
        Transform ground = FindDeepChild(mapRoot, "Main_Dark_Ground");
        if (ground != null)
            ground.localScale = new Vector3(36f, 1f, 32f);

        Transform north = FindDeepChild(mapRoot, "North_Wall");
        Transform south = FindDeepChild(mapRoot, "South_Wall");
        Transform west = FindDeepChild(mapRoot, "West_Wall");
        Transform east = FindDeepChild(mapRoot, "East_Wall");

        ResizeHorizontalWall(north, ExpandedHalfDepth, 340f);
        ResizeHorizontalWall(south, -ExpandedHalfDepth, 340f);
        ResizeVerticalWall(west, -ExpandedHalfWidth, 318f);
        ResizeVerticalWall(east, ExpandedHalfWidth, 318f);

        Transform legacyForest = FindDeepChild(mapRoot, "08_Dark_Forest_Border");
        if (legacyForest != null)
            legacyForest.gameObject.SetActive(false);

        Transform existing = mapRoot.Find("10_Expanded_Objective_Pocket");
        if (existing != null)
            Destroy(existing.gameObject);

        GameObject expansion = new GameObject("10_Expanded_Objective_Pocket");
        expansion.transform.SetParent(mapRoot, false);

        BuildTitanSanctuary(expansion.transform);
        BuildBoundaryLandmarks(expansion.transform);
        BuildApproachRoad(expansion.transform);
    }

    private static void ResizeHorizontalWall(Transform wall, float z, float width)
    {
        if (wall == null) return;
        Vector3 p = wall.position;
        p.z = z;
        wall.position = p;
        Vector3 s = wall.localScale;
        s.x = width;
        wall.localScale = s;
    }

    private static void ResizeVerticalWall(Transform wall, float x, float depth)
    {
        if (wall == null) return;
        Vector3 p = wall.position;
        p.x = x;
        wall.position = p;
        Vector3 s = wall.localScale;
        s.z = depth;
        wall.localScale = s;
    }

    private void BuildTitanSanctuary(Transform parent)
    {
        GameObject root = new GameObject("Void_Titan_Sanctuary");
        root.transform.SetParent(parent, false);
        root.transform.position = TitanSanctuaryPosition;

        Material dark = CreateLit(new Color(0.035f, 0.028f, 0.060f), 0.32f, 0.22f);
        Material stone = CreateLit(new Color(0.11f, 0.10f, 0.15f), 0.24f, 0.30f);
        Material voidEnergy = CreateEmission(new Color(0.42f, 0.12f, 0.88f), 4.8f);

        CreateCylinder("Titan_Outer_Basin", root.transform, new Vector3(0f, 0.12f, 0f), new Vector3(20f, 0.22f, 20f), dark);
        CreateCylinder("Titan_Inner_Basin", root.transform, new Vector3(0f, 0.28f, 0f), new Vector3(12f, 0.16f, 12f), stone);
        CreateCylinder("Titan_Energy_Core", root.transform, new Vector3(0f, 0.46f, 0f), new Vector3(7.5f, 0.08f, 7.5f), voidEnergy);

        for (int i = 0; i < 8; i++)
        {
            float angle = i * Mathf.PI * 2f / 8f;
            Vector3 p = new Vector3(Mathf.Cos(angle) * 15f, 2.3f, Mathf.Sin(angle) * 15f);
            GameObject obelisk = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obelisk.name = "Titan_Obelisk_" + i;
            obelisk.transform.SetParent(root.transform, false);
            obelisk.transform.localPosition = p;
            obelisk.transform.localRotation = Quaternion.Euler(0f, -angle * Mathf.Rad2Deg, 12f);
            obelisk.transform.localScale = new Vector3(0.8f, 4.8f, 1.1f);
            obelisk.GetComponent<Renderer>().sharedMaterial = i % 2 == 0 ? stone : dark;
            Collider c = obelisk.GetComponent<Collider>();
            if (c != null) Destroy(c);
        }

        GameObject spawn = new GameObject("Void_Titan_Sanctuary_Spawn");
        spawn.transform.SetParent(root.transform, false);
        spawn.transform.localPosition = new Vector3(0f, 0.2f, 0f);

        BuildRing(root.transform, 18f, 0.12f, voidEnergy, "Titan_Sanctuary_Ring");
        BuildRing(root.transform, 10f, 0.07f, voidEnergy, "Titan_Core_Ring");
    }

    private void BuildBoundaryLandmarks(Transform parent)
    {
        Material dark = CreateLit(new Color(0.025f,0.040f,0.035f),0.18f,0.05f);
        Material trunk = CreateLit(new Color(0.12f,0.075f,0.04f),0.12f,0.0f);

        Random.State state = Random.state;
        Random.InitState(9921);

        for (int i = 0; i < 120; i++)
        {
            bool horizontal = Random.value > 0.5f;
            float x;
            float z;
            if (horizontal)
            {
                x = Random.Range(-160f,160f);
                z = Random.value > 0.5f ? Random.Range(143f,152f) : Random.Range(-152f,-143f);
            }
            else
            {
                x = Random.value > 0.5f ? Random.Range(158f,166f) : Random.Range(-166f,-158f);
                z = Random.Range(-145f,145f);
            }

            GameObject tree = new GameObject("Expanded_Border_Tree_" + i);
            tree.transform.SetParent(parent,false);
            tree.transform.position = new Vector3(x,0f,z);

            GameObject trunkObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunkObj.transform.SetParent(tree.transform,false);
            trunkObj.transform.localPosition = new Vector3(0f,2.2f,0f);
            trunkObj.transform.localScale = new Vector3(0.55f,2.2f,0.55f);
            trunkObj.GetComponent<Renderer>().sharedMaterial = trunk;
            Collider tc = trunkObj.GetComponent<Collider>(); if (tc != null) Destroy(tc);

            GameObject crown = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            crown.transform.SetParent(tree.transform,false);
            crown.transform.localPosition = new Vector3(0f,5.1f,0f);
            crown.transform.localScale = new Vector3(3.0f,4.5f,3.0f) * Random.Range(0.8f,1.25f);
            crown.GetComponent<Renderer>().sharedMaterial = dark;
            Collider cc = crown.GetComponent<Collider>(); if (cc != null) Destroy(cc);
        }

        Random.state = state;
    }

    private void BuildApproachRoad(Transform parent)
    {
        Material road = CreateLit(new Color(0.22f,0.19f,0.18f),0.18f,0.12f);
        Vector3[] points =
        {
            new Vector3(38f,0.06f,-36f),
            new Vector3(32f,0.06f,-62f),
            new Vector3(22f,0.06f,-88f),
            new Vector3(10f,0.06f,-110f),
            TitanSanctuaryPosition
        };

        for (int i = 0; i < points.Length - 1; i++)
        {
            Vector3 a = points[i];
            Vector3 b = points[i+1];
            Vector3 delta = b-a;
            float length = delta.magnitude;
            GameObject segment = GameObject.CreatePrimitive(PrimitiveType.Cube);
            segment.name = "Titan_Approach_" + i;
            segment.transform.SetParent(parent,false);
            segment.transform.position = (a+b)*0.5f;
            segment.transform.rotation = Quaternion.LookRotation(delta.normalized);
            segment.transform.localScale = new Vector3(7.5f,0.12f,length);
            segment.GetComponent<Renderer>().sharedMaterial = road;
            Collider c = segment.GetComponent<Collider>(); if (c != null) Destroy(c);
        }
    }

    private static Transform FindDeepChild(Transform root, string name)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            if (child.name == name) return child;
        return null;
    }

    private static void CreateCylinder(string name, Transform parent, Vector3 pos, Vector3 scale, Material material)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = name;
        go.transform.SetParent(parent,false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = material;
        Collider c = go.GetComponent<Collider>(); if (c != null) Destroy(c);
    }

    private static void BuildRing(Transform parent, float radius, float width, Material material, string name)
    {
        GameObject ringObject = new GameObject(name);
        ringObject.transform.SetParent(parent,false);
        ringObject.transform.localPosition = new Vector3(0f,0.55f,0f);
        LineRenderer line = ringObject.AddComponent<LineRenderer>();
        line.loop = true;
        line.useWorldSpace = false;
        line.positionCount = 72;
        line.startWidth = width;
        line.endWidth = width;
        line.sharedMaterial = material;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        for (int i=0;i<line.positionCount;i++)
        {
            float a = i*Mathf.PI*2f/line.positionCount;
            line.SetPosition(i,new Vector3(Mathf.Cos(a)*radius,0f,Mathf.Sin(a)*radius));
        }
    }

    private static Material CreateLit(Color color,float smoothness,float metallic)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        Material material = new Material(shader) { color=color };
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor",color);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness",smoothness);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic",metallic);
        return material;
    }

    private static Material CreateEmission(Color color,float strength)
    {
        Material material = CreateLit(color,0.42f,0.18f);
        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor",color*strength);
        }
        return material;
    }
}

/// <summary>
/// Moves the late-game Void Titan into the expanded sanctuary once it is spawned.
/// This avoids coupling the boss spawner to a hard-coded map coordinate and preserves
/// the boss/objective PR as an independent system.
/// </summary>
public class AOGVoidTitanSanctuaryRelocatorRuntime : MonoBehaviour
{
    private AOGVoidTitanMarker relocatedTitan;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindFirstObjectByType<AOGVoidTitanSanctuaryRelocatorRuntime>() != null)
            return;
        GameObject host = new GameObject("AOG_Void_Titan_Sanctuary_Relocator");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGVoidTitanSanctuaryRelocatorRuntime>();
    }

    private void Update()
    {
        if (relocatedTitan != null)
            return;

        AOGVoidTitanMarker titan = FindFirstObjectByType<AOGVoidTitanMarker>();
        GameObject spawn = GameObject.Find("Void_Titan_Sanctuary_Spawn");
        if (titan == null || spawn == null)
            return;

        titan.transform.position = spawn.transform.position;
        titan.transform.rotation = spawn.transform.rotation;
        relocatedTitan = titan;
    }
}
