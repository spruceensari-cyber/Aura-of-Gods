using UnityEngine;

[DefaultExecutionOrder(-25)]
public class AOGMinionLegacyAISuppression : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindFirstObjectByType<AOGMinionLegacyAISuppression>() != null)
            return;

        GameObject host = new GameObject("AOG_Minion_Legacy_AI_Suppression");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGMinionLegacyAISuppression>();
    }

    private void Update()
    {
        AOGLaneMinionAI[] legacyControllers = FindObjectsByType<AOGLaneMinionAI>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (AOGLaneMinionAI legacy in legacyControllers)
        {
            if (legacy != null && legacy.enabled && legacy.GetComponent<Minion>() != null)
                legacy.enabled = false;
        }
    }
}
