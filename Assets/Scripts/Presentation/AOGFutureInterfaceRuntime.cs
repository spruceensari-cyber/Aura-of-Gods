using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Lightweight future-mythic UI framing layer that complements the gameplay and tournament HUDs.
/// </summary>
public class AOGFutureInterfaceRuntime : MonoBehaviour
{
    private const string RuntimeName = "AOG_Future_Interface_Runtime";
    private RectTransform pulseLine;
    private float pulseTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindObjectOfType<AOGFutureInterfaceRuntime>() != null)
            return;

        GameObject root = new GameObject(RuntimeName);
        root.AddComponent<AOGFutureInterfaceRuntime>();
    }

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        Build();
    }

    void Update()
    {
        pulseTime += Time.unscaledDeltaTime;
        if (pulseLine != null)
        {
            Vector2 pos = pulseLine.anchoredPosition;
            pos.x = Mathf.Lerp(-430f, 430f, (Mathf.Sin(pulseTime * 0.65f) + 1f) * 0.5f);
            pulseLine.anchoredPosition = pos;
        }
    }

    private void Build()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 350;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        CreateCorner("TL", new Vector2(0f, 1f), new Vector2(44f, -44f), 1f, -1f);
        CreateCorner("TR", new Vector2(1f, 1f), new Vector2(-44f, -44f), -1f, -1f);
        CreateCorner("BL", new Vector2(0f, 0f), new Vector2(44f, 44f), 1f, 1f);
        CreateCorner("BR", new Vector2(1f, 0f), new Vector2(-44f, 44f), -1f, 1f);

        GameObject rail = NewImage("TopDataRail", transform, new Color(0.08f, 0.72f, 1f, 0.20f));
        RectTransform rect = rail.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(900f, 2f);
        rect.anchoredPosition = new Vector2(0f, -112f);

        GameObject pulse = NewImage("DataPulse", rail.transform, new Color(0.48f, 0.20f, 1f, 0.85f));
        pulseLine = pulse.GetComponent<RectTransform>();
        pulseLine.anchorMin = pulseLine.anchorMax = new Vector2(0.5f, 0.5f);
        pulseLine.sizeDelta = new Vector2(70f, 4f);
        pulseLine.anchoredPosition = new Vector2(-430f, 0f);
    }

    private void CreateCorner(string name, Vector2 anchor, Vector2 position, float sx, float sy)
    {
        GameObject root = new GameObject("FutureCorner_" + name, typeof(RectTransform));
        root.transform.SetParent(transform, false);
        RectTransform rr = root.GetComponent<RectTransform>();
        rr.anchorMin = rr.anchorMax = anchor;
        rr.pivot = anchor;
        rr.anchoredPosition = position;
        rr.localScale = new Vector3(sx, sy, 1f);

        GameObject horizontal = NewImage("H", root.transform, new Color(0.08f, 0.72f, 1f, 0.55f));
        RectTransform h = horizontal.GetComponent<RectTransform>();
        h.anchorMin = h.anchorMax = Vector2.zero;
        h.pivot = Vector2.zero;
        h.sizeDelta = new Vector2(130f, 3f);

        GameObject vertical = NewImage("V", root.transform, new Color(0.46f, 0.16f, 1f, 0.55f));
        RectTransform v = vertical.GetComponent<RectTransform>();
        v.anchorMin = v.anchorMax = Vector2.zero;
        v.pivot = Vector2.zero;
        v.sizeDelta = new Vector2(3f, 90f);
    }

    private static GameObject NewImage(string name, Transform parent, Color color)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(parent, false);
        Image image = obj.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return obj;
    }
}
