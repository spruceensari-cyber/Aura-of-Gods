using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Installs the clean three-hero vertical-slice roster on the local player and first combat bots.
/// Legacy pilot runtimes are disabled so they cannot reinstall old kits after the rebuild roster is active.
/// </summary>
public class AOGOriginalRosterBootstrapRuntime : MonoBehaviour
{
    private bool installed;

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
        AOGOriginalRosterBootstrapRuntime runtime = FindObjectOfType<AOGOriginalRosterBootstrapRuntime>();
        if (runtime != null)
        {
            runtime.installed = false;
            runtime.DisableLegacyPilotRuntimes();
        }
    }

    private static void Ensure()
    {
        if (FindObjectOfType<AOGOriginalRosterBootstrapRuntime>() != null) return;
        new GameObject("AOG_Original_Roster_Bootstrap").AddComponent<AOGOriginalRosterBootstrapRuntime>();
    }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        DisableLegacyPilotRuntimes();
    }

    private void Update()
    {
        if (installed) return;
        DisableLegacyPilotRuntimes();

        ChampionController local = FindObjectOfType<ChampionController>();
        if (local == null) return;
        StartCoroutine(InstallRoster(local));
        installed = true;
    }

    private void DisableLegacyPilotRuntimes()
    {
        MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour == null || behaviour == this) continue;
            string n = behaviour.GetType().Name;
            if (n == "AOGChampionPilotRuntime" || n == "AOGKharvosPilotRuntime")
                behaviour.enabled = false;
        }
    }

    private IEnumerator InstallRoster(ChampionController local)
    {
        Champion localChampion = local.GetComponent<Champion>();
        if (localChampion != null)
        {
            RemoveLegacyKits(localChampion.gameObject);
            yield return null;
            InstallHero(localChampion.gameObject, AOGOriginalHeroId.SorynPrismHuntress);
        }

        BotChampionAI[] bots = FindObjectsByType<BotChampionAI>(FindObjectsSortMode.None);
        List<GameObject> uniqueBots = new();
        foreach (BotChampionAI bot in bots)
        {
            if (bot == null || uniqueBots.Contains(bot.gameObject)) continue;
            uniqueBots.Add(bot.gameObject);
        }

        if (uniqueBots.Count > 0)
        {
            RemoveLegacyKits(uniqueBots[0]);
            yield return null;
            InstallHero(uniqueBots[0], AOGOriginalHeroId.CaelixRiftVanguard);
        }

        if (uniqueBots.Count > 1)
        {
            RemoveLegacyKits(uniqueBots[1]);
            yield return null;
            InstallHero(uniqueBots[1], AOGOriginalHeroId.VaelithChronoOracle);
        }
    }

    private static void RemoveLegacyKits(GameObject go)
    {
        NyxaraRiftDancerKit nyxara = go.GetComponent<NyxaraRiftDancerKit>();
        if (nyxara != null) Destroy(nyxara);

        KharvosWorldbreakerKit kharvos = go.GetComponent<KharvosWorldbreakerKit>();
        if (kharvos != null) Destroy(kharvos);

        AOGOriginalHeroKitRuntime original = go.GetComponent<AOGOriginalHeroKitRuntime>();
        if (original != null) Destroy(original);
    }

    private static void InstallHero(GameObject go, AOGOriginalHeroId id)
    {
        if (go == null) return;
        AOGOriginalHeroKitRuntime kit = go.AddComponent<AOGOriginalHeroKitRuntime>();
        kit.Initialize(id);

        AOGHeroMotionAnimator motion = go.GetComponent<AOGHeroMotionAnimator>();
        if (motion == null) motion = go.AddComponent<AOGHeroMotionAnimator>();
        motion.Initialize(id);

        ChampionController controller = go.GetComponent<ChampionController>();
        if (controller != null) controller.RefreshAbilities();
    }
}
