using System.Collections;
using UnityEngine;

public class AOGUnifiedInputAttachRetry : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (Object.FindFirstObjectByType<AOGUnifiedInputAttachRetry>() != null)
            return;

        GameObject host = new GameObject("AOG_Unified_Input_Attach_Retry");
        Object.DontDestroyOnLoad(host);
        host.AddComponent<AOGUnifiedInputAttachRetry>();
    }

    private void Start()
    {
        StartCoroutine(AttachUntilReady());
    }

    private IEnumerator AttachUntilReady()
    {
        for (int attempt = 0; attempt < 40; attempt++)
        {
            AOGPlayerMOBAController[] players = Object.FindObjectsByType<AOGPlayerMOBAController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (AOGPlayerMOBAController player in players)
            {
                if (player == null || !player.gameObject.name.ToLowerInvariant().Contains("lyra"))
                    continue;

                if (player.GetComponent<AOGUnifiedMobaInputDriver>() == null)
                    player.gameObject.AddComponent<AOGUnifiedMobaInputDriver>();

                if (player.GetComponent<LyraSkillSet>() != null && player.GetComponent<AOGLyraSkillInputBridgeRuntime>() == null)
                    player.gameObject.AddComponent<AOGLyraSkillInputBridgeRuntime>();

                yield break;
            }

            yield return new WaitForSecondsRealtime(0.1f);
        }
    }
}
