using UnityEngine;

public class AOGMobaCameraController : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Camera Position")]
    public Vector3 offset = new Vector3(0f, 30f, -24f);
    public float followSpeed = 10f;
    public float panSpeed = 24f;
    public float edgePanBorder = 18f;
    public float dragPanSpeed = 0.08f;

    [Header("Rotation")]
    public float pitch = 56f;
    public float yaw = 0f;

    [Header("Zoom")]
    public float zoomSpeed = 5.5f;
    public float minZoom = 18f;
    public float maxZoom = 40f;
    public float combatZoomOut = 4.5f;
    public float combatDetectionRadius = 13f;

    [Header("Controls")]
    public KeyCode focusKey = KeyCode.Space;
    public KeyCode dragKey = KeyCode.Mouse2;

    private float currentZoom;
    private Vector3 freePanOffset;
    private Vector3 lastMousePosition;
    private bool dragging;

    void Start()
    {
        currentZoom = offset.y;
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    void LateUpdate()
    {
        ResolveTarget();
        if (target == null)
            return;

        HandleZoom();
        HandlePan();
        HandleFocus();
        UpdateCameraPosition();
    }

    private void ResolveTarget()
    {
        if (target != null)
            return;

        ChampionController controller = FindObjectOfType<ChampionController>();
        if (controller != null)
            target = controller.transform;
    }

    private void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.001f)
            currentZoom = Mathf.Clamp(currentZoom - scroll * zoomSpeed * 10f, minZoom, maxZoom);
    }

    private void HandlePan()
    {
        Vector3 pan = Vector3.zero;
        Vector3 mouse = Input.mousePosition;

        if (mouse.x <= edgePanBorder) pan.x -= 1f;
        else if (mouse.x >= Screen.width - edgePanBorder) pan.x += 1f;

        if (mouse.y <= edgePanBorder) pan.z -= 1f;
        else if (mouse.y >= Screen.height - edgePanBorder) pan.z += 1f;

        if (pan.sqrMagnitude > 0f)
            freePanOffset += pan.normalized * panSpeed * Time.unscaledDeltaTime;

        if (Input.GetKeyDown(dragKey))
        {
            dragging = true;
            lastMousePosition = mouse;
        }

        if (Input.GetKeyUp(dragKey))
            dragging = false;

        if (dragging)
        {
            Vector3 delta = mouse - lastMousePosition;
            lastMousePosition = mouse;
            freePanOffset += new Vector3(-delta.x, 0f, -delta.y) * dragPanSpeed;
        }

        freePanOffset = Vector3.ClampMagnitude(freePanOffset, 32f);
    }

    private void HandleFocus()
    {
        if (Input.GetKey(focusKey))
            freePanOffset = Vector3.Lerp(freePanOffset, Vector3.zero, Time.unscaledDeltaTime * 10f);
    }

    private void UpdateCameraPosition()
    {
        float dynamicZoom = DetectCombatPressure() ? combatZoomOut : 0f;
        float zoom = Mathf.Clamp(currentZoom + dynamicZoom, minZoom, maxZoom + combatZoomOut);

        Vector3 desiredOffset = new Vector3(offset.x, zoom, -zoom * 0.82f);
        Vector3 desiredPosition = target.position + freePanOffset + desiredOffset;

        transform.position = Vector3.Lerp(transform.position, desiredPosition, 1f - Mathf.Exp(-followSpeed * Time.unscaledDeltaTime));
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(pitch, yaw, 0f), Time.unscaledDeltaTime * 8f);
    }

    private bool DetectCombatPressure()
    {
        if (target == null)
            return false;

        Champion local = target.GetComponent<Champion>();
        Collider[] hits = Physics.OverlapSphere(target.position, combatDetectionRadius);
        foreach (Collider hit in hits)
        {
            Champion other = hit.GetComponentInParent<Champion>();
            if (other != null && other != local && other.IsAlive && (local == null || other.Team != local.Team))
                return true;
        }

        return false;
    }
}
