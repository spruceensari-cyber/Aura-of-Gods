using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

public class AOGActiveChampion : MonoBehaviour
{
    public static AOGActiveChampion Current { get; private set; }

    public string championId = "unknown";
    public string displayName = "UNKNOWN";
    public string roleName = "CHAMPION";
    public Color accentColor = Color.white;
    public bool IsActiveChampion { get; private set; }

    public void SetActiveChampion(bool active)
    {
        IsActiveChampion = active;
        if (active)
            Current = this;

        AOGUnifiedMobaInputDriver input = GetComponent<AOGUnifiedMobaInputDriver>();
        if (active)
        {
            if (input == null)
                input = gameObject.AddComponent<AOGUnifiedMobaInputDriver>();
            input.enabled = true;
        }
        else if (input != null)
        {
            input.enabled = false;
        }

        AOGPlayerMOBAController legacyMoba = GetComponent<AOGPlayerMOBAController>();
        if (legacyMoba != null)
            legacyMoba.enabled = false;

        foreach (Collider col in GetComponentsInChildren<Collider>(true))
            if (col != null) col.enabled = active;

        foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
            if (renderer != null) renderer.enabled = active;

        if (!active)
            return;

        Camera camera = Camera.main;
        if (camera != null)
            camera.GetComponent<AOGMobaCameraController>()?.SetTarget(transform, true);

        AOGPlayerEconomy economy = GetComponent<AOGPlayerEconomy>();
        if (economy == null)
            economy = gameObject.AddComponent<AOGPlayerEconomy>();
        AOGShopRuntime.Instance?.Bind(economy);
    }
}

[DefaultExecutionOrder(-850)]
public class AOGChampionSelectionRuntime : MonoBehaviour
{
    public static AOGChampionSelectionRuntime Instance { get; private set; }

    private Canvas canvas;
    private Font font;
    private AOGActiveChampion lyra;
    private AOGActiveChampion kaelith;
    private bool selectionMade;
    private bool setupStarted;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        EnsureInstance();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureInstance();
        if (Instance != null)
            Instance.StartSetup();
    }

    private static void EnsureInstance()
    {
        if (Instance != null)
            return;

        AOGChampionSelectionRuntime existing = FindFirstObjectByType<AOGChampionSelectionRuntime>();
        if (existing != null)
        {
            Instance = existing;
            return;
        }

        GameObject host = new GameObject("AOG_Champion_Selection_Runtime");
        Instance = host.AddComponent<AOGChampionSelectionRuntime>();
        DontDestroyOnLoad(host);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        EnsureEventSystem();
    }

    private void Start()
    {
        StartSetup();
    }

    private void StartSetup()
    {
        if (setupStarted || selectionMade)
            return;

        setupStarted = true;
        StartCoroutine(SetupSelection());
    }

    private IEnumerator SetupSelection()
    {
        for (int attempt = 0; attempt < 35; attempt++)
        {
            PrepareChampions();
            if (lyra != null && kaelith != null)
                break;
            yield return new WaitForSecondsRealtime(0.15f);
        }

        if (lyra == null && kaelith == null)
        {
            setupStarted = false;
            yield break;
        }

        lyra?.SetActiveChampion(false);
        kaelith?.SetActiveChampion(false);
        BuildSelectionUI();
    }

    private void PrepareChampions()
    {
        if (lyra == null)
        {
            GameObject lyraObject = FindRootByNameContains("lyra_player") ?? FindRootByNameContains("lyra");
            if (lyraObject != null)
            {
                lyra = EnsureChampion(lyraObject, "lyra", "LYRA", "MOON HUNTRESS", new Color(0.62f, 0.28f, 0.92f, 1f));
                if (lyraObject.GetComponent<LyraSkillSet>() == null)
                    lyraObject.AddComponent<LyraSkillSet>();
            }
        }

        if (kaelith == null)
        {
            GameObject kaelithObject = FindRootByNameContains("kaelith_player");
            if (kaelithObject == null)
            {
                kaelithObject = new GameObject("Kaelith_Player");
                Transform blueSpawn = FindTransformByNames("BlueSpawn", "Blue_Spawn", "BlueBaseSpawn");
                if (blueSpawn != null)
                    kaelithObject.transform.position = blueSpawn.position + new Vector3(2.5f, 0.2f, 2.5f);
            }

            EnsureKaelithVisual(kaelithObject);
            kaelith = EnsureChampion(kaelithObject, "kaelith", "KAELITH", "ECLIPSE REAVER", new Color(0.36f, 0.18f, 0.86f, 1f));

            if (kaelithObject.GetComponent<KaelithEclipseSkillSet>() == null)
                kaelithObject.AddComponent<KaelithEclipseSkillSet>();
            if (kaelithObject.GetComponent<AOGChampionProceduralAnimator>() == null)
                kaelithObject.AddComponent<AOGChampionProceduralAnimator>();
        }
    }

    private AOGActiveChampion EnsureChampion(GameObject obj, string id, string display, string role, Color accent)
    {
        AOGCharacterStats stats = obj.GetComponent<AOGCharacterStats>();
        if (stats == null) stats = obj.AddComponent<AOGCharacterStats>();
        stats.team = MinionTeam.Blue;
        stats.maxHp = id == "kaelith" ? 980f : Mathf.Max(stats.maxHp, 900f);
        stats.hp = stats.maxHp;
        stats.moveSpeed = id == "kaelith" ? 6.25f : Mathf.Max(stats.moveSpeed, 6f);
        stats.attackDamage = id == "kaelith" ? 64f : Mathf.Max(stats.attackDamage, 55f);
        stats.attackRange = id == "kaelith" ? 3.2f : Mathf.Max(stats.attackRange, 4.5f);
        stats.attackCooldown = id == "kaelith" ? 0.92f : stats.attackCooldown;

        ChampionAudioController audio = obj.GetComponent<ChampionAudioController>();
        if (audio == null) audio = obj.AddComponent<ChampionAudioController>();

        ChampionPresentationController presentation = obj.GetComponent<ChampionPresentationController>();
        if (presentation == null) presentation = obj.AddComponent<ChampionPresentationController>();
        presentation.audioController = audio;
        if (presentation.animator == null)
            presentation.animator = obj.GetComponentInChildren<Animator>(true);

        AOGPlayerMOBAController moba = obj.GetComponent<AOGPlayerMOBAController>();
        if (moba == null) moba = obj.AddComponent<AOGPlayerMOBAController>();
        moba.presentation = presentation;
        moba.enabled = false;

        CapsuleCollider capsule = obj.GetComponent<CapsuleCollider>();
        if (capsule == null) capsule = obj.AddComponent<CapsuleCollider>();
        capsule.center = new Vector3(0f, 1.1f, 0f);
        capsule.height = 2.4f;
        capsule.radius = 0.65f;

        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb == null) rb = obj.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true;

        AOGWorldHealthBar hpBar = obj.GetComponent<AOGWorldHealthBar>();
        if (hpBar == null) hpBar = obj.AddComponent<AOGWorldHealthBar>();
        hpBar.barOffset = new Vector3(0f, 3.2f, 0f);
        hpBar.barWidth = 2.5f;
        hpBar.barHeight = 0.20f;

        if (obj.GetComponent<AOGPlayerEconomy>() == null)
            obj.AddComponent<AOGPlayerEconomy>();
        if (obj.GetComponent<AOGChampionProgression>() == null)
            obj.AddComponent<AOGChampionProgression>();

        AOGActiveChampion marker = obj.GetComponent<AOGActiveChampion>();
        if (marker == null) marker = obj.AddComponent<AOGActiveChampion>();
        marker.championId = id;
        marker.displayName = display;
        marker.roleName = role;
        marker.accentColor = accent;
        return marker;
    }

    private void EnsureKaelithVisual(GameObject player)
    {
        if (player.GetComponentInChildren<Renderer>(true) != null)
            return;

        GameObject source = FindBestKaelithModel();
        if (source != null)
        {
            GameObject clone = Instantiate(source, player.transform);
            clone.name = "Kaelith_Eclipse_Reaver_Visual";
            clone.transform.localPosition = Vector3.zero;
            clone.transform.localRotation = Quaternion.identity;
            clone.transform.localScale = Vector3.one;
            foreach (Collider collider in clone.GetComponentsInChildren<Collider>(true))
                Destroy(collider);
            return;
        }

        AOGChampionVisualFactory.BuildKaelithVisual(player.transform);
    }

    private void BuildSelectionUI()
    {
        if (canvas != null)
            Destroy(canvas.gameObject);

        GameObject canvasObject = new GameObject("ChampionSelectCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 4000;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform shade = Panel("Shade", canvas.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.004f, 0.008f, 0.018f, 0.94f), true);
        Label("Title", shade, "CHOOSE YOUR ASCENDANT", 48, TextAnchor.MiddleCenter, new Vector2(0f, 380f), new Vector2(1200f, 80f), new Color(0.90f, 0.74f, 0.36f));
        Label("Subtitle", shade, "TWO CHAMPIONS — TWO DIFFERENT COMBAT IDENTITIES", 18, TextAnchor.MiddleCenter, new Vector2(0f, 325f), new Vector2(1100f, 42f), new Color(0.52f, 0.66f, 0.78f));

        if (lyra != null) BuildChampionCard(shade, lyra, new Vector2(-285f, 0f));
        if (kaelith != null) BuildChampionCard(shade, kaelith, new Vector2(285f, 0f));

        Label("Hint", shade, "Lyra stays in the roster. Kaelith begins the new melee-combo phase.", 17, TextAnchor.MiddleCenter, new Vector2(0f, -370f), new Vector2(1200f, 50f), new Color(0.58f, 0.70f, 0.80f));
    }

    private void BuildChampionCard(RectTransform parent, AOGActiveChampion champion, Vector2 position)
    {
        RectTransform card = Panel("ChampionCard_" + champion.championId, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, new Vector2(460f, 610f), new Color(0.018f, 0.035f, 0.055f, 0.985f), false);
        Outline outline = card.gameObject.AddComponent<Outline>();
        outline.effectColor = champion.accentColor;
        outline.effectDistance = new Vector2(3f, -3f);

        RectTransform portrait = Panel("Portrait", card, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(390f, 330f), new Color(champion.accentColor.r * 0.18f, champion.accentColor.g * 0.18f, champion.accentColor.b * 0.18f, 1f), false);
        portrait.GetComponent<Image>().sprite = BuildPortraitSprite(champion.accentColor, champion.championId == "kaelith" ? 1 : 0);

        Label("Name", card, champion.displayName, 40, TextAnchor.MiddleCenter, new Vector2(0f, -54f), new Vector2(400f, 60f), Color.white);
        Label("Role", card, champion.roleName, 17, TextAnchor.MiddleCenter, new Vector2(0f, -103f), new Vector2(400f, 36f), champion.accentColor);

        string kit = champion.championId == "kaelith"
            ? "Q  VOID LANCE\nW  ECLIPSE DOMAIN\nE  RIFT DASH\nR  TOTAL ECLIPSE"
            : "Q  NEON DAGGER\nW  VANISH STEP\nE  HUNTER'S NET\nR  BLOOD MOON";
        Label("Kit", card, kit, 18, TextAnchor.UpperLeft, new Vector2(0f, -190f), new Vector2(330f, 150f), new Color(0.72f, 0.82f, 0.90f));

        GameObject buttonObject = new GameObject("SelectButton", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(card, false);
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0f);
        buttonRect.anchorMax = new Vector2(0.5f, 0f);
        buttonRect.pivot = new Vector2(0.5f, 0f);
        buttonRect.anchoredPosition = new Vector2(0f, 28f);
        buttonRect.sizeDelta = new Vector2(340f, 64f);
        buttonObject.GetComponent<Image>().color = new Color(champion.accentColor.r * 0.46f, champion.accentColor.g * 0.46f, champion.accentColor.b * 0.46f, 1f);
        AOGActiveChampion captured = champion;
        buttonObject.GetComponent<Button>().onClick.AddListener(() => SelectChampion(captured));
        Label("Text", buttonRect, champion.championId == "kaelith" ? "ENTER AS KAELITH" : "ENTER AS LYRA", 21, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(320f, 56f), Color.white);
    }

    private void SelectChampion(AOGActiveChampion selected)
    {
        if (selectionMade || selected == null)
            return;

        selectionMade = true;

        if (lyra != null)
        {
            bool active = lyra == selected;
            lyra.SetActiveChampion(active);
            if (!active) lyra.gameObject.SetActive(false);
        }

        if (kaelith != null)
        {
            bool active = kaelith == selected;
            kaelith.SetActiveChampion(active);
            if (!active) kaelith.gameObject.SetActive(false);
        }

        selected.gameObject.SetActive(true);
        selected.SetActiveChampion(true);
        FindFirstObjectByType<AOGDynamicChampionHudBinder>()?.Bind(selected);
        AOGMatchDirector.Instance?.BeginMatch();
        StartCoroutine(FadeOutSelection());
    }

    private IEnumerator FadeOutSelection()
    {
        if (canvas == null)
            yield break;

        CanvasGroup group = canvas.gameObject.AddComponent<CanvasGroup>();
        for (float t = 0f; t < 0.45f; t += Time.unscaledDeltaTime)
        {
            group.alpha = 1f - t / 0.45f;
            yield return null;
        }

        Destroy(canvas.gameObject);
        canvas = null;
    }

    private Sprite BuildPortraitSprite(Color accent, int seed)
    {
        const int width = 256;
        const int height = 220;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float u = x / (float)(width - 1);
                float v = y / (float)(height - 1);
                float radial = Mathf.Clamp01(1f - Vector2.Distance(new Vector2(u, v), new Vector2(0.5f, 0.48f)) * 1.45f);
                float nebula = Mathf.PerlinNoise(u * 4.2f + seed * 11f, v * 4.2f + seed * 7f);
                Color color = Color.Lerp(new Color(0.01f, 0.018f, 0.035f, 1f), accent * 0.72f, radial * (0.45f + nebula * 0.55f));
                bool silhouette = Mathf.Abs(u - 0.5f) < 0.10f + v * 0.12f && v > 0.12f && v < 0.83f;
                if (silhouette)
                    color = Color.Lerp(color, new Color(0.015f, 0.018f, 0.03f, 1f), 0.86f);
                pixels[y * width + x] = color;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f);
    }

    private static GameObject FindBestKaelithModel()
    {
        foreach (GameObject obj in FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (obj == null) continue;
            string lower = obj.name.ToLowerInvariant();
            if (lower.Contains("kaelith") && lower.Contains("model") && obj.GetComponentInChildren<Renderer>(true) != null)
                return obj;
        }
        return null;
    }

    private static GameObject FindRootByNameContains(string token)
    {
        foreach (GameObject obj in FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (obj != null && obj.name.ToLowerInvariant().Contains(token)) return obj;
        return null;
    }

    private static Transform FindTransformByNames(params string[] names)
    {
        foreach (GameObject obj in FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (obj == null) continue;
            foreach (string candidate in names)
                if (string.Equals(obj.name, candidate, System.StringComparison.OrdinalIgnoreCase)) return obj.transform;
        }
        return null;
    }

    private void EnsureEventSystem()
    {
        if (EventSystem.current != null)
            return;

        GameObject eventSystem = new GameObject("AOG_ChampionSelect_EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
        eventSystem.AddComponent<InputSystemUIInputModule>();
#else
        eventSystem.AddComponent<StandaloneInputModule>();
#endif
        DontDestroyOnLoad(eventSystem);
    }

    private RectTransform Panel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size, Color color, bool stretch)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = stretch ? new Vector2(0.5f, 0.5f) : anchorMin;
        rect.anchoredPosition = position;
        if (stretch)
        {
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
        else
        {
            rect.sizeDelta = size;
        }
        go.GetComponent<Image>().color = color;
        return rect;
    }

    private Text Label(string name, Transform parent, string value, int size, TextAnchor alignment, Vector2 position, Vector2 dimensions, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = dimensions;

        Text text = go.GetComponent<Text>();
        text.font = font;
        text.text = value;
        text.fontSize = size;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }
}
