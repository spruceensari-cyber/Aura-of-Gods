using UnityEngine;

[DefaultExecutionOrder(110)]
public class AOGTowerHealthBarConsolidation : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindFirstObjectByType<AOGTowerHealthBarConsolidation>() != null)
            return;

        GameObject host = new GameObject("AOG_Tower_HealthBar_Consolidation");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGTowerHealthBarConsolidation>();
    }

    private void Update()
    {
        TowerHealth[] towers = FindObjectsByType<TowerHealth>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (TowerHealth tower in towers)
        {
            if (tower == null)
                continue;

            AOGWorldHealthBar legacy = tower.GetComponent<AOGWorldHealthBar>();
            if (legacy != null)
                legacy.Hide();

            if (tower.GetComponent<AOGObjectiveWorldBar>() == null)
            {
                AOGObjectiveWorldBar bar = tower.gameObject.AddComponent<AOGObjectiveWorldBar>();
                bar.offset = new Vector3(0f, 6.5f, 0f);
                bar.width = 3.6f;
                bar.height = 0.26f;
            }
        }
    }
}
