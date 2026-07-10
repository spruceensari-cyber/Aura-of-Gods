using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(1900)]
public class AOGTeamRosterHudRuntime : MonoBehaviour
{
    private static AOGTeamRosterHudRuntime instance;
    private readonly Dictionary<AOGTeamMemberIdentity, Text> labels = new Dictionary<AOGTeamMemberIdentity, Text>();
    private Canvas canvas;
    private Font font;

    public static void Ensure()
    {
        if (instance != null)
            return;
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
        Build();
    }

    private void Update()
    {
        foreach (KeyValuePair<AOGTeamMemberIdentity, Text> pair in labels)
        {
            if (pair.Key == null || pair.Value == null)
                continue;
            AOGCharacterStats stats = pair.Key.GetComponent<AOGCharacterStats>();
            bool dead = stats != null && stats.IsDead;
            pair.Value.color = dead ? new Color(0.35f,0.35f,0.38f) : TeamColor(pair.Key.team);
            pair.Value.text = RoleAbbreviation(pair.Key.role) + "  " + pair.Key.displayName + (dead ? "  ✕" : string.Empty);
        }
    }

    private void Build()
    {
        GameObject canvasObject = new GameObject("TeamRosterCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvasObject.transform.SetParent(transform, false);
        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 2750;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f,1080f);
        scaler.matchWidthOrHeight = 0.5f;

        BuildSide(AOGRoleBasedTeamRuntime.BlueRoster, new Vector2(0f,-18f), true);
        BuildSide(AOGRoleBasedTeamRuntime.RedRoster, new Vector2(1920f,-18f), false);
    }

    private void BuildSide(List<AOGTeamMemberIdentity> roster, Vector2 origin, bool blue)
    {
        for (int i = 0; i < roster.Count; i++)
        {
            AOGTeamMemberIdentity member = roster[i];
            if (member == null) continue;

            GameObject panel = new GameObject((blue ? "Blue" : "Red") + "_Roster_" + member.role, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(canvas.transform, false);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = blue ? new Vector2(0f,1f) : new Vector2(1f,1f);
            rect.pivot = blue ? new Vector2(0f,1f) : new Vector2(1f,1f);
            rect.anchoredPosition = new Vector2(blue ? 18f : -18f, -18f - i * 32f);
            rect.sizeDelta = new Vector2(250f,28f);
            panel.GetComponent<Image>().color = new Color(0.005f,0.012f,0.022f,0.84f);

            GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(panel.transform, false);
            RectTransform tr = textObject.GetComponent<RectTransform>();
            tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one; tr.offsetMin = new Vector2(8f,0f); tr.offsetMax = new Vector2(-8f,0f);
            Text text = textObject.GetComponent<Text>();
            text.font = font;
            text.fontSize = 15;
            text.fontStyle = FontStyle.Bold;
            text.alignment = blue ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight;
            text.color = TeamColor(member.team);
            text.raycastTarget = false;
            labels[member] = text;
        }
    }

    private static string RoleAbbreviation(AOGRole role)
    {
        return role switch
        {
            AOGRole.Top => "TOP",
            AOGRole.Jungle => "JGL",
            AOGRole.Mid => "MID",
            AOGRole.ADC => "ADC",
            _ => "SUP"
        };
    }

    private static Color TeamColor(MinionTeam team)
    {
        return team == MinionTeam.Blue ? new Color(0.28f,0.70f,1f) : new Color(1f,0.30f,0.32f);
    }
}
