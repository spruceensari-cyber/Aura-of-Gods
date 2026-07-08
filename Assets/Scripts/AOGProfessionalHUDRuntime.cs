using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class AOGProfessionalHUDRuntime : MonoBehaviour
{
    private const string ManagerName = "AOG_Professional_HUD_Runtime";
    private const string CanvasName = "AOG_Professional_HUD_Canvas";

    private static AOGProfessionalHUDRuntime instance;

    private Canvas canvas;
    private Text championNameText;
    private Text championTitleText;
    private Text timerText;
    private Text scoreText;
    private Text resourceText;
    private Text hpText;
    private Image hpFill;
    private Image manaFill;
    private RawImage portraitImage;
    private Text[] abilityLabels = new Text[4];
    private Image[] abilityAccents = new Image[4];
    private AOGChampionDefinition champion;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        EnsureManager();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureManager();
    }

    public static void RefreshAll()
    {
        if (instance != null)
            instance.RefreshChampion();
    }

    private static void EnsureManager()
    {
        if (instance != null)
            return;

        AOGProfessionalHUDRuntime found = Object.FindAnyObjectByType<AOGProfessionalHUDRuntime>();
        if (found != null)
        {
            instance = found;
            return;
        }

        GameObject manager = new GameObject(ManagerName);
        instance = manager.AddComponent<AOGProfessionalHUDRuntime>();
    }

    private void Start()
    {
        instance = this;
        Build();
        RefreshChampion();
    }

    private void Update()
    {
        UpdateTimer();
        UpdatePlayerBars();
    }

    private void Build()
    {
        if (canvas != null)
            Destroy(canvas.gameObject);

        GameObject canvasObject = new GameObject(CanvasName);
        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 2000;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasObject.AddComponent<GraphicRaycaster>();

        RectTransform root = FullRect(canvasObject.transform, "HUD_Root");
        CreateTopScoreboard(root);
        CreateBottomChampionPanel(root);
        CreateAbilityBar(root);
        CreateObjectivePanel(root);
        CreateMinimapFrame(root);
    }

    private void CreateTopScoreboard(RectTransform root)
    {
        RectTransform panel = Panel(root, "Top_Scoreboard", new Vector2(0.35f, 1f), new Vector2(0.65f, 1f), new Vector2(0f, -70f), new Vector2(0f, -14f), new Color(0.015f, 0.018f, 0.024f, 0.88f));

        timerText = Label(panel, "00:00", 24, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
        timerText.rectTransform.anchorMin = new Vector2(0.4f, 0f);
        timerText.rectTransform.anchorMax = new Vector2(0.6f, 1f);
        timerText.rectTransform.offsetMin = Vector2.zero;
        timerText.rectTransform.offsetMax = Vector2.zero;

        scoreText = Label(panel, "0  -  0", 21, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.92f, 0.78f, 0.48f));
        scoreText.rectTransform.anchorMin = new Vector2(0f, 0f);
        scoreText.rectTransform.anchorMax = new Vector2(1f, 1f);
        scoreText.rectTransform.offsetMin = Vector2.zero;
        scoreText.rectTransform.offsetMax = Vector2.zero;
    }

    private void CreateBottomChampionPanel(RectTransform root)
    {
        RectTransform panel = Panel(root, "Champion_Status", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(24f, 24f), new Vector2(442f, 196f), new Color(0.015f, 0.018f, 0.024f, 0.92f));

        portraitImage = RawImage(panel, "Portrait", null);
        portraitImage.rectTransform.anchorMin = new Vector2(0f, 0f);
        portraitImage.rectTransform.anchorMax = new Vector2(0f, 1f);
        portraitImage.rectTransform.offsetMin = new Vector2(12f, 12f);
        portraitImage.rectTransform.offsetMax = new Vector2(150f, -12f);

        championNameText = Label(panel, "RAGNAR", 24, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
        championNameText.rectTransform.anchorMin = new Vector2(0f, 1f);
        championNameText.rectTransform.anchorMax = new Vector2(1f, 1f);
        championNameText.rectTransform.offsetMin = new Vector2(168f, -52f);
        championNameText.rectTransform.offsetMax = new Vector2(-18f, -14f);

        championTitleText = Label(panel, "Titan of Ash", 15, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.92f, 0.78f, 0.48f));
        championTitleText.rectTransform.anchorMin = new Vector2(0f, 1f);
        championTitleText.rectTransform.anchorMax = new Vector2(1f, 1f);
        championTitleText.rectTransform.offsetMin = new Vector2(168f, -82f);
        championTitleText.rectTransform.offsetMax = new Vector2(-18f, -52f);

        RectTransform hpTrack = Panel(panel, "HP_Track", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(168f, 78f), new Vector2(-18f, 104f), new Color(0.08f, 0.1f, 0.08f, 0.96f));
        hpFill = Panel(hpTrack, "HP_Fill", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.16f, 0.82f, 0.32f, 0.98f)).GetComponent<Image>();

        RectTransform manaTrack = Panel(panel, "Mana_Track", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(168f, 46f), new Vector2(-18f, 68f), new Color(0.06f, 0.075f, 0.12f, 0.96f));
        manaFill = Panel(manaTrack, "Mana_Fill", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.16f, 0.44f, 1f, 0.98f)).GetComponent<Image>();

        hpText = Label(panel, "HP", 13, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
        hpText.rectTransform.anchorMin = new Vector2(0f, 0f);
        hpText.rectTransform.anchorMax = new Vector2(1f, 0f);
        hpText.rectTransform.offsetMin = new Vector2(168f, 108f);
        hpText.rectTransform.offsetMax = new Vector2(-18f, 136f);

        resourceText = Label(panel, "LV 1  |  500 G", 13, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.92f, 0.78f, 0.48f));
        resourceText.rectTransform.anchorMin = new Vector2(0f, 0f);
        resourceText.rectTransform.anchorMax = new Vector2(1f, 0f);
        resourceText.rectTransform.offsetMin = new Vector2(168f, 14f);
        resourceText.rectTransform.offsetMax = new Vector2(-18f, 40f);
    }

    private void CreateAbilityBar(RectTransform root)
    {
        RectTransform bar = Panel(root, "Ability_Bar", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-330f, 24f), new Vector2(330f, 136f), new Color(0.01f, 0.012f, 0.016f, 0.9f));

        string[] keys = { "Q", "W", "E", "R" };
        for (int i = 0; i < 4; i++)
        {
            RectTransform slot = Panel(bar, "Ability_" + keys[i], new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(20f + i * 160f, 14f), new Vector2(140f + i * 160f, -14f), new Color(0.035f, 0.04f, 0.05f, 0.96f));
            abilityAccents[i] = Panel(slot, "Accent", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 5f), Color.white).GetComponent<Image>();

            Text key = Label(slot, keys[i], 26, FontStyle.Bold, TextAnchor.UpperLeft, Color.white);
            key.rectTransform.anchorMin = Vector2.zero;
            key.rectTransform.anchorMax = Vector2.one;
            key.rectTransform.offsetMin = new Vector2(12f, 8f);
            key.rectTransform.offsetMax = new Vector2(-8f, -8f);

            abilityLabels[i] = Label(slot, "", 12, FontStyle.Bold, TextAnchor.LowerLeft, new Color(0.84f, 0.86f, 0.88f));
            abilityLabels[i].rectTransform.anchorMin = Vector2.zero;
            abilityLabels[i].rectTransform.anchorMax = Vector2.one;
            abilityLabels[i].rectTransform.offsetMin = new Vector2(12f, 8f);
            abilityLabels[i].rectTransform.offsetMax = new Vector2(-10f, -42f);
        }
    }

    private void CreateObjectivePanel(RectTransform root)
    {
        RectTransform panel = Panel(root, "Objective_Info", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-352f, -80f), new Vector2(-24f, -24f), new Color(0.015f, 0.018f, 0.024f, 0.82f));
        Text label = Label(panel, "DRAGON  05:00   |   TITAN  07:00", 14, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.92f, 0.78f, 0.48f));
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = Vector2.zero;
        label.rectTransform.offsetMax = Vector2.zero;
    }

    private void CreateMinimapFrame(RectTransform root)
    {
        RectTransform map = Panel(root, "Stylized_Minimap", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-268f, 24f), new Vector2(-24f, 268f), new Color(0.012f, 0.018f, 0.022f, 0.94f));
        CreateMiniLine(map, new Vector2(0.5f, 0.5f), 290f, 45f, new Color(0.76f, 0.66f, 0.44f, 0.7f));
        CreateMiniLine(map, new Vector2(0.24f, 0.74f), 210f, 8f, new Color(0.36f, 0.6f, 0.95f, 0.75f));
        CreateMiniLine(map, new Vector2(0.76f, 0.26f), 210f, 8f, new Color(0.95f, 0.32f, 0.28f, 0.75f));
        CreateMiniDot(map, new Vector2(0.18f, 0.18f), new Color(0.2f, 0.56f, 1f));
        CreateMiniDot(map, new Vector2(0.82f, 0.82f), new Color(1f, 0.22f, 0.18f));
    }

    private void RefreshChampion()
    {
        champion = AOGChampionCatalog.GetSelectedOrDefault();
        Texture2D portrait = AOGChampionCatalog.LoadPortrait(champion);
        portraitImage.texture = portrait;
        portraitImage.color = portrait == null ? champion.accent : Color.white;
        championNameText.text = champion.displayName.ToUpperInvariant();
        championTitleText.text = champion.title;
        championTitleText.color = champion.accent;

        for (int i = 0; i < abilityLabels.Length; i++)
        {
            abilityLabels[i].text = champion.abilityNames[i + 1].ToUpperInvariant();
            abilityAccents[i].color = champion.accent;
        }

        AOGChampionVisualApplier.ApplyToCurrentPlayer(champion);
    }

    private void UpdateTimer()
    {
        float t = Time.timeSinceLevelLoad;
        int minutes = Mathf.FloorToInt(t / 60f);
        int seconds = Mathf.FloorToInt(t % 60f);
        timerText.text = minutes.ToString("00") + ":" + seconds.ToString("00");

        GameStateManager gameState = Object.FindAnyObjectByType<GameStateManager>();
        if (gameState != null)
        {
            TeamStats blue = gameState.GetTeamStats(TeamType.Blue);
            TeamStats red = gameState.GetTeamStats(TeamType.Red);
            int blueKills = blue != null ? blue.Kills : 0;
            int redKills = red != null ? red.Kills : 0;
            scoreText.text = blueKills + "  -  " + redKills;
        }
    }

    private void UpdatePlayerBars()
    {
        GameObject player = AOGChampionVisualApplier.FindPlayerObject();
        if (player == null)
            return;

        AOGCharacterStats stats = player.GetComponent<AOGCharacterStats>();
        if (stats != null)
        {
            float hpPercent = Mathf.Clamp01(stats.hp / Mathf.Max(1f, stats.maxHp));
            hpFill.rectTransform.anchorMax = new Vector2(hpPercent, 1f);
            hpText.text = Mathf.RoundToInt(stats.hp) + " / " + Mathf.RoundToInt(stats.maxHp) + " HP";
            manaFill.rectTransform.anchorMax = new Vector2(0.82f, 1f);
            resourceText.text = champion.displayName + "  |  " + Mathf.RoundToInt(stats.attackDamage) + " AD";
            return;
        }

        Champion championComponent = player.GetComponent<Champion>();
        if (championComponent != null)
        {
            hpFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(championComponent.CurrentHealth / 500f), 1f);
            manaFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(championComponent.CurrentMana / 300f), 1f);
            hpText.text = Mathf.RoundToInt(championComponent.CurrentHealth) + " HP";
        }
    }

    private void CreateMiniLine(Transform parent, Vector2 normalizedCenter, float length, float angle, Color color)
    {
        RectTransform line = Panel(parent, "Map_Line", new Vector2(normalizedCenter.x, normalizedCenter.y), new Vector2(normalizedCenter.x, normalizedCenter.y), new Vector2(-length * 0.5f, -2f), new Vector2(length * 0.5f, 2f), color);
        line.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void CreateMiniDot(Transform parent, Vector2 normalizedCenter, Color color)
    {
        Panel(parent, "Map_Dot", new Vector2(normalizedCenter.x, normalizedCenter.y), new Vector2(normalizedCenter.x, normalizedCenter.y), new Vector2(-8f, -8f), new Vector2(8f, 8f), color);
    }

    private RectTransform FullRect(Transform parent, string name)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return rect;
    }

    private RectTransform Panel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        Image image = obj.AddComponent<Image>();
        image.color = color;
        return rect;
    }

    private Text Label(Transform parent, string text, int size, FontStyle style, TextAnchor anchor, Color color)
    {
        GameObject obj = new GameObject("Text");
        obj.transform.SetParent(parent, false);
        Text label = obj.AddComponent<Text>();
        label.text = text;
        label.font = Font.CreateDynamicFontFromOSFont("Arial", size);
        label.fontSize = size;
        label.fontStyle = style;
        label.alignment = anchor;
        label.color = color;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Truncate;
        return label;
    }

    private RawImage RawImage(Transform parent, string name, Texture texture)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        RawImage image = obj.AddComponent<RawImage>();
        image.texture = texture;
        image.color = Color.white;
        return image;
    }
}
