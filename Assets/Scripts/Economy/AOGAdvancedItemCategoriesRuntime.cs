using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum AOGItemCategory
{
    Attack,
    Magic,
    Tank,
    Mobility,
    Haste,
    Sustain,
    Utility
}

public class AOGAdvancedItemDefinition : AOGItemDefinition
{
    public AOGItemCategory category;
    public float abilityPower;
    public float armor;
    public float magicResistance;
    public float attackSpeedBonus;
    public float abilityHaste;
    public float lifesteal;
    public float spellVamp;
    public float resourceBonus;
    public float resourceRegen;
}

/// <summary>
/// Applies the advanced part of item definitions after the existing economy Buy method
/// adds them to inventory. Base HP/AD/move speed remains handled by AOGPlayerEconomy.
/// </summary>
public class AOGAdvancedInventoryStatsRuntime : MonoBehaviour
{
    private AOGPlayerEconomy economy;
    private AOGCombatStatBlock block;
    private readonly HashSet<string> appliedIds = new HashSet<string>();

    private void Awake()
    {
        economy=GetComponent<AOGPlayerEconomy>();
        block=GetComponent<AOGCombatStatBlock>();
        if(block==null)block=gameObject.AddComponent<AOGCombatStatBlock>();
    }

    private void OnEnable()
    {
        if(economy!=null)economy.EconomyChanged+=Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        if(economy!=null)economy.EconomyChanged-=Refresh;
    }

    public void Refresh()
    {
        if(economy==null||block==null)return;
        foreach(AOGItemDefinition baseItem in economy.inventory)
        {
            AOGAdvancedItemDefinition item=baseItem as AOGAdvancedItemDefinition;
            if(item==null||string.IsNullOrEmpty(item.id)||appliedIds.Contains(item.id))continue;
            appliedIds.Add(item.id);
            block.abilityPower+=item.abilityPower;
            block.armor+=item.armor;
            block.magicResistance+=item.magicResistance;
            block.attackSpeedBonus+=item.attackSpeedBonus;
            block.abilityHaste+=item.abilityHaste;
            block.lifesteal+=item.lifesteal;
            block.spellVamp+=item.spellVamp;
            block.maxResourceBonus+=item.resourceBonus;
            block.resourceRegenBonus+=item.resourceRegen;
        }
    }
}

/// <summary>
/// Extends the existing Aether Market panel with one category-driven desktop item wing.
/// The original market remains the single shop authority and AOGPlayerEconomy.Buy remains
/// the only purchase path.
/// </summary>
public class AOGAdvancedMarketCategoriesRuntime : MonoBehaviour
{
    private readonly List<AOGAdvancedItemDefinition> catalog=new List<AOGAdvancedItemDefinition>();
    private RectTransform extension;
    private RectTransform cardsRoot;
    private Text categoryTitle;
    private Text detailText;
    private Font font;
    private AOGItemCategory selected=AOGItemCategory.Attack;
    private AOGPlayerEconomy economy;
    private bool built;
    private float nextBind;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if(FindFirstObjectByType<AOGAdvancedMarketCategoriesRuntime>()!=null)return;
        GameObject host=new GameObject("AOG_Advanced_Market_Categories_Runtime");DontDestroyOnLoad(host);host.AddComponent<AOGAdvancedMarketCategoriesRuntime>();
    }

    private void Awake()
    {
        font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        BuildCatalog();
    }

    private void Update()
    {
        if(!built)
        {
            GameObject market=GameObject.Find("AetherMarket");
            if(market!=null)
            {
                BuildExtension(market.GetComponent<RectTransform>());
                built=true;
                SelectCategory(selected);
            }
        }

        if(Time.unscaledTime>=nextBind)
        {
            nextBind=Time.unscaledTime+0.5f;
            AOGActiveChampion player=AOGPlayerChampionAuthority.CurrentChampion;
            AOGPlayerEconomy current=player!=null?player.GetComponent<AOGPlayerEconomy>():null;
            if(current!=economy)Bind(current);
        }
    }

    private void Bind(AOGPlayerEconomy next)
    {
        economy=next;
        if(economy!=null&&economy.GetComponent<AOGAdvancedInventoryStatsRuntime>()==null)
            economy.gameObject.AddComponent<AOGAdvancedInventoryStatsRuntime>();
        RefreshDetail();
    }

    private void BuildCatalog()
    {
        catalog.Add(Item("sunsteel_edge","SUNSTEEL EDGE",AOGItemCategory.Attack,1350,"+32 AD • +8% attack tempo",new Color(0.95f,0.52f,0.18f),ad:32f,attackSpeed:0.08f));
        catalog.Add(Item("void_repeater","VOID REPEATER",AOGItemCategory.Attack,1650,"+24 AD • +18% attack tempo",new Color(0.58f,0.26f,0.88f),ad:24f,attackSpeed:0.18f));

        catalog.Add(Item("astral_codex","ASTRAL CODEX",AOGItemCategory.Magic,1400,"+72 AP • +10 haste",new Color(0.34f,0.68f,1f),ap:72f,haste:10f));
        catalog.Add(Item("ember_crown","EMBER CROWN",AOGItemCategory.Magic,1750,"+96 AP • +6% spell vamp",new Color(1f,0.24f,0.05f),ap:96f,spellVamp:0.06f));

        catalog.Add(Item("citadel_plate","CITADEL PLATE",AOGItemCategory.Tank,1450,"+320 HP • +38 armor",new Color(0.46f,0.58f,0.66f),hp:320f,armor:38f));
        catalog.Add(Item("oracle_bastion","ORACLE BASTION",AOGItemCategory.Tank,1550,"+260 HP • +42 magic resist",new Color(0.38f,0.76f,0.70f),hp:260f,mr:42f));

        catalog.Add(Item("riftstep_greaves","RIFTSTEP GREAVES",AOGItemCategory.Mobility,1050,"+1.15 move speed",new Color(0.32f,0.78f,0.92f),move:1.15f));
        catalog.Add(Item("windglass_treads","WINDGLASS TREADS",AOGItemCategory.Mobility,1250,"+0.8 move • +12 haste",new Color(0.56f,0.84f,0.94f),move:0.8f,haste:12f));

        catalog.Add(Item("chronicle_shard","CHRONICLE SHARD",AOGItemCategory.Haste,1300,"+45 AP • +22 haste",new Color(0.44f,0.42f,0.96f),ap:45f,haste:22f));
        catalog.Add(Item("warclock_core","WARCLOCK CORE",AOGItemCategory.Haste,1550,"+18 AD • +28 haste",new Color(0.82f,0.56f,0.22f),ad:18f,haste:28f));

        catalog.Add(Item("moonwell_sigil","MOONWELL SIGIL",AOGItemCategory.Sustain,1350,"+18 AD • +10% lifesteal",new Color(0.46f,0.58f,1f),ad:18f,lifesteal:0.10f));
        catalog.Add(Item("spirit_chalice","SPIRIT CHALICE",AOGItemCategory.Sustain,1450,"+52 AP • +10% spell vamp",new Color(0.94f,0.34f,0.72f),ap:52f,spellVamp:0.10f));

        catalog.Add(Item("aether_reservoir","AETHER RESERVOIR",AOGItemCategory.Utility,1200,"+240 resource • +8 regen",new Color(0.20f,0.70f,1f),resource:240f,resourceRegen:8f));
        catalog.Add(Item("veil_compass","VEIL COMPASS",AOGItemCategory.Utility,1500,"+18 haste • +0.45 move",new Color(0.42f,0.86f,0.66f),move:0.45f,haste:18f));
    }

    private AOGAdvancedItemDefinition Item(string id,string name,AOGItemCategory category,int cost,string description,Color accent,float hp=0f,float ad=0f,float move=0f,float ap=0f,float armor=0f,float mr=0f,float attackSpeed=0f,float haste=0f,float lifesteal=0f,float spellVamp=0f,float resource=0f,float resourceRegen=0f)
    {
        return new AOGAdvancedItemDefinition
        {
            id=id,displayName=name,category=category,cost=cost,description=description,accent=accent,
            bonusHp=hp,bonusDamage=ad,bonusMoveSpeed=move,abilityPower=ap,armor=armor,magicResistance=mr,
            attackSpeedBonus=attackSpeed,abilityHaste=haste,lifesteal=lifesteal,spellVamp=spellVamp,
            resourceBonus=resource,resourceRegen=resourceRegen
        };
    }

    private void BuildExtension(RectTransform market)
    {
        extension=Panel("AscensionWing",market,new Vector2(0f,0.5f),new Vector2(0f,0.5f),new Vector2(780f,0f),new Vector2(620f,820f),new Color(0.010f,0.020f,0.032f,0.985f));
        Outline outline=extension.gameObject.AddComponent<Outline>();outline.effectColor=new Color(0.30f,0.56f,0.78f);outline.effectDistance=new Vector2(2f,-2f);

        Text title=Label("WingTitle",extension,"ASCENSION ARMORY",28,TextAnchor.MiddleLeft,new Vector2(28f,-30f),new Vector2(420f,44f),new Color(0.72f,0.86f,1f),new Vector2(0f,1f));
        categoryTitle=Label("CategoryTitle",extension,"ATTACK",18,TextAnchor.MiddleRight,new Vector2(-28f,-34f),new Vector2(160f,38f),Color.white,new Vector2(1f,1f));

        RectTransform tabs=Panel("CategoryTabs",extension,new Vector2(0f,1f),new Vector2(0f,1f),new Vector2(24f,-86f),new Vector2(572f,74f),new Color(0.022f,0.040f,0.058f,0.98f));
        AOGItemCategory[] values=(AOGItemCategory[])System.Enum.GetValues(typeof(AOGItemCategory));
        for(int i=0;i<values.Length;i++)CreateCategoryButton(tabs,values[i],i);

        cardsRoot=Panel("AdvancedItemCards",extension,new Vector2(0f,1f),new Vector2(0f,1f),new Vector2(24f,-182f),new Vector2(572f,470f),new Color(0.018f,0.030f,0.045f,0.88f));
        detailText=Label("AdvancedDetail",extension,"Select a category and buy inside your allied base.",15,TextAnchor.UpperLeft,new Vector2(30f,42f),new Vector2(550f,110f),new Color(0.66f,0.76f,0.84f),new Vector2(0f,0f));
    }

    private void CreateCategoryButton(RectTransform parent,AOGItemCategory category,int index)
    {
        float width=76f;
        GameObject go=new GameObject(category.ToString(),typeof(RectTransform),typeof(Image),typeof(Button));go.transform.SetParent(parent,false);
        RectTransform r=go.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=new Vector2(0f,0.5f);r.pivot=new Vector2(0f,0.5f);r.anchoredPosition=new Vector2(8f+index*80f,0f);r.sizeDelta=new Vector2(width,48f);
        Image image=go.GetComponent<Image>();image.color=new Color(0.05f,0.09f,0.13f,1f);
        Button button=go.GetComponent<Button>();AOGItemCategory captured=category;button.onClick.AddListener(()=>SelectCategory(captured));
        Label("Text",r,ShortCategory(category),11,TextAnchor.MiddleCenter,Vector2.zero,new Vector2(width-4f,44f),new Color(0.78f,0.86f,0.92f));
    }

    private void SelectCategory(AOGItemCategory category)
    {
        selected=category;
        if(categoryTitle!=null)categoryTitle.text=category.ToString().ToUpperInvariant();
        if(cardsRoot==null)return;
        for(int i=cardsRoot.childCount-1;i>=0;i--)Destroy(cardsRoot.GetChild(i).gameObject);

        int index=0;
        foreach(AOGAdvancedItemDefinition item in catalog)
        {
            if(item.category!=category)continue;
            BuildCard(item,index++);
        }
        RefreshDetail();
    }

    private void BuildCard(AOGAdvancedItemDefinition item,int index)
    {
        RectTransform card=Panel("Advanced_"+item.id,cardsRoot,new Vector2(0f,1f),new Vector2(0f,1f),new Vector2(16f,-18f-index*210f),new Vector2(540f,188f),new Color(0.035f,0.058f,0.080f,0.98f));
        Outline outline=card.gameObject.AddComponent<Outline>();outline.effectColor=item.accent;outline.effectDistance=new Vector2(1.5f,-1.5f);
        Panel("Icon",card,new Vector2(0f,0.5f),new Vector2(0f,0.5f),new Vector2(18f,0f),new Vector2(100f,100f),new Color(item.accent.r*0.24f,item.accent.g*0.24f,item.accent.b*0.24f,1f));
        Label("Name",card,item.displayName,18,TextAnchor.MiddleLeft,new Vector2(136f,-22f),new Vector2(270f,34f),Color.white,new Vector2(0f,1f));
        Label("Stats",card,item.description,14,TextAnchor.MiddleLeft,new Vector2(136f,-60f),new Vector2(360f,44f),new Color(0.72f,0.82f,0.90f),new Vector2(0f,1f));
        Label("Cost",card,"◈ "+item.cost,18,TextAnchor.MiddleLeft,new Vector2(136f,24f),new Vector2(150f,34f),new Color(0.94f,0.72f,0.28f),new Vector2(0f,0f));

        GameObject buy=new GameObject("Buy",typeof(RectTransform),typeof(Image),typeof(Button));buy.transform.SetParent(card,false);RectTransform br=buy.GetComponent<RectTransform>();br.anchorMin=br.anchorMax=new Vector2(1f,0f);br.pivot=new Vector2(1f,0f);br.anchoredPosition=new Vector2(-18f,18f);br.sizeDelta=new Vector2(128f,42f);buy.GetComponent<Image>().color=Color.Lerp(item.accent,new Color(0.05f,0.08f,0.11f),0.55f);Button button=buy.GetComponent<Button>();button.onClick.AddListener(()=>TryBuy(item));Label("Text",br,"BUY",15,TextAnchor.MiddleCenter,Vector2.zero,new Vector2(124f,38f),Color.white);
    }

    private void TryBuy(AOGAdvancedItemDefinition item)
    {
        if(economy==null){SetDetail("NO ACTIVE CHAMPION ECONOMY");return;}
        if(!AOGBaseAccessUtility.IsShopAvailable(economy)){SetDetail("SHOP OUT OF RANGE — RECALL WITH B");return;}
        if(economy.Buy(item))
        {
            AOGAdvancedInventoryStatsRuntime runtime=economy.GetComponent<AOGAdvancedInventoryStatsRuntime>();runtime?.Refresh();
            SetDetail("PURCHASED  •  "+item.displayName+"  •  POWER INCREASED");
            SpawnPurchaseFeedback(item.accent);
        }
        else SetDetail(economy.inventory.Count>=economy.inventoryCapacity?"INVENTORY FULL":"NOT ENOUGH GOLD");
    }

    private void SpawnPurchaseFeedback(Color accent)
    {
        if(economy==null)return;
        GameObject ring=AOGAbilityVisuals.CreateRing("Advanced_Item_Purchase",economy.transform.position+Vector3.up*0.06f,2.2f,accent,0.11f);Destroy(ring,0.6f);
    }

    private void RefreshDetail()
    {
        if(detailText==null)return;
        SetDetail(economy==null?"WAITING FOR ACTIVE CHAMPION":"CATEGORY: "+selected.ToString().ToUpperInvariant()+"   •   GOLD: ◈ "+economy.gold);
    }

    private void SetDetail(string text){if(detailText!=null)detailText.text=text;}

    private string ShortCategory(AOGItemCategory category)
    {
        switch(category){case AOGItemCategory.Attack:return"ATK";case AOGItemCategory.Magic:return"MAG";case AOGItemCategory.Tank:return"TANK";case AOGItemCategory.Mobility:return"MOVE";case AOGItemCategory.Haste:return"HASTE";case AOGItemCategory.Sustain:return"SUST";default:return"UTIL";}
    }

    private RectTransform Panel(string name,Transform parent,Vector2 anchorMin,Vector2 anchorMax,Vector2 pos,Vector2 size,Color color)
    {
        GameObject go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);RectTransform r=go.GetComponent<RectTransform>();r.anchorMin=anchorMin;r.anchorMax=anchorMax;r.pivot=anchorMin;r.anchoredPosition=pos;r.sizeDelta=size;go.GetComponent<Image>().color=color;return r;
    }

    private Text Label(string name,Transform parent,string text,int size,TextAnchor alignment,Vector2 pos,Vector2 rectSize,Color color,Vector2? anchor=null)
    {
        GameObject go=new GameObject(name,typeof(RectTransform),typeof(Text));go.transform.SetParent(parent,false);RectTransform r=go.GetComponent<RectTransform>();Vector2 a=anchor??new Vector2(0.5f,0.5f);r.anchorMin=r.anchorMax=a;r.pivot=a;r.anchoredPosition=pos;r.sizeDelta=rectSize;Text label=go.GetComponent<Text>();label.font=font;label.fontSize=size;label.fontStyle=FontStyle.Bold;label.alignment=alignment;label.color=color;label.text=text;label.raycastTarget=false;return label;
    }
}

public class AOGAdvancedInventoryStatsBootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if(FindFirstObjectByType<AOGAdvancedInventoryStatsBootstrap>()!=null)return;
        GameObject host=new GameObject("AOG_Advanced_Inventory_Stats_Bootstrap");DontDestroyOnLoad(host);host.AddComponent<AOGAdvancedInventoryStatsBootstrap>();
    }

    private float nextScan;
    private void Update()
    {
        if(Time.unscaledTime<nextScan)return;nextScan=Time.unscaledTime+0.75f;
        foreach(AOGPlayerEconomy economy in FindObjectsByType<AOGPlayerEconomy>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            if(economy!=null&&economy.GetComponent<AOGAdvancedInventoryStatsRuntime>()==null)economy.gameObject.AddComponent<AOGAdvancedInventoryStatsRuntime>();
    }
}
