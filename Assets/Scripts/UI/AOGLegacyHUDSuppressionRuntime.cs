using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AOGLegacyHUDSuppressionRuntime : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Ensure();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Ensure();
    }

    private static void Ensure()
    {
        if (Object.FindFirstObjectByType<AOGLegacyHUDSuppressionRuntime>() != null)
            return;

        GameObject host = new GameObject("AOG_Legacy_HUD_Suppression");
        Object.DontDestroyOnLoad(host);
        host.AddComponent<AOGLegacyHUDSuppressionRuntime>();
    }

    private void Start()
    {
        StartCoroutine(SuppressDuringStartup());
    }

    private IEnumerator SuppressDuringStartup()
    {
        for (int i = 0; i < 30; i++)
        {
            AOGCombatHUDRuntime legacy = Object.FindFirstObjectByType<AOGCombatHUDRuntime>();
            if (legacy != null)
                Destroy(legacy.gameObject);

            yield return new WaitForSecondsRealtime(0.1f);
        }
    }
}
