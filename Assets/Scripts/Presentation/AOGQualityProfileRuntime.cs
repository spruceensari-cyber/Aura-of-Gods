using UnityEngine;

/// <summary>
/// Platform-aware quality baseline for the playable production slice.
/// Keeps visual ambition high while avoiding unbounded settings on mobile hardware.
/// </summary>
public class AOGQualityProfileRuntime : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindObjectOfType<AOGQualityProfileRuntime>() != null)
            return;

        GameObject obj = new GameObject("AOG_Quality_Profile_Runtime");
        obj.AddComponent<AOGQualityProfileRuntime>();
    }

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        ApplyProfile();
    }

    private void ApplyProfile()
    {
        bool mobile = Application.isMobilePlatform;

        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = mobile ? 60 : 120;
        QualitySettings.shadowDistance = mobile ? 55f : 110f;
        QualitySettings.shadowResolution = mobile ? ShadowResolution.Medium : ShadowResolution.High;
        QualitySettings.antiAliasing = mobile ? 2 : 4;
        QualitySettings.anisotropicFiltering = AnisotropicFiltering.Enable;
        QualitySettings.realtimeReflectionProbes = !mobile;
        QualitySettings.softParticles = !mobile;
        QualitySettings.lodBias = mobile ? 1.2f : 2f;
        QualitySettings.maximumLODLevel = mobile ? 1 : 0;
        QualitySettings.skinWeights = mobile ? SkinWeights.TwoBones : SkinWeights.FourBones;
    }
}
