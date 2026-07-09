using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class AOGRoadDetailUpgrade : MonoBehaviour
{
    Transform root;
    Material darkEdgeMat;
    Material stoneMat;
    Material goldMat;
    Material crackMat;

    [Header("Detail Settings")]
    public int stonesPerLaneSegment = 8;
    public int cracksPerLaneSegment = 5;
    public float sideOffset = 7.0f;

    void Awake()
    {
        FreezeExistingRoadDecor();
    }

    void Start()
    {
        FreezeExistingRoadDecor();
    }

    private void FreezeExistingRoadDecor()
    {
        Transform existing = transform.Find("AOG_Road_Detail_Upgrade");
        if (existing == null) return;

        foreach (Collider col in existing.GetComponentsInChildren<Collider>(true))
        {
            if (col != null) Destroy(col);
        }

        foreach (Transform child in existing.GetComponentsInChildren<Transform>(true))
        {
            if (child != null) child.gameObject.isStatic = true;
        }
    }

    [ContextMenu("Build Road Detail Upgrade")]
    public void Build()
    {
        Clear();
        GameObject rootObj = new GameObject("AOG_Road_Detail_Upgrade");
        root = rootObj.transform;
        root.SetParent(transform);
        CreateMaterials();
        BuildLaneDetails("Mid", GetMidLane());
        BuildLaneDetails("Top", GetTopLane());
        BuildLaneDetails("Bot", GetBotLane());
        FreezeExistingRoadDecor();
        Debug.Log("AOG road detail upgrade oluşturuldu ve fizik dışı dekor olarak kilitlendi.");
    }

    [ContextMenu("Clear Road Detail Upgrade")]
    public void Clear()
    {
        Transform old = transform.Find("AOG_Road_Detail_Upgrade");
        if (old == null) return;
#if UNITY_EDITOR
        if (!Application.isPlaying) DestroyImmediate(old.gameObject);
        else Destroy(old.gameObject);
#else
        Destroy(old.gameObject);
#endif
    }

    void CreateMaterials()
    {
        darkEdgeMat = MakeMat("AOG_RDU_Dark_Road_Edge", new Color32(24, 22, 20, 255));
        stoneMat = MakeMat("AOG_RDU_Broken_Stone", new Color32(96, 86, 72, 255));
        goldMat = MakeMat("AOG_RDU_Faint_Gold_Rune", new Color32(190, 135, 42, 255));
        crackMat = MakeMat("AOG_RDU_Dark_Crack", new Color32(12, 10, 9, 255));
    }

    Material MakeMat(string name, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material mat = new Material(shader) { name = name, color = color };
        return mat;
    }

    void BuildLaneDetails(string laneName, Vector3[] points)
    {
        GameObject group = new GameObject(laneName + "_Road_Details");
        group.transform.SetParent(root);
        for (int i = 0; i < points.Length - 1; i++)
        {
            Vector3 a = points[i];
            Vector3 b = points[i + 1];
            CreateEdgeStones(laneName, i, a, b, group.transform);
            CreateBrokenRoadStones(laneName, i, a, b, group.transform);
            CreateCracks(laneName, i, a, b, group.transform);
            CreateSmallRunes(laneName, i, a, b, group.transform);
        }
    }

    void DecoratePrimitive(GameObject obj, Material material)
    {
        Collider col = obj.GetComponent<Collider>();
        if (col != null)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) DestroyImmediate(col);
            else Destroy(col);
#else
            Destroy(col);
#endif
        }
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null) renderer.sharedMaterial = material;
        obj.isStatic = true;
    }

    void CreateEdgeStones(string laneName, int segmentIndex, Vector3 a, Vector3 b, Transform parent)
    {
        Vector3 dir = (b - a).normalized;
        Vector3 side = new Vector3(-dir.z, 0, dir.x);
        float length = Vector3.Distance(a, b);
        int count = Mathf.Max(2, Mathf.RoundToInt(length / 8f));
        for (int i = 0; i <= count; i++)
        {
            float t = (float)i / count;
            Vector3 center = Vector3.Lerp(a, b, t);
            CreateSmallEdgeBlock(laneName + "_Edge_L_" + segmentIndex + "_" + i, center + side * sideOffset, Random.Range(0.8f, 1.8f), parent);
            CreateSmallEdgeBlock(laneName + "_Edge_R_" + segmentIndex + "_" + i, center - side * sideOffset, Random.Range(0.8f, 1.8f), parent);
        }
    }

    void CreateSmallEdgeBlock(string name, Vector3 pos, float scale, Transform parent)
    {
        GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        block.name = name;
        block.transform.SetParent(parent);
        block.transform.position = pos + new Vector3(0, 0.18f, 0);
        block.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
        block.transform.localScale = new Vector3(scale, 0.18f, scale * Random.Range(0.6f, 1.4f));
        DecoratePrimitive(block, darkEdgeMat);
    }

    void CreateBrokenRoadStones(string laneName, int segmentIndex, Vector3 a, Vector3 b, Transform parent)
    {
        Vector3 dir = (b - a).normalized;
        Vector3 side = new Vector3(-dir.z, 0, dir.x);
        for (int i = 0; i < stonesPerLaneSegment; i++)
        {
            float t = Random.Range(0.08f, 0.92f);
            Vector3 center = Vector3.Lerp(a, b, t);
            Vector3 offset = side * Random.Range(-4.5f, 4.5f);
            GameObject stone = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stone.name = laneName + "_Broken_Stone_" + segmentIndex + "_" + i;
            stone.transform.SetParent(parent);
            stone.transform.position = center + offset + new Vector3(0, 0.22f, 0);
            stone.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
            stone.transform.localScale = new Vector3(Random.Range(0.8f, 2.4f), 0.12f, Random.Range(0.5f, 1.5f));
            DecoratePrimitive(stone, stoneMat);
        }
    }

    void CreateCracks(string laneName, int segmentIndex, Vector3 a, Vector3 b, Transform parent)
    {
        Vector3 dir = (b - a).normalized;
        Vector3 side = new Vector3(-dir.z, 0, dir.x);
        for (int i = 0; i < cracksPerLaneSegment; i++)
        {
            float t = Random.Range(0.12f, 0.88f);
            Vector3 center = Vector3.Lerp(a, b, t);
            Vector3 offset = side * Random.Range(-3.8f, 3.8f);
            GameObject crack = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crack.name = laneName + "_Dark_Crack_" + segmentIndex + "_" + i;
            crack.transform.SetParent(parent);
            crack.transform.position = center + offset + new Vector3(0, 0.245f, 0);
            crack.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
            crack.transform.localScale = new Vector3(Random.Range(1.2f, 3.2f), 0.04f, Random.Range(0.12f, 0.25f));
            DecoratePrimitive(crack, crackMat);
        }
    }

    void CreateSmallRunes(string laneName, int segmentIndex, Vector3 a, Vector3 b, Transform parent)
    {
        if (segmentIndex % 2 != 0) return;
        Vector3 pos = Vector3.Lerp(a, b, 0.5f);
        GameObject rune = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        rune.name = laneName + "_Small_Rune_" + segmentIndex;
        rune.transform.SetParent(parent);
        rune.transform.position = pos + new Vector3(0, 0.27f, 0);
        rune.transform.localScale = new Vector3(1.2f, 0.035f, 1.2f);
        DecoratePrimitive(rune, goldMat);
    }

    Vector3[] GetMidLane() => new[]
    {
        new Vector3(-105,0,-78), new Vector3(-60,0,-44), new Vector3(-25,0,-18), new Vector3(0,0,0),
        new Vector3(25,0,18), new Vector3(60,0,44), new Vector3(105,0,78)
    };

    Vector3[] GetTopLane() => new[]
    {
        new Vector3(-105,0,-78), new Vector3(-118,0,-52), new Vector3(-112,0,10), new Vector3(-92,0,50),
        new Vector3(-48,0,88), new Vector3(10,0,96), new Vector3(58,0,108), new Vector3(105,0,78)
    };

    Vector3[] GetBotLane() => new[]
    {
        new Vector3(-105,0,-78), new Vector3(-58,0,-108), new Vector3(-10,0,-96), new Vector3(48,0,-88),
        new Vector3(92,0,-50), new Vector3(112,0,10), new Vector3(118,0,52), new Vector3(105,0,78)
    };
}
