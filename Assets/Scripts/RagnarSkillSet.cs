using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RagnarSkillSet : MonoBehaviour
{
    [Header("Team")]
    public MinionTeam team = MinionTeam.Blue;

    [Header("References")]
    public Animator animator;

    // =========================================================
    // COOLDOWNS
    // =========================================================

    [Header("Cooldowns")]
    public float qCooldown = 5f;
    public float wCooldown = 10f;
    public float eCooldown = 9f;
    public float rCooldown = 45f;

    // =========================================================
    // Q - MAGMA CLEAVE
    // =========================================================

    [Header("Q - Magma Cleave")]
    public float qRange = 5.5f;
    public float qDamage = 95f;
    public float qAngle = 80f;

    // =========================================================
    // W - VOLCANIC SKIN
    // =========================================================

    [Header("W - Volcanic Skin")]
    public float wDuration = 4f;
    public float wBonusHealth = 180f;

    public float wAuraDamage = 20f;
    public float wAuraRange = 3.2f;
    public float wAuraTickRate = 0.5f;

    // =========================================================
    // E - ASHEN CHARGE
    // =========================================================

    [Header("E - Ashen Charge")]
    public float eDistance = 6f;
    public float eDamage = 80f;
    public float eHitRange = 1.7f;
    public float eDashDuration = 0.25f;

    // =========================================================
    // R - WORLD BREAKER
    // =========================================================

    [Header("R - World Breaker")]
    public float rCastRange = 8f;
    public float rRadius = 5.5f;
    public float rDamage = 220f;

    // =========================================================
    // VISUALS
    // =========================================================

    [Header("Prototype Visuals")]
    public Color ragnarColor =
        new Color(1f, 0.2f, 0.02f, 1f);

    public float visualLife = 0.65f;

    // =========================================================
    // INTERNAL VALUES
    // =========================================================

    private float nextQTime;
    private float nextWTime;
    private float nextETime;
    private float nextRTime;

    private bool volcanicSkinActive;
    private bool bonusHealthApplied;

    private float volcanicSkinEndTime;
    private float nextAuraTickTime;

    private bool isDashing;

    private AOGCharacterStats stats;

    // =========================================================
    // UNITY
    // =========================================================

    void Awake()
    {
        stats = GetComponent<AOGCharacterStats>();

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    void Update()
    {
        UpdateVolcanicSkin();
    }

    // =========================================================
    // Q - MAGMA CLEAVE
    // =========================================================

    public bool TryCastQ(Vector3 aimPoint)
    {
        if (Time.time < nextQTime)
        {
            Debug.Log("Ragnar Q cooldown.");
            return false;
        }

        if (isDashing)
            return false;

        Vector3 direction =
            GetFlatDirection(aimPoint);

        if (direction.sqrMagnitude < 0.01f)
            return false;

        FaceDirection(direction);

        nextQTime =
            Time.time + qCooldown;

        PlayAnim("SkillQ");

        Minion[] minions =
            FindObjectsByType<Minion>(
                FindObjectsSortMode.None
            );

        int hitCount = 0;

        foreach (Minion minion in minions)
        {
            if (!IsValidEnemy(minion))
                continue;

            Vector3 toEnemy =
                minion.transform.position -
                transform.position;

            toEnemy.y = 0f;

            float distance =
                toEnemy.magnitude;

            if (distance > qRange)
                continue;

            if (toEnemy.sqrMagnitude < 0.01f)
                continue;

            float angle =
                Vector3.Angle(
                    direction,
                    toEnemy.normalized
                );

            if (angle <= qAngle * 0.5f)
            {
                minion.TakeDamage(
                    qDamage,
                    gameObject
                );

                hitCount++;
            }
        }

        CreateGroundVisual(
            transform.position +
            direction * (qRange * 0.45f),

            2.4f,

            "Ragnar_Q_Magma_Cleave"
        );

        Debug.Log(
            "Ragnar Q kullandı. Hit: " +
            hitCount
        );

        return true;
    }

    // =========================================================
    // W - VOLCANIC SKIN
    // =========================================================

    public bool TryCastW()
    {
        if (Time.time < nextWTime)
        {
            Debug.Log("Ragnar W cooldown.");
            return false;
        }

        if (isDashing)
            return false;

        nextWTime =
            Time.time + wCooldown;

        volcanicSkinActive = true;

        volcanicSkinEndTime =
            Time.time + wDuration;

        nextAuraTickTime = 0f;

        ApplyBonusHealth();

        PlayAnim("SkillW");

        CreateGroundVisual(
            transform.position,
            wAuraRange,
            "Ragnar_W_Volcanic_Skin"
        );

        Debug.Log(
            "Ragnar W kullandı: Volcanic Skin"
        );

        return true;
    }

    void UpdateVolcanicSkin()
    {
        if (!volcanicSkinActive)
            return;

        if (Time.time >= volcanicSkinEndTime)
        {
            volcanicSkinActive = false;

            RemoveBonusHealth();

            Debug.Log(
                "Ragnar Volcanic Skin sona erdi."
            );

            return;
        }

        if (Time.time >= nextAuraTickTime)
        {
            nextAuraTickTime =
                Time.time + wAuraTickRate;

            DamageEnemiesInRadius(
                transform.position,
                wAuraRange,
                wAuraDamage
            );
        }
    }

    void ApplyBonusHealth()
    {
        if (stats == null)
            return;

        if (bonusHealthApplied)
            return;

        stats.maxHp += wBonusHealth;
        stats.hp += wBonusHealth;

        bonusHealthApplied = true;
    }

    void RemoveBonusHealth()
    {
        if (stats == null)
            return;

        if (!bonusHealthApplied)
            return;

        stats.maxHp -= wBonusHealth;

        stats.hp = Mathf.Min(
            stats.hp,
            stats.maxHp
        );

        bonusHealthApplied = false;
    }

    // =========================================================
    // E - ASHEN CHARGE
    // =========================================================

    public bool TryCastE(Vector3 aimPoint)
    {
        if (Time.time < nextETime)
        {
            Debug.Log("Ragnar E cooldown.");
            return false;
        }

        if (isDashing)
            return false;

        Vector3 direction =
            GetFlatDirection(aimPoint);

        if (direction.sqrMagnitude < 0.01f)
            return false;

        nextETime =
            Time.time + eCooldown;

        FaceDirection(direction);

        PlayAnim("SkillE");

        StartCoroutine(
            DashRoutine(direction)
        );

        return true;
    }

    IEnumerator DashRoutine(Vector3 direction)
    {
        isDashing = true;

        Vector3 start =
            transform.position;

        Vector3 destination =
            start +
            direction.normalized *
            eDistance;

        HashSet<int> damagedEnemies =
            new HashSet<int>();

        // Sadece bir kere bulunur.
        // Her kare FindObjectsByType çalışmaz.
        Minion[] dashTargets =
            FindObjectsByType<Minion>(
                FindObjectsSortMode.None
            );

        float elapsed = 0f;

        while (elapsed < eDashDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(
                elapsed /
                Mathf.Max(0.01f, eDashDuration)
            );

            transform.position =
                Vector3.Lerp(
                    start,
                    destination,
                    t
                );

            DamageChargePath(
                damagedEnemies,
                dashTargets
            );

            yield return null;
        }

        transform.position =
            destination;

        DamageChargePath(
            damagedEnemies,
            dashTargets
        );

        CreateGroundVisual(
            transform.position,
            eHitRange,
            "Ragnar_E_Ashen_Charge"
        );

        isDashing = false;

        Debug.Log(
            "Ragnar E kullandı. Hit: " +
            damagedEnemies.Count
        );
    }

    void DamageChargePath(
        HashSet<int> damagedEnemies,
        Minion[] targets
    )
    {
        if (targets == null)
            return;

        foreach (Minion minion in targets)
        {
            if (!IsValidEnemy(minion))
                continue;

            int id =
                minion.GetInstanceID();

            if (damagedEnemies.Contains(id))
                continue;

            float distance =
                FlatDistance(
                    transform.position,
                    minion.transform.position
                );

            if (distance <= eHitRange)
            {
                minion.TakeDamage(
                    eDamage,
                    gameObject
                );

                damagedEnemies.Add(id);
            }
        }
    }

    // =========================================================
    // R - WORLD BREAKER
    // =========================================================

    public bool TryCastR(Vector3 aimPoint)
    {
        if (Time.time < nextRTime)
        {
            Debug.Log("Ragnar R cooldown.");
            return false;
        }

        if (isDashing)
            return false;

        Vector3 castPoint =
            ClampPointToRange(
                aimPoint,
                rCastRange
            );

        Vector3 direction =
            GetFlatDirection(castPoint);

        FaceDirection(direction);

        nextRTime =
            Time.time + rCooldown;

        PlayAnim("SkillR");

        int hitCount =
            DamageEnemiesInRadius(
                castPoint,
                rRadius,
                rDamage
            );

        CreateGroundVisual(
            castPoint,
            rRadius,
            "Ragnar_R_World_Breaker"
        );

        Debug.Log(
            "Ragnar R kullandı. Hit: " +
            hitCount
        );

        return true;
    }

    // =========================================================
    // DAMAGE HELPERS
    // =========================================================

    int DamageEnemiesInRadius(
        Vector3 center,
        float radius,
        float damage
    )
    {
        Minion[] minions =
            FindObjectsByType<Minion>(
                FindObjectsSortMode.None
            );

        int hitCount = 0;

        foreach (Minion minion in minions)
        {
            if (!IsValidEnemy(minion))
                continue;

            float distance =
                FlatDistance(
                    center,
                    minion.transform.position
                );

            if (distance <= radius)
            {
                minion.TakeDamage(
                    damage,
                    gameObject
                );

                hitCount++;
            }
        }

        return hitCount;
    }

    bool IsValidEnemy(Minion minion)
    {
        if (minion == null)
            return false;

        if (!minion.gameObject.activeInHierarchy)
            return false;

        if (minion.hp <= 0f)
            return false;

        if (minion.team == team)
            return false;

        return true;
    }

    // =========================================================
    // AIM / POSITION
    // =========================================================

    Vector3 GetFlatDirection(Vector3 targetPoint)
    {
        Vector3 direction =
            targetPoint -
            transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return Vector3.zero;

        return direction.normalized;
    }

    Vector3 ClampPointToRange(
        Vector3 point,
        float maxRange
    )
    {
        Vector3 delta =
            point -
            transform.position;

        delta.y = 0f;

        if (delta.magnitude > maxRange)
        {
            delta =
                delta.normalized *
                maxRange;
        }

        Vector3 result =
            transform.position +
            delta;

        result.y =
            transform.position.y;

        return result;
    }

    void FaceDirection(Vector3 direction)
    {
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.01f)
            return;

        transform.rotation =
            Quaternion.LookRotation(direction);
    }

    float FlatDistance(
        Vector3 a,
        Vector3 b
    )
    {
        a.y = 0f;
        b.y = 0f;

        return Vector3.Distance(a, b);
    }

    // =========================================================
    // ANIMATION
    // =========================================================

    void PlayAnim(string triggerName)
    {
        if (animator == null)
        {
            animator =
                GetComponentInChildren<Animator>();
        }

        if (animator == null)
            return;

        // Animator Controller'da trigger yoksa
        // Unity Console'a sürekli hata basmaz.
        if (!HasAnimatorParameter(
                animator,
                triggerName,
                AnimatorControllerParameterType.Trigger
            ))
        {
            return;
        }

        animator.SetTrigger(triggerName);
    }

    bool HasAnimatorParameter(
        Animator targetAnimator,
        string parameterName,
        AnimatorControllerParameterType parameterType
    )
    {
        if (targetAnimator == null)
            return false;

        foreach (
            AnimatorControllerParameter parameter
            in targetAnimator.parameters
        )
        {
            if (
                parameter.name == parameterName &&
                parameter.type == parameterType
            )
            {
                return true;
            }
        }

        return false;
    }

    // =========================================================
    // PROTOTYPE VISUALS
    // =========================================================

    void CreateGroundVisual(
        Vector3 position,
        float size,
        string visualName
    )
    {
        GameObject visual =
            GameObject.CreatePrimitive(
                PrimitiveType.Cylinder
            );

        visual.name =
            visualName;

        visual.transform.position =
            new Vector3(
                position.x,
                transform.position.y + 0.04f,
                position.z
            );

        visual.transform.localScale =
            new Vector3(
                size,
                0.025f,
                size
            );

        Collider col =
            visual.GetComponent<Collider>();

        if (col != null)
        {
            Destroy(col);
        }

        Renderer visualRenderer =
            visual.GetComponent<Renderer>();

        if (visualRenderer != null)
        {
            Shader shader =
                Shader.Find(
                    "Universal Render Pipeline/Unlit"
                );

            if (shader == null)
            {
                shader =
                    Shader.Find(
                        "Universal Render Pipeline/Lit"
                    );
            }

            if (shader != null)
            {
                Material material =
                    new Material(shader);

                if (
                    material.HasProperty(
                        "_BaseColor"
                    )
                )
                {
                    material.SetColor(
                        "_BaseColor",
                        ragnarColor
                    );
                }

                visualRenderer.material =
                    material;
            }
        }

        Destroy(
            visual,
            visualLife
        );
    }

    // =========================================================
    // COOLDOWN INFORMATION
    // =========================================================

    public float GetQCooldownRatio()
    {
        if (Time.time >= nextQTime)
            return 0f;

        return Mathf.Clamp01(
            (nextQTime - Time.time) /
            Mathf.Max(0.01f, qCooldown)
        );
    }

    public float GetWCooldownRatio()
    {
        if (Time.time >= nextWTime)
            return 0f;

        return Mathf.Clamp01(
            (nextWTime - Time.time) /
            Mathf.Max(0.01f, wCooldown)
        );
    }

    public float GetECooldownRatio()
    {
        if (Time.time >= nextETime)
            return 0f;

        return Mathf.Clamp01(
            (nextETime - Time.time) /
            Mathf.Max(0.01f, eCooldown)
        );
    }

    public float GetRCooldownRatio()
    {
        if (Time.time >= nextRTime)
            return 0f;

        return Mathf.Clamp01(
            (nextRTime - Time.time) /
            Mathf.Max(0.01f, rCooldown)
        );
    }

    // =========================================================
    // PUBLIC STATE
    // =========================================================

    public bool IsDashing()
    {
        return isDashing;
    }

    public bool IsVolcanicSkinActive()
    {
        return volcanicSkinActive;
    }

    public bool IsQReady()
    {
        return Time.time >= nextQTime;
    }

    public bool IsWReady()
    {
        return Time.time >= nextWTime;
    }

    public bool IsEReady()
    {
        return Time.time >= nextETime;
    }

    public bool IsRReady()
    {
        return Time.time >= nextRTime;
    }
}