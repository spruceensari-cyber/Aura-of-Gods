using UnityEngine;

/// <summary>
/// Provides MOBA-style passive gold income to the human player after the match begins.
/// Last-hit and objective rewards continue to stack through AOGPlayerEconomy.AddGold.
/// </summary>
public class AOGPassiveEconomyRuntime : MonoBehaviour
{
    public float goldInterval = 1.0f;
    public int goldPerInterval = 3;
    public float startDelay = 8f;

    private AOGPlayerEconomy economy;
    private AOGActiveChampion champion;
    private float incomeStart;
    private float nextIncome;
    private bool initialized;

    private void Awake()
    {
        economy = GetComponent<AOGPlayerEconomy>();
        champion = GetComponent<AOGActiveChampion>();
    }

    private void Update()
    {
        if (economy == null || champion == null || !champion.IsActiveChampion)
            return;
        if (AOGPlayerChampionAuthority.CurrentChampion != champion)
            return;
        if (AOGMatchDirector.Instance == null || AOGMatchDirector.Instance.State != AOGMatchState.Playing)
        {
            initialized = false;
            return;
        }

        if (!initialized)
        {
            initialized = true;
            incomeStart = Time.unscaledTime + startDelay;
            nextIncome = incomeStart;
        }

        if (Time.unscaledTime < incomeStart || Time.unscaledTime < nextIncome)
            return;

        nextIncome += Mathf.Max(0.25f, goldInterval);
        economy.AddGold(Mathf.Max(1, goldPerInterval));
    }
}

public class AOGPassiveEconomyBootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindFirstObjectByType<AOGPassiveEconomyBootstrap>() != null)
            return;
        GameObject host = new GameObject("AOG_Passive_Economy_Bootstrap");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGPassiveEconomyBootstrap>();
    }

    private void Update()
    {
        AOGActiveChampion player = AOGPlayerChampionAuthority.CurrentChampion;
        if (player != null && player.GetComponent<AOGPassiveEconomyRuntime>() == null)
            player.gameObject.AddComponent<AOGPassiveEconomyRuntime>();
    }
}
