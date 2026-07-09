using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-1200)]
public class AOGActiveChampionCameraAuthority : MonoBehaviour
{
    private static AOGActiveChampionCameraAuthority instance;
    private Camera gameplayCamera;
    private AOGMobaCameraController controller;
    private AOGActiveChampion boundChampion;
    private float nextResolve;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        EnsureInstance();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureInstance();
        if (instance != null)
            instance.ResolveCamera(true);
    }

    private static void EnsureInstance()
    {
        if (instance != null)
            return;

        GameObject host = new GameObject("AOG_Active_Champion_Camera_Authority");
        instance = host.AddComponent<AOGActiveChampionCameraAuthority>();
        DontDestroyOnLoad(host);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void LateUpdate()
    {
        if (Time.unscaledTime >= nextResolve)
        {
            nextResolve = Time.unscaledTime + 0.15f;
            ResolveCamera(false);
        }

        AOGActiveChampion active = AOGActiveChampion.Current;
        if (active == null || !active.IsActiveChampion || !active.gameObject.activeInHierarchy)
            return;

        if (boundChampion != active)
        {
            boundChampion = active;
            BindToActiveChampion(true);
        }
        else
        {
            BindToActiveChampion(false);
        }
    }

    private void ResolveCamera(bool snap)
    {
        gameplayCamera = Camera.main;
        if (gameplayCamera == null)
            return;

        controller = gameplayCamera.GetComponent<AOGMobaCameraController>();
        if (controller == null)
            controller = gameplayCamera.gameObject.AddComponent<AOGMobaCameraController>();

        ApplyCompetitiveSettings();
        if (snap)
            BindToActiveChampion(true);
    }

    private void BindToActiveChampion(bool snap)
    {
        if (controller == null)
            ResolveCamera(false);

        AOGActiveChampion active = AOGActiveChampion.Current;
        if (controller == null || active == null || !active.IsActiveChampion)
            return;

        ApplyCompetitiveSettings();

        if (controller.target != active.transform)
            controller.SetTarget(active.transform, snap);

        if (AOGInputBridge.KeyPressedThisFrame(KeyCode.Space))
            controller.SetTarget(active.transform, true);
    }

    private void ApplyCompetitiveSettings()
    {
        if (controller == null)
            return;

        controller.autoFindLyra = false;
        controller.pitch = 57f;
        controller.yaw = 45f;
        controller.edgePanEnabled = false;
        controller.edgePanSpeed = 0f;
        controller.maxPanDistanceFromTarget = 0f;
        controller.panReturnSpeed = 100f;
        controller.middleDragEnabled = false;
        controller.forwardFramingBias = 0.50f;
        controller.targetLookAhead = 0.05f;
        controller.maxLookAheadDistance = 0.65f;
        controller.defaultZoom = 30f;
        controller.minZoom = 20f;
        controller.maxZoom = 48f;
        controller.zoomStep = 3.4f;
        controller.fieldOfView = 50f;

        gameplayCamera.orthographic = false;
        gameplayCamera.fieldOfView = 50f;
        gameplayCamera.nearClipPlane = 0.08f;
        gameplayCamera.farClipPlane = Mathf.Max(gameplayCamera.farClipPlane, 1000f);
    }
}
