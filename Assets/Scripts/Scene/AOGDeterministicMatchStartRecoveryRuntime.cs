using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Deterministically finalizes champion selection when legacy/unified selection ordering leaves
/// the session or player authority incomplete. It only acts after the selection canvas is gone
/// or a selection is already committed.
/// </summary>
[DefaultExecutionOrder(-705)]
public sealed class AOGDeterministicMatchStartRecoveryRuntime : MonoBehaviour
{
    private const string PlayableSceneName = "AOGSymmetricReferenceMap_TowerTest";
    private float nextCheck;
    private bool completed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindFirstObjectByType<AOGDeterministicMatchStartRecoveryRuntime>() != null) return;
        GameObject host = new GameObject("AOG_Deterministic_Match_Start_Recovery");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGDeterministicMatchStartRecoveryRuntime>();
    }

    private void Update()
    {
        if (completed || Time.unscaledTime < nextCheck) return;
        nextCheck = Time.unscaledTime + 0.10f;

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !string.Equals(scene.name, PlayableSceneName, System.StringComparison.OrdinalIgnoreCase)) return;

        AOGMatchDirector director = AOGMatchDirector.Instance;
        if (director != null && director.State == AOGMatchState.Playing)
        {
            completed = true;
            return;
        }

        AOGGameSession session = AOGGameSession.EnsureInstance();
        bool selectionCanvasVisible = IsSelectionCanvasVisible();
        if (!session.SelectionCommitted && selectionCanvasVisible) return;

        AOGActiveChampion selected = ResolveSelectedChampion(session);
        if (selected == null) return;

        AOGChampionDefinition definition = AOGRosterDatabase.EnsureInstance().GetDefinition(selected.championId);
        if (!session.SelectionCommitted)
        {
            if (definition == null) return;
            if (!session.TryCommitSelection(definition)) return;
        }

        selected.gameObject.SetActive(true);
        AOGRole role = definition != null ? definition.primaryRole : session.PlayerRole;

        AOGPlayerChampionAuthority authority = AOGPlayerChampionAuthority.EnsureInstance();
        authority.RegisterPlayerChampion(selected, role);
        selected.SetActiveChampion(true);

        if (!authority.HasValidPlayer)
        {
            // RegisterPlayerChampion normally sets all of this. Reassert once for runtimes that
            // changed the marker state later in the same frame.
            selected.gameObject.SetActive(true);
            selected.SetActiveChampion(true);
            authority.RegisterPlayerChampion(selected, role);
        }

        if (!authority.HasValidPlayer) return;

        AOGRoleBasedTeamRuntime.EnsureAndBuildTeams(selected, role);
        director = AOGMatchDirector.Instance;
        if (director != null)
            director.BeginMatch();

        if (director != null && (director.State == AOGMatchState.Playing || director.State == AOGMatchState.MatchStarting))
            completed = true;
    }

    private static AOGActiveChampion ResolveSelectedChampion(AOGGameSession session)
    {
        AOGPlayerChampionAuthority authority = AOGPlayerChampionAuthority.Instance;
        if (authority != null && authority.Current != null)
            return authority.Current;

        if (AOGActiveChampion.Current != null)
            return AOGActiveChampion.Current;

        AOGActiveChampion[] all = FindObjectsByType<AOGActiveChampion>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (session != null && !string.IsNullOrEmpty(session.SelectedPlayerChampionId))
        {
            foreach (AOGActiveChampion candidate in all)
                if (candidate != null && string.Equals(candidate.championId, session.SelectedPlayerChampionId, System.StringComparison.OrdinalIgnoreCase))
                    return candidate;
        }

        AOGActiveChampion onlyVisible = null;
        int visibleCount = 0;
        foreach (AOGActiveChampion candidate in all)
        {
            if (candidate == null || !candidate.gameObject.activeInHierarchy) continue;
            if (candidate.GetComponent<AOGTeamMemberIdentity>() != null && !candidate.GetComponent<AOGTeamMemberIdentity>().isHumanPlayer) continue;
            if (!HasVisibleRenderer(candidate)) continue;
            onlyVisible = candidate;
            visibleCount++;
            if (visibleCount > 1) break;
        }
        return visibleCount == 1 ? onlyVisible : null;
    }

    private static bool HasVisibleRenderer(AOGActiveChampion candidate)
    {
        foreach (Renderer renderer in candidate.GetComponentsInChildren<Renderer>(true))
            if (renderer != null && renderer.enabled && renderer.gameObject.activeInHierarchy)
                return true;
        return false;
    }

    private static bool IsSelectionCanvasVisible()
    {
        foreach (Canvas canvas in FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            if (canvas != null && canvas.gameObject.name == "ChampionSelectCanvas")
                return true;
        return false;
    }
}
