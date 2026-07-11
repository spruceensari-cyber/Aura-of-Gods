using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum MinionTeam
{
    Blue,
    Red
}

public enum MinionRole
{
    Melee,
    Ranged,
    Cannon
}

public class Minion : MonoBehaviour
{
    public static readonly HashSet<Minion> Active = new HashSet<Minion>();

    public MinionTeam team;
    public MinionRole role;

    [Header("Path / Lane")]
    public Vector3[] path;
    public int currentPathIndex = 1;
    public float waypointReachDistance = 0.9f;
    public float laneWidth = 1.0f;
    public float laneOffset;

    [Header("Stats")]
    public float speed = 3f;
    public float maxHp = 50f;
    public float hp = 50f;
    public float damage = 8f;
    public float attackRange = 3.5f;
    public float attackRate = 1.1f;

    [Header("Projectile")]
    public GameObject projectilePrefab;

    [Header("Combat")]
    public float aggroRange = 5.5f;
    public float towerAggroRange = 18f;
    public float towerAttackDistance = 3.2f;
    public float rotationSpeed = 8f;
    public float targetScanInterval = 0.18f;
    public float attackWindup = 0.30f;

    private float nextAttackTime;
    private float nextTargetScan;
    private Animator animator;
    private AOGMinionProceduralAnimator proceduralAnimator;
    private Minion targetMinion;
    private TowerHealth targetTower;
    private AOGNexusCore targetNexus;
    private Coroutine attackRoutine;
    private bool dying;
    private Vector3 lastPosition;
    private readonly List<Minion> nearbyBuffer = new List<Minion>(24);

    private void OnEnable()
    {
        Active.Add(this);
        float stagger = Mathf.Abs(GetInstanceID() % 11) * 0.013f;
        nextTargetScan = Time.time + stagger;
    }

    private void OnDisable()
    {
        Active.Remove(this);
    }

    private void Start()
    {
        if (hp <= 0f)
            hp = maxHp;

        animator = GetComponentInChildren<Animator>(true);
        proceduralAnimator = AOGMinionVisualFactory.Build(this);
        lastPosition = transform.position;

        CapsuleCollider capsule = GetComponent<CapsuleCollider>();
        if (capsule == null)
            capsule = gameObject.AddComponent<CapsuleCollider>();
        capsule.center = new Vector3(0f, role == MinionRole.Cannon ? 0.75f : 1.0f, 0f);
        capsule.height = role == MinionRole.Cannon ? 1.5f : 2.1f;
        capsule.radius = role == MinionRole.Cannon ? 0.8f : 0.52f;
    }

    private void Update()
    {
        if (dying || hp <= 0f)
            return;

        if (Time.time >= nextTargetScan)
        {
            nextTargetScan = Time.time + targetScanInterval;
            RefreshTarget();
        }

        bool moving = false;

        if (targetMinion != null)
        {
            if (!IsValidEnemy(targetMinion))
            {
                targetMinion = null;
            }
            else
            {
                float distance = FlatDistance(transform.position, targetMinion.transform.position);
                if (distance <= attackRange)
                {
                    FaceTarget(targetMinion.transform.position);
                    AttackMinion(targetMinion);
                }
                else
                {
                    MoveTo(targetMinion.transform.position, true);
                    moving = true;
                }
                UpdateAnimation(moving);
                return;
            }
        }

        if (targetTower != null)
        {
            if (targetTower.hp <= 0f || !targetTower.gameObject.activeInHierarchy)
            {
                targetTower = null;
            }
            else
            {
                float distance = FlatDistance(transform.position, targetTower.transform.position);
                if (distance <= towerAttackDistance)
                {
                    FaceTarget(targetTower.transform.position);
                    AttackTower(targetTower);
                }
                else
                {
                    MoveTo(targetTower.transform.position, true);
                    moving = true;
                }
                UpdateAnimation(moving);
                return;
            }
        }

        if (targetNexus != null)
        {
            if (targetNexus.IsDestroyed || !targetNexus.gameObject.activeInHierarchy)
            {
                targetNexus = null;
            }
            else
            {
                float distance = FlatDistance(transform.position, targetNexus.transform.position);
                if (distance <= Mathf.Max(attackRange, 3.4f))
                {
                    FaceTarget(targetNexus.transform.position);
                    AttackNexus(targetNexus);
                }
                else
                {
                    MoveTo(targetNexus.transform.position, true);
                    moving = true;
                }
                UpdateAnimation(moving);
                return;
            }
        }

        moving = MoveAlongPath();
        UpdateAnimation(moving);
    }

    private void RefreshTarget()
    {
        if (targetMinion != null && IsValidEnemy(targetMinion) && FlatDistance(transform.position, targetMinion.transform.position) <= aggroRange * 1.6f)
            return;

        targetMinion = FindEnemyMinionInRange();
        if (targetMinion != null)
        {
            targetTower = null;
            targetNexus = null;
            return;
        }

        targetTower = FindEnemyTowerInRange();
        if (targetTower != null)
        {
            targetNexus = null;
            return;
        }

        if (currentPathIndex >= (path != null ? path.Length - 1 : 0))
            targetNexus = AOGMatchDirector.Instance != null ? AOGMatchDirector.Instance.GetEnemyNexus(team) : null;
        else if (targetNexus == null && AOGMatchDirector.Instance != null)
        {
            AOGNexusCore nexus = AOGMatchDirector.Instance.GetEnemyNexus(team);
            if (nexus != null && FlatDistance(transform.position, nexus.transform.position) <= 22f)
                targetNexus = nexus;
        }
    }

    private bool MoveAlongPath()
    {
        if (path == null || path.Length == 0)
            return false;

        if (currentPathIndex >= path.Length)
        {
            if (AOGMatchDirector.Instance != null)
                targetNexus = AOGMatchDirector.Instance.GetEnemyNexus(team);
            return false;
        }

        Vector3 target = GetLaneTarget(currentPathIndex);
        float distance = FlatDistance(transform.position, target);

        if (distance <= waypointReachDistance)
        {
            currentPathIndex++;
            if (currentPathIndex >= path.Length)
            {
                if (AOGMatchDirector.Instance != null)
                    targetNexus = AOGMatchDirector.Instance.GetEnemyNexus(team);
                return false;
            }
            target = GetLaneTarget(currentPathIndex);
        }

        MoveTo(target, false);
        return true;
    }

    private Vector3 GetLaneTarget(int index)
    {
        Vector3 target = path[Mathf.Clamp(index, 0, path.Length - 1)];
        target.y = transform.position.y;

        int previousIndex = Mathf.Max(0, index - 1);
        Vector3 segment = path[Mathf.Clamp(index, 0, path.Length - 1)] - path[previousIndex];
        segment.y = 0f;
        if (segment.sqrMagnitude > 0.01f)
        {
            segment.Normalize();
            Vector3 right = new Vector3(segment.z, 0f, -segment.x);
            target += right * laneOffset;
        }

        return target;
    }

    private void MoveTo(Vector3 target, bool chasing)
    {
        target.y = transform.position.y;
        Vector3 desired = target - transform.position;
        desired.y = 0f;
        if (desired.sqrMagnitude <= 0.0025f)
            return;

        Vector3 direction = desired.normalized;
        Vector3 separation = ComputeSeparation();
        float separationWeight = chasing ? 0.52f : 0.82f;
        Vector3 blended = (direction + separation * separationWeight).normalized;

        transform.position += blended * speed * Time.deltaTime;
        FaceDirection(blended);
    }

    private Vector3 ComputeSeparation()
    {
        Vector3 force = Vector3.zero;
        int count = 0;
        AOGMinionSpatialGridRuntime.Query(transform.position, 1.5f, nearbyBuffer);

        for (int i = 0; i < nearbyBuffer.Count; i++)
        {
            Minion other = nearbyBuffer[i];
            if (other == null || other == this || other.team != team || other.hp <= 0f)
                continue;

            Vector3 delta = transform.position - other.transform.position;
            delta.y = 0f;
            float sqr = delta.sqrMagnitude;
            if (sqr <= 0.001f || sqr > 2.25f)
                continue;

            force += delta.normalized / Mathf.Max(0.25f, Mathf.Sqrt(sqr));
            count++;
        }

        if (count > 0)
            force /= count;

        return Vector3.ClampMagnitude(force, 1f);
    }

    private Minion FindEnemyMinionInRange()
    {
        Minion closest = null;
        float closestDistance = Mathf.Infinity;
        AOGMinionSpatialGridRuntime.Query(transform.position, aggroRange, nearbyBuffer);

        for (int i = 0; i < nearbyBuffer.Count; i++)
        {
            Minion minion = nearbyBuffer[i];
            if (!IsValidEnemy(minion))
                continue;

            float distance = FlatDistance(transform.position, minion.transform.position);
            if (distance < closestDistance)
            {
                closest = minion;
                closestDistance = distance;
            }
        }

        return closest;
    }

    private TowerHealth FindEnemyTowerInRange()
    {
        TowerHealth closest = null;
        float closestDistance = Mathf.Infinity;

        foreach (TowerHealth tower in AOGWorldRegistry.Towers)
        {
            if (tower == null || !tower.gameObject.activeInHierarchy || tower.hp <= 0f || tower.towerTeam == team)
                continue;

            float distance = FlatDistance(transform.position, tower.transform.position);
            if (distance <= towerAggroRange && distance < closestDistance)
            {
                closest = tower;
                closestDistance = distance;
            }
        }

        return closest;
    }

    private void AttackMinion(Minion target)
    {
        if (target == null || Time.time < nextAttackTime || attackRoutine != null)
            return;

        nextAttackTime = Time.time + attackRate;
        attackRoutine = StartCoroutine(ResolveAttack(target, null, null));
    }

    private void AttackTower(TowerHealth tower)
    {
        if (tower == null || Time.time < nextAttackTime || attackRoutine != null)
            return;

        nextAttackTime = Time.time + attackRate;
        attackRoutine = StartCoroutine(ResolveAttack(null, tower, null));
    }

    private void AttackNexus(AOGNexusCore nexus)
    {
        if (nexus == null || Time.time < nextAttackTime || attackRoutine != null)
            return;

        nextAttackTime = Time.time + attackRate;
        attackRoutine = StartCoroutine(ResolveAttack(null, null, nexus));
    }

    private IEnumerator ResolveAttack(Minion minion, TowerHealth tower, AOGNexusCore nexus)
    {
        proceduralAnimator?.PlayAttack();
        TriggerAnimator("Attack");

        float windup = role == MinionRole.Cannon ? 0.46f : role == MinionRole.Ranged ? 0.34f : attackWindup;
        yield return new WaitForSeconds(windup);

        Transform targetTransform = minion != null ? minion.transform : tower != null ? tower.transform : nexus != null ? nexus.transform : null;
        if (targetTransform != null)
        {
            float allowed = minion != null ? attackRange + 0.7f : tower != null ? towerAttackDistance + 1.0f : Mathf.Max(attackRange, 3.4f) + 1f;
            if (FlatDistance(transform.position, targetTransform.position) <= allowed)
            {
                if (role == MinionRole.Melee)
                {
                    if (minion != null) minion.TakeDamage(damage, gameObject);
                    else if (tower != null) tower.TakeDamage(damage);
                    else if (nexus != null) nexus.TakeDamage(damage);
                    GameObject impact = AOGAbilityVisuals.CreateRing("Minion_Melee_Impact", targetTransform.position + Vector3.up * 0.08f, 0.48f, TeamColor(), 0.045f);
                    Destroy(impact,0.25f);
                }
                else
                {
                    LaunchProjectile(targetTransform, minion, tower, nexus);
                }
            }
        }

        yield return new WaitForSeconds(0.08f);
        attackRoutine = null;
    }

    private void LaunchProjectile(Transform targetTransform, Minion minion, TowerHealth tower, AOGNexusCore nexus)
    {
        GameObject projectile = new GameObject(team + "_" + role + "_Projectile");
        projectile.transform.position = transform.position + Vector3.up * (role == MinionRole.Cannon ? 1.25f : 1.45f) + transform.forward * 0.45f;
        AOGMinionProjectile logic = projectile.AddComponent<AOGMinionProjectile>();
        logic.source = this;
        logic.targetTransform = targetTransform;
        logic.minionTarget = minion;
        logic.towerTarget = tower;
        logic.nexusTarget = nexus;
        logic.damage = damage;
        logic.speed = role == MinionRole.Cannon ? 14f : 17f;
        logic.color = TeamColor();
        logic.cannon = role == MinionRole.Cannon;
        logic.BuildVisual();
    }

    public void TakeDamage(float amount)
    {
        TakeDamage(amount, null);
    }

    public void TakeDamage(float amount, GameObject attacker)
    {
        if (dying || hp <= 0f || amount <= 0f)
            return;

        hp = Mathf.Clamp(hp - amount, 0f, maxHp);
        proceduralAnimator?.PlayHit();
        TriggerAnimator("Hit");

        if (hp <= 0f)
            StartCoroutine(Die(attacker));
    }

    private IEnumerator Die(GameObject attacker)
    {
        if (dying)
            yield break;

        dying = true;
        Active.Remove(this);

        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }

        AwardKill(attacker);
        proceduralAnimator?.PlayDeath();
        TriggerAnimator("Death");

        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider collider in colliders)
            collider.enabled = false;

        yield return new WaitForSeconds(0.58f);
        Destroy(gameObject);
    }

    private void AwardKill(GameObject attacker)
    {
        if (attacker == null)
            return;

        AOGPlayerEconomy economy = attacker.GetComponentInParent<AOGPlayerEconomy>();
        AOGChampionProgression progression = attacker.GetComponentInParent<AOGChampionProgression>();

        int goldReward = role == MinionRole.Cannon ? 62 : role == MinionRole.Melee ? 22 : 18;
        int xpReward = role == MinionRole.Cannon ? 92 : role == MinionRole.Melee ? 42 : 34;

        economy?.AddGold(goldReward);
        progression?.AddExperience(xpReward);
    }

    private void UpdateAnimation(bool moving)
    {
        float dt = Mathf.Max(Time.deltaTime, 0.0001f);
        float velocity = (transform.position - lastPosition).magnitude / dt;
        lastPosition = transform.position;
        proceduralAnimator?.SetMoving(moving, Mathf.Clamp01(velocity / Mathf.Max(0.1f, speed)));
        SetAnimatorFloat("Speed", moving ? Mathf.Clamp01(velocity / Mathf.Max(0.1f, speed)) : 0f);
    }

    private void FaceTarget(Vector3 target)
    {
        Vector3 direction = target - transform.position;
        direction.y = 0f;
        FaceDirection(direction);
    }

    private void FaceDirection(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
            return;

        Quaternion lookRotation = Quaternion.LookRotation(direction.normalized);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, rotationSpeed * Time.deltaTime);
    }

    private bool IsValidEnemy(Minion minion)
    {
        return minion != null && minion != this && minion.gameObject.activeInHierarchy && minion.hp > 0f && minion.team != team;
    }

    private Color TeamColor()
    {
        return team == MinionTeam.Blue ? new Color(0.16f, 0.58f, 1f, 1f) : new Color(1f, 0.18f, 0.22f, 1f);
    }

    private void TriggerAnimator(string trigger)
    {
        if (animator == null)
            return;

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.name == trigger && parameter.type == AnimatorControllerParameterType.Trigger)
            {
                animator.SetTrigger(trigger);
                return;
            }
        }
    }

    private void SetAnimatorFloat(string parameter, float value)
    {
        if (animator == null)
            return;

        foreach (AnimatorControllerParameter p in animator.parameters)
        {
            if (p.name == parameter && p.type == AnimatorControllerParameterType.Float)
            {
                animator.SetFloat(parameter, value, 0.08f, Time.deltaTime);
                return;
            }
        }
    }

    private static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}

public class AOGMinionProjectile : MonoBehaviour
{
    public Minion source;
    public Transform targetTransform;
    public Minion minionTarget;
    public TowerHealth towerTarget;
    public AOGNexusCore nexusTarget;
    public float damage;
    public float speed = 17f;
    public Color color = Color.cyan;
    public bool cannon;

    private GameObject visual;

    private void Start()
    {
        Destroy(gameObject, 4f);
    }

    public void BuildVisual()
    {
        visual = GameObject.CreatePrimitive(cannon ? PrimitiveType.Sphere : PrimitiveType.Capsule);
        visual.name = "Projectile_Visual";
        visual.transform.SetParent(transform, false);
        visual.transform.localScale = cannon ? Vector3.one * 0.38f : new Vector3(0.16f, 0.38f, 0.16f);
        if (!cannon)
            visual.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        Collider col = visual.GetComponent<Collider>();
        if (col != null) Destroy(col);

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        Material material = new Material(shader) { color = color };
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * (cannon ? 5f : 3.5f));
        }
        visual.GetComponent<Renderer>().sharedMaterial = material;

        TrailRenderer trail = visual.AddComponent<TrailRenderer>();
        trail.time = cannon ? 0.38f : 0.24f;
        trail.startWidth = cannon ? 0.38f : 0.22f;
        trail.endWidth = 0f;
        trail.sharedMaterial = material;
        trail.startColor = color;
        trail.endColor = new Color(color.r, color.g, color.b, 0f);
    }

    private void Update()
    {
        if (targetTransform == null || !targetTransform.gameObject.activeInHierarchy)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 targetPoint = targetTransform.position + Vector3.up * (towerTarget != null || nexusTarget != null ? 2f : 0.9f);
        Vector3 direction = targetPoint - transform.position;
        float distance = direction.magnitude;

        if (distance <= Mathf.Max(0.22f, speed * Time.deltaTime))
        {
            ResolveHit(targetPoint);
            return;
        }

        transform.position += direction.normalized * speed * Time.deltaTime;
        transform.rotation = Quaternion.LookRotation(direction.normalized);
    }

    private void ResolveHit(Vector3 point)
    {
        if (minionTarget != null && minionTarget.hp > 0f)
            minionTarget.TakeDamage(damage, source != null ? source.gameObject : null);
        else if (towerTarget != null && towerTarget.hp > 0f)
            towerTarget.TakeDamage(damage);
        else if (nexusTarget != null && !nexusTarget.IsDestroyed)
            nexusTarget.TakeDamage(damage);

        GameObject ring = AOGAbilityVisuals.CreateRing("Minion_Projectile_Impact", point, cannon ? 0.9f : 0.48f, color, cannon ? 0.10f : 0.055f);
        Destroy(ring,0.35f);
        Destroy(gameObject);
    }
}
