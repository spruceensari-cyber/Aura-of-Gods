using UnityEngine;

[DefaultExecutionOrder(-35)]
public class AOGChampionAbilityActivationGuard : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindFirstObjectByType<AOGChampionAbilityActivationGuard>() != null)
            return;

        GameObject host = new GameObject("AOG_Champion_Ability_Activation_Guard");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGChampionAbilityActivationGuard>();
    }

    private void Update()
    {
        AOGActiveChampion[] champions = FindObjectsByType<AOGActiveChampion>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (AOGActiveChampion champion in champions)
        {
            if (champion == null)
                continue;

            AOGLyraSkillInputBridgeRuntime lyraBridge = champion.GetComponent<AOGLyraSkillInputBridgeRuntime>();
            if (lyraBridge != null && lyraBridge.enabled != champion.IsActiveChampion)
                lyraBridge.enabled = champion.IsActiveChampion;
        }
    }
}
