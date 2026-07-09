using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Match-facing HUD layer: minimap, scoreboard panel, kill feed and ping markers.
/// Uses runtime primitives as a production bridge until authored UI prefabs replace it.
/// </summary>
public class AOGMatchHUDRuntime : MonoBehaviour
{
    private Canvas canvas;
    private RectTransform minimapRect;
    private RectTransform iconLayer;
    private Text scoreboardText;
    private RectTransform killFeedRoot;
    private readonly Dictionary<Object, RectTransform> trackedIcons = new();
    private readonly List<Text> killFeed = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindObjectOfType<AOGMatchHUDRuntime>() != null)
            return;
        GameObject obj = new GameObject("AOG_Match_HUD_Runtime");
        obj.AddComponent<AOGMatchHUDRuntime>();
    }

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        BuildUI();
    }

    void Update()
    {
        RefreshScoreboard();
        RefreshMinimap();
        HandlePingInput();
    }

    private void BuildUI()
    {
        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 420;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        GameObject minimap = Panel("Minimap", transform, new Vector2(0f, 0f), new Vector2(290f, 290f), new Vector2(28f, 28f));
        minimapRect = minimap.GetComponent<RectTransform>();
        iconLayer = new GameObject("Icons", typeof(RectTransform)).GetComponent<RectTransform>();
        iconLayer.SetParent(minimap.transform, false);
        iconLayer.anchorMin = Vector2.zero;
        iconLayer.anchorMax = Vector2.one;
        iconLayer.offsetMin = Vector2.zero;
        iconLayer.offsetMax = Vector2.zero;

        GameObject score = Panel("Scoreboard", transform, new Vector2(0.5f, 1f), new Vector2(760f, 70f), new Vector2(0f, -10f));
        scoreboardText = Text("ScoreText", score.transform, "BLUE 0 / 0 / 0   •   00:00   •   0 / 0 / 0 RED", 24);

        GameObject feed = Panel("KillFeed", transform, new Vector2(1f, 1f), new Vector2(430f, 260f), new Vector2(-28f, -110f));
        killFeedRoot = feed.GetComponent<RectTransform>();
    }

    private void RefreshScoreboard()
    {
        GameStateManager gsm = FindObjectOfType<GameStateManager>();
        if (gsm == null || scoreboardText == null)
            return;

        float time = gsm.GetGameTime();
        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);

        TeamStats blue = gsm.GetTeamStats(TeamType.Blue);
        TeamStats red = gsm.GetTeamStats(TeamType.Red);
        if (blue == null || red == null)
            return;

        scoreboardText.text = $"BLUE {blue.Kills}/{blue.Deaths}/{blue.Objectives}   •   {minutes:00}:{seconds:00}   •   {red.Kills}/{red.Deaths}/{red.Objectives} RED";
    }

    private void RefreshMinimap()
    {
        if (iconLayer == null)
            return;

        Champion[] champions = FindObjectsByType<Champion>(FindObjectsSortMode.None);
        foreach (Champion champion in champions)
            UpdateIcon(champion, champion.transform.position, champion.Team == TeamType.Blue ? new Color(0.1f, 0.7f, 1f) : new Color(1f, 0.15f, 0.12f), 12f);

        ObjectiveManager objectives = FindObjectOfType<ObjectiveManager>();
        if (objectives != null)
        {
            if (objectives.DragonObject != null)
                UpdateIcon(objectives.DragonObject, objectives.DragonObject.transform.position, new Color(1f, 0.35f, 0.05f), 14f);
            if (objectives.MedusaObject != null)
                UpdateIcon(objectives.MedusaObject, objectives.MedusaObject.transform.position, new Color(0.25f, 1f, 0.35f), 14f);
        }
    }

    private void UpdateIcon(Object key, Vector3 world, Color color, float size)
    {
        if (!trackedIcons.TryGetValue(key, out RectTransform rect) || rect == null)
        {
            GameObject icon = new GameObject("MapIcon", typeof(RectTransform), typeof(Image));
            icon.transform.SetParent(iconLayer, false);
            rect = icon.GetComponent<RectTransform>();
            rect.sizeDelta = Vector2.one * size;
            icon.GetComponent<Image>().color = color;
            trackedIcons[key] = rect;
        }

        Vector2 normalized = WorldToMap(world);
        rect.anchorMin = rect.anchorMax = normalized;
        rect.anchoredPosition = Vector2.zero;
    }

    private Vector2 WorldToMap(Vector3 world)
    {
        const float worldExtent = 90f;
        float x = Mathf.InverseLerp(-worldExtent, worldExtent, world.x);
        float y = Mathf.InverseLerp(-worldExtent, worldExtent, world.z);
        return new Vector2(x, y);
    }

    public void AddKillFeed(string killer, string victim)
    {
        Text entry = Text("FeedEntry", killFeedRoot, $"{killer}  ✦  {victim}", 20);
        RectTransform rect = entry.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.sizeDelta = new Vector2(390f, 34f);
        rect.anchoredPosition = new Vector2(-16f, -16f - killFeed.Count * 36f);
        killFeed.Insert(0, entry);

        while (killFeed.Count > 6)
        {
            Text old = killFeed[killFeed.Count - 1];
            killFeed.RemoveAt(killFeed.Count - 1);
            if (old != null) Destroy(old.gameObject);
        }
    }

    private void HandlePingInput()
    {
        if (!Input.GetKeyDown(KeyCode.G) || Camera.main == null)
            return;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
            CreatePing(hit.point);
    }

    public void CreatePing(Vector3 world)
    {
        GameObject ping = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ping.name = "AOG_World_Ping";
        ping.transform.position = world + Vector3.up * 0.08f;
        ping.transform.localScale = new Vector3(2f, 0.03f, 2f);
        Collider col = ping.GetComponent<Collider>();
        if (col != null) Destroy(col);

        AOGAudioDirectorRuntime.Instance?.PlayCue(AOGAudioCue.UIConfirm);
        Destroy(ping, 2.2f);
    }

    private static GameObject Panel(string name, Transform parent, Vector2 anchor, Vector2 size, Vector2 pos)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.sizeDelta = size;
        rect.anchoredPosition = pos;
        obj.GetComponent<Image>().color = new Color(0.015f, 0.025f, 0.055f, 0.88f);
        return obj;
    }

    private static Text Text(string name, Transform parent, string value, int size)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Text));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Text text = obj.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = value;
        text.fontSize = size;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }
}
