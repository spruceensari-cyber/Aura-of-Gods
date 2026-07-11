using UnityEngine;

[DefaultExecutionOrder(-790)]
public class AOGLegacySelectionSuppressionRuntime : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindFirstObjectByType<AOGLegacySelectionSuppressionRuntime>() != null) return;
        GameObject host = new GameObject("AOG_Legacy_Selection_Suppression");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGLegacySelectionSuppressionRuntime>();
    }

    private void Update()
    {
        foreach (AOGChampionSelectionRuntime legacy in FindObjectsByType<AOGChampionSelectionRuntime>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (legacy == null) continue;
            if (!legacy.gameObject.activeSelf) legacy.gameObject.SetActive(true);
            legacy.StopAllCoroutines();
            legacy.enabled = false;
        }

        foreach (AOGChampionSelectRecoveryRuntime recovery in FindObjectsByType<AOGChampionSelectRecoveryRuntime>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (recovery == null) continue;
            if (!recovery.gameObject.activeSelf) recovery.gameObject.SetActive(true);
            recovery.StopAllCoroutines();
            recovery.enabled = false;
        }

        foreach (AOGExpandedHeroRosterRuntime legacyRoster in FindObjectsByType<AOGExpandedHeroRosterRuntime>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (legacyRoster == null) continue;
            if (!legacyRoster.gameObject.activeSelf) legacyRoster.gameObject.SetActive(true);
            legacyRoster.StopAllCoroutines();
            legacyRoster.enabled = false;
        }

        foreach (AOGPremiumFemaleHeroRosterRuntime legacyRoster in FindObjectsByType<AOGPremiumFemaleHeroRosterRuntime>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (legacyRoster == null) continue;
            if (!legacyRoster.gameObject.activeSelf) legacyRoster.gameObject.SetActive(true);
            legacyRoster.StopAllCoroutines();
            legacyRoster.enabled = false;
        }
    }
}
