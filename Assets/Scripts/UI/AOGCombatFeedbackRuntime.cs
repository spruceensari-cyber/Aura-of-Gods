using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(1700)]
public class AOGCombatFeedbackRuntime : MonoBehaviour
{
    private class TrackedHealth
    {
        public Object target;
        public Transform transform;
        public System.Func<float> hpGetter;
        public float lastHp;
        public Vector3 offset;
    }

    private Canvas canvas;
    private RectTransform canvasRect;
    private Font font;
    private float nextScan;
    private readonly List<TrackedHealth> tracked = new List<TrackedHealth>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        GameObject host = new GameObject("AOG_Combat_Feedback_Runtime");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGCombatFeedbackRuntime>();
    }

    private void Awake()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        GameObject go = new GameObject("CombatFeedbackCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        go.transform.SetParent(transform, false);
        canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 2600;
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
            nextScan = Time.unscaledTime + 0.5f;
            Scan();
        }

        for (int i = tracked.Count - 1; i >= 0; i--)
        {
            TrackedHealth t = tracked[i];
            if (t.target == null || t.transform == null)
            {
                tracked.RemoveAt(i);
                continue;
            }

            float hp = t.hpGetter();
            float delta = t.lastHp - hp;
            if (delta >= 1f)
                SpawnDamageNumber(t.transform.position + t.offset, Mathf.RoundToInt(delta));
            t.lastHp = hp;
        }
    }

    private void Scan()
    {
        foreach (AOGCharacterStats hero in FindObjectsByType<AOGCharacterStats>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (hero == null || IsTracked(hero)) continue;
            tracked.Add(new TrackedHealth { target = hero, transform = hero.transform, hpGetter = () => hero.hp, lastHp = hero.hp, offset = new Vector3(0f, 3.6f, 0f) });
        }

        foreach (TowerHealth tower in FindObjectsByType<TowerHealth>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (tower == null || IsTracked(tower)) continue;
            tracked.Add(new TrackedHealth { target = tower, transform = tower.transform, hpGetter = () => tower.hp, lastHp = tower.hp, offset = new Vector3(0f, 7.2f, 0f) });
        }

        foreach (AOGNeutralBossAI boss in FindObjectsByType<AOGNeutralBossAI>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (boss == null || IsTracked(boss)) continue;
            tracked.Add(new TrackedHealth { target = boss, transform = boss.transform, hpGetter = () => boss.hp, lastHp = boss.hp, offset = new Vector3(0f, 6f, 0f) });
        }
    }

    private bool IsTracked(Object target)
    {
        foreach (TrackedHealth t in tracked) if (t.target == target) return true;
        return false;
    }

    private void SpawnDamageNumber(Vector3 worldPosition, int damage)
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        Vector3 screen = cam.WorldToScreenPoint(worldPosition);
        if (screen.z <= 0f) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, new Vector2(screen.x, screen.y), null, out Vector2 local);
        GameObject go = new GameObject("Damage_" + damage, typeof(RectTransform), typeof(Text), typeof(CanvasGroup));
        go.transform.SetParent(canvas.transform, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchoredPosition = local + new Vector2(Random.Range(-16f, 16f), Random.Range(-4f, 16f));
        rect.sizeDelta = new Vector2(150f, 48f);

        Text text = go.GetComponent<Text>();
        text.font = font;
        text.fontSize = damage >= 250 ? 30 : 23;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = damage >= 250 ? new Color(1f, 0.58f, 0.12f) : Color.white;
        text.text = "-" + damage;
        text.raycastTarget = false;

        go.AddComponent<AOGFloatingCombatText>();
    }
}

public class AOGFloatingCombatText : MonoBehaviour
{
    private RectTransform rect;
    private CanvasGroup group;
    private float life = 0.75f;
    private float elapsed;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        group = GetComponent<CanvasGroup>();
    }

    private void Update()
    {
        elapsed += Time.unscaledDeltaTime;
        rect.anchoredPosition += Vector2.up * 52f * Time.unscaledDeltaTime;
        group.alpha = 1f - Mathf.Clamp01(elapsed / life);
        if (elapsed >= life) Destroy(gameObject);
    }
}
