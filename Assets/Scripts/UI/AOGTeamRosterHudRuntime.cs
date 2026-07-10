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
    private Text centerScore;

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
        int blueKills = 0;
        int redKills = 0;

        foreach (KeyValuePair<AOGTeamMemberIdentity,Text> pair in labels)
        {
            if (pair.Key == null || pair.Value == null)
                continue;

            AOGCharacterStats stats = pair.Key.GetComponent<AOGCharacterStats>();
            AOGChampionMatchStats matchStats = pair.Key.GetComponent<AOGChampionMatchStats>();
            bool dead = stats != null && stats.IsDead;
            int kills = matchStats != null ? matchStats.kills : 0;
            int deaths = matchStats != null ? matchStats.deaths : 0;
            int assists = matchStats != null ? matchStats.assists : 0;

            if (pair.Key.team == MinionTeam.Blue) blueKills += kills;
            else redKills += kills;

            string respawn = dead && stats != null ? "  " + Mathf.CeilToInt(stats.RespawnRemaining) + "s" : string.Empty;
            pair.Value.color = dead ? new Color(0.35f,0.35f,0.38f) : TeamColor(pair.Key.team);
            pair.Value.text = RoleAbbreviation(pair.Key.role) + "  " + pair.Key.displayName +
                              "   " + kills + "/" + deaths + "/" + assists +
                              (dead ? "  ✕" + respawn : string.Empty);
        }

        if (centerScore != null)
            centerScore.text = blueKills + "   ◈   " + redKills;
    }

    private void Build()
    {
        GameObject canvasObject = new GameObject("TeamRosterCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvasObject.transform.SetParent(transform,false);
        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 2750;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f,1080f);
        scaler.matchWidthOrHeight = 0.5f;

        BuildSide(AOGRoleBasedTeamRuntime.BlueRoster,true);
        BuildSide(AOGRoleBasedTeamRuntime.RedRoster,false);
        BuildCenterScore();
    }

    private void BuildSide(List<AOGTeamMemberIdentity> roster, bool blue)
    {
        for (int i=0;i<roster.Count;i++)
        {
            AOGTeamMemberIdentity member=roster[i];
            if (member==null) continue;

            GameObject panel=new GameObject((blue?"Blue":"Red")+"_Roster_"+member.role,typeof(RectTransform),typeof(Image));
            panel.transform.SetParent(canvas.transform,false);
            RectTransform rect=panel.GetComponent<RectTransform>();
            rect.anchorMin=rect.anchorMax=blue?new Vector2(0f,1f):new Vector2(1f,1f);
            rect.pivot=blue?new Vector2(0f,1f):new Vector2(1f,1f);
            rect.anchoredPosition=new Vector2(blue?18f:-18f,-18f-i*32f);
            rect.sizeDelta=new Vector2(315f,28f);
            panel.GetComponent<Image>().color=new Color(0.005f,0.012f,0.022f,0.84f);

            GameObject textObject=new GameObject("Text",typeof(RectTransform),typeof(Text));
            textObject.transform.SetParent(panel.transform,false);
            RectTransform tr=textObject.GetComponent<RectTransform>();
            tr.anchorMin=Vector2.zero; tr.anchorMax=Vector2.one; tr.offsetMin=new Vector2(8f,0f); tr.offsetMax=new Vector2(-8f,0f);
            Text text=textObject.GetComponent<Text>();
            text.font=font;
            text.fontSize=14;
            text.fontStyle=FontStyle.Bold;
            text.alignment=blue?TextAnchor.MiddleLeft:TextAnchor.MiddleRight;
            text.color=TeamColor(member.team);
            text.raycastTarget=false;
            labels[member]=text;
        }
    }

    private void BuildCenterScore()
    {
        GameObject scoreObject=new GameObject("RealTeamKillScore",typeof(RectTransform),typeof(Text));
        scoreObject.transform.SetParent(canvas.transform,false);
        RectTransform rect=scoreObject.GetComponent<RectTransform>();
        rect.anchorMin=rect.anchorMax=new Vector2(0.5f,1f);
        rect.pivot=new Vector2(0.5f,1f);
        rect.anchoredPosition=new Vector2(0f,-16f);
        rect.sizeDelta=new Vector2(260f,42f);
        centerScore=scoreObject.GetComponent<Text>();
        centerScore.font=font;
        centerScore.fontSize=25;
        centerScore.fontStyle=FontStyle.Bold;
        centerScore.alignment=TextAnchor.MiddleCenter;
        centerScore.color=new Color(0.92f,0.80f,0.48f);
        centerScore.raycastTarget=false;
    }

    private static string RoleAbbreviation(AOGRole role)
    {
        return role switch
        {
            AOGRole.Top=>"TOP",
            AOGRole.Jungle=>"JGL",
            AOGRole.Mid=>"MID",
            AOGRole.ADC=>"ADC",
            _=>"SUP"
        };
    }

    private static Color TeamColor(MinionTeam team)
    {
        return team==MinionTeam.Blue?new Color(0.28f,0.70f,1f):new Color(1f,0.30f,0.32f);
    }
}
