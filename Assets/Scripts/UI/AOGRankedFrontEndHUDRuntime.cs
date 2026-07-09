using UnityEngine;
using UnityEngine.UI;

public class AOGRankedFrontEndHUDRuntime : MonoBehaviour
{
    private CanvasGroup group;
    private Text rankText;
    private Text placementText;
    private Text integrityText;
    private Text autofillText;
    private string localPlayerId = "LOCAL_PLAYER";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        if (FindObjectOfType<AOGRankedFrontEndHUDRuntime>() != null) return;
        new GameObject("AOG_Ranked_FrontEnd_HUD_Runtime").AddComponent<AOGRankedFrontEndHUDRuntime>();
    }

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        BuildUI();
    }

    void Update()
    {
        AOGRankProfile rank = AOGRankedProgressionRuntime.Instance?.GetOrCreate(localPlayerId);
        AOGIntegrityProfile integrity = AOGPlayerIntegrityRuntime.Instance?.GetOrCreate(localPlayerId);
        if (rank != null)
        {
            rankText.text = $"{rank.Tier}  {rank.Division}   •   {rank.Elo} ELO";
            placementText.text = rank.PlacementComplete
                ? "PLACEMENT COMPLETE"
                : $"PLACEMENT {rank.PlacementGames}/10   •   WINS {rank.PlacementWins}";
            autofillText.text = $"AUTOFILL PROTECTION  ×{rank.AutofillProtectionCharges}";
        }

        if (integrity != null)
        {
            integrityText.text = integrity.IsRestricted
                ? $"QUEUE LOCK  {Mathf.CeilToInt(integrity.QueueLockMinutes)} MIN"
                : $"INTEGRITY  {Mathf.Max(0f, 100f - integrity.PenaltyScore * 10f):0}";
        }
    }

    void BuildUI()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 260;
        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        group = gameObject.AddComponent<CanvasGroup>();

        GameObject panel = NewPanel("RankedCard", transform, new Vector2(1f, 0.5f), new Vector2(470f, 300f), new Vector2(-34f, 0f), new Color(0.01f, 0.025f, 0.06f, 0.94f));
        CreateText(panel.transform, "Title", "RANKED AETHER", 30, new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(420f, 50f));
        rankText = CreateText(panel.transform, "Rank", "SILVER 3 • 1200 ELO", 26, new Vector2(0.5f, 1f), new Vector2(0f, -95f), new Vector2(420f, 48f));
        placementText = CreateText(panel.transform, "Placement", "PLACEMENT 0/10", 18, new Vector2(0.5f, 1f), new Vector2(0f, -145f), new Vector2(420f, 40f));
        autofillText = CreateText(panel.transform, "Autofill", "AUTOFILL PROTECTION ×2", 18, new Vector2(0.5f, 1f), new Vector2(0f, -190f), new Vector2(420f, 40f));
        integrityText = CreateText(panel.transform, "Integrity", "INTEGRITY 100", 18, new Vector2(0.5f, 1f), new Vector2(0f, -235f), new Vector2(420f, 40f));
    }

    static GameObject NewPanel(string name, Transform parent, Vector2 anchor, Vector2 size, Vector2 pos, Color color)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.sizeDelta = size;
        rect.anchoredPosition = pos;
        obj.GetComponent<Image>().color = color;
        return obj;
    }

    static Text CreateText(Transform parent, string name, string value, int size, Vector2 anchor, Vector2 pos, Vector2 dims)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Text));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = anchor;
        rect.anchoredPosition = pos;
        rect.sizeDelta = dims;
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
