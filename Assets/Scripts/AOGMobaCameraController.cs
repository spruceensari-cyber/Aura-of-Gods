using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public class AOGMobaCameraController : MonoBehaviour
{
    [Header("Target")]
    public Transform target;
    public bool autoFindLyra = true;
    public KeyCode focusKey = KeyCode.Space;

    [Header("MOBA Framing")]
    [Range(35f, 75f)] public float pitch = 57f;
    [Range(-180f, 180f)] public float yaw = 45f;
    [Range(30f, 60f)] public float fieldOfView = 48f;
    public float defaultZoom = 28f;
    public float minZoom = 16f;
    public float maxZoom = 46f;
    public float forwardFramingBias = 0.6f;

    [Header("Follow")]
    public float followSmoothTime = 0.10f;
    public float rotationSmoothSpeed = 14f;
    public float targetLookAhead = 0.10f;
    public float maxLookAheadDistance = 1.6f;

    [Header("Free Pan")]
    public bool edgePanEnabled = true;
    [Range(2f, 60f)] public float edgeBorderPixels = 14f;
    public float edgePanSpeed = 18f;
    public float middleDragSpeed = 0.035f;
    public float maxPanDistanceFromTarget = 36f;
    public float panReturnSpeed = 12f;

    [Header("Zoom")]
    public float zoomStep = 3.2f;
    public float zoomSmoothTime = 0.08f;

    [Header("Impact Feel")]
    public float impulseDecay = 12f;
    public float maxImpulse = 0.45f;

    private Camera controlledCamera;
    private Vector3 followVelocity;
    private float zoomVelocity;
    private float currentZoom;
    private float desiredZoom;
    private Vector3 panOffset;
    private Vector3 targetLastPosition;
    private Vector3 targetPlanarVelocity;
    private Vector3 impulseOffset;
    private Vector2 dragStartMouse;
    private bool dragging;
    private float nextTargetSearchTime;
    private bool snappedOnce;

    private void Awake()
    {
        controlledCamera = GetComponent<Camera>();
        controlledCamera.orthographic = false;
        controlledCamera.fieldOfView = fieldOfView;
        controlledCamera.nearClipPlane = Mathf.Min(controlledCamera.nearClipPlane, 0.15f);
        controlledCamera.farClipPlane = Mathf.Max(controlledCamera.farClipPlane, 700f);

        currentZoom = Mathf.Clamp(defaultZoom, minZoom, maxZoom);
        desiredZoom = currentZoom;
    }

    private void Start()
    {
        DisableLegacyControllerOnSameCamera();
        TryFindTarget(true);

        if (target != null)
        {
            targetLastPosition = target.position;
            SnapToTarget();
        }
    }

    private void Update()
    {
        TryFindTarget(false);
        HandleZoomInput();
        HandlePanInput();
        HandleFocusInput();
        UpdateTargetVelocity();
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        currentZoom = Mathf.SmoothDamp(currentZoom, desiredZoom, ref zoomVelocity, zoomSmoothTime);

        Quaternion desiredRotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 planarForward = desiredRotation * Vector3.forward;
        planarForward.y = 0f;
        if (planarForward.sqrMagnitude < 0.001f)
            planarForward = Vector3.forward;
        planarForward.Normalize();

        Vector3 lookAhead = Vector3.ClampMagnitude(targetPlanarVelocity * targetLookAhead, maxLookAheadDistance);
        Vector3 focusPosition = target.position + lookAhead + planarForward * forwardFramingBias + panOffset;
        Vector3 desiredPosition = focusPosition + desiredRotation * Vector3.back * currentZoom;

        impulseOffset = Vector3.Lerp(impulseOffset, Vector3.zero, impulseDecay * Time.deltaTime);
        desiredPosition += impulseOffset;

        if (!snappedOnce)
        {
            transform.position = desiredPosition;
            transform.rotation = desiredRotation;
            snappedOnce = true;
            return;
        }

        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref followVelocity, followSmoothTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationSmoothSpeed * Time.deltaTime);
    }

    public void SetTarget(Transform newTarget, bool snap = true)
    {
        target = newTarget;
        panOffset = Vector3.zero;
        followVelocity = Vector3.zero;

        if (target != null)
        {
            targetLastPosition = target.position;
            targetPlanarVelocity = Vector3.zero;
            if (snap)
                SnapToTarget();
        }
    }

    public void SnapToTarget()
    {
        if (target == null)
            return;

        panOffset = Vector3.zero;
        currentZoom = desiredZoom = Mathf.Clamp(defaultZoom, minZoom, maxZoom);
        followVelocity = Vector3.zero;
        zoomVelocity = 0f;
        snappedOnce = false;
    }

    public void AddImpulse(Vector3 worldDirection, float strength)
    {
        if (strength <= 0f)
            return;

        Vector3 direction = worldDirection.sqrMagnitude > 0.001f ? worldDirection.normalized : Random.insideUnitSphere.normalized;
        direction.y = Mathf.Clamp(direction.y, -0.35f, 0.35f);
        impulseOffset += direction * Mathf.Min(strength, maxImpulse);
        impulseOffset = Vector3.ClampMagnitude(impulseOffset, maxImpulse);
    }

    public void AddRandomImpulse(float strength)
    {
        Vector2 random = Random.insideUnitCircle.normalized;
        AddImpulse(new Vector3(random.x, 0.18f, random.y), strength);
    }

    private void HandleZoomInput()
    {
        float scroll = AOGInputBridge.ScrollDelta.y;
        if (Mathf.Abs(scroll) < 0.01f)
            return;

        desiredZoom = Mathf.Clamp(desiredZoom - scroll * zoomStep, minZoom, maxZoom);
    }

    private void HandlePanInput()
    {
        if (target == null)
            return;

        bool pointerOverUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        if (AOGInputBridge.MiddlePressedThisFrame())
        {
            dragging = true;
            dragStartMouse = AOGInputBridge.PointerPosition;
        }

        if (AOGInputBridge.MiddleReleasedThisFrame())
            dragging = false;

        Vector3 panDelta = Vector3.zero;

        if (dragging && AOGInputBridge.MiddleIsPressed())
        {
            Vector2 pointer = AOGInputBridge.PointerPosition;
            Vector2 mouseDelta = pointer - dragStartMouse;
            dragStartMouse = pointer;

            Vector3 right = PlanarRight();
            Vector3 forward = PlanarForward();
            float zoomMultiplier = Mathf.Lerp(0.8f, 1.35f, Mathf.InverseLerp(minZoom, maxZoom, currentZoom));
            panDelta += (-right * mouseDelta.x - forward * mouseDelta.y) * middleDragSpeed * zoomMultiplier;
        }
        else if (edgePanEnabled && !pointerOverUi && Application.isFocused)
        {
            Vector2 mouse = AOGInputBridge.PointerPosition;
            Vector2 input = Vector2.zero;

            if (mouse.x <= edgeBorderPixels) input.x -= 1f;
            if (mouse.x >= Screen.width - edgeBorderPixels) input.x += 1f;
            if (mouse.y <= edgeBorderPixels) input.y -= 1f;
            if (mouse.y >= Screen.height - edgeBorderPixels) input.y += 1f;

            if (input.sqrMagnitude > 0.01f)
            {
                input.Normalize();
                float zoomMultiplier = Mathf.Lerp(0.85f, 1.35f, Mathf.InverseLerp(minZoom, maxZoom, currentZoom));
                panDelta += (PlanarRight() * input.x + PlanarForward() * input.y) * edgePanSpeed * zoomMultiplier * Time.unscaledDeltaTime;
            }
        }

        panOffset += panDelta;
        panOffset.y = 0f;
        panOffset = Vector3.ClampMagnitude(panOffset, maxPanDistanceFromTarget);
    }

    private void HandleFocusInput()
    {
        if (target == null)
            return;

        if (AOGInputBridge.KeyIsPressed(focusKey))
        {
            panOffset = Vector3.MoveTowards(panOffset, Vector3.zero, panReturnSpeed * Time.unscaledDeltaTime);
            if (panOffset.sqrMagnitude < 0.01f)
                panOffset = Vector3.zero;
        }
    }

    private void UpdateTargetVelocity()
    {
        if (target == null)
            return;

        float dt = Mathf.Max(Time.deltaTime, 0.0001f);
        Vector3 velocity = (target.position - targetLastPosition) / dt;
        velocity.y = 0f;
        targetLastPosition = target.position;
        targetPlanarVelocity = Vector3.Lerp(targetPlanarVelocity, velocity, 10f * Time.deltaTime);
    }

    private void TryFindTarget(bool immediate)
    {
        if (target != null || !autoFindLyra)
            return;

        if (!immediate && Time.unscaledTime < nextTargetSearchTime)
            return;

        nextTargetSearchTime = Time.unscaledTime + 0.5f;

        AOGPlayerMOBAController[] players = FindObjectsByType<AOGPlayerMOBAController>(FindObjectsSortMode.None);
        Transform fallback = null;

        foreach (AOGPlayerMOBAController player in players)
        {
            if (player == null || !player.gameObject.activeInHierarchy)
                continue;

            if (fallback == null)
                fallback = player.transform;

            if (player.gameObject.name.ToLowerInvariant().Contains("lyra"))
            {
                SetTarget(player.transform, true);
                return;
            }
        }

        if (fallback != null)
            SetTarget(fallback, true);
    }

    private Vector3 PlanarRight()
    {
        Vector3 right = transform.right;
        right.y = 0f;
        return right.sqrMagnitude > 0.001f ? right.normalized : Vector3.right;
    }

    private Vector3 PlanarForward()
    {
        Vector3 forward = transform.forward;
        forward.y = 0f;
        return forward.sqrMagnitude > 0.001f ? forward.normalized : Vector3.forward;
    }

    private void DisableLegacyControllerOnSameCamera()
    {
        CameraController legacy = GetComponent<CameraController>();
        if (legacy != null)
            legacy.enabled = false;
    }
}
