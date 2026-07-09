using UnityEngine;

/// <summary>
/// Installs Veyra on the next available combat bot after Kharvos, creating a broader playable roster slice.
/// </summary>
public class AOGVeyraPilotRuntime : MonoBehaviour
{
    private bool installed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindObjectOfType<AOGVeyraPilotRuntime>() != null)
            return;

        GameObject obj = new GameObject("AOG_Veyra_Pilot_Runtime");
        obj.AddComponent<AOGVeyraPilotRuntime>();
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
            if (champion == null)
                continue;
            if (champion.GetComponent<KharvosWorldbreakerKit>() != null || champion.GetComponent<NyxaraRiftDancerKit>() != null)
                continue;

            VeyraVectorSaintKit kit = champion.GetComponent<VeyraVectorSaintKit>();
            if (kit == null)
            {
                kit = champion.gameObject.AddComponent<VeyraVectorSaintKit>();
                kit.Initialize(AOGChampionCatalog.CreateVeyra());
            }

            champion.gameObject.name = "Bot_Veyra_Vector_Saint";
            installed = true;
            break;
        }
    }
}
