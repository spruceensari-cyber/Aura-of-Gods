using UnityEngine;

/// <summary>
/// Primary MOBA camera for the rebuild slice. Uses the shared input bridge, allocation-free combat detection,
/// smooth zoom and stable edge/drag pan.
/// </summary>
public class AOGMobaCameraController : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Camera Position")]
    public Vector3 offset = new Vector3(0f, 30f, -24f);
    public float followSpeed = 11f;
    public float panSpeed = 20f;
    public float edgePanBorder = 14f;
    public float dragPanSpeed = 0.065f;

    [Header("Rotation")]
    public float pitch = 56f;
    public float yaw = 0f;

    [Header("Zoom")]
    public float zoomSpeed = 4.5f;
    public float minZoom = 20f;
    public float maxZoom = 38f;
    public float combatZoomOut = 3f;
    public float combatDetectionRadius = 13f;

    private readonly Collider[] combatHits = new Collider[32];
    private float currentZoom;
    private float targetZoom;
    private Vector3 freePanOffset;
    private Vector3 lastMousePosition;
    private bool dragging;

    void Awake()
    {
        CameraController legacy = GetComponent<CameraController>();
        if (legacy != null && legacy.enabled)
            legacy.enabled = false;
    }

    void Start()
    {
        currentZoom = Mathf.Clamp(offset.y, minZoom, maxZoom);
        targetZoom = currentZoom;
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);

        Camera cam = GetComponent<Camera>();
        if (cam != null)
        {
            cam.nearClipPlane = 0.15f;
            cam.farClipPlane = Mathf.Max(cam.farClipPlane, 600f);
            cam.allowHDR = true;
            cam.allowMSAA = true;
        }
    }

    void LateUpdate()
    {
        ResolveTarget();
        if (target == null) return;

        HandleZoom();
        HandlePan();
        HandleFocus();
        UpdateCameraPosition();
    }

    private void ResolveTarget()
    {
        if (target != null && target.gameObject.activeInHierarchy) return;
        ChampionController controller = FindObjectOfType<ChampionController>();
        if (controller != null) target = controller.transform;
    }

    private void HandleZoom()
    {
        float scroll = AOGInputBridge.ScrollY;
        if (Mathf.Abs(scroll) > 0.001f)
            targetZoom = Mathf.Clamp(targetZoom - scroll * zoomSpeed * 2.2f, minZoom, maxZoom);

        currentZoom = Mathf.Lerp(currentZoom, targetZoom, 1f - Mathf.Exp(-10f * Time.unscaledDeltaTime));
    }

    private void HandlePan()
    {
        Vector3 pan = Vector3.zero;
        Vector2 mouse = AOGInputBridge.PointerPosition;

        bool insideWindow = mouse.x >= 0f && mouse.x <= Screen.width && mouse.y >= 0f && mouse.y <= Screen.height;
        if (insideWindow && !dragging)
        {
            if (mouse.x <= edgePanBorder) pan.x -= 1f;
            else if (mouse.x >= Screen.width - edgePanBorder) pan.x += 1f;

            if (mouse.y <= edgePanBorder) pan.z -= 1f;
            else if (mouse.y >= Screen.height - edgePanBorder) pan.z += 1f;
        }

        if (pan.sqrMagnitude > 0f)
            freePanOffset += pan.normalized * panSpeed * Time.unscaledDeltaTime;

        if (AOGInputBridge.MiddleClickPressed)
        {
            dragging = true;
            lastMousePosition = mouse;
        }

        if (AOGInputBridge.MiddleClickReleased)
            dragging = false;

        if (dragging)
        {
            Vector3 delta = (Vector3)mouse - lastMousePosition;
            lastMousePosition = mouse;
            freePanOffset += new Vector3(-delta.x, 0f, -delta.y) * dragPanSpeed;
        }

        freePanOffset = Vector3.ClampMagnitude(freePanOffset, 30f);
    }

    private void HandleFocus()
    {
        if (AOGInputBridge.FocusHeld)
            freePanOffset = Vector3.Lerp(freePanOffset, Vector3.zero, 1f - Mathf.Exp(-12f * Time.unscaledDeltaTime));
    }

    private void UpdateCameraPosition()
    {
        float dynamicZoom = DetectCombatPressure() ? combatZoomOut : 0f;
        float zoom = Mathf.Clamp(currentZoom + dynamicZoom, minZoom, maxZoom + combatZoomOut);

        Vector3 desiredOffset = new Vector3(offset.x, zoom, -zoom * 0.82f);
        Vector3 desiredPosition = target.position + freePanOffset + desiredOffset;

        transform.position = Vector3.Lerp(transform.position, desiredPosition, 1f - Mathf.Exp(-followSpeed * Time.unscaledDeltaTime));
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(pitch, yaw, 0f), 1f - Mathf.Exp(-8f * Time.unscaledDeltaTime));
    }

    private bool DetectCombatPressure()
    {
        if (target == null) return false;

        Champion local = target.GetComponent<Champion>();
        int hitCount = Physics.OverlapSphereNonAlloc(target.position, combatDetectionRadius, combatHits);
        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = combatHits[i];
            if (hit == null) continue;
            Champion other = hit.GetComponentInParent<Champion>();
            if (other != null && other != local && other.IsAlive && (local == null || other.Team != local.Team))
                return true;
        }
        return false;
    }
}
