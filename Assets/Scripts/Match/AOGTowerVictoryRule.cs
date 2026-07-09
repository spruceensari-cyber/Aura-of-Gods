using UnityEngine;

[DefaultExecutionOrder(200)]
public class AOGTowerVictoryRule : MonoBehaviour
{
    private int initialBlue;
    private int initialRed;
    private bool initialized;
    private bool blueVictoryTriggered;
    private bool redVictoryTriggered;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        GameObject host = new GameObject("AOG_Tower_Victory_Rule");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGTowerVictoryRule>();
    }

    private void Start()
    {
        Invoke(nameof(CaptureInitialCounts), 1.2f);
    }

    private void CaptureInitialCounts()
    {
        CountTowers(out initialBlue, out initialRed);
        initialized = initialBlue > 0 || initialRed > 0;
    }

    private void Update()
    {
        if (!initialized || AOGMatchDirector.Instance == null || AOGMatchDirector.Instance.State != AOGMatchState.Playing)
            return;

        CountTowers(out int blueAlive, out int redAlive);

        if (!blueVictoryTriggered && initialRed > 0 && redAlive == 0)
        {
            blueVictoryTriggered = true;
            AOGNexusCore redNexus = AOGMatchDirector.Instance.RedNexus;
            if (redNexus != null) redNexus.TakeDamage(redNexus.maxHp + 1f);
        }

        if (!redVictoryTriggered && initialBlue > 0 && blueAlive == 0)
        {
            redVictoryTriggered = true;
            AOGNexusCore blueNexus = AOGMatchDirector.Instance.BlueNexus;
            if (blueNexus != null) blueNexus.TakeDamage(blueNexus.maxHp + 1f);
        }
    }

    private static void CountTowers(out int blue, out int red)
    {
        blue = 0;
        red = 0;
        foreach (TowerHealth tower in FindObjectsByType<TowerHealth>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (tower == null || tower.hp <= 0f || !tower.gameObject.activeInHierarchy) continue;
            if (tower.towerTeam == MinionTeam.Blue) blue++; else red++;
        }
    }
}
