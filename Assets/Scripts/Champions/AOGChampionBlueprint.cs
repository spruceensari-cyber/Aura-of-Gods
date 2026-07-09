using System;
using UnityEngine;

public enum AOGCombatRole
{
    Vanguard,
    Duelist,
    Assassin,
    Battlemage,
    Marksman,
    Controller,
    Support
}

[Serializable]
public class AOGAbilityBlueprint
{
    public AbilityKey key;
    public AbilityType type;
    public string name;
    public string description;
    public float manaCost;
    public float cooldown;
    public float castTime;
    public float baseDamage;
    public float apRatio;
    public float range;
    public float radius;
}

[Serializable]
public class AOGChampionBlueprint
{
    public string id;
    public string displayName;
    public string title;
    public string fantasy;
    public AOGCombatRole primaryRole;
    public AOGCombatRole secondaryRole;

    public float bonusAttackDamage;
    public float bonusAbilityPower;
    public float bonusArmor;
    public float bonusSpellBlock;
    public float bonusMoveSpeed;
    public float bonusAttackSpeed;

    public AOGAbilityBlueprint[] abilities;

    public StatModifier CreateStatModifier()
    {
        return new StatModifier
        {
            AttackDamageBonus = bonusAttackDamage,
            AbilityPowerBonus = bonusAbilityPower,
            ArmorBonus = bonusArmor,
            SpellBlockBonus = bonusSpellBlock,
            MovementSpeedBonus = bonusMoveSpeed,
            AttackSpeedBonus = bonusAttackSpeed
        };
    }
}

public static class AOGChampionCatalog
{
    public static AOGChampionBlueprint CreateNyxara()
    {
        return new AOGChampionBlueprint
        {
            id = "nyxara_rift_dancer",
            displayName = "Nyxara",
            title = "Rift Dancer",
            fantasy = "A duelist who leaves temporal echoes behind every decisive movement and weaponizes the distance between past and present positions.",
            primaryRole = AOGCombatRole.Duelist,
            secondaryRole = AOGCombatRole.Battlemage,
            bonusAttackDamage = 6f,
            bonusAbilityPower = 22f,
            bonusArmor = 4f,
            bonusSpellBlock = 2f,
            bonusMoveSpeed = 0.45f,
            bonusAttackSpeed = 0.12f,
            abilities = new[]
            {
                Ability(AbilityKey.Q, AbilityType.Linear, "Severing Step", "Cut through the aimed line, then create a delayed echo strike from the starting position.", 35f, 5.5f, 0.08f, 55f, 0.45f, 9.5f, 1.4f),
                Ability(AbilityKey.W, AbilityType.Instant, "Veil Parry", "Raise a short reactive veil. Nearby enemies are marked and Nyxara gains a brief combat rhythm window.", 45f, 11f, 0.05f, 35f, 0.25f, 0.1f, 4.2f),
                Ability(AbilityKey.E, AbilityType.AOE, "Memory Well", "Open a rift well at the target point. Enemies inside are damaged and the location becomes an anchor for Nyxara's next movement.", 60f, 10f, 0.22f, 75f, 0.55f, 11f, 3.6f),
                Ability(AbilityKey.R, AbilityType.Channeled, "Two Futures Collide", "Collapse Nyxara's recent movement history into the chosen location, detonating layered echoes in sequence.", 100f, 70f, 0.65f, 180f, 0.85f, 13f, 5.2f)
            }
        };
    }

    /// <summary>
    /// Heavy Vanguard built around creating temporary terrain pressure and controlling routes.
    /// </summary>
    public static AOGChampionBlueprint CreateKharvos()
    {
        return new AOGChampionBlueprint
        {
            id = "kharvos_worldbreaker",
            displayName = "Kharvos",
            title = "Worldbreaker",
            fantasy = "A living fault line who turns space itself into a weapon, raising walls, splitting lanes and forcing fights through unstable ground.",
            primaryRole = AOGCombatRole.Vanguard,
            secondaryRole = AOGCombatRole.Controller,
            bonusAttackDamage = 12f,
            bonusAbilityPower = 8f,
            bonusArmor = 16f,
            bonusSpellBlock = 12f,
            bonusMoveSpeed = -0.25f,
            bonusAttackSpeed = -0.08f,
            abilities = new[]
            {
                Ability(AbilityKey.Q, AbilityType.Linear, "Faultline Rush", "Charge along a fracture path and erupt stone behind the impact line.", 40f, 7f, 0.12f, 70f, 0.30f, 8.5f, 1.8f),
                Ability(AbilityKey.W, AbilityType.AOE, "Gravestone Rampart", "Raise a temporary stone wall that blocks passage and splits team formations.", 55f, 14f, 0.28f, 35f, 0.15f, 8f, 4.6f),
                Ability(AbilityKey.E, AbilityType.Instant, "Seismic Guard", "Harden the body, pulse nearby enemies and gain a short defensive window.", 50f, 11f, 0.05f, 45f, 0.20f, 0.1f, 4.5f),
                Ability(AbilityKey.R, AbilityType.AOE, "Worldbreak", "Create a collapsing ring of stone that reshapes the fight and detonates toward the center.", 100f, 85f, 0.8f, 210f, 0.55f, 10f, 7.5f)
            }
        };
    }

    private static AOGAbilityBlueprint Ability(
        AbilityKey key,
        AbilityType type,
        string name,
        string description,
        float mana,
        float cooldown,
        float castTime,
        float damage,
        float apRatio,
        float range,
        float radius)
    {
        return new AOGAbilityBlueprint
        {
            key = key,
            type = type,
            name = name,
            description = description,
            manaCost = mana,
            cooldown = cooldown,
            castTime = castTime,
            baseDamage = damage,
            apRatio = apRatio,
            range = range,
            radius = radius
        };
    }
}
