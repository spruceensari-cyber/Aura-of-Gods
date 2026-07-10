using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

/// <summary>
/// Single MOBA command driver for the human-controlled champion. World clicks choose combat
/// targets or movement points. Combat target ownership never changes camera ownership.
/// </summary>
[DefaultExecutionOrder(-50)]
public class AOGUnifiedMobaInputDriver : MonoBehaviour
{
    public Camera gameplayCamera;
    public LayerMask commandMask = ~0;
    public float maxRayDistance = 1200f;
    public float stopDistance = 0.32f;
    public float attackRangeTolerance = 0.9f;
    public bool leftClickAlsoMoves;

    private AOGCharacterStats stats;
    private ChampionPresentationController presentation;
    private Vector3 moveTarget;
    private bool hasMoveTarget;
    private AOGCharacterStats targetHero;
    private Minion targetMinion;
    private TowerHealth targetTower;
    private AOGNexusCore targetNexus;
    private AOGNeutralBossAI targetBoss;
    private float nextAttackTime;
    private Coroutine attackRoutine;
    private Vector3 lastPosition;
    private readonly List<IChampionBasicAttackModifier> attackModifiers = new List<IChampionBasicAttackModifier>();
    private AOGTargetRingIndicator targetRing;

    private void Awake()
    {
        stats = GetComponent<AOGCharacterStats>();
        presentation = GetComponent<ChampionPresentationController>();
        gameplayCamera = Camera.main;
        lastPosition = transform.position;
        moveTarget = transform.position;
        RefreshAttackModifiers();
        DisableConflictingControllers();
    }

    private void OnEnable()
    {
        if (stats == null) stats = GetComponent<AOGCharacterStats>();
        if (presentation == null) presentation = GetComponent<ChampionPresentationController>();
        RefreshAttackModifiers();
    }

    private void OnDisable()
    {
        ClearTargets();
        hasMoveTarget = false;
        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }
    }

    private void RefreshAttackModifiers()
    {
        attackModifiers.Clear();
        foreach (MonoBehaviour behaviour in GetComponents<MonoBehaviour>())
            if (behaviour is IChampionBasicAttackModifier modifier)
                attackModifiers.Add(modifier);
    }

    private void DisableConflictingControllers()
    {
        AOGPlayerMOBAController oldMoba = GetComponent<AOGPlayerMOBAController>();
        if (oldMoba != null) oldMoba.enabled = false;
        PlayerAutoAttack oldAuto = GetComponent<PlayerAutoAttack>();
        if (oldAuto != null) oldAuto.enabled = false;
        PlayerAttack oldAttack = GetComponent<PlayerAttack>();
        if (oldAttack != null) oldAttack.enabled = false;
        ChampionController oldChampionController = GetComponent<ChampionController>();
        if (oldChampionController != null) oldChampionController.enabled = false;
    }

    private void Update()
    {
        if (stats == null || stats.IsDead)
            return;

        AOGActiveChampion marker = GetComponent<AOGActiveChampion>();
        if (marker != null && !marker.IsActiveChampion)
            return;
        if (AOGPlayerChampionAuthority.CurrentChampion != null && marker != AOGPlayerChampionAuthority.CurrentChampion)
            return;

        if (gameplayCamera == null || !gameplayCamera.isActiveAndEnabled)
            gameplayCamera = Camera.main;

        HandleCommandInput();
        HandleAbilityInput();
        HandleMovement();
        HandleAttackLogic();
        UpdateAnimationVelocity();
        UpdateTargetRing();
    }

    private void HandleCommandInput()
    {
        if (AOGInputBridge.KeyPressedThisFrame(KeyCode.S))
        {
            StopAllActions();
            return;
        }

        bool right = AOGInputBridge.RightPressedThisFrame();
        bool left = AOGInputBridge.LeftPressedThisFrame();
        bool commandPressed = right || (leftClickAlsoMoves && left) || left;
        if (!commandPressed || gameplayCamera == null)
            return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        Vector2 pointer = AOGInputBridge.PointerPosition;
        Ray ray = gameplayCamera.ScreenPointToRay(new Vector3(pointer.x, pointer.y, 0f));
        RaycastHit[] hits = Physics.RaycastAll(ray, maxRayDistance, commandMask, QueryTriggerInteraction.Ignore);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        if (TrySelectEnemy(hits))
            return;

        if (right || leftClickAlsoMoves)
        {
            if (TryFindWalkPoint(hits, out Vector3 point))
            {
                SetMoveTarget(point);
                return;
            }

            Plane fallbackGround = new Plane(Vector3.up, new Vector3(0f, transform.position.y, 0f));
            if (fallbackGround.Raycast(ray, out float enter))
                SetMoveTarget(ray.GetPoint(enter));
        }
    }

    private void HandleAbilityInput()
    {
        if (AOGInputBridge.KeyPressedThisFrame(KeyCode.Q)) SendMessage("CastQ", SendMessageOptions.DontRequireReceiver);
        if (AOGInputBridge.KeyPressedThisFrame(KeyCode.W)) SendMessage("CastW", SendMessageOptions.DontRequireReceiver);
        if (AOGInputBridge.KeyPressedThisFrame(KeyCode.E)) SendMessage("CastE", SendMessageOptions.DontRequireReceiver);
        if (AOGInputBridge.KeyPressedThisFrame(KeyCode.R)) SendMessage("CastR", SendMessageOptions.DontRequireReceiver);
    }

    private bool TrySelectEnemy(RaycastHit[] hits)
    {
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null || hit.collider.transform == transform || hit.collider.transform.IsChildOf(transform))
                continue;

            AOGCharacterStats hero = hit.collider.GetComponentInParent<AOGCharacterStats>();
            if (hero != null && hero != stats && !hero.IsDead && hero.team != stats.team)
            {
                ClearTargets();
                targetHero = hero;
                hasMoveTarget = false;
                ShowTargetRing(hero.transform, TeamTargetColor(hero.team), 1.15f);
                return true;
            }

            Minion minion = hit.collider.GetComponentInParent<Minion>();
            if (minion != null && minion.hp > 0f && minion.team != stats.team)
            {
                ClearTargets();
                targetMinion = minion;
                hasMoveTarget = false;
                ShowTargetRing(minion.transform, TeamTargetColor(minion.team), 0.72f);
                return true;
            }

            TowerHealth tower = hit.collider.GetComponentInParent<TowerHealth>();
            if (tower != null && tower.hp > 0f && tower.towerTeam != stats.team)
            {
                ClearTargets();
                targetTower = tower;
                hasMoveTarget = false;
                ShowTargetRing(tower.transform, TeamTargetColor(tower.towerTeam), 2.2f);
                return true;
            }

            AOGNexusCore nexus = hit.collider.GetComponentInParent<AOGNexusCore>();
            if (nexus != null && !nexus.IsDestroyed && nexus.team != stats.team)
            {
                ClearTargets();
                targetNexus = nexus;
                hasMoveTarget = false;
                ShowTargetRing(nexus.transform, TeamTargetColor(nexus.team), 3.0f);
                return true;
            }

            AOGNeutralBossAI boss = hit.collider.GetComponentInParent<AOGNeutralBossAI>();
            if (boss != null && !boss.IsDead)
            {
                ClearTargets();
                targetBoss = boss;
                hasMoveTarget = false;
                ShowTargetRing(boss.transform, new Color(0.76f,0.30f,1f), 2.8f);
                return true;
            }
        }
        return false;
    }

    private bool TryFindWalkPoint(RaycastHit[] hits, out Vector3 point)
    {
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == null) continue;
            Transform t = hit.collider.transform;
            if (t == transform || t.IsChildOf(transform)) continue;

            string n = t.gameObject.name.ToLowerInvariant();
            if (n.Contains("hp_bar") || n.Contains("worldbar") || n.Contains("click_indicator") || n.Contains("readability") || n.Contains("telegraph") || n.Contains("target_ring"))
                continue;

            if (hit.collider.GetComponentInParent<AOGCharacterStats>() != null ||
                hit.collider.GetComponentInParent<Minion>() != null ||
                hit.collider.GetComponentInParent<TowerHealth>() != null ||
                hit.collider.GetComponentInParent<AOGNexusCore>() != null ||
                hit.collider.GetComponentInParent<AOGNeutralBossAI>() != null)
                continue;

            point = hit.point;
            point.y = transform.position.y;
            return true;
        }
        point = default;
        return false;
    }

    private void SetMoveTarget(Vector3 point)
    {
        moveTarget = point;
        moveTarget.y = transform.position.y;
        hasMoveTarget = true;
        ClearTargets();
        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }
        AOGActiveChampion marker = GetComponent<AOGActiveChampion>();
        Color color = marker != null ? marker.accentColor : new Color(0.20f,0.72f,1f,0.95f);
        AOGMoveClickIndicator.Show(moveTarget, color);
    }

    private void HandleMovement()
    {
        if (!hasMoveTarget || attackRoutine != null)
            return;
        Vector3 delta = moveTarget - transform.position;
        delta.y = 0f;
        float distance = delta.magnitude;
        if (distance <= stopDistance)
        {
            hasMoveTarget = false;
            return;
        }
        Vector3 direction = delta / Mathf.Max(distance, 0.0001f);
        transform.position += direction * stats.moveSpeed * Time.deltaTime;
        Face(direction);
    }

    private void HandleAttackLogic()
    {
        if (attackRoutine != null)
            return;

        if (targetHero != null)
        {
            if (targetHero.IsDead || !targetHero.gameObject.activeInHierarchy)
            {
                targetHero = null;
                HideTargetRing();
                return;
            }
            AOGCharacterStats locked = targetHero;
            AttackOrChase(locked.transform, stats.attackRange, () => StartCoroutine(HitHeroAfterWindup(locked, CurrentWindup())));
            return;
        }

        if (targetMinion != null)
        {
            if (targetMinion.hp <= 0f || !targetMinion.gameObject.activeInHierarchy)
            {
                targetMinion = null;
                HideTargetRing();
                return;
            }
            Minion locked = targetMinion;
            AttackOrChase(locked.transform, stats.attackRange, () => StartCoroutine(HitMinionAfterWindup(locked, CurrentWindup())));
            return;
        }

        if (targetTower != null)
        {
            if (targetTower.hp <= 0f || !targetTower.gameObject.activeInHierarchy)
            {
                targetTower = null;
                HideTargetRing();
                return;
            }
            float range = stats.attackRange + 2.8f;
            TowerHealth locked = targetTower;
            AttackOrChase(locked.transform, range, () => StartCoroutine(HitTowerAfterWindup(locked, CurrentWindup(), range)));
            return;
        }

        if (targetNexus != null)
        {
            if (targetNexus.IsDestroyed || !targetNexus.gameObject.activeInHierarchy)
            {
                targetNexus = null;
                HideTargetRing();
                return;
            }
            float range = stats.attackRange + 2.6f;
            AOGNexusCore locked = targetNexus;
            AttackOrChase(locked.transform, range, () => StartCoroutine(HitNexusAfterWindup(locked, CurrentWindup(), range)));
            return;
        }

        if (targetBoss != null)
        {
            if (targetBoss.IsDead || !targetBoss.gameObject.activeInHierarchy)
            {
                targetBoss = null;
                HideTargetRing();
                return;
            }
            float range = stats.attackRange + 1.4f;
            AOGNeutralBossAI locked = targetBoss;
            AttackOrChase(locked.transform, range, () => StartCoroutine(HitBossAfterWindup(locked, CurrentWindup(), range)));
        }
    }

    private void AttackOrChase(Transform target, float allowedRange, Func<Coroutine> beginAttack)
    {
        if (target == null) return;
        float distance = FlatDistance(transform.position, target.position);
        if (distance > allowedRange)
        {
            MoveToward(target.position);
            return;
        }
        hasMoveTarget = false;
        Face(target.position - transform.position);
        if (Time.time < nextAttackTime) return;
        nextAttackTime = Time.time + stats.attackCooldown;
        presentation?.PlayBasicAttack();
        attackRoutine = beginAttack();
    }

    private float CurrentWindup()
    {
        return presentation != null ? presentation.BasicAttackWindup : 0.2f;
    }

    private IEnumerator HitHeroAfterWindup(AOGCharacterStats target, float windup)
    {
        if (windup > 0f) yield return new WaitForSeconds(windup);
        if (target != null && !target.IsDead && target.team != stats.team && FlatDistance(transform.position,target.transform.position) <= stats.attackRange + attackRangeTolerance)
        {
            target.TakeDamage(stats.attackDamage);
            PlayAttackImpact(target.transform.position + Vector3.up * 1.1f);
        }
        attackRoutine = null;
    }

    private IEnumerator HitMinionAfterWindup(Minion target, float windup)
    {
        if (windup > 0f) yield return new WaitForSeconds(windup);
        if (target != null && target.hp > 0f && FlatDistance(transform.position,target.transform.position) <= stats.attackRange + attackRangeTolerance)
        {
            target.TakeDamage(stats.attackDamage, gameObject);
            foreach (IChampionBasicAttackModifier modifier in attackModifiers) modifier?.OnBasicAttackHit(target);
            PlayAttackImpact(target.transform.position + Vector3.up * 0.8f);
        }
        attackRoutine = null;
    }

    private IEnumerator HitTowerAfterWindup(TowerHealth target, float windup, float allowedRange)
    {
        if (windup > 0f) yield return new WaitForSeconds(windup);
        if (target != null && target.hp > 0f && FlatDistance(transform.position,target.transform.position) <= allowedRange + attackRangeTolerance)
        {
            target.TakeDamage(stats.attackDamage);
            PlayAttackImpact(target.transform.position + Vector3.up * 1.5f);
        }
        attackRoutine = null;
    }

    private IEnumerator HitNexusAfterWindup(AOGNexusCore target, float windup, float allowedRange)
    {
        if (windup > 0f) yield return new WaitForSeconds(windup);
        if (target != null && !target.IsDestroyed && FlatDistance(transform.position,target.transform.position) <= allowedRange + attackRangeTolerance)
        {
            target.TakeDamage(stats.attackDamage);
            PlayAttackImpact(target.transform.position + Vector3.up * 2.2f);
        }
        attackRoutine = null;
    }

    private IEnumerator HitBossAfterWindup(AOGNeutralBossAI target, float windup, float allowedRange)
    {
        if (windup > 0f) yield return new WaitForSeconds(windup);
        if (target != null && !target.IsDead && FlatDistance(transform.position,target.transform.position) <= allowedRange + attackRangeTolerance)
        {
            target.TakeDamage(stats.attackDamage, gameObject);
            PlayAttackImpact(target.transform.position + Vector3.up * 1.5f);
        }
        attackRoutine = null;
    }

    private void PlayAttackImpact(Vector3 position)
    {
        presentation?.audioController?.PlayAttackImpact();
        presentation?.SpawnImpactVfx(position);
    }

    private void MoveToward(Vector3 point)
    {
        Vector3 delta = point - transform.position;
        delta.y = 0f;
        if (delta.sqrMagnitude < 0.01f) return;
        Vector3 direction = delta.normalized;
        transform.position += direction * stats.moveSpeed * Time.deltaTime;
        Face(direction);
    }

    private void Face(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f) return;
        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);
        transform.rotation = Quaternion.Slerp(transform.rotation,targetRotation,15f*Time.deltaTime);
    }

    private void StopAllActions()
    {
        hasMoveTarget = false;
        ClearTargets();
        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }
        presentation?.SetPlanarVelocity(Vector3.zero);
    }

    private void ClearTargets()
    {
        targetHero = null;
        targetMinion = null;
        targetTower = null;
        targetNexus = null;
        targetBoss = null;
        HideTargetRing();
    }

    private void ShowTargetRing(Transform target, Color color, float radius)
    {
        if (targetRing == null)
        {
            GameObject go = new GameObject("AOG_Target_Ring");
            targetRing = go.AddComponent<AOGTargetRingIndicator>();
        }
        targetRing.Bind(target, color, radius);
    }

    private void HideTargetRing()
    {
        if (targetRing != null)
            targetRing.Hide();
    }

    private void UpdateTargetRing()
    {
        if (targetRing != null)
            targetRing.Tick();
    }

    private void UpdateAnimationVelocity()
    {
        float dt = Mathf.Max(Time.deltaTime,0.0001f);
        Vector3 velocity = (transform.position-lastPosition)/dt;
        velocity.y = 0f;
        lastPosition = transform.position;
        presentation?.SetPlanarVelocity(velocity);
    }

    private static Color TeamTargetColor(MinionTeam team)
    {
        return team == MinionTeam.Blue ? new Color(0.18f,0.62f,1f) : new Color(1f,0.18f,0.24f);
    }

    private static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y=0f;b.y=0f;return Vector3.Distance(a,b);
    }
}

public class AOGTargetRingIndicator : MonoBehaviour
{
    private Transform target;
    private LineRenderer line;
    private float radius;
    private const int Segments = 48;

    private void Awake()
    {
        line = gameObject.AddComponent<LineRenderer>();
        line.loop = true;
        line.useWorldSpace = true;
        line.positionCount = Segments;
        line.startWidth = 0.075f;
        line.endWidth = 0.075f;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        line.material = new Material(shader);
    }

    public void Bind(Transform newTarget, Color color, float newRadius)
    {
        target = newTarget;
        radius = newRadius;
        line.startColor = color;
        line.endColor = color;
        if (line.material != null) line.material.color = color;
        gameObject.SetActive(true);
        Tick();
    }

    public void Tick()
    {
        if (target == null)
        {
            Hide();
            return;
        }
        Vector3 center = target.position + Vector3.up * 0.08f;
        for (int i = 0; i < Segments; i++)
        {
            float angle = i * Mathf.PI * 2f / Segments;
            line.SetPosition(i, center + new Vector3(Mathf.Cos(angle)*radius,0f,Mathf.Sin(angle)*radius));
        }
    }

    public void Hide()
    {
        target = null;
        gameObject.SetActive(false);
    }
}

public static class AOGUnifiedMobaInputBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        AttachToCandidates();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        AttachToCandidates();
    }

    private static void AttachToCandidates()
    {
        AOGActiveChampion[] candidates = UnityEngine.Object.FindObjectsByType<AOGActiveChampion>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (AOGActiveChampion marker in candidates)
        {
            if (marker == null) continue;
            AOGUnifiedMobaInputDriver driver = marker.GetComponent<AOGUnifiedMobaInputDriver>();
            if (driver == null) driver = marker.gameObject.AddComponent<AOGUnifiedMobaInputDriver>();
            driver.enabled = marker.IsActiveChampion;
        }
    }
}
