using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class AOGCombatHUDRuntimeBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        EnsureHud();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureHud();
    }

    private static void EnsureHud()
    {
        if (Object.FindFirstObjectByType<AOGCombatHUDRuntime>() != null)
            return;

        HUDManager existingHud = Object.FindFirstObjectByType<HUDManager>();
        if (existingHud != null && existingHud.isActiveAndEnabled)
            return;

        GameObject host = new GameObject("AOG_Runtime_CombatHUD");
        Object.DontDestroyOnLoad(host);
        host.AddComponent<AOGCombatHUDRuntime>();
    }
}

public class AOGCombatHUDRuntime : MonoBehaviour
{
    private readonly Color panelColor = new Color(0.025f, 0.04f, 0.075f, 0.94f);
    private readonly Color panelEdgeColor = new Color(0.13f, 0.28f, 0.48f, 0.92f);
    private readonly Color healthColor = new Color(0.12f, 0.78f, 0.44f, 1f);
    private readonly Color accentColor = new Color(0.28f, 0.56f, 1f, 1f);
    private readonly Color ultimateColor = new Color(0.68f, 0.32f, 1f, 1f);

    private Canvas canvas;
    private Font font;
    private AOGCharacterStats stats;
    private LyraSkillSet lyra;
    private Text championName;
    private Text healthText;
    private Image healthFill;
    private Text timerText;
    private readonly Image[] cooldownFills = new Image[4];
    private readonly Text[] cooldownTexts = new Text[4];
    private readonly float[] cooldownRatios = new float[4];
    private float matchStartTime;
    private float nextPlayerSearchTime;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        matchStartTime = Time.unscaledTime;
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        BuildHud();
        FindPlayer();
    }

    private void Update()
    {
        if (stats == null || !stats.gameObject.activeInHierarchy)
        {
            if (Time.unscaledTime >= nextPlayerSearchTime)
            {
                nextPlayerSearchTime = Time.unscaledTime + 0.5f;
                FindPlayer();
            }
        }

        UpdateTimer();
        UpdateHealth();
        UpdateCooldowns();
    }

    private void FindPlayer()
    {
        AOGPlayerMOBAController[] players = FindObjectsByType<AOGPlayerMOBAController>(FindObjectsSortMode.None);
        AOGPlayerMOBAController selected = null;

        foreach (AOGPlayerMOBAController player in players)
        {
            if (player == null || !player.gameObject.activeInHierarchy)
                continue;

            if (selected == null)
                selected = player;

            if (player.gameObject.name.ToLowerInvariant().Contains("lyra"))
            {
                selected = player;
                break;
            }
        }

        if (selected == null)
            return;

        stats = selected.GetComponent<AOGCharacterStats>();
        lyra = selected.GetComponent<LyraSkillSet>();
        championName.text = lyra != null ? "LYRA" : selected.gameObject.name.ToUpperInvariant();
    }

    private void BuildHud()
    {
        GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvasObject.transform.SetParent(transform, false);

        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform topStrip = CreatePanel("TopStrip", canvas.transform, panelColor, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(520f, 52f));
        CreateBorder(topStrip, panelEdgeColor);
        CreateText("BlueScore", topStrip, "BLUE   0 / 0 / 0", 20, TextAnchor.MiddleLeft, new Vector2(-160f, 0f), new Vector2(170f, 42f), accentColor);
        timerText = CreateText("Timer", topStrip, "00:00", 24, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(100f, 42f), Color.white);
        CreateText("RedScore", topStrip, "0 / 0 / 0   RED", 20, TextAnchor.MiddleRight, new Vector2(160f, 0f), new Vector2(170f, 42f), new Color(1f, 0.32f, 0.38f, 1f));

        RectTransform bottom = CreatePanel("CombatPanel", canvas.transform, panelColor, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(760f, 142f));
        CreateBorder(bottom, panelEdgeColor);

        RectTransform identity = CreatePanel("Identity", bottom, new Color(0.035f, 0.065f, 0.12f, 0.94f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(96f, 0f), new Vector2(180f, 116f));
        championName = CreateText("ChampionName", identity, "LYRA", 26, TextAnchor.UpperCenter, new Vector2(0f, -7f), new Vector2(160f, 42f), Color.white);
        CreateText("Role", identity, "MOON HUNTRESS", 14, TextAnchor.MiddleCenter, new Vector2(0f, -22f), new Vector2(160f, 30f), new Color(0.65f, 0.78f, 1f, 1f));

        RectTransform healthBg = CreatePanel("HealthBackground", identity, new Color(0.02f, 0.02f, 0.03f, 1f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 17f), new Vector2(154f, 24f));
        healthFill = CreateFill("HealthFill", healthBg, healthColor);
        healthText = CreateText("HealthText", healthBg, "0 / 0", 14, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(150f, 22f), Color.white);

        string[] keys = { "Q", "W", "E", "R" };
        string[] labels = { "DAGGER", "VANISH", "NET", "BLOOD MOON" };

        for (int i = 0; i < 4; i++)
        {
            float x = -170f + i * 112f;
            RectTransform slot = CreatePanel("Ability_" + keys[i], bottom, new Color(0.045f, 0.07f, 0.13f, 0.98f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(x + 90f, 2f), new Vector2(98f, 116f));
            CreateBorder(slot, i == 3 ? ultimateColor : accentColor);

            CreateText("Key", slot, keys[i], 30, TextAnchor.UpperCenter, new Vector2(0f, -5f), new Vector2(86f, 44f), i == 3 ? ultimateColor : Color.white);
            CreateText("Label", slot, labels[i], 12, TextAnchor.LowerCenter, new Vector2(0f, 9f), new Vector2(88f, 30f), new Color(0.72f, 0.82f, 1f, 1f));

            cooldownFills[i] = CreateFill("Cooldown", slot, new Color(0.01f, 0.015f, 0.03f, 0.78f));
            cooldownFills[i].fillMethod = Image.FillMethod.Vertical;
            cooldownFills[i].fillOrigin = (int)Image.OriginVertical.Top;
            cooldownFills[i].fillAmount = 0f;

            cooldownTexts[i] = CreateText("CooldownText", slot, string.Empty, 22, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(90f, 50f), Color.white);
        }

        CreateText("Controls", canvas.transform, "RIGHT CLICK: MOVE / ATTACK    •    MIDDLE DRAG OR SCREEN EDGE: PAN    •    SPACE: FOCUS    •    WHEEL: ZOOM", 15, TextAnchor.MiddleCenter, new Vector2(0f, 10f), new Vector2(1100f, 28f), new Color(0.72f, 0.82f, 0.96f, 0.88f), new Vector2(0.5f, 0f));
    }

    private void UpdateTimer()
    {
        int totalSeconds = Mathf.Max(0, Mathf.FloorToInt(Time.unscaledTime - matchStartTime));
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        timerText.text = minutes.ToString("00") + ":" + seconds.ToString("00");
    }

    private void UpdateHealth()
    {
        if (stats == null)
        {
            healthFill.fillAmount = 0f;
            healthText.text = "SEARCHING...";
            return;
        }

        float maxHp = Mathf.Max(1f, stats.maxHp);
        float ratio = Mathf.Clamp01(stats.hp / maxHp);
        healthFill.fillAmount = ratio;
        healthText.text = Mathf.CeilToInt(stats.hp) + " / " + Mathf.CeilToInt(maxHp);
    }

    private void UpdateCooldowns()
    {
        cooldownRatios[0] = lyra != null ? lyra.GetQCooldownRatio() : 0f;
        cooldownRatios[1] = lyra != null ? lyra.GetWCooldownRatio() : 0f;
        cooldownRatios[2] = lyra != null ? lyra.GetECooldownRatio() : 0f;
        cooldownRatios[3] = lyra != null ? lyra.GetRCooldownRatio() : 0f;

        for (int i = 0; i < 4; i++)
        {
            float ratio = Mathf.Clamp01(cooldownRatios[i]);
            cooldownFills[i].fillAmount = ratio;
            cooldownTexts[i].text = ratio > 0.01f ? Mathf.CeilToInt(ratio * GetCooldownDuration(i)).ToString() : string.Empty;
        }
    }

    private float GetCooldownDuration(int index)
    {
        if (lyra == null)
            return 0f;

        switch (index)
        {
            case 0: return lyra.qCooldown;
            case 1: return lyra.wCooldown;
            case 2: return lyra.eCooldown;
            default: return lyra.rCooldown;
        }
    }

    private RectTransform CreatePanel(string name, Transform parent, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = anchorMin == anchorMax ? anchorMin : new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        Image image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return rect;
    }

    private Image CreateFill(string name, Transform parent, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = go.GetComponent<Image>();
        image.color = color;
        image.type = Image.Type.Filled;
        image.fillMethod = Image.FillMethod.Horizontal;
        image.fillOrigin = (int)Image.OriginHorizontal.Left;
        image.fillAmount = 1f;
        image.raycastTarget = false;
        return image;
    }

    private Text CreateText(string name, Transform parent, string text, int size, TextAnchor alignment, Vector2 anchoredPosition, Vector2 rectSize, Color color, Vector2? anchor = null)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        Vector2 resolvedAnchor = anchor ?? new Vector2(0.5f, 0.5f);
        rect.anchorMin = resolvedAnchor;
        rect.anchorMax = resolvedAnchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = rectSize;

        Text label = go.GetComponent<Text>();
        label.font = font;
        label.text = text;
        label.fontSize = size;
        label.alignment = alignment;
        label.color = color;
        label.raycastTarget = false;
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        return label;
    }

    private void CreateBorder(RectTransform target, Color color)
    {
        GameObject borderObject = new GameObject("Border", typeof(RectTransform), typeof(Image), typeof(Outline));
        borderObject.transform.SetParent(target, false);
        RectTransform rect = borderObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = borderObject.GetComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.001f);
        image.raycastTarget = false;

        Outline outline = borderObject.GetComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = new Vector2(2f, -2f);
    }
}
