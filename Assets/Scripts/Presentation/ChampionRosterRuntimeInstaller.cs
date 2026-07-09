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

        ChampionPresentationProfile lyraProfile = FindProfile(profiles, "lyra");
        ChampionPresentationProfile[] nonLyraProfiles = Array.FindAll(profiles, p => p != null && !string.Equals(p.championId, "lyra", StringComparison.OrdinalIgnoreCase));

        AOGPlayerMOBAController[] controllers = UnityEngine.Object.FindObjectsByType<AOGPlayerMOBAController>(FindObjectsSortMode.None);
        int genericProfileIndex = 0;

        foreach (AOGPlayerMOBAController player in controllers)
        {
            if (player == null)
                continue;

            string lowerName = player.gameObject.name.ToLowerInvariant();
            if (lowerName.Contains("ragnar"))
            {
                UnityEngine.Object.Destroy(player.gameObject);
                continue;
            }

            ChampionAudioController audio = player.GetComponent<ChampionAudioController>();
            if (audio == null)
                audio = player.gameObject.AddComponent<ChampionAudioController>();

            ChampionPresentationController presentation = player.GetComponent<ChampionPresentationController>();
            if (presentation == null)
                presentation = player.gameObject.AddComponent<ChampionPresentationController>();

            presentation.audioController = audio;

            ChampionPresentationProfile selected = null;
            if (lowerName.Contains("lyra") && lyraProfile != null)
            {
                selected = lyraProfile;
            }
            else if (presentation.profile != null)
            {
                selected = presentation.profile;
            }
            else if (nonLyraProfiles.Length > 0)
            {
                selected = nonLyraProfiles[genericProfileIndex % nonLyraProfiles.Length];
                genericProfileIndex++;
            }
            else if (profiles.Length > 0)
            {
                selected = profiles[0];
            }

            if (selected != null && presentation.profile != selected)
                presentation.SetProfile(selected);

            ChampionArchetype archetype = selected != null ? selected.archetype : ChampionArchetype.Duelist;
            audio.ConfigureProceduralFallback(archetype);

            if (lowerName.Contains("lyra"))
            {
                LyraSkillSet lyra = player.GetComponent<LyraSkillSet>();
                if (lyra == null)
                    lyra = player.gameObject.AddComponent<LyraSkillSet>();

                lyra.presentation = presentation;
                lyra.animator = presentation.animator != null ? presentation.animator : player.GetComponentInChildren<Animator>();

                AOGCharacterStats stats = player.GetComponent<AOGCharacterStats>();
                if (stats != null)
                    lyra.team = stats.team;
            }
        }
    }

    private static ChampionPresentationProfile FindProfile(ChampionPresentationProfile[] profiles, string championId)
    {
        foreach (ChampionPresentationProfile profile in profiles)
        {
            if (profile != null && string.Equals(profile.championId, championId, StringComparison.OrdinalIgnoreCase))
                return profile;
        }

        return null;
    }
}
