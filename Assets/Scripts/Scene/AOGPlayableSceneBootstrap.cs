using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-10000)]
public class AOGPlayableSceneBootstrap : MonoBehaviour
{
    private static bool loadingPlayableScene;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        Scene active = SceneManager.GetActiveScene();
        if (!active.IsValid())
            return;

        // Unity can enter Play Mode from an unsaved 'Untitled' scene. In that case
        // runtime systems (HUD/heroes/camps) still bootstrap, but the actual map is absent.
        bool unsavedScene = string.IsNullOrEmpty(active.path) || string.Equals(active.name, "Untitled", System.StringComparison.OrdinalIgnoreCase);
        if (!unsavedScene || loadingPlayableScene)
            return;

        GameObject host = new GameObject("AOG_Playable_Scene_Bootstrap");
        DontDestroyOnLoad(host);
        AOGPlayableSceneBootstrap bootstrap = host.AddComponent<AOGPlayableSceneBootstrap>();
        bootstrap.StartCoroutine(bootstrap.LoadPlayableScene());
    }

    private IEnumerator LoadPlayableScene()
    {
        loadingPlayableScene = true;
        yield return null;

        // SampleScene is the canonical enabled gameplay scene in EditorBuildSettings.
        AsyncOperation operation = SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);
        if (operation == null)
        {
            Debug.LogError("AOG: Could not load SampleScene. Ensure Assets/Scenes/SampleScene.unity is enabled in Build Settings.");
            loadingPlayableScene = false;
            yield break;
        }

        while (!operation.isDone)
            yield return null;

        loadingPlayableScene = false;
        Destroy(gameObject);
    }
}
