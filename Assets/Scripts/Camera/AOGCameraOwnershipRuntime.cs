using UnityEngine;

[DefaultExecutionOrder(-1300)]
public class AOGCameraOwnershipRuntime : MonoBehaviour
{
    private static AOGCameraOwnershipRuntime instance;
    private bool legacyAuthorityDisabled;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (instance != null)
            return;
        GameObject host = new GameObject("AOG_Camera_Ownership_Runtime");
        instance = host.AddComponent<AOGCameraOwnershipRuntime>();
        DontDestroyOnLoad(host);
    }

    private void Update()
    {
        if (!legacyAuthorityDisabled)
        {
            foreach (AOGActiveChampionCameraAuthority legacy in FindObjectsByType<AOGActiveChampionCameraAuthority>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (legacy != null)
                    legacy.enabled = false;
            }
            legacyAuthorityDisabled = true;
        }

        Camera camera = Camera.main;
        AOGActiveChampion player = AOGPlayerChampionAuthority.CurrentChampion;
        if (camera == null)
            return;

        AOGMobaCameraController controller = camera.GetComponent<AOGMobaCameraController>();
        if (controller == null)
            controller = camera.gameObject.AddComponent<AOGMobaCameraController>();

        controller.autoFindLyra = false;
        controller.edgePanEnabled = false;
        controller.edgePanSpeed = 0f;
        controller.middleDragSpeed = 0f;
        controller.maxPanDistanceFromTarget = 0f;
        controller.panReturnSpeed = 100f;
        controller.pitch = 57f;
        controller.yaw = 45f;
        controller.fieldOfView = 50f;
        controller.defaultZoom = 30f;
        controller.minZoom = 20f;
        controller.maxZoom = 48f;
        controller.forwardFramingBias = 0.55f;
        controller.targetLookAhead = 0.06f;
        controller.maxLookAheadDistance = 0.8f;

        camera.orthographic = false;
        camera.fieldOfView = 50f;
        camera.nearClipPlane = Mathf.Min(camera.nearClipPlane, 0.08f);
        camera.farClipPlane = Mathf.Max(camera.farClipPlane, 1000f);

        if (player != null && player.gameObject.activeInHierarchy && controller.target != player.transform)
            controller.SetTarget(player.transform, false);

        if (player != null && AOGInputBridge.KeyPressedThisFrame(KeyCode.Space))
            controller.SetTarget(player.transform, true);
    }
}
