using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Subtle URP post-processing tuned for competitive readability.
/// Keeps bloom restrained, improves contrast and avoids heavy cinematic effects.
/// </summary>
public class AOGCompetitivePostProcessRuntime : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindObjectOfType<AOGCompetitivePostProcessRuntime>() != null) return;
        new GameObject("AOG_Competitive_Post_Process_Runtime").AddComponent<AOGCompetitivePostProcessRuntime>();
    }

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        BuildVolume();
    }

    private void BuildVolume()
    {
        Volume volume = gameObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 20f;

        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        profile.name = "AOG_Runtime_Competitive_Profile";
        volume.profile = profile;

        Bloom bloom = profile.Add<Bloom>(true);
        bloom.intensity.Override(0.28f);
        bloom.threshold.Override(1.15f);
        bloom.scatter.Override(0.42f);

        ColorAdjustments color = profile.Add<ColorAdjustments>(true);
        color.postExposure.Override(0.05f);
        color.contrast.Override(8f);
        color.saturation.Override(4f);

        Tonemapping tone = profile.Add<Tonemapping>(true);
        tone.mode.Override(TonemappingMode.ACES);

        Vignette vignette = profile.Add<Vignette>(true);
        vignette.intensity.Override(0.08f);
        vignette.smoothness.Override(0.35f);
    }
}
