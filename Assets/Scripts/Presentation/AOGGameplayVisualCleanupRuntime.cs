using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(10000)]
public class AOGGameplayVisualCleanupRuntime : MonoBehaviour
{
    private static AOGGameplayVisualCleanupRuntime instance;
    private readonly Dictionary<Material, Material> grassMaterialCache = new Dictionary<Material, Material>();
    private readonly Dictionary<Material, Material> roadMaterialCache = new Dictionary<Material, Material>();
    private readonly Dictionary<Material, Material> characterMaterialCache = new Dictionary<Material, Material>();
    private Texture2D grassTexture;
    private Texture2D roadTexture;

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
        if (instance != null)
            instance.StartCoroutine(instance.DelayedCleanup());
    }

    private static void EnsureInstance()
    {
        if (instance != null)
            return;

        AOGGameplayVisualCleanupRuntime existing = Object.FindFirstObjectByType<AOGGameplayVisualCleanupRuntime>();
        if (existing != null)
        {
            instance = existing;
            return;
        }

        GameObject host = new GameObject("AOG_Gameplay_Visual_Cleanup");
        instance = host.AddComponent<AOGGameplayVisualCleanupRuntime>();
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
        BuildSurfaceTextures();
        ApplyQualitySettings();
    }

    private void Start()
    {
        StartCoroutine(DelayedCleanup());
    }

    private IEnumerator DelayedCleanup()
    {
        for (int i = 0; i < 12; i++)
        {
            CleanupGeneratedPrototypePresentation();
            ApplySurfaceMaterialPolish();
            ApplyCharacterPolish();
            ApplyLightingPolish();
            yield return new WaitForSecondsRealtime(0.15f);
        }
    }

    private void CleanupGeneratedPrototypePresentation()
    {
        AOGPremiumMobaLookRuntime[] premiumManagers = Object.FindObjectsByType<AOGPremiumMobaLookRuntime>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (AOGPremiumMobaLookRuntime manager in premiumManagers)
        {
            if (manager == null)
                continue;

            manager.enabled = false;
            Transform generatedArt = manager.transform.Find("AOG_Premium_Moba_Art");
            if (generatedArt != null)
                Destroy(generatedArt.gameObject);
        }

        AOGPremiumUnitAnimator[] legacyUnitPresentation = Object.FindObjectsByType<AOGPremiumUnitAnimator>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (AOGPremiumUnitAnimator presentation in legacyUnitPresentation)
        {
            if (presentation == null)
                continue;

            presentation.enabled = false;
            RemoveChildByName(presentation.transform, "AOG_Premium_Readability_Ring");
            RemoveChildByName(presentation.transform, "AOG_Premium_Ground_Shadow");
        }

        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (GameObject obj in allObjects)
        {
            if (obj == null)
                continue;

            string lower = obj.name.ToLowerInvariant();
            if (lower.Contains("premium_readability_ring") ||
                lower == "aog_premium_ground_shadow" ||
                lower.Contains("readability_ring"))
            {
                Destroy(obj);
            }
        }
    }

    private static void RemoveChildByName(Transform root, string childName)
    {
        if (root == null)
            return;

        Transform child = root.Find(childName);
        if (child != null)
            Destroy(child.gameObject);
    }

    private void BuildSurfaceTextures()
    {
        grassTexture = BuildNoiseTexture(
            "AOG_Grass_Detail_Runtime",
            new Color(0.075f, 0.16f, 0.075f),
            new Color(0.24f, 0.40f, 0.16f),
            4.5f,
            0.32f);

        roadTexture = BuildNoiseTexture(
            "AOG_Road_Detail_Runtime",
            new Color(0.18f, 0.135f, 0.10f),
            new Color(0.46f, 0.34f, 0.22f),
            7.5f,
            0.26f);
    }

    private static Texture2D BuildNoiseTexture(string name, Color dark, Color light, float frequency, float microStrength)
    {
        const int size = 96;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, true)
        {
            name = name,
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Trilinear,
            anisoLevel = 8
        };

        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = x / (float)size;
                float v = y / (float)size;
                float broad = Mathf.PerlinNoise(u * frequency + 11.3f, v * frequency + 7.1f);
                float micro = Mathf.PerlinNoise(u * frequency * 5.5f + 31.7f, v * frequency * 5.5f + 19.9f);
                float value = Mathf.Clamp01(broad * (1f - microStrength) + micro * microStrength);
                Color color = Color.Lerp(dark, light, value);

                float grain = ((x * 17 + y * 31) % 23) / 23f;
                color *= Mathf.Lerp(0.94f, 1.06f, grain);
                pixels[y * size + x] = color;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply(true, false);
        return texture;
    }

    private void ApplySurfaceMaterialPolish()
    {
        Renderer[] renderers = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || renderer is SkinnedMeshRenderer)
                continue;

            string objectName = renderer.gameObject.name.ToLowerInvariant();
            if (objectName.Contains("shadow") || objectName.Contains("indicator") || objectName.Contains("ring"))
                continue;

            Material source = renderer.sharedMaterial;
            if (source == null || source.name.Contains("_GameplayPolished"))
                continue;

            bool grassLike = IsGrassLike(objectName, source);
            bool roadLike = IsRoadLike(objectName, source);
            if (!grassLike && !roadLike)
                continue;

            Dictionary<Material, Material> cache = grassLike ? grassMaterialCache : roadMaterialCache;
            if (!cache.TryGetValue(source, out Material polished))
            {
                polished = CreatePolishedSurfaceMaterial(source, grassLike);
                cache[source] = polished;
            }

            renderer.sharedMaterial = polished;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            renderer.receiveShadows = true;
        }
    }

    private Material CreatePolishedSurfaceMaterial(Material source, bool grassLike)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = source.shader;

        Material material = new Material(shader)
        {
            name = source.name + "_GameplayPolished"
        };

        Texture2D detail = grassLike ? grassTexture : roadTexture;
        if (material.HasProperty("_BaseMap"))
        {
            material.SetTexture("_BaseMap", detail);
            material.SetTextureScale("_BaseMap", grassLike ? new Vector2(9f, 9f) : new Vector2(5f, 5f));
        }

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", Color.white);

        material.color = Color.white;

        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", grassLike ? 0.06f : 0.13f);

        if (material.HasProperty("_Metallic"))
            material.SetFloat("_Metallic", 0f);

        if (material.HasProperty("_OcclusionStrength"))
            material.SetFloat("_OcclusionStrength", 1f);

        material.enableInstancing = true;
        return material;
    }

    private void ApplyCharacterPolish()
    {
        AOGPlayerMOBAController[] players = Object.FindObjectsByType<AOGPlayerMOBAController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (AOGPlayerMOBAController player in players)
        {
            if (player == null || !player.gameObject.name.ToLowerInvariant().Contains("lyra"))
                continue;

            Renderer[] renderers = player.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null)
                    continue;

                string lowerName = renderer.gameObject.name.ToLowerInvariant();
                if (lowerName.Contains("ring") || lowerName.Contains("shadow") || lowerName.Contains("hp_bar"))
                    continue;

                renderer.shadowCastingMode = ShadowCastingMode.On;
                renderer.receiveShadows = true;
                renderer.lightProbeUsage = LightProbeUsage.BlendProbes;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.BlendProbes;

                Material[] materials = renderer.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < materials.Length; i++)
                {
                    Material source = materials[i];
                    if (source == null || source.name.Contains("_CharacterPolished"))
                        continue;

                    if (!characterMaterialCache.TryGetValue(source, out Material polished))
                    {
                        polished = new Material(source)
                        {
                            name = source.name + "_CharacterPolished",
                            enableInstancing = true
                        };

                        string materialName = source.name.ToLowerInvariant();
                        bool metallicSurface = materialName.Contains("metal") || materialName.Contains("armor") || materialName.Contains("blade");

                        if (polished.HasProperty("_Smoothness"))
                            polished.SetFloat("_Smoothness", metallicSurface ? 0.58f : 0.30f);

                        if (polished.HasProperty("_Metallic"))
                            polished.SetFloat("_Metallic", metallicSurface ? 0.48f : 0.05f);

                        if (polished.HasProperty("_OcclusionStrength"))
                            polished.SetFloat("_OcclusionStrength", 1f);

                        characterMaterialCache[source] = polished;
                    }

                    materials[i] = polished;
                    changed = true;
                }

                if (changed)
                    renderer.sharedMaterials = materials;
            }
        }
    }

    private static bool IsGrassLike(string objectName, Material material)
    {
        string materialName = material != null ? material.name.ToLowerInvariant() : string.Empty;
        return objectName.Contains("ground") ||
               objectName.Contains("grass") ||
               objectName.Contains("terrain") ||
               materialName.Contains("ground") ||
               materialName.Contains("grass");
    }

    private static bool IsRoadLike(string objectName, Material material)
    {
        string materialName = material != null ? material.name.ToLowerInvariant() : string.Empty;
        return objectName.Contains("road") ||
               objectName.Contains("lane_dirt") ||
               objectName.Contains("lane_centered") ||
               objectName.Contains("lane_outer") ||
               materialName.Contains("road") ||
               materialName.Contains("lane_dirt");
    }

    private static void ApplyQualitySettings()
    {
        QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
        QualitySettings.antiAliasing = 4;
        QualitySettings.shadowDistance = Mathf.Max(QualitySettings.shadowDistance, 95f);
        QualitySettings.shadowResolution = ShadowResolution.VeryHigh;
        QualitySettings.lodBias = Mathf.Max(QualitySettings.lodBias, 2f);
        QualitySettings.maximumLODLevel = 0;
    }

    private static void ApplyLightingPolish()
    {
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.34f, 0.39f, 0.48f);
        RenderSettings.ambientEquatorColor = new Color(0.20f, 0.25f, 0.24f);
        RenderSettings.ambientGroundColor = new Color(0.055f, 0.07f, 0.07f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.10f, 0.14f, 0.17f);
        RenderSettings.fogDensity = 0.0022f;

        Light[] lights = Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (Light light in lights)
        {
            if (light == null || light.type != LightType.Directional)
                continue;

            light.color = new Color(0.93f, 0.97f, 1f);
            light.intensity = 1.25f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.78f;
            break;
        }

        Camera camera = Camera.main;
        if (camera != null)
        {
            camera.allowHDR = true;
            camera.allowMSAA = true;
            camera.backgroundColor = new Color(0.055f, 0.08f, 0.10f);
        }
    }
}
