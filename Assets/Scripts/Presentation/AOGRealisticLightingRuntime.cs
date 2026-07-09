using UnityEngine;

/// <summary>
/// Realism-oriented lighting and atmosphere baseline for the future-mythic map.
/// Tunes ambient light, fog, sun softness and camera background without replacing authored scene lighting.
/// </summary>
public class AOGRealisticLightingRuntime : MonoBehaviour
{
    [SerializeField] private Color ambientSky = new Color(0.12f, 0.16f, 0.24f);
    [SerializeField] private Color ambientEquator = new Color(0.08f, 0.10f, 0.16f);
    [SerializeField] private Color ambientGround = new Color(0.025f, 0.03f, 0.05f);
    [SerializeField] private Color fogColor = new Color(0.035f, 0.05f, 0.09f);
    [SerializeField] private float fogDensity = 0.0065f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        if (FindObjectOfType<AOGRealisticLightingRuntime>() != null) return;
        new GameObject("AOG_Realistic_Lighting_Runtime").AddComponent<AOGRealisticLightingRuntime>();
    }

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        Apply();
    }

    void Apply()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = ambientSky;
        RenderSettings.ambientEquatorColor = ambientEquator;
        RenderSettings.ambientGroundColor = ambientGround;

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = fogColor;
        RenderSettings.fogDensity = fogDensity;

        Light sun = RenderSettings.sun;
        if (sun != null)
        {
            sun.color = new Color(0.82f, 0.90f, 1f);
            sun.intensity = Mathf.Clamp(sun.intensity, 0.8f, 1.25f);
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.78f;
        }

        Camera camera = Camera.main;
        if (camera != null)
        {
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.allowHDR = true;
        }
    }
}
