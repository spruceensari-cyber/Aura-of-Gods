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
    private AOGMobaCameraController cameraController;
    private AOGChampionProceduralAnimator proceduralAnimator;
    private AOGAutoAttackPresentationRuntime autoAttackPresentation;

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
        if (animator == null) animator = GetComponentInChildren<Animator>(true);
        if (audioController == null) audioController = GetComponent<ChampionAudioController>();
        if (proceduralAnimator == null) proceduralAnimator = GetComponent<AOGChampionProceduralAnimator>();
        if (autoAttackPresentation == null) autoAttackPresentation = GetComponent<AOGAutoAttackPresentationRuntime>();
        if (cameraController == null && Camera.main != null) cameraController = Camera.main.GetComponent<AOGMobaCameraController>();
    }

    public void SetProfile(ChampionPresentationProfile newProfile)
    {
        profile = newProfile;
        ResolveReferences();
        ApplyProfile();
    }

    private void ApplyProfile()
    {
        if (profile == null || animator == null) return;
        if (profile.animatorController != null && animator.runtimeAnimatorController != profile.animatorController)
            animator.runtimeAnimatorController = profile.animatorController;
        animator.applyRootMotion = false;
    }

    public void SetPlanarVelocity(Vector3 worldVelocity)
    {
        if (dead) return;
        ResolveReferences();
        if (animator == null) return;

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
        if (dead) return;
        ResolveReferences();

        int variantCount = profile != null ? Mathf.Clamp(profile.basicAttackVariants, 1, 3) : 3;
        int next = variantCount == 1 ? 0 : Random.Range(0, variantCount);
        if (variantCount > 1 && next == lastAttackVariant) next = (next + 1) % variantCount;
        lastAttackVariant = next;

        if (animator != null)
        {
            SetInteger(ProfileString(p => p.attackIndexParameter, "AttackIndex"), next);
            SetTrigger(ProfileString(p => p.attackTrigger, "Attack"));
        }

        proceduralAnimator?.PlayAttack(next);
        autoAttackPresentation?.PlayAttack();
        audioController?.PlayAttackWhoosh();
    }

    public void PlayAbility(int slot)
    {
        if (dead) return;
        ResolveReferences();
        string trigger;
        switch (slot)
        {
            case 0: trigger = ProfileString(p => p.qTrigger, "SkillQ"); break;
            case 1: trigger = ProfileString(p => p.wTrigger, "SkillW"); break;
            case 2: trigger = ProfileString(p => p.eTrigger, "SkillE"); break;
            default: trigger = ProfileString(p => p.rTrigger, "SkillR"); break;
        }
        SetTrigger(trigger);
        proceduralAnimator?.PlaySkill(Mathf.Clamp(slot, 0, 3));
        audioController?.PlayAbilityCast(Mathf.Clamp(slot, 0, 3));
    }

    public void PlayHitReaction()
    {
        if (dead || Time.time - lastHitReactionTime < 0.18f) return;
        ResolveReferences();
        lastHitReactionTime = Time.time;
        SetTrigger(ProfileString(p => p.hitTrigger, "Hit"));
        proceduralAnimator?.PlayHit();
        autoAttackPresentation?.PlayHit();
        audioController?.PlayHitReaction();
    }

    public void PlayDeath()
    {
        if (dead) return;
        ResolveReferences();
        dead = true;
        SetWeaponTrail(false);
        SetTrigger(ProfileString(p => p.deathTrigger, "Death"));
        proceduralAnimator?.PlayDeath();
        audioController?.PlayDeath();
        GetCameraController()?.AddRandomImpulse(0.34f);
    }

    public void PlayRecall()
    {
        if (dead) return;
        SetTrigger(ProfileString(p => p.recallTrigger, "Recall"));
        audioController?.PlayRecall();
    }

    public void SpawnImpactVfx(Vector3 worldPosition, bool empowered = false, bool ultimate = false)
    {
        GameObject prefab = null;
        if (profile != null)
            prefab = ultimate ? profile.ultimateImpactVfx : empowered ? profile.empoweredImpactVfx : profile.basicImpactVfx;

        if (prefab != null)
        {
            GameObject instance = Instantiate(prefab, worldPosition, Quaternion.identity);
            Destroy(instance, 3f);
        }
        else
        {
            AOGActiveChampion active = GetComponent<AOGActiveChampion>();
            Color color = active != null ? active.accentColor : new Color(0.42f, 0.62f, 1f, 1f);
            float radius = ultimate ? 1.8f : empowered ? 1.15f : 0.65f;
            GameObject ring = AOGAbilityVisuals.CreateRing("Champion_Impact", worldPosition + Vector3.up * 0.05f, radius, color, ultimate ? 0.16f : 0.075f);
            Destroy(ring, ultimate ? 0.75f : 0.35f);
        }
        GetCameraController()?.AddRandomImpulse(ultimate ? 0.38f : empowered ? 0.22f : 0.10f);
    }

    public void SpawnAbilityImpactVfx(Vector3 worldPosition, int slot)
    {
        GameObject prefab = null;
        if (profile != null) prefab = slot == 3 ? profile.ultimateImpactVfx : profile.abilityImpactVfx;

        if (prefab != null)
        {
            GameObject instance = Instantiate(prefab, worldPosition, Quaternion.identity);
            Destroy(instance, 4f);
        }
        else
        {
            AOGActiveChampion active = GetComponent<AOGActiveChampion>();
            Color color = active != null ? active.accentColor : new Color(0.52f, 0.28f, 0.92f, 1f);
            float radius = slot == 3 ? 2.4f : 1.15f;
            GameObject ring = AOGAbilityVisuals.CreateRing("Ability_Impact_" + slot, worldPosition + Vector3.up * 0.04f, radius, color, slot == 3 ? 0.18f : 0.08f);
            Destroy(ring, slot == 3 ? 0.9f : 0.45f);
        }
        audioController?.PlayAbilityImpact(Mathf.Clamp(slot, 0, 3));
        GetCameraController()?.AddRandomImpulse(slot == 3 ? 0.40f : 0.18f);
    }

    public void SetWeaponTrail(bool enabled)
    {
        if (weaponTrailObject != null && weaponTrailObject.activeSelf != enabled) weaponTrailObject.SetActive(enabled);
    }

    public void AE_Footstep() => audioController?.PlayFootstep();
    public void AE_AttackWhoosh() => audioController?.PlayAttackWhoosh();
    public void AE_AttackImpact() => audioController?.PlayAttackImpact();
    public void AE_WeaponTrailOn() => SetWeaponTrail(true);
    public void AE_WeaponTrailOff() => SetWeaponTrail(false);
    public void AE_QCast() => audioController?.PlayAbilityCast(0);
    public void AE_WCast() => audioController?.PlayAbilityCast(1);
    public void AE_ECast() => audioController?.PlayAbilityCast(2);
    public void AE_RCast() => audioController?.PlayAbilityCast(3);

    private AOGMobaCameraController GetCameraController()
    {
        if (cameraController == null && Camera.main != null) cameraController = Camera.main.GetComponent<AOGMobaCameraController>();
        return cameraController;
    }

    private string ProfileString(System.Func<ChampionPresentationProfile, string> selector, string fallback)
    {
        if (profile == null) return fallback;
        string value = selector(profile);
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }

    private bool HasParameter(string parameterName, AnimatorControllerParameterType type)
    {
        if (animator == null || string.IsNullOrWhiteSpace(parameterName)) return false;
        foreach (AnimatorControllerParameter parameter in animator.parameters)
            if (parameter.name == parameterName && parameter.type == type) return true;
        return false;
    }

    private void SetTrigger(string parameterName)
    {
        if (HasParameter(parameterName, AnimatorControllerParameterType.Trigger)) animator.SetTrigger(parameterName);
    }

    private void SetInteger(string parameterName, int value)
    {
        if (HasParameter(parameterName, AnimatorControllerParameterType.Int)) animator.SetInteger(parameterName, value);
    }

    private void SetFloat(string parameterName, float value, float damp)
    {
        if (HasParameter(parameterName, AnimatorControllerParameterType.Float)) animator.SetFloat(parameterName, value, damp, Time.deltaTime);
    }
}
