using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tournament-style announcer presentation layer.
/// Shows high-priority game events and routes optional voice clips through the audio director.
/// </summary>
public class AOGAnnouncerRuntime : MonoBehaviour
{
    private Text banner;
    private CanvasGroup group;
    private Coroutine activeRoutine;
    private ObjectiveManager objectives;
    private bool lastDragonAlive;
    private bool lastMedusaAlive;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindObjectOfType<AOGAnnouncerRuntime>() != null)
            return;
        GameObject obj = new GameObject("AOG_Announcer_Runtime");
        obj.AddComponent<AOGAnnouncerRuntime>();
    }

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        BuildUI();
    }

    void Start()
    {
        StartCoroutine(DelayedStartCue());
    }

    void Update()
    {
        if (objectives == null)
        {
            objectives = FindObjectOfType<ObjectiveManager>();
            if (objectives != null)
            {
                lastDragonAlive = objectives.DragonAlive;
                lastMedusaAlive = objectives.MedusaAlive;
            }
        }

        if (objectives == null)
            return;

        if (lastDragonAlive && !objectives.DragonAlive)
            Announce("DRAGON SLAIN", AOGAudioCue.ObjectiveSlain, 2.0f);
        else if (!lastDragonAlive && objectives.DragonAlive)
            Announce("DRAGON AWAKENED", AOGAudioCue.ObjectiveSpawn, 1.6f);

        if (lastMedusaAlive && !objectives.MedusaAlive)
            Announce("MEDUSA SLAIN", AOGAudioCue.ObjectiveSlain, 2.0f);
        else if (!lastMedusaAlive && objectives.MedusaAlive)
            Announce("MEDUSA AWAKENED", AOGAudioCue.ObjectiveSpawn, 1.6f);

        lastDragonAlive = objectives.DragonAlive;
        lastMedusaAlive = objectives.MedusaAlive;
    }

    public void Announce(string text, AOGAudioCue cue, float duration = 1.5f)
    {
        if (activeRoutine != null)
            StopCoroutine(activeRoutine);
        activeRoutine = StartCoroutine(ShowRoutine(text, cue, duration));
    }

    private IEnumerator DelayedStartCue()
    {
        yield return new WaitForSecondsRealtime(1.0f);
        Announce("ENTER THE AETHER", AOGAudioCue.MatchStart, 2.2f);
        AOGAudioDirectorRuntime.Instance?.PlayMatchMusic();
    }

    private IEnumerator ShowRoutine(string text, AOGAudioCue cue, float duration)
    {
        if (banner == null || group == null)
            yield break;

        banner.text = text;
        group.alpha = 0f;
        AOGAudioDirectorRuntime.Instance?.PlayCue(cue);

        float inTime = 0.18f;
        for (float t = 0f; t < inTime; t += Time.unscaledDeltaTime)
        {
            group.alpha = Mathf.Clamp01(t / inTime);
            yield return null;
        }
        group.alpha = 1f;

        yield return new WaitForSecondsRealtime(duration);

        float outTime = 0.28f;
        for (float t = 0f; t < outTime; t += Time.unscaledDeltaTime)
        {
            group.alpha = 1f - Mathf.Clamp01(t / outTime);
            yield return null;
        }
        group.alpha = 0f;
        activeRoutine = null;
    }

    private void BuildUI()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 600;
        gameObject.AddComponent<CanvasScaler>().referenceResolution = new Vector2(1920f, 1080f);
        group = gameObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;

        GameObject panel = new GameObject("AnnouncerPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(transform, false);
        RectTransform pr = panel.GetComponent<RectTransform>();
        pr.anchorMin = pr.anchorMax = new Vector2(0.5f, 0.73f);
        pr.sizeDelta = new Vector2(720f, 72f);
        panel.GetComponent<Image>().color = new Color(0.02f, 0.03f, 0.06f, 0.84f);

        GameObject textObj = new GameObject("AnnouncerText", typeof(RectTransform), typeof(Text));
        textObj.transform.SetParent(panel.transform, false);
        RectTransform tr = textObj.GetComponent<RectTransform>();
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.offsetMin = Vector2.zero;
        tr.offsetMax = Vector2.zero;

        banner = textObj.GetComponent<Text>();
        banner.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        banner.fontSize = 34;
        banner.fontStyle = FontStyle.Bold;
        banner.alignment = TextAnchor.MiddleCenter;
        banner.color = new Color(0.80f, 0.95f, 1f, 1f);
        banner.raycastTarget = false;
    }
}
