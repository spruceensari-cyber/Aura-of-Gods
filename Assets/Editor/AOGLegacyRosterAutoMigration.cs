#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class AOGLegacyRosterAutoMigration
{
    private const string SessionKey = "AOG.LegacyRosterMigration.KeepLyraRemoveRagnar.v2";
    private const string LyraProfilePath = "Assets/Resources/AOGChampions/Lyra_Presentation.asset";

    static AOGLegacyRosterAutoMigration()
    {
        EditorSceneManager.sceneOpened -= OnSceneOpened;
        EditorSceneManager.sceneOpened += OnSceneOpened;
        EditorApplication.delayCall += RunInitialMigration;
    }

    [MenuItem("Aura of Gods/Repair Current Scene - Keep Lyra Remove Ragnar")]
    public static void RepairCurrentSceneNow()
    {
        AOGChampionPresentationBuilder.EnsureGeneratedAssets();

        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.IsValid() && activeScene.isLoaded)
        {
            bool changed = RepairScene(activeScene);
            if (changed)
                EditorSceneManager.SaveScene(activeScene);
        }

        RepairAllPrefabs();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("AOG roster repair completed: Lyra kept and repaired, Ragnar removed.");
    }

    private static void RunInitialMigration()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
            return;

        if (SessionState.GetBool(SessionKey, false))
            return;

        SessionState.SetBool(SessionKey, true);
        AOGChampionPresentationBuilder.EnsureGeneratedAssets();

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!scene.IsValid() || !scene.isLoaded)
                continue;

            bool changed = RepairScene(scene);
            if (changed && !string.IsNullOrEmpty(scene.path))
                EditorSceneManager.SaveScene(scene);
        }

        RepairAllPrefabs();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
            return;

        EditorApplication.delayCall += () =>
        {
            if (!scene.IsValid() || !scene.isLoaded)
                return;

            AOGChampionPresentationBuilder.EnsureGeneratedAssets();
            bool changed = RepairScene(scene);
            if (changed && !string.IsNullOrEmpty(scene.path))
                EditorSceneManager.SaveScene(scene);
        };
    }

    private static bool RepairScene(Scene scene)
    {
        bool changed = false;
        List<GameObject> ragnarObjects = new List<GameObject>();

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            CollectRagnarObjects(root, ragnarObjects);
            changed |= RemoveMissingScriptsRecursive(root);
            changed |= RepairLyraRecursive(root);
        }

        foreach (GameObject ragnar in ragnarObjects)
        {
            if (ragnar == null)
                continue;

            UnityEngine.Object.DestroyImmediate(ragnar);
            changed = true;
        }

        if (changed)
            EditorSceneManager.MarkSceneDirty(scene);

        return changed;
    }

    private static void CollectRagnarObjects(GameObject root, List<GameObject> output)
    {
        if (root == null)
            return;

        if (ContainsIgnoreCase(root.name, "ragnar"))
        {
            output.Add(root);
            return;
        }

        foreach (Transform child in root.transform)
            CollectRagnarObjects(child.gameObject, output);
    }

    private static bool RemoveMissingScriptsRecursive(GameObject gameObject)
    {
        if (gameObject == null)
            return false;

        bool changed = false;
        int missingCount = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(gameObject);
        if (missingCount > 0)
        {
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(gameObject);
            changed = true;
        }

        foreach (Transform child in gameObject.transform)
            changed |= RemoveMissingScriptsRecursive(child.gameObject);

        return changed;
    }

    private static bool RepairLyraRecursive(GameObject gameObject)
    {
        if (gameObject == null)
            return false;

        bool changed = false;
        if (ContainsIgnoreCase(gameObject.name, "lyra"))
            changed |= RepairLyraObject(gameObject);

        foreach (Transform child in gameObject.transform)
            changed |= RepairLyraRecursive(child.gameObject);

        return changed;
    }

    private static bool RepairLyraObject(GameObject lyraObject)
    {
        AOGPlayerMOBAController playerController = lyraObject.GetComponent<AOGPlayerMOBAController>();
        AOGCharacterStats stats = lyraObject.GetComponent<AOGCharacterStats>();

        // Ignore child meshes/textures that happen to contain Lyra in their name.
        if (playerController == null && stats == null)
            return false;

        bool changed = false;

        ChampionAudioController audio = lyraObject.GetComponent<ChampionAudioController>();
        if (audio == null)
        {
            audio = lyraObject.AddComponent<ChampionAudioController>();
            changed = true;
        }

        ChampionPresentationController presentation = lyraObject.GetComponent<ChampionPresentationController>();
        if (presentation == null)
        {
            presentation = lyraObject.AddComponent<ChampionPresentationController>();
            changed = true;
        }

        Animator animator = lyraObject.GetComponentInChildren<Animator>(true);
        ChampionPresentationProfile lyraProfile = AssetDatabase.LoadAssetAtPath<ChampionPresentationProfile>(LyraProfilePath);

        if (presentation.audioController != audio)
        {
            presentation.audioController = audio;
            changed = true;
        }

        if (presentation.animator != animator)
        {
            presentation.animator = animator;
            changed = true;
        }

        if (lyraProfile != null && presentation.profile != lyraProfile)
        {
            presentation.profile = lyraProfile;
            changed = true;
        }

        if (animator != null && lyraProfile != null && lyraProfile.animatorController != null && animator.runtimeAnimatorController != lyraProfile.animatorController)
        {
            animator.runtimeAnimatorController = lyraProfile.animatorController;
            animator.applyRootMotion = false;
            changed = true;
        }

        LyraSkillSet skills = lyraObject.GetComponent<LyraSkillSet>();
        if (skills == null)
        {
            skills = lyraObject.AddComponent<LyraSkillSet>();
            changed = true;
        }

        if (skills.presentation != presentation)
        {
            skills.presentation = presentation;
            changed = true;
        }

        if (skills.animator != animator)
        {
            skills.animator = animator;
            changed = true;
        }

        if (stats != null && skills.team != stats.team)
        {
            skills.team = stats.team;
            changed = true;
        }

        if (playerController != null && playerController.presentation != presentation)
        {
            playerController.presentation = presentation;
            changed = true;
        }

        if (changed)
        {
            EditorUtility.SetDirty(lyraObject);
            EditorUtility.SetDirty(presentation);
            EditorUtility.SetDirty(skills);
            if (animator != null) EditorUtility.SetDirty(animator);
            if (playerController != null) EditorUtility.SetDirty(playerController);
        }

        return changed;
    }

    private static void RepairAllPrefabs()
    {
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });

        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path))
                continue;

            GameObject root = PrefabUtility.LoadPrefabContents(path);
            if (root == null)
                continue;

            bool changed = false;
            try
            {
                List<GameObject> ragnarObjects = new List<GameObject>();
                CollectRagnarObjects(root, ragnarObjects);
                changed |= RemoveMissingScriptsRecursive(root);
                changed |= RepairLyraRecursive(root);

                foreach (GameObject ragnar in ragnarObjects)
                {
                    if (ragnar == null)
                        continue;

                    if (ragnar == root)
                    {
                        // A prefab whose root itself is Ragnar is removed as an asset after unloading.
                        changed = false;
                        PrefabUtility.UnloadPrefabContents(root);
                        root = null;
                        AssetDatabase.DeleteAsset(path);
                        break;
                    }

                    UnityEngine.Object.DestroyImmediate(ragnar);
                    changed = true;
                }

                if (root != null && changed)
                    PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("AOG prefab migration skipped " + path + ": " + exception.Message);
            }
            finally
            {
                if (root != null)
                    PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }

    private static bool ContainsIgnoreCase(string source, string value)
    {
        return !string.IsNullOrEmpty(source) && source.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
#endif
