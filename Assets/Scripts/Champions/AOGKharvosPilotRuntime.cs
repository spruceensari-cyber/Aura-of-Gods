using UnityEngine;

/// <summary>
/// Installs Kharvos on the first valid bot champion to create a playable PvP-style vertical slice against Nyxara.
/// </summary>
public class AOGKharvosPilotRuntime : MonoBehaviour
{
    private const string RuntimeName = "AOG_Kharvos_Pilot_Runtime";
    private bool installed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindObjectOfType<AOGKharvosPilotRuntime>() != null)
            return;

        GameObject obj = new GameObject(RuntimeName);
        obj.AddComponent<AOGKharvosPilotRuntime>();
    }

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        if (installed)
            return;

        BotChampionAI[] bots = FindObjectsByType<BotChampionAI>(FindObjectsSortMode.None);
        foreach (BotChampionAI bot in bots)
        {
            Champion champion = bot.GetComponent<Champion>();
            if (champion == null || champion.GetComponent<NyxaraRiftDancerKit>() != null)
                continue;

            KharvosWorldbreakerKit kit = champion.GetComponent<KharvosWorldbreakerKit>();
            if (kit == null)
            {
                kit = champion.gameObject.AddComponent<KharvosWorldbreakerKit>();
                kit.Initialize(AOGChampionCatalog.CreateKharvos());
            }

            champion.gameObject.name = "Bot_Kharvos_Worldbreaker";
            installed = true;
            break;
        }
    }
}
