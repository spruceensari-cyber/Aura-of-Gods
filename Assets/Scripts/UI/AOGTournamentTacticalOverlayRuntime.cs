using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Hold-TAB tactical scoreboard for competitive desktop play.
/// This is a temporary overlay, not a second primary gameplay HUD.
/// </summary>
[DefaultExecutionOrder(3100)]
public class AOGTournamentTacticalOverlayRuntime : MonoBehaviour
{
    private Canvas canvas;
    private CanvasGroup group;
    private Text timerText;
    private Text blueSummary;
    private Text redSummary;
    private Text objectiveSummary;
    private readonly List<PlayerRow> blueRows = new List<PlayerRow>();
    private readonly List<PlayerRow> redRows = new List<PlayerRow>();
    private Font font;
    private float nextRefresh;
    private bool visible;

    private class PlayerRow
    {
        public RectTransform root;
        public Text name;
        public Text role;
        public Text level;
        public Text kda;
        public Text gold;
        public Text items;
        public Image lifeState;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindFirstObjectByType<AOGTournamentTacticalOverlayRuntime>() != null) return;
        GameObject host = new GameObject("AOG_Tournament_Tactical_Overlay_Runtime");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGTournamentTacticalOverlayRuntime>();
    }

    private void Awake()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        BuildUi();
        SetVisible(false);
    }

    private void Update()
    {
        bool shouldShow = AOGInputBridge.KeyIsPressed(KeyCode.Tab);
        if (shouldShow != visible) SetVisible(shouldShow);
        if (!visible) return;

        if (Time.unscaledTime >= nextRefresh)
        {
            nextRefresh = Time.unscaledTime + 0.20f;
            Refresh();
        }
    }

    private void BuildUi()
    {
        GameObject canvasObject = new GameObject("TournamentTacticalOverlayCanvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster),typeof(CanvasGroup));
        canvasObject.transform.SetParent(transform,false);
        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5500;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f,1080f);
        scaler.matchWidthOrHeight = 0.5f;
        group = canvasObject.GetComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;

        RectTransform dim = Panel("Dim",canvasObject.transform,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero,new Color(0.002f,0.006f,0.014f,0.72f),true);
        RectTransform board = Panel("TacticalBoard",dim,new Vector2(0.5f,0.5f),new Vector2(0.5f,0.5f),Vector2.zero,new Vector2(1560f,820f),new Color(0.008f,0.018f,0.030f,0.985f),false);
        Outline boardOutline = board.gameObject.AddComponent<Outline>();
        boardOutline.effectColor = new Color(0.20f,0.42f,0.62f,0.80f);
        boardOutline.effectDistance = new Vector2(2f,-2f);

        Label("Title",board,"AETHER WAR TABLE",30,TextAnchor.MiddleCenter,new Vector2(0f,372f),new Vector2(600f,48f),new Color(0.86f,0.92f,1f));
        Label("Hint",board,"HOLD TAB • LIVE TACTICAL STATE",12,TextAnchor.MiddleCenter,new Vector2(0f,338f),new Vector2(600f,28f),new Color(0.48f,0.62f,0.72f));
        timerText = Label("MatchTimer",board,"00:00",22,TextAnchor.MiddleCenter,new Vector2(0f,292f),new Vector2(180f,34f),Color.white);

        RectTransform bluePanel = Panel("BlueTeam",board,new Vector2(0f,0.5f),new Vector2(0f,0.5f),new Vector2(36f,-12f),new Vector2(710f,620f),new Color(0.012f,0.038f,0.070f,0.96f),false);
        bluePanel.pivot = new Vector2(0f,0.5f);
        RectTransform redPanel = Panel("RedTeam",board,new Vector2(1f,0.5f),new Vector2(1f,0.5f),new Vector2(-36f,-12f),new Vector2(710f,620f),new Color(0.070f,0.014f,0.024f,0.96f),false);
        redPanel.pivot = new Vector2(1f,0.5f);

        Label("BlueTitle",bluePanel,"CELESTIAL COVENANT",22,TextAnchor.MiddleLeft,new Vector2(22f,276f),new Vector2(380f,38f),new Color(0.34f,0.72f,1f),new Vector2(0f,0.5f));
        blueSummary = Label("BlueSummary",bluePanel,"0 KILLS   ◈ 0",15,TextAnchor.MiddleRight,new Vector2(-22f,276f),new Vector2(260f,34f),new Color(0.74f,0.86f,0.96f),new Vector2(1f,0.5f));
        Label("RedTitle",redPanel,"FALLEN DOMINION",22,TextAnchor.MiddleLeft,new Vector2(22f,276f),new Vector2(380f,38f),new Color(1f,0.34f,0.38f),new Vector2(0f,0.5f));
        redSummary = Label("RedSummary",redPanel,"0 KILLS   ◈ 0",15,TextAnchor.MiddleRight,new Vector2(-22f,276f),new Vector2(260f,34f),new Color(0.96f,0.78f,0.82f),new Vector2(1f,0.5f));

        BuildHeader(bluePanel);
        BuildHeader(redPanel);
        for (int i=0;i<5;i++)
        {
            blueRows.Add(BuildPlayerRow(bluePanel,i,new Color(0.08f,0.20f,0.34f,0.94f),new Color(0.22f,0.62f,1f)));
            redRows.Add(BuildPlayerRow(redPanel,i,new Color(0.30f,0.06f,0.10f,0.94f),new Color(1f,0.22f,0.28f)));
        }

        RectTransform objectiveBar = Panel("ObjectiveWarState",board,new Vector2(0.5f,0f),new Vector2(0.5f,0f),new Vector2(0f,26f),new Vector2(1460f,66f),new Color(0.016f,0.028f,0.044f,0.98f),false);
        objectiveSummary = Label("ObjectiveSummary",objectiveBar,"DRAGON • MEDUSA • VOID TITAN • LANE SEALS",14,TextAnchor.MiddleCenter,Vector2.zero,new Vector2(1420f,42f),new Color(0.68f,0.80f,0.90f));
    }

    private void BuildHeader(RectTransform panel)
    {
        float y = 230f;
        Label("H_Name",panel,"CHAMPION",11,TextAnchor.MiddleLeft,new Vector2(60f,y),new Vector2(190f,24f),new Color(0.48f,0.62f,0.72f),new Vector2(0f,0.5f));
        Label("H_Role",panel,"ROLE",11,TextAnchor.MiddleCenter,new Vector2(280f,y),new Vector2(90f,24f),new Color(0.48f,0.62f,0.72f));
        Label("H_Lvl",panel,"LVL",11,TextAnchor.MiddleCenter,new Vector2(365f,y),new Vector2(60f,24f),new Color(0.48f,0.62f,0.72f));
        Label("H_KDA",panel,"K / D / A",11,TextAnchor.MiddleCenter,new Vector2(455f,y),new Vector2(110f,24f),new Color(0.48f,0.62f,0.72f));
        Label("H_Gold",panel,"GOLD",11,TextAnchor.MiddleCenter,new Vector2(560f,y),new Vector2(90f,24f),new Color(0.48f,0.62f,0.72f));
        Label("H_Items",panel,"BUILD",11,TextAnchor.MiddleCenter,new Vector2(650f,y),new Vector2(80f,24f),new Color(0.48f,0.62f,0.72f));
    }

    private PlayerRow BuildPlayerRow(RectTransform panel,int index,Color background,Color accent)
    {
        float y = 180f - index*92f;
        RectTransform row = Panel("PlayerRow_"+index,panel,new Vector2(0.5f,0.5f),new Vector2(0.5f,0.5f),new Vector2(0f,y),new Vector2(666f,78f),background,false);
        Image life = Panel("LifeState",row,new Vector2(0f,0.5f),new Vector2(0f,0.5f),new Vector2(7f,0f),new Vector2(7f,58f),accent,false).GetComponent<Image>();
        PlayerRow result = new PlayerRow { root=row, lifeState=life };
        result.name = Label("Name",row,"—",15,TextAnchor.MiddleLeft,new Vector2(18f,12f),new Vector2(220f,28f),Color.white,new Vector2(0f,0.5f));
        result.role = Label("Role",row,"—",12,TextAnchor.MiddleCenter,new Vector2(-65f,0f),new Vector2(90f,26f),new Color(0.64f,0.76f,0.86f));
        result.level = Label("Level",row,"1",14,TextAnchor.MiddleCenter,new Vector2(20f,0f),new Vector2(60f,26f),Color.white);
        result.kda = Label("KDA",row,"0 / 0 / 0",14,TextAnchor.MiddleCenter,new Vector2(110f,0f),new Vector2(120f,26f),Color.white);
        result.gold = Label("Gold",row,"◈ 0",13,TextAnchor.MiddleCenter,new Vector2(215f,0f),new Vector2(90f,26f),new Color(0.94f,0.74f,0.28f));
        result.items = Label("Items",row,"—",10,TextAnchor.MiddleRight,new Vector2(320f,0f),new Vector2(120f,54f),new Color(0.72f,0.82f,0.90f),new Vector2(1f,0.5f));
        return result;
    }

    private void Refresh()
    {
        if (AOGMatchDirector.Instance != null && timerText != null)
        {
            int total = Mathf.Max(0,Mathf.FloorToInt(AOGMatchDirector.Instance.MatchTime));
            timerText.text = (total/60).ToString("00") + ":" + (total%60).ToString("00");
        }

        RefreshTeam(AOGRoleBasedTeamRuntime.BlueRoster,blueRows,blueSummary,MinionTeam.Blue);
        RefreshTeam(AOGRoleBasedTeamRuntime.RedRoster,redRows,redSummary,MinionTeam.Red);
        RefreshObjectives();
    }

    private void RefreshTeam(List<AOGTeamMemberIdentity> roster,List<PlayerRow> rows,Text summary,MinionTeam team)
    {
        int teamKills=0;
        int teamGold=0;
        for(int i=0;i<rows.Count;i++)
        {
            AOGTeamMemberIdentity member = i < roster.Count ? roster[i] : null;
            RefreshRow(rows[i],member);
            if(member==null)continue;
            AOGChampionMatchStats match=member.GetComponent<AOGChampionMatchStats>();
            AOGPlayerEconomy economy=member.GetComponent<AOGPlayerEconomy>();
            if(match!=null)teamKills+=match.kills;
            if(economy!=null)teamGold+=economy.gold;
        }
        if(summary!=null)summary.text=teamKills+" KILLS   ◈ "+teamGold;
    }

    private void RefreshRow(PlayerRow row,AOGTeamMemberIdentity member)
    {
        if(row==null)return;
        row.root.gameObject.SetActive(member!=null);
        if(member==null)return;

        AOGCharacterStats stats=member.GetComponent<AOGCharacterStats>();
        AOGChampionProgression progression=member.GetComponent<AOGChampionProgression>();
        AOGChampionMatchStats match=member.GetComponent<AOGChampionMatchStats>();
        AOGPlayerEconomy economy=member.GetComponent<AOGPlayerEconomy>();

        row.name.text=(member.isHumanPlayer?"◆ ":"")+(!string.IsNullOrEmpty(member.displayName)?member.displayName:member.championId.ToUpperInvariant());
        row.role.text=member.role.ToString().ToUpperInvariant();
        row.level.text=(progression!=null?progression.level:1).ToString();
        row.kda.text=match!=null?match.kills+" / "+match.deaths+" / "+match.assists:"0 / 0 / 0";
        row.gold.text="◈ "+(economy!=null?economy.gold:0);
        row.items.text=BuildItemSummary(economy);

        bool dead=stats!=null&&stats.IsDead;
        row.lifeState.color=dead?new Color(0.22f,0.22f,0.24f):member.team==MinionTeam.Blue?new Color(0.22f,0.62f,1f):new Color(1f,0.22f,0.28f);
        row.name.color=dead?new Color(0.46f,0.48f,0.52f):Color.white;
    }

    private static string BuildItemSummary(AOGPlayerEconomy economy)
    {
        if(economy==null||economy.inventory==null||economy.inventory.Count==0)return "—";
        List<string> names=new List<string>();
        int start=Mathf.Max(0,economy.inventory.Count-3);
        for(int i=start;i<economy.inventory.Count;i++)
        {
            AOGItemDefinition item=economy.inventory[i];
            if(item==null)continue;
            string label=!string.IsNullOrEmpty(item.displayName)?item.displayName:item.id;
            if(label.Length>11)label=label.Substring(0,11);
            names.Add(label);
        }
        return string.Join("\n",names);
    }

    private void RefreshObjectives()
    {
        if(objectiveSummary==null)return;
        string dragon="DRAGON: UNSEEN";
        string medusa="MEDUSA: UNSEEN";
        string titan="TITAN: LOCKED";
        foreach(AOGNeutralBossAI boss in AOGWorldRegistry.Bosses)
        {
            if(boss==null)continue;
            if(boss.GetComponent<AOGVoidTitanMarker>()!=null)titan="TITAN: "+(boss.IsDead?"DOWN":"ACTIVE");
            else if(boss.bossType==AOGNeutralBossType.Dragon)dragon="DRAGON: "+(boss.IsDead?"DOWN":"ACTIVE");
            else medusa="MEDUSA: "+(boss.IsDead?"DOWN":"ACTIVE");
        }

        int blueSeals=0;int redSeals=0;
        foreach(AOGStrategicLaneSeal seal in AOGWorldRegistry.Seals)
        {
            if(seal==null||seal.State!=AOGSealState.Active)continue;
            if(seal.team==MinionTeam.Blue)blueSeals++;else redSeals++;
        }
        objectiveSummary.text=dragon+"    •    "+medusa+"    •    "+titan+"    •    SEALS "+blueSeals+" : "+redSeals;
    }

    private void SetVisible(bool value)
    {
        visible=value;
        if(group==null)return;
        group.alpha=value?1f:0f;
        group.blocksRaycasts=false;
        group.interactable=false;
    }

    private RectTransform Panel(string name,Transform parent,Vector2 anchorMin,Vector2 anchorMax,Vector2 position,Vector2 size,Color color,bool stretch)
    {
        GameObject go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);
        RectTransform rect=go.GetComponent<RectTransform>();rect.anchorMin=anchorMin;rect.anchorMax=anchorMax;rect.anchoredPosition=position;
        if(stretch){rect.offsetMin=Vector2.zero;rect.offsetMax=Vector2.zero;}else rect.sizeDelta=size;
        Image image=go.GetComponent<Image>();image.color=color;image.raycastTarget=false;
        return rect;
    }

    private Text Label(string name,Transform parent,string value,int size,TextAnchor alignment,Vector2 position,Vector2 dimensions,Color color,Vector2? anchor=null)
    {
        GameObject go=new GameObject(name,typeof(RectTransform),typeof(Text));go.transform.SetParent(parent,false);
        RectTransform rect=go.GetComponent<RectTransform>();Vector2 a=anchor??new Vector2(0.5f,0.5f);rect.anchorMin=rect.anchorMax=a;rect.pivot=a;rect.anchoredPosition=position;rect.sizeDelta=dimensions;
        Text text=go.GetComponent<Text>();text.font=font;text.fontSize=size;text.fontStyle=FontStyle.Bold;text.alignment=alignment;text.color=color;text.text=value;text.raycastTarget=false;
        return text;
    }
}
