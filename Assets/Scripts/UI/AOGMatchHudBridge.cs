using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DefaultExecutionOrder(1350)]
public class AOGMatchHudBridge : MonoBehaviour
{
    private Text timerText;
    private Text hintText;
    private float nextSearch;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindFirstObjectByType<AOGMatchHudBridge>() != null)
            return;

        GameObject host = new GameObject("AOG_Match_HUD_Bridge");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGMatchHudBridge>();
    }

    private void Update()
    {
        if ((timerText == null || hintText == null) && Time.unscaledTime >= nextSearch)
        {
            nextSearch = Time.unscaledTime + 0.5f;
            ResolveHudReferences();
        }

        AOGMatchDirector director = AOGMatchDirector.Instance;
        if (timerText != null && director != null)
        {
            int total = Mathf.Max(0, Mathf.FloorToInt(director.MatchTime));
            timerText.text = (total / 60).ToString("00") + ":" + (total % 60).ToString("00");
        }

        if (hintText != null)
            hintText.text = "RIGHT CLICK: MOVE / ATTACK    •    P: AETHER MARKET    •    SPACE: FOCUS    •    WHEEL: ZOOM";
    }

    private void ResolveHudReferences()
    {
        AOGCompetitiveMobaHUDRuntime hud = FindFirstObjectByType<AOGCompetitiveMobaHUDRuntime>();
        if (hud == null)
            return;

        timerText = FindText(hud.transform, "Timer");
        hintText = FindText(hud.transform, "Hint");
    }

    private static Text FindText(Transform root, string objectName)
    {
        Transform found = Find(root, objectName);
        return found != null ? found.GetComponent<Text>() : null;
    }

    private static Transform Find(Transform root, string objectName)
    {
        if (root == null)
            return null;
        if (root.name == objectName)
            return root;

        foreach (Transform child in root)
        {
            Transform found = Find(child, objectName);
            if (found != null)
                return found;
        }

        return null;
    }
}
