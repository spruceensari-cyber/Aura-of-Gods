using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(1450)]
public class AOGNextStageHudRuntime : MonoBehaviour
{
    private Canvas canvas;
    private Font font;
    private AOGActiveChampion active;
    private AOGCharacterStats stats;
    private AOGChampionProgression progression;
    private AOGPlayerEconomy economy;
    private IAOGAbilityCooldownProvider provider;

    private Text nameText;
    private Text roleText;
    private Text levelText;
    private Text hpText;
    private Image hpFill;
    private Text xpText;
    private Image xpFill;
    private Text goldText;
    private Text statText;
    private Text skillPointText;
    private readonly Text[] abilityNameText = new Text[4];
    private readonly Text[] abilityRankText = new Text[4];
    private readonly Text[] cooldownText = new Text[4];
    private readonly Image[] cooldownMask = new Image[4];
    private readonly Button[] upgradeButtons = new Button[4];
    private readonly Image[] itemSlots = new Image[6];

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindFirstObjectByType<AOGNextStageHudRuntime>() != null)
            return;

        GameObject host = new GameObject("AOG_Next_Stage_HUD_Runtime");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGNextStageHudRuntime>();
    }

    private void Awake()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        BuildHud();
    }

    private void Update()
    {
        if (AOGActiveChampion.Current != null && AOGActiveChampion.Current != active)
            Bind(AOGActiveChampion.Current);

        if (active == null)
            return;

        RefreshVitals();
        RefreshAbilities();
        RefreshEconomy();
        RefreshStats();
    }

    private void Bind(AOGActiveChampion champion)
    {
        active = champion;
        stats = active.GetComponent<AOGCharacterStats>();
        progression = active.GetComponent<AOGChampionProgression>();
        economy = active.GetComponent<AOGPlayerEconomy>();
        provider = null;

        foreach (MonoBehaviour behaviour in active.GetComponents<MonoBehaviour>())
        {
            if (behaviour is IAOGAbilityCooldownProvider cooldownProvider)
            {
                provider = cooldownProvider;
                break;
            }
        }

        nameText.text = active.displayName;
        roleText.text = active.roleName;
        roleText.color = active.accentColor;
    }

    private void BuildHud()
    {
        GameObject canvasObject = new GameObject("NextStageHudCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 2100;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        Color deep = new Color(0.008f, 0.018f, 0.032f, 0.97f);
        Color panel = new Color(0.018f, 0.038f, 0.060f, 0.98f);
        Color line = new Color(0.18f, 0.32f, 0.44f, 1f);
        Color gold = new Color(0.94f, 0.73f, 0.28f, 1f);

        RectTransform root = Panel("MainActionBar", canvas.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(1080f, 205f), deep);
        Outline rootOutline = root.gameObject.AddComponent<Outline>();
        rootOutline.effectColor = line;
        rootOutline.effectDistance = new Vector2(2f, -2f);

        RectTransform identity = Panel("Identity", root, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(22f, 0f), new Vector2(270f, 165f), panel);
        nameText = Label("HeroName", identity, "HERO", 28, TextAnchor.MiddleLeft, new Vector2(18f, -22f), new Vector2(220f, 38f), Color.white, new Vector2(0f, 1f));
        roleText = Label("HeroRole", identity, "ROLE", 14, TextAnchor.MiddleLeft, new Vector2(18f, -56f), new Vector2(220f, 28f), Color.white, new Vector2(0f, 1f));
        levelText = Label("Level", identity, "1", 25, TextAnchor.MiddleCenter, new Vector2(224f, -24f), new Vector2(42f, 42f), gold, new Vector2(0f, 1f));

        hpFill = BuildBar(identity, "HP", new Vector2(18f, -96f), new Vector2(232f, 24f), new Color(0.12f, 0.72f, 0.34f));
        hpText = Label("HpText", identity, "0 / 0", 13, TextAnchor.MiddleCenter, new Vector2(18f, -96f), new Vector2(232f, 24f), Color.white, new Vector2(0f, 1f));
        xpFill = BuildBar(identity, "XP", new Vector2(18f, -130f), new Vector2(232f, 14f), new Color(0.24f, 0.62f, 1f));
        xpText = Label("XpText", identity, "XP 0 / 0", 10, TextAnchor.MiddleCenter, new Vector2(18f, -130f), new Vector2(232f, 16f), Color.white, new Vector2(0f, 1f));

        RectTransform abilities = Panel("Abilities", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-35f, 0f), new Vector2(500f, 165f), panel);
        string[] keys = { "Q", "W", "E", "R" };
        for (int i = 0; i < 4; i++)
        {
            RectTransform slot = Panel("Ability_" + keys[i], abilities, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(18f + i * 120f, 0f), new Vector2(104f, 130f), new Color(0.025f, 0.055f, 0.085f, 1f));
            Label("Key", slot, keys[i], 26, TextAnchor.MiddleCenter, new Vector2(0f, -22f), new Vector2(88f, 42f), Color.white, new Vector2(0.5f, 1f));
            abilityNameText[i] = Label("AbilityName", slot, "ABILITY", 11, TextAnchor.MiddleCenter, new Vector2(0f, -67f), new Vector2(96f, 30f), new Color(0.72f, 0.84f, 0.92f), new Vector2(0.5f, 1f));
            cooldownText[i] = Label("Cooldown", slot, string.Empty, 20, TextAnchor.MiddleCenter, new Vector2(0f, 0f), new Vector2(96f, 80f), Color.white, new Vector2(0.5f, 0.5f));
            abilityRankText[i] = Label("Rank", slot, "0/5", 11, TextAnchor.MiddleCenter, new Vector2(0f, 11f), new Vector2(90f, 20f), gold, new Vector2(0.5f, 0f));

            GameObject maskObject = new GameObject("CooldownMask", typeof(RectTransform), typeof(Image));
            maskObject.transform.SetParent(slot, false);
            RectTransform maskRect = maskObject.GetComponent<RectTransform>();
            maskRect.anchorMin = Vector2.zero;
            maskRect.anchorMax = Vector2.one;
            maskRect.offsetMin = Vector2.zero;
            maskRect.offsetMax = Vector2.zero;
            Image mask = maskObject.GetComponent<Image>();
            mask.color = new Color(0f, 0f, 0f, 0.58f);
            mask.type = Image.Type.Filled;
            mask.fillMethod = Image.FillMethod.Radial360;
            mask.fillOrigin = 2;
            mask.fillClockwise = false;
            cooldownMask[i] = mask;

            GameObject buttonObject = new GameObject("Upgrade", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(slot, false);
            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(1f, 1f);
            buttonRect.anchorMax = new Vector2(1f, 1f);
            buttonRect.pivot = new Vector2(1f, 1f);
            buttonRect.anchoredPosition = new Vector2(-3f, -3f);
            buttonRect.sizeDelta = new Vector2(25f, 25f);
            buttonObject.GetComponent<Image>().color = new Color(0.15f, 0.62f, 1f, 1f);
            Label("Plus", buttonRect, "+", 20, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(24f, 24f), Color.white, new Vector2(0.5f, 0.5f));
            int captured = i;
            Button button = buttonObject.GetComponent<Button>();
            button.onClick.AddListener(() => UpgradeAbility(captured));
            upgradeButtons[i] = button;
        }

        RectTransform side = Panel("SideInfo", root, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-22f, 0f), new Vector2(250f, 165f), panel);
        goldText = Label("Gold", side, "◈ 0", 22, TextAnchor.MiddleLeft, new Vector2(15f, -18f), new Vector2(210f, 32f), gold, new Vector2(0f, 1f));
        skillPointText = Label("SkillPoints", side, "SKILL POINTS 0", 13, TextAnchor.MiddleLeft, new Vector2(15f, -52f), new Vector2(210f, 26f), new Color(0.34f, 0.76f, 1f), new Vector2(0f, 1f));
        statText = Label("Stats", side, "AD 0  AS 0.00\nMS 0.0  HP 0", 13, TextAnchor.UpperLeft, new Vector2(15f, -82f), new Vector2(210f, 48f), new Color(0.72f, 0.84f, 0.92f), new Vector2(0f, 1f));

        RectTransform items = Panel("Items", side, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(15f, 12f), new Vector2(220f, 34f), new Color(0f,0f,0f,0f));
        for (int i = 0; i < 6; i++)
        {
            RectTransform slot = Panel("Item_" + i, items, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(i * 36f, 0f), new Vector2(30f, 30f), new Color(0.035f, 0.06f, 0.09f, 1f));
            itemSlots[i] = slot.GetComponent<Image>();
        }

        Label("MarketHint", root, "P  AETHER MARKET", 12, TextAnchor.MiddleCenter, new Vector2(0f, -88f), new Vector2(300f, 20f), new Color(0.58f, 0.72f, 0.82f), new Vector2(0.5f, 0.5f));
    }

    private void UpgradeAbility(int slot)
    {
        progression?.UpgradeAbility(slot);
    }

    private void RefreshVitals()
    {
        if (stats != null)
        {
            float hpRatio = Mathf.Clamp01(stats.hp / Mathf.Max(1f, stats.maxHp));
            hpFill.fillAmount = hpRatio;
            hpText.text = Mathf.CeilToInt(stats.hp) + " / " + Mathf.CeilToInt(stats.maxHp);
        }

        if (progression != null)
        {
            levelText.text = progression.level.ToString();
            xpFill.fillAmount = progression.ExperienceRatio;
            xpText.text = progression.level >= progression.maxLevel ? "MAX LEVEL" : "XP " + progression.experience + " / " + progression.experienceToNext;
            skillPointText.text = "SKILL POINTS  " + progression.unspentSkillPoints;
        }
    }

    private void RefreshAbilities()
    {
        for (int i = 0; i < 4; i++)
        {
            abilityNameText[i].text = GetAbilityName(i);
            float ratio = provider != null ? provider.GetAbilityCooldownRatio(i) : 0f;
            float duration = provider != null ? provider.GetAbilityCooldownDuration(i) : 0f;
            cooldownMask[i].fillAmount = ratio;
            cooldownText[i].text = ratio > 0.01f ? Mathf.CeilToInt(ratio * duration).ToString() : string.Empty;

            int rank = progression != null ? progression.GetAbilityRank(i) : 0;
            int max = progression != null ? (i == 3 ? progression.ultimateMaxRank : progression.basicAbilityMaxRank) : (i == 3 ? 3 : 5);
            abilityRankText[i].text = rank + "/" + max;
            bool canUpgrade = progression != null && progression.CanUpgradeAbility(i);
            upgradeButtons[i].gameObject.SetActive(canUpgrade);
        }
    }

    private void RefreshEconomy()
    {
        if (economy == null)
            return;

        goldText.text = "◈ " + economy.gold;
        for (int i = 0; i < itemSlots.Length; i++)
        {
            if (i < economy.inventory.Count)
            {
                Color c = economy.inventory[i].accent;
                itemSlots[i].color = new Color(c.r * 0.82f, c.g * 0.82f, c.b * 0.82f, 1f);
            }
            else
            {
                itemSlots[i].color = new Color(0.035f, 0.06f, 0.09f, 1f);
            }
        }
    }

    private void RefreshStats()
    {
        if (stats == null)
            return;

        float attackSpeed = 1f / Mathf.Max(0.01f, stats.attackCooldown);
        statText.text = "AD  " + Mathf.RoundToInt(stats.attackDamage) + "   AS  " + attackSpeed.ToString("0.00") + "\nMS  " + stats.moveSpeed.ToString("0.0") + "   HP  " + Mathf.RoundToInt(stats.maxHp);
    }

    private string GetAbilityName(int slot)
    {
        if (provider != null)
            return provider.GetAbilityName(slot);

        return slot == 0 ? "ABILITY I" : slot == 1 ? "ABILITY II" : slot == 2 ? "ABILITY III" : "ULTIMATE";
    }

    private Image BuildBar(Transform parent, string name, Vector2 pos, Vector2 size, Color fillColor)
    {
        RectTransform background = Panel(name + "_BG", parent, new Vector2(0f, 1f), new Vector2(0f, 1f), pos, size, new Color(0.01f, 0.018f, 0.026f, 1f));
        GameObject fillObject = new GameObject(name + "_FILL", typeof(RectTransform), typeof(Image));
        fillObject.transform.SetParent(background, false);
        RectTransform rect = fillObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(2f, 2f);
        rect.offsetMax = new Vector2(-2f, -2f);
        Image image = fillObject.GetComponent<Image>();
        image.color = fillColor;
        image.type = Image.Type.Filled;
        image.fillMethod = Image.FillMethod.Horizontal;
        return image;
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
        go.GetComponent<Image>().color = color;
        return rect;
    }

    private Text Label(string name, Transform parent, string value, int size, TextAnchor alignment, Vector2 position, Vector2 dimensions, Color color, Vector2 anchor)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = dimensions;
        Text text = go.GetComponent<Text>();
        text.font = font;
        text.text = value;
        text.fontSize = size;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }
}
