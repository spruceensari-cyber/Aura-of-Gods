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
    /// <summary>
    /// First original pilot champion. Built as a hybrid mobility battlemage/duelist.
    /// The design intentionally depends on timing, spatial echoes and repeated position choices.
    /// </summary>
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
                new AOGAbilityBlueprint
                {
                    key = AbilityKey.Q,
                    type = AbilityType.Linear,
                    name = "Severing Step",
                    description = "Cut through the aimed line, then create a delayed echo strike from the starting position.",
                    manaCost = 35f,
                    cooldown = 5.5f,
                    castTime = 0.08f,
                    baseDamage = 55f,
                    apRatio = 0.45f,
                    range = 9.5f,
                    radius = 1.4f
                },
                new AOGAbilityBlueprint
                {
                    key = AbilityKey.W,
                    type = AbilityType.Instant,
                    name = "Veil Parry",
                    description = "Raise a short reactive veil. Nearby enemies are marked and Nyxara gains a brief combat rhythm window.",
                    manaCost = 45f,
                    cooldown = 11f,
                    castTime = 0.05f,
                    baseDamage = 35f,
                    apRatio = 0.25f,
                    range = 0.1f,
                    radius = 4.2f
                },
                new AOGAbilityBlueprint
                {
                    key = AbilityKey.E,
                    type = AbilityType.AOE,
                    name = "Memory Well",
                    description = "Open a rift well at the target point. Enemies inside are damaged and the location becomes an anchor for Nyxara's next movement.",
                    manaCost = 60f,
                    cooldown = 10f,
                    castTime = 0.22f,
                    baseDamage = 75f,
                    apRatio = 0.55f,
                    range = 11f,
                    radius = 3.6f
                },
                new AOGAbilityBlueprint
                {
                    key = AbilityKey.R,
                    type = AbilityType.Channeled,
                    name = "Two Futures Collide",
                    description = "Collapse Nyxara's recent movement history into the chosen location, detonating layered echoes in sequence.",
                    manaCost = 100f,
                    cooldown = 70f,
                    castTime = 0.65f,
                    baseDamage = 180f,
                    apRatio = 0.85f,
                    range = 13f,
                    radius = 5.2f
                }
            }
        };
    }
}
