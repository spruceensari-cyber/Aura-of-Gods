using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-2000)]
public class AOGCurrentMapAuthorityRuntime : MonoBehaviour
{
    private static readonly string[] PreferredMapNames =
    {
        "AOG_Symmetric_Reference_Map",
        "AOGSymmetricReferenceMap",
        "Map",
        "Terrain"
    };

    private static readonly string[] ExplicitLegacyMapRoots =
    {
        "AOG_VisualMap",
        "AOG_ArtPass_RealGameLook",
        "LeagueOfGodsMap",
        "OldMap",
        "LegacyMap"
    };

    private static AOGCurrentMapAuthorityRuntime instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        EnsureInstance();
        if (instance != null)
            instance.StartCoroutine(instance.ResolveAfterSceneReady());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureInstance();
        if (instance != null)
            instance.StartCoroutine(instance.ResolveAfterSceneReady());
    }

    private static void EnsureInstance()
    {
        if (instance != null)
            return;

        GameObject host = new GameObject("AOG_Current_Map_Authority_Runtime");
        instance = host.AddComponent<AOGCurrentMapAuthorityRuntime>();
        DontDestroyOnLoad(host);
    }

    private IEnumerator ResolveAfterSceneReady()
    {
        // Runtime-generated map objects may appear over several frames.
        yield return null;
        yield return new WaitForSecondsRealtime(0.20f);
        RestorePlayableWorld();
        yield return new WaitForSecondsRealtime(0.80f);
        RestorePlayableWorld();
        RebindGameplayToCurrentMap();
    }

    private void RestorePlayableWorld()
    {
        Transform authoritativeMap = FindBestPlayableMap();

        if (authoritativeMap != null)
        {
            authoritativeMap.gameObject.SetActive(true);
            SetParentsActive(authoritativeMap);

            // Only disable explicitly named legacy roots and only when a valid authoritative map exists.
            foreach (string legacyName in ExplicitLegacyMapRoots)
            {
                Transform legacy = FindTransformByExactName(legacyName);
                if (legacy != null && legacy != authoritativeMap && !authoritativeMap.IsChildOf(legacy))
                    legacy.gameObject.SetActive(false);
            }
        }
        else
        {
            // Safety fallback: never leave the game with an invisible world.
            // Re-enable likely scene map/terrain roots rather than guessing and disabling them.
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (root == null)
                    continue;

                string lower = root.name.ToLowerInvariant();
                bool likelyWorld = lower.Contains("map") || lower.Contains("terrain") || lower.Contains("ground") || lower.Contains("lane");
                if (likelyWorld)
                    root.SetActive(true);
            }
        }
    }

    private Transform FindBestPlayableMap()
    {
        foreach (string preferredName in PreferredMapNames)
        {
            Transform exact = FindTransformByExactName(preferredName);
            if (exact != null)
                return exact;
        }

        Transform best = null;
        int bestScore = -1;
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root == null)
                continue;

            int score = ScorePlayableWorld(root.transform);
            if (score > bestScore)
            {
                bestScore = score;
                best = root.transform;
            }
        }

        return bestScore >= 3 ? best : null;
    }

    private int ScorePlayableWorld(Transform root)
    {
        int score = 0;
        string rootName = root.name.ToLowerInvariant();
        if (rootName.Contains("map")) score += 2;
        if (rootName.Contains("terrain")) score += 2;
        if (FindChildContains(root, "lane")) score += 2;
        if (FindChildContains(root, "ground")) score += 1;
        if (FindChildContains(root, "tower")) score += 1;
        if (FindChildContains(root, "nexus")) score += 1;
        return score;
    }

    private void RebindGameplayToCurrentMap()
    {
        MinionSpawner spawner = FindFirstObjectByType<MinionSpawner>();
        if (spawner != null)
        {
            if (spawner.blueBaseSpawn == null)
                spawner.blueBaseSpawn = FindNamedTransform("BlueBaseSpawn", "BlueSpawn", "Blue_Spawn");
            if (spawner.redBaseSpawn == null)
                spawner.redBaseSpawn = FindNamedTransform("RedBaseSpawn", "RedSpawn", "Red_Spawn");
        }

        Camera camera = Camera.main;
        AOGActiveChampion active = AOGActiveChampion.Current;
        if (camera != null && active != null && active.IsActiveChampion)
        {
            AOGMobaCameraController controller = camera.GetComponent<AOGMobaCameraController>();
            if (controller != null)
                controller.SetTarget(active.transform, true);
        }
    }

    private static void SetParentsActive(Transform child)
    {
        Transform current = child;
        while (current != null)
        {
            if (!current.gameObject.activeSelf)
                current.gameObject.SetActive(true);
            current = current.parent;
        }
    }

    private static Transform FindTransformByExactName(string exactName)
    {
        foreach (GameObject obj in FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (obj != null && string.Equals(obj.name, exactName, System.StringComparison.OrdinalIgnoreCase))
                return obj.transform;
        return null;
    }

    private static Transform FindNamedTransform(params string[] names)
    {
        foreach (GameObject obj in FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (obj == null)
                continue;
            foreach (string n in names)
                if (string.Equals(obj.name, n, System.StringComparison.OrdinalIgnoreCase))
                    return obj.transform;
        }
        return null;
    }

    private static bool FindChildContains(Transform root, string token)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            if (t.name.ToLowerInvariant().Contains(token))
                return true;
        return false;
    }
}
