using UnityEngine;

/// <summary>
/// Keeps legacy runtime host GameObjects active so Unity scene callbacks never attempt to start
/// coroutines on inactive objects. Their components remain disabled, so no legacy UI or art rebuilds.
/// </summary>
[DefaultExecutionOrder(-1200)]
public class AOGInactiveRuntimeHostSafetyRuntime : MonoBehaviour
{
    private static readonly string[] HostNames =
    {
        "AOG_Champion_Selection_Runtime",
        "AOG_World_Art_Director"
    };

    private float nextSweep;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        if (FindFirstObjectByType<AOGInactiveRuntimeHostSafetyRuntime>() != null) return;
        GameObject host = new GameObject("AOG_Inactive_Runtime_Host_Safety");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGInactiveRuntimeHostSafetyRuntime>();
    }

    private void Update()
    {
        if (Time.unscaledTime < nextSweep) return;
        nextSweep = Time.unscaledTime + 0.10f;
        Sweep();
    }

    private static void Sweep()
    {
        foreach (Transform t in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t == null) continue;
            bool match = false;
            for (int i = 0; i < HostNames.Length; i++)
            {
                if (t.name == HostNames[i])
                {
                    match = true;
                    break;
                }
            }
            if (!match) continue;

            if (!t.gameObject.activeSelf)
                t.gameObject.SetActive(true);

            if (t.name == "AOG_Champion_Selection_Runtime")
            {
                AOGChampionSelectionRuntime selection = t.GetComponent<AOGChampionSelectionRuntime>();
                if (selection != null)
                {
                    selection.StopAllCoroutines();
                    selection.enabled = false;
                }
            }
            else if (t.name == "AOG_World_Art_Director")
            {
                AOGWorldArtDirectorRuntime art = t.GetComponent<AOGWorldArtDirectorRuntime>();
                if (art != null)
                {
                    art.StopAllCoroutines();
                    art.enabled = false;
                }
            }
        }
    }
}
