using UnityEngine;

/// <summary>
/// Removes champion-select candidates that are not the authoritative player and were not promoted
/// into the 5v5 team roster. This prevents preview characters from remaining in the live match.
/// </summary>
[DefaultExecutionOrder(1850)]
public class AOGSelectionArtifactCleanupRuntime : MonoBehaviour
{
    private bool cleaned;
    private float nextTry;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindFirstObjectByType<AOGSelectionArtifactCleanupRuntime>() != null) return;
        GameObject host = new GameObject("AOG_Selection_Artifact_Cleanup_Runtime");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGSelectionArtifactCleanupRuntime>();
    }

    private void Update()
    {
        if (cleaned) return;
        if (Time.unscaledTime < nextTry) return;
        nextTry = Time.unscaledTime + 0.35f;

        if (AOGMatchDirector.Instance == null || AOGMatchDirector.Instance.State != AOGMatchState.Playing)
            return;

        AOGActiveChampion player = AOGPlayerChampionAuthority.CurrentChampion;
        if (player == null) return;

        foreach (AOGActiveChampion candidate in FindObjectsByType<AOGActiveChampion>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {
            if (candidate == null || candidate == player) continue;
            if (candidate.GetComponent<AOGTeamMemberIdentity>() != null) continue;

            candidate.SetActiveChampion(false);
            candidate.gameObject.SetActive(false);
        }

        cleaned = true;
    }
}
