using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class AOGChampionSelectRuntime : MonoBehaviour
{
    private const string ManagerName = "AOG_Champion_Select_Runtime";
    private const string CanvasName = "AOG_Champion_Select_Canvas";

    private Canvas canvas;
    private RectTransform root;
    private RawImage portrait;
    private Text titleText;
    private Text subtitleText;
    private Text quoteText;
    private Text roleText;
    private Text laneText;
    private Text difficultyText;
    private Text tagsText;
    private Text[] abilityTexts;
    private Image[] statFills;
    private Button[] cardButtons;
    private AOGChampionDefinition selected;
    private float previousTimeScale = 1f;

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

    private static void EnsureManager()
    {
        if (Object.FindAnyObjectByType<AOGChampionSelectRuntime>() != null)
            return;

        GameObject manager = new GameObject(ManagerName);
        manager.AddComponent<AOGChampionSelectRuntime>();
    }

    private void Start()
    {
        selected = AOGChampionCatalog.GetSelectedOrDefault();
        Build();
        Show();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
            Show();
    }

    public void Show()
    {
        if (canvas == null)
            Build();

        selected = AOGChampionCatalog.GetSelectedOrDefault();
        UpdateDetails(selected);
        canvas.gameObject.SetActive(true);
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
    }

    private void HideAndLock()
    {
        if (selected == null)
            selected = AOGChampionCatalog.All[0];

        PlayerPrefs.SetString(AOGChampionCatalog.PlayerPrefsSelectedChampion, selected.id);
        PlayerPrefs.Save();
        AOGChampionVisualApplier.ApplyToCurrentPlayer(selected);
        AOGProfessionalHUDRuntime.RefreshAll();

        canvas.gameObject.SetActive(false);
        Time.timeScale = previousTimeScale <= 0f ? 1f : previousTimeScale;
    }

    private void Build()
    {
        if (canvas != null)
            Destroy(canvas.gameObject);

        GameObject canvasObject = new GameObject(CanvasName);
        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5000;
        canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObject.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920f, 1080f);
        canvasObject.AddComponent<GraphicRaycaster>();
        EnsureEventSystem();

        root = FullRect(canvasObject.transform, "Root");
        Image background = root.gameObject.AddComponent<Image>();
        background.color = new Color(0.015f, 0.017f, 0.021f, 0.96f);

        CreateTopBar();
        CreateCardGrid();
        CreateDetailsPanel();
        CreateFooter();
    }

    private void EnsureEventSystem()
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            GameObject eventObject = new GameObject("AOG_EventSystem");
            eventSystem = eventObject.AddComponent<EventSystem>();
        }

        if (eventSystem.GetComponent<BaseInputModule>() != null)
            return;

        System.Type inputSystemModuleType = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
        if (inputSystemModuleType != null)
        {
            eventSystem.gameObject.AddComponent(inputSystemModuleType);
            return;
        }

        eventSystem.gameObject.AddComponent<StandaloneInputModule>();
    }

    private void CreateTopBar()
    {
        RectTransform bar = Panel(root, "Top_Bar", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -112f), new Vector2(0f, 0f), new Color(0.025f, 0.028f, 0.034f, 0.92f));
        Text title = Label(bar, "AURA OF GODS", 40, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
        title.rectTransform.anchorMin = new Vector2(0f, 0f);
        title.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        title.rectTransform.offsetMin = new Vector2(44f, 0f);
        title.rectTransform.offsetMax = new Vector2(0f, 0f);

        Text mode = Label(bar, "CHAMPION SELECT", 24, FontStyle.Bold, TextAnchor.MiddleRight, new Color(0.92f, 0.78f, 0.48f));
        mode.rectTransform.anchorMin = new Vector2(0.5f, 0f);
        mode.rectTransform.anchorMax = new Vector2(1f, 1f);
        mode.rectTransform.offsetMin = new Vector2(0f, 0f);
        mode.rectTransform.offsetMax = new Vector2(-44f, 0f);
    }

    private void CreateCardGrid()
    {
        RectTransform panel = Panel(root, "Champion_Cards", new Vector2(0f, 0f), new Vector2(0.48f, 1f), new Vector2(34f, 34f), new Vector2(-10f, -138f), new Color(0.025f, 0.03f, 0.037f, 0.88f));
        Text header = Label(panel, "ROSTER", 24, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.92f, 0.78f, 0.48f));
        header.rectTransform.anchorMin = new Vector2(0f, 1f);
        header.rectTransform.anchorMax = new Vector2(1f, 1f);
        header.rectTransform.offsetMin = new Vector2(24f, -58f);
        header.rectTransform.offsetMax = new Vector2(-24f, -14f);

        AOGChampionDefinition[] champions = AOGChampionCatalog.All;
        cardButtons = new Button[champions.Length];

        float startY = -82f;
        float cardW = 258f;
        float cardH = 188f;
        float gap = 14f;
        int columns = 3;

        for (int i = 0; i < champions.Length; i++)
        {
            AOGChampionDefinition champion = champions[i];
            int col = i % columns;
            int row = i / columns;

            RectTransform card = Panel(panel, "Card_" + champion.id, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f + col * (cardW + gap), startY - row * (cardH + gap) - cardH), new Vector2(24f + col * (cardW + gap) + cardW, startY - row * (cardH + gap)), new Color(0.045f, 0.05f, 0.06f, 0.95f));
            Button button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = card.GetComponent<Image>();
            cardButtons[i] = button;

            RawImage image = RawImage(card, "Portrait", AOGChampionCatalog.LoadPortrait(champion));
            image.rectTransform.anchorMin = new Vector2(0f, 0.22f);
            image.rectTransform.anchorMax = new Vector2(1f, 1f);
            image.rectTransform.offsetMin = new Vector2(0f, 0f);
            image.rectTransform.offsetMax = new Vector2(0f, 0f);
            image.color = image.texture == null ? champion.accent : Color.white;

            Image accent = Panel(card, "Accent", new Vector2(0f, 0f), new Vector2(0.035f, 1f), Vector2.zero, Vector2.zero, champion.accent).GetComponent<Image>();
            accent.color = new Color(champion.accent.r, champion.accent.g, champion.accent.b, 0.95f);

            Text name = Label(card, champion.displayName.ToUpperInvariant(), 20, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
            name.rectTransform.anchorMin = new Vector2(0f, 0f);
            name.rectTransform.anchorMax = new Vector2(1f, 0.22f);
            name.rectTransform.offsetMin = new Vector2(16f, 24f);
            name.rectTransform.offsetMax = new Vector2(-10f, 0f);

            Text role = Label(card, champion.role, 13, FontStyle.Normal, TextAnchor.MiddleLeft, new Color(0.78f, 0.8f, 0.82f));
            role.rectTransform.anchorMin = new Vector2(0f, 0f);
            role.rectTransform.anchorMax = new Vector2(1f, 0.16f);
            role.rectTransform.offsetMin = new Vector2(16f, 0f);
            role.rectTransform.offsetMax = new Vector2(-10f, 7f);

            button.onClick.AddListener(() =>
            {
                selected = champion;
                UpdateDetails(champion);
            });
        }
    }

    private void CreateDetailsPanel()
    {
        RectTransform panel = Panel(root, "Details", new Vector2(0.48f, 0f), new Vector2(1f, 1f), new Vector2(10f, 34f), new Vector2(-34f, -138f), new Color(0.02f, 0.024f, 0.031f, 0.92f));

        portrait = RawImage(panel, "Hero_Portrait", null);
        portrait.rectTransform.anchorMin = new Vector2(0f, 0.25f);
        portrait.rectTransform.anchorMax = new Vector2(0.54f, 1f);
        portrait.rectTransform.offsetMin = new Vector2(22f, 20f);
        portrait.rectTransform.offsetMax = new Vector2(-18f, -22f);

        RectTransform info = Panel(panel, "Info", new Vector2(0.54f, 0f), new Vector2(1f, 1f), new Vector2(0f, 20f), new Vector2(-22f, -22f), new Color(0.018f, 0.021f, 0.028f, 0.72f));

        titleText = Label(info, "", 44, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
        titleText.rectTransform.anchorMin = new Vector2(0f, 1f);
        titleText.rectTransform.anchorMax = new Vector2(1f, 1f);
        titleText.rectTransform.offsetMin = new Vector2(24f, -82f);
        titleText.rectTransform.offsetMax = new Vector2(-24f, -20f);

        subtitleText = Label(info, "", 21, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
        subtitleText.rectTransform.anchorMin = new Vector2(0f, 1f);
        subtitleText.rectTransform.anchorMax = new Vector2(1f, 1f);
        subtitleText.rectTransform.offsetMin = new Vector2(24f, -122f);
        subtitleText.rectTransform.offsetMax = new Vector2(-24f, -86f);

        quoteText = Label(info, "", 17, FontStyle.Italic, TextAnchor.UpperLeft, new Color(0.75f, 0.77f, 0.8f));
        quoteText.rectTransform.anchorMin = new Vector2(0f, 1f);
        quoteText.rectTransform.anchorMax = new Vector2(1f, 1f);
        quoteText.rectTransform.offsetMin = new Vector2(24f, -182f);
        quoteText.rectTransform.offsetMax = new Vector2(-24f, -130f);

        roleText = SmallInfo(info, "Role", -238f);
        laneText = SmallInfo(info, "Lane", -284f);
        difficultyText = SmallInfo(info, "Difficulty", -330f);
        tagsText = SmallInfo(info, "Tags", -376f);

        statFills = new Image[5];
        string[] statNames = { "Durability", "Attack", "Power", "Control", "Mobility" };
        for (int i = 0; i < statFills.Length; i++)
        {
            RectTransform row = Panel(info, "Stat_" + i, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, -445f - i * 36f), new Vector2(-24f, -416f - i * 36f), new Color(0f, 0f, 0f, 0f));
            Text label = Label(row, statNames[i], 13, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.78f, 0.8f, 0.82f));
            label.rectTransform.anchorMin = new Vector2(0f, 0f);
            label.rectTransform.anchorMax = new Vector2(0.34f, 1f);
            label.rectTransform.offsetMin = Vector2.zero;
            label.rectTransform.offsetMax = Vector2.zero;

            RectTransform track = Panel(row, "Track", new Vector2(0.36f, 0.25f), new Vector2(1f, 0.75f), Vector2.zero, Vector2.zero, new Color(0.11f, 0.12f, 0.13f, 0.9f));
            RectTransform fill = Panel(track, "Fill", new Vector2(0f, 0f), new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero, Color.white);
            statFills[i] = fill.GetComponent<Image>();
        }

        abilityTexts = new Text[5];
        Text abilityHeader = Label(info, "ABILITIES", 18, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.92f, 0.78f, 0.48f));
        abilityHeader.rectTransform.anchorMin = new Vector2(0f, 0f);
        abilityHeader.rectTransform.anchorMax = new Vector2(1f, 0f);
        abilityHeader.rectTransform.offsetMin = new Vector2(24f, 250f);
        abilityHeader.rectTransform.offsetMax = new Vector2(-24f, 285f);

        for (int i = 0; i < abilityTexts.Length; i++)
        {
            abilityTexts[i] = Label(info, "", 14, FontStyle.Normal, TextAnchor.UpperLeft, new Color(0.86f, 0.88f, 0.9f));
            abilityTexts[i].rectTransform.anchorMin = new Vector2(0f, 0f);
            abilityTexts[i].rectTransform.anchorMax = new Vector2(1f, 0f);
            abilityTexts[i].rectTransform.offsetMin = new Vector2(24f, 175f - i * 42f);
            abilityTexts[i].rectTransform.offsetMax = new Vector2(-24f, 228f - i * 42f);
        }

        Button lockButton = Button(info, "LOCK IN", new Vector2(0.06f, 0f), new Vector2(0.94f, 0f), new Vector2(0f, 24f), new Vector2(0f, 86f), new Color(0.92f, 0.78f, 0.48f));
        lockButton.onClick.AddListener(HideAndLock);
    }

    private void CreateFooter()
    {
        RectTransform footer = Panel(root, "Footer", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 28f), new Color(0.01f, 0.012f, 0.016f, 0.95f));
        Text hint = Label(footer, "Press C during Play Mode to reopen champion select", 13, FontStyle.Normal, TextAnchor.MiddleCenter, new Color(0.55f, 0.58f, 0.62f));
        hint.rectTransform.anchorMin = Vector2.zero;
        hint.rectTransform.anchorMax = Vector2.one;
        hint.rectTransform.offsetMin = Vector2.zero;
        hint.rectTransform.offsetMax = Vector2.zero;
    }

    private Text SmallInfo(RectTransform parent, string label, float y)
    {
        Text text = Label(parent, "", 16, FontStyle.Bold, TextAnchor.MiddleLeft, Color.white);
        text.rectTransform.anchorMin = new Vector2(0f, 1f);
        text.rectTransform.anchorMax = new Vector2(1f, 1f);
        text.rectTransform.offsetMin = new Vector2(24f, y - 34f);
        text.rectTransform.offsetMax = new Vector2(-24f, y);

        Text labelText = Label(text.rectTransform, label.ToUpperInvariant(), 11, FontStyle.Bold, TextAnchor.MiddleLeft, new Color(0.55f, 0.58f, 0.62f));
        labelText.rectTransform.anchorMin = new Vector2(0f, 0f);
        labelText.rectTransform.anchorMax = new Vector2(0.32f, 1f);
        labelText.rectTransform.offsetMin = Vector2.zero;
        labelText.rectTransform.offsetMax = Vector2.zero;

        text.rectTransform.offsetMin += new Vector2(132f, 0f);
        return text;
    }

    private void UpdateDetails(AOGChampionDefinition champion)
    {
        if (champion == null)
            return;

        selected = champion;
        Texture2D texture = AOGChampionCatalog.LoadPortrait(champion);
        portrait.texture = texture;
        portrait.color = texture == null ? champion.accent : Color.white;

        titleText.text = champion.displayName.ToUpperInvariant();
        titleText.color = Color.Lerp(Color.white, champion.accent, 0.35f);
        subtitleText.text = champion.title.ToUpperInvariant();
        subtitleText.color = champion.accent;
        quoteText.text = "\"" + champion.quote + "\"";
        roleText.text = champion.role;
        laneText.text = champion.lane;
        difficultyText.text = champion.difficulty;
        tagsText.text = string.Join("  /  ", champion.tags);

        for (int i = 0; i < statFills.Length; i++)
        {
            float value = champion.statBars != null && i < champion.statBars.Length ? champion.statBars[i] : 50f;
            statFills[i].rectTransform.anchorMax = new Vector2(Mathf.Clamp01(value / 100f), 1f);
            statFills[i].color = champion.accent;
        }

        for (int i = 0; i < abilityTexts.Length; i++)
        {
            string key = champion.abilityKeys[i];
            string name = champion.abilityNames[i];
            string body = champion.abilityDescriptions[i];
            abilityTexts[i].text = key + " - " + name + "\n" + body;
        }

        for (int i = 0; i < cardButtons.Length; i++)
        {
            Image image = cardButtons[i].GetComponent<Image>();
            image.color = AOGChampionCatalog.All[i] == champion ? new Color(champion.accent.r, champion.accent.g, champion.accent.b, 0.38f) : new Color(0.045f, 0.05f, 0.06f, 0.95f);
        }
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

    private Button Button(Transform parent, string text, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Color color)
    {
        RectTransform rect = Panel(parent, "Button_" + text, anchorMin, anchorMax, offsetMin, offsetMax, color);
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = rect.GetComponent<Image>();

        Text label = Label(rect, text, 19, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(0.05f, 0.045f, 0.035f));
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = Vector2.zero;
        label.rectTransform.offsetMax = Vector2.zero;

        return button;
    }
}
