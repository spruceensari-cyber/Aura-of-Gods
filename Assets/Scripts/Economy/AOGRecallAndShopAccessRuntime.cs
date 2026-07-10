using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Player recall channel: B begins a timed return to allied base. Movement, damage or death cancels.
/// Designed around the existing player authority so AI and preview champions cannot consume player input.
/// </summary>
public class AOGRecallRuntime : MonoBehaviour
{
    public float channelDuration = 7.5f;
    public float movementCancelDistance = 0.18f;

    private AOGActiveChampion champion;
    private AOGCharacterStats stats;
    private bool channeling;
    private float channelStart;
    private float startHp;
    private Vector3 startPosition;
    private GameObject recallRing;
    private Canvas canvas;
    private Image progressFill;
    private Text statusText;

    public bool IsChanneling => channeling;
    public float Progress01 => channeling ? Mathf.Clamp01((Time.time-channelStart)/Mathf.Max(0.1f,channelDuration)) : 0f;

    private void Awake()
    {
        champion=GetComponent<AOGActiveChampion>();
        stats=GetComponent<AOGCharacterStats>();
        BuildUi();
        SetUiVisible(false);
    }

    private void OnDisable()
    {
        CancelRecall(false);
    }

    private void Update()
    {
        if (champion==null || stats==null || AOGPlayerChampionAuthority.CurrentChampion!=champion || !champion.IsActiveChampion)
            return;

        if (AOGInputBridge.KeyPressedThisFrame(KeyCode.B))
        {
            if (channeling) CancelRecall(true);
            else BeginRecall();
        }

        if (!channeling)
            return;

        if (stats.IsDead || stats.hp < startHp-0.01f || FlatDistance(transform.position,startPosition)>movementCancelDistance)
        {
            CancelRecall(true);
            return;
        }

        float progress=Progress01;
        if (progressFill!=null) progressFill.fillAmount=progress;
        if (statusText!=null) statusText.text="RECALLING  "+Mathf.CeilToInt(Mathf.Max(0f,channelDuration-(Time.time-channelStart)))+"s";

        if (progress>=1f)
            CompleteRecall();
    }

    public void BeginRecall()
    {
        if (channeling || stats==null || stats.IsDead)
            return;
        if (AOGMatchDirector.Instance!=null && AOGMatchDirector.Instance.State!=AOGMatchState.Playing)
            return;

        channeling=true;
        channelStart=Time.time;
        startHp=stats.hp;
        startPosition=transform.position;
        recallRing=AOGAbilityVisuals.CreateRing("AOG_Recall_Channel",transform.position+Vector3.up*0.05f,2.25f,champion!=null?champion.accentColor:new Color(0.2f,0.7f,1f),0.10f);
        SetUiVisible(true);
        if (progressFill!=null) progressFill.fillAmount=0f;
        if (statusText!=null) statusText.text="RECALLING";
    }

    public void CancelRecall(bool showFeedback)
    {
        if (!channeling && recallRing==null)
            return;
        channeling=false;
        if (recallRing!=null)
        {
            Destroy(recallRing);
            recallRing=null;
        }
        SetUiVisible(false);
        if (showFeedback)
        {
            GameObject ring=AOGAbilityVisuals.CreateRing("Recall_Cancelled",transform.position+Vector3.up*0.04f,1.2f,new Color(1f,0.3f,0.24f),0.05f);
            Destroy(ring,0.25f);
        }
    }

    private void CompleteRecall()
    {
        channeling=false;
        if (recallRing!=null)
        {
            Destroy(recallRing);
            recallRing=null;
        }

        Transform spawn=AOGBaseAccessUtility.FindTeamBase(stats.team);
        if (spawn!=null)
        {
            Vector3 offset=stats.team==MinionTeam.Blue?new Vector3(1.5f,0.25f,1.5f):new Vector3(-1.5f,0.25f,-1.5f);
            transform.position=spawn.position+offset;
            transform.rotation=spawn.rotation;
        }

        stats.hp=stats.maxHp;
        SetUiVisible(false);
        GameObject complete=AOGAbilityVisuals.CreateRing("Recall_Complete",transform.position+Vector3.up*0.05f,2.4f,new Color(0.25f,0.85f,1f),0.10f);
        Destroy(complete,0.55f);
    }

    private void BuildUi()
    {
        GameObject canvasObject=new GameObject("RecallCanvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));
        canvasObject.transform.SetParent(transform,false);
        canvas=canvasObject.GetComponent<Canvas>();
        canvas.renderMode=RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder=2900;
        CanvasScaler scaler=canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution=new Vector2(1920f,1080f);

        GameObject panel=new GameObject("RecallProgress",typeof(RectTransform),typeof(Image));
        panel.transform.SetParent(canvasObject.transform,false);
        RectTransform pr=panel.GetComponent<RectTransform>();
        pr.anchorMin=pr.anchorMax=new Vector2(0.5f,0f);
        pr.pivot=new Vector2(0.5f,0f);
        pr.anchoredPosition=new Vector2(0f,250f);
        pr.sizeDelta=new Vector2(360f,44f);
        panel.GetComponent<Image>().color=new Color(0.01f,0.02f,0.04f,0.94f);

        GameObject fill=new GameObject("Fill",typeof(RectTransform),typeof(Image));
        fill.transform.SetParent(panel.transform,false);
        RectTransform fr=fill.GetComponent<RectTransform>();
        fr.anchorMin=Vector2.zero; fr.anchorMax=Vector2.one; fr.offsetMin=new Vector2(4f,4f); fr.offsetMax=new Vector2(-4f,-4f);
        progressFill=fill.GetComponent<Image>();
        progressFill.color=new Color(0.18f,0.66f,1f,0.92f);
        progressFill.type=Image.Type.Filled;
        progressFill.fillMethod=Image.FillMethod.Horizontal;
        progressFill.fillOrigin=0;

        GameObject label=new GameObject("Status",typeof(RectTransform),typeof(Text));
        label.transform.SetParent(panel.transform,false);
        RectTransform lr=label.GetComponent<RectTransform>();
        lr.anchorMin=Vector2.zero; lr.anchorMax=Vector2.one; lr.offsetMin=Vector2.zero; lr.offsetMax=Vector2.zero;
        statusText=label.GetComponent<Text>();
        statusText.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        statusText.fontSize=16;
        statusText.fontStyle=FontStyle.Bold;
        statusText.alignment=TextAnchor.MiddleCenter;
        statusText.color=Color.white;
        statusText.raycastTarget=false;
    }

    private void SetUiVisible(bool visible)
    {
        if (canvas!=null) canvas.gameObject.SetActive(visible);
    }

    private static float FlatDistance(Vector3 a,Vector3 b)
    {
        a.y=0f;b.y=0f;return Vector3.Distance(a,b);
    }
}

public static class AOGBaseAccessUtility
{
    public static float shopRadius=13f;

    public static bool IsShopAvailable(AOGPlayerEconomy economy)
    {
        if (economy==null) return false;
        AOGCharacterStats stats=economy.GetComponent<AOGCharacterStats>();
        if (stats==null) return false;
        if (stats.IsDead) return true;
        Transform spawn=FindTeamBase(stats.team);
        if (spawn==null) return false;
        Vector3 a=economy.transform.position; a.y=0f;
        Vector3 b=spawn.position; b.y=0f;
        return Vector3.Distance(a,b)<=shopRadius;
    }

    public static Transform FindTeamBase(MinionTeam team)
    {
        string[] names=team==MinionTeam.Blue
            ? new[]{"BlueSpawn","Blue_Spawn","BlueBaseSpawn","BluePlayerSpawn"}
            : new[]{"RedSpawn","Red_Spawn","RedBaseSpawn","RedPlayerSpawn"};

        foreach (GameObject obj in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {
            if (obj==null) continue;
            foreach (string candidate in names)
                if (string.Equals(obj.name,candidate,System.StringComparison.OrdinalIgnoreCase))
                    return obj.transform;
        }
        return null;
    }
}

/// <summary>
/// Keeps the existing Aether Market browseable anywhere while disabling purchase buttons
/// outside the allied base radius. This extends the current shop without replacing it.
/// </summary>
public class AOGShopAccessGuardRuntime : MonoBehaviour
{
    private float nextRefresh;
    private bool lastState;
    private bool initialized;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindFirstObjectByType<AOGShopAccessGuardRuntime>()!=null)
            return;
        GameObject host=new GameObject("AOG_Shop_Access_Guard");
        Object.DontDestroyOnLoad(host);
        host.AddComponent<AOGShopAccessGuardRuntime>();
    }

    private void Update()
    {
        if (Time.unscaledTime<nextRefresh) return;
        nextRefresh=Time.unscaledTime+0.25f;

        AOGActiveChampion player=AOGPlayerChampionAuthority.CurrentChampion;
        AOGPlayerEconomy economy=player!=null?player.GetComponent<AOGPlayerEconomy>():null;
        bool available=AOGBaseAccessUtility.IsShopAvailable(economy);
        if (initialized && available==lastState) return;
        initialized=true;
        lastState=available;
        ApplyShopState(available);
    }

    private static void ApplyShopState(bool available)
    {
        AOGShopRuntime shop=AOGShopRuntime.Instance;
        if (shop==null) return;

        foreach (Button button in shop.GetComponentsInChildren<Button>(true))
        {
            if (button!=null && button.gameObject.name=="Buy")
                button.interactable=available;
        }

        foreach (Text text in shop.GetComponentsInChildren<Text>(true))
        {
            if (text!=null && text.gameObject.name=="Status")
            {
                text.text=available?"SHOP AVAILABLE  •  P: CLOSE MARKET":"SHOP OUT OF RANGE  •  RECALL WITH B";
                text.color=available?new Color(0.42f,0.86f,0.56f):new Color(1f,0.40f,0.32f);
            }
        }
    }
}

public class AOGRecallBootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindFirstObjectByType<AOGRecallBootstrap>()!=null) return;
        GameObject host=new GameObject("AOG_Recall_Bootstrap");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGRecallBootstrap>();
    }

    private float nextScan;
    private void Update()
    {
        if (Time.unscaledTime<nextScan) return;
        nextScan=Time.unscaledTime+0.5f;
        AOGActiveChampion player=AOGPlayerChampionAuthority.CurrentChampion;
        if (player!=null && player.GetComponent<AOGRecallRuntime>()==null)
            player.gameObject.AddComponent<AOGRecallRuntime>();
    }
}
