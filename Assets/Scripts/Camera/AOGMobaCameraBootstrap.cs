using UnityEngine;
using UnityEngine.SceneManagement;

public static class AOGMobaCameraBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        EnsureCamera();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureCamera();
    }

    private static void EnsureCamera()
    {
        Camera mainCamera = Camera.main;

        if (mainCamera == null)
        {
            Camera[] cameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
            foreach (Camera candidate in cameras)
            {
                if (candidate != null && candidate.enabled && candidate.gameObject.activeInHierarchy)
                {
                    mainCamera = candidate;
                    break;
                }
            }
        }

        if (mainCamera == null)
            return;

        if (!mainCamera.CompareTag("MainCamera"))
            mainCamera.tag = "MainCamera";

        CameraController legacy = mainCamera.GetComponent<CameraController>();
        if (legacy != null)
            legacy.enabled = false;

        AOGMobaCameraController controller = mainCamera.GetComponent<AOGMobaCameraController>();
        if (controller == null)
            controller = mainCamera.gameObject.AddComponent<AOGMobaCameraController>();

        controller.enabled = true;
        controller.autoFindLyra = true;
        controller.pitch = 57f;
        controller.yaw = 45f;
        controller.fieldOfView = 48f;
        controller.defaultZoom = 28f;
        controller.minZoom = 16f;
        controller.maxZoom = 46f;
        controller.zoomStep = 3.2f;
        controller.edgePanEnabled = true;
        controller.edgePanSpeed = 18f;
        controller.maxPanDistanceFromTarget = 36f;
        controller.forwardFramingBias = 0.6f;
        controller.targetLookAhead = 0.10f;
        controller.maxLookAheadDistance = 1.6f;

        mainCamera.orthographic = false;
        mainCamera.fieldOfView = controller.fieldOfView;
        mainCamera.nearClipPlane = 0.1f;
        mainCamera.farClipPlane = Mathf.Max(mainCamera.farClipPlane, 700f);
        mainCamera.allowHDR = true;
        mainCamera.allowMSAA = true;

        AudioListener[] listeners = Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
        bool activeListenerFound = false;
        foreach (AudioListener listener in listeners)
        {
            if (listener == null)
                continue;

            bool shouldBeActive = listener.gameObject == mainCamera.gameObject && !activeListenerFound;
            listener.enabled = shouldBeActive;
            if (shouldBeActive)
                activeListenerFound = true;
        }

        if (!activeListenerFound)
            mainCamera.gameObject.AddComponent<AudioListener>();
    }
}
