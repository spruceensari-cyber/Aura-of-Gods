using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class AOGCompetitiveMobaHUDBootstrap
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
        AOGCombatHUDRuntime oldHud = Object.FindFirstObjectByType<AOGCombatHUDRuntime>();
        if (oldHud != null)
            Object.Destroy(oldHud.gameObject);

        if (Object.FindFirstObjectByType<AOGCompetitiveMobaHUDRuntime>() != null)
            return;

        GameObject host = new GameObject("AOG_Competitive_MOBA_HUD");
        Object.DontDestroyOnLoad(host);
        host.AddComponent<AOGCompetitiveMobaHUDRuntime>();
    }
}

[DefaultExecutionOrder(200)]
public class AOGCompetitiveMobaHUDRuntime : MonoBehaviour
{
    private readonly Color ink = new Color(0.015f, 0.024f, 0.038f, 0.98f);
    private readonly Color inkSoft = new Color(0.03f, 0.05f, 0.075f, 0.96f);
    private readonly Color steel = new Color(0.16f, 0.23f, 0.30f, 1f);
    private readonly Color gold = new Color(0.86f, 0.68f, 0.30f, 1f);
    private readonly Color blue = new Color(0.15f, 0.55f, 1f, 1f);
    private readonly Color green = new Color(0.10f, 0.78f, 0.34f, 1f);
    private readonly Color purple = new Color(0.58f, 0.24f, 0.92f, 1f);
    private readonly Color red = new Color(0.92f, 0.18f, 0.22f, 1f);

    private Canvas canvas;
    private Font font;
    private AOGActiveChampion activeChampion;
    private AOGCharacterStats stats;
    private LyraSkillSet lyra;
    private IAOGAbilityCooldownProvider abilityProvider;
    private AOGPlayerEconomy economy;
    private AOGChampionProgression progression;

    private Image hpFill;
    private Image resourceFill;
    private Image portraitImage;
    private Text hpText;
    private Text resourceText;
    private Text portraitGlyph;
    private Text playerName;
    private Text playerRole;
    private Text playerLevel;
    private Text timerText;
    private Text goldText;
    private Text attackText;
    private Text armorText;
    private Text speedText;
    private Text fpsText;

    private readonly Image[] cooldownMasks = new Image[4];
    private readonly Text[] cooldownNumbers = new Text[4];
    private readonly Image[] abilityGlow = new Image[4];

    private float matchStart;
    private float smoothedFps = 60f;
    private float nextSearchTime;
    private int simulatedGold = 500;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        matchStart = Time.unscaledTime;
        BuildHud();
        FindPlayer();
    }

    private void Update()
    {
        if (activeChampion != AOGPlayerChampionAuthority.CurrentChampion || stats == null || !stats.gameObject.activeInHierarchy)
        {
            if (Time.unscaledTime >= nextSearchTime)
            {
                nextSearchTime = Time.unscaledTime + 0.5f;
                FindPlayer();
            }
        }

        UpdateTimer();
        UpdateVitals();
        UpdateAbilities();
        UpdateStats();
        smoothedFps = Mathf.Lerp(smoothedFps, 1f / Mathf.Max(Time.unscaledDeltaTime, 0.0001f), 0.06f);
        if (fpsText != null)
            fpsText.text = Mathf.RoundToInt(smoothedFps) + " FPS";
    }

    private void FindPlayer()
    {
        activeChampion = AOGPlayerChampionAuthority.CurrentChampion;
        if (activeChampion == null || !activeChampion.gameObject.activeInHierarchy)
        {
            stats = null;
            lyra = null;
            abilityProvider = null;
            economy = null;
            progression = null;
            return;
        }

        stats = activeChampion.GetComponent<AOGCharacterStats>();
        lyra = activeChampion.GetComponent<LyraSkillSet>();
        economy = activeChampion.GetComponent<AOGPlayerEconomy>();
        progression = activeChampion.GetComponent<AOGChampionProgression>();
        abilityProvider = null;
        foreach (MonoBehaviour behaviour in activeChampion.GetComponents<MonoBehaviour>())
        {
            if (behaviour is IAOGAbilityCooldownProvider provider)
            {
                abilityProvider = provider;
                break;
            }
        }

        ApplyPlayerIdentity();
    }

    private void BuildHud()
    {
        GameObject canvasObject = new GameObject("CompetitiveHUDCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        BuildTopScoreboard();
        BuildBottomChampionHud();
        BuildMinimap();
        BuildLeftInfoPanel();
        BuildPerformanceTag();
    }

    private void BuildTopScoreboard()
    {
        RectTransform panel = Panel("TopScoreboard", canvas.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(620f, 58f), ink);
        AddOutline(panel, gold, 2f);

        RectTransform blueWing = Panel("BlueWing", panel, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(140f, 0f), new Vector2(270f, 48f), new Color(0.025f, 0.12f, 0.24f, 0.96f));
        Label("BlueScore", blueWing, "BLUE   0 / 0 / 0", 21, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(250f, 44f), new Color(0.42f, 0.76f, 1f, 1f));

        RectTransform redWing = Panel("RedWing", panel, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-140f, 0f), new Vector2(270f, 48f), new Color(0.24f, 0.045f, 0.06f, 0.96f));
        Label("RedScore", redWing, "0 / 0 / 0   RED", 21, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(250f, 44f), new Color(1f, 0.48f, 0.48f, 1f));

        RectTransform clock = Panel("Clock", panel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(92f, 54f), new Color(0.01f, 0.015f, 0.025f, 1f));
        AddOutline(clock, gold, 1.5f);
        timerText = Label("Timer", clock, "00:00", 23, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(86f, 48f), Color.white);
    }

    private void BuildBottomChampionHud()
    {
        RectTransform root = Panel("ChampionHUD", canvas.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 12f), new Vector2(980f, 218f), ink);
        AddOutline(root, steel, 2f);

        BuildPortrait(root);
        BuildVitals(root);
        BuildAbilityRow(root);
        BuildItems(root);
        BuildCombatStats(root);
    }

    private void BuildPortrait(RectTransform root)
    {
        RectTransform frame = Panel("PortraitFrame", root, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(94f, 108f), new Vector2(174f, 194f), new Color(0.025f, 0.04f, 0.065f, 1f));
        AddOutline(frame, gold, 3f);

        GameObject portraitObject = new GameObject("LyraPortrait", typeof(RectTransform), typeof(Image));
        portraitObject.transform.SetParent(frame, false);
        RectTransform portrait = portraitObject.GetComponent<RectTransform>();
        portrait.anchorMin = new Vector2(0.5f, 1f);
        portrait.anchorMax = new Vector2(0.5f, 1f);
        portrait.pivot = new Vector2(0.5f, 1f);
        portrait.anchoredPosition = new Vector2(0f, -10f);
        portrait.sizeDelta = new Vector2(150f, 118f);
        Image portraitImage = portraitObject.GetComponent<Image>();
        portraitImage.sprite = BuildPortraitSprite();
        portraitImage.preserveAspect = true;
        portraitImage.raycastTarget = false;

        Label("Name", frame, "LYRA", 27, TextAnchor.MiddleCenter, new Vector2(0f, 35f), new Vector2(150f, 34f), Color.white);
        Label("Role", frame, "MOON HUNTRESS", 12, TextAnchor.MiddleCenter, new Vector2(0f, 13f), new Vector2(150f, 24f), new Color(0.58f, 0.76f, 1f, 1f));

        RectTransform levelBadge = Panel("LevelBadge", frame, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(17f, 17f), new Vector2(38f, 38f), new Color(0.02f, 0.03f, 0.05f, 1f));
        AddOutline(levelBadge, gold, 2f);
        Label("Level", levelBadge, "1", 21, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(34f, 34f), Color.white);
    }

    private void BuildVitals(RectTransform root)
    {
        RectTransform vitals = Panel("Vitals", root, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(303f, 43f), new Vector2(240f, 74f), new Color(0.02f, 0.035f, 0.055f, 1f));

        RectTransform hpBg = Panel("HP_BG", vitals, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(224f, 25f), new Color(0.035f, 0.055f, 0.055f, 1f));
        hpFill = FillImage("HP_FILL", hpBg, green);
        hpText = Label("HP_TEXT", hpBg, "600 / 600", 14, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(210f, 22f), Color.white);

        RectTransform resourceBg = Panel("Resource_BG", vitals, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 8f), new Vector2(224f, 20f), new Color(0.035f, 0.045f, 0.075f, 1f));
        resourceFill = FillImage("Resource_FILL", resourceBg, new Color(0.18f, 0.43f, 0.95f, 1f));
        resourceText = Label("Resource_TEXT", resourceBg, "100 AETHER", 12, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(210f, 18f), Color.white);
    }

    private void BuildAbilityRow(RectTransform root)
    {
        RectTransform abilities = Panel("Abilities", root, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(56f, 103f), new Vector2(470f, 128f), new Color(0.018f, 0.03f, 0.05f, 0.96f));
        AddOutline(abilities, steel, 1.5f);

        string[] keys = { "Q", "W", "E", "R" };
        string[] names = { "DAGGER", "VANISH", "NET", "BLOOD MOON" };
        Color[] colors = { blue, new Color(0.32f, 0.68f, 1f, 1f), purple, new Color(0.76f, 0.18f, 0.50f, 1f) };

        for (int i = 0; i < 4; i++)
        {
            float x = -166f + i * 110f;
            RectTransform slot = Panel("Ability_" + keys[i], abilities, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(x, 0f), new Vector2(96f, 110f), new Color(0.028f, 0.045f, 0.07f, 1f));
            AddOutline(slot, colors[i], i == 3 ? 3f : 2f);

            Image icon = Panel("Icon", slot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -7f), new Vector2(80f, 72f), new Color(colors[i].r * 0.34f, colors[i].g * 0.34f, colors[i].b * 0.34f, 1f)).GetComponent<Image>();
            icon.sprite = BuildAbilitySprite(colors[i], i);
            icon.type = Image.Type.Simple;
            icon.raycastTarget = false;

            cooldownMasks[i] = FillImage("CooldownMask", icon.rectTransform, new Color(0.005f, 0.008f, 0.015f, 0.90f));
            cooldownMasks[i].fillMethod = Image.FillMethod.Radial360;
            cooldownMasks[i].fillOrigin = (int)Image.Origin360.Top;
            cooldownMasks[i].fillClockwise = false;
            cooldownMasks[i].fillAmount = 0f;

            abilityGlow[i] = icon;
            cooldownNumbers[i] = Label("Cooldown", icon.rectTransform, string.Empty, 25, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(72f, 48f), Color.white);

            Label("Key", slot, keys[i], 20, TextAnchor.MiddleLeft, new Vector2(-30f, 8f), new Vector2(26f, 24f), gold);
            Label("AbilityName", slot, names[i], 10, TextAnchor.MiddleRight, new Vector2(9f, 8f), new Vector2(62f, 22f), new Color(0.72f, 0.80f, 0.92f, 1f));
        }

        BuildUtilitySlot(abilities, "D", new Vector2(177f, 24f), new Color(0.95f, 0.72f, 0.18f, 1f));
        BuildUtilitySlot(abilities, "F", new Vector2(177f, -28f), new Color(0.28f, 0.78f, 1f, 1f));
    }

    private void BuildUtilitySlot(RectTransform parent, string key, Vector2 position, Color color)
    {
        RectTransform slot = Panel("Utility_" + key, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, new Vector2(44f, 44f), new Color(color.r * 0.22f, color.g * 0.22f, color.b * 0.22f, 1f));
        AddOutline(slot, color, 1.5f);
        Label("Key", slot, key, 18, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(40f, 40f), Color.white);
    }

    private void BuildItems(RectTransform root)
    {
        RectTransform items = Panel("Items", root, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-112f, 76f), new Vector2(204f, 142f), new Color(0.02f, 0.032f, 0.052f, 1f));
        AddOutline(items, steel, 1.5f);

        for (int i = 0; i < 6; i++)
        {
            int row = i / 3;
            int col = i % 3;
            RectTransform item = Panel("Item_" + i, items, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-62f + col * 62f, 27f - row * 62f), new Vector2(52f, 52f), new Color(0.035f, 0.05f, 0.075f, 1f));
            AddOutline(item, new Color(0.23f, 0.30f, 0.39f, 1f), 1f);
            Label("Empty", item, "•", 22, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(48f, 48f), new Color(0.26f, 0.32f, 0.40f, 1f));
        }

        RectTransform goldBar = Panel("GoldBar", root, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-112f, 20f), new Vector2(204f, 34f), new Color(0.05f, 0.045f, 0.025f, 1f));
        AddOutline(goldBar, gold, 1.5f);
        goldText = Label("Gold", goldBar, "◈ 500", 18, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(190f, 30f), gold);
    }

    private void BuildCombatStats(RectTransform root)
    {
        RectTransform statsPanel = Panel("CombatStats", root, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(302f, 145f), new Vector2(240f, 92f), new Color(0.02f, 0.032f, 0.052f, 1f));
        AddOutline(statsPanel, steel, 1f);

        attackText = Label("Attack", statsPanel, "⚔ 45", 15, TextAnchor.MiddleLeft, new Vector2(-54f, 20f), new Vector2(100f, 28f), new Color(1f, 0.72f, 0.40f, 1f));
        armorText = Label("Armor", statsPanel, "◆ 30", 15, TextAnchor.MiddleLeft, new Vector2(58f, 20f), new Vector2(100f, 28f), new Color(0.72f, 0.82f, 0.96f, 1f));
        speedText = Label("Speed", statsPanel, "➤ 6.0", 15, TextAnchor.MiddleLeft, new Vector2(-54f, -20f), new Vector2(100f, 28f), new Color(0.55f, 0.90f, 0.72f, 1f));
        Label("Range", statsPanel, "◎ 4.0", 15, TextAnchor.MiddleLeft, new Vector2(58f, -20f), new Vector2(100f, 28f), new Color(0.72f, 0.65f, 1f, 1f));
    }

    private void BuildMinimap()
    {
        RectTransform frame = Panel("MinimapFrame", canvas.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-16f, 16f), new Vector2(270f, 270f), ink);
        AddOutline(frame, gold, 3f);

        GameObject mapObject = new GameObject("Minimap", typeof(RectTransform), typeof(Image));
        mapObject.transform.SetParent(frame, false);
        RectTransform map = mapObject.GetComponent<RectTransform>();
        map.anchorMin = new Vector2(0.5f, 0.5f);
        map.anchorMax = new Vector2(0.5f, 0.5f);
        map.pivot = new Vector2(0.5f, 0.5f);
        map.sizeDelta = new Vector2(250f, 250f);
        Image mapImage = mapObject.GetComponent<Image>();
        mapImage.sprite = BuildMinimapSprite();
        mapImage.raycastTarget = false;

        Label("BlueBase", map, "◆", 24, TextAnchor.MiddleCenter, new Vector2(-91f, -91f), new Vector2(30f, 30f), blue);
        Label("RedBase", map, "◆", 24, TextAnchor.MiddleCenter, new Vector2(91f, 91f), new Vector2(30f, 30f), red);
        Label("Player", map, "▲", 22, TextAnchor.MiddleCenter, new Vector2(-62f, -58f), new Vector2(26f, 26f), new Color(0.95f, 0.90f, 0.40f, 1f));
    }

    private void BuildLeftInfoPanel()
    {
        RectTransform panel = Panel("ObjectivePanel", canvas.transform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(16f, 18f), new Vector2(250f, 150f), new Color(0.015f, 0.025f, 0.04f, 0.88f));
        AddOutline(panel, steel, 1f);
        Label("Title", panel, "AETHER RIFT", 17, TextAnchor.MiddleLeft, new Vector2(-50f, 53f), new Vector2(130f, 28f), gold);
        Label("Objective1", panel, "◇ DRAGON  04:30", 14, TextAnchor.MiddleLeft, new Vector2(0f, 18f), new Vector2(220f, 28f), new Color(0.82f, 0.88f, 0.96f, 1f));
        Label("Objective2", panel, "◇ MEDUSA  05:30", 14, TextAnchor.MiddleLeft, new Vector2(0f, -13f), new Vector2(220f, 28f), new Color(0.82f, 0.88f, 0.96f, 1f));
        Label("Hint", panel, "Right click: move / attack", 12, TextAnchor.MiddleLeft, new Vector2(0f, -51f), new Vector2(220f, 24f), new Color(0.50f, 0.62f, 0.74f, 1f));
    }

    private void BuildPerformanceTag()
    {
        fpsText = Label("FPS", canvas.transform, "60 FPS", 12, TextAnchor.MiddleRight, new Vector2(-12f, -12f), new Vector2(120f, 24f), new Color(0.62f, 0.74f, 0.80f, 0.78f), new Vector2(1f, 1f));
    }

    private void UpdateTimer()
    {
        int total = Mathf.Max(0, Mathf.FloorToInt(Time.unscaledTime - matchStart));
        timerText.text = (total / 60).ToString("00") + ":" + (total % 60).ToString("00");
        simulatedGold = 500 + Mathf.FloorToInt(total * 1.7f);
        goldText.text = "◈ " + simulatedGold;
    }

    private void UpdateVitals()
    {
        if (stats == null)
            return;

        float hpRatio = Mathf.Clamp01(stats.hp / Mathf.Max(1f, stats.maxHp));
        hpFill.fillAmount = hpRatio;
        hpText.text = Mathf.CeilToInt(stats.hp) + " / " + Mathf.CeilToInt(stats.maxHp);

        float pulse = 0.94f + Mathf.Sin(Time.unscaledTime * 1.8f) * 0.06f;
        resourceFill.fillAmount = pulse;
        resourceText.text = Mathf.RoundToInt(pulse * 100f) + " AETHER";
    }

    private void UpdateAbilities()
    {
        float[] ratios = new float[4];
        float[] durations = new float[4];

        if (lyra != null)
        {
            ratios[0] = lyra.GetQCooldownRatio();
            ratios[1] = lyra.GetWCooldownRatio();
            ratios[2] = lyra.GetECooldownRatio();
            ratios[3] = lyra.GetRCooldownRatio();
            durations[0] = lyra.qCooldown;
            durations[1] = lyra.wCooldown;
            durations[2] = lyra.eCooldown;
            durations[3] = lyra.rCooldown;
        }

        for (int i = 0; i < 4; i++)
        {
            float ratio = Mathf.Clamp01(ratios[i]);
            cooldownMasks[i].fillAmount = ratio;
            cooldownNumbers[i].text = ratio > 0.01f ? Mathf.CeilToInt(ratio * durations[i]).ToString() : string.Empty;
            abilityGlow[i].color = ratio > 0.01f ? new Color(0.48f, 0.48f, 0.48f, 1f) : Color.white;
        }
    }

    private void UpdateStats()
    {
        if (stats == null)
            return;

        attackText.text = "⚔ " + Mathf.RoundToInt(stats.attackDamage);
        armorText.text = "◆ 30";
        speedText.text = "➤ " + stats.moveSpeed.ToString("0.0");
    }

    private void ApplyPlayerIdentity()
    {
        if (activeChampion == null)
            return;

        Color accent = activeChampion.accentColor;
        if (playerName != null)
            playerName.text = activeChampion.displayName;
        if (playerRole != null)
        {
            playerRole.text = activeChampion.roleName;
            playerRole.color = accent;
        }
        if (portraitGlyph != null)
        {
            portraitGlyph.text = string.IsNullOrEmpty(activeChampion.displayName)
                ? "?"
                : activeChampion.displayName.Substring(0, 1);
            portraitGlyph.color = Color.Lerp(accent, Color.white, 0.22f);
        }
        if (portraitImage != null)
            portraitImage.color = Color.Lerp(Color.white, accent, 0.30f);
        if (playerLevel != null && progression != null)
            playerLevel.text = progression.level.ToString();
    }

    private RectTransform Panel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = anchorMin == anchorMax ? anchorMin : new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        Image image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return rect;
    }

    private Text Label(string name, Transform parent, string text, int size, TextAnchor alignment, Vector2 position, Vector2 dimensions, Color color, Vector2? anchor = null)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        Vector2 a = anchor ?? new Vector2(0.5f, 0.5f);
        rect.anchorMin = a;
        rect.anchorMax = a;
        rect.pivot = a;
        rect.anchoredPosition = position;
        rect.sizeDelta = dimensions;

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

    private Image FillImage(string name, RectTransform parent, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(2f, 2f);
        rect.offsetMax = new Vector2(-2f, -2f);
        Image image = go.GetComponent<Image>();
        image.color = color;
        image.type = Image.Type.Filled;
        image.fillMethod = Image.FillMethod.Horizontal;
        image.fillOrigin = (int)Image.OriginHorizontal.Left;
        image.fillAmount = 1f;
        image.raycastTarget = false;
        return image;
    }

    private void AddOutline(RectTransform target, Color color, float distance)
    {
        Outline outline = target.gameObject.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = new Vector2(distance, -distance);
    }

    private Sprite BuildPortraitSprite()
    {
        const int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];
        Vector2 center = new Vector2(size * 0.5f, size * 0.52f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), center) / (size * 0.56f);
                float glow = Mathf.Clamp01(1f - d);
                float moon = Mathf.Clamp01(1f - Vector2.Distance(new Vector2(x, y), new Vector2(82f, 82f)) / 48f);
                Color c = Color.Lerp(new Color(0.03f, 0.05f, 0.11f, 1f), new Color(0.36f, 0.10f, 0.48f, 1f), glow);
                c += new Color(0.10f, 0.28f, 0.48f, 0f) * moon * 0.45f;
                pixels[y * size + x] = c;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private Sprite BuildAbilitySprite(Color baseColor, int seed)
    {
        const int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = (x - size * 0.5f) / (size * 0.5f);
                float ny = (y - size * 0.5f) / (size * 0.5f);
                float r = Mathf.Sqrt(nx * nx + ny * ny);
                float swirl = Mathf.Sin((Mathf.Atan2(ny, nx) + r * (4f + seed)) * (3f + seed * 0.35f));
                float energy = Mathf.Clamp01(1f - r) * (0.65f + 0.35f * swirl);
                Color c = Color.Lerp(new Color(0.015f, 0.02f, 0.04f, 1f), baseColor, Mathf.Clamp01(energy));
                pixels[y * size + x] = c;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private Sprite BuildMinimapSprite()
    {
        const int size = 192;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = x / (float)(size - 1);
                float v = y / (float)(size - 1);
                float noise = Mathf.PerlinNoise(u * 5f + 2f, v * 5f + 8f);
                Color grass = Color.Lerp(new Color(0.025f, 0.09f, 0.06f, 1f), new Color(0.07f, 0.18f, 0.09f, 1f), noise);

                float mid = Mathf.Abs(v - u);
                float top = Mathf.Min(Mathf.Abs(v - 0.82f), Mathf.Abs(u - 0.18f));
                float bot = Mathf.Min(Mathf.Abs(v - 0.18f), Mathf.Abs(u - 0.82f));
                bool lane = mid < 0.055f || top < 0.035f || bot < 0.035f;
                pixels[y * size + x] = lane ? new Color(0.30f, 0.26f, 0.20f, 1f) : grass;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }
}
