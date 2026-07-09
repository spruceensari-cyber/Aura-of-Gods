using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DefaultExecutionOrder(1600)]
public class AOGScreenSpaceCombatBars : MonoBehaviour
{
    private class BarEntry
    {
        public Object target;
        public Transform worldTransform;
        public RectTransform root;
        public Image fill;
        public Text label;
        public System.Func<float> ratio;
        public System.Func<bool> visible;
        public Vector3 worldOffset;
        public Vector2 size;
    }

    private static AOGScreenSpaceCombatBars instance;
    private Canvas canvas;
    private RectTransform canvasRect;
    private Font font;
    private readonly List<BarEntry> entries = new List<BarEntry>();
    private float nextScan;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Ensure();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Ensure();
        if (instance != null) instance.Rebuild();
    }

    private static void Ensure()
    {
        if (instance != null) return;
        GameObject host = new GameObject("AOG_ScreenSpace_Combat_Bars");
        DontDestroyOnLoad(host);
        instance = host.AddComponent<AOGScreenSpaceCombatBars>();
    }

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        BuildCanvas();
    }

    private void BuildCanvas()
    {
        GameObject go = new GameObject("CombatBarsCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        go.transform.SetParent(transform, false);
        canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 2200;
        CanvasScaler scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasRect = go.GetComponent<RectTransform>();
    }

    private void Update()
    {
        if (Time.unscaledTime >= nextScan)
        {
            nextScan = Time.unscaledTime + 0.75f;
            ScanTargets();
        }
        UpdateBars();
    }

    private void Rebuild()
    {
        foreach (BarEntry e in entries) if (e.root != null) Destroy(e.root.gameObject);
        entries.Clear();
        nextScan = 0f;
    }

    private void ScanTargets()
    {
        foreach (TowerHealth tower in FindObjectsByType<TowerHealth>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (tower == null || HasEntry(tower)) continue;
            AddBar(tower, tower.transform, () => Mathf.Clamp01(tower.hp / Mathf.Max(1f, tower.maxHp)), () => tower != null && tower.gameObject.activeInHierarchy && tower.hp > 0f,
                new Vector3(0f, 7.8f, 0f), new Vector2(260f, 24f), tower.name.Replace('_', ' ').ToUpperInvariant(), TeamColor(tower.towerTeam));
        }

        foreach (AOGCharacterStats hero in FindObjectsByType<AOGCharacterStats>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (hero == null || HasEntry(hero)) continue;
            AddBar(hero, hero.transform, () => Mathf.Clamp01(hero.hp / Mathf.Max(1f, hero.maxHp)), () => hero != null && hero.gameObject.activeInHierarchy && !hero.IsDead,
                new Vector3(0f, 3.25f, 0f), new Vector2(118f, 11f), string.Empty, hero.team == MinionTeam.Blue ? new Color(0.20f, 0.92f, 0.40f) : new Color(0.98f, 0.24f, 0.26f));

            AOGWorldHealthBar legacy = hero.GetComponent<AOGWorldHealthBar>();
            if (legacy != null) legacy.Hide();
        }

        foreach (AOGNeutralBossAI boss in FindObjectsByType<AOGNeutralBossAI>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (boss == null || HasEntry(boss)) continue;
            AddBar(boss, boss.transform, () => Mathf.Clamp01(boss.hp / Mathf.Max(1f, boss.maxHp)), () => boss != null && boss.gameObject.activeInHierarchy && !boss.IsDead,
                new Vector3(0f, boss.bossType == AOGNeutralBossType.Dragon ? 8.5f : 6.8f, 0f), new Vector2(300f, 20f), boss.bossType.ToString().ToUpperInvariant(), new Color(0.72f, 0.28f, 0.96f));
        }
    }

    private bool HasEntry(Object target)
    {
        for (int i = 0; i < entries.Count; i++) if (entries[i].target == target) return true;
        return false;
    }

    private void AddBar(Object target, Transform world, System.Func<float> ratio, System.Func<bool> visible, Vector3 offset, Vector2 size, string labelText, Color fillColor)
    {
        GameObject rootGo = new GameObject("Bar_" + target.name, typeof(RectTransform), typeof(Image));
        rootGo.transform.SetParent(canvas.transform, false);
        RectTransform root = rootGo.GetComponent<RectTransform>();
        root.sizeDelta = size;
        rootGo.GetComponent<Image>().color = new Color(0.008f, 0.012f, 0.018f, 0.96f);

        Outline outline = rootGo.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.95f);
        outline.effectDistance = new Vector2(2f, -2f);

        GameObject fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillGo.transform.SetParent(root, false);
        RectTransform fillRect = fillGo.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(1f, 1f);
        fillRect.offsetMin = new Vector2(3f, 3f);
        fillRect.offsetMax = new Vector2(-3f, -3f);
        Image fill = fillGo.GetComponent<Image>();
        fill.color = fillColor;
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = 0;

        Text label = null;
        if (!string.IsNullOrEmpty(labelText))
        {
            GameObject labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelGo.transform.SetParent(root, false);
            RectTransform lr = labelGo.GetComponent<RectTransform>();
            lr.anchorMin = new Vector2(0.5f, 1f);
            lr.anchorMax = new Vector2(0.5f, 1f);
            lr.pivot = new Vector2(0.5f, 0f);
            lr.anchoredPosition = new Vector2(0f, 4f);
            lr.sizeDelta = new Vector2(size.x + 80f, 24f);
            label = labelGo.GetComponent<Text>();
            label.font = font;
            label.fontSize = 13;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.text = labelText;
            label.raycastTarget = false;
        }

        entries.Add(new BarEntry { target = target, worldTransform = world, root = root, fill = fill, label = label, ratio = ratio, visible = visible, worldOffset = offset, size = size });
    }

    private void UpdateBars()
    {
        Camera cam = Camera.main;
        if (cam == null || canvasRect == null) return;

        for (int i = entries.Count - 1; i >= 0; i--)
        {
            BarEntry e = entries[i];
            if (e.target == null || e.worldTransform == null)
            {
                if (e.root != null) Destroy(e.root.gameObject);
                entries.RemoveAt(i);
                continue;
            }

            bool show = e.visible == null || e.visible();
            Vector3 screen = cam.WorldToScreenPoint(e.worldTransform.position + e.worldOffset);
            show &= screen.z > 0f && screen.x > -100f && screen.x < Screen.width + 100f && screen.y > -100f && screen.y < Screen.height + 100f;
            e.root.gameObject.SetActive(show);
            if (!show) continue;

            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, new Vector2(screen.x, screen.y), null, out Vector2 local);
            e.root.anchoredPosition = local;
            e.fill.fillAmount = Mathf.Clamp01(e.ratio());
        }
    }

    private static Color TeamColor(MinionTeam team)
    {
        return team == MinionTeam.Blue ? new Color(0.15f, 0.58f, 1f) : new Color(1f, 0.18f, 0.22f);
    }
}
