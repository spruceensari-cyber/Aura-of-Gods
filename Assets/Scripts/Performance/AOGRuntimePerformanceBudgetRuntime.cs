using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Conservative one-time performance budget for the current URP runtime scene.
/// It avoids touching gameplay simulation and only reduces presentation cost that is safe to trim.
/// </summary>
[DefaultExecutionOrder(17000)]
public class AOGRuntimePerformanceBudgetRuntime : MonoBehaviour
{
    public int maxDecorativePointLights = 20;
    public int maxParticlesPerSystem = 220;
    private bool applied;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindFirstObjectByType<AOGRuntimePerformanceBudgetRuntime>() != null) return;
        GameObject host = new GameObject("AOG_Runtime_Performance_Budget_Runtime");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGRuntimePerformanceBudgetRuntime>();
    }

    private IEnumerator Start()
    {
        yield return new WaitForSecondsRealtime(2.5f);
        ApplyBudget();
    }

    private void ApplyBudget()
    {
        if (applied) return;
        applied = true;

        QualitySettings.shadowDistance = Mathf.Min(QualitySettings.shadowDistance,70f);
        QualitySettings.shadowCascades = Mathf.Min(QualitySettings.shadowCascades,2);

        List<Light> decorative = new List<Light>();
        foreach (Light light in FindObjectsByType<Light>(FindObjectsInactive.Exclude,FindObjectsSortMode.None))
        {
            if (light == null || light.type == LightType.Directional) continue;
            if (light.type == LightType.Point || light.type == LightType.Spot)
            {
                light.shadows = LightShadows.None;
                string n = light.gameObject.name.ToLowerInvariant();
                if (n.Contains("rim") || n.Contains("aura") || n.Contains("accent") || n.Contains("landmark") || n.Contains("core") || n.Contains("vfx") || n.Contains("identity"))
                    decorative.Add(light);
            }
        }

        decorative.Sort((a,b) => (b.intensity*b.range).CompareTo(a.intensity*a.range));
        for (int i=maxDecorativePointLights;i<decorative.Count;i++)
            if (decorative[i] != null) decorative[i].enabled = false;

        foreach (ParticleSystem system in FindObjectsByType<ParticleSystem>(FindObjectsInactive.Exclude,FindObjectsSortMode.None))
        {
            if (system == null) continue;
            ParticleSystem.MainModule main = system.main;
            main.maxParticles = Mathf.Min(main.maxParticles,maxParticlesPerSystem);
        }

        Camera camera = Camera.main;
        if (camera != null)
        {
            camera.farClipPlane = Mathf.Min(camera.farClipPlane,360f);
            camera.useOcclusionCulling = true;
        }
    }
}
