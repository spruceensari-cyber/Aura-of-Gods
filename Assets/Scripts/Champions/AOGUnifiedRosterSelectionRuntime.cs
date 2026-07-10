using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DefaultExecutionOrder(-780)]
public class AOGUnifiedRosterSelectionRuntime : MonoBehaviour
{
    private const string PlayableSceneName = "AOGSymmetricReferenceMap_TowerTest";
    private static AOGUnifiedRosterSelectionRuntime instance;

    private readonly List<AOGActiveChampion> candidates = new List<AOGActiveChampion>();
    private Canvas canvas;
    private Font font;
    private bool building;
    private bool selected;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        EnsureInstance();
    }

    private static void EnsureInstance()
    {
        if (instance != null)
            return;
        GameObject host = new GameObject("AOG_Unified_Roster_Selection_Runtime");
        instance = host.AddComponent<AOGUnifiedRosterSelectionRuntime>();
        DontDestroyOnLoad(host);
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureInstance();
        if (instance == null)
            return;

        instance.StopAllCoroutines();
        instance.selected = false;
        instance.building = false;
        instance.ClearSelectionCanvas();

        if (string.Equals(scene.name, PlayableSceneName, System.StringComparison.OrdinalIgnoreCase))
            instance.StartCoroutine(instance.BuildFlow());
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
    }

    private IEnumerator Start()
    {
        yield return null;
        Scene scene = SceneManager.GetActiveScene();
        if (scene.IsValid() && string.Equals(scene.name, PlayableSceneName, System.StringComparison.OrdinalIgnoreCase))
            yield return BuildFlow();
    }

    private IEnumerator BuildFlow()
    {
        if (building)
            yield break;
        building = true;

        yield return new WaitForSecondsRealtime(0.45f);
        DisableConflictingSelectionRuntimes();
        DestroyLegacySelectionCanvases();
        EnsureRosterCandidates();
        yield return null;
        CollectCandidates();

        if (candidates.Count == 0)
        {
            Debug.LogError("AOG unified roster: no playable champion candidates were created.");
            building = false;
            yield break;
        }

        DeactivateAllCandidates();
        BuildSelectionUI();
        building = false;
    }

    private void DisableConflictingSelectionRuntimes()
    {
        foreach (AOGChampionSelectionRuntime legacy in FindObjectsByType<AOGChampionSelectionRuntime>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (legacy != null) legacy.enabled = false;

        foreach (AOGExpandedHeroRosterRuntime legacy in FindObjectsByType<AOGExpandedHeroRosterRuntime>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (legacy != null) legacy.enabled = false;

        foreach (AOGPremiumFemaleHeroRosterRuntime legacy in FindObjectsByType<AOGPremiumFemaleHeroRosterRuntime>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (legacy != null) legacy.enabled = false;

        foreach (AOGChampionSelectRecoveryRuntime recovery in FindObjectsByType<AOGChampionSelectRecoveryRuntime>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (recovery != null) recovery.enabled = false;
    }

    private void DestroyLegacySelectionCanvases()
    {
        foreach (Canvas c in FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (c != null && c.gameObject.name == "ChampionSelectCanvas")
                Destroy(c.gameObject);
    }

    private void EnsureRosterCandidates()
    {
        EnsureLyra();
        EnsureKaelith();
        EnsureAuron();
        EnsureVesper();
        EnsurePremiumMage("nyra", "NYRA", "SPIRIT VIXEN", AOGPremiumMageType.NyraSpiritVixen, new Color(0.94f, 0.28f, 0.74f));
        EnsurePremiumMage("pyrelle", "PYRELLE", "FLAME SOVEREIGN", AOGPremiumMageType.PyrelleFlameSovereign, new Color(1f, 0.22f, 0.035f));
        EnsurePremiumMage("selene", "SELENE", "ASTRAL ORACLE", AOGPremiumMageType.SeleneAstralOracle, new Color(0.36f, 0.68f, 1f));
        EnsureSupportJungle("seris", "SERIS", "AETHER VEIL", AOGSupportJungleHeroType.Seris, new Color(0.24f,0.88f,0.95f), 920f, 48f, 6.1f, 1.0f, 6.2f);
        EnsureSupportJungle("mireva", "MIREVA", "BLOOM WARDEN", AOGSupportJungleHeroType.Mireva, new Color(0.18f,0.82f,0.42f), 980f, 50f, 6.0f, 1.02f, 6.0f);
        EnsureSupportJungle("dravenor", "DRAVENOR", "FANG STALKER", AOGSupportJungleHeroType.Dravenor, new Color(0.92f,0.28f,0.08f), 1040f, 70f, 2.8f, 0.90f, 6.8f);
        EnsureSupportJungle("nocthyr", "NOCTHYR", "SHADE TRACKER", AOGSupportJungleHeroType.Nocthyr, new Color(0.28f,0.18f,0.72f), 850f, 74f, 2.7f, 0.82f, 7.1f);
    }

    private void EnsureLyra()
    {
        AOGActiveChampion marker = FindCandidateById("lyra");
        if (marker == null)
        {
            GameObject obj = FindRootByToken("lyra_player") ?? FindRootByToken("lyra");
            if (obj == null)
            {
                obj = new GameObject("Lyra_Player");
                AOGOriginalChampionModelFactory.BuildChampion(obj.transform, "lyra", new Color(0.62f,0.28f,0.92f));
            }
            marker = EnsureCommon(obj, "lyra", "LYRA", "MOON HUNTRESS", new Color(0.62f,0.28f,0.92f), 900f, 55f, 5.6f, 0.96f, 6.0f);
        }
        if (marker.GetComponent<LyraSkillSet>() == null) marker.gameObject.AddComponent<LyraSkillSet>();
    }

    private void EnsureKaelith()
    {
        AOGActiveChampion marker = FindCandidateById("kaelith");
        if (marker == null)
        {
            GameObject obj = FindRootByToken("kaelith_player") ?? new GameObject("Kaelith_Player");
            if (obj.GetComponentInChildren<Renderer>(true) == null)
                AOGOriginalChampionModelFactory.BuildChampion(obj.transform, "kaelith", new Color(0.36f,0.18f,0.86f));
            marker = EnsureCommon(obj, "kaelith", "KAELITH", "ECLIPSE REAVER", new Color(0.36f,0.18f,0.86f), 980f, 64f, 3.2f, 0.92f, 6.25f);
        }
        if (marker.GetComponent<KaelithEclipseSkillSet>() == null) marker.gameObject.AddComponent<KaelithEclipseSkillSet>();
        if (marker.GetComponent<AOGChampionProceduralAnimator>() == null) marker.gameObject.AddComponent<AOGChampionProceduralAnimator>();
    }

    private void EnsureAuron()
    {
        AOGActiveChampion marker = FindCandidateById("auron");
        if (marker == null)
        {
            GameObject obj = new GameObject("Auron_Player");
            AOGOriginalChampionModelFactory.BuildChampion(obj.transform, "auron", new Color(1f,0.56f,0.12f));
            marker = EnsureCommon(obj, "auron", "AURON", "SOLAR VANGUARD", new Color(1f,0.56f,0.12f), 1250f, 72f, 2.5f, 1.05f, 5.6f);
        }
        AOGExtraHeroSkillSet kit = marker.GetComponent<AOGExtraHeroSkillSet>();
        if (kit == null) kit = marker.gameObject.AddComponent<AOGExtraHeroSkillSet>();
        kit.heroType = AOGExtraHeroType.Auron;
    }

    private void EnsureVesper()
    {
        AOGActiveChampion marker = FindCandidateById("vesper");
        if (marker == null)
        {
            GameObject obj = new GameObject("Vesper_Player");
            AOGOriginalChampionModelFactory.BuildChampion(obj.transform, "vesper", new Color(0.12f,0.78f,0.95f));
            marker = EnsureCommon(obj, "vesper", "VESPER", "VOID ARCHER", new Color(0.12f,0.78f,0.95f), 780f, 58f, 6.2f, 0.86f, 6.7f);
        }
        AOGExtraHeroSkillSet kit = marker.GetComponent<AOGExtraHeroSkillSet>();
        if (kit == null) kit = marker.gameObject.AddComponent<AOGExtraHeroSkillSet>();
        kit.heroType = AOGExtraHeroType.Vesper;
    }

    private void EnsurePremiumMage(string id, string display, string title, AOGPremiumMageType type, Color accent)
    {
        AOGActiveChampion marker = FindCandidateById(id);
        if (marker == null)
        {
            GameObject obj = new GameObject(display + "_Player");
            AOGOriginalChampionModelFactory.BuildChampion(obj.transform, id, accent);
            float hp = id == "pyrelle" ? 860f : 790f;
            float damage = id == "pyrelle" ? 64f : 57f;
            float move = id == "nyra" ? 6.9f : 6.3f;
            float cadence = id == "nyra" ? 0.82f : 0.92f;
            marker = EnsureCommon(obj, id, display, title, accent, hp, damage, 6.2f, cadence, move);
        }
        AOGPremiumMageSkillSet kit = marker.GetComponent<AOGPremiumMageSkillSet>();
        if (kit == null) kit = marker.gameObject.AddComponent<AOGPremiumMageSkillSet>();
        kit.mageType = type;
    }

    private void EnsureSupportJungle(string id,string display,string title,AOGSupportJungleHeroType type,Color accent,float hp,float damage,float range,float cooldown,float speed)
    {
        AOGActiveChampion marker = FindCandidateById(id);
        if (marker == null)
        {
            GameObject obj = new GameObject(display + "_Player");
            AOGOriginalChampionModelFactory.BuildChampion(obj.transform,id,accent);
            marker = EnsureCommon(obj,id,display,title,accent,hp,damage,range,cooldown,speed);
        }
        AOGSupportJungleHeroKitRuntime kit = marker.GetComponent<AOGSupportJungleHeroKitRuntime>();
        if (kit == null) kit = marker.gameObject.AddComponent<AOGSupportJungleHeroKitRuntime>();
        kit.heroType = type;
    }

    private AOGActiveChampion EnsureCommon(GameObject obj, string id, string display, string title, Color accent, float hp, float damage, float range, float cooldown, float speed)
    {
        obj.name = display.Substring(0,1) + display.Substring(1).ToLowerInvariant() + "_Player";
        MoveCandidateNearBlueBase(obj.transform, id);

        AOGCharacterStats stats = obj.GetComponent<AOGCharacterStats>();
        if (stats == null) stats = obj.AddComponent<AOGCharacterStats>();
        stats.team = MinionTeam.Blue;
        stats.maxHp = hp;
        stats.hp = hp;
        stats.attackDamage = damage;
        stats.attackRange = range;
        stats.attackCooldown = cooldown;
        stats.moveSpeed = speed;

        ChampionAudioController audio = obj.GetComponent<ChampionAudioController>();
        if (audio == null) audio = obj.AddComponent<ChampionAudioController>();
        ChampionPresentationController presentation = obj.GetComponent<ChampionPresentationController>();
        if (presentation == null) presentation = obj.AddComponent<ChampionPresentationController>();
        presentation.audioController = audio;

        if (obj.GetComponent<AOGPlayerEconomy>() == null) obj.AddComponent<AOGPlayerEconomy>();
        if (obj.GetComponent<AOGChampionProgression>() == null) obj.AddComponent<AOGChampionProgression>();
        if (obj.GetComponent<AOGAutoAttackPresentationRuntime>() == null) obj.AddComponent<AOGAutoAttackPresentationRuntime>();

        CapsuleCollider capsule = obj.GetComponent<CapsuleCollider>();
        if (capsule == null) capsule = obj.AddComponent<CapsuleCollider>();
        capsule.center = new Vector3(0f,1.1f,0f); capsule.height = 2.4f; capsule.radius = 0.62f;
        Rigidbody body = obj.GetComponent<Rigidbody>();
        if (body == null) body = obj.AddComponent<Rigidbody>();
        body.isKinematic = true; body.useGravity = false;

        AOGActiveChampion marker = obj.GetComponent<AOGActiveChampion>();
        if (marker == null) marker = obj.AddComponent<AOGActiveChampion>();
        marker.championId = id;
        marker.displayName = display;
        marker.roleName = title;
        marker.accentColor = accent;
        marker.SetActiveChampion(false);
        return marker;
    }

    private void CollectCandidates()
    {
        candidates.Clear();
        string[] order = { "lyra", "kaelith", "auron", "vesper", "nyra", "pyrelle", "selene", "seris", "mireva", "dravenor", "nocthyr" };
        foreach (string id in order)
        {
            AOGActiveChampion candidate = FindCandidateById(id);
            if (candidate != null && !candidates.Contains(candidate)) candidates.Add(candidate);
        }
    }

    private void DeactivateAllCandidates()
    {
        foreach (AOGActiveChampion candidate in candidates)
        {
            if (candidate == null) continue;
            candidate.gameObject.SetActive(true);
            candidate.SetActiveChampion(false);
        }
    }

    private void BuildSelectionUI()
    {
        ClearSelectionCanvas();
        GameObject canvasObject = new GameObject("ChampionSelectCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 6000;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f,1080f);
        scaler.matchWidthOrHeight = 0.5f;

        Image shade = CreatePanel(canvas.transform, "Shade", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.003f,0.006f,0.015f,0.97f), true);
        CreateLabel(shade.transform,"Title","CHOOSE YOUR ASCENDANT",42,new Vector2(0f,470f),new Vector2(1200f,64f),new Color(0.96f,0.78f,0.34f));
        CreateLabel(shade.transform,"Subtitle","ELEVEN ORIGINAL CHAMPIONS — FIVE COMPETITIVE ROLES",17,new Vector2(0f,426f),new Vector2(1400f,34f),new Color(0.55f,0.72f,0.88f));

        for (int i = 0; i < candidates.Count; i++)
        {
            int row = i < 6 ? 0 : 1;
            int column = row == 0 ? i : i - 6;
            int count = row == 0 ? Mathf.Min(6,candidates.Count) : Mathf.Max(0,candidates.Count-6);
            float spacing = 282f;
            float x = (column - (count - 1) * 0.5f) * spacing;
            float y = row == 0 ? 145f : -180f;
            BuildCard(shade.transform, candidates[i], new Vector2(x,y));
        }
    }

    private void BuildCard(Transform parent, AOGActiveChampion champion, Vector2 position)
    {
        Image card = CreatePanel(parent,"Card_"+champion.championId,new Vector2(0.5f,0.5f),new Vector2(0.5f,0.5f),position,new Vector2(252f,276f),new Color(0.015f,0.026f,0.045f,0.99f),false);
        Outline outline = card.gameObject.AddComponent<Outline>(); outline.effectColor = champion.accentColor; outline.effectDistance = new Vector2(2f,-2f);
        Image portrait = CreatePanel(card.transform,"Portrait",new Vector2(0.5f,1f),new Vector2(0.5f,1f),new Vector2(0f,-14f),new Vector2(218f,104f),new Color(champion.accentColor.r*0.18f,champion.accentColor.g*0.18f,champion.accentColor.b*0.18f,1f),false);
        CreateLabel(portrait.transform,"Glyph",champion.displayName.Substring(0,1),50,Vector2.zero,new Vector2(190f,84f),champion.accentColor);
        CreateLabel(card.transform,"Name",champion.displayName,23,new Vector2(0f,15f),new Vector2(230f,36f),Color.white);
        CreateLabel(card.transform,"Role",champion.roleName,11,new Vector2(0f,-16f),new Vector2(230f,26f),champion.accentColor);
        CreateLabel(card.transform,"Lane",RoleFor(champion.championId).ToString().ToUpperInvariant(),11,new Vector2(0f,-42f),new Vector2(230f,24f),new Color(0.62f,0.72f,0.80f));

        GameObject buttonObject = new GameObject("Select_"+champion.championId,typeof(RectTransform),typeof(Image),typeof(Button));
        buttonObject.transform.SetParent(card.transform,false);
        RectTransform br = buttonObject.GetComponent<RectTransform>(); br.anchorMin=br.anchorMax=new Vector2(0.5f,0f); br.pivot=new Vector2(0.5f,0f); br.anchoredPosition=new Vector2(0f,14f); br.sizeDelta=new Vector2(214f,42f);
        buttonObject.GetComponent<Image>().color = new Color(champion.accentColor.r*0.50f,champion.accentColor.g*0.50f,champion.accentColor.b*0.50f,1f);
        AOGActiveChampion captured = champion;
        buttonObject.GetComponent<Button>().onClick.AddListener(()=>Select(captured));
        CreateLabel(buttonObject.transform,"Text","ENTER AS "+champion.displayName,13,Vector2.zero,new Vector2(204f,38f),Color.white);
    }

    private void Select(AOGActiveChampion champion)
    {
        if (selected || champion == null)
            return;
        selected = true;

        AOGRole role = RoleFor(champion.championId);
        AOGPlayerChampionAuthority.Instance?.RegisterPlayerChampion(champion, role);
        AOGRoleBasedTeamRuntime.EnsureAndBuildTeams(champion, role);
        AOGMatchDirector.Instance?.BeginMatch();
        StartCoroutine(FadeOut());
    }

    private IEnumerator FadeOut()
    {
        if (canvas == null) yield break;
        CanvasGroup group = canvas.gameObject.AddComponent<CanvasGroup>();
        for(float t=0f;t<0.32f;t+=Time.unscaledDeltaTime){group.alpha=1f-t/0.32f;yield return null;}
        ClearSelectionCanvas();
    }

    private static AOGRole RoleFor(string id)
    {
        if (id == "kaelith" || id == "auron") return AOGRole.Top;
        if (id == "dravenor" || id == "nocthyr") return AOGRole.Jungle;
        if (id == "vesper") return AOGRole.ADC;
        if (id == "selene" || id == "seris" || id == "mireva") return AOGRole.Support;
        return AOGRole.Mid;
    }

    private void ClearSelectionCanvas()
    {
        if (canvas != null) Destroy(canvas.gameObject);
        canvas = null;
    }

    private static AOGActiveChampion FindCandidateById(string id)
    {
        foreach (AOGActiveChampion marker in FindObjectsByType<AOGActiveChampion>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (marker != null && string.Equals(marker.championId,id,System.StringComparison.OrdinalIgnoreCase)) return marker;
        return null;
    }

    private static GameObject FindRootByToken(string token)
    {
        foreach(GameObject obj in FindObjectsByType<GameObject>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            if(obj!=null && obj.name.ToLowerInvariant().Contains(token)) return obj;
        return null;
    }

    private static void MoveCandidateNearBlueBase(Transform candidate, string id)
    {
        Transform spawn = FindNamedTransform("BluePlayerSpawn","BlueBaseSpawn","Blue_Spawn","BlueSpawn");
        if(spawn==null)return;
        int index = Mathf.Abs(id.GetHashCode()) % 11;
        candidate.position = spawn.position + new Vector3((index-5)*0.92f,0.2f,3f+(index%2)*1.1f);
    }

    private static Transform FindNamedTransform(params string[] names)
    {
        foreach(GameObject obj in FindObjectsByType<GameObject>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {
            if(obj==null)continue;
            foreach(string name in names)if(string.Equals(obj.name,name,System.StringComparison.OrdinalIgnoreCase))return obj.transform;
        }
        return null;
    }

    private Image CreatePanel(Transform parent,string name,Vector2 anchorMin,Vector2 anchorMax,Vector2 position,Vector2 size,Color color,bool stretch)
    {
        GameObject go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);RectTransform r=go.GetComponent<RectTransform>();r.anchorMin=anchorMin;r.anchorMax=anchorMax;r.anchoredPosition=position;
        if(stretch){r.offsetMin=Vector2.zero;r.offsetMax=Vector2.zero;}else r.sizeDelta=size;
        Image image=go.GetComponent<Image>();image.color=color;return image;
    }

    private Text CreateLabel(Transform parent,string name,string value,int size,Vector2 position,Vector2 dimensions,Color color)
    {
        GameObject go=new GameObject(name,typeof(RectTransform),typeof(Text));go.transform.SetParent(parent,false);RectTransform r=go.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=new Vector2(0.5f,0.5f);r.anchoredPosition=position;r.sizeDelta=dimensions;
        Text text=go.GetComponent<Text>();text.font=font;text.text=value;text.fontSize=size;text.fontStyle=FontStyle.Bold;text.alignment=TextAnchor.MiddleCenter;text.color=color;text.raycastTarget=false;return text;
    }
}
