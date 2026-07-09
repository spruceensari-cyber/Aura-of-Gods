using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Compact match HUD focused on combat readability. Rank/profile information is intentionally excluded.
/// </summary>
public class AOGPlayableHUDRuntime : MonoBehaviour
{
    private const string HudRootName = "AOG_Playable_HUD_Runtime";

    private Champion player;
    private readonly ChampionAbility[] abilities = new ChampionAbility[4];
    private GameStateManager gameState;
    private ObjectiveManager objectives;
    private float nextAbilityRefresh;

    private Text timerText;
    private Text scoreText;
    private Text levelText;
    private Text goldText;
    private Text healthText;
    private Text manaText;
    private Text objectiveText;
    private Image healthFill;
    private Image manaFill;
    private Image xpFill;
    private readonly Text[] abilityTexts = new Text[4];
    private readonly Image[] abilityCooldownFills = new Image[4];
    private readonly Image[] abilityFrames = new Image[4];

    private static readonly string[] AbilityKeys = { "Q", "W", "E", "R" };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindObjectOfType<AOGPlayableHUDRuntime>() != null) return;
        new GameObject(HudRootName).AddComponent<AOGPlayableHUDRuntime>();
    }

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        BuildHUD();
    }

    void Start() => RebindRuntimeObjects();

    void Update()
    {
        if (player == null || !player.gameObject.activeInHierarchy)
            RebindRuntimeObjects();

        if (Time.unscaledTime >= nextAbilityRefresh)
        {
            nextAbilityRefresh = Time.unscaledTime + 0.25f;
            RefreshAbilityBindings();
        }

        if (gameState == null) gameState = FindObjectOfType<GameStateManager>();
        if (objectives == null) objectives = FindObjectOfType<ObjectiveManager>();
        RefreshHUD();
    }

    private void RebindRuntimeObjects()
    {
        ChampionController controller = FindObjectOfType<ChampionController>();
        player = controller != null ? controller.GetComponent<Champion>() : FindObjectOfType<Champion>();
        gameState = FindObjectOfType<GameStateManager>();
        objectives = FindObjectOfType<ObjectiveManager>();
        RefreshAbilityBindings();
    }

    private void RefreshAbilityBindings()
    {
        for (int i = 0; i < abilities.Length; i++) abilities[i] = null;
        if (player == null) return;

        foreach (ChampionAbility ability in player.GetComponents<ChampionAbility>())
        {
            int index = ability.Key switch
            {
                AbilityKey.Q => 0,
                AbilityKey.W => 1,
                AbilityKey.E => 2,
                AbilityKey.R => 3,
                _ => -1
            };
            if (index >= 0) abilities[index] = ability;
        }
    }

    private void BuildHUD()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 250;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        BuildTopBar(transform);
        BuildBottomHUD(transform);
        BuildObjectivePanel(transform);
    }

    private void BuildTopBar(Transform parent)
    {
        GameObject panel = CreatePanel("TopBar", parent, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -14f), new Vector2(430f, 48f), new Color(0.018f, 0.025f, 0.045f, 0.88f));

        timerText = CreateText("Timer", panel.transform, "00:00", 22, TextAnchor.MiddleCenter,
            new Vector2(0f, 10f), new Vector2(150f, 24f));
        scoreText = CreateText("Score", panel.transform, "BLUE 0  •  0 RED", 18, TextAnchor.MiddleCenter,
            new Vector2(0f, -11f), new Vector2(330f, 22f));
    }

    private void BuildBottomHUD(Transform parent)
    {
        GameObject panel = CreatePanel("BottomHUD", parent, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 16f), new Vector2(720f, 138f), new Color(0.014f, 0.022f, 0.042f, 0.92f));

        levelText = CreateText("Level", panel.transform, "LV 1", 20, TextAnchor.MiddleCenter,
            new Vector2(-320f, 26f), new Vector2(72f, 30f));
        goldText = CreateText("Gold", panel.transform, "0 G", 17, TextAnchor.MiddleCenter,
            new Vector2(-320f, -15f), new Vector2(90f, 28f));

        healthFill = CreateBar("Health", panel.transform, new Vector2(-165f, 38f), new Vector2(250f, 22f),
            new Color(0.035f, 0.045f, 0.06f, 1f), new Color(0.10f, 0.72f, 0.30f, 1f));
        healthText = CreateText("HealthText", panel.transform, "0 / 0", 15, TextAnchor.MiddleCenter,
            new Vector2(-165f, 38f), new Vector2(250f, 22f));

        manaFill = CreateBar("Mana", panel.transform, new Vector2(-165f, 8f), new Vector2(250f, 17f),
            new Color(0.035f, 0.045f, 0.06f, 1f), new Color(0.10f, 0.46f, 0.92f, 1f));
        manaText = CreateText("ManaText", panel.transform, "0 / 0", 13, TextAnchor.MiddleCenter,
            new Vector2(-165f, 8f), new Vector2(250f, 17f));

        xpFill = CreateBar("XP", panel.transform, new Vector2(-165f, -17f), new Vector2(250f, 5f),
            new Color(0.035f, 0.045f, 0.06f, 1f), new Color(0.55f, 0.30f, 0.92f, 1f));

        float startX = 22f;
        for (int i = 0; i < 4; i++)
        {
            GameObject slot = CreatePanel("Ability_" + AbilityKeys[i], panel.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(startX + i * 86f, 8f), new Vector2(72f, 72f),
                new Color(0.035f, 0.055f, 0.095f, 1f));
            abilityFrames[i] = slot.GetComponent<Image>();

            Image cooldown = CreateFillImage("Cooldown", slot.transform, new Color(0.005f, 0.008f, 0.015f, 0.82f));
            cooldown.type = Image.Type.Filled;
            cooldown.fillMethod = Image.FillMethod.Radial360;
            cooldown.fillOrigin = 2;
            cooldown.fillClockwise = false;
            cooldown.fillAmount = 0f;
            abilityCooldownFills[i] = cooldown;

            abilityTexts[i] = CreateText("AbilityText", slot.transform, AbilityKeys[i], 20, TextAnchor.MiddleCenter,
                Vector2.zero, new Vector2(68f, 68f));
        }
    }

    private void BuildObjectivePanel(Transform parent)
    {
        GameObject panel = CreatePanel("ObjectivePanel", parent, new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-18f, -18f), new Vector2(210f, 58f), new Color(0.014f, 0.022f, 0.042f, 0.86f));

        objectiveText = CreateText("ObjectiveText", panel.transform, "DRAGON LIVE   •   MEDUSA LIVE", 14,
            TextAnchor.MiddleCenter, Vector2.zero, new Vector2(196f, 44f));
    }

    private void RefreshHUD()
    {
        if (gameState != null)
        {
            float time = gameState.GetGameTime();
            int minutes = Mathf.FloorToInt(time / 60f);
            int seconds = Mathf.FloorToInt(time % 60f);
            timerText.text = $"{minutes:00}:{seconds:00}";

            TeamStats blue = gameState.GetTeamStats(TeamType.Blue);
            TeamStats red = gameState.GetTeamStats(TeamType.Red);
            int blueKills = blue != null ? blue.Kills : 0;
            int redKills = red != null ? red.Kills : 0;
            scoreText.text = $"BLUE {blueKills}   •   {redKills} RED";
        }

        if (player != null)
        {
            healthFill.fillAmount = player.HealthPercent;
            manaFill.fillAmount = player.ManaPercent;
            xpFill.fillAmount = player.ExperiencePercent;
            healthText.text = $"{player.CurrentHealth:0} / {player.MaxHealth:0}";
            manaText.text = $"{player.CurrentMana:0} / {player.MaxMana:0}";
            levelText.text = $"LV {player.Level}";
            goldText.text = $"{player.Gold:N0} G";
        }

        for (int i = 0; i < abilityTexts.Length; i++)
        {
            ChampionAbility ability = abilities[i];
            if (ability == null)
            {
                abilityTexts[i].text = AbilityKeys[i];
                abilityCooldownFills[i].fillAmount = 0f;
                abilityFrames[i].color = new Color(0.12f, 0.04f, 0.05f, 1f);
                continue;
            }

            float remaining = ability.GetCooldownRemaining();
            float cooldown = Mathf.Max(0.01f, ability.CooldownSeconds);
            abilityCooldownFills[i].fillAmount = Mathf.Clamp01(remaining / cooldown);
            abilityTexts[i].text = remaining > 0.05f
                ? $"{AbilityKeys[i]}\n{remaining:0.0}"
                : AbilityKeys[i];
            abilityFrames[i].color = remaining > 0.05f
                ? new Color(0.035f, 0.055f, 0.095f, 1f)
                : new Color(0.05f, 0.18f, 0.30f, 1f);
        }

        if (objectives != null)
        {
            string dragon = objectives.DragonAlive ? "LIVE" : FormatRespawn(objectives.DragonRespawnRemaining);
            string medusa = objectives.MedusaAlive ? "LIVE" : FormatRespawn(objectives.MedusaRespawnRemaining);
            objectiveText.text = $"DRAGON {dragon}   •   MEDUSA {medusa}";
        }
    }

    private static string FormatRespawn(float seconds)
    {
        int total = Mathf.CeilToInt(seconds);
        return $"{total / 60:00}:{total % 60:00}";
    }

    private static GameObject CreatePanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 anchoredPosition, Vector2 size, Color color)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = anchorMin;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        Image image = obj.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return obj;
    }

    private static Text CreateText(string name, Transform parent, string value, int size, TextAnchor alignment,
        Vector2 anchoredPosition, Vector2 dimensions)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Text));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = dimensions;

        Text text = obj.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = value;
        text.fontSize = size;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }

    private static Image CreateBar(string name, Transform parent, Vector2 position, Vector2 size, Color background, Color fillColor)
    {
        GameObject root = CreatePanel(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, size, background);
        Image fill = CreateFillImage("Fill", root.transform, fillColor);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = 0;
        fill.fillAmount = 1f;
        return fill;
    }

    private static Image CreateFillImage(string name, Transform parent, Color color)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        Image image = obj.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }
}
