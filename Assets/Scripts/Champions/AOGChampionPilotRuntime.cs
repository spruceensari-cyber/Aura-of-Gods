using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Pilot bootstrap for the first original champion vertical slice.
/// Finds the locally controlled champion, installs Nyxara's gameplay kit and uses the procedural rig only when no authored skinned model exists.
/// </summary>
public class AOGChampionPilotRuntime : MonoBehaviour
{
    private const string ManagerName = "AOG_Champion_Pilot_Runtime";
    private bool installedForCurrentScene;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        EnsureManager();
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureManager();
        AOGChampionPilotRuntime runtime = FindObjectOfType<AOGChampionPilotRuntime>();
        if (runtime != null)
            runtime.installedForCurrentScene = false;
    }

    private static void EnsureManager()
    {
        if (FindObjectOfType<AOGChampionPilotRuntime>() != null)
            return;

        GameObject manager = new GameObject(ManagerName);
        manager.AddComponent<AOGChampionPilotRuntime>();
    }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (installedForCurrentScene)
            return;

        ChampionController controller = FindObjectOfType<ChampionController>();
        if (controller == null)
            return;

        Champion champion = controller.GetComponent<Champion>();
        if (champion == null)
            return;

        InstallNyxaraVerticalSlice(champion, controller);
        installedForCurrentScene = true;
    }

    private void InstallNyxaraVerticalSlice(Champion champion, ChampionController controller)
    {
        NyxaraRiftDancerKit kit = champion.GetComponent<NyxaraRiftDancerKit>();
        if (kit == null)
        {
            kit = champion.gameObject.AddComponent<NyxaraRiftDancerKit>();
            kit.Initialize(AOGChampionCatalog.CreateNyxara());
        }

        controller.RefreshAbilities();

        bool hasAuthoredCharacterModel = champion.GetComponentInChildren<SkinnedMeshRenderer>(true) != null;
        if (!hasAuthoredCharacterModel)
        {
            DisableSimplePlaceholderRenderers(champion.transform);
            AOGProceduralChampionRig rig = champion.GetComponent<AOGProceduralChampionRig>();
            if (rig == null)
                rig = champion.gameObject.AddComponent<AOGProceduralChampionRig>();
            rig.BuildNyxaraPresentation();
        }

        champion.gameObject.name = "Player_Nyxara_Rift_Dancer";
    }

    private void DisableSimplePlaceholderRenderers(Transform championRoot)
    {
        Renderer[] renderers = championRoot.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
                continue;

            string objectName = renderer.gameObject.name.ToLowerInvariant();
            bool obviousPrimitive = objectName.Contains("capsule")
                || objectName.Contains("sphere")
                || objectName.Contains("cube")
                || renderer.transform == championRoot;

            if (obviousPrimitive)
                renderer.enabled = false;
        }
    }
}
