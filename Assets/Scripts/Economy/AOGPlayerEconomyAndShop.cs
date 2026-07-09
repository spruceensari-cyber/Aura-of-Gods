using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

[Serializable]
public class AOGItemDefinition
{
    public string id;
    public string displayName;
    public string description;
    public int cost;
    public float bonusHp;
    public float bonusDamage;
    public float bonusMoveSpeed;
    public float attackCooldownMultiplier = 1f;
    public Color accent;
}

public class AOGPlayerEconomy : MonoBehaviour
{
    public int gold = 500;
    public int inventoryCapacity = 6;
    public readonly List<AOGItemDefinition> inventory = new List<AOGItemDefinition>();

    private AOGCharacterStats stats;

    public event Action EconomyChanged;

    private void Awake()
    {
        stats = GetComponent<AOGCharacterStats>();
    }

    public void AddGold(int amount)
    {
        if (amount <= 0)
            return;

        gold += amount;
        EconomyChanged?.Invoke();
    }

    public bool CanBuy(AOGItemDefinition item)
    {
        return item != null && gold >= item.cost && inventory.Count < inventoryCapacity;
    }

    public bool Buy(AOGItemDefinition item)
    {
        if (!CanBuy(item))
            return false;

        gold -= item.cost;
        inventory.Add(item);
        ApplyItem(item);
        EconomyChanged?.Invoke();
        return true;
    }

    private void ApplyItem(AOGItemDefinition item)
    {
        if (stats == null)
            stats = GetComponent<AOGCharacterStats>();

        if (stats == null)
            return;

        if (item.bonusHp > 0f)
        {
            float oldMax = stats.maxHp;
            stats.maxHp += item.bonusHp;
            stats.hp += stats.maxHp - oldMax;
        }

        stats.attackDamage += item.bonusDamage;
        stats.moveSpeed += item.bonusMoveSpeed;
        stats.attackCooldown = Mathf.Max(0.25f, stats.attackCooldown * Mathf.Clamp(item.attackCooldownMultiplier, 0.45f, 1.5f));
    }
}

[DefaultExecutionOrder(600)]
public class AOGShopRuntime : MonoBehaviour
{
    public static AOGShopRuntime Instance { get; private set; }

    private Canvas canvas;
    private RectTransform panel;
    private Text goldText;
    private Text statusText;
    private Font font;
    private AOGPlayerEconomy economy;
    private bool open;
    private readonly List<Image> inventoryIcons = new List<Image>();
    private readonly List<AOGItemDefinition> catalog = new List<AOGItemDefinition>();

    private readonly Color ink = new Color(0.012f, 0.022f, 0.035f, 0.985f);
    private readonly Color panelColor = new Color(0.025f, 0.045f, 0.065f, 0.985f);
    private readonly Color steel = new Color(0.18f, 0.27f, 0.34f, 1f);
    private readonly Color gold = new Color(0.92f, 0.72f, 0.28f, 1f);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        BuildCatalog();
        EnsureEventSystem();
        BuildUI();
        SetOpen(false);
    }

    private void Update()
    {
        if (AOGInputBridge.KeyPressedThisFrame(KeyCode.P))
            SetOpen(!open);

        if (AOGInputBridge.KeyPressedThisFrame(KeyCode.Escape) && open)
            SetOpen(false);

        if (economy == null)
            FindEconomy();

        RefreshHeader();
    }

    public void Bind(AOGPlayerEconomy targetEconomy)
    {
        if (economy != null)
            economy.EconomyChanged -= RefreshAll;

        economy = targetEconomy;
        if (economy != null)
            economy.EconomyChanged += RefreshAll;

        RefreshAll();
    }

    public void Toggle()
    {
        SetOpen(!open);
    }

    private void SetOpen(bool value)
    {
        open = value;
        if (panel != null)
            panel.gameObject.SetActive(open);
    }

    private void FindEconomy()
    {
        AOGPlayerEconomy[] economies = FindObjectsByType<AOGPlayerEconomy>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (AOGPlayerEconomy candidate in economies)
        {
            if (candidate == null)
                continue;

            AOGActiveChampion active = candidate.GetComponent<AOGActiveChampion>();
            if (active != null && active.IsActiveChampion)
            {
                Bind(candidate);
                return;
            }
        }

        if (economies.Length > 0)
            Bind(economies[0]);
    }

    private void BuildCatalog()
    {
        catalog.Add(new AOGItemDefinition
        {
            id = "moonblade",
            displayName = "MOONFALL BLADE",
            description = "+35 Attack Damage",
            cost = 1100,
            bonusDamage = 35f,
            accent = new Color(0.48f, 0.55f, 1f)
        });
        catalog.Add(new AOGItemDefinition
        {
            id = "aetherboots",
            displayName = "AETHER BOOTS",
            description = "+1.3 Move Speed",
            cost = 900,
            bonusMoveSpeed = 1.3f,
            accent = new Color(0.22f, 0.82f, 0.92f)
        });
        catalog.Add(new AOGItemDefinition
        {
            id = "titanheart",
            displayName = "TITAN HEART",
            description = "+420 Maximum HP",
            cost = 1250,
            bonusHp = 420f,
            accent = new Color(0.96f, 0.26f, 0.32f)
        });
        catalog.Add(new AOGItemDefinition
        {
            id = "starfang",
            displayName = "STARFANG",
            description = "+22 Damage, +10% Attack Speed",
            cost = 1450,
            bonusDamage = 22f,
            attackCooldownMultiplier = 0.90f,
            accent = new Color(0.94f, 0.72f, 0.20f)
        });
        catalog.Add(new AOGItemDefinition
        {
            id = "voidglass",
            displayName = "VOIDGLASS EDGE",
            description = "+48 Attack Damage",
            cost = 1850,
            bonusDamage = 48f,
            accent = new Color(0.66f, 0.22f, 0.92f)
        });
        catalog.Add(new AOGItemDefinition
        {
            id = "warstride",
            displayName = "WARSTRIDE RELIC",
            description = "+250 HP, +0.7 Move Speed",
            cost = 1500,
            bonusHp = 250f,
            bonusMoveSpeed = 0.7f,
            accent = new Color(0.42f, 0.86f, 0.48f)
        });
        catalog.Add(new AOGItemDefinition
        {
            id = "eclipse",
            displayName = "ECLIPSE CROWN",
            description = "+30 Damage, +300 HP",
            cost = 2200,
            bonusDamage = 30f,
            bonusHp = 300f,
            accent = new Color(0.52f, 0.28f, 0.82f)
        });
        catalog.Add(new AOGItemDefinition
        {
            id = "godbreaker",
            displayName = "GODBREAKER",
            description = "+70 Attack Damage",
            cost = 2800,
            bonusDamage = 70f,
            accent = new Color(1f, 0.42f, 0.12f)
        });
    }

    private void BuildUI()
    {
        GameObject canvasObject = new GameObject("ShopCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1400;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        panel = CreatePanel("AetherMarket", canvas.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(26f, 0f), new Vector2(760f, 820f), ink);
        Outline outline = panel.gameObject.AddComponent<Outline>();
        outline.effectColor = gold;
        outline.effectDistance = new Vector2(2f, -2f);

        CreateText("Title", panel, "AETHER MARKET", 34, TextAnchor.MiddleLeft, new Vector2(35f, -35f), new Vector2(420f, 50f), gold, new Vector2(0f, 1f));
        CreateText("Subtitle", panel, "BUILD YOUR ASCENSION", 15, TextAnchor.MiddleLeft, new Vector2(37f, -77f), new Vector2(420f, 32f), new Color(0.55f, 0.68f, 0.78f), new Vector2(0f, 1f));

        goldText = CreateText("Gold", panel, "◈ 500", 26, TextAnchor.MiddleRight, new Vector2(-36f, -50f), new Vector2(220f, 48f), gold, new Vector2(1f, 1f));

        for (int i = 0; i < catalog.Count; i++)
            BuildItemCard(i, catalog[i]);

        statusText = CreateText("Status", panel, "P: CLOSE MARKET", 15, TextAnchor.MiddleLeft, new Vector2(35f, 26f), new Vector2(660f, 34f), new Color(0.56f, 0.70f, 0.80f), new Vector2(0f, 0f));

        RectTransform inventory = CreatePanel("Inventory", panel, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-28f, 26f), new Vector2(370f, 70f), panelColor);
        for (int i = 0; i < 6; i++)
        {
            RectTransform slot = CreatePanel("Slot_" + i, inventory, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(34f + i * 60f, 0f), new Vector2(48f, 48f), new Color(0.04f, 0.07f, 0.095f));
            Outline slotOutline = slot.gameObject.AddComponent<Outline>();
            slotOutline.effectColor = steel;
            slotOutline.effectDistance = new Vector2(1f, -1f);
            Image icon = slot.GetComponent<Image>();
            inventoryIcons.Add(icon);
        }
    }

    private void BuildItemCard(int index, AOGItemDefinition item)
    {
        int column = index % 2;
        int row = index / 2;
        float x = 30f + column * 360f;
        float y = -130f - row * 145f;

        RectTransform card = CreatePanel("Item_" + item.id, panel, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(x, y), new Vector2(340f, 126f), panelColor);
        Outline outline = card.gameObject.AddComponent<Outline>();
        outline.effectColor = item.accent;
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        RectTransform icon = CreatePanel("Icon", card, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(14f, 0f), new Vector2(84f, 84f), new Color(item.accent.r * 0.26f, item.accent.g * 0.26f, item.accent.b * 0.26f, 1f));
        Outline iconOutline = icon.gameObject.AddComponent<Outline>();
        iconOutline.effectColor = item.accent;
        iconOutline.effectDistance = new Vector2(2f, -2f);
        CreateText("Rune", icon, "◆", 38, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(74f, 74f), item.accent);

        CreateText("Name", card, item.displayName, 17, TextAnchor.MiddleLeft, new Vector2(112f, -14f), new Vector2(205f, 28f), Color.white, new Vector2(0f, 1f));
        CreateText("Description", card, item.description, 13, TextAnchor.MiddleLeft, new Vector2(112f, -48f), new Vector2(205f, 26f), new Color(0.70f, 0.80f, 0.88f), new Vector2(0f, 1f));

        GameObject buttonObject = new GameObject("Buy", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(card, false);
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(1f, 0f);
        buttonRect.anchorMax = new Vector2(1f, 0f);
        buttonRect.pivot = new Vector2(1f, 0f);
        buttonRect.anchoredPosition = new Vector2(-10f, 10f);
        buttonRect.sizeDelta = new Vector2(112f, 34f);
        buttonObject.GetComponent<Image>().color = new Color(item.accent.r * 0.35f, item.accent.g * 0.35f, item.accent.b * 0.35f, 1f);
        Button button = buttonObject.GetComponent<Button>();
        AOGItemDefinition captured = item;
        button.onClick.AddListener(() => TryBuy(captured));
        CreateText("Price", buttonRect, "◈ " + item.cost, 14, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(104f, 30f), gold);
    }

    private void TryBuy(AOGItemDefinition item)
    {
        if (economy == null)
        {
            statusText.text = "NO ACTIVE CHAMPION ECONOMY";
            return;
        }

        if (economy.inventory.Count >= economy.inventoryCapacity)
        {
            statusText.text = "INVENTORY FULL";
            return;
        }

        if (economy.gold < item.cost)
        {
            statusText.text = "NOT ENOUGH GOLD";
            return;
        }

        if (economy.Buy(item))
        {
            statusText.text = item.displayName + " PURCHASED";
            RefreshAll();
        }
    }

    private void RefreshHeader()
    {
        if (goldText != null)
            goldText.text = economy != null ? "◈ " + economy.gold : "◈ ---";
    }

    private void RefreshAll()
    {
        RefreshHeader();

        for (int i = 0; i < inventoryIcons.Count; i++)
        {
            Image icon = inventoryIcons[i];
            if (i < (economy != null ? economy.inventory.Count : 0))
            {
                AOGItemDefinition item = economy.inventory[i];
                icon.color = new Color(item.accent.r * 0.55f, item.accent.g * 0.55f, item.accent.b * 0.55f, 1f);
            }
            else
            {
                icon.color = new Color(0.04f, 0.07f, 0.095f, 1f);
            }
        }
    }

    private void EnsureEventSystem()
    {
        if (EventSystem.current != null)
            return;

        GameObject eventSystem = new GameObject("AOG_EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
        eventSystem.AddComponent<InputSystemUIInputModule>();
#else
        eventSystem.AddComponent<StandaloneInputModule>();
#endif
        DontDestroyOnLoad(eventSystem);
    }

    private RectTransform CreatePanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = anchorMin;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        go.GetComponent<Image>().color = color;
        return rect;
    }

    private Text CreateText(string name, Transform parent, string value, int size, TextAnchor alignment, Vector2 position, Vector2 dimensions, Color color, Vector2? anchor = null)
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

        Text text = go.GetComponent<Text>();
        text.font = font;
        text.text = value;
        text.fontSize = size;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }
}

public class AOGShopBootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindFirstObjectByType<AOGShopRuntime>() == null)
        {
            GameObject shop = new GameObject("AOG_Aether_Market");
            DontDestroyOnLoad(shop);
            shop.AddComponent<AOGShopRuntime>();
        }

        if (FindFirstObjectByType<AOGShopBootstrap>() == null)
        {
            GameObject host = new GameObject("AOG_Shop_Bootstrap");
            DontDestroyOnLoad(host);
            host.AddComponent<AOGShopBootstrap>();
        }
    }

    private void Start()
    {
        InvokeRepeating(nameof(AttachEconomies), 0.2f, 1f);
    }

    private void AttachEconomies()
    {
        AOGCharacterStats[] heroes = FindObjectsByType<AOGCharacterStats>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (AOGCharacterStats hero in heroes)
        {
            if (hero == null)
                continue;

            AOGPlayerEconomy economy = hero.GetComponent<AOGPlayerEconomy>();
            if (economy == null)
                economy = hero.gameObject.AddComponent<AOGPlayerEconomy>();

            AOGActiveChampion active = hero.GetComponent<AOGActiveChampion>();
            if (active != null && active.IsActiveChampion)
                AOGShopRuntime.Instance?.Bind(economy);
        }
    }
}
