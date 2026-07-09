using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-2000)]
public class AOGCurrentMapAuthorityRuntime : MonoBehaviour
{
    private static readonly string[] LegacyMapRoots =
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
        yield return new WaitForSecondsRealtime(0.45f);
        ResolveMapAuthority();
        yield return new WaitForSecondsRealtime(0.75f);
        RebindGameplayToCurrentMap();
    }

    private void ResolveMapAuthority()
    {
        Transform currentMap = FindTransformByExactName("AOG_Symmetric_Reference_Map");
        if (currentMap != null)
            currentMap.gameObject.SetActive(true);

        foreach (string legacyName in LegacyMapRoots)
        {
            Transform legacy = FindTransformByExactName(legacyName);
            if (legacy != null && legacy != currentMap)
                legacy.gameObject.SetActive(false);
        }

        // Disable duplicate full-map roots that contain ground + lane structures but are not the authoritative map.
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root == null || !root.activeSelf || root.transform == currentMap)
                continue;

            string lower = root.name.ToLowerInvariant();
            bool suspiciousMap = lower.Contains("map") &&
                                 (FindChildContains(root.transform, "lane") || FindChildContains(root.transform, "ground"));
            if (suspiciousMap && !lower.Contains("game") && !lower.Contains("waypoint"))
                root.SetActive(false);
        }
    }

    private void RebindGameplayToCurrentMap()
    {
        MinionSpawner spawner = FindFirstObjectByType<MinionSpawner>();
        if (spawner == null)
            return;

        if (spawner.blueBaseSpawn == null)
            spawner.blueBaseSpawn = FindNamedTransform("BlueBaseSpawn", "BlueSpawn", "Blue_Spawn");
        if (spawner.redBaseSpawn == null)
            spawner.redBaseSpawn = FindNamedTransform("RedBaseSpawn", "RedSpawn", "Red_Spawn");

        Camera camera = Camera.main;
        if (camera != null)
        {
            AOGMobaCameraController controller = camera.GetComponent<AOGMobaCameraController>();
            if (controller != null && AOGActiveChampion.Current != null)
                controller.SetTarget(AOGActiveChampion.Current.transform, true);
        }
    }

    private static Transform FindTransformByExactName(string exactName)
    {
        foreach (GameObject obj in FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (obj != null && obj.name == exactName)
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
