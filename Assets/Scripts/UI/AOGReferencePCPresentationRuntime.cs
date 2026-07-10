using UnityEngine;

/// <summary>
/// Refines the existing competitive HUD in-place for a cleaner desktop presentation.
/// No competing HUD canvas is created; current HUD authority remains unchanged.
/// </summary>
[DefaultExecutionOrder(2100)]
public class AOGReferencePCPresentationRuntime : MonoBehaviour
{
    private bool applied;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindFirstObjectByType<AOGReferencePCPresentationRuntime>() != null)
            return;

        GameObject host = new GameObject("AOG_Reference_PC_Presentation_Runtime");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGReferencePCPresentationRuntime>();
    }

    private void Update()
    {
        if (applied)
            return;

        AOGCompetitiveMobaHUDRuntime hud = FindFirstObjectByType<AOGCompetitiveMobaHUDRuntime>();
        if (hud == null)
            return;

        ApplyCompactDesktopLayout(hud.transform);
        applied = true;
    }

    private static void ApplyCompactDesktopLayout(Transform hudRoot)
    {
        RectTransform championHud = FindRect(hudRoot, "ChampionHUD");
        if (championHud != null)
        {
            championHud.localScale = Vector3.one * 0.80f;
            championHud.anchoredPosition = new Vector2(0f, 7f);
        }

        RectTransform minimapFrame = FindRect(hudRoot, "MinimapFrame");
        if (minimapFrame != null)
        {
            minimapFrame.localScale = Vector3.one * 0.84f;
            minimapFrame.anchoredPosition = new Vector2(-10f, 10f);
        }

        RectTransform objectivePanel = FindRect(hudRoot, "ObjectivePanel");
        if (objectivePanel != null)
        {
            objectivePanel.localScale = Vector3.one * 0.78f;
            objectivePanel.anchoredPosition = new Vector2(10f, 12f);
        }

        RectTransform topScoreboard = FindRect(hudRoot, "TopScoreboard");
        if (topScoreboard != null)
        {
            topScoreboard.localScale = Vector3.one * 0.88f;
            topScoreboard.anchoredPosition = new Vector2(0f, -9f);
        }

        RectTransform abilities = FindRect(hudRoot, "Abilities");
        if (abilities != null)
            abilities.localScale = new Vector3(0.95f, 0.92f, 1f);

        RectTransform portrait = FindRect(hudRoot, "PortraitFrame");
        if (portrait != null)
            portrait.localScale = Vector3.one * 0.92f;

        RectTransform combatStats = FindRect(hudRoot, "CombatStats");
        if (combatStats != null)
            combatStats.localScale = Vector3.one * 0.90f;

        RectTransform items = FindRect(hudRoot, "Items");
        if (items != null)
            items.localScale = Vector3.one * 0.92f;
    }

    private static RectTransform FindRect(Transform root, string name)
    {
        foreach (RectTransform rect in root.GetComponentsInChildren<RectTransform>(true))
        {
            if (rect.name == name)
                return rect;
        }
        return null;
    }
}
