using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Repositions the existing Aura of Gods HUD toward a readable desktop esports layout.
/// It reuses the existing primary HUD and roster canvases; no second gameplay HUD is created.
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
            championHud.localScale = Vector3.one * 0.88f;
            championHud.anchoredPosition = new Vector2(0f,8f);
        }

        RectTransform minimapFrame = FindRect(root,"MinimapFrame");
        if (minimapFrame != null)
        {
            minimapFrame.localScale = Vector3.one * 0.95f;
            minimapFrame.anchoredPosition = new Vector2(-12f,12f);
        }

        RectTransform objectivePanel = FindRect(root,"ObjectivePanel");
        if (objectivePanel != null)
        {
            objectivePanel.localScale = Vector3.one * 0.80f;
            objectivePanel.anchoredPosition = new Vector2(14f,14f);
        }

        RectTransform topScoreboard = FindRect(root,"TopScoreboard");
        if (topScoreboard != null)
        {
            topScoreboard.localScale = new Vector3(0.96f,0.96f,1f);
            topScoreboard.anchoredPosition = new Vector2(0f,-10f);
        }

        RectTransform abilities = FindRect(root,"Abilities");
        if (abilities != null) abilities.localScale = Vector3.one;
        RectTransform items = FindRect(root,"Items");
        if (items != null) items.localScale = Vector3.one;
        RectTransform stats = FindRect(root,"CombatStats");
        if (stats != null) stats.localScale = Vector3.one * 0.96f;
        RectTransform portrait = FindRect(root,"PortraitFrame");
        if (portrait != null) portrait.localScale = Vector3.one;

        if (championHud != null)
        {
            Image image = championHud.GetComponent<Image>();
            if (image != null)
            {
                Color c = image.color;
                c.a = Mathf.Clamp(c.a,0.88f,0.95f);
                image.color = c;
            }
        }
    }

    private static void ApplyRoster(Transform root)
    {
        RectTransform canvas = FindRect(root,"TeamRosterCanvas");
        if (canvas == null) return;

        int blueIndex = 0;
        int redIndex = 0;
        foreach (RectTransform rect in canvas.GetComponentsInChildren<RectTransform>(true))
        {
            if (rect == canvas) continue;
            bool blue = rect.name.StartsWith("Blue_Roster_");
            bool red = rect.name.StartsWith("Red_Roster_");
            if (!blue && !red) continue;

            int index = blue ? blueIndex++ : redIndex++;
            rect.anchorMin = rect.anchorMax = blue ? new Vector2(0f,1f) : new Vector2(1f,1f);
            rect.pivot = blue ? new Vector2(0f,1f) : new Vector2(1f,1f);
            rect.anchoredPosition = new Vector2(blue ? 12f : -12f,-90f-index*49f);
            rect.sizeDelta = new Vector2(286f,43f);

            Image panel = rect.GetComponent<Image>();
            if (panel != null)
                panel.color = blue ? new Color(0.008f,0.045f,0.09f,0.90f) : new Color(0.09f,0.010f,0.020f,0.90f);

            Text label = rect.GetComponentInChildren<Text>(true);
            if (label != null)
            {
                label.fontSize = 15;
                label.alignment = blue ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight;
            }

            EnsureRosterAccent(rect,blue);
        }

        RectTransform duplicateCenter = FindRect(root,"RealTeamKillScore");
        if (duplicateCenter != null)
            duplicateCenter.gameObject.SetActive(false);
    }

    private static void EnsureRosterAccent(RectTransform row,bool blue)
    {
        Transform existing = row.Find("TeamAccent");
        if (existing != null) return;

        GameObject accent = new GameObject("TeamAccent",typeof(RectTransform),typeof(Image));
        accent.transform.SetParent(row,false);
        RectTransform r = accent.GetComponent<RectTransform>();
        r.anchorMin = r.anchorMax = blue ? new Vector2(0f,0.5f) : new Vector2(1f,0.5f);
        r.pivot = blue ? new Vector2(0f,0.5f) : new Vector2(1f,0.5f);
        r.anchoredPosition = Vector2.zero;
        r.sizeDelta = new Vector2(5f,43f);
        accent.GetComponent<Image>().color = blue ? new Color(0.10f,0.55f,1f,1f) : new Color(1f,0.10f,0.16f,1f);
    }

    private static RectTransform FindRect(Transform root,string name)
    {
        foreach (RectTransform rect in root.GetComponentsInChildren<RectTransform>(true))
            if (rect.name == name) return rect;
        return null;
    }
}
