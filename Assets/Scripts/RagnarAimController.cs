using UnityEngine;

public class RagnarAimController : MonoBehaviour
{
    private enum AimSkill
    {
        None,
        Q,
        E,
        R
    }

    [Header("References")]
    public RagnarSkillSet skills;
    public AOGPlayerMOBAController movementController;
    public Camera mainCamera;

    [Header("Quick Cast")]
    [Tooltip("Shift + skill: indicator appears while key is held, cast happens on release.")]
    public bool shiftQuickCastWithIndicator = true;

    [Header("Indicator")]
    public Color indicatorColor = new Color(1f, 0.35f, 0.05f, 1f);
    public int circleSegments = 48;
    public float lineWidth = 0.08f;

    private AimSkill pendingSkill = AimSkill.None;

    private bool quickCastHeld;
    private KeyCode quickCastKey = KeyCode.None;

    private LineRenderer rangeRing;
    private LineRenderer targetRing;
    private LineRenderer directionLine;
    private LineRenderer coneLine;

    private Material indicatorMaterial;

    void Awake()
    {
        if (skills == null)
            skills = GetComponent<RagnarSkillSet>();

        if (movementController == null)
            movementController = GetComponent<AOGPlayerMOBAController>();

        if (mainCamera == null)
            mainCamera = Camera.main;

        CreateIndicators();
        HideAll();
    }

    void Update()
    {
        HandleInstantSkill();
        HandleSkillInput();

        if (pendingSkill != AimSkill.None)
        {
            UpdateAiming();
        }
    }

    // =========================================================
    // INPUT
    // =========================================================

    void HandleInstantSkill()
    {
        if (!Input.GetKeyDown(KeyCode.W))
            return;

        CancelAim();

        if (skills != null)
            skills.TryCastW();
    }

    void HandleSkillInput()
    {
        if (Input.GetKeyDown(KeyCode.Q))
            HandleSkillPressed(AimSkill.Q, KeyCode.Q);

        if (Input.GetKeyDown(KeyCode.E))
            HandleSkillPressed(AimSkill.E, KeyCode.E);

        if (Input.GetKeyDown(KeyCode.R))
            HandleSkillPressed(AimSkill.R, KeyCode.R);

        if (quickCastHeld)
        {
            if (Input.GetKeyUp(quickCastKey))
            {
                Vector3 point = GetMouseGroundPoint();

                CastPendingSkill(point);
                EndAim();
            }
        }
    }

    void HandleSkillPressed(AimSkill skill, KeyCode key)
    {
        bool shiftPressed =
            Input.GetKey(KeyCode.LeftShift) ||
            Input.GetKey(KeyCode.RightShift);

        if (shiftPressed && shiftQuickCastWithIndicator)
        {
            BeginQuickCast(skill, key);
        }
        else
        {
            BeginNormalAim(skill);
        }
    }

    // =========================================================
    // AIM MODES
    // =========================================================

    void BeginNormalAim(AimSkill skill)
    {
        pendingSkill = skill;

        quickCastHeld = false;
        quickCastKey = KeyCode.None;

        ShowCorrectIndicators();
    }

    void BeginQuickCast(AimSkill skill, KeyCode key)
    {
        pendingSkill = skill;

        quickCastHeld = true;
        quickCastKey = key;

        ShowCorrectIndicators();
    }

    void UpdateAiming()
    {
        Vector3 mousePoint = GetMouseGroundPoint();

        UpdateIndicatorPositions(mousePoint);

        // NORMAL CAST:
        // left click confirms.
        if (!quickCastHeld && Input.GetMouseButtonDown(0))
        {
            CastPendingSkill(mousePoint);
            EndAim();
            return;
        }

        // RIGHT CLICK ALWAYS CANCELS.
        if (Input.GetMouseButtonDown(1))
        {
            CancelAim();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CancelAim();
        }
    }

    void CastPendingSkill(Vector3 point)
    {
        if (skills == null)
            return;

        switch (pendingSkill)
        {
            case AimSkill.Q:
                skills.TryCastQ(point);
                break;

            case AimSkill.E:
                skills.TryCastE(point);
                break;

            case AimSkill.R:
                skills.TryCastR(point);
                break;
        }
    }

    void CancelAim()
    {
        if (pendingSkill == AimSkill.None)
            return;

        EndAim();
    }

    void EndAim()
    {
        pendingSkill = AimSkill.None;

        quickCastHeld = false;
        quickCastKey = KeyCode.None;

        HideAll();
    }

    // =========================================================
    // MOUSE WORLD POSITION
    // =========================================================

    Vector3 GetMouseGroundPoint()
    {
        if (mainCamera == null)
        {
            return transform.position +
                   transform.forward * 4f;
        }

        Ray ray = mainCamera.ScreenPointToRay(
            Input.mousePosition
        );

        Plane plane = new Plane(
            Vector3.up,
            new Vector3(
                0f,
                transform.position.y,
                0f
            )
        );

        if (plane.Raycast(ray, out float distance))
        {
            return ray.GetPoint(distance);
        }

        return transform.position +
               transform.forward * 4f;
    }

    // =========================================================
    // INDICATOR UPDATE
    // =========================================================

    void UpdateIndicatorPositions(Vector3 mousePoint)
    {
        if (skills == null)
            return;

        switch (pendingSkill)
        {
            case AimSkill.Q:
            {
                Vector3 target = ClampPoint(
                    mousePoint,
                    skills.qRange
                );

                DrawRing(
                    rangeRing,
                    transform.position,
                    skills.qRange
                );

                DrawCone(
                    coneLine,
                    transform.position,
                    target,
                    skills.qRange,
                    skills.qAngle
                );

                break;
            }

            case AimSkill.E:
            {
                Vector3 target = ClampPoint(
                    mousePoint,
                    skills.eDistance
                );

                DrawRing(
                    rangeRing,
                    transform.position,
                    skills.eDistance
                );

                DrawDirectionLine(
                    transform.position,
                    target
                );

                DrawRing(
                    targetRing,
                    target,
                    skills.eHitRange
                );

                break;
            }

            case AimSkill.R:
            {
                Vector3 target = ClampPoint(
                    mousePoint,
                    skills.rCastRange
                );

                DrawRing(
                    rangeRing,
                    transform.position,
                    skills.rCastRange
                );

                DrawRing(
                    targetRing,
                    target,
                    skills.rRadius
                );

                break;
            }
        }
    }

    Vector3 ClampPoint(Vector3 point, float range)
    {
        Vector3 delta = point - transform.position;

        delta.y = 0f;

        if (delta.magnitude > range)
        {
            delta = delta.normalized * range;
        }

        Vector3 result = transform.position + delta;

        result.y = transform.position.y + 0.08f;

        return result;
    }

    // =========================================================
    // CREATE INDICATORS
    // =========================================================

    void CreateIndicators()
    {
        Shader shader = Shader.Find(
            "Universal Render Pipeline/Unlit"
        );

        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        if (shader != null)
        {
            indicatorMaterial = new Material(shader);

            if (indicatorMaterial.HasProperty("_BaseColor"))
            {
                indicatorMaterial.SetColor(
                    "_BaseColor",
                    indicatorColor
                );
            }
        }

        rangeRing = CreateLineRenderer(
            "Ragnar_Range_Ring"
        );

        targetRing = CreateLineRenderer(
            "Ragnar_Target_Ring"
        );

        directionLine = CreateLineRenderer(
            "Ragnar_Direction_Line"
        );

        coneLine = CreateLineRenderer(
            "Ragnar_Cone_Line"
        );
    }

    LineRenderer CreateLineRenderer(string objectName)
    {
        GameObject obj = new GameObject(objectName);

        obj.transform.SetParent(transform);
        obj.transform.localPosition = Vector3.zero;

        LineRenderer line =
            obj.AddComponent<LineRenderer>();

        if (indicatorMaterial != null)
        {
            line.material = indicatorMaterial;
        }

        line.startColor = indicatorColor;
        line.endColor = indicatorColor;

        line.startWidth = lineWidth;
        line.endWidth = lineWidth;

        line.useWorldSpace = true;
        line.loop = false;

        line.positionCount = 0;

        return line;
    }

    // =========================================================
    // DRAW RING
    // =========================================================

    void DrawRing(
        LineRenderer line,
        Vector3 center,
        float radius
    )
    {
        if (line == null)
            return;

        line.enabled = true;

        int segments = Mathf.Max(16, circleSegments);

        line.positionCount = segments + 1;

        float y = transform.position.y + 0.08f;

        for (int i = 0; i <= segments; i++)
        {
            float progress =
                (float)i / segments;

            float angle =
                progress * Mathf.PI * 2f;

            Vector3 point = new Vector3(
                center.x + Mathf.Cos(angle) * radius,
                y,
                center.z + Mathf.Sin(angle) * radius
            );

            line.SetPosition(i, point);
        }
    }

    // =========================================================
    // DRAW DIRECTION
    // =========================================================

    void DrawDirectionLine(
        Vector3 start,
        Vector3 end
    )
    {
        if (directionLine == null)
            return;

        directionLine.enabled = true;
        directionLine.positionCount = 2;

        start.y = transform.position.y + 0.10f;
        end.y = transform.position.y + 0.10f;

        directionLine.SetPosition(0, start);
        directionLine.SetPosition(1, end);
    }

    // =========================================================
    // DRAW CONE
    // =========================================================

    void DrawCone(
        LineRenderer line,
        Vector3 origin,
        Vector3 target,
        float range,
        float totalAngle
    )
    {
        if (line == null)
            return;

        line.enabled = true;

        Vector3 direction = target - origin;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
        {
            direction = transform.forward;
        }

        direction.Normalize();

        int arcSteps = 24;

        line.positionCount = arcSteps + 3;

        Vector3 elevatedOrigin =
            origin + Vector3.up * 0.10f;

        line.SetPosition(0, elevatedOrigin);

        for (int i = 0; i <= arcSteps; i++)
        {
            float progress =
                (float)i / arcSteps;

            float angle = Mathf.Lerp(
                -totalAngle * 0.5f,
                totalAngle * 0.5f,
                progress
            );

            Vector3 rotated =
                Quaternion.Euler(
                    0f,
                    angle,
                    0f
                ) * direction;

            Vector3 point =
                origin +
                rotated * range +
                Vector3.up * 0.10f;

            line.SetPosition(i + 1, point);
        }

        line.SetPosition(
            arcSteps + 2,
            elevatedOrigin
        );
    }

    // =========================================================
    // VISIBILITY
    // =========================================================

    void ShowCorrectIndicators()
    {
        HideAll();

        if (rangeRing != null)
            rangeRing.enabled = true;

        switch (pendingSkill)
        {
            case AimSkill.Q:
                if (coneLine != null)
                    coneLine.enabled = true;
                break;

            case AimSkill.E:
                if (directionLine != null)
                    directionLine.enabled = true;

                if (targetRing != null)
                    targetRing.enabled = true;
                break;

            case AimSkill.R:
                if (targetRing != null)
                    targetRing.enabled = true;
                break;
        }
    }

    void HideAll()
    {
        if (rangeRing != null)
            rangeRing.enabled = false;

        if (targetRing != null)
            targetRing.enabled = false;

        if (directionLine != null)
            directionLine.enabled = false;

        if (coneLine != null)
            coneLine.enabled = false;
    }

    void OnDisable()
    {
        HideAll();
    }

    void OnDestroy()
    {
        if (indicatorMaterial != null)
        {
            Destroy(indicatorMaterial);
        }
    }
}