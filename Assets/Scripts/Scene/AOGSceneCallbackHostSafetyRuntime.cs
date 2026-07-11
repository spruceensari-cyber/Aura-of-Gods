using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Keeps persistent callback hosts active before SceneManager.sceneLoaded handlers execute.
/// Their components may remain disabled by suppression guards; the GameObjects must stay active
/// because Unity rejects StartCoroutine on inactive hosts even from static scene callbacks.
/// </summary>
[DefaultExecutionOrder(-2000)]
public sealed class AOGSceneCallbackHostSafetyRuntime : MonoBehaviour
{
    private static readonly string[] HostNames =
    {
        "AOG_Champion_Selection_Runtime",
        "AOG_World_Art_Director",
        "AOG_Champion_Select_Recovery_Runtime",
        "AOG_Expanded_Hero_Roster_Runtime",
        "AOG_Premium_Female_Hero_Roster_Runtime"
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        SceneManager.sceneLoaded -= BeforeOtherSceneCallbacks;
        SceneManager.sceneLoaded += BeforeOtherSceneCallbacks;
        ReactivateKnownHosts();
    }

    private static void BeforeOtherSceneCallbacks(Scene scene, LoadSceneMode mode)
    {
        ReactivateKnownHosts();
    }

    private static void ReactivateKnownHosts()
    {
        foreach (GameObject obj in FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (obj == null || obj.activeSelf) continue;
            for (int i = 0; i < HostNames.Length; i++)
            {
                if (!string.Equals(obj.name, HostNames[i], System.StringComparison.Ordinal)) continue;
                obj.SetActive(true);
                break;
            }
        }
    }
}
