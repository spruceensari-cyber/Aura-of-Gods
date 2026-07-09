using UnityEngine;
using UnityEngine.UI;

public class AOGMatchEndFlowRuntime : MonoBehaviour
{
    private CanvasGroup group;
    private Text title;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        if (FindObjectOfType<AOGMatchEndFlowRuntime>() != null) return;
        new GameObject("AOG_Match_End_Flow_Runtime").AddComponent<AOGMatchEndFlowRuntime>();
    }

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        BuildUI();
        Hide();
    }

    public void ShowResult(TeamType winner, TeamType localTeam)
    {
        bool victory = winner == localTeam;
        title.text = victory ? "VICTORY" : "DEFEAT";
        group.alpha = 1f;
        group.interactable = true;
        group.blocksRaycasts = true;
        Time.timeScale = 0f;
        AOGAudioDirectorRuntime.Instance?.PlayCue(victory ? AOGAudioCue.Victory : AOGAudioCue.Defeat);
        AOGReplayEventLogRuntime.Instance?.Record("MatchEnd", winner.ToString(), string.Empty, Vector3.zero);
    }

    public void Hide()
    {
        if (group == null) return;
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
        Time.timeScale = 1f;
    }

    void BuildUI()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 900;
        group = gameObject.AddComponent<CanvasGroup>();

        GameObject panel = new GameObject("ResultPanel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(transform, false);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        panel.GetComponent<Image>().color = new Color(0.005f, 0.01f, 0.025f, 0.94f);

        GameObject textObj = new GameObject("ResultTitle", typeof(RectTransform), typeof(Text));
        textObj.transform.SetParent(panel.transform, false);
        RectTransform tr = textObj.GetComponent<RectTransform>();
        tr.anchorMin = tr.anchorMax = new Vector2(0.5f, 0.5f);
        tr.sizeDelta = new Vector2(900f, 160f);

        title = textObj.GetComponent<Text>();
        title.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        title.fontSize = 72;
        title.fontStyle = FontStyle.Bold;
        title.alignment = TextAnchor.MiddleCenter;
        title.color = new Color(0.78f, 0.95f, 1f, 1f);
    }
}
