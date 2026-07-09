using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Runtime shop UI for the production slice. Press P to toggle. Purchases require proximity to team base.
/// </summary>
public class AOGShopHUDRuntime : MonoBehaviour
{
    private CanvasGroup group;
    private Text goldText;
    private Text inventoryText;
    private readonly List<Item> catalog = new();
    private Champion localChampion;
    private AOGChampionInventoryRuntime inventory;
    private Transform blueBase;
    private Transform redBase;
    private bool visible;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindObjectOfType<AOGShopHUDRuntime>() != null)
            return;

        GameObject obj = new GameObject("AOG_Shop_HUD_Runtime");
        obj.AddComponent<AOGShopHUDRuntime>();
    }

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        catalog.AddRange(AOGItemCatalogRuntime.CreateCoreCatalog());
        BuildUI();
        SetVisible(false);
    }

    void Update()
    {
        ResolvePlayerAndBases();

        if (Input.GetKeyDown(KeyCode.P))
            SetVisible(!visible);

        RefreshText();
    }

    private void ResolvePlayerAndBases()
    {
        if (localChampion == null)
        {
            ChampionController controller = FindObjectOfType<ChampionController>();
            if (controller != null)
            {
                localChampion = controller.GetComponent<Champion>();
                if (localChampion != null)
                {
                    inventory = localChampion.GetComponent<AOGChampionInventoryRuntime>();
                    if (inventory == null)
                        inventory = localChampion.gameObject.AddComponent<AOGChampionInventoryRuntime>();
                }
            }
        }

        if (blueBase == null || redBase == null)
        {
            MinionSpawner spawner = FindObjectOfType<MinionSpawner>();
            if (spawner != null)
            {
                blueBase = spawner.blueBaseSpawn;
                redBase = spawner.redBaseSpawn;
            }
        }
    }

    private bool IsNearBase()
    {
        if (localChampion == null)
            return false;

        Transform teamBase = localChampion.Team == TeamType.Red ? redBase : blueBase;
        return teamBase != null && Vector3.Distance(localChampion.transform.position, teamBase.position) <= 12f;
    }

    private void BuildUI()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 520;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        group = gameObject.AddComponent<CanvasGroup>();

        GameObject panel = new GameObject("ShopPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(transform, false);
        RectTransform pr = panel.GetComponent<RectTransform>();
        pr.anchorMin = pr.anchorMax = new Vector2(0.5f, 0.5f);
        pr.sizeDelta = new Vector2(920f, 640f);
        panel.GetComponent<Image>().color = new Color(0.012f, 0.025f, 0.055f, 0.96f);

        goldText = CreateText(panel.transform, "Gold", "0 G", 26, new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(300f, 48f));
        inventoryText = CreateText(panel.transform, "Inventory", "INVENTORY", 20, new Vector2(0.5f, 0f), new Vector2(0f, 32f), new Vector2(820f, 80f));

        for (int i = 0; i < catalog.Count; i++)
        {
            Item item = catalog[i];
            int column = i % 2;
            int row = i / 2;
            float x = column == 0 ? -220f : 220f;
            float y = 190f - row * 130f;

            GameObject buttonObj = new GameObject("Buy_" + item.Name, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObj.transform.SetParent(panel.transform, false);
            RectTransform br = buttonObj.GetComponent<RectTransform>();
            br.anchorMin = br.anchorMax = new Vector2(0.5f, 0.5f);
            br.anchoredPosition = new Vector2(x, y);
            br.sizeDelta = new Vector2(390f, 100f);
            buttonObj.GetComponent<Image>().color = new Color(0.05f, 0.12f, 0.22f, 0.95f);

            Item captured = item;
            buttonObj.GetComponent<Button>().onClick.AddListener(() => Buy(captured));
            CreateText(buttonObj.transform, "Label", $"{item.Name}\n{item.Cost} G  •  {item.Description}", 18, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(360f, 84f));
        }

        GameObject undoObj = new GameObject("UndoButton", typeof(RectTransform), typeof(Image), typeof(Button));
        undoObj.transform.SetParent(panel.transform, false);
        RectTransform ur = undoObj.GetComponent<RectTransform>();
        ur.anchorMin = ur.anchorMax = new Vector2(0.5f, 0f);
        ur.anchoredPosition = new Vector2(0f, 105f);
        ur.sizeDelta = new Vector2(220f, 54f);
        undoObj.GetComponent<Image>().color = new Color(0.20f, 0.08f, 0.28f, 0.95f);
        undoObj.GetComponent<Button>().onClick.AddListener(Undo);
        CreateText(undoObj.transform, "Label", "UNDO PURCHASE", 18, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(210f, 48f));
    }

    private void Buy(Item item)
    {
        if (!IsNearBase() || inventory == null)
        {
            AOGAudioDirectorRuntime.Instance?.PlayCue(AOGAudioCue.UIBack);
            return;
        }

        bool bought = inventory.TryBuy(item);
        AOGAudioDirectorRuntime.Instance?.PlayCue(bought ? AOGAudioCue.UIConfirm : AOGAudioCue.UIBack);
        RefreshText();
    }

    private void Undo()
    {
        if (!IsNearBase() || inventory == null)
            return;

        bool undone = inventory.UndoLastPurchase();
        AOGAudioDirectorRuntime.Instance?.PlayCue(undone ? AOGAudioCue.UIConfirm : AOGAudioCue.UIBack);
        RefreshText();
    }

    private void RefreshText()
    {
        if (goldText != null)
            goldText.text = localChampion != null ? $"{localChampion.Gold} G" : "NO PLAYER";

        if (inventoryText == null)
            return;

        if (inventory == null || inventory.Items.Count == 0)
        {
            inventoryText.text = "INVENTORY: EMPTY";
            return;
        }

        string text = "INVENTORY: ";
        for (int i = 0; i < inventory.Items.Count; i++)
        {
            if (i > 0) text += "  •  ";
            text += inventory.Items[i].Name;
        }
        inventoryText.text = text;
    }

    private void SetVisible(bool value)
    {
        visible = value;
        if (group == null)
            return;
        group.alpha = value ? 1f : 0f;
        group.interactable = value;
        group.blocksRaycasts = value;
    }

    private static Text CreateText(Transform parent, string name, string value, int size, Vector2 anchor, Vector2 position, Vector2 dimensions)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Text));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = dimensions;

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
