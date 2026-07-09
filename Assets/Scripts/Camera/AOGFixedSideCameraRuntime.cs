using UnityEngine;

[DefaultExecutionOrder(-500)]
public class AOGFixedSideCameraRuntime : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        if (cam.GetComponent<AOGFixedSideCameraRuntime>() == null)
            cam.gameObject.AddComponent<AOGFixedSideCameraRuntime>();
    }

    private AOGMobaCameraController controller;

    private void Awake()
    {
        controller = GetComponent<AOGMobaCameraController>();
        ApplyLock();
    }

    private void LateUpdate()
    {
        if (controller == null) controller = GetComponent<AOGMobaCameraController>();
        ApplyLock();
    }

    private void ApplyLock()
    {
        if (controller == null) return;
        controller.pitch = 57f;
        controller.yaw = 45f;
        controller.edgePanEnabled = false;
        controller.maxPanDistanceFromTarget = 0f;
        controller.panReturnSpeed = 100f;
        controller.forwardFramingBias = 0.55f;
        controller.targetLookAhead = 0.06f;
        controller.maxLookAheadDistance = 0.75f;
        controller.defaultZoom = 30f;
        controller.minZoom = 20f;
        controller.maxZoom = 48f;
    }
}
