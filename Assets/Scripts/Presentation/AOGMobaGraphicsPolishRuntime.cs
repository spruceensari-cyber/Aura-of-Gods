using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(15500)]
public class AOGMobaGraphicsPolishRuntime : MonoBehaviour
{
    private static AOGMobaGraphicsPolishRuntime instance;
    private float nextRefresh;

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
        if (instance != null) instance.ApplyScenePolish();
    }

    private static void Ensure()
    {
        if (instance != null) return;
        GameObject host = new GameObject("AOG_MOBA_Graphics_Polish");
        DontDestroyOnLoad(host);
        instance = host.AddComponent<AOGMobaGraphicsPolishRuntime>();
    }

    private void Start() { ApplyScenePolish(); }

    private void Update()
    {
        if (Time.unscaledTime < nextRefresh) return;
        nextRefresh = Time.unscaledTime + 2.5f;
        ImproveLights();
        ImproveRenderers();
        ImproveCamera();
    }

    private void ApplyScenePolish()
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.0065f;
        RenderSettings.fogColor = new Color(0.065f, 0.105f, 0.115f, 1f);
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.20f, 0.28f, 0.33f, 1f);
        RenderSettings.ambientEquatorColor = new Color(0.12f, 0.19f, 0.20f, 1f);
        RenderSettings.ambientGroundColor = new Color(0.045f, 0.075f, 0.065f, 1f);
        RenderSettings.reflectionIntensity = 0.72f;

        ImproveLights();
        ImproveRenderers();
        ImproveCamera();
    }

    private void ImproveLights()
    {
        foreach (Light light in FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (light == null) continue;
            if (light.type == LightType.Directional)
            {
                light.shadows = LightShadows.Soft;
                light.shadowStrength = 0.72f;
                light.shadowBias = 0.035f;
                light.shadowNormalBias = 0.28f;
                light.intensity = Mathf.Max(light.intensity, 1.15f);
                light.color = new Color(0.91f, 0.96f, 1f, 1f);
            }
            else if (light.type == LightType.Point)
            {
                light.intensity = Mathf.Min(light.intensity, 2.2f);
                light.shadows = LightShadows.None;
            }
        }
    }

    private void ImproveRenderers()
    {
        foreach (Renderer renderer in FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (renderer == null) continue;
            string lower = renderer.gameObject.name.ToLowerInvariant();
            if (lower.Contains("hp_") || lower.Contains("bar_") || lower.Contains("telegraph"))
            {
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                continue;
            }

            renderer.receiveShadows = true;
            if (!(renderer is ParticleSystemRenderer))
                renderer.shadowCastingMode = ShadowCastingMode.On;
        }
    }

    private void ImproveCamera()
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        cam.allowHDR = true;
        cam.allowMSAA = true;
        cam.nearClipPlane = Mathf.Min(cam.nearClipPlane, 0.08f);
        cam.farClipPlane = Mathf.Max(cam.farClipPlane, 1100f);
    }
}
