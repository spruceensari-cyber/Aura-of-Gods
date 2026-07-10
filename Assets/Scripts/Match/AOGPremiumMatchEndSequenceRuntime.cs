using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Presentation and final-stat layer for match completion.
/// AOGMatchDirector remains the authoritative match-state owner.
/// </summary>
[DefaultExecutionOrder(4200)]
public class AOGPremiumMatchEndSequenceRuntime : MonoBehaviour
{
    private AOGMatchState previousState = AOGMatchState.Loading;
    private float cachedMatchTime;
    private bool sequenceStarted;
    private Font font;
    private Canvas resultCanvas;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindFirstObjectByType<AOGPremiumMatchEndSequenceRuntime>() != null) return;
        GameObject host = new GameObject("AOG_Premium_Match_End_Sequence_Runtime");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGPremiumMatchEndSequenceRuntime>();
    }

    private void Awake()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    private void Update()
    {
        if (AOGMatchDirector.Instance == null) return;
        AOGMatchState state = AOGMatchDirector.Instance.State;
        if (state == AOGMatchState.Playing)
        {
            cachedMatchTime = AOGMatchDirector.Instance.MatchTime;
            sequenceStarted = false;
        }

        if (state != previousState)
        {
            previousState = state;
            if (!sequenceStarted && (state == AOGMatchState.BlueVictory || state == AOGMatchState.RedVictory))
            {
                sequenceStarted = true;
                StartCoroutine(PlayEndSequence(state == AOGMatchState.BlueVictory));
            }
        }
    }

    private IEnumerator PlayEndSequence(bool blueWon)
    {
        LockResidualGameplayControllers();
        AOGNexusCore destroyedNexus = blueWon ? AOGMatchDirector.Instance.RedNexus : AOGMatchDirector.Instance.BlueNexus;
        if (destroyedNexus != null)
            yield return StartCoroutine(NexusCollapse(destroyedNexus,blueWon));
        else
            yield return new WaitForSecondsRealtime(0.8f);

        SuppressLegacyResultCanvas();
        BuildPremiumResult(blueWon);
    }

    private void LockResidualGameplayControllers()
    {
        foreach (AOGUnifiedMobaInputDriver input in FindObjectsByType<AOGUnifiedMobaInputDriver>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            if (input != null) input.enabled = false;
        foreach (AOGEnemyTargetSelectionRuntime targeting in FindObjectsByType<AOGEnemyTargetSelectionRuntime>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            if (targeting != null) targeting.enabled = false;
        foreach (AOGBotChampionAI ai in FindObjectsByType<AOGBotChampionAI>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            if (ai != null) ai.enabled = false;
        foreach (AOGJungleChampionAIRuntime ai in FindObjectsByType<AOGJungleChampionAIRuntime>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            if (ai != null) ai.enabled = false;
    }

    private IEnumerator NexusCollapse(AOGNexusCore nexus,bool blueWon)
    {
        Vector3 center=nexus.transform.position+Vector3.up*2.4f;
        Color color=blueWon?new Color(1f,0.18f,0.28f):new Color(0.20f,0.62f,1f);

        Camera.main?.GetComponent<AOGMobaCameraController>()?.AddRandomImpulse(0.55f);
        for(int wave=0;wave<4;wave++)
        {
            GameObject ring=AOGAbilityVisuals.CreateRing("Nexus_Final_Shockwave_"+wave,nexus.transform.position+Vector3.up*0.08f,2.5f+wave*2.4f,color,0.18f-wave*0.02f);
            Destroy(ring,1.2f);
            SpawnShardBurst(center,color,8+wave*3,1.0f+wave*0.25f);
            yield return new WaitForSecondsRealtime(0.20f);
        }

        GameObject core=GameObject.CreatePrimitive(PrimitiveType.Sphere);
        core.name="Nexus_Final_Core_Burst";core.transform.position=center;core.transform.localScale=Vector3.one*0.5f;core.GetComponent<Renderer>().sharedMaterial=Emissive(Color.Lerp(color,Color.white,0.55f),5f);Collider c=core.GetComponent<Collider>();if(c!=null)Destroy(c);
        float elapsed=0f;
        while(elapsed<0.75f)
        {
            elapsed+=Time.unscaledDeltaTime;
            float t=Mathf.Clamp01(elapsed/0.75f);
            core.transform.localScale=Vector3.one*Mathf.Lerp(0.5f,5.5f,t);
            yield return null;
        }
        Destroy(core);
        Camera.main?.GetComponent<AOGMobaCameraController>()?.AddRandomImpulse(0.75f);
        yield return new WaitForSecondsRealtime(0.55f);
    }

    private void SpawnShardBurst(Vector3 center,Color color,int count,float scale)
    {
        Material energy=Emissive(color,3.8f);
        Material dark=Lit(Color.Lerp(new Color(0.025f,0.03f,0.045f),color,0.18f),0.28f,0.24f);
        for(int i=0;i<count;i++)
        {
            GameObject shard=GameObject.CreatePrimitive(PrimitiveType.Cube);
            shard.name="Nexus_Final_Shard";
            Vector3 dir=Random.onUnitSphere;dir.y=Mathf.Abs(dir.y)*0.75f+0.15f;
            shard.transform.position=center+dir*Random.Range(0.25f,1.1f);
            shard.transform.rotation=Random.rotation;
            shard.transform.localScale=new Vector3(0.08f,Random.Range(0.35f,0.85f)*scale,0.10f);
            shard.GetComponent<Renderer>().sharedMaterial=i%3==0?energy:dark;
            Collider c=shard.GetComponent<Collider>();if(c!=null)Destroy(c);
            AOGMatchEndShardMotionRuntime motion=shard.AddComponent<AOGMatchEndShardMotionRuntime>();
            motion.velocity=dir*Random.Range(3.5f,7.5f)*scale;
            motion.spin=Random.onUnitSphere*Random.Range(100f,260f);
            motion.life=Random.Range(0.8f,1.5f);
        }
    }

    private void SuppressLegacyResultCanvas()
    {
        foreach(Canvas c in FindObjectsByType<Canvas>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {
            if(c!=null&&c.gameObject.name=="MatchResultCanvas")Destroy(c.gameObject);
        }
    }

    private void BuildPremiumResult(bool blueWon)
    {
        if(resultCanvas!=null)Destroy(resultCanvas.gameObject);
        GameObject canvasObject=new GameObject("PremiumMatchResultCanvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));
        canvasObject.transform.SetParent(transform,false);
        resultCanvas=canvasObject.GetComponent<Canvas>();resultCanvas.renderMode=RenderMode.ScreenSpaceOverlay;resultCanvas.sortingOrder=6500;
        CanvasScaler scaler=canvasObject.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920f,1080f);scaler.matchWidthOrHeight=0.5f;

        RectTransform shade=Panel("Shade",canvasObject.transform,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero,new Color(0.002f,0.006f,0.014f,0.90f),true);
        Color accent=blueWon?new Color(0.28f,0.72f,1f):new Color(1f,0.28f,0.34f);
        string title=blueWon?"AETHER ASCENDANT":"COVENANT FALLEN";
        string subtitle=blueWon?"CELESTIAL COVENANT SECURED THE REALM":"THE FALLEN DOMINION CLAIMS THE REALM";

        Label("Title",shade,title,64,TextAnchor.MiddleCenter,new Vector2(0f,250f),new Vector2(1500f,96f),accent);
        Label("Subtitle",shade,subtitle,18,TextAnchor.MiddleCenter,new Vector2(0f,185f),new Vector2(1300f,42f),new Color(0.70f,0.80f,0.90f));
        Label("Time",shade,"FINAL TIME  "+FormatTime(cachedMatchTime),22,TextAnchor.MiddleCenter,new Vector2(0f,130f),new Vector2(600f,42f),Color.white);

        RectTransform statsPanel=Panel("FinalStats",shade,new Vector2(0.5f,0.5f),new Vector2(0.5f,0.5f),new Vector2(0f,-35f),new Vector2(880f,210f),new Color(0.010f,0.022f,0.038f,0.98f),false);
        Outline outline=statsPanel.gameObject.AddComponent<Outline>();outline.effectColor=accent;outline.effectDistance=new Vector2(2f,-2f);

        AOGActiveChampion player=AOGPlayerChampionAuthority.CurrentChampion;
        AOGChampionMatchStats match=player!=null?player.GetComponent<AOGChampionMatchStats>():null;
        AOGChampionProgression progression=player!=null?player.GetComponent<AOGChampionProgression>():null;
        AOGPlayerEconomy economy=player!=null?player.GetComponent<AOGPlayerEconomy>():null;

        string championName=player!=null?player.displayName:"HERO";
        Label("Hero",statsPanel,championName,28,TextAnchor.MiddleCenter,new Vector2(0f,66f),new Vector2(500f,44f),player!=null?player.accentColor:accent);
        Label("KDA",statsPanel,"K / D / A    "+(match!=null?match.kills+" / "+match.deaths+" / "+match.assists:"0 / 0 / 0"),20,TextAnchor.MiddleCenter,new Vector2(-250f,0f),new Vector2(340f,40f),Color.white);
        Label("Level",statsPanel,"LEVEL  "+(progression!=null?progression.level:1),20,TextAnchor.MiddleCenter,new Vector2(0f,0f),new Vector2(220f,40f),new Color(0.54f,0.76f,1f));
        Label("Gold",statsPanel,"◈  "+(economy!=null?economy.gold:0),20,TextAnchor.MiddleCenter,new Vector2(250f,0f),new Vector2(260f,40f),new Color(0.96f,0.76f,0.28f));
        Label("Build",statsPanel,"ITEMS  "+(economy!=null?economy.inventory.Count:0)+" / 6",15,TextAnchor.MiddleCenter,new Vector2(0f,-58f),new Vector2(500f,32f),new Color(0.66f,0.78f,0.88f));

        int blueKills=TeamKills(AOGRoleBasedTeamRuntime.BlueRoster);
        int redKills=TeamKills(AOGRoleBasedTeamRuntime.RedRoster);
        Label("TeamScore",shade,"CELESTIAL  "+blueKills+"     :     "+redKills+"  FALLEN",24,TextAnchor.MiddleCenter,new Vector2(0f,-205f),new Vector2(900f,48f),Color.white);
        Label("Footer",shade,"MATCH COMPLETE • RETURN TO LOBBY / REMATCH FLOW CAN BE BOUND HERE",12,TextAnchor.MiddleCenter,new Vector2(0f,-390f),new Vector2(1200f,30f),new Color(0.40f,0.54f,0.64f));
    }

    private static int TeamKills(System.Collections.Generic.List<AOGTeamMemberIdentity> roster)
    {
        int total=0;foreach(AOGTeamMemberIdentity member in roster){if(member==null)continue;AOGChampionMatchStats stats=member.GetComponent<AOGChampionMatchStats>();if(stats!=null)total+=stats.kills;}return total;
    }

    private static string FormatTime(float seconds)
    {
        int total=Mathf.Max(0,Mathf.FloorToInt(seconds));return (total/60).ToString("00")+":"+(total%60).ToString("00");
    }

    private RectTransform Panel(string name,Transform parent,Vector2 anchorMin,Vector2 anchorMax,Vector2 pos,Vector2 size,Color color,bool stretch)
    {
        GameObject go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);RectTransform rect=go.GetComponent<RectTransform>();rect.anchorMin=anchorMin;rect.anchorMax=anchorMax;rect.anchoredPosition=pos;if(stretch){rect.offsetMin=Vector2.zero;rect.offsetMax=Vector2.zero;}else rect.sizeDelta=size;Image image=go.GetComponent<Image>();image.color=color;image.raycastTarget=false;return rect;
    }

    private Text Label(string name,Transform parent,string value,int size,TextAnchor alignment,Vector2 pos,Vector2 dimensions,Color color)
    {
        GameObject go=new GameObject(name,typeof(RectTransform),typeof(Text));go.transform.SetParent(parent,false);RectTransform rect=go.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=new Vector2(0.5f,0.5f);rect.anchoredPosition=pos;rect.sizeDelta=dimensions;Text text=go.GetComponent<Text>();text.font=font;text.fontSize=size;text.fontStyle=FontStyle.Bold;text.alignment=alignment;text.color=color;text.text=value;text.raycastTarget=false;return text;
    }

    private static Material Lit(Color color,float smoothness,float metallic)
    {
        Shader shader=Shader.Find("Universal Render Pipeline/Lit");if(shader==null)shader=Shader.Find("Standard");Material mat=new Material(shader){color=color};if(mat.HasProperty("_BaseColor"))mat.SetColor("_BaseColor",color);if(mat.HasProperty("_Smoothness"))mat.SetFloat("_Smoothness",smoothness);if(mat.HasProperty("_Metallic"))mat.SetFloat("_Metallic",metallic);return mat;
    }

    private static Material Emissive(Color color,float strength)
    {
        Material mat=Lit(color,0.42f,0.12f);if(mat.HasProperty("_EmissionColor")){mat.EnableKeyword("_EMISSION");mat.SetColor("_EmissionColor",color*strength);}return mat;
    }
}

public class AOGMatchEndShardMotionRuntime : MonoBehaviour
{
    public Vector3 velocity;
    public Vector3 spin;
    public float life=1f;
    private float age;

    private void Update()
    {
        float dt=Time.unscaledDeltaTime;age+=dt;transform.position+=velocity*dt;velocity+=Vector3.down*5f*dt;transform.Rotate(spin*dt,Space.Self);float t=Mathf.Clamp01(age/Mathf.Max(0.01f,life));transform.localScale*=1f-dt*0.65f;if(t>=1f)Destroy(gameObject);
    }
}
