using UnityEngine;

public class AOGMobaCameraController : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Camera Position")]
    public Vector3 offset = new Vector3(0f, 36f, -30f);
    public float followSpeed = 8f;

    [Header("Rotation")]
    public Vector3 rotation = new Vector3(60f, 0f, 0f);

    [Header("Zoom")]
    public bool useOrthographic = true;
    public float orthographicSize = 25f;
    public float zoomSpeed = 6f;
    public float minZoom = 18f;
    public float maxZoom = 32f;

    [Header("Controls")]
    public KeyCode focusKey = KeyCode.Space;

    private Camera cameraComponent;
    private float currentZoom;

    void Start()
    {
        cameraComponent = GetComponent<Camera>();
        if (target == null)
            target = FindPlayerTarget();

        currentZoom = useOrthographic ? orthographicSize : offset.y;
        ApplyProjection();
        transform.rotation = Quaternion.Euler(rotation);
    }

    void LateUpdate()
    {
        if (target == null)
            target = FindPlayerTarget();

        if (target == null)
            return;

        HandleZoom();
        ApplyProjection();

        Vector3 desiredOffset = offset;
        if (!useOrthographic)
        {
            desiredOffset.y = currentZoom;
            desiredOffset.z = -currentZoom * 0.82f;
        }

        Vector3 desiredPosition = target.position + desiredOffset;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Euler(rotation);

        if (Input.GetKey(focusKey))
            transform.position = desiredPosition;
    }

    private Transform FindPlayerTarget()
    {
        GameObject tagged = GameObject.FindGameObjectWithTag("Player");
        if (tagged != null)
            return tagged.transform;

        AOGPlayerMOBAController player = Object.FindAnyObjectByType<AOGPlayerMOBAController>();
        if (player != null)
            return player.transform;

        AOGCharacterStats stats = Object.FindAnyObjectByType<AOGCharacterStats>();
        return stats != null ? stats.transform : null;
    }

    private void ApplyProjection()
    {
        if (cameraComponent == null)
            cameraComponent = GetComponent<Camera>();

        if (cameraComponent == null)
            return;

        cameraComponent.orthographic = useOrthographic;
        if (useOrthographic)
            cameraComponent.orthographicSize = currentZoom;

        cameraComponent.nearClipPlane = 0.1f;
        cameraComponent.farClipPlane = 600f;
    }

    private void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) <= 0.01f)
            return;

        currentZoom -= scroll * zoomSpeed * 4f;
        currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);
    }
}
