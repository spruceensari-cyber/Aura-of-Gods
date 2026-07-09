using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Broadcast overlay. Hidden for normal players and only visible while spectator mode is active.
/// </summary>
public class AOGTournamentHUDRuntime : MonoBehaviour
{
    private Canvas canvas;
    private Text phaseText;
    private Text timerText;
    private Text seriesText;
    private Text objectiveText;
    private ObjectiveManager objectives;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindObjectOfType<AOGTournamentHUDRuntime>() != null) return;
        new GameObject("AOG_Tournament_HUD_Runtime").AddComponent<AOGTournamentHUDRuntime>();
    }

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        BuildOverlay();
        SetVisible(false);
    }

    void Update()
    {
        AOGSpectatorCameraRuntime spectator = FindObjectOfType<AOGSpectatorCameraRuntime>();
        bool visible = spectator != null && spectator.SpectatorMode;
        SetVisible(visible);
        if (!visible) return;

        if (objectives == null) objectives = FindObjectOfType<ObjectiveManager>();
        Refresh();
    }

    private void SetVisible(bool visible)
    {
        if (canvas != null) canvas.enabled = visible;
    }

    private void BuildOverlay()
    {
        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 400;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        GameObject top = CreatePanel("TournamentTopBar", transform, new Vector2(0.5f, 1f), new Vector2(840f, 92f), new Vector2(0f, -18f));
        phaseText = CreateText("Phase", top.transform, "WARMUP", 18, new Vector2(0f, 28f), new Vector2(180f, 26f));
        timerText = CreateText("Timer", top.transform, "00:00", 30, new Vector2(0f, -4f), new Vector2(180f, 40f));
        seriesText = CreateText("Series", top.transform, "BLUE 0  |  BO3  |  0 RED", 22, new Vector2(0f, -34f), new Vector2(760f, 30f));

        GameObject objective = CreatePanel("ObjectiveBar", transform, new Vector2(0.5f, 1f), new Vector2(520f, 44f), new Vector2(0f, -118f));
        objectiveText = CreateText("Objectives", objective.transform, "DRAGON LIVE     MEDUSA LIVE", 18, Vector2.zero, new Vector2(500f, 34f));
    }

    private void Refresh()
    {
        AOGCompetitiveMatchController match = AOGCompetitiveMatchController.Instance;
        if (match != null)
        {
            phaseText.text = match.Phase.ToString().ToUpperInvariant();
            float time = match.Phase == AOGMatchPhase.Warmup || match.Phase == AOGMatchPhase.Countdown
                ? match.PhaseRemaining
                : match.MatchTime;
            int minutes = Mathf.FloorToInt(time / 60f);
            int seconds = Mathf.FloorToInt(time % 60f);
            timerText.text = $"{minutes:00}:{seconds:00}";
            seriesText.text = $"{match.BlueTeamName} {match.BlueSeriesWins}   |   BO{match.BestOf}   |   {match.RedSeriesWins} {match.RedTeamName}";
        }

        if (objectives != null)
        {
            string dragon = objectives.DragonAlive ? "LIVE" : FormatTime(objectives.DragonRespawnRemaining);
            string medusa = objectives.MedusaAlive ? "LIVE" : FormatTime(objectives.MedusaRespawnRemaining);
            objectiveText.text = $"DRAGON {dragon}     MEDUSA {medusa}";
        }
    }

    private static string FormatTime(float time)
    {
        int total = Mathf.CeilToInt(time);
        return $"{total / 60:00}:{total % 60:00}";
    }

    private static GameObject CreatePanel(string name, Transform parent, Vector2 anchor, Vector2 size, Vector2 position)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        Image image = obj.GetComponent<Image>();
        image.color = new Color(0.02f, 0.03f, 0.055f, 0.93f);
        image.raycastTarget = false;
        return obj;
    }

    private static Text CreateText(string name, Transform parent, string value, int fontSize, Vector2 position, Vector2 size)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Text));
        obj.transform.SetParent(parent, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;

        Text text = obj.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = value;
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.raycastTarget = false;
        return text;
    }
}
