using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(1900)]
public class AOGTeamRosterHudRuntime : MonoBehaviour
{
    private sealed class RosterSlot
    {
        public AOGTeamMemberIdentity member;
        public Image background;
        public Image portrait;
        public Image healthFill;
        public Text glyph;
        public Text name;
        public Text role;
        public Text level;
        public Text state;
    }

    private static AOGTeamRosterHudRuntime instance;
    private readonly List<RosterSlot> blueSlots = new List<RosterSlot>();
    private readonly List<RosterSlot> redSlots = new List<RosterSlot>();

    private Canvas canvas;
    private Font font;
    private RectTransform blueRoot;
    private RectTransform redRoot;
    private Text primaryBlueScore;
    private Text primaryRedScore;
    private Text primaryTimer;
    private int rosterSignature = int.MinValue;
    private float nextPrimaryResolve;

    public static void Ensure()
    {
        if (instance != null)
        {
            instance.rosterSignature = int.MinValue;
            return;
        }

        GameObject host = new GameObject("AOG_Team_Roster_Hud");
        instance = host.AddComponent<AOGTeamRosterHudRuntime>();
        DontDestroyOnLoad(host);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        BuildCanvas();
    }

    private void Update()
    {
        int signature = GetRosterSignature();
        if (signature != rosterSignature)
        {
            rosterSignature = signature;
            RebuildSlots();
        }

        if (Time.unscaledTime >= nextPrimaryResolve)
        {
            nextPrimaryResolve = Time.unscaledTime + 0.5f;
            ResolvePrimaryHeader();
        }

        RefreshSlots(blueSlots, MinionTeam.Blue);
        RefreshSlots(redSlots, MinionTeam.Red);
    }

    private void BuildCanvas()
    {
        GameObject canvasObject = new GameObject("TeamRosterCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvasObject.transform.SetParent(transform, false);
        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 2750;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        blueRoot = CreateRoot("BlueRosterRail", new Vector2(0f, 1f), new Vector2(16f, -86f), new Vector2(252f, 324f));
        redRoot = CreateRoot("RedRosterRail", new Vector2(1f, 1f), new Vector2(-16f, -86f), new Vector2(252f, 324f));
    }

    private RectTransform CreateRoot(string name, Vector2 anchor, Vector2 position, Vector2 size)
    {
        GameObject root = new GameObject(name, typeof(RectTransform));
        root.transform.SetParent(canvas.transform, false);
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    private int GetRosterSignature()
    {
        unchecked
        {
            int signature = AOGRoleBasedTeamRuntime.BlueRoster.Count * 397 ^ AOGRoleBasedTeamRuntime.RedRoster.Count;
            foreach (AOGTeamMemberIdentity member in AOGRoleBasedTeamRuntime.BlueRoster)
                signature = signature * 31 + (member != null ? member.GetInstanceID() : 0);
            foreach (AOGTeamMemberIdentity member in AOGRoleBasedTeamRuntime.RedRoster)
                signature = signature * 31 + (member != null ? member.GetInstanceID() : 0);
            return signature;
        }
    }

    private void RebuildSlots()
    {
        ClearSlots(blueRoot, blueSlots);
        ClearSlots(redRoot, redSlots);

        BuildSide(AOGRoleBasedTeamRuntime.BlueRoster, blueRoot, true, blueSlots);
        BuildSide(AOGRoleBasedTeamRuntime.RedRoster, redRoot, false, redSlots);
    }

    private void ClearSlots(RectTransform root, List<RosterSlot> slots)
    {
        slots.Clear();
        if (root == null)
            return;

        for (int i = root.childCount - 1; i >= 0; i--)
            Destroy(root.GetChild(i).gameObject);
    }

    private void BuildSide(List<AOGTeamMemberIdentity> roster, RectTransform root, bool blue, List<RosterSlot> output)
    {
        for (int index = 0; index < roster.Count && index < 5; index++)
        {
            AOGTeamMemberIdentity member = roster[index];
            if (member == null)
                continue;

            Color teamColor = blue ? new Color(0.20f, 0.66f, 1f) : new Color(1f, 0.26f, 0.30f);
            Color background = blue ? new Color(0.008f, 0.035f, 0.065f, 0.90f) : new Color(0.070f, 0.010f, 0.020f, 0.90f);
            Vector2 anchor = blue ? new Vector2(0f, 1f) : new Vector2(1f, 1f);

            RectTransform row = Panel((blue ? "Blue" : "Red") + "_Roster_" + member.role, root, anchor, anchor,
                new Vector2(0f, -index * 62f), new Vector2(252f, 56f), background, false);
            row.pivot = anchor;
            Outline outline = row.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(teamColor.r, teamColor.g, teamColor.b, 0.60f);
            outline.effectDistance = new Vector2(1f, -1f);

            RectTransform portrait = Panel("Portrait", row, blue ? new Vector2(0f, 0.5f) : new Vector2(1f, 0.5f),
                blue ? new Vector2(0f, 0.5f) : new Vector2(1f, 0.5f), blue ? new Vector2(7f, 0f) : new Vector2(-7f, 0f),
                new Vector2(46f, 46f), new Color(teamColor.r * 0.22f, teamColor.g * 0.22f, teamColor.b * 0.22f, 1f), false);
            portrait.pivot = blue ? new Vector2(0f, 0.5f) : new Vector2(1f, 0.5f);
            Image portraitImage = portrait.GetComponent<Image>();
            Outline portraitOutline = portrait.gameObject.AddComponent<Outline>();
            portraitOutline.effectColor = teamColor;
            portraitOutline.effectDistance = new Vector2(1.5f, -1.5f);

            Text glyph = Label("Glyph", portrait, "?", 24, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(42f, 42f), teamColor);
            RectTransform levelBadge = Panel("LevelBadge", portrait, blue ? new Vector2(1f, 0f) : new Vector2(0f, 0f),
                blue ? new Vector2(1f, 0f) : new Vector2(0f, 0f), blue ? new Vector2(-1f, 1f) : new Vector2(1f, 1f),
                new Vector2(18f, 18f), new Color(0.008f, 0.014f, 0.026f, 1f), false);
            levelBadge.pivot = blue ? new Vector2(1f, 0f) : new Vector2(0f, 0f);
            Text level = Label("Level", levelBadge, "1", 11, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(17f, 17f), Color.white);

            Vector2 textAnchor = blue ? new Vector2(0f, 0.5f) : new Vector2(1f, 0.5f);
            float textX = blue ? 61f : -61f;
            TextAnchor alignment = blue ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight;
            Text name = Label("Name", row, "WAITING", 14, alignment, new Vector2(textX, 12f), new Vector2(164f, 22f), Color.white, textAnchor);
            Text role = Label("Role", row, "---", 10, alignment, new Vector2(textX, -7f), new Vector2(164f, 18f), teamColor, textAnchor);
            Text state = Label("State", row, "0 / 0 / 0", 10, alignment, new Vector2(textX, -22f), new Vector2(164f, 17f), new Color(0.72f, 0.80f, 0.88f), textAnchor);

            RectTransform hpBackground = Panel("HealthBackground", row, blue ? new Vector2(0f, 0f) : new Vector2(1f, 0f),
                blue ? new Vector2(0f, 0f) : new Vector2(1f, 0f), blue ? new Vector2(61f, 4f) : new Vector2(-61f, 4f),
                new Vector2(164f, 5f), new Color(0.015f, 0.020f, 0.030f, 1f), false);
            hpBackground.pivot = blue ? new Vector2(0f, 0f) : new Vector2(1f, 0f);
            Image healthFill = Fill("HealthFill", hpBackground, teamColor);

            output.Add(new RosterSlot
            {
                member = member,
                background = row.GetComponent<Image>(),
                portrait = portraitImage,
                healthFill = healthFill,
                glyph = glyph,
                name = name,
                role = role,
                level = level,
                state = state
            });
        }
    }

    private void RefreshSlots(List<RosterSlot> slots, MinionTeam team)
    {
        int teamKills = 0;
        foreach (RosterSlot slot in slots)
        {
            if (slot == null || slot.member == null)
                continue;

            AOGCharacterStats stats = slot.member.GetComponent<AOGCharacterStats>();
            AOGChampionProgression progression = slot.member.GetComponent<AOGChampionProgression>();
            AOGChampionMatchStats match = slot.member.GetComponent<AOGChampionMatchStats>();
            AOGActiveChampion champion = slot.member.GetComponent<AOGActiveChampion>();

            int kills = match != null ? match.kills : 0;
            int deaths = match != null ? match.deaths : 0;
            int assists = match != null ? match.assists : 0;
            teamKills += kills;

            bool dead = stats != null && stats.IsDead;
            Color teamColor = team == MinionTeam.Blue ? new Color(0.20f, 0.66f, 1f) : new Color(1f, 0.26f, 0.30f);
            Color accent = champion != null ? champion.accentColor : teamColor;
            string display = !string.IsNullOrEmpty(slot.member.displayName) ? slot.member.displayName : slot.member.championId.ToUpperInvariant();

            slot.name.text = display;
            slot.role.text = RoleLabel(slot.member.role) + (slot.member.isHumanPlayer ? "  PLAYER" : string.Empty);
            slot.level.text = (progression != null ? progression.level : 1).ToString();
            slot.glyph.text = display.Length > 0 ? display.Substring(0, 1) : "?";
            slot.glyph.color = dead ? new Color(0.35f, 0.35f, 0.38f) : accent;
            slot.portrait.color = dead
                ? new Color(0.06f, 0.06f, 0.07f, 1f)
                : new Color(accent.r * 0.24f, accent.g * 0.24f, accent.b * 0.24f, 1f);
            slot.healthFill.fillAmount = stats != null ? Mathf.Clamp01(stats.hp / Mathf.Max(1f, stats.maxHp)) : 1f;
            slot.healthFill.color = dead ? new Color(0.20f, 0.20f, 0.22f) : teamColor;
            slot.background.color = dead
                ? new Color(0.018f, 0.020f, 0.026f, 0.92f)
                : team == MinionTeam.Blue ? new Color(0.008f, 0.035f, 0.065f, 0.90f) : new Color(0.070f, 0.010f, 0.020f, 0.90f);
            slot.name.color = dead ? new Color(0.48f, 0.50f, 0.54f) : Color.white;

            if (dead)
            {
                int remaining = stats != null ? Mathf.CeilToInt(stats.RespawnRemaining) : 0;
                slot.state.text = "DOWN " + remaining + "s";
            }
            else
            {
                slot.state.text = kills + " / " + deaths + " / " + assists;
            }
        }

        UpdatePrimaryHeader(team, teamKills);
    }

    private void ResolvePrimaryHeader()
    {
        AOGCompetitiveMobaHUDRuntime hud = FindFirstObjectByType<AOGCompetitiveMobaHUDRuntime>();
        if (hud == null)
            return;

        primaryBlueScore = FindText(hud.transform, "BlueScore");
        primaryRedScore = FindText(hud.transform, "RedScore");
        primaryTimer = FindText(hud.transform, "Timer");
    }

    private void UpdatePrimaryHeader(MinionTeam team, int kills)
    {
        if (team == MinionTeam.Blue && primaryBlueScore != null)
            primaryBlueScore.text = "CELESTIAL  " + kills;
        if (team == MinionTeam.Red && primaryRedScore != null)
            primaryRedScore.text = kills + "  DOMINION";

        if (primaryTimer != null && AOGMatchDirector.Instance != null)
        {
            int total = Mathf.Max(0, Mathf.FloorToInt(AOGMatchDirector.Instance.MatchTime));
            primaryTimer.text = (total / 60).ToString("00") + ":" + (total % 60).ToString("00");
        }
    }

    private static string RoleLabel(AOGRole role)
    {
        switch (role)
        {
            case AOGRole.Top: return "TOP";
            case AOGRole.Jungle: return "JGL";
            case AOGRole.Mid: return "MID";
            case AOGRole.ADC: return "ADC";
            default: return "SUP";
        }
    }

    private RectTransform Panel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size, Color color, bool stretch)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = anchorMin == anchorMax ? anchorMin : new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        if (stretch)
        {
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
        else
        {
            rect.sizeDelta = size;
        }

        Image image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return rect;
    }

    private Image Fill(string name, RectTransform parent, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(1f, 1f);
        rect.offsetMax = new Vector2(-1f, -1f);
        Image image = go.GetComponent<Image>();
        image.type = Image.Type.Filled;
        image.fillMethod = Image.FillMethod.Horizontal;
        image.fillOrigin = (int)Image.OriginHorizontal.Left;
        image.fillAmount = 1f;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private Text Label(string name, Transform parent, string value, int size, TextAnchor alignment, Vector2 position, Vector2 dimensions, Color color, Vector2? anchor = null)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        Vector2 a = anchor ?? new Vector2(0.5f, 0.5f);
        rect.anchorMin = rect.anchorMax = a;
        rect.pivot = a;
        rect.anchoredPosition = position;
        rect.sizeDelta = dimensions;

        Text text = go.GetComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.fontStyle = FontStyle.Bold;
        text.alignment = alignment;
        text.color = color;
        text.text = value;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        return text;
    }

    private static Text FindText(Transform root, string name)
    {
        foreach (Text text in root.GetComponentsInChildren<Text>(true))
            if (text.name == name) return text;
        return null;
    }
}
