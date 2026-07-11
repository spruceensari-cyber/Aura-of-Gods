using UnityEngine;

/// <summary>
/// Repairs a committed roster selection when the player authority was not registered in time.
/// This is a narrow recovery path for scene-start ordering conflicts between selection runtimes.
/// </summary>
[DefaultExecutionOrder(-760)]
public class AOGSelectionAuthorityRecoveryRuntime : MonoBehaviour
{
    private float nextCheck;
    private bool recovered;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindFirstObjectByType<AOGSelectionAuthorityRecoveryRuntime>() != null) return;
        GameObject host = new GameObject("AOG_Selection_Authority_Recovery");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGSelectionAuthorityRecoveryRuntime>();
    }

    private void Update()
    {
        if (recovered || Time.unscaledTime < nextCheck) return;
        nextCheck = Time.unscaledTime + 0.15f;

        AOGGameSession session = AOGGameSession.Instance;
        if (session == null || !session.SelectionCommitted || string.IsNullOrEmpty(session.SelectedPlayerChampionId)) return;

        AOGPlayerChampionAuthority authority = AOGPlayerChampionAuthority.EnsureInstance();
        if (authority.HasValidPlayer)
        {
            recovered = true;
            return;
        }

        AOGActiveChampion selected = null;
        foreach (AOGActiveChampion candidate in FindObjectsByType<AOGActiveChampion>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (candidate == null) continue;
            if (string.Equals(candidate.championId, session.SelectedPlayerChampionId, System.StringComparison.OrdinalIgnoreCase))
            {
                selected = candidate;
                break;
            }
        }

        if (selected == null) return;

        selected.gameObject.SetActive(true);
        foreach (AOGActiveChampion candidate in FindObjectsByType<AOGActiveChampion>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (candidate == null || candidate == selected) continue;
            candidate.SetActiveChampion(false);
            if (candidate.GetComponent<AOGTeamMemberIdentity>() == null)
                candidate.gameObject.SetActive(false);
        }

        authority.RegisterPlayerChampion(selected, session.PlayerRole);
        selected.SetActiveChampion(true);

        if (!authority.HasValidPlayer) return;

        AOGRoleBasedTeamRuntime.EnsureAndBuildTeams(selected, session.PlayerRole);
        AOGMatchDirector.Instance?.BeginMatch();
        recovered = true;
    }
}
