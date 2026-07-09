#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class AOGGameplayVerticalSliceAutoSetup
{
    private const string SessionKey = "AOG.GameplayVerticalSliceAutoSetup.v3";

    static AOGGameplayVerticalSliceAutoSetup()
    {
        EditorSceneManager.sceneOpened -= OnSceneOpened;
        EditorSceneManager.sceneOpened += OnSceneOpened;
        EditorApplication.delayCall += RunOncePerSession;
    }

    [MenuItem("Aura of Gods/Setup MOBA Gameplay Camera")]
    public static void SetupCurrentScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
            return;

        if (SetupScene(scene) && !string.IsNullOrEmpty(scene.path))
            EditorSceneManager.SaveScene(scene);
    }

    private static void RunOncePerSession()
    {
        if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        if (SessionState.GetBool(SessionKey, false))
            return;

        SessionState.SetBool(SessionKey, true);

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!scene.IsValid() || !scene.isLoaded)
                continue;

            if (SetupScene(scene) && !string.IsNullOrEmpty(scene.path))
                EditorSceneManager.SaveScene(scene);
        }
    }

    private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        EditorApplication.delayCall += () =>
        {
            if (scene.IsValid() && scene.isLoaded && SetupScene(scene) && !string.IsNullOrEmpty(scene.path))
                EditorSceneManager.SaveScene(scene);
        };
    }

    private static bool SetupScene(Scene scene)
    {
        bool changed = RemoveLegacyReadabilityObjects(scene);
        Camera camera = FindGameplayCamera(scene);
        if (camera == null)
            return changed;

        if (camera.tag != "MainCamera")
        {
            camera.tag = "MainCamera";
            changed = true;
        }

        CameraController legacy = camera.GetComponent<CameraController>();
        if (legacy != null && legacy.enabled)
        {
            legacy.enabled = false;
            EditorUtility.SetDirty(legacy);
            changed = true;
        }

        AOGMobaCameraController controller = camera.GetComponent<AOGMobaCameraController>();
        if (controller == null)
        {
            controller = Undo.AddComponent<AOGMobaCameraController>(camera.gameObject);
            changed = true;
        }

        Transform initialChampion = FindInitialChampion(scene);
        if (initialChampion != null && controller.target != initialChampion)
        {
            controller.target = initialChampion;
            changed = true;
        }

        changed |= SetIfDifferent(ref controller.pitch, 58f);
        changed |= SetIfDifferent(ref controller.yaw, 45f);
        changed |= SetIfDifferent(ref controller.fieldOfView, 50f);
        changed |= SetIfDifferent(ref controller.defaultZoom, 32f);
        changed |= SetIfDifferent(ref controller.minZoom, 18f);
        changed |= SetIfDifferent(ref controller.maxZoom, 65f);
        changed |= SetIfDifferent(ref controller.zoomStep, 4.2f);
        changed |= SetIfDifferent(ref controller.edgePanSpeed, 20f);
        changed |= SetIfDifferent(ref controller.maxPanDistanceFromTarget, 46f);
        changed |= SetIfDifferent(ref controller.forwardFramingBias, 0.35f);
        changed |= SetIfDifferent(ref controller.targetLookAhead, 0.08f);
        changed |= SetIfDifferent(ref controller.maxLookAheadDistance, 1.4f);

        if (!controller.autoFindLyra)
        {
            controller.autoFindLyra = true;
            changed = true;
        }

        if (!controller.edgePanEnabled)
        {
            controller.edgePanEnabled = true;
            changed = true;
        }

        if (camera.orthographic)
        {
            camera.orthographic = false;
            changed = true;
        }

        if (!Mathf.Approximately(camera.fieldOfView, 50f))
        {
            camera.fieldOfView = 50f;
            changed = true;
        }

        if (camera.farClipPlane < 900f)
        {
            camera.farClipPlane = 900f;
            changed = true;
        }

        if (changed)
        {
            EditorUtility.SetDirty(camera);
            EditorUtility.SetDirty(controller);
            EditorSceneManager.MarkSceneDirty(scene);
        }

        return changed;
    }

    private static bool RemoveLegacyReadabilityObjects(Scene scene)
    {
        bool changed = false;

        foreach (GameObject root in scene.GetRootGameObjects())
            changed |= RemoveLegacyRecursive(root.transform);

        AOGPremiumUnitAnimator[] legacyAnimators = Object.FindObjectsByType<AOGPremiumUnitAnimator>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (AOGPremiumUnitAnimator animator in legacyAnimators)
        {
            if (animator == null || animator.gameObject.scene != scene)
                continue;

            if (animator.enabled)
            {
                animator.enabled = false;
                EditorUtility.SetDirty(animator);
                changed = true;
            }
        }

        return changed;
    }

    private static bool RemoveLegacyRecursive(Transform current)
    {
        if (current == null)
            return false;

        bool changed = false;
        for (int i = current.childCount - 1; i >= 0; i--)
        {
            Transform child = current.GetChild(i);
            string lower = child.gameObject.name.ToLowerInvariant();

            if (lower.Contains("readability_ring") || lower == "aog_premium_ground_shadow")
            {
                Object.DestroyImmediate(child.gameObject);
                changed = true;
                continue;
            }

            changed |= RemoveLegacyRecursive(child);
        }

        return changed;
    }

    private static Camera FindGameplayCamera(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Camera[] cameras = root.GetComponentsInChildren<Camera>(true);
            foreach (Camera candidate in cameras)
            {
                if (candidate == null)
                    continue;

                if (candidate.CompareTag("MainCamera") || candidate.gameObject.name.ToLowerInvariant().Contains("main camera"))
                    return candidate;
            }
        }

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Camera candidate = root.GetComponentInChildren<Camera>(true);
            if (candidate != null)
                return candidate;
        }

        return null;
    }

    private static Transform FindInitialChampion(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform result = FindChampionRecursive(root.transform);
            if (result != null)
                return result;
        }

        return null;
    }

    private static Transform FindChampionRecursive(Transform current)
    {
        if (current == null)
            return null;

        string lower = current.gameObject.name.ToLowerInvariant();
        bool candidateName = lower.Contains("lyra") || lower.Contains("kaelith") || lower.Contains("player");
        if (candidateName && current.GetComponent<AOGPlayerMOBAController>() != null)
            return current;

        foreach (Transform child in current)
        {
            Transform found = FindChampionRecursive(child);
            if (found != null)
                return found;
        }

        return null;
    }

    private static bool SetIfDifferent(ref float field, float value)
    {
        if (Mathf.Approximately(field, value))
            return false;

        field = value;
        return true;
    }
}
#endif
