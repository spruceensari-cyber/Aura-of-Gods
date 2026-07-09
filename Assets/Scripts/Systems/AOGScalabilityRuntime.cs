using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Runtime scalability defaults for the current Aura of Gods map.
/// Raises conservative prototype caps while keeping platform-specific headroom.
/// </summary>
public class AOGScalabilityRuntime : MonoBehaviour
{
    private const string ManagerName = "AOG_Scalability_Runtime";

    [SerializeField] private int desktopMinionCapPerTeam = 120;
    [SerializeField] private int mobileMinionCapPerTeam = 72;
    [SerializeField] private int desktopTargetFrameRate = 120;
    [SerializeField] private int mobileTargetFrameRate = 60;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        EnsureManager();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureManager();
        ApplyToScene();
    }

    private static void EnsureManager()
    {
        AOGScalabilityRuntime existing = FindObjectOfType<AOGScalabilityRuntime>();
        if (existing != null)
            return;

        GameObject manager = new GameObject(ManagerName);
        manager.AddComponent<AOGScalabilityRuntime>();
    }

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        ApplySettings();
    }

    void Start()
    {
        ApplySettings();
    }

    private void ApplySettings()
    {
        int cap = Application.isMobilePlatform ? mobileMinionCapPerTeam : desktopMinionCapPerTeam;
        Application.targetFrameRate = Application.isMobilePlatform ? mobileTargetFrameRate : desktopTargetFrameRate;

        MinionSpawner[] spawners = FindObjectsOfType<MinionSpawner>();
        foreach (MinionSpawner spawner in spawners)
        {
            if (spawner != null)
                spawner.maxMinionsPerTeam = Mathf.Max(spawner.maxMinionsPerTeam, cap);
        }
    }

    private static void ApplyToScene()
    {
        AOGScalabilityRuntime runtime = FindObjectOfType<AOGScalabilityRuntime>();
        if (runtime != null)
            runtime.ApplySettings();
    }
}
