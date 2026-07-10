using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Compact progression toasts layered over the existing HUD. This is not a second gameplay HUD.
/// </summary>
public class AOGProgressionFeedbackRuntime : MonoBehaviour
{
    private Canvas canvas;
    private Text title;
    private Text subtitle;
    private CanvasGroup group;
    private AOGActiveChampion boundChampion;
    private AOGChampionProgression progression;
    private AOGPlayerEconomy economy;
    private int lastLevel = -1;
    private int lastSkillPoints = -1;
    private int lastInventoryCount = -1;
    private Coroutine toastRoutine;
    private float nextBind;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindFirstObjectByType<AOGProgressionFeedbackRuntime>() != null)
            return;
        GameObject host = new GameObject("AOG_Progression_Feedback_Runtime");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGProgressionFeedbackRuntime>();
    }

    private void Awake()
    {
        BuildUi();
    }

    private void Update()
    {
        if (Time.unscaledTime >= nextBind)
        {
            nextBind = Time.unscaledTime + 0.4f;
            AOGActiveChampion current = AOGPlayerChampionAuthority.CurrentChampion;
            if (current != boundChampion)
                Bind(current);
        }
    }

    private void Bind(AOGActiveChampion champion)
    {
        Unbind();
        boundChampion = champion;
        if (boundChampion == null)
            return;

        progression = boundChampion.GetComponent<AOGChampionProgression>();
        economy = boundChampion.GetComponent<AOGPlayerEconomy>();

        if (progression != null)
        {
            lastLevel = progression.level;
            lastSkillPoints = progression.unspentSkillPoints;
            progression.ProgressionChanged += OnProgressionChanged;
        }
        if (economy != null)
        {
            lastInventoryCount = economy.inventory.Count;
            economy.EconomyChanged += OnEconomyChanged;
        }
    }

    private void Unbind()
    {
        if (progression != null) progression.ProgressionChanged -= OnProgressionChanged;
        if (economy != null) economy.EconomyChanged -= OnEconomyChanged;
        progression = null;
        economy = null;
        boundChampion = null;
    }

    private void OnDestroy()
    {
        Unbind();
    }

    private void OnProgressionChanged()
    {
        if (progression == null)
            return;

        if (lastLevel >= 0 && progression.level > lastLevel)
        {
            ShowToast("ASCENSION LEVEL " + progression.level,"POWER INCREASED  •  SKILL POINT +1",new Color(0.96f,0.78f,0.28f));
            if (boundChampion != null)
            {
                GameObject ring = AOGAbilityVisuals.CreateRing("Advanced_Level_Up",boundChampion.transform.position+Vector3.up*0.08f,3.0f,new Color(0.96f,0.78f,0.28f),0.16f);
                Destroy(ring,0.8f);
            }
        }
        else if (lastSkillPoints >= 0 && progression.unspentSkillPoints < lastSkillPoints)
        {
            ShowToast("ABILITY EVOLVED","NEW RANK ACTIVE",new Color(0.30f,0.72f,1f));
        }

        lastLevel = progression.level;
        lastSkillPoints = progression.unspentSkillPoints;
    }

    private void OnEconomyChanged()
    {
        if (economy == null)
            return;

        if (lastInventoryCount >= 0 && economy.inventory.Count > lastInventoryCount)
        {
            AOGItemDefinition item = economy.inventory[economy.inventory.Count-1];
            string itemName = item != null ? item.displayName : "ITEM ACQUIRED";
            Color accent = item != null ? item.accent : new Color(0.42f,0.72f,1f);
            ShowToast("ASCENSION ACQUIRED",itemName,accent);
        }
        lastInventoryCount = economy.inventory.Count;
    }

    private void ShowToast(string headline,string detail,Color accent)
    {
        if (toastRoutine != null)
            StopCoroutine(toastRoutine);
        toastRoutine = StartCoroutine(ToastRoutine(headline,detail,accent));
    }

    private IEnumerator ToastRoutine(string headline,string detail,Color accent)
    {
        title.text = headline;
        subtitle.text = detail;
        title.color = accent;
        group.alpha = 0f;

        float elapsed=0f;
        while(elapsed<0.16f)
        {
            elapsed+=Time.unscaledDeltaTime;
            group.alpha=Mathf.Clamp01(elapsed/0.16f);
            yield return null;
        }

        yield return new WaitForSecondsRealtime(1.35f);

        elapsed=0f;
        while(elapsed<0.34f)
        {
            elapsed+=Time.unscaledDeltaTime;
            group.alpha=1f-Mathf.Clamp01(elapsed/0.34f);
            yield return null;
        }
        group.alpha=0f;
        toastRoutine=null;
    }

    private void BuildUi()
    {
        GameObject canvasObject=new GameObject("ProgressionFeedbackCanvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));
        canvasObject.transform.SetParent(transform,false);
        canvas=canvasObject.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=2750;
        CanvasScaler scaler=canvasObject.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920f,1080f);

        GameObject panel=new GameObject("ProgressionToast",typeof(RectTransform),typeof(Image),typeof(CanvasGroup));panel.transform.SetParent(canvasObject.transform,false);
        RectTransform rect=panel.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=new Vector2(0.5f,0.72f);rect.pivot=new Vector2(0.5f,0.5f);rect.anchoredPosition=Vector2.zero;rect.sizeDelta=new Vector2(470f,86f);
        panel.GetComponent<Image>().color=new Color(0.008f,0.018f,0.030f,0.88f);group=panel.GetComponent<CanvasGroup>();group.alpha=0f;group.blocksRaycasts=false;group.interactable=false;

        title=CreateText("Title",panel.transform,"ASCENSION LEVEL",24,new Vector2(0f,16f),new Vector2(440f,34f),new Color(0.96f,0.78f,0.28f));
        subtitle=CreateText("Subtitle",panel.transform,"POWER INCREASED",13,new Vector2(0f,-20f),new Vector2(440f,28f),new Color(0.72f,0.82f,0.90f));
    }

    private Text CreateText(string name,Transform parent,string value,int size,Vector2 pos,Vector2 dimensions,Color color)
    {
        GameObject go=new GameObject(name,typeof(RectTransform),typeof(Text));go.transform.SetParent(parent,false);RectTransform rect=go.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=new Vector2(0.5f,0.5f);rect.pivot=new Vector2(0.5f,0.5f);rect.anchoredPosition=pos;rect.sizeDelta=dimensions;Text text=go.GetComponent<Text>();text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=size;text.fontStyle=FontStyle.Bold;text.alignment=TextAnchor.MiddleCenter;text.color=color;text.text=value;text.raycastTarget=false;return text;
    }
}
