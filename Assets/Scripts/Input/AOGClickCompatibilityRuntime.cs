using UnityEngine;

[DefaultExecutionOrder(-30)]
public class AOGClickCompatibilityRuntime : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindFirstObjectByType<AOGClickCompatibilityRuntime>() != null)
            return;

        GameObject host = new GameObject("AOG_Click_Compatibility_Runtime");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGClickCompatibilityRuntime>();
    }

    private void Update()
    {
        AOGActiveChampion current = AOGActiveChampion.Current;
        if (current == null)
            return;

        AOGUnifiedMobaInputDriver driver = current.GetComponent<AOGUnifiedMobaInputDriver>();
        if (driver != null)
            driver.leftClickAlsoMoves = true;
    }
}
