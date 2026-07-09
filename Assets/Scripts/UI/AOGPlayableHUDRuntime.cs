using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Zero-setup playable HUD for the current Aura of Gods map.
/// It auto-installs at runtime, binds to the locally controlled champion and can later be replaced by art-prefab UI.
/// </summary>
public class AOGPlayableHUDRuntime : MonoBehaviour
{
    private const string HudRootName = "AOG_Playable_HUD_Runtime";

    private Champion player;
    private ChampionAbility[] abilities;
    private GameStateManager gameState;
    private ObjectiveManager objectives;

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

    private static readonly string[] AbilityKeys = { "Q", "W", "E", "R" };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindObjectOfType<AOGPlayableHUDRuntime>() != null)
            return;

        GameObject root = new GameObject(HudRootName);
        root.AddComponent<AOGPlayableHUDRuntime>();
    }

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        BuildHUD();
    }

    void Start()
    {
        RebindRuntimeObjects();
    }

    void Update()
    {
        if (player == null || !player.gameObject.activeInHierarchy)
            RebindRuntimeObjects();

        if (gameState == null)
            gameState = FindObjectOfType<GameStateManager>();
        if (objectives == null)
            objectives = FindObjectOfType<ObjectiveManager>();

        RefreshHUD();
    }

    private void RebindRuntimeObjects()
    {
        ChampionController controller = FindObjectOfType<ChampionController>();
        player = controller != null ? controller.GetComponent<Champion>() : FindObjectOfType<Champion>();
        abilities = player != null ? player.GetComponents<ChampionAbility>() : null;
        gameState = FindObjectOfType<GameStateManager>();
        objectives = FindObjectOfType<ObjectiveManager>();
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
        GameObject panel = CreatePanel("TopBar", parent, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(0f, -18f), new Vector2(480f, 62f), new Color(0.035f, 0.045f, 0.07f, 0.92f));

        timerText = CreateText("Timer", panel.transform, "00:00", 26, TextAnchor.MiddleCenter,
            new Vector2(0f, 12f), new Vector2(180f, 30f));
        scoreText = CreateText("Score", panel.transform, "0   -   0", 22, TextAnchor.MiddleCenter,
            new Vector2(0f, -15f), new Vector2(260f, 28f));
    }

    private void BuildBottomHUD(Transform parent)
    {
        GameObject panel = CreatePanel("BottomHUD", parent, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 24f), new Vector2(890f, 190f), new Color(0.025f, 0.035f, 0.06f, 0.94f));

        levelText = CreateText("Level", panel.transform, "LV 1", 24, TextAnchor.MiddleCenter,
            new Vector2(-382f, 40f), new Vector2(100f, 42f));
        goldText = CreateText("Gold", panel.transform, "0 G", 22, TextAnchor.MiddleCenter,
            new Vector2(-382f, -12f), new Vector2(120f, 40f));

        healthFill = CreateBar("Health", panel.transform, new Vector2(-165f, 56f), new Vector2(330f, 27f),
            new Color(0.06f, 0.08f, 0.10f, 1f), new Color(0.12f, 0.72f, 0.31f, 1f));
        healthText = CreateText("HealthText", panel.transform, "0 / 0", 18, TextAnchor.MiddleCenter,
            new Vector2(-165f, 56f), new Vector2(330f, 27f));

        manaFill = CreateBar("Mana", panel.transform, new Vector2(-165f, 20f), new Vector2(330f, 22f),
            new Color(0.06f, 0.08f, 0.10f, 1f), new Color(0.16f, 0.46f, 0.92f, 1f));
        manaText = CreateText("ManaText", panel.transform, "0 / 0", 16, TextAnchor.MiddleCenter,
            new Vector2(-165f, 20f), new Vector2(330f, 22f));

        xpFill = CreateBar("XP", panel.transform, new Vector2(-165f, -9f), new Vector2(330f, 8f),
            new Color(0.06f, 0.08f, 0.10f, 1f), new Color(0.58f, 0.32f, 0.92f, 1f));

        float startX = 72f;
        for (int i = 0; i < 4; i++)
        {
            GameObject slot = CreatePanel("Ability_" + AbilityKeys[i], panel.transform,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(startX + i * 112f, 25f), new Vector2(96f, 96f),
                new Color(0.08f, 0.10f, 0.16f, 1f));

            Image cooldown = CreateFillImage("Cooldown", slot.transform, new Color(0.02f, 0.02f, 0.03f, 0.78f));
            cooldown.type = Image.Type.Filled;
            cooldown.fillMethod = Image.FillMethod.Radial360;
            cooldown.fillOrigin = 2;
            cooldown.fillClockwise = false;
            cooldown.fillAmount = 0f;
            abilityCooldownFills[i] = cooldown;

            abilityTexts[i] = CreateText("AbilityText", slot.transform, AbilityKeys[i], 24, TextAnchor.MiddleCenter,
                Vector2.zero, new Vector2(90f, 90f));
        }
    }

    private void BuildObjectivePanel(Transform parent)
    {
        GameObject panel = CreatePanel("ObjectivePanel", parent, new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-28f, -28f), new Vector2(330f, 104f), new Color(0.025f, 0.035f, 0.06f, 0.90f));

        objectiveText = CreateText("ObjectiveText", panel.transform, "DRAGON  LIVE\nMEDUSA  LIVE", 20,
            TextAnchor.MiddleLeft, Vector2.zero, new Vector2(294f, 76f));
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
            scoreText.text = $"BLUE  {blueKills}    -    {redKills}  RED";
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
            ChampionAbility ability = abilities != null && i < abilities.Length ? abilities[i] : null;
            if (ability == null)
            {
                abilityTexts[i].text = AbilityKeys[i];
                abilityCooldownFills[i].fillAmount = 0f;
                continue;
            }

            float remaining = ability.GetCooldownRemaining();
            float cooldown = Mathf.Max(0.01f, ability.CooldownSeconds);
            abilityCooldownFills[i].fillAmount = Mathf.Clamp01(remaining / cooldown);
            abilityTexts[i].text = remaining > 0.05f
                ? $"{AbilityKeys[i]}\n{remaining:0.0}"
                : $"{AbilityKeys[i]}\nREADY";
        }

        if (objectives != null)
        {
            string dragon = objectives.DragonAlive ? "LIVE" : FormatRespawn(objectives.DragonRespawnRemaining);
            string medusa = objectives.MedusaAlive ? "LIVE" : FormatRespawn(objectives.MedusaRespawnRemaining);
            objectiveText.text = $"DRAGON   {dragon}\nMEDUSA   {medusa}";
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
        obj.GetComponent<Image>().color = color;
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
