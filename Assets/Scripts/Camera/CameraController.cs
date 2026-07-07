using UnityEngine;

/// <summary>
Camera controller for spectator view - top-down isometric MOBA view
/// </summary>
public class CameraController : MonoBehaviour
{
    [SerializeField] private Transform followTarget;
    [SerializeField] private float height = 10f;
    [SerializeField] private float distance = 8f;
    [SerializeField] private float smoothSpeed = 5f;
    [SerializeField] private float zoomSpeed = 2f;
    [SerializeField] private float minZoom = 5f;
    [SerializeField] private float maxZoom = 20f;
    
    private float currentZoom;
    
    void Start()
    {
        currentZoom = height;
    }
    
    void LateUpdate()
    {
        HandleZoom();
        UpdateCameraPosition();
    }
    
    private void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        currentZoom -= scroll * zoomSpeed;
        currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);
    }
    
    private void UpdateCameraPosition()
    {
        if (followTarget == null) return;
        
        Vector3 targetPos = followTarget.position + new Vector3(-distance, currentZoom, -distance);
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * smoothSpeed);
        
        transform.LookAt(followTarget.position);
    }
}
