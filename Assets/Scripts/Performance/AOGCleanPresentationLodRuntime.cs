using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Distance-based presentation LOD for the clean competitive build. Simulation remains active;
/// only minion renderers, particles and decorative lights are culled at conservative distances.
/// </summary>
[DefaultExecutionOrder(17200)]
public class AOGCleanPresentationLodRuntime : MonoBehaviour
{
    public float minionVisualDistance = 105f;
    public float particleVisualDistance = 78f;
    public float decorativeLightDistance = 48f;

    private readonly Dictionary<Minion, Renderer[]> minionRenderers = new Dictionary<Minion, Renderer[]>();
    private float nextRefresh;
    private Camera camera;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindFirstObjectByType<AOGCleanPresentationLodRuntime>() != null) return;
        GameObject host = new GameObject("AOG_Clean_Presentation_LOD_Runtime");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGCleanPresentationLodRuntime>();
    }

    private void Update()
    {
        if (Time.unscaledTime < nextRefresh) return;
        nextRefresh = Time.unscaledTime + 0.35f;
        if (camera == null) camera = Camera.main;
        if (camera == null) return;

        Vector3 cameraPosition = camera.transform.position;
        UpdateMinions(cameraPosition);
        UpdateParticles(cameraPosition);
        UpdateDecorativeLights(cameraPosition);
    }

    private void UpdateMinions(Vector3 cameraPosition)
    {
        float sqr = minionVisualDistance * minionVisualDistance;
        HashSet<Minion> active = new HashSet<Minion>();

        foreach (Minion minion in Minion.Active)
        {
            if (minion == null) continue;
            active.Add(minion);
            if (!minionRenderers.TryGetValue(minion,out Renderer[] renderers) || renderers == null)
            {
                renderers = minion.GetComponentsInChildren<Renderer>(true);
                minionRenderers[minion] = renderers;
            }

            bool visible = (minion.transform.position - cameraPosition).sqrMagnitude <= sqr;
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null) continue;
                string lower = renderer.gameObject.name.ToLowerInvariant();
                if (lower.Contains("hp_") || lower.Contains("health")) continue;
                renderer.enabled = visible;
            }
        }

        List<Minion> stale = null;
        foreach (Minion key in minionRenderers.Keys)
        {
            if (key != null && active.Contains(key)) continue;
            if (stale == null) stale = new List<Minion>();
            stale.Add(key);
        }
        if (stale != null) foreach (Minion key in stale) minionRenderers.Remove(key);
    }

    private void UpdateParticles(Vector3 cameraPosition)
    {
        float sqr = particleVisualDistance * particleVisualDistance;
        foreach (ParticleSystem system in FindObjectsByType<ParticleSystem>(FindObjectsInactive.Exclude,FindObjectsSortMode.None))
        {
            if (system == null) continue;
            bool near = (system.transform.position - cameraPosition).sqrMagnitude <= sqr;
            ParticleSystem.EmissionModule emission = system.emission;
            emission.enabled = near;
        }
    }

    private void UpdateDecorativeLights(Vector3 cameraPosition)
    {
        float sqr = decorativeLightDistance * decorativeLightDistance;
        foreach (Light light in FindObjectsByType<Light>(FindObjectsInactive.Exclude,FindObjectsSortMode.None))
        {
            if (light == null || light.type == LightType.Directional) continue;
            string lower = light.gameObject.name.ToLowerInvariant();
            bool decorative = lower.Contains("rim") || lower.Contains("accent") || lower.Contains("aura") ||
                              lower.Contains("vfx") || lower.Contains("identity") || lower.Contains("landmark");
            if (!decorative) continue;
            light.enabled = (light.transform.position - cameraPosition).sqrMagnitude <= sqr;
        }
    }
}
