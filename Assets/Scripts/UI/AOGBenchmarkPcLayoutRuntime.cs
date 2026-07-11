using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Repositions the existing Aura of Gods HUD toward a compact desktop esports layout.
/// It does not create a second gameplay HUD and does not replace live-data binders.
/// </summary>
[DefaultExecutionOrder(2600)]
public class AOGBenchmarkPcLayoutRuntime : MonoBehaviour
{
    private bool primaryApplied;
    private bool rosterApplied;
    private float nextTry;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindFirstObjectByType<AOGBenchmarkPcLayoutRuntime>() != null) return;
        GameObject host = new GameObject("AOG_Benchmark_PC_Layout_Runtime");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGBenchmarkPcLayoutRuntime>();
    }

    private void Update()
    {
        if (primaryApplied && rosterApplied) return;
        if (Time.unscaledTime < nextTry) return;
        nextTry = Time.unscaledTime + 0.35f;

        if (!primaryApplied)
        {
            AOGCompetitiveMobaHUDRuntime hud = FindFirstObjectByType<AOGCompetitiveMobaHUDRuntime>();
            if (hud != null)
            {
                ApplyPrimaryHud(hud.transform);
                primaryApplied = true;
            }
        }

        if (!rosterApplied)
        {
            AOGTeamRosterHudRuntime roster = FindFirstObjectByType<AOGTeamRosterHudRuntime>();
            if (roster != null)
            {
                ApplyRoster(roster.transform);
                rosterApplied = true;
            }
        }
    }

    private static void ApplyPrimaryHud(Transform root)
    {
        RectTransform championHud = FindRect(root,"ChampionHUD");
        if (championHud != null)
        {
            championHud.localScale = Vector3.one * 0.72f;
            championHud.anchoredPosition = new Vector2(0f,6f);
        }

        RectTransform minimapFrame = FindRect(root,"MinimapFrame");
        if (minimapFrame != null)
        {
            minimapFrame.localScale = Vector3.one * 0.80f;
            minimapFrame.anchoredPosition = new Vector2(-10f,10f);
        }

        RectTransform objectivePanel = FindRect(root,"ObjectivePanel");
        if (objectivePanel != null)
        {
            objectivePanel.localScale = Vector3.one * 0.76f;
            objectivePanel.anchoredPosition = new Vector2(12f,12f);
        }

        RectTransform topScoreboard = FindRect(root,"TopScoreboard");
        if (topScoreboard != null)
        {
            topScoreboard.localScale = new Vector3(0.86f,0.86f,1f);
            topScoreboard.anchoredPosition = new Vector2(0f,-10f);
        }

        // Keep the battlefield clear: the benchmark direction uses one compact bottom command deck.
        RectTransform abilities = FindRect(root,"Abilities");
        if (abilities != null) abilities.localScale = new Vector3(0.94f,0.92f,1f);
        RectTransform items = FindRect(root,"Items");
        if (items != null) items.localScale = Vector3.one * 0.92f;
        RectTransform stats = FindRect(root,"CombatStats");
        if (stats != null) stats.localScale = Vector3.one * 0.88f;

        // Slightly reduce background opacity for PC battlefield visibility.
        if (championHud != null)
        {
            Image image = championHud.GetComponent<Image>();
            if (image != null)
            {
                Color c = image.color;
                c.a = Mathf.Min(c.a,0.93f);
                image.color = c;
            }
        }
    }

    private static void ApplyRoster(Transform root)
    {
        // AOGTeamRosterHudRuntime owns this layout end-to-end. Keeping its geometry local
        // prevents presentation passes from fighting over the side roster on scene reload.
        return;
    }

    private static RectTransform FindRect(Transform root,string name)
    {
        foreach (RectTransform rect in root.GetComponentsInChildren<RectTransform>(true))
            if (rect.name == name) return rect;
        return null;
    }
}
