using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class AOGPlayerMOBAController : MonoBehaviour
{
    [Header("References")]
    public Camera mainCamera;
    public Animator animator;
    public ChampionPresentationController presentation;

    [Header("Movement")]
    public LayerMask groundMask = ~0;
    public float stopDistance = 0.35f;
    public bool allowLeftClickMove = true;
    public bool allowRightClickMove = true;
    public float maxClickRayDistance = 1000f;

    [Header("Auto Attack")]
    public bool autoAttackNearestEnemy = true;
    public float attackRangeTolerance = 0.8f;

    [Header("Click Feedback")]
    public bool showMoveClickIndicator = true;
    public Color moveClickColor = new Color(0.25f, 0.65f, 1f, 0.9f);

    private AOGCharacterStats stats;
    private Vector3 moveTarget;
    private bool hasMoveTarget;
    private Minion targetMinion;
    private TowerHealth targetTower;
    private float nextAttackTime;
    private Coroutine attackRoutine;
    private Vector3 lastFramePosition;
    private readonly List<IChampionBasicAttackModifier> basicAttackModifiers = new List<IChampionBasicAttackModifier>();

    private void Start()
    {
        stats = GetComponent<AOGCharacterStats>();
        if (stats == null)
            stats = gameObject.AddComponent<AOGCharacterStats>();

        RefreshMainCamera();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (presentation == null)
            presentation = GetComponent<ChampionPresentationController>();

        if (presentation == null)
        {
            ChampionAudioController audio = GetComponent<ChampionAudioController>();
            if (audio == null)
                audio = gameObject.AddComponent<ChampionAudioController>();

            presentation = gameObject.AddComponent<ChampionPresentationController>();
            presentation.animator = animator;
            presentation.audioController = audio;
        }

        DisableLegacyInputComponents();
        CacheBasicAttackModifiers();
        moveTarget = transform.position;
        lastFramePosition = transform.position;
    }

    private void DisableLegacyInputComponents()
    {
        PlayerAutoAttack legacyAutoAttack = GetComponent<PlayerAutoAttack>();
        if (legacyAutoAttack != null)
            legacyAutoAttack.enabled = false;

        PlayerAttack legacyAttack = GetComponent<PlayerAttack>();
        if (legacyAttack != null)
            legacyAttack.enabled = false;

        ChampionController legacyChampionController = GetComponent<ChampionController>();
        if (legacyChampionController != null)
            legacyChampionController.enabled = false;
    }

    private void CacheBasicAttackModifiers()
    {
        basicAttackModifiers.Clear();
        MonoBehaviour[] behaviours = GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour is IChampionBasicAttackModifier modifier)
                basicAttackModifiers.Add(modifier);
        }
    }

    private void Update()
    {
        if (stats == null || stats.IsDead)
            return;

        if (mainCamera == null || !mainCamera.isActiveAndEnabled)
            RefreshMainCamera();

        if (Input.GetKeyDown(KeyCode.S))
            StopAllActions();

        HandleMouseInput();
        HandleMovement();
        HandleAttackLogic();
        UpdateAnimationVelocity();
    }

    private void RefreshMainCamera()
    {
        mainCamera = Camera.main;

        if (mainCamera != null)
            return;

        Camera[] cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
        foreach (Camera candidate in cameras)
        {
            if (candidate != null && candidate.isActiveAndEnabled)
            {
                mainCamera = candidate;
                return;
            }
        }
    }

    private void HandleMouseInput()
    {
        bool click = (allowRightClickMove && Input.GetMouseButtonDown(1)) ||
                     (allowLeftClickMove && Input.GetMouseButtonDown(0));

        if (!click || mainCamera == null)
            return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, maxClickRayDistance, groundMask, QueryTriggerInteraction.Ignore);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        if (TrySelectCombatTarget(hits))
            return;

        if (TryResolveMovePoint(hits, out Vector3 destination))
        {
            SetMoveDestination(destination);
            return;
        }

        Plane fallbackPlane = new Plane(Vector3.up, new Vector3(0f, transform.position.y, 0f));
        if (fallbackPlane.Raycast(ray, out float enter))
            SetMoveDestination(ray.GetPoint(enter));
    }

    private bool TrySelectCombatTarget(RaycastHit[] hits)
    {
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null || IsOwnHierarchy(hit.collider.transform))
                continue;

            Minion clickedMinion = hit.collider.GetComponentInParent<Minion>();
            if (clickedMinion != null && clickedMinion.team != stats.team && clickedMinion.hp > 0f)
            {
                targetMinion = clickedMinion;
                targetTower = null;
                hasMoveTarget = false;
                return true;
            }

            TowerHealth clickedTower = hit.collider.GetComponentInParent<TowerHealth>();
            if (clickedTower != null && clickedTower.towerTeam != stats.team && clickedTower.hp > 0f)
            {
                targetTower = clickedTower;
                targetMinion = null;
                hasMoveTarget = false;
                return true;
            }
        }

        return false;
    }

    private bool TryResolveMovePoint(RaycastHit[] hits, out Vector3 destination)
    {
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null)
                continue;

            Transform hitTransform = hit.collider.transform;
            if (IsOwnHierarchy(hitTransform) || IsPresentationOnlyObject(hitTransform))
                continue;

            if (hit.collider.GetComponentInParent<Minion>() != null)
                continue;

            if (hit.collider.GetComponentInParent<TowerHealth>() != null)
                continue;

            destination = hit.point;
            destination.y = transform.position.y;
            return true;
        }

        destination = default;
        return false;
    }

    private bool IsOwnHierarchy(Transform hitTransform)
    {
        return hitTransform == transform || hitTransform.IsChildOf(transform);
    }

    private static bool IsPresentationOnlyObject(Transform hitTransform)
    {
        if (hitTransform == null)
            return false;

        string lower = hitTransform.gameObject.name.ToLowerInvariant();
        return lower.Contains("readability_ring") ||
               lower.Contains("ground_shadow") ||
               lower.Contains("hp_bar") ||
               lower.Contains("click_indicator") ||
               lower.Contains("aog_runtime_combathud");
    }

    private void SetMoveDestination(Vector3 destination)
    {
        moveTarget = destination;
        moveTarget.y = transform.position.y;
        targetMinion = null;
        targetTower = null;
        hasMoveTarget = true;

        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }

        if (showMoveClickIndicator)
            AOGMoveClickIndicator.Show(moveTarget, moveClickColor);
    }

    private void HandleMovement()
    {
        if (!hasMoveTarget || attackRoutine != null)
            return;

        Vector3 direction = moveTarget - transform.position;
        direction.y = 0f;

        if (direction.magnitude <= stopDistance)
        {
            hasMoveTarget = false;
            return;
        }

        transform.position += direction.normalized * stats.moveSpeed * Time.deltaTime;
        FaceTarget(moveTarget);
    }

    private void HandleAttackLogic()
    {
        if (attackRoutine != null)
            return;

        if (targetMinion != null)
        {
            AttackSelectedMinion();
            return;
        }

        if (targetTower != null)
        {
            AttackSelectedTower();
            return;
        }

        if (autoAttackNearestEnemy && !hasMoveTarget)
        {
            Minion nearest = FindNearestEnemyMinion(stats.attackRange);
            if (nearest != null)
            {
                targetMinion = nearest;
                AttackSelectedMinion();
            }
        }
    }

    private void AttackSelectedMinion()
    {
        if (targetMinion == null || targetMinion.hp <= 0f)
        {
            targetMinion = null;
            return;
        }

        float distance = FlatDistance(transform.position, targetMinion.transform.position);
        if (distance > stats.attackRange)
        {
            MoveTowardTarget(targetMinion.transform.position);
            return;
        }

        hasMoveTarget = false;
        FaceTarget(targetMinion.transform.position);

        if (Time.time < nextAttackTime)
            return;

        nextAttackTime = Time.time + stats.attackCooldown;
        Minion lockedTarget = targetMinion;
        presentation?.PlayBasicAttack();
        float windup = presentation != null ? presentation.BasicAttackWindup : Mathf.Min(0.22f, stats.attackCooldown * 0.35f);
        attackRoutine = StartCoroutine(ResolveMinionAttack(lockedTarget, windup));
    }

    private IEnumerator ResolveMinionAttack(Minion lockedTarget, float windup)
    {
        if (windup > 0f)
            yield return new WaitForSeconds(windup);

        if (lockedTarget != null && lockedTarget.gameObject.activeInHierarchy && lockedTarget.hp > 0f)
        {
            float distance = FlatDistance(transform.position, lockedTarget.transform.position);
            if (distance <= stats.attackRange + attackRangeTolerance)
            {
                lockedTarget.TakeDamage(stats.attackDamage, gameObject);

                foreach (IChampionBasicAttackModifier modifier in basicAttackModifiers)
                    modifier?.OnBasicAttackHit(lockedTarget);

                if (presentation != null)
                {
                    presentation.audioController?.PlayAttackImpact();
                    presentation.SpawnImpactVfx(lockedTarget.transform.position + Vector3.up * 0.8f);
                }
            }
        }

        attackRoutine = null;
    }

    private void AttackSelectedTower()
    {
        if (targetTower == null || targetTower.hp <= 0f)
        {
            targetTower = null;
            return;
        }

        float distance = FlatDistance(transform.position, targetTower.transform.position);
        float allowedRange = stats.attackRange + 3f;

        if (distance > allowedRange)
        {
            MoveTowardTarget(targetTower.transform.position);
            return;
        }

        hasMoveTarget = false;
        FaceTarget(targetTower.transform.position);

        if (Time.time < nextAttackTime)
            return;

        nextAttackTime = Time.time + stats.attackCooldown;
        TowerHealth lockedTarget = targetTower;
        presentation?.PlayBasicAttack();
        float windup = presentation != null ? presentation.BasicAttackWindup : Mathf.Min(0.22f, stats.attackCooldown * 0.35f);
        attackRoutine = StartCoroutine(ResolveTowerAttack(lockedTarget, windup, allowedRange));
    }

    private IEnumerator ResolveTowerAttack(TowerHealth lockedTarget, float windup, float allowedRange)
    {
        if (windup > 0f)
            yield return new WaitForSeconds(windup);

        if (lockedTarget != null && lockedTarget.gameObject.activeInHierarchy && lockedTarget.hp > 0f)
        {
            float distance = FlatDistance(transform.position, lockedTarget.transform.position);
            if (distance <= allowedRange + attackRangeTolerance)
            {
                lockedTarget.TakeDamage(stats.attackDamage);
                if (presentation != null)
                {
                    presentation.audioController?.PlayAttackImpact();
                    presentation.SpawnImpactVfx(lockedTarget.transform.position + Vector3.up * 1.5f);
                }
            }
        }

        attackRoutine = null;
    }

    private Minion FindNearestEnemyMinion(float range)
    {
        Minion[] minions = FindObjectsByType<Minion>(FindObjectsSortMode.None);
        Minion closest = null;
        float closestDistance = Mathf.Infinity;

        foreach (Minion minion in minions)
        {
            if (minion == null || !minion.gameObject.activeInHierarchy || minion.hp <= 0f || minion.team == stats.team)
                continue;

            float distance = FlatDistance(transform.position, minion.transform.position);
            if (distance <= range && distance < closestDistance)
            {
                closest = minion;
                closestDistance = distance;
            }
        }

        return closest;
    }

    private void MoveTowardTarget(Vector3 target)
    {
        Vector3 direction = target - transform.position;
        direction.y = 0f;

        if (direction.magnitude <= 0.1f)
            return;

        transform.position += direction.normalized * stats.moveSpeed * Time.deltaTime;
        FaceTarget(target);
    }

    private void StopAllActions()
    {
        hasMoveTarget = false;
        targetMinion = null;
        targetTower = null;

        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }

        presentation?.SetPlanarVelocity(Vector3.zero);
        SetAnimatorFloatIfPresent("Speed", 0f);
    }

    private void FaceTarget(Vector3 target)
    {
        Vector3 direction = target - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
            return;

        Quaternion lookRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, 12f * Time.deltaTime);
    }

    private static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private void UpdateAnimationVelocity()
    {
        float delta = Mathf.Max(Time.deltaTime, 0.0001f);
        Vector3 velocity = (transform.position - lastFramePosition) / delta;
        velocity.y = 0f;
        lastFramePosition = transform.position;

        if (presentation != null)
        {
            presentation.SetPlanarVelocity(velocity);
            return;
        }

        SetAnimatorFloatIfPresent("Speed", Mathf.Clamp01(velocity.magnitude / Mathf.Max(stats.moveSpeed, 0.01f)));
    }

    private void SetAnimatorFloatIfPresent(string parameterName, float value)
    {
        if (animator == null)
            return;

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.name == parameterName && parameter.type == AnimatorControllerParameterType.Float)
            {
                animator.SetFloat(parameterName, value);
                return;
            }
        }
    }
}

public static class AOGMoveClickIndicator
{
    public static void Show(Vector3 worldPosition, Color color)
    {
        GameObject indicator = new GameObject("AOG_Move_Click_Indicator");
        indicator.transform.position = worldPosition + Vector3.up * 0.08f;

        LineRenderer line = indicator.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.loop = true;
        line.positionCount = 24;
        line.startWidth = 0.045f;
        line.endWidth = 0.045f;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");

        Material material = new Material(shader);
        material.color = color;
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        line.material = material;

        const float radius = 0.55f;
        for (int i = 0; i < line.positionCount; i++)
        {
            float angle = i * Mathf.PI * 2f / line.positionCount;
            line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
        }

        AOGMoveClickIndicatorFade fade = indicator.AddComponent<AOGMoveClickIndicatorFade>();
        fade.line = line;
        fade.life = 0.45f;
    }
}

public class AOGMoveClickIndicatorFade : MonoBehaviour
{
    public LineRenderer line;
    public float life = 0.45f;
    private float elapsed;
    private Color initialColor;

    private void Start()
    {
        if (line != null)
            initialColor = line.material.color;
    }

    private void Update()
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, life));

        if (line != null)
        {
            Color c = initialColor;
            c.a *= 1f - t;
            line.material.color = c;
            if (line.material.HasProperty("_BaseColor"))
                line.material.SetColor("_BaseColor", c);
        }

        transform.localScale = Vector3.one * Mathf.Lerp(0.75f, 1.15f, t);

        if (elapsed >= life)
            Destroy(gameObject);
    }
}
