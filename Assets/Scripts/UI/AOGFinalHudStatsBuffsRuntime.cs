using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Final HUD extension attached to the existing competitive HUD hierarchy.
/// Adds a toggleable advanced stat panel, compact timed buff icons and objective ribbon.
/// No second primary gameplay HUD or input authority is created.
/// </summary>
[DefaultExecutionOrder(2200)]
public class AOGFinalHudStatsBuffsRuntime : MonoBehaviour
{
    private class BuffSlot
    {
        public RectTransform root;
        public Image icon;
        public Text glyph;
        public Text timer;
        public Text stacks;
    }

    private AOGCompetitiveMobaHUDRuntime hud;
    private RectTransform hudCanvas;
    private RectTransform statsPanel;
    private Text statsText;
    private Text statsHint;
    private RectTransform buffStrip;
    private readonly List<BuffSlot> buffSlots = new List<BuffSlot>();
    private readonly Dictionary<string,Text> objectiveTexts = new Dictionary<string,Text>();

    private AOGActiveChampion player;
    private AOGCharacterStats characterStats;
    private AOGCombatStatBlock combatStats;
    private AOGJungleBuffRuntime jungleBuffs;
    private AOGChampionProgression progression;
    private AOGPlayerEconomy economy;

    private bool built;
    private bool statsVisible;
    private float nextBind;
    private float nextObjectiveRefresh;
    private Font font;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindFirstObjectByType<AOGFinalHudStatsBuffsRuntime>() != null)
            return;

        GameObject host = new GameObject("AOG_Final_HUD_Stats_Buffs_Runtime");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGFinalHudStatsBuffsRuntime>();
    }

    private void Awake()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    private void Update()
    {
        if (!built)
        {
            hud = FindFirstObjectByType<AOGCompetitiveMobaHUDRuntime>();
            if (hud != null)
            {
                Canvas canvas = hud.GetComponentInChildren<Canvas>(true);
                if (canvas != null)
                {
                    hudCanvas = canvas.GetComponent<RectTransform>();
                    BuildUi();
                    DisableLegacyBuffText();
                    built = true;
                }
            }
            return;
        }

        if (Time.unscaledTime >= nextBind)
        {
            nextBind = Time.unscaledTime + 0.35f;
            BindPlayer();
            RefreshStats();
            RefreshBuffs();
        }

        if (Time.unscaledTime >= nextObjectiveRefresh)
        {
            nextObjectiveRefresh = Time.unscaledTime + 0.5f;
            RefreshObjectives();
        }

        if (AOGInputBridge.KeyPressedThisFrame(KeyCode.C))
        {
            statsVisible = !statsVisible;
            if (statsPanel != null)
                statsPanel.gameObject.SetActive(statsVisible);
        }
    }

    private void BindPlayer()
    {
        AOGActiveChampion current = AOGPlayerChampionAuthority.CurrentChampion;
        if (current == player)
            return;

        player = current;
        characterStats = player != null ? player.GetComponent<AOGCharacterStats>() : null;
        combatStats = player != null ? player.GetComponent<AOGCombatStatBlock>() : null;
        jungleBuffs = player != null ? player.GetComponent<AOGJungleBuffRuntime>() : null;
        progression = player != null ? player.GetComponent<AOGChampionProgression>() : null;
        economy = player != null ? player.GetComponent<AOGPlayerEconomy>() : null;
    }

    private void BuildUi()
    {
        BuildStatsPanel();
        BuildBuffStrip();
        BuildObjectiveRibbon();
    }

    private void BuildStatsPanel()
    {
        statsPanel = Panel(
            "AdvancedStatsPanel",
            hudCanvas,
            new Vector2(0f,0f),
            new Vector2(0f,0f),
            new Vector2(18f,18f),
            new Vector2(286f,286f),
            new Color(0.010f,0.020f,0.033f,0.965f));

        Outline outline = statsPanel.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.22f,0.44f,0.62f,0.95f);
        outline.effectDistance = new Vector2(1.5f,-1.5f);

        Label("Title",statsPanel,"COMBAT MATRIX",18,TextAnchor.MiddleLeft,new Vector2(18f,-18f),new Vector2(220f,32f),new Color(0.72f,0.86f,1f),new Vector2(0f,1f));
        statsText = Label("Stats",statsPanel,"WAITING FOR CHAMPION",14,TextAnchor.UpperLeft,new Vector2(18f,-62f),new Vector2(250f,178f),new Color(0.80f,0.86f,0.91f),new Vector2(0f,1f));
        statsHint = Label("Hint",statsPanel,"C  CLOSE",12,TextAnchor.MiddleRight,new Vector2(-18f,16f),new Vector2(160f,24f),new Color(0.48f,0.62f,0.72f),new Vector2(1f,0f));
        statsPanel.gameObject.SetActive(false);
    }

    private void BuildBuffStrip()
    {
        buffStrip = Panel(
            "FinalBuffStrip",
            hudCanvas,
            new Vector2(0.5f,0f),
            new Vector2(0.5f,0f),
            new Vector2(0f,190f),
            new Vector2(360f,58f),
            new Color(0.008f,0.016f,0.026f,0.78f));

        for (int i=0;i<5;i++)
        {
            RectTransform slot = Panel(
                "BuffSlot_"+i,
                buffStrip,
                new Vector2(0f,0.5f),
                new Vector2(0f,0.5f),
                new Vector2(10f+i*70f,0f),
                new Vector2(62f,50f),
                new Color(0.025f,0.045f,0.065f,0.95f));

            Image icon = Panel("Icon",slot,new Vector2(0f,0.5f),new Vector2(0f,0.5f),new Vector2(5f,0f),new Vector2(40f,40f),new Color(0.16f,0.30f,0.44f,1f)).GetComponent<Image>();
            Text glyph = Label("Glyph",icon.rectTransform,"◆",20,TextAnchor.MiddleCenter,Vector2.zero,new Vector2(38f,38f),Color.white);
            Text timer = Label("Timer",slot,"",11,TextAnchor.MiddleRight,new Vector2(-3f,-10f),new Vector2(20f,18f),Color.white,new Vector2(1f,0.5f));
            Text stacks = Label("Stacks",slot,"",10,TextAnchor.MiddleRight,new Vector2(-3f,10f),new Vector2(20f,18f),new Color(0.92f,0.78f,0.36f),new Vector2(1f,0.5f));

            buffSlots.Add(new BuffSlot{root=slot,icon=icon,glyph=glyph,timer=timer,stacks=stacks});
            slot.gameObject.SetActive(false);
        }
    }

    private void BuildObjectiveRibbon()
    {
        RectTransform ribbon = Panel(
            "FinalObjectiveRibbon",
            hudCanvas,
            new Vector2(0.5f,1f),
            new Vector2(0.5f,1f),
            new Vector2(0f,-78f),
            new Vector2(360f,40f),
            new Color(0.010f,0.018f,0.030f,0.80f));

        BuildObjectiveCell(ribbon,"DRAGON","DRG",0,new Color(1f,0.42f,0.08f));
        BuildObjectiveCell(ribbon,"MEDUSA","MED",1,new Color(0.66f,0.28f,0.94f));
        BuildObjectiveCell(ribbon,"TITAN","TIT",2,new Color(0.44f,0.18f,0.86f));
    }

    private void BuildObjectiveCell(RectTransform parent,string key,string shortName,int index,Color accent)
    {
        RectTransform cell = Panel(
            key,
            parent,
            new Vector2(0f,0.5f),
            new Vector2(0f,0.5f),
            new Vector2(8f+index*116f,0f),
            new Vector2(108f,32f),
            new Color(accent.r*0.12f,accent.g*0.12f,accent.b*0.12f,0.95f));
        Outline outline = cell.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(accent.r,accent.g,accent.b,0.75f);
        outline.effectDistance = new Vector2(1f,-1f);
        Text text = Label("State",cell,shortName+"  --",11,TextAnchor.MiddleCenter,Vector2.zero,new Vector2(100f,28f),Color.Lerp(accent,Color.white,0.32f));
        objectiveTexts[key] = text;
    }

    private void RefreshStats()
    {
        if (statsText == null)
            return;

        if (characterStats == null)
        {
            statsText.text = "WAITING FOR CHAMPION";
            return;
        }

        float armor = combatStats != null ? combatStats.armor : 0f;
        float mr = combatStats != null ? combatStats.magicResistance : 0f;
        float ap = combatStats != null ? combatStats.abilityPower : 0f;
        float haste = combatStats != null ? combatStats.abilityHaste : 0f;
        float lifesteal = combatStats != null ? combatStats.lifesteal*100f : 0f;
        float spellVamp = combatStats != null ? combatStats.spellVamp*100f : 0f;
        float attacksPerSecond = 1f/Mathf.Max(0.25f,characterStats.attackCooldown);
        float level = progression != null ? progression.level : 1;
        int gold = economy != null ? economy.gold : 0;

        statsText.text =
            "LEVEL            " + level.ToString("0") + "\n" +
            "ATTACK DAMAGE    " + characterStats.attackDamage.ToString("0") + "\n" +
            "ABILITY POWER    " + ap.ToString("0") + "\n" +
            "ARMOR            " + armor.ToString("0") + "\n" +
            "MAGIC RESIST     " + mr.ToString("0") + "\n" +
            "ATTACK RATE      " + attacksPerSecond.ToString("0.00") + "/s\n" +
            "MOVE SPEED       " + characterStats.moveSpeed.ToString("0.00") + "\n" +
            "ABILITY HASTE    " + haste.ToString("0") + "\n" +
            "LIFESTEAL        " + lifesteal.ToString("0") + "%\n" +
            "SPELL VAMP       " + spellVamp.ToString("0") + "%\n" +
            "GOLD             ◈ " + gold;
    }

    private void RefreshBuffs()
    {
        foreach (BuffSlot slot in buffSlots)
            slot.root.gameObject.SetActive(false);

        if (jungleBuffs == null)
            return;

        int index = 0;
        foreach (KeyValuePair<AOGTemporaryBuffType,float> pair in jungleBuffs.Remaining)
        {
            if (pair.Value <= 0f || index >= buffSlots.Count)
                continue;

            BuffSlot slot = buffSlots[index++];
            slot.root.gameObject.SetActive(true);
            Color color = AOGJungleBuffRuntime.BuffColor(pair.Key);
            slot.icon.color = new Color(color.r*0.28f,color.g*0.28f,color.b*0.28f,1f);
            slot.glyph.color = Color.Lerp(color,Color.white,0.22f);
            slot.glyph.text = BuffGlyph(pair.Key);
            slot.timer.text = Mathf.CeilToInt(pair.Value).ToString();
            slot.stacks.text = string.Empty;
        }
    }

    private void RefreshObjectives()
    {
        AOGNeutralBossAI dragon = null;
        AOGNeutralBossAI medusa = null;
        AOGNeutralBossAI titan = null;

        foreach (AOGNeutralBossAI boss in FindObjectsByType<AOGNeutralBossAI>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {
            if (boss == null)
                continue;
            if (boss.GetComponent<AOGVoidTitanMarker>() != null)
                titan = boss;
            else if (boss.bossType == AOGNeutralBossType.Dragon)
                dragon = boss;
            else
                medusa = boss;
        }

        SetObjectiveText("DRAGON","DRG",dragon);
        SetObjectiveText("MEDUSA","MED",medusa);
        SetObjectiveText("TITAN","TIT",titan);
    }

    private void SetObjectiveText(string key,string shortName,AOGNeutralBossAI boss)
    {
        if (!objectiveTexts.TryGetValue(key,out Text text) || text == null)
            return;

        if (boss == null)
        {
            text.text = shortName + "  LOCK";
            return;
        }

        text.text = shortName + (boss.IsDead ? "  DOWN" : "  LIVE");
    }

    private void DisableLegacyBuffText()
    {
        AOGPlayerBuffHudRuntime legacy = FindFirstObjectByType<AOGPlayerBuffHudRuntime>();
        if (legacy == null)
            return;
        Transform canvas = legacy.transform.Find("BuffHudCanvas");
        if (canvas != null)
            canvas.gameObject.SetActive(false);
    }

    private string BuffGlyph(AOGTemporaryBuffType type)
    {
        switch (type)
        {
            case AOGTemporaryBuffType.AetherFlow: return "↯";
            case AOGTemporaryBuffType.InfernalFury: return "▲";
            case AOGTemporaryBuffType.DragonResonance: return "◇";
            case AOGTemporaryBuffType.MedusaInsight: return "◎";
            default: return "◆";
        }
    }

    private RectTransform Panel(string name,Transform parent,Vector2 anchorMin,Vector2 anchorMax,Vector2 position,Vector2 size,Color color)
    {
        GameObject go = new GameObject(name,typeof(RectTransform),typeof(Image));
        go.transform.SetParent(parent,false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = anchorMin;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        go.GetComponent<Image>().color = color;
        return rect;
    }

    private Text Label(string name,Transform parent,string value,int size,TextAnchor alignment,Vector2 position,Vector2 dimensions,Color color,Vector2? anchor=null)
    {
        GameObject go = new GameObject(name,typeof(RectTransform),typeof(Text));
        go.transform.SetParent(parent,false);
        RectTransform rect = go.GetComponent<RectTransform>();
        Vector2 a = anchor ?? new Vector2(0.5f,0.5f);
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
        return text;
    }
}
