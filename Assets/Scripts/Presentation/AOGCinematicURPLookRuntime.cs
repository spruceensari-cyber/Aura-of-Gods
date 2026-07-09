using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(9000)]
public class AOGCinematicURPLookRuntime : MonoBehaviour
{
    private static AOGCinematicURPLookRuntime instance;
    private Volume volume;

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
        instance?.ApplyCameraSettings();
    }

    private static void EnsureInstance()
    {
        if (instance != null)
            return;

        AOGCinematicURPLookRuntime existing = Object.FindFirstObjectByType<AOGCinematicURPLookRuntime>();
        if (existing != null)
        {
            instance = existing;
            return;
        }

        GameObject host = new GameObject("AOG_Cinematic_URP_Look");
        instance = host.AddComponent<AOGCinematicURPLookRuntime>();
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
        BuildVolume();
        ApplyCameraSettings();
    }

    private void BuildVolume()
    {
        volume = gameObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 1000f;
        volume.weight = 1f;

        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        profile.name = "AOG_Runtime_Cinematic_Profile";
        volume.sharedProfile = profile;

        Tonemapping tonemapping = profile.Add<Tonemapping>(true);
        tonemapping.mode.Override(TonemappingMode.ACES);

        Bloom bloom = profile.Add<Bloom>(true);
        bloom.threshold.Override(1.15f);
        bloom.intensity.Override(0.38f);
        bloom.scatter.Override(0.62f);
        bloom.clamp.Override(8f);

        ColorAdjustments color = profile.Add<ColorAdjustments>(true);
        color.postExposure.Override(0.10f);
        color.contrast.Override(13f);
        color.hueShift.Override(-1f);
        color.saturation.Override(7f);
        color.colorFilter.Override(new Color(0.97f, 0.99f, 1f, 1f));

        WhiteBalance whiteBalance = profile.Add<WhiteBalance>(true);
        whiteBalance.temperature.Override(-5f);
        whiteBalance.tint.Override(2f);

        Vignette vignette = profile.Add<Vignette>(true);
        vignette.color.Override(new Color(0.015f, 0.025f, 0.045f, 1f));
        vignette.intensity.Override(0.16f);
        vignette.smoothness.Override(0.52f);
        vignette.rounded.Override(false);

        FilmGrain grain = profile.Add<FilmGrain>(true);
        grain.type.Override(FilmGrainLookup.Thin1);
        grain.intensity.Override(0.035f);
        grain.response.Override(0.72f);

        ChromaticAberration chroma = profile.Add<ChromaticAberration>(true);
        chroma.intensity.Override(0.025f);
    }

    private void ApplyCameraSettings()
    {
        Camera camera = Camera.main;
        if (camera == null)
            return;

        camera.allowHDR = true;
        camera.allowMSAA = true;

        UniversalAdditionalCameraData cameraData = camera.GetUniversalAdditionalCameraData();
        cameraData.renderPostProcessing = true;
        cameraData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        cameraData.antialiasingQuality = AntialiasingQuality.High;
        cameraData.stopNaN = true;
        cameraData.dithering = true;
    }
}
