using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ChampionRosterRuntimeInstaller
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InstallAfterLoad()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        InstallIntoCurrentScene();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        InstallIntoCurrentScene();
    }

    private static void InstallIntoCurrentScene()
    {
        ChampionPresentationProfile[] profiles = Resources.LoadAll<ChampionPresentationProfile>("AOGChampions");
        Array.Sort(profiles, (a, b) => string.CompareOrdinal(a.championId, b.championId));

        AOGPlayerMOBAController[] controllers = UnityEngine.Object.FindObjectsByType<AOGPlayerMOBAController>(FindObjectsSortMode.None);

        for (int i = 0; i < controllers.Length; i++)
        {
            AOGPlayerMOBAController player = controllers[i];
            if (player == null)
                continue;

            ChampionAudioController audio = player.GetComponent<ChampionAudioController>();
            if (audio == null)
                audio = player.gameObject.AddComponent<ChampionAudioController>();

            ChampionPresentationController presentation = player.GetComponent<ChampionPresentationController>();
            if (presentation == null)
                presentation = player.gameObject.AddComponent<ChampionPresentationController>();

            presentation.audioController = audio;

            if (profiles.Length > 0 && presentation.profile == null)
                presentation.SetProfile(profiles[i % profiles.Length]);
        }
    }
}
