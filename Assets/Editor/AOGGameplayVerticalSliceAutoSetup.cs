#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class AOGGameplayVerticalSliceAutoSetup
{
    private const string SessionKey = "AOG.GameplayVerticalSliceAutoSetup.v1";

    static AOGGameplayVerticalSliceAutoSetup()
    {
        EditorSceneManager.sceneOpened -= OnSceneOpened;
        EditorSceneManager.sceneOpened += OnSceneOpened;
        EditorApplication.delayCall += RunOncePerSession;
    }

    [MenuItem("Aura of Gods/Setup Lyra Gameplay Camera")]
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
        Camera camera = FindGameplayCamera(scene);
        if (camera == null)
            return false;

        bool changed = false;

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

        Transform lyra = FindLyra(scene);
        if (lyra != null && controller.target != lyra)
        {
            controller.target = lyra;
            changed = true;
        }

        changed |= SetIfDifferent(ref controller.pitch, 56f);
        changed |= SetIfDifferent(ref controller.yaw, 45f);
        changed |= SetIfDifferent(ref controller.fieldOfView, 40f);
        changed |= SetIfDifferent(ref controller.defaultZoom, 19f);
        changed |= SetIfDifferent(ref controller.minZoom, 13f);
        changed |= SetIfDifferent(ref controller.maxZoom, 27f);

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

        if (!Mathf.Approximately(camera.fieldOfView, 40f))
        {
            camera.fieldOfView = 40f;
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

    private static Transform FindLyra(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform result = FindLyraRecursive(root.transform);
            if (result != null)
                return result;
        }

        return null;
    }

    private static Transform FindLyraRecursive(Transform current)
    {
        if (current == null)
            return null;

        if (current.gameObject.name.ToLowerInvariant().Contains("lyra") && current.GetComponent<AOGPlayerMOBAController>() != null)
            return current;

        foreach (Transform child in current)
        {
            Transform found = FindLyraRecursive(child);
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
