using System.Collections.Generic;

public static class AOGItemCatalogRuntime
{
    public static IReadOnlyList<Item> CreateCoreCatalog()
    {
        return new List<Item>
        {
            new Item
            {
                Name = "Aether Edge",
                Cost = 900,
                Description = "+18 Attack Damage, +0.10 Attack Speed",
                Stats = new StatModifier { AttackDamageBonus = 18f, AttackSpeedBonus = 0.10f }
            },
            new Item
            {
                Name = "Void Prism",
                Cost = 950,
                Description = "+35 Ability Power, +2 Movement Speed",
                Stats = new StatModifier { AbilityPowerBonus = 35f, MovementSpeedBonus = 0.20f }
            },
            new Item
            {
                Name = "Obsidian Aegis",
                Cost = 1050,
                Description = "+24 Armor, +18 Spell Block",
                Stats = new StatModifier { ArmorBonus = 24f, SpellBlockBonus = 18f }
            },
            new Item
            {
                Name = "Vector Relay",
                Cost = 1100,
                Description = "+10 Attack Damage, +0.28 Attack Speed, +0.25 Move Speed",
                Stats = new StatModifier { AttackDamageBonus = 10f, AttackSpeedBonus = 0.28f, MovementSpeedBonus = 0.25f }
            },
            new Item
            {
                Name = "Rift Crown",
                Cost = 1450,
                Description = "+70 Ability Power, +8 Spell Block",
                Stats = new StatModifier { AbilityPowerBonus = 70f, SpellBlockBonus = 8f }
            },
            new Item
            {
                Name = "Titan Protocol",
                Cost = 1500,
                Description = "+30 Attack Damage, +28 Armor",
                Stats = new StatModifier { AttackDamageBonus = 30f, ArmorBonus = 28f }
            }
        };
    }
}
