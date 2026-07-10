using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-10000)]
public class AOGPlayableSceneBootstrap : MonoBehaviour
{
    private const string PlayableSceneName = "AOGSymmetricReferenceMap_TowerTest";
    private static bool loadingPlayableScene;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        Scene active = SceneManager.GetActiveScene();
        if (!active.IsValid() || loadingPlayableScene)
            return;

        bool unsavedScene = string.IsNullOrEmpty(active.path) ||
                            string.Equals(active.name, "Untitled", System.StringComparison.OrdinalIgnoreCase);
        bool fallbackSampleScene = string.Equals(active.name, "SampleScene", System.StringComparison.OrdinalIgnoreCase);
        bool alreadyPlayable = string.Equals(active.name, PlayableSceneName, System.StringComparison.OrdinalIgnoreCase);

        if (alreadyPlayable || (!unsavedScene && !fallbackSampleScene))
            return;

        GameObject host = new GameObject("AOG_Playable_Scene_Bootstrap");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGPlayableSceneBootstrap>();
    }

    private void Start()
    {
        if (!loadingPlayableScene)
            StartCoroutine(LoadPlayableSceneRoutine());
    }

    private IEnumerator LoadPlayableSceneRoutine()
    {
        loadingPlayableScene = true;
        yield return null;

        AsyncOperation operation = SceneManager.LoadSceneAsync(PlayableSceneName, LoadSceneMode.Single);
        if (operation == null)
        {
            Debug.LogError(
                "AOG: Could not load the playable scene '" + PlayableSceneName +
                "'. Ensure Assets/Scenes/AOGSymmetricReferenceMap_TowerTest.unity is enabled in Build Settings.");
            loadingPlayableScene = false;
            Destroy(gameObject);
            yield break;
        }

        while (!operation.isDone)
            yield return null;

        loadingPlayableScene = false;
        Destroy(gameObject);
    }
}
