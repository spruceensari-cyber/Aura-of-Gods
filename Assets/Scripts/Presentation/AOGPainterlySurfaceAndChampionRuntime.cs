using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(12000)]
public class AOGPainterlySurfaceAndChampionRuntime : MonoBehaviour
{
    private static AOGPainterlySurfaceAndChampionRuntime instance;
    private Texture2D grassAlbedo;
    private Texture2D grassNormal;
    private Texture2D laneAlbedo;
    private Texture2D laneNormal;
    private readonly Dictionary<Material, Material> cache = new Dictionary<Material, Material>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        EnsureInstance();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureInstance();
        instance?.StartCoroutine(instance.ReapplyForStartupWindow());
    }

    private static void EnsureInstance()
    {
        if (instance != null)
            return;

        AOGPainterlySurfaceAndChampionRuntime existing = Object.FindFirstObjectByType<AOGPainterlySurfaceAndChampionRuntime>();
        if (existing != null)
        {
            instance = existing;
            return;
        }

        GameObject host = new GameObject("AOG_Painterly_Surface_And_Champion");
        instance = host.AddComponent<AOGPainterlySurfaceAndChampionRuntime>();
        Object.DontDestroyOnLoad(host);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        BuildTextures();
    }

    private void Start()
    {
        StartCoroutine(ReapplyForStartupWindow());
    }

    private IEnumerator ReapplyForStartupWindow()
    {
        for (int i = 0; i < 16; i++)
        {
            ApplySurfaces();
            PolishLyraReadability();
            yield return new WaitForSecondsRealtime(0.22f);
        }
    }

    private void BuildTextures()
    {
        grassAlbedo = BuildPainterlyAlbedo(
            "AOG_Painterly_Grass",
            new Color(0.075f, 0.19f, 0.085f),
            new Color(0.19f, 0.34f, 0.12f),
            3.0f,
            0.12f,
            out grassNormal);

        laneAlbedo = BuildPainterlyAlbedo(
            "AOG_Painterly_Lane",
            new Color(0.19f, 0.14f, 0.10f),
            new Color(0.40f, 0.30f, 0.20f),
            4.0f,
            0.16f,
            out laneNormal);
    }

    private static Texture2D BuildPainterlyAlbedo(string name, Color dark, Color light, float frequency, float microAmount, out Texture2D normal)
    {
        const int size = 256;
        Texture2D albedo = new Texture2D(size, size, TextureFormat.RGBA32, true)
        {
            name = name,
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Trilinear,
            anisoLevel = 16
        };

        float[,] heights = new float[size, size];
        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = x / (float)size;
                float v = y / (float)size;

                float broadA = Mathf.PerlinNoise(u * frequency + 9.7f, v * frequency + 2.3f);
                float broadB = Mathf.PerlinNoise(u * frequency * 0.52f + 31.2f, v * frequency * 0.52f + 17.8f);
                float brush = Mathf.PerlinNoise((u + v * 0.28f) * frequency * 2.2f + 5.1f, v * frequency * 0.65f + 12.6f);
                float micro = Mathf.PerlinNoise(u * frequency * 7f + 21.4f, v * frequency * 7f + 41.9f);

                float value = Mathf.Clamp01(broadA * 0.56f + broadB * 0.30f + brush * 0.14f);
                value = Mathf.Lerp(value, micro, microAmount);
                value = Mathf.SmoothStep(0.16f, 0.86f, value);
                heights[x, y] = value;

                Color c = Color.Lerp(dark, light, value);
                float brushStroke = Mathf.Sin((u * 23f + v * 7f) * Mathf.PI) * 0.012f;
                c += new Color(brushStroke, brushStroke, brushStroke, 0f);
                pixels[y * size + x] = c;
            }
        }

        albedo.SetPixels(pixels);
        albedo.Apply(true, false);

        normal = new Texture2D(size, size, TextureFormat.RGBA32, true)
        {
            name = name + "_Normal",
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Trilinear,
            anisoLevel = 16
        };

        Color[] normalPixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float left = heights[(x - 1 + size) % size, y];
                float right = heights[(x + 1) % size, y];
                float down = heights[x, (y - 1 + size) % size];
                float up = heights[x, (y + 1) % size];
                Vector3 n = new Vector3((left - right) * 0.75f, (down - up) * 0.75f, 1f).normalized;
                normalPixels[y * size + x] = new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z, 1f);
            }
        }

        normal.SetPixels(normalPixels);
        normal.Apply(true, false);
        return albedo;
    }

    private void ApplySurfaces()
    {
        Renderer[] renderers = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || renderer is SkinnedMeshRenderer)
                continue;

            Material source = renderer.sharedMaterial;
            if (source == null)
                continue;

            string objectName = renderer.gameObject.name.ToLowerInvariant();
            string materialName = source.name.ToLowerInvariant();
            bool grass = IsGrass(objectName, materialName);
            bool lane = IsLane(objectName, materialName);
            if (!grass && !lane)
                continue;

            if (source.name.Contains("_PainterlyFinal"))
                continue;

            if (!cache.TryGetValue(source, out Material final))
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                    shader = source.shader;

                final = new Material(shader)
                {
                    name = source.name + "_PainterlyFinal",
                    enableInstancing = true
                };

                Texture2D albedo = grass ? grassAlbedo : laneAlbedo;
                Texture2D normal = grass ? grassNormal : laneNormal;

                if (final.HasProperty("_BaseMap"))
                {
                    final.SetTexture("_BaseMap", albedo);
                    final.SetTextureScale("_BaseMap", grass ? new Vector2(2.8f, 2.8f) : new Vector2(2.1f, 2.1f));
                }

                if (final.HasProperty("_BaseColor"))
                    final.SetColor("_BaseColor", Color.white);
                final.color = Color.white;

                if (final.HasProperty("_BumpMap"))
                {
                    final.SetTexture("_BumpMap", normal);
                    final.SetFloat("_BumpScale", grass ? 0.42f : 0.56f);
                    final.EnableKeyword("_NORMALMAP");
                }

                if (final.HasProperty("_Smoothness"))
                    final.SetFloat("_Smoothness", grass ? 0.045f : 0.11f);
                if (final.HasProperty("_Metallic"))
                    final.SetFloat("_Metallic", 0f);

                cache[source] = final;
            }

            renderer.sharedMaterial = final;
            renderer.receiveShadows = true;
        }
    }

    private static bool IsGrass(string objectName, string materialName)
    {
        return objectName.Contains("grass") || objectName.Contains("ground") || objectName.Contains("terrain") ||
               materialName.Contains("grass") || materialName.Contains("ground");
    }

    private static bool IsLane(string objectName, string materialName)
    {
        return objectName.Contains("road") || objectName.Contains("lane_dirt") || objectName.Contains("lane_centered") ||
               objectName.Contains("lane_outer") || materialName.Contains("road") || materialName.Contains("lane_dirt");
    }

    private static void PolishLyraReadability()
    {
        AOGPlayerMOBAController[] players = Object.FindObjectsByType<AOGPlayerMOBAController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (AOGPlayerMOBAController player in players)
        {
            if (player == null || !player.gameObject.name.ToLowerInvariant().Contains("lyra"))
                continue;

            if (player.GetComponent<AOGLyraVisualPolishMarker>() == null)
            {
                player.gameObject.AddComponent<AOGLyraVisualPolishMarker>();

                Animator animator = player.GetComponentInChildren<Animator>(true);
                if (animator != null && animator.transform != player.transform)
                    animator.transform.localScale *= 1.12f;
            }

            Transform lightTransform = player.transform.Find("AOG_Lyra_Rim_Light");
            if (lightTransform == null)
            {
                GameObject lightObject = new GameObject("AOG_Lyra_Rim_Light");
                lightObject.transform.SetParent(player.transform);
                lightObject.transform.localPosition = new Vector3(-0.8f, 2.4f, -0.4f);
                Light rim = lightObject.AddComponent<Light>();
                rim.type = LightType.Point;
                rim.color = new Color(0.45f, 0.32f, 1f);
                rim.intensity = 0.75f;
                rim.range = 4.8f;
                rim.shadows = LightShadows.None;
            }
        }
    }
}

public class AOGLyraVisualPolishMarker : MonoBehaviour
{
}
