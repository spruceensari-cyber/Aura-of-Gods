using UnityEngine;

public enum ChampionArchetype
{
    Duelist,
    ArcaneCaster,
    Guardian
}

[CreateAssetMenu(menuName = "Aura of Gods/Champion Presentation Profile", fileName = "ChampionPresentationProfile")]
public class ChampionPresentationProfile : ScriptableObject
{
    [Header("Identity")]
    public string championId = "astryn";
    public string displayName = "Astryn";
    public ChampionArchetype archetype = ChampionArchetype.Duelist;

    [Header("Animation")]
    public RuntimeAnimatorController animatorController;
    [Min(0.01f)] public float locomotionDamp = 0.08f;
    [Min(0f)] public float basicAttackWindup = 0.22f;
    [Min(0f)] public float basicAttackRecovery = 0.18f;
    [Range(1, 3)] public int basicAttackVariants = 3;

    [Header("Animator Parameters")]
    public string speedParameter = "Speed";
    public string moveXParameter = "MoveX";
    public string moveYParameter = "MoveY";
    public string attackIndexParameter = "AttackIndex";
    public string attackTrigger = "Attack";
    public string qTrigger = "SkillQ";
    public string wTrigger = "SkillW";
    public string eTrigger = "SkillE";
    public string rTrigger = "SkillR";
    public string hitTrigger = "Hit";
    public string deathTrigger = "Death";
    public string recallTrigger = "Recall";

    [Header("Presentation")]
    public GameObject basicImpactVfx;
    public GameObject empoweredImpactVfx;
    public GameObject abilityImpactVfx;
    public GameObject ultimateImpactVfx;
}
