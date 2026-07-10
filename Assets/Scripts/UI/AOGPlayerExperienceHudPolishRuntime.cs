using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Extends the existing competitive HUD hierarchy with live XP, KDA, gold delta,
/// item acquisition pulses and objective/buff state animation. No additional primary HUD canvas.
/// </summary>
[DefaultExecutionOrder(2250)]
public class AOGPlayerExperienceHudPolishRuntime : MonoBehaviour
{
    private AOGCompetitiveMobaHUDRuntime hud;
    private RectTransform hudCanvas;
    private RectTransform championHud;
    private RectTransform xpRoot;
    private Image xpFill;
    private Text xpText;
    private Text skillPointText;
    private Text kdaText;
    private Text goldDeltaText;
    private CanvasGroup goldDeltaGroup;
    private RectTransform goldBar;

    private readonly List<RectTransform> itemSlots = new List<RectTransform>();
    private readonly Dictionary<Text,string> objectiveStates = new Dictionary<Text,string>();
    private readonly List<RectTransform> buffSlots = new List<RectTransform>();

    private AOGActiveChampion player;
    private AOGChampionProgression progression;
    private AOGPlayerEconomy economy;
    private AOGChampionMatchStats matchStats;

    private int lastGold;
    private int lastInventoryCount;
    private int lastLevel;
    private int lastExperience;
    private int lastSkillPoints;
    private float displayedXp;
    private float targetXp;
    private float nextBind;
    private bool built;
    private Coroutine goldDeltaRoutine;
    private Font font;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindFirstObjectByType<AOGPlayerExperienceHudPolishRuntime>() != null) return;
        GameObject host = new GameObject("AOG_Player_Experience_HUD_Polish_Runtime");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGPlayerExperienceHudPolishRuntime>();
    }

    private void Awake()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    private void OnEnable()
    {
        AOGCombatEvents.ChampionDeath += OnChampionDeath;
    }

    private void OnDisable()
    {
        AOGCombatEvents.ChampionDeath -= OnChampionDeath;
        UnbindPlayer();
    }

    private void Update()
    {
        if (!built)
        {
            TryBuild();
            return;
        }

        if (Time.unscaledTime >= nextBind)
        {
            nextBind = Time.unscaledTime + 0.30f;
            BindPlayerIfChanged();
            RefreshLiveText();
            AnimateExistingObjectiveRibbon();
            AnimateBuffUrgency();
        }

        displayedXp = Mathf.MoveTowards(displayedXp,targetXp,Time.unscaledDeltaTime*1.65f);
        if (xpFill != null) xpFill.fillAmount = displayedXp;
    }

    private void TryBuild()
    {
        hud = FindFirstObjectByType<AOGCompetitiveMobaHUDRuntime>();
        if (hud == null) return;
        Canvas canvas = hud.GetComponentInChildren<Canvas>(true);
        if (canvas == null) return;

        hudCanvas = canvas.GetComponent<RectTransform>();
        championHud = FindRect(hud.transform,"ChampionHUD");
        if (hudCanvas == null || championHud == null) return;

        BuildXpStrip();
        BuildKdaReadout();
        BuildGoldDeltaReadout();
        CacheItemSlots();
        CacheExistingObjectiveRibbon();
        CacheExistingBuffStrip();
        built = true;
        BindPlayerIfChanged();
    }

    private void BuildXpStrip()
    {
        xpRoot = Panel("IntegratedXPStrip",championHud,new Vector2(0.5f,0f),new Vector2(0.5f,0f),new Vector2(30f,7f),new Vector2(470f,12f),new Color(0.014f,0.026f,0.045f,0.96f));
        Outline outline = xpRoot.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.18f,0.42f,0.72f,0.85f);
        outline.effectDistance = new Vector2(1f,-1f);

        GameObject fillObject = new GameObject("XP_FILL",typeof(RectTransform),typeof(Image));
        fillObject.transform.SetParent(xpRoot,false);
        RectTransform fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(2f,2f);
        fillRect.offsetMax = new Vector2(-2f,-2f);
        xpFill = fillObject.GetComponent<Image>();
        xpFill.color = new Color(0.22f,0.58f,1f,1f);
        xpFill.type = Image.Type.Filled;
        xpFill.fillMethod = Image.FillMethod.Horizontal;
        xpFill.fillOrigin = 0;
        xpFill.fillAmount = 0f;
        xpFill.raycastTarget = false;

        xpText = Label("XP_TEXT",xpRoot,"XP  0 / 280",10,TextAnchor.MiddleCenter,Vector2.zero,new Vector2(450f,16f),Color.white);
        skillPointText = Label("SkillPointPulse",championHud,"",12,TextAnchor.MiddleCenter,new Vector2(30f,27f),new Vector2(280f,24f),new Color(0.96f,0.78f,0.28f));
    }

    private void BuildKdaReadout()
    {
        RectTransform combatStats = FindRect(hud.transform,"CombatStats");
        if (combatStats == null) return;
        kdaText = Label("PlayerKDA",combatStats,"K / D / A   0 / 0 / 0",12,TextAnchor.MiddleCenter,new Vector2(0f,-31f),new Vector2(210f,22f),new Color(0.72f,0.84f,0.94f));
    }

    private void BuildGoldDeltaReadout()
    {
        goldBar = FindRect(hud.transform,"GoldBar");
        if (goldBar == null) return;
        GameObject go = new GameObject("GoldDelta",typeof(RectTransform),typeof(Text),typeof(CanvasGroup));
        go.transform.SetParent(goldBar,false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f,1f);
        rect.pivot = new Vector2(0.5f,0f);
        rect.anchoredPosition = new Vector2(0f,4f);
        rect.sizeDelta = new Vector2(190f,24f);
        goldDeltaText = go.GetComponent<Text>();
        goldDeltaText.font = font;
        goldDeltaText.fontSize = 13;
        goldDeltaText.fontStyle = FontStyle.Bold;
        goldDeltaText.alignment = TextAnchor.MiddleCenter;
        goldDeltaText.raycastTarget = false;
        goldDeltaGroup = go.GetComponent<CanvasGroup>();
        goldDeltaGroup.alpha = 0f;
    }

    private void CacheItemSlots()
    {
        itemSlots.Clear();
        RectTransform items = FindRect(hud.transform,"Items");
        if (items == null) return;
        for (int i=0;i<6;i++)
        {
            RectTransform slot = FindRect(items,"Item_"+i);
            if (slot != null) itemSlots.Add(slot);
        }
    }

    private void CacheExistingObjectiveRibbon()
    {
        objectiveStates.Clear();
        RectTransform ribbon = FindRect(hud.transform,"FinalObjectiveRibbon");
        if (ribbon == null) return;
        foreach (Text text in ribbon.GetComponentsInChildren<Text>(true))
            if (text.name == "State") objectiveStates[text] = text.text;
    }

    private void CacheExistingBuffStrip()
    {
        buffSlots.Clear();
        RectTransform strip = FindRect(hud.transform,"FinalBuffStrip");
        if (strip == null) return;
        for(int i=0;i<5;i++)
        {
            RectTransform slot = FindRect(strip,"BuffSlot_"+i);
            if (slot != null) buffSlots.Add(slot);
        }
    }

    private void BindPlayerIfChanged()
    {
        AOGActiveChampion current = AOGPlayerChampionAuthority.CurrentChampion;
        if (current == player) return;
        UnbindPlayer();
        player = current;
        if (player == null) return;

        progression = player.GetComponent<AOGChampionProgression>();
        economy = player.GetComponent<AOGPlayerEconomy>();
        matchStats = player.GetComponent<AOGChampionMatchStats>();

        if (progression != null)
        {
            progression.ProgressionChanged += OnProgressionChanged;
            lastLevel = progression.level;
            lastExperience = progression.experience;
            lastSkillPoints = progression.unspentSkillPoints;
            displayedXp = targetXp = progression.ExperienceRatio;
        }

        if (economy != null)
        {
            economy.EconomyChanged += OnEconomyChanged;
            lastGold = economy.gold;
            lastInventoryCount = economy.inventory.Count;
        }
        RefreshLiveText();
    }

    private void UnbindPlayer()
    {
        if (progression != null) progression.ProgressionChanged -= OnProgressionChanged;
        if (economy != null) economy.EconomyChanged -= OnEconomyChanged;
        player = null;
        progression = null;
        economy = null;
        matchStats = null;
    }

    private void OnProgressionChanged()
    {
        if (progression == null) return;
        targetXp = progression.ExperienceRatio;

        if (progression.level > lastLevel)
        {
            displayedXp = 0f;
            targetXp = progression.ExperienceRatio;
            StartCoroutine(PulseRect(xpRoot,1.08f,0.36f));
            if (skillPointText != null) StartCoroutine(SkillPointPulse("SKILL POINT AVAILABLE  +" + progression.unspentSkillPoints));
        }
        else if (progression.experience > lastExperience)
        {
            int delta = progression.experience - lastExperience;
            if (xpText != null) StartCoroutine(TemporaryXpDelta(delta));
        }

        if (progression.unspentSkillPoints > lastSkillPoints && progression.level == lastLevel && skillPointText != null)
            StartCoroutine(SkillPointPulse("SKILL POINT AVAILABLE  +" + progression.unspentSkillPoints));

        lastLevel = progression.level;
        lastExperience = progression.experience;
        lastSkillPoints = progression.unspentSkillPoints;
        RefreshLiveText();
    }

    private void OnEconomyChanged()
    {
        if (economy == null) return;
        int delta = economy.gold - lastGold;
        if (delta != 0) ShowGoldDelta(delta);

        if (economy.inventory.Count > lastInventoryCount)
        {
            if (itemSlots.Count > 0)
            {
                int slotIndex = Mathf.Clamp(economy.inventory.Count-1,0,itemSlots.Count-1);
                StartCoroutine(PulseRect(itemSlots[slotIndex],1.16f,0.48f));
            }
            if (goldBar != null) StartCoroutine(PulseRect(goldBar,1.07f,0.30f));
        }

        lastGold = economy.gold;
        lastInventoryCount = economy.inventory.Count;
    }

    private void RefreshLiveText()
    {
        if (progression != null)
        {
            if (xpText != null)
                xpText.text = progression.level >= progression.maxLevel ? "MAX LEVEL" : "XP  " + progression.experience + " / " + progression.experienceToNext;
            if (skillPointText != null)
                skillPointText.text = progression.unspentSkillPoints > 0 ? "+  " + progression.unspentSkillPoints + " SKILL POINT" + (progression.unspentSkillPoints > 1 ? "S" : "") : string.Empty;
            targetXp = progression.ExperienceRatio;
        }

        if (kdaText != null && matchStats != null)
        {
            kdaText.text = "K / D / A   " + matchStats.kills + " / " + matchStats.deaths + " / " + matchStats.assists;
            kdaText.color = matchStats.currentKillStreak >= 5 ? new Color(1f,0.42f,0.12f) : matchStats.currentKillStreak >= 3 ? new Color(0.96f,0.72f,0.26f) : new Color(0.72f,0.84f,0.94f);
        }
    }

    private void OnChampionDeath(AOGChampionDeathEvent data)
    {
        if (player == null) return;
        bool playerKill = data.killer != null && BelongsToPlayer(data.killer);
        bool playerAssist = false;
        if (!playerKill && data.assistants != null)
        {
            foreach (GameObject assistant in data.assistants)
            {
                if (assistant != null && BelongsToPlayer(assistant)) { playerAssist = true; break; }
            }
        }

        if (playerKill) StartCoroutine(DeferredCombatAnnouncement(true));
        else if (playerAssist) StartCoroutine(DeferredCombatAnnouncement(false));
    }

    private IEnumerator DeferredCombatAnnouncement(bool kill)
    {
        yield return null;
        RefreshLiveText();
        if (kill)
        {
            int streak = matchStats != null ? matchStats.currentKillStreak : 1;
            string message = streak >= 5 ? "ASCENDANT RAMPAGE  ×" + streak : streak >= 3 ? "DOMINATING STREAK  ×" + streak : "ELIMINATION SECURED";
            Color color = streak >= 5 ? new Color(1f,0.28f,0.10f) : new Color(0.96f,0.72f,0.24f);
            AOGScoreboardAndAnnouncerRuntime.Instance?.ShowExternalMessage(message,color,2.4f);
        }
        else
        {
            AOGScoreboardAndAnnouncerRuntime.Instance?.ShowExternalMessage("ASSIST CONFIRMED",new Color(0.36f,0.72f,1f),1.7f);
        }
    }

    private bool BelongsToPlayer(GameObject source)
    {
        if (source == null || player == null) return false;
        if (source == player.gameObject || source.transform.IsChildOf(player.transform)) return true;
        AOGCharacterStats stats = source.GetComponentInParent<AOGCharacterStats>();
        return stats != null && stats.gameObject == player.gameObject;
    }

    private void ShowGoldDelta(int delta)
    {
        if (goldDeltaText == null || goldDeltaGroup == null) return;
        if (goldDeltaRoutine != null) StopCoroutine(goldDeltaRoutine);
        goldDeltaRoutine = StartCoroutine(GoldDeltaRoutine(delta));
    }

    private IEnumerator GoldDeltaRoutine(int delta)
    {
        goldDeltaText.text = (delta > 0 ? "+" : "") + delta + " ◈";
        goldDeltaText.color = delta > 0 ? new Color(0.96f,0.78f,0.28f) : new Color(1f,0.34f,0.28f);
        goldDeltaGroup.alpha = 1f;
        RectTransform rect = goldDeltaText.rectTransform;
        Vector2 start = new Vector2(0f,4f);
        Vector2 end = new Vector2(0f,28f);
        float elapsed = 0f;
        while (elapsed < 0.70f)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed/0.70f);
            rect.anchoredPosition = Vector2.Lerp(start,end,t);
            goldDeltaGroup.alpha = 1f - Mathf.Clamp01((t-0.55f)/0.45f);
            yield return null;
        }
        rect.anchoredPosition = start;
        goldDeltaGroup.alpha = 0f;
        goldDeltaRoutine = null;
    }

    private IEnumerator TemporaryXpDelta(int delta)
    {
        if (xpText == null || progression == null) yield break;
        string finalText = "XP  " + progression.experience + " / " + progression.experienceToNext;
        xpText.text = "+" + delta + " XP";
        xpText.color = new Color(0.42f,0.78f,1f);
        yield return new WaitForSecondsRealtime(0.45f);
        xpText.text = finalText;
        xpText.color = Color.white;
    }

    private IEnumerator SkillPointPulse(string message)
    {
        if (skillPointText == null) yield break;
        skillPointText.text = message;
        RectTransform rect = skillPointText.rectTransform;
        Vector3 baseScale = rect.localScale;
        float elapsed = 0f;
        while (elapsed < 0.55f)
        {
            elapsed += Time.unscaledDeltaTime;
            float wave = Mathf.Sin(Mathf.Clamp01(elapsed/0.55f)*Mathf.PI);
            rect.localScale = baseScale*(1f+0.16f*wave);
            yield return null;
        }
        rect.localScale = baseScale;
    }

    private void AnimateExistingObjectiveRibbon()
    {
        if (objectiveStates.Count == 0) CacheExistingObjectiveRibbon();
        foreach (Text text in new List<Text>(objectiveStates.Keys))
        {
            if (text == null) continue;
            string previous = objectiveStates[text];
            if (previous == text.text) continue;
            objectiveStates[text] = text.text;
            StartCoroutine(PulseRect(text.transform.parent as RectTransform,1.12f,0.50f));
        }
    }

    private void AnimateBuffUrgency()
    {
        if (buffSlots.Count == 0) CacheExistingBuffStrip();
        foreach (RectTransform slot in buffSlots)
        {
            if (slot == null || !slot.gameObject.activeInHierarchy) continue;
            Text timer = FindText(slot,"Timer");
            if (timer == null) continue;
            int seconds;
            if (!int.TryParse(timer.text,out seconds)) continue;
            float pulse = seconds <= 10 ? 1f + 0.035f*Mathf.Sin(Time.unscaledTime*8f) : 1f;
            slot.localScale = Vector3.one*pulse;
            timer.color = seconds <= 5 ? new Color(1f,0.38f,0.26f) : Color.white;
        }
    }

    private IEnumerator PulseRect(RectTransform rect,float peak,float duration)
    {
        if (rect == null) yield break;
        Vector3 baseScale = rect.localScale;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed/duration);
            float wave = Mathf.Sin(t*Mathf.PI);
            rect.localScale = baseScale*(1f+(peak-1f)*wave);
            yield return null;
        }
        rect.localScale = baseScale;
    }

    private RectTransform Panel(string name,Transform parent,Vector2 anchorMin,Vector2 anchorMax,Vector2 pos,Vector2 size,Color color)
    {
        GameObject go = new GameObject(name,typeof(RectTransform),typeof(Image));
        go.transform.SetParent(parent,false);
        RectTransform rect=go.GetComponent<RectTransform>();
        rect.anchorMin=anchorMin;rect.anchorMax=anchorMax;rect.pivot=new Vector2(0.5f,0.5f);rect.anchoredPosition=pos;rect.sizeDelta=size;
        Image image=go.GetComponent<Image>();image.color=color;image.raycastTarget=false;
        return rect;
    }

    private Text Label(string name,Transform parent,string value,int size,TextAnchor alignment,Vector2 pos,Vector2 dimensions,Color color)
    {
        GameObject go=new GameObject(name,typeof(RectTransform),typeof(Text));go.transform.SetParent(parent,false);
        RectTransform rect=go.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=new Vector2(0.5f,0.5f);rect.pivot=new Vector2(0.5f,0.5f);rect.anchoredPosition=pos;rect.sizeDelta=dimensions;
        Text text=go.GetComponent<Text>();text.font=font;text.fontSize=size;text.fontStyle=FontStyle.Bold;text.alignment=alignment;text.color=color;text.text=value;text.raycastTarget=false;
        return text;
    }

    private static RectTransform FindRect(Transform root,string name)
    {
        foreach(RectTransform rect in root.GetComponentsInChildren<RectTransform>(true))if(rect.name==name)return rect;
        return null;
    }

    private static Text FindText(Transform root,string name)
    {
        foreach(Text text in root.GetComponentsInChildren<Text>(true))if(text.name==name)return text;
        return null;
    }
}
