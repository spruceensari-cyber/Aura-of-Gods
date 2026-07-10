using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(1800)]
public class AOGScoreboardAndAnnouncerRuntime : MonoBehaviour
{
    public static AOGScoreboardAndAnnouncerRuntime Instance { get; private set; }

    private Canvas canvas;
    private Text blueScoreText;
    private Text redScoreText;
    private Text centerText;
    private Font font;
    private int blueKills;
    private int redKills;
    private readonly Dictionary<AOGCharacterStats, bool> deadState = new Dictionary<AOGCharacterStats, bool>();
    private readonly Dictionary<TowerHealth, bool> towerAlive = new Dictionary<TowerHealth, bool>();
    private float messageUntil;
    private float nextScan;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindFirstObjectByType<AOGScoreboardAndAnnouncerRuntime>() != null)
            return;
        GameObject host = new GameObject("AOG_Scoreboard_And_Announcer");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGScoreboardAndAnnouncerRuntime>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        BuildUi();
    }

    private void Update()
    {
        if (Time.unscaledTime >= nextScan)
        {
            nextScan = Time.unscaledTime + 0.25f;
            ScanHeroes();
            ScanTowers();
        }

        blueScoreText.text = "BLUE  " + blueKills;
        redScoreText.text = redKills + "  RED";
        if (centerText != null && Time.unscaledTime > messageUntil)
            centerText.text = string.Empty;
    }

    public void ShowExternalMessage(string message, Color color, float duration)
    {
        ShowMessage(message,color,duration);
    }

    private void ScanHeroes()
    {
        foreach (AOGCharacterStats hero in FindObjectsByType<AOGCharacterStats>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (hero == null) continue;
            bool dead = hero.IsDead;
            if (!deadState.TryGetValue(hero, out bool wasDead))
            {
                deadState[hero] = dead;
                continue;
            }

            if (!wasDead && dead)
            {
                if (hero.team == MinionTeam.Blue) redKills++; else blueKills++;
                ShowMessage(hero.team == MinionTeam.Blue ? "ALLY SLAIN" : "ENEMY SLAIN", hero.team == MinionTeam.Blue ? new Color(1f,0.22f,0.22f) : new Color(0.22f,0.72f,1f), 2.2f);
            }
            deadState[hero] = dead;
        }
    }

    private void ScanTowers()
    {
        foreach (TowerHealth tower in FindObjectsByType<TowerHealth>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (tower == null) continue;
            bool alive = tower.hp > 0f && tower.gameObject.activeInHierarchy;
            if (!towerAlive.TryGetValue(tower, out bool wasAlive))
            {
                towerAlive[tower] = alive;
                continue;
            }

            if (wasAlive && !alive)
            {
                string text = tower.towerTeam == MinionTeam.Blue ? "BLUE TOWER DESTROYED" : "RED TOWER DESTROYED";
                Color color = tower.towerTeam == MinionTeam.Blue ? new Color(0.20f,0.62f,1f) : new Color(1f,0.20f,0.16f);
                ShowMessage(text, color, 2.8f);
            }
            towerAlive[tower] = alive;
        }
    }

    private void ShowMessage(string message, Color color, float duration)
    {
        if (centerText == null)
            return;
        centerText.text = message;
        centerText.color = color;
        messageUntil = Time.unscaledTime + duration;
    }

    private void BuildUi()
    {
        GameObject canvasObject = new GameObject("ScoreboardCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvasObject.transform.SetParent(transform, false);
        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 2800;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f,1080f);
        scaler.matchWidthOrHeight = 0.5f;

        blueScoreText = CreateText(canvasObject.transform, "BlueScore", new Vector2(-190f,-34f), new Vector2(220f,46f), 22, TextAnchor.MiddleCenter, new Color(0.22f,0.62f,1f));
        blueScoreText.rectTransform.anchorMin = blueScoreText.rectTransform.anchorMax = new Vector2(0.5f,1f);
        blueScoreText.text = "BLUE  0";

        redScoreText = CreateText(canvasObject.transform, "RedScore", new Vector2(190f,-34f), new Vector2(220f,46f), 22, TextAnchor.MiddleCenter, new Color(1f,0.22f,0.22f));
        redScoreText.rectTransform.anchorMin = redScoreText.rectTransform.anchorMax = new Vector2(0.5f,1f);
        redScoreText.text = "0  RED";

        centerText = CreateText(canvasObject.transform, "Announcer", new Vector2(0f,-120f), new Vector2(900f,70f), 34, TextAnchor.MiddleCenter, Color.white);
        centerText.rectTransform.anchorMin = centerText.rectTransform.anchorMax = new Vector2(0.5f,1f);
    }

    private Text CreateText(Transform parent, string name, Vector2 anchoredPosition, Vector2 size, int fontSize, TextAnchor anchor, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f,0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        Text text = go.GetComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.fontStyle = FontStyle.Bold;
        text.alignment = anchor;
        text.color = color;
        text.raycastTarget = false;
        Outline outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(0f,0f,0f,0.92f);
        outline.effectDistance = new Vector2(2f,-2f);
        return text;
    }
}
