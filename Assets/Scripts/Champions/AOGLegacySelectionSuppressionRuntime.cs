using UnityEngine;

[DefaultExecutionOrder(-790)]
public class AOGLegacySelectionSuppressionRuntime : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        GameObject host = new GameObject("AOG_Legacy_Selection_Suppression");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGLegacySelectionSuppressionRuntime>();
    }

    private void Update()
    {
        foreach (AOGChampionSelectionRuntime legacy in FindObjectsByType<AOGChampionSelectionRuntime>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (legacy == null || !legacy.gameObject.activeSelf) continue;
            legacy.StopAllCoroutines();
            legacy.enabled = false;
            legacy.gameObject.SetActive(false);
        }

        foreach (AOGChampionSelectRecoveryRuntime recovery in FindObjectsByType<AOGChampionSelectRecoveryRuntime>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (recovery == null || !recovery.gameObject.activeSelf) continue;
            recovery.StopAllCoroutines();
            recovery.enabled = false;
            recovery.gameObject.SetActive(false);
        }

        foreach (AOGExpandedHeroRosterRuntime legacyRoster in FindObjectsByType<AOGExpandedHeroRosterRuntime>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (legacyRoster == null || !legacyRoster.gameObject.activeSelf) continue;
            legacyRoster.enabled = false;
            legacyRoster.gameObject.SetActive(false);
        }

        foreach (AOGPremiumFemaleHeroRosterRuntime legacyRoster in FindObjectsByType<AOGPremiumFemaleHeroRosterRuntime>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (legacyRoster == null || !legacyRoster.gameObject.activeSelf) continue;
            legacyRoster.enabled = false;
            legacyRoster.gameObject.SetActive(false);
        }
    }
}
