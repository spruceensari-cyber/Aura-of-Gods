using UnityEngine;
using UnityEngine.UI;

public class AOGCooldownHUD : MonoBehaviour
{
    private const float SlotWidth = 74f;
    private const float SlotHeight = 58f;

    [Header("Layout")]
    public Vector2 anchoredPosition = new Vector2(0f, 30f);
    public Color panelColor = new Color(0.03f, 0.04f, 0.05f, 0.78f);
    public Color readyColor = new Color(0.25f, 0.9f, 1f, 1f);
    public Color cooldownColor = new Color(0f, 0f, 0f, 0.62f);
    public Color textColor = Color.white;

    private CanvasGroup canvasGroup;
    private Text titleText;
    private Slot[] slots;
    private Font hudFont;

    private GameObject currentTarget;
    private RagnarSkillSet ragnar;
    private LyraSkillSet lyra;
    private float nextTargetSearchTime;

    private class Slot
    {
        public RectTransform root;
        public RectTransform cooldownFill;
        public Image border;
        public Text keyText;
        public Text statusText;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (FindAnyObjectByType<AOGCooldownHUD>() != null)
            return;

        GameObject hudObject = new GameObject("AOG_Cooldown_HUD");
        DontDestroyOnLoad(hudObject);
        hudObject.AddComponent<AOGCooldownHUD>();
    }

    private void Awake()
    {
        hudFont = GetBuiltinFont();
        BuildUI();
    }

    private void Update()
    {
        if (Time.time >= nextTargetSearchTime || currentTarget == null || !currentTarget.activeInHierarchy)
        {
            nextTargetSearchTime = Time.time + 0.5f;
            FindCurrentTarget();
        }

        if (ragnar != null && ragnar.enabled)
        {
            UpdateRagnarHUD();
            return;
        }

        if (lyra != null && lyra.enabled)
        {
            UpdateLyraHUD();
            return;
        }

        SetVisible(false);
    }

    private void FindCurrentTarget()
    {
        currentTarget = null;
        ragnar = null;
        lyra = null;

        AOGLocalPlayerAuthority[] authorities = FindObjectsByType<AOGLocalPlayerAuthority>(FindObjectsInactive.Exclude);

        foreach (AOGLocalPlayerAuthority authority in authorities)
        {
            if (authority == null || !authority.isLocalPlayer)
                continue;

            BindTarget(authority.gameObject);
            return;
        }

        AOGPlayerMOBAController[] controllers = FindObjectsByType<AOGPlayerMOBAController>(FindObjectsInactive.Exclude);

        foreach (AOGPlayerMOBAController controller in controllers)
        {
            if (controller == null || !controller.enabled)
                continue;

            BindTarget(controller.gameObject);
            return;
        }
    }

    private void BindTarget(GameObject target)
    {
        currentTarget = target;
        ragnar = target.GetComponent<RagnarSkillSet>();
        lyra = target.GetComponent<LyraSkillSet>();
    }

    private void UpdateRagnarHUD()
    {
        SetVisible(true);
        titleText.text = currentTarget != null ? currentTarget.name + " / Ragnar" : "Ragnar";

        SetSlot(slots[0], "Q", ragnar.GetQCooldownRatio(), ragnar.qCooldown, ragnar.IsQReady() ? "READY" : null);
        SetSlot(slots[1], "W", ragnar.GetWCooldownRatio(), ragnar.wCooldown, ragnar.IsVolcanicSkinActive() ? "BUFF" : null);
        SetSlot(slots[2], "E", ragnar.GetECooldownRatio(), ragnar.eCooldown, ragnar.IsEReady() ? "READY" : null);
        SetSlot(slots[3], "R", ragnar.GetRCooldownRatio(), ragnar.rCooldown, ragnar.IsRReady() ? "READY" : null);
    }

    private void UpdateLyraHUD()
    {
        SetVisible(true);
        titleText.text = currentTarget != null ? currentTarget.name + " / Lyra" : "Lyra";

        SetSlot(slots[0], "Q", lyra.GetQCooldownRatio(), lyra.qCooldown, null);
        SetSlot(slots[1], "W", lyra.GetWCooldownRatio(), lyra.wCooldown, lyra.IsVanished ? "VANISH" : null);
        SetSlot(slots[2], "E", lyra.GetECooldownRatio(), lyra.eCooldown, null);
        SetSlot(slots[3], "R", lyra.GetRCooldownRatio(), lyra.rCooldown, null);
    }

    private void SetSlot(Slot slot, string key, float cooldownRatio, float cooldownDuration, string overrideStatus)
    {
        float ratio = Mathf.Clamp01(cooldownRatio);
        slot.keyText.text = key;
        slot.cooldownFill.sizeDelta = new Vector2(SlotWidth * ratio, 0f);

        if (!string.IsNullOrEmpty(overrideStatus))
        {
            slot.statusText.text = overrideStatus;
        }
        else if (ratio <= 0.001f)
        {
            slot.statusText.text = "READY";
        }
        else
        {
            float seconds = Mathf.Max(0f, ratio * cooldownDuration);
            slot.statusText.text = Mathf.CeilToInt(seconds).ToString();
        }

        slot.border.color = ratio <= 0.001f ? readyColor : new Color(0.18f, 0.2f, 0.23f, 0.92f);
    }

    private void SetVisible(bool visible)
    {
        canvasGroup.alpha = visible ? 1f : 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    private void BuildUI()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 80;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        gameObject.AddComponent<GraphicRaycaster>();
        canvasGroup = gameObject.AddComponent<CanvasGroup>();

        GameObject panel = CreateUIObject("Panel", transform);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0f);
        panelRect.anchorMax = new Vector2(0.5f, 0f);
        panelRect.pivot = new Vector2(0.5f, 0f);
        panelRect.anchoredPosition = anchoredPosition;
        panelRect.sizeDelta = new Vector2(360f, 84f);

        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = panelColor;

        titleText = CreateText("Title", panel.transform, 14, FontStyle.Bold, TextAnchor.MiddleCenter);
        RectTransform titleRect = titleText.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -4f);
        titleRect.sizeDelta = new Vector2(-16f, 20f);

        GameObject row = CreateUIObject("Slots", panel.transform);
        RectTransform rowRect = row.GetComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0.5f, 0f);
        rowRect.anchorMax = new Vector2(0.5f, 0f);
        rowRect.pivot = new Vector2(0.5f, 0f);
        rowRect.anchoredPosition = new Vector2(0f, 8f);
        rowRect.sizeDelta = new Vector2(328f, SlotHeight);

        HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 8f;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        slots = new Slot[4];

        for (int i = 0; i < slots.Length; i++)
            slots[i] = CreateSlot(row.transform);

        SetVisible(false);
    }

    private Slot CreateSlot(Transform parent)
    {
        GameObject root = CreateUIObject("AbilitySlot", parent);
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(SlotWidth, SlotHeight);

        LayoutElement layout = root.AddComponent<LayoutElement>();
        layout.preferredWidth = SlotWidth;
        layout.preferredHeight = SlotHeight;

        Image border = root.AddComponent<Image>();
        border.color = new Color(0.18f, 0.2f, 0.23f, 0.92f);

        GameObject fillObject = CreateUIObject("CooldownFill", root.transform);
        RectTransform fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(0f, 1f);
        fillRect.pivot = new Vector2(0f, 0.5f);
        fillRect.anchoredPosition = Vector2.zero;
        fillRect.sizeDelta = Vector2.zero;

        Image fill = fillObject.AddComponent<Image>();
        fill.color = cooldownColor;

        Text keyText = CreateText("Key", root.transform, 24, FontStyle.Bold, TextAnchor.MiddleCenter);
        RectTransform keyRect = keyText.GetComponent<RectTransform>();
        keyRect.anchorMin = new Vector2(0f, 0.22f);
        keyRect.anchorMax = new Vector2(1f, 1f);
        keyRect.offsetMin = Vector2.zero;
        keyRect.offsetMax = Vector2.zero;

        Text statusText = CreateText("Status", root.transform, 12, FontStyle.Bold, TextAnchor.MiddleCenter);
        RectTransform statusRect = statusText.GetComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0f, 0f);
        statusRect.anchorMax = new Vector2(1f, 0.32f);
        statusRect.offsetMin = Vector2.zero;
        statusRect.offsetMax = Vector2.zero;

        return new Slot
        {
            root = rect,
            cooldownFill = fillRect,
            border = border,
            keyText = keyText,
            statusText = statusText
        };
    }

    private Text CreateText(string objectName, Transform parent, int fontSize, FontStyle fontStyle, TextAnchor alignment)
    {
        GameObject obj = CreateUIObject(objectName, parent);
        Text text = obj.AddComponent<Text>();
        text.font = hudFont;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = alignment;
        text.color = textColor;
        text.raycastTarget = false;
        return text;
    }

    private GameObject CreateUIObject(string objectName, Transform parent)
    {
        GameObject obj = new GameObject(objectName, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        return obj;
    }

    private Font GetBuiltinFont()
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        if (font == null)
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        return font;
    }
}

public class AOGFloatingCombatText : MonoBehaviour
{
    public float lifetime = 0.85f;
    public float riseSpeed = 1.9f;
    public float sideDrift = 0.35f;
    public float startScale = 1.15f;
    public float endScale = 0.85f;

    private TextMesh textMesh;
    private Color startColor;
    private Vector3 velocity;
    private float birthTime;

    public static void SpawnDamage(Vector3 worldPosition, float amount, Color color)
    {
        if (amount <= 0f)
            return;

        GameObject obj = new GameObject("AOG_Damage_Text");
        obj.transform.position = worldPosition + Vector3.up * 1.7f + Random.insideUnitSphere * 0.22f;
        obj.transform.localScale = Vector3.one * 0.9f;

        TextMesh mesh = obj.AddComponent<TextMesh>();
        mesh.text = Mathf.CeilToInt(amount).ToString();
        mesh.anchor = TextAnchor.MiddleCenter;
        mesh.alignment = TextAlignment.Center;
        mesh.fontSize = 48;
        mesh.characterSize = 0.08f;
        mesh.color = color;

        AOGFloatingCombatText text = obj.AddComponent<AOGFloatingCombatText>();
        text.Initialize(mesh, color);
    }

    public static void SpawnHeal(Vector3 worldPosition, float amount)
    {
        SpawnDamage(worldPosition, amount, new Color(0.25f, 1f, 0.45f, 1f));
    }

    private void Initialize(TextMesh mesh, Color color)
    {
        textMesh = mesh;
        startColor = color;
        birthTime = Time.time;

        velocity = new Vector3(
            Random.Range(-sideDrift, sideDrift),
            riseSpeed,
            Random.Range(-sideDrift, sideDrift)
        );
    }

    private void Update()
    {
        float age = Time.time - birthTime;
        float t = Mathf.Clamp01(age / Mathf.Max(0.01f, lifetime));

        transform.position += velocity * Time.deltaTime;
        transform.localScale = Vector3.one * Mathf.Lerp(startScale, endScale, t);

        Camera cam = Camera.main;

        if (cam != null)
        {
            Vector3 direction = transform.position - cam.transform.position;

            if (direction.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(direction);
        }

        if (textMesh != null)
        {
            Color color = startColor;
            color.a = 1f - t;
            textMesh.color = color;
        }

        if (age >= lifetime)
            Destroy(gameObject);
    }
}
