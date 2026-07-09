using UnityEngine;

public class ChampionPresentationController : MonoBehaviour
{
    [Header("Profile")]
    public ChampionPresentationProfile profile;

    [Header("References")]
    public Animator animator;
    public ChampionAudioController audioController;
    public Transform impactSocket;
    public GameObject weaponTrailObject;

    private Vector3 localVelocity;
    private int lastAttackVariant = -1;
    private float lastHitReactionTime;
    private bool dead;

    public float BasicAttackWindup => profile != null ? profile.basicAttackWindup : 0.22f;
    public float BasicAttackRecovery => profile != null ? profile.basicAttackRecovery : 0.18f;

    private void Awake()
    {
        ResolveReferences();
        ApplyProfile();
        SetWeaponTrail(false);
    }

    private void ResolveReferences()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (audioController == null)
            audioController = GetComponent<ChampionAudioController>();
    }

    public void SetProfile(ChampionPresentationProfile newProfile)
    {
        profile = newProfile;
        ResolveReferences();
        ApplyProfile();
    }

    private void ApplyProfile()
    {
        if (profile == null || animator == null)
            return;

        if (profile.animatorController != null && animator.runtimeAnimatorController != profile.animatorController)
            animator.runtimeAnimatorController = profile.animatorController;

        animator.applyRootMotion = false;
    }

    public void SetPlanarVelocity(Vector3 worldVelocity)
    {
        if (dead || animator == null)
            return;

        Vector3 planar = new Vector3(worldVelocity.x, 0f, worldVelocity.z);
        localVelocity = transform.InverseTransformDirection(planar);

        float speed = planar.magnitude;
        float normalizedSpeed = speed <= 0.03f ? 0f : Mathf.Clamp01(speed / 7f);
        float damp = profile != null ? profile.locomotionDamp : 0.08f;

        SetFloat(ProfileString(p => p.speedParameter, "Speed"), normalizedSpeed, damp);
        SetFloat(ProfileString(p => p.moveXParameter, "MoveX"), Mathf.Clamp(localVelocity.x / 7f, -1f, 1f), damp);
        SetFloat(ProfileString(p => p.moveYParameter, "MoveY"), Mathf.Clamp(localVelocity.z / 7f, -1f, 1f), damp);
    }

    public void PlayBasicAttack()
    {
        if (dead)
            return;

        int variantCount = profile != null ? Mathf.Clamp(profile.basicAttackVariants, 1, 3) : 3;
        int next = variantCount == 1 ? 0 : Random.Range(0, variantCount);

        if (variantCount > 1 && next == lastAttackVariant)
            next = (next + 1) % variantCount;

        lastAttackVariant = next;

        if (animator != null)
        {
            SetInteger(ProfileString(p => p.attackIndexParameter, "AttackIndex"), next);
            SetTrigger(ProfileString(p => p.attackTrigger, "Attack"));
        }

        audioController?.PlayAttackWhoosh();
    }

    public void PlayAbility(int slot)
    {
        if (dead)
            return;

        string trigger;
        switch (slot)
        {
            case 0: trigger = ProfileString(p => p.qTrigger, "SkillQ"); break;
            case 1: trigger = ProfileString(p => p.wTrigger, "SkillW"); break;
            case 2: trigger = ProfileString(p => p.eTrigger, "SkillE"); break;
            default: trigger = ProfileString(p => p.rTrigger, "SkillR"); break;
        }

        SetTrigger(trigger);
        audioController?.PlayAbilityCast(Mathf.Clamp(slot, 0, 3));
    }

    public void PlayHitReaction()
    {
        if (dead || Time.time - lastHitReactionTime < 0.18f)
            return;

        lastHitReactionTime = Time.time;
        SetTrigger(ProfileString(p => p.hitTrigger, "Hit"));
        audioController?.PlayHitReaction();
    }

    public void PlayDeath()
    {
        if (dead)
            return;

        dead = true;
        SetWeaponTrail(false);
        SetTrigger(ProfileString(p => p.deathTrigger, "Death"));
        audioController?.PlayDeath();
    }

    public void PlayRecall()
    {
        if (dead)
            return;

        SetTrigger(ProfileString(p => p.recallTrigger, "Recall"));
        audioController?.PlayRecall();
    }

    public void SpawnImpactVfx(Vector3 worldPosition, bool empowered = false, bool ultimate = false)
    {
        if (profile == null)
            return;

        GameObject prefab = ultimate
            ? profile.ultimateImpactVfx
            : empowered
                ? profile.empoweredImpactVfx
                : profile.basicImpactVfx;

        if (prefab == null)
            return;

        GameObject instance = Instantiate(prefab, worldPosition, Quaternion.identity);
        Destroy(instance, 3f);
    }

    public void SpawnAbilityImpactVfx(Vector3 worldPosition, int slot)
    {
        if (profile == null)
            return;

        GameObject prefab = slot == 3 ? profile.ultimateImpactVfx : profile.abilityImpactVfx;
        if (prefab != null)
        {
            GameObject instance = Instantiate(prefab, worldPosition, Quaternion.identity);
            Destroy(instance, 4f);
        }

        audioController?.PlayAbilityImpact(Mathf.Clamp(slot, 0, 3));
    }

    public void SetWeaponTrail(bool enabled)
    {
        if (weaponTrailObject != null && weaponTrailObject.activeSelf != enabled)
            weaponTrailObject.SetActive(enabled);
    }

    // Animation Event hooks. These are presentation-only; gameplay damage remains authoritative in combat code.
    public void AE_Footstep() => audioController?.PlayFootstep();
    public void AE_AttackWhoosh() => audioController?.PlayAttackWhoosh();
    public void AE_AttackImpact() => audioController?.PlayAttackImpact();
    public void AE_WeaponTrailOn() => SetWeaponTrail(true);
    public void AE_WeaponTrailOff() => SetWeaponTrail(false);
    public void AE_QCast() => audioController?.PlayAbilityCast(0);
    public void AE_WCast() => audioController?.PlayAbilityCast(1);
    public void AE_ECast() => audioController?.PlayAbilityCast(2);
    public void AE_RCast() => audioController?.PlayAbilityCast(3);

    private string ProfileString(System.Func<ChampionPresentationProfile, string> selector, string fallback)
    {
        if (profile == null)
            return fallback;

        string value = selector(profile);
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }

    private bool HasParameter(string parameterName, AnimatorControllerParameterType type)
    {
        if (animator == null || string.IsNullOrWhiteSpace(parameterName))
            return false;

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.name == parameterName && parameter.type == type)
                return true;
        }

        return false;
    }

    private void SetTrigger(string parameterName)
    {
        if (HasParameter(parameterName, AnimatorControllerParameterType.Trigger))
            animator.SetTrigger(parameterName);
    }

    private void SetInteger(string parameterName, int value)
    {
        if (HasParameter(parameterName, AnimatorControllerParameterType.Int))
            animator.SetInteger(parameterName, value);
    }

    private void SetFloat(string parameterName, float value, float damp)
    {
        if (HasParameter(parameterName, AnimatorControllerParameterType.Float))
            animator.SetFloat(parameterName, value, damp, Time.deltaTime);
    }
}
