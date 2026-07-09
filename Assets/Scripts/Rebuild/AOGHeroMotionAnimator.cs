using System.Collections;
using UnityEngine;

/// <summary>
/// Lightweight motion layer for the rebuild slice. Adds breathing, locomotion lean, attack recoil and cast impulses
/// to authored or procedural character visuals without requiring a specific Animator Controller.
/// </summary>
public class AOGHeroMotionAnimator : MonoBehaviour
{
    private AOGOriginalHeroId heroId;
    private ChampionController controller;
    private Champion champion;
    private Transform visualRoot;
    private Vector3 baseLocalPosition;
    private Quaternion baseLocalRotation;
    private float attackImpulse;
    private float castImpulse;
    private float hitImpulse;
    private bool initialized;

    public void Initialize(AOGOriginalHeroId id)
    {
        heroId = id;
        controller = GetComponent<ChampionController>();
        champion = GetComponent<Champion>();
        ResolveVisualRoot();
        Subscribe();
        initialized = true;
    }

    private void Awake()
    {
        controller = GetComponent<ChampionController>();
        champion = GetComponent<Champion>();
    }

    private void Start()
    {
        if (!initialized)
        {
            ResolveVisualRoot();
            Subscribe();
            initialized = true;
        }
    }

    private void ResolveVisualRoot()
    {
        SkinnedMeshRenderer skinned = GetComponentInChildren<SkinnedMeshRenderer>(true);
        Renderer renderer = skinned != null ? skinned : GetComponentInChildren<Renderer>(true);
        visualRoot = renderer != null ? renderer.transform.parent ?? renderer.transform : transform;
        baseLocalPosition = visualRoot.localPosition;
        baseLocalRotation = visualRoot.localRotation;
    }

    private void Subscribe()
    {
        if (controller != null)
        {
            controller.OnBasicAttackWindup -= HandleAttack;
            controller.OnBasicAttackWindup += HandleAttack;
            controller.OnAbilityInputResolved -= HandleAbility;
            controller.OnAbilityInputResolved += HandleAbility;
        }

        if (champion != null)
        {
            champion.OnDamaged -= HandleDamage;
            champion.OnDamaged += HandleDamage;
            champion.OnDeath -= HandleDeath;
            champion.OnDeath += HandleDeath;
        }
    }

    private void Update()
    {
        if (visualRoot == null) return;

        float speed = 0f;
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) speed = new Vector3(rb.velocity.x, 0f, rb.velocity.z).magnitude;
        else if (controller != null && controller.IsMoving) speed = 1f;

        float time = Time.time;
        float breath = Mathf.Sin(time * 2.2f) * 0.018f;
        float stride = Mathf.Sin(time * Mathf.Lerp(2.5f, 8f, Mathf.Clamp01(speed / 6f))) * Mathf.Clamp01(speed / 6f);

        attackImpulse = Mathf.MoveTowards(attackImpulse, 0f, Time.deltaTime * 8f);
        castImpulse = Mathf.MoveTowards(castImpulse, 0f, Time.deltaTime * 5f);
        hitImpulse = Mathf.MoveTowards(hitImpulse, 0f, Time.deltaTime * 10f);

        Vector3 localPos = baseLocalPosition;
        localPos.y += breath + Mathf.Abs(stride) * 0.025f;
        localPos.z -= attackImpulse * 0.08f;

        float heroLean = heroId switch
        {
            AOGOriginalHeroId.SorynPrismHuntress => 5f,
            AOGOriginalHeroId.CaelixRiftVanguard => 2.5f,
            _ => 3.5f
        };

        Quaternion motion = Quaternion.Euler(
            stride * 1.5f + hitImpulse * 4f,
            castImpulse * 5f,
            -stride * heroLean - attackImpulse * 4f);

        visualRoot.localPosition = Vector3.Lerp(visualRoot.localPosition, localPos, 1f - Mathf.Exp(-12f * Time.deltaTime));
        visualRoot.localRotation = Quaternion.Slerp(visualRoot.localRotation, baseLocalRotation * motion, 1f - Mathf.Exp(-10f * Time.deltaTime));
    }

    private void HandleAttack()
    {
        attackImpulse = 1f;
    }

    private void HandleAbility(AbilityKey key, bool success)
    {
        if (!success) return;
        castImpulse = key == AbilityKey.R ? 1.8f : 1f;
    }

    private void HandleDamage(float damage, DamageType type)
    {
        hitImpulse = Mathf.Clamp(damage / 120f, 0.35f, 1f);
    }

    private void HandleDeath()
    {
        if (visualRoot != null)
            StartCoroutine(DeathMotion());
    }

    private IEnumerator DeathMotion()
    {
        Quaternion start = visualRoot.localRotation;
        Quaternion end = start * Quaternion.Euler(0f, 0f, 78f);
        float elapsed = 0f;
        while (visualRoot != null && elapsed < 0.45f)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / 0.45f);
            visualRoot.localRotation = Quaternion.Slerp(start, end, t * t);
            yield return null;
        }
    }

    private void OnDestroy()
    {
        if (controller != null)
        {
            controller.OnBasicAttackWindup -= HandleAttack;
            controller.OnAbilityInputResolved -= HandleAbility;
        }

        if (champion != null)
        {
            champion.OnDamaged -= HandleDamage;
            champion.OnDeath -= HandleDeath;
        }
    }
}
