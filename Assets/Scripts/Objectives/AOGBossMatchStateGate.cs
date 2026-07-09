using UnityEngine;

[DefaultExecutionOrder(-20)]
public class AOGBossMatchStateGate : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindFirstObjectByType<AOGBossMatchStateGate>() != null)
            return;

        GameObject host = new GameObject("AOG_Boss_Match_State_Gate");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGBossMatchStateGate>();
    }

    private void Update()
    {
        bool shouldRun = AOGMatchDirector.Instance != null && AOGMatchDirector.Instance.State == AOGMatchState.Playing;
        AOGNeutralBossAI[] bosses = FindObjectsByType<AOGNeutralBossAI>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (AOGNeutralBossAI boss in bosses)
        {
            if (boss != null && boss.enabled != shouldRun)
                boss.enabled = shouldRun;
        }
    }
}
