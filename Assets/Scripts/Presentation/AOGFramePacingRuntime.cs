using UnityEngine;

/// <summary>
/// Stable frame pacing defaults for competitive play.
/// Avoids runaway frame rates and keeps physics timing consistent.
/// </summary>
public class AOGFramePacingRuntime : MonoBehaviour
{
    [SerializeField] private int desktopTargetFps = 120;
    [SerializeField] private int mobileTargetFps = 60;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        if (FindObjectOfType<AOGFramePacingRuntime>() != null) return;
        new GameObject("AOG_Frame_Pacing_Runtime").AddComponent<AOGFramePacingRuntime>();
    }

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = Application.isMobilePlatform ? mobileTargetFps : desktopTargetFps;
        Time.fixedDeltaTime = 1f / 60f;
        Time.maximumDeltaTime = 1f / 15f;
    }
}
