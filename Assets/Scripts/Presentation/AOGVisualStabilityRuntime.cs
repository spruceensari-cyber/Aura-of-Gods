using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Conservative runtime stability pass for camera clipping, renderer visibility and LOD pop-in.
/// Keeps authored assets intact while reducing aggressive disappearance during gameplay.
/// </summary>
public class AOGVisualStabilityRuntime : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;

        if (FindObjectOfType<AOGVisualStabilityRuntime>() != null) return;
        new GameObject("AOG_Visual_Stability_Runtime").AddComponent<AOGVisualStabilityRuntime>();
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        AOGVisualStabilityRuntime runtime = FindObjectOfType<AOGVisualStabilityRuntime>();
        if (runtime == null) return;
        runtime.ApplyCameraSafety();
        runtime.ApplyLodSafety();
    }

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        ApplyCameraSafety();
        ApplyLodSafety();
    }

    private void ApplyCameraSafety()
    {
        Camera camera = Camera.main;
        if (camera == null) return;
        camera.nearClipPlane = Mathf.Min(camera.nearClipPlane, 0.15f);
        camera.farClipPlane = Mathf.Max(camera.farClipPlane, 600f);
        camera.useOcclusionCulling = false;
        camera.allowHDR = true;
        camera.allowMSAA = true;
    }

    private void ApplyLodSafety()
    {
        foreach (LODGroup group in FindObjectsByType<LODGroup>(FindObjectsSortMode.None))
        {
            if (group == null) continue;
            group.animateCrossFading = true;
            group.fadeMode = LODFadeMode.CrossFade;
            group.size = Mathf.Max(group.size, 1f);
        }

        foreach (SkinnedMeshRenderer renderer in FindObjectsByType<SkinnedMeshRenderer>(FindObjectsSortMode.None))
        {
            if (renderer == null) continue;
            renderer.updateWhenOffscreen = true;
        }
    }
}
