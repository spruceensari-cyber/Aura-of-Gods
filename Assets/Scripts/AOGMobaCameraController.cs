using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public class AOGMobaCameraController : MonoBehaviour
{
    [Header("Target")]
    public Transform target;
    public bool autoFindLyra = false;
    public KeyCode focusKey = KeyCode.Space;

    [Header("Desktop MOBA Framing")]
    [Range(35f,75f)] public float pitch = 57f;
    [Range(-180f,180f)] public float yaw = 45f;
    [Range(30f,60f)] public float fieldOfView = 47f;
    public float defaultZoom = 30f;
    public float minZoom = 24f;
    public float maxZoom = 39f;
    public float forwardFramingBias = 0.85f;

    [Header("Follow")]
    public float followSmoothTime = 0.085f;
    public float rotationSmoothSpeed = 16f;
    public float targetLookAhead = 0.09f;
    public float maxLookAheadDistance = 1.4f;

    [Header("Free Pan")]
    public bool edgePanEnabled = true;
    [Range(2f,60f)] public float edgeBorderPixels = 12f;
    public float edgePanSpeed = 17f;
    public float middleDragSpeed = 0.030f;
    public float maxPanDistanceFromTarget = 30f;
    public float panReturnSpeed = 13f;

    [Header("Zoom")]
    public float zoomStep = 2.6f;
    public float zoomSmoothTime = 0.08f;

    [Header("Impact Feel")]
    public float impulseDecay = 13f;
    public float maxImpulse = 0.34f;

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
        controlledCamera.nearClipPlane = Mathf.Min(controlledCamera.nearClipPlane,0.15f);
        controlledCamera.farClipPlane = Mathf.Max(controlledCamera.farClipPlane,700f);
        controlledCamera.useOcclusionCulling = true;
        currentZoom = Mathf.Clamp(defaultZoom,minZoom,maxZoom);
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
        EnforceAuthoritativeTarget();
        TryFindTarget(false);
        HandleZoomInput();
        HandlePanInput();
        HandleFocusInput();
        UpdateTargetVelocity();
    }

    private void LateUpdate()
    {
        if (target == null) return;

        currentZoom = Mathf.SmoothDamp(currentZoom,desiredZoom,ref zoomVelocity,zoomSmoothTime);
        Quaternion desiredRotation = Quaternion.Euler(pitch,yaw,0f);
        Vector3 planarForward = desiredRotation*Vector3.forward;
        planarForward.y = 0f;
        if (planarForward.sqrMagnitude < 0.001f) planarForward = Vector3.forward;
        planarForward.Normalize();

        Vector3 lookAhead = Vector3.ClampMagnitude(targetPlanarVelocity*targetLookAhead,maxLookAheadDistance);
        Vector3 focusPosition = target.position+lookAhead+planarForward*forwardFramingBias+panOffset;
        Vector3 desiredPosition = focusPosition+desiredRotation*Vector3.back*currentZoom;

        impulseOffset = Vector3.Lerp(impulseOffset,Vector3.zero,impulseDecay*Time.deltaTime);
        desiredPosition += impulseOffset;

        if (!snappedOnce)
        {
            transform.position = desiredPosition;
            transform.rotation = desiredRotation;
            snappedOnce = true;
            return;
        }

        transform.position = Vector3.SmoothDamp(transform.position,desiredPosition,ref followVelocity,followSmoothTime);
        transform.rotation = Quaternion.Slerp(transform.rotation,desiredRotation,rotationSmoothSpeed*Time.deltaTime);
    }

    public void SetTarget(Transform newTarget,bool snap = true)
    {
        AOGActiveChampion authoritative = AOGPlayerChampionAuthority.CurrentChampion;
        if (authoritative != null && newTarget != authoritative.transform)
            newTarget = authoritative.transform;

        target = newTarget;
        panOffset = Vector3.zero;
        followVelocity = Vector3.zero;
        if (target == null) return;
        targetLastPosition = target.position;
        targetPlanarVelocity = Vector3.zero;
        if (snap) SnapToTarget();
    }

    public void SnapToTarget()
    {
        if (target == null) return;
        panOffset = Vector3.zero;
        currentZoom = desiredZoom = Mathf.Clamp(defaultZoom,minZoom,maxZoom);
        followVelocity = Vector3.zero;
        zoomVelocity = 0f;
        snappedOnce = false;
    }

    public void AddImpulse(Vector3 worldDirection,float strength)
    {
        if (strength <= 0f) return;
        Vector3 direction = worldDirection.sqrMagnitude > 0.001f ? worldDirection.normalized : UnityEngine.Random.insideUnitSphere.normalized;
        direction.y = Mathf.Clamp(direction.y,-0.35f,0.35f);
        impulseOffset += direction*Mathf.Min(strength,maxImpulse);
        impulseOffset = Vector3.ClampMagnitude(impulseOffset,maxImpulse);
    }

    public void AddRandomImpulse(float strength)
    {
        Vector2 random = UnityEngine.Random.insideUnitCircle.normalized;
        AddImpulse(new Vector3(random.x,0.18f,random.y),strength);
    }

    private void EnforceAuthoritativeTarget()
    {
        AOGActiveChampion authoritative = AOGPlayerChampionAuthority.CurrentChampion;
        if (authoritative == null || !authoritative.gameObject.activeInHierarchy) return;
        if (target != authoritative.transform) SetTarget(authoritative.transform,true);
    }

    private void HandleZoomInput()
    {
        float scroll = AOGInputBridge.ScrollDelta.y;
        if (Mathf.Abs(scroll) < 0.01f) return;
        desiredZoom = Mathf.Clamp(desiredZoom-scroll*zoomStep,minZoom,maxZoom);
    }

    private void HandlePanInput()
    {
        if (target == null) return;
        bool pointerOverUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        if (AOGInputBridge.MiddlePressedThisFrame())
        {
            dragging = true;
            dragStartMouse = AOGInputBridge.PointerPosition;
        }
        if (AOGInputBridge.MiddleReleasedThisFrame()) dragging = false;

        Vector3 panDelta = Vector3.zero;
        if (dragging && AOGInputBridge.MiddleIsPressed())
        {
            Vector2 pointer = AOGInputBridge.PointerPosition;
            Vector2 mouseDelta = pointer-dragStartMouse;
            dragStartMouse = pointer;
            float zoomMultiplier = Mathf.Lerp(0.8f,1.25f,Mathf.InverseLerp(minZoom,maxZoom,currentZoom));
            panDelta += (-PlanarRight()*mouseDelta.x-PlanarForward()*mouseDelta.y)*middleDragSpeed*zoomMultiplier;
        }
        else if (edgePanEnabled && !pointerOverUi && Application.isFocused)
        {
            Vector2 mouse = AOGInputBridge.PointerPosition;
            Vector2 input = Vector2.zero;
            if (mouse.x <= edgeBorderPixels) input.x -= 1f;
            if (mouse.x >= Screen.width-edgeBorderPixels) input.x += 1f;
            if (mouse.y <= edgeBorderPixels) input.y -= 1f;
            if (mouse.y >= Screen.height-edgeBorderPixels) input.y += 1f;
            if (input.sqrMagnitude > 0.01f)
            {
                input.Normalize();
                float zoomMultiplier = Mathf.Lerp(0.85f,1.25f,Mathf.InverseLerp(minZoom,maxZoom,currentZoom));
                panDelta += (PlanarRight()*input.x+PlanarForward()*input.y)*edgePanSpeed*zoomMultiplier*Time.unscaledDeltaTime;
            }
        }

        panOffset += panDelta;
        panOffset.y = 0f;
        panOffset = Vector3.ClampMagnitude(panOffset,maxPanDistanceFromTarget);
    }

    private void HandleFocusInput()
    {
        if (target == null) return;
        if (!AOGInputBridge.KeyIsPressed(focusKey)) return;
        panOffset = Vector3.MoveTowards(panOffset,Vector3.zero,panReturnSpeed*Time.unscaledDeltaTime);
        if (panOffset.sqrMagnitude < 0.01f) panOffset = Vector3.zero;
    }

    private void UpdateTargetVelocity()
    {
        if (target == null) return;
        float dt = Mathf.Max(Time.deltaTime,0.0001f);
        Vector3 velocity = (target.position-targetLastPosition)/dt;
        velocity.y = 0f;
        targetLastPosition = target.position;
        targetPlanarVelocity = Vector3.Lerp(targetPlanarVelocity,velocity,10f*Time.deltaTime);
    }

    private void TryFindTarget(bool immediate)
    {
        if (target != null) return;
        if (!immediate && Time.unscaledTime < nextTargetSearchTime) return;
        nextTargetSearchTime = Time.unscaledTime+0.35f;

        AOGActiveChampion authoritative = AOGPlayerChampionAuthority.CurrentChampion;
        if (authoritative != null && authoritative.gameObject.activeInHierarchy)
        {
            SetTarget(authoritative.transform,true);
            return;
        }

        AOGActiveChampion active = AOGActiveChampion.Current;
        if (active != null && active.IsActiveChampion && active.gameObject.activeInHierarchy)
            target = active.transform;
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
        if (legacy != null) legacy.enabled = false;
    }
}
