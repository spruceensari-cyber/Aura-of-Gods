using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Enforces one human player plus the ten authoritative 5v5 roster members.
/// It quarantines late selection artifacts for several seconds after match start so scene-prefab
/// leftovers cannot reappear after the first cleanup pass.
/// </summary>
[DefaultExecutionOrder(1900)]
public class AOGCleanRosterAndSelectionRuntime : MonoBehaviour
{
    private static readonly string[] ChampionTokens =
    {
        "lyra", "kaelith", "auron", "vesper", "nyra", "pyrelle", "selene",
        "seris", "mireva", "dravenor", "nocthyr"
    };

    private float stopAt;
    private float nextSweep;
    private bool started;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindFirstObjectByType<AOGCleanRosterAndSelectionRuntime>() != null) return;
        GameObject host = new GameObject("AOG_Clean_Roster_Selection_Runtime");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGCleanRosterAndSelectionRuntime>();
    }

    private void Update()
    {
        if (AOGMatchDirector.Instance == null || AOGMatchDirector.Instance.State != AOGMatchState.Playing)
            return;

        AOGActiveChampion player = AOGPlayerChampionAuthority.CurrentChampion;
        if (player == null) return;

        if (!started)
        {
            started = true;
            stopAt = Time.unscaledTime + 8f;
            StartCoroutine(HardCleanupNextFrame());
        }

        if (Time.unscaledTime > stopAt || Time.unscaledTime < nextSweep) return;
        nextSweep = Time.unscaledTime + 0.45f;
        Sweep(player);
    }

    private IEnumerator HardCleanupNextFrame()
    {
        yield return null;
        AOGActiveChampion player = AOGPlayerChampionAuthority.CurrentChampion;
        if (player != null) Sweep(player);
        yield return new WaitForSecondsRealtime(0.35f);
        if (player != null) Sweep(player);
    }

    private static void Sweep(AOGActiveChampion player)
    {
        HashSet<GameObject> allowedRoots = new HashSet<GameObject>();
        allowedRoots.Add(player.transform.root.gameObject);

        foreach (AOGTeamMemberIdentity member in AOGRoleBasedTeamRuntime.BlueRoster)
            if (member != null) allowedRoots.Add(member.transform.root.gameObject);
        foreach (AOGTeamMemberIdentity member in AOGRoleBasedTeamRuntime.RedRoster)
            if (member != null) allowedRoots.Add(member.transform.root.gameObject);

        foreach (AOGTeamMemberIdentity member in AOGWorldRegistry.TeamMembers)
            if (member != null) allowedRoots.Add(member.transform.root.gameObject);

        foreach (AOGActiveChampion candidate in FindObjectsByType<AOGActiveChampion>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {
            if (candidate == null) continue;
            GameObject root = candidate.transform.root.gameObject;
            if (allowedRoots.Contains(root)) continue;
            DisableGameplay(candidate.gameObject);
            root.SetActive(false);
        }

        foreach (GameObject obj in FindObjectsByType<GameObject>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {
            if (obj == null || !obj.activeSelf) continue;
            Transform rootTransform = obj.transform.root;
            if (rootTransform == null) continue;
            GameObject root = rootTransform.gameObject;
            if (allowedRoots.Contains(root)) continue;
            if (root.scene.name == "DontDestroyOnLoad") continue;

            string lower = root.name.ToLowerInvariant();
            bool championNamed = false;
            foreach (string token in ChampionTokens)
            {
                if (lower.Contains(token)) { championNamed = true; break; }
            }
            if (!championNamed) continue;

            AOGTeamMemberIdentity identity = root.GetComponentInChildren<AOGTeamMemberIdentity>(true);
            if (identity != null) continue;
            DisableGameplay(root);
            root.SetActive(false);
        }
    }

    private static void DisableGameplay(GameObject target)
    {
        if (target == null) return;
        foreach (AOGUnifiedMobaInputDriver input in target.GetComponentsInChildren<AOGUnifiedMobaInputDriver>(true))
            input.enabled = false;
        foreach (AOGBotChampionAI ai in target.GetComponentsInChildren<AOGBotChampionAI>(true))
            ai.enabled = false;
        foreach (AOGJungleChampionAIRuntime ai in target.GetComponentsInChildren<AOGJungleChampionAIRuntime>(true))
            ai.enabled = false;
        foreach (AudioSource source in target.GetComponentsInChildren<AudioSource>(true))
            source.Stop();
    }
}
