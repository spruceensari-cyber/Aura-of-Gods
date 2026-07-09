using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Per-champion inventory with purchase, sell and undo support.
/// Applies item stat modifiers directly to Champion while keeping slots data-driven.
/// </summary>
public class AOGChampionInventoryRuntime : MonoBehaviour
{
    public const int MaxSlots = 6;

    private Champion champion;
    private readonly List<Item> items = new();
    private readonly List<StatModifier> modifiers = new();
    private Item lastPurchased;
    private StatModifier lastModifier;
    private int lastPurchaseCost;

    public IReadOnlyList<Item> Items => items;
    public bool IsFull => items.Count >= MaxSlots;

    void Awake()
    {
        champion = GetComponent<Champion>();
    }

    public bool TryBuy(Item item)
    {
        if (champion == null || item == null || item.Stats == null || IsFull)
            return false;

        if (!champion.SpendGold(item.Cost))
            return false;

        StatModifier modifier = Clone(item.Stats);
        champion.AddStatModifier(modifier);
        items.Add(item);
        modifiers.Add(modifier);
        lastPurchased = item;
        lastModifier = modifier;
        lastPurchaseCost = item.Cost;
        return true;
    }

    public bool TrySell(int slot, float refundRatio = 0.7f)
    {
        if (champion == null || slot < 0 || slot >= items.Count)
            return false;

        Item item = items[slot];
        StatModifier modifier = modifiers[slot];
        champion.RemoveStatModifier(modifier);
        items.RemoveAt(slot);
        modifiers.RemoveAt(slot);
        champion.GainGold(Mathf.RoundToInt(item.Cost * Mathf.Clamp01(refundRatio)));
        return true;
    }

    public bool UndoLastPurchase()
    {
        if (champion == null || lastPurchased == null || lastModifier == null)
            return false;

        int index = modifiers.IndexOf(lastModifier);
        if (index < 0)
            return false;

        champion.RemoveStatModifier(lastModifier);
        modifiers.RemoveAt(index);
        items.RemoveAt(index);
        champion.GainGold(lastPurchaseCost);
        lastPurchased = null;
        lastModifier = null;
        lastPurchaseCost = 0;
        return true;
    }

    private static StatModifier Clone(StatModifier source)
    {
        return new StatModifier
        {
            AttackDamageBonus = source.AttackDamageBonus,
            AbilityPowerBonus = source.AbilityPowerBonus,
            ArmorBonus = source.ArmorBonus,
            SpellBlockBonus = source.SpellBlockBonus,
            MovementSpeedBonus = source.MovementSpeedBonus,
            AttackSpeedBonus = source.AttackSpeedBonus
        };
    }
}
