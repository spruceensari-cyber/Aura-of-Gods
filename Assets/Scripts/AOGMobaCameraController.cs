using UnityEngine;

public class AOGMobaCameraController : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Camera Position")]
    public Vector3 offset = new Vector3(0f, 28f, -22f);
    public float followSpeed = 8f;

    [Header("Rotation")]
    public Vector3 rotation = new Vector3(58f, 0f, 0f);

    [Header("Zoom")]
    public float zoomSpeed = 6f;
    public float minZoom = 18f;
    public float maxZoom = 38f;

    [Header("Controls")]
    public KeyCode focusKey = KeyCode.Space;

    private float currentZoom;

    void Start()
    {
        currentZoom = offset.y;

        transform.rotation = Quaternion.Euler(rotation);
    }

    void LateUpdate()
    {
        if (target == null)
            return;

        HandleZoom();

        Vector3 desiredOffset = offset;
        desiredOffset.y = currentZoom;
        desiredOffset.z = -currentZoom * 0.8f;

        Vector3 desiredPosition = target.position + desiredOffset;

        transform.position = Vector3.Lerp(
            transform.position,
            desiredPosition,
            followSpeed * Time.deltaTime
        );

        transform.rotation = Quaternion.Euler(rotation);

        if (Input.GetKey(focusKey))
        {
            transform.position = desiredPosition;
        }
    }

    void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (Mathf.Abs(scroll) > 0.01f)
        {
            currentZoom -= scroll * zoomSpeed * 10f;
            currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);
        }
    }
}