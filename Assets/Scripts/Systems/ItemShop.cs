using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
Professional item shop system - buy/sell items, passive gold income display
/// </summary>
public class ItemShop : MonoBehaviour
{
    [SerializeField] private Canvas shopCanvas;
    [SerializeField] private TextMeshProUGUI goldDisplay;
    [SerializeField] private TextMeshProUGUI incomeDisplay;
    [SerializeField] private GameObject itemButtonPrefab;
    [SerializeField] private Transform itemGridParent;
    
    private Champion playerChampion;
    private List<Item> availableItems = new();
    private int currentGold;
    private float passiveIncomePerSecond = 1.25f;
    
    void Start()
    {
        playerChampion = FindObjectOfType<Champion>();
        InitializeItems();
    }
    
    void Update()
    {
        UpdateGoldDisplay();
    }
    
    private void InitializeItems()
    {
        // Basic items
        availableItems.Add(new Item
        {
            Name = "Longsword",
            Cost = 360,
            Stats = new StatModifier { AttackDamageBonus = 10f },
            Description = "+10 Attack Damage"
        });
        
        availableItems.Add(new Item
        {
            Name = "Blasting Wand",
            Cost = 850,
            Stats = new StatModifier { AbilityPowerBonus = 40f },
            Description = "+40 Ability Power"
        });
        
        availableItems.Add(new Item
        {
            Name = "Spectre's Cowl",
            Cost = 1200,
            Stats = new StatModifier { SpellBlockBonus = 25f },
            Description = "+25 Spell Block, +150 HP (calculated separately)"
        });
        
        DisplayItems();
    }
    
    private void DisplayItems()
    {
        foreach (Item item in availableItems)
        {
            GameObject btn = Instantiate(itemButtonPrefab, itemGridParent);
            Button button = btn.GetComponent<Button>();
            TextMeshProUGUI text = btn.GetComponentInChildren<TextMeshProUGUI>();
            
            text.text = $"{item.Name}\n{item.Cost}g";
            button.onClick.AddListener(() => BuyItem(item));
        }
    }
    
    private void BuyItem(Item item)
    {
        if (currentGold >= item.Cost)
        {
            currentGold -= item.Cost;
            playerChampion.AddStatModifier(item.Stats);
            Debug.Log($"Bought {item.Name}!");
        }
        else
        {
            Debug.Log("Not enough gold!");
        }
    }
    
    private void UpdateGoldDisplay()
    {
        if (goldDisplay != null)
            goldDisplay.text = $"Gold: {currentGold}";
        
        if (incomeDisplay != null)
            incomeDisplay.text = $"Income: +{passiveIncomePerSecond}/sec";
    }
    
    public void AddGold(int amount)
    {
        currentGold += amount;
    }
}
