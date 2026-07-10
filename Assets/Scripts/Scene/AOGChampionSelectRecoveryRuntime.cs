using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-1200)]
public class AOGChampionSelectRecoveryRuntime : MonoBehaviour
{
    private const string PlayableSceneName = "AOGSymmetricReferenceMap_TowerTest";
    private static AOGChampionSelectRecoveryRuntime instance;
    private bool recoveryRunning;
    private bool minionWaveGuardRunning;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        EnsureInstance();
    }

    private static void EnsureInstance()
    {
        if (instance != null)
            return;

        GameObject host = new GameObject("AOG_Champion_Select_Recovery_Runtime");
        instance = host.AddComponent<AOGChampionSelectRecoveryRuntime>();
        DontDestroyOnLoad(host);
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureInstance();
        if (instance == null)
            return;

        instance.StopAllCoroutines();
        instance.recoveryRunning = false;
        instance.minionWaveGuardRunning = false;

        if (string.Equals(scene.name, PlayableSceneName, System.StringComparison.OrdinalIgnoreCase))
            instance.StartCoroutine(instance.RecoverPlayableSceneFlow());
    }

    private IEnumerator Start()
    {
        yield return null;
        Scene scene = SceneManager.GetActiveScene();
        if (scene.IsValid() && string.Equals(scene.name, PlayableSceneName, System.StringComparison.OrdinalIgnoreCase))
            yield return RecoverPlayableSceneFlow();
    }

    private IEnumerator RecoverPlayableSceneFlow()
    {
        if (recoveryRunning)
            yield break;

        recoveryRunning = true;

        // Let the gameplay scene, player model and the normal selector initialize first.
        yield return new WaitForSecondsRealtime(0.9f);

        AOGMatchDirector director = AOGMatchDirector.Instance;
        if (director != null && director.State == AOGMatchState.Playing)
        {
            recoveryRunning = false;
            StartMinionWaveGuard();
            yield break;
        }

        GameObject selectionCanvas = GameObject.Find("ChampionSelectCanvas");
        if (selectionCanvas == null)
        {
            // A selector can survive the SampleScene -> gameplay-scene redirect with its
            // setup coroutine stuck in the previous scene. Recreate only that runtime host.
            AOGChampionSelectionRuntime selector = FindFirstObjectByType<AOGChampionSelectionRuntime>(FindObjectsInactive.Include);
            if (selector != null)
            {
                GameObject selectorHost = selector.gameObject;
                Destroy(selectorHost);
                yield return null;
            }

            GameObject host = new GameObject("AOG_Champion_Selection_Runtime");
            host.AddComponent<AOGChampionSelectionRuntime>();
            DontDestroyOnLoad(host);

            // Give the rebuilt selector time to find Lyra/create Kaelith and draw the UI.
            float deadline = Time.unscaledTime + 5f;
            while (Time.unscaledTime < deadline && GameObject.Find("ChampionSelectCanvas") == null)
                yield return new WaitForSecondsRealtime(0.15f);
        }

        // If the UI still failed, make one final clean retry rather than starting the match silently.
        if (GameObject.Find("ChampionSelectCanvas") == null)
        {
            AOGChampionSelectionRuntime stale = FindFirstObjectByType<AOGChampionSelectionRuntime>(FindObjectsInactive.Include);
            if (stale != null)
            {
                Destroy(stale.gameObject);
                yield return null;
            }

            GameObject retryHost = new GameObject("AOG_Champion_Selection_Runtime_Retry");
            retryHost.AddComponent<AOGChampionSelectionRuntime>();
            DontDestroyOnLoad(retryHost);
        }

        recoveryRunning = false;
        StartMinionWaveGuard();
    }

    private void StartMinionWaveGuard()
    {
        if (!minionWaveGuardRunning)
            StartCoroutine(EnsureFirstMinionWave());
    }

    private IEnumerator EnsureFirstMinionWave()
    {
        minionWaveGuardRunning = true;

        while (AOGMatchDirector.Instance == null || AOGMatchDirector.Instance.State != AOGMatchState.Playing)
            yield return new WaitForSecondsRealtime(0.2f);

        // The normal MinionSpawner loop waits 1.5 seconds before the first wave.
        // Allow it plenty of time before intervening.
        yield return new WaitForSeconds(4f);

        bool anyLivingMinion = false;
        foreach (Minion minion in Minion.Active)
        {
            if (minion != null && minion.hp > 0f)
            {
                anyLivingMinion = true;
                break;
            }
        }

        if (!anyLivingMinion)
        {
            MinionSpawner spawner = FindFirstObjectByType<MinionSpawner>();
            if (spawner != null)
                spawner.SendMessage("StartWave", SendMessageOptions.DontRequireReceiver);
            else
                Debug.LogError("AOG: Match started but no MinionSpawner exists in the playable scene.");
        }

        minionWaveGuardRunning = false;
    }
}
