using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Clean competitive art-direction pass for the rebuild slice.
/// Removes known fallback clutter groups, applies deterministic palette via MaterialPropertyBlock,
/// and adds subtle lane energy guides without moving authored geometry.
/// </summary>
public class AOGMapArtDirectionRuntime : MonoBehaviour
{
    private static readonly Color RoadColor = new(0.16f, 0.13f, 0.11f, 1f);
    private static readonly Color GroundColor = new(0.055f, 0.13f, 0.07f, 1f);
    private static readonly Color StoneColor = new(0.20f, 0.22f, 0.25f, 1f);
    private static readonly Color BlueAccent = new(0.08f, 0.52f, 1f, 1f);
    private static readonly Color RedAccent = new(1f, 0.12f, 0.08f, 1f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Ensure();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Ensure();
        AOGMapArtDirectionRuntime runtime = FindObjectOfType<AOGMapArtDirectionRuntime>();
        if (runtime != null) runtime.Apply();
    }

    private static void Ensure()
    {
        if (FindObjectOfType<AOGMapArtDirectionRuntime>() != null) return;
        new GameObject("AOG_Map_Art_Direction_Runtime").AddComponent<AOGMapArtDirectionRuntime>();
    }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        Apply();
    }

    public void Apply()
    {
        HideLegacyFallbackClutter();
        RecolorScene();
        BuildLaneGuides();
    }

    private void HideLegacyFallbackClutter()
    {
        Transform[] all = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Transform t in all)
        {
            if (t == null || !t.gameObject.scene.IsValid()) continue;
            string n = t.name;
            if (n == "04_Forest_Rocks_Border" || n == "06_Atmosphere_Runes_Torches")
                t.gameObject.SetActive(false);
        }
    }

    private void RecolorScene()
    {
        MaterialPropertyBlock block = new();
        Renderer[] renderers = FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || !renderer.gameObject.scene.IsValid()) continue;
            string n = renderer.name.ToLowerInvariant();
            string parent = renderer.transform.parent != null ? renderer.transform.parent.name.ToLowerInvariant() : string.Empty;
            string key = n + " " + parent;

            Color color;
            if (key.Contains("road") || key.Contains("lane")) color = RoadColor;
            else if (key.Contains("terrain") || key.Contains("ground") || key.Contains("grass")) color = GroundColor;
            else if (key.Contains("blue_") || key.Contains("blue ")) color = BlueAccent;
            else if (key.Contains("red_") || key.Contains("red ")) color = RedAccent;
            else if (key.Contains("tower") || key.Contains("nexus") || key.Contains("stone")) color = StoneColor;
            else continue;

            renderer.GetPropertyBlock(block);
            block.SetColor("_BaseColor", color);
            block.SetColor("_Color", color);
            renderer.SetPropertyBlock(block);
        }
    }

    private void BuildLaneGuides()
    {
        Transform old = transform.Find("AOG_Rebuild_Lane_Guides");
        if (old != null) Destroy(old.gameObject);

        GameObject root = new("AOG_Rebuild_Lane_Guides");
        root.transform.SetParent(transform, false);

        CreateGuide("Mid", GetMidLane(), new Color(0.12f, 0.58f, 1f, 0.26f), root.transform);
        CreateGuide("Top", GetTopLane(), new Color(0.18f, 0.42f, 0.88f, 0.20f), root.transform);
        CreateGuide("Bot", GetBotLane(), new Color(0.18f, 0.42f, 0.88f, 0.20f), root.transform);
    }

    private static void CreateGuide(string name, Vector3[] points, Color color, Transform parent)
    {
        GameObject obj = new(name + "_Lane_Guide");
        obj.transform.SetParent(parent, false);
        LineRenderer lr = obj.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.loop = false;
        lr.positionCount = points.Length;
        lr.widthMultiplier = 0.10f;
        lr.numCornerVertices = 4;
        lr.material = CreateGuideMaterial(color);
        for (int i = 0; i < points.Length; i++)
            lr.SetPosition(i, points[i] + Vector3.up * 0.32f);
    }

    private static Material CreateGuideMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
        Material material = new(shader) { color = color };
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * 1.6f);
        }
        return material;
    }

    private static Vector3[] GetMidLane() => new[]
    {
        new Vector3(-105,0,-78), new Vector3(-60,0,-44), new Vector3(-25,0,-18), new Vector3(0,0,0),
        new Vector3(25,0,18), new Vector3(60,0,44), new Vector3(105,0,78)
    };

    private static Vector3[] GetTopLane() => new[]
    {
        new Vector3(-105,0,-78), new Vector3(-118,0,-52), new Vector3(-112,0,10), new Vector3(-92,0,50),
        new Vector3(-48,0,88), new Vector3(10,0,96), new Vector3(58,0,108), new Vector3(105,0,78)
    };

    private static Vector3[] GetBotLane() => new[]
    {
        new Vector3(-105,0,-78), new Vector3(-58,0,-108), new Vector3(-10,0,-96), new Vector3(48,0,-88),
        new Vector3(92,0,-50), new Vector3(112,0,10), new Vector3(118,0,52), new Vector3(105,0,78)
    };
}
