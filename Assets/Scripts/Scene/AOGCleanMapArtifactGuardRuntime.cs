using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Prevents legacy presentation-only map passes from rebuilding after the clean competitive map.
/// Gameplay map expansion/objective logic is intentionally not disabled.
/// </summary>
[DefaultExecutionOrder(-590)]
public class AOGCleanMapArtifactGuardRuntime : MonoBehaviour
{
    private static readonly HashSet<string> BlockedRuntimeTypes = new HashSet<string>
    {
        "AOGPremiumMapVisualRuntime",
        "AOGMapSpatialIdentityRuntime",
        "AOGDistinctJungleFlowRuntime",
        "AOGMapGameplayBeautyRuntime",
        "AOGMapBeautyReadabilityRuntime",
        "AOGBenchmarkMapArtDirectionRuntime",
        "AOGWorldArtDirectorRuntime"
    };

    private static readonly HashSet<string> BlockedRoots = new HashSet<string>
    {
        "AOG_Runtime_World_Art_Layer",
        "12_Map_Beauty_Readability_Pass",
        "AOG_Premium_Map_Pass",
        "AOG_Map_Spatial_Identity_Pass",
        "11_Distinct_Jungle_Flow",
        "AOG_Map_Gameplay_Beauty_Pass"
    };

    private float stopAt;
    private float nextSweep;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindFirstObjectByType<AOGCleanMapArtifactGuardRuntime>() != null) return;
        GameObject host = new GameObject("AOG_Clean_Map_Artifact_Guard");
        DontDestroyOnLoad(host);
        AOGCleanMapArtifactGuardRuntime guard = host.AddComponent<AOGCleanMapArtifactGuardRuntime>();
        guard.stopAt = Time.unscaledTime + 7f;
        guard.StopCompetingVisualRuntimes();
        guard.SweepGeneratedRoots();
    }

    private void Update()
    {
        if (Time.unscaledTime > stopAt || Time.unscaledTime < nextSweep) return;
        nextSweep = Time.unscaledTime + 0.30f;
        StopCompetingVisualRuntimes();
        SweepGeneratedRoots();
    }

    private void StopCompetingVisualRuntimes()
    {
        foreach (MonoBehaviour behaviour in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {
            if (behaviour == null || behaviour == this) continue;
            if (!BlockedRuntimeTypes.Contains(behaviour.GetType().Name)) continue;
            behaviour.StopAllCoroutines();
            behaviour.enabled = false;
        }
    }

    private void SweepGeneratedRoots()
    {
        foreach (Transform transformItem in FindObjectsByType<Transform>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {
            if (transformItem == null || !BlockedRoots.Contains(transformItem.name)) continue;
            if (transformItem.gameObject.activeSelf)
                transformItem.gameObject.SetActive(false);
        }

        foreach (LineRenderer line in FindObjectsByType<LineRenderer>(FindObjectsInactive.Exclude,FindObjectsSortMode.None))
        {
            if (line == null) continue;
            string n = line.gameObject.name.ToLowerInvariant();
            bool legacyMapLine = n.Contains("readability_edge") || n.Contains("boundary_edge") ||
                                 n.Contains("lane_readability") || n.Contains("map_spine") ||
                                 n.Contains("jungle_route") || n.Contains("path_trim");
            if (legacyMapLine) line.enabled = false;
        }
    }
}
