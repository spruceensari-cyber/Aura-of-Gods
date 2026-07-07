using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Main HUD manager - displays all player-facing UI elements
/// Health, mana, abilities, minimap, objectives, etc.
/// </summary>
public class HUDManager : MonoBehaviour
{
    [Header("Health & Mana")]
    [SerializeField] private Image healthBar;
    [SerializeField] private Image manaBar;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI manaText;
    
    [Header("Abilities")]
    [SerializeField] private AbilityButton[] abilityButtons = new AbilityButton[4]; // Q, W, E, R
    
    [Header("Resources")]
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private Image experienceBar;
    
    [Header("Minimap")]
    [SerializeField] private RawImage minimapRender;
    [SerializeField] private Canvas minimapCanvas;
    
    private Champion playerChampion;
    private Camera minimapCamera;
    
    void Start()
    {
        playerChampion = FindObjectOfType<Champion>();
        if (playerChampion != null)
        {
            playerChampion.OnDamaged += UpdateHealthDisplay;
        }
        
        SetupAbilityButtons();
    }
    
    void Update()
    {
        if (playerChampion == null) return;
        
        UpdateHealthDisplay(0, DamageType.Physical);
        UpdateManaDisplay();
    }
    
    private void UpdateHealthDisplay(float damage, DamageType type)
    {
        if (playerChampion == null) return;
        
        float healthPercent = playerChampion.CurrentHealth / 500f; // Placeholder max health
        healthBar.fillAmount = healthPercent;
        healthText.text = $"{playerChampion.CurrentHealth:F0} HP";
    }
    
    private void UpdateManaDisplay()
    {
        if (playerChampion == null) return;
        
        float manaPercent = playerChampion.CurrentMana / 300f; // Placeholder max mana
        manaBar.fillAmount = manaPercent;
        manaText.text = $"{playerChampion.CurrentMana:F0} Mana";
    }
    
    private void SetupAbilityButtons()
    {
        string[] keys = { "Q", "W", "E", "R" };
        for (int i = 0; i < abilityButtons.Length; i++)
        {
            if (abilityButtons[i] != null)
            {
                abilityButtons[i].SetKeyLabel(keys[i]);
            }
        }
    }
}

[System.Serializable]
public class AbilityButton
{
    public Button button;
    public Image cooldownOverlay;
    public TextMeshProUGUI keyLabel;
    public ChampionAbility ability;
    
    public void SetKeyLabel(string key)
    {
        if (keyLabel != null)
            keyLabel.text = key;
    }
    
    public void UpdateCooldown(float percent)
    {
        if (cooldownOverlay != null)
            cooldownOverlay.fillAmount = 1 - percent;
    }
}
