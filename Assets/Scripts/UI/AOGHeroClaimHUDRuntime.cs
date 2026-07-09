using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AOGHeroClaimHUDRuntime : MonoBehaviour
{
    private CanvasGroup group;
    private Text phaseText;
    private Text claimText;
    private readonly List<string> heroIds = new() { "nyxara_rift_dancer", "kharvos_worldbreaker", "veyra_vector_saint" };
    private string localPlayerId = "LOCAL_PLAYER";
    private TeamType localTeam = TeamType.Blue;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        if (FindObjectOfType<AOGHeroClaimHUDRuntime>() != null) return;
        new GameObject("AOG_Hero_Claim_HUD_Runtime").AddComponent<AOGHeroClaimHUDRuntime>();
    }

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        BuildUI();
    }

    void Update()
    {
        AOGDraftBanRuntime draft = FindObjectOfType<AOGDraftBanRuntime>();
        bool visible = draft != null && draft.Phase != AOGDraftPhase.Idle && draft.Phase != AOGDraftPhase.Locked;
        SetVisible(visible);
        if (!visible || draft == null) return;

        phaseText.text = $"HERO CLAIM  •  {Mathf.CeilToInt(draft.PhaseRemaining)}";
        claimText.text = "Performance Priority: 55% strength-adjusted wins • 15% raw win rate • 12% role impact • 8% KDA • 5% objectives • 3% confidence • 2% recent form";
    }

    void BuildUI()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 700;
        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        group = gameObject.AddComponent<CanvasGroup>();

        GameObject panel = Panel("HeroClaimPanel", transform, new Vector2(0.5f, 0.5f), new Vector2(1180f, 720f), new Vector2(0f, 0f), new Color(0.008f, 0.018f, 0.045f, 0.97f));
        phaseText = CreateText(panel.transform, "Phase", "HERO CLAIM", 40, new Vector2(0.5f, 1f), new Vector2(0f, -54f), new Vector2(900f, 64f));
        claimText = CreateText(panel.transform, "Rule", string.Empty, 17, new Vector2(0.5f, 1f), new Vector2(0f, -108f), new Vector2(1000f, 54f));

        for (int i = 0; i < heroIds.Count; i++)
        {
            string hero = heroIds[i];
            float x = -360f + i * 360f;
            GameObject card = Panel("Card_" + hero, panel.transform, new Vector2(0.5f, 0.5f), new Vector2(300f, 430f), new Vector2(x, -20f), new Color(0.025f, 0.07f, 0.13f, 0.96f));
            CreateText(card.transform, "Name", DisplayName(hero), 30, new Vector2(0.5f, 1f), new Vector2(0f, -46f), new Vector2(260f, 52f));
            CreateText(card.transform, "Role", RoleName(hero), 18, new Vector2(0.5f, 1f), new Vector2(0f, -94f), new Vector2(260f, 40f));

            GameObject button = Panel("Claim", card.transform, new Vector2(0.5f, 0f), new Vector2(220f, 58f), new Vector2(0f, 40f), new Color(0.09f, 0.42f, 0.78f, 0.95f));
            Button b = button.AddComponent<Button>();
            string captured = hero;
            b.onClick.AddListener(() => Submit(captured));
            CreateText(button.transform, "Label", "CLAIM", 21, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(200f, 48f));
        }
    }

    void Submit(string heroId)
    {
        AOGDraftBanRuntime draft = FindObjectOfType<AOGDraftBanRuntime>();
        if (draft == null) return;
        bool ok = draft.SubmitClaim(localPlayerId, localTeam, heroId);
        AOGAudioDirectorRuntime.Instance?.PlayCue(ok ? AOGAudioCue.UIConfirm : AOGAudioCue.UIBack);
    }

    void SetVisible(bool visible)
    {
        if (group == null) return;
        group.alpha = visible ? 1f : 0f;
        group.interactable = visible;
        group.blocksRaycasts = visible;
    }

    static string DisplayName(string id) => id switch
    {
        "nyxara_rift_dancer" => "NYXARA",
        "kharvos_worldbreaker" => "KHARVOS",
        "veyra_vector_saint" => "VEYRA",
        _ => id.ToUpperInvariant()
    };

    static string RoleName(string id) => id switch
    {
        "nyxara_rift_dancer" => "DUELIST / BATTLEMAGE",
        "kharvos_worldbreaker" => "VANGUARD / CONTROLLER",
        "veyra_vector_saint" => "MARKSMAN / CONTROLLER",
        _ => "FLEX"
    };

    static GameObject Panel(string name, Transform parent, Vector2 anchor, Vector2 size, Vector2 pos, Color color)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = anchor;
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;
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
