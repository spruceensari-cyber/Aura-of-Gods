using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Applies a single readable desktop MOBA layout to the existing authoritative HUD.
/// It preserves live bindings and only changes hierarchy visibility, anchors, sizes and spacing.
/// </summary>
[DefaultExecutionOrder(16620)]
public class AOGOriginalCompetitiveHudLayoutRuntime : MonoBehaviour
{
    private float nextRefresh;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindFirstObjectByType<AOGOriginalCompetitiveHudLayoutRuntime>() != null) return;
        GameObject host = new GameObject("AOG_Original_Competitive_HUD_Layout");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGOriginalCompetitiveHudLayoutRuntime>();
    }

    private IEnumerator Start()
    {
        yield return new WaitForSecondsRealtime(0.8f);
        Apply();
    }

    private void Update()
    {
        if (Time.unscaledTime < nextRefresh) return;
        nextRefresh = Time.unscaledTime + 1.0f;
        Apply();
    }

    private static void Apply()
    {
        AOGCompetitiveMobaHUDRuntime hud = FindFirstObjectByType<AOGCompetitiveMobaHUDRuntime>();
        if (hud == null) return;

        Transform canvas = hud.transform.Find("CompetitiveHUDCanvas");
        if (canvas == null) canvas = hud.GetComponentInChildren<Canvas>(true)?.transform;
        if (canvas == null) return;

        HideDebugChildren(canvas);

        RectTransform champion = FindRect(canvas,"ChampionHUD");
        if (champion != null)
        {
            champion.gameObject.SetActive(true);
            SetBottomCenter(champion,new Vector2(0f,10f),new Vector2(1030f,224f));
            champion.localScale=Vector3.one;
        }

        RectTransform abilities = FindRect(canvas,"Abilities");
        if (abilities != null)
        {
            abilities.gameObject.SetActive(true);
            abilities.anchorMin=abilities.anchorMax=new Vector2(0.5f,0f);
            abilities.pivot=new Vector2(0.5f,0f);
            abilities.anchoredPosition=new Vector2(58f,91f);
            abilities.sizeDelta=new Vector2(472f,124f);
            abilities.localScale=Vector3.one;
            for(int i=0;i<abilities.childCount;i++) abilities.GetChild(i).gameObject.SetActive(true);
        }

        RectTransform vitals = FindRect(canvas,"Vitals");
        if (vitals != null)
        {
            vitals.gameObject.SetActive(true);
            vitals.anchoredPosition=new Vector2(306f,42f);
            vitals.sizeDelta=new Vector2(252f,78f);
            vitals.localScale=Vector3.one;
        }

        RectTransform items = FindRect(canvas,"Items");
        if (items != null)
        {
            items.gameObject.SetActive(true);
            items.localScale=Vector3.one;
        }

        RectTransform minimap = FindRect(canvas,"MinimapFrame");
        if (minimap != null)
        {
            minimap.gameObject.SetActive(true);
            minimap.anchorMin=minimap.anchorMax=new Vector2(1f,0f);
            minimap.pivot=new Vector2(1f,0f);
            minimap.anchoredPosition=new Vector2(-18f,18f);
            minimap.sizeDelta=new Vector2(258f,258f);
            minimap.localScale=Vector3.one;
        }

        RectTransform top = FindRect(canvas,"TopScoreboard");
        if (top != null)
        {
            top.gameObject.SetActive(true);
            top.anchorMin=top.anchorMax=new Vector2(0.5f,1f);
            top.pivot=new Vector2(0.5f,1f);
            top.anchoredPosition=new Vector2(0f,-12f);
            top.sizeDelta=new Vector2(590f,56f);
            top.localScale=Vector3.one;
        }
    }

    private static void HideDebugChildren(Transform root)
    {
        foreach(Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            if(t==null || t==root) continue;
            string n=t.name.ToLowerInvariant();
            bool debug=n.Contains("debug") || n.Contains("diagnostic") || n.Contains("performance") ||
                       n.Contains("fps") || n.Contains("leftinfopanel") || n.Contains("direction") ||
                       n.Contains("tri_lock") || n.Contains("mid_lock") || n.Contains("dir_live");
            if(debug) t.gameObject.SetActive(false);
        }
    }

    private static RectTransform FindRect(Transform root,string name)
    {
        foreach(RectTransform rect in root.GetComponentsInChildren<RectTransform>(true))
            if(rect!=null && rect.name==name) return rect;
        return null;
    }

    private static void SetBottomCenter(RectTransform rect,Vector2 position,Vector2 size)
    {
        rect.anchorMin=rect.anchorMax=new Vector2(0.5f,0f);
        rect.pivot=new Vector2(0.5f,0f);
        rect.anchoredPosition=position;
        rect.sizeDelta=size;
    }
}
