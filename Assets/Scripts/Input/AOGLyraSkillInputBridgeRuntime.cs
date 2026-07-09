using System.Reflection;
using UnityEngine;

[DefaultExecutionOrder(-45)]
public class AOGLyraSkillInputBridgeRuntime : MonoBehaviour
{
    private LyraSkillSet skills;
    private MethodInfo castQ;
    private MethodInfo castW;
    private MethodInfo castE;
    private MethodInfo castR;

    private void Awake()
    {
        skills = GetComponent<LyraSkillSet>();
        if (skills == null)
        {
            enabled = false;
            return;
        }

        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        System.Type type = typeof(LyraSkillSet);
        castQ = type.GetMethod("CastQ", flags);
        castW = type.GetMethod("CastW", flags);
        castE = type.GetMethod("CastE", flags);
        castR = type.GetMethod("CastR", flags);

        // Stop LyraSkillSet.Update from polling the legacy Input API.
        // Ability methods remain callable directly through reflection.
        skills.enabled = false;
    }

    private void Update()
    {
        if (skills == null)
            return;

        if (AOGInputBridge.KeyPressedThisFrame(KeyCode.Q)) castQ?.Invoke(skills, null);
        if (AOGInputBridge.KeyPressedThisFrame(KeyCode.W)) castW?.Invoke(skills, null);
        if (AOGInputBridge.KeyPressedThisFrame(KeyCode.E)) castE?.Invoke(skills, null);
        if (AOGInputBridge.KeyPressedThisFrame(KeyCode.R)) castR?.Invoke(skills, null);
    }
}

public static class AOGLyraSkillInputBridgeBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        AOGPlayerMOBAController[] players = Object.FindObjectsByType<AOGPlayerMOBAController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (AOGPlayerMOBAController player in players)
        {
            if (player == null || !player.gameObject.name.ToLowerInvariant().Contains("lyra"))
                continue;

            if (player.GetComponent<AOGLyraSkillInputBridgeRuntime>() == null)
                player.gameObject.AddComponent<AOGLyraSkillInputBridgeRuntime>();
            return;
        }
    }
}
