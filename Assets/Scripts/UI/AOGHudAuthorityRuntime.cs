using UnityEngine;

/// <summary>
/// Ensures the competitive HUD is the only primary gameplay HUD and continuously binds
/// identity data to the human-controlled champion rather than whichever champion most
/// recently wrote AOGActiveChampion.Current.
/// </summary>
[DefaultExecutionOrder(1180)]
public class AOGHudAuthorityRuntime : MonoBehaviour
{
    private AOGActiveChampion lastBound;
    private float nextResolve;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindFirstObjectByType<AOGHudAuthorityRuntime>() != null)
            return;
        GameObject host = new GameObject("AOG_HUD_Authority_Runtime");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGHudAuthorityRuntime>();
    }

    private void Update()
    {
        if (Time.unscaledTime < nextResolve)
            return;
        nextResolve = Time.unscaledTime + 0.20f;

        DisableDuplicatePrimaryHud();
        BindPlayerHud();
    }

    private static void DisableDuplicatePrimaryHud()
    {
        AOGCompetitiveMobaHUDRuntime competitive = FindFirstObjectByType<AOGCompetitiveMobaHUDRuntime>();
        if (competitive == null)
            return;

        foreach (AOGNextStageHudRuntime duplicate in FindObjectsByType<AOGNextStageHudRuntime>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (duplicate != null && duplicate.enabled)
            {
                duplicate.enabled = false;
                Canvas canvas = duplicate.GetComponentInChildren<Canvas>(true);
                if (canvas != null)
                    canvas.gameObject.SetActive(false);
            }
        }
    }

    private void BindPlayerHud()
    {
        AOGActiveChampion player = AOGPlayerChampionAuthority.CurrentChampion;
        if (player == null || player == lastBound)
            return;

        lastBound = player;
        foreach (AOGDynamicChampionHudBinder binder in FindObjectsByType<AOGDynamicChampionHudBinder>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            if (binder != null) binder.Bind(player);

        AOGPlayerEconomy economy = player.GetComponent<AOGPlayerEconomy>();
        if (economy != null)
            AOGShopRuntime.Instance?.Bind(economy);
    }
}
