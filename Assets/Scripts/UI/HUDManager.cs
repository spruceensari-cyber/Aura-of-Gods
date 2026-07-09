using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Player HUD binding layer. Uses live champion values rather than hard-coded placeholders.
/// Designed so the visual prefab can be replaced later without changing gameplay systems.
/// </summary>
public class HUDManager : MonoBehaviour
{
    [Header("Health & Mana")]
    [SerializeField] private Image healthBar;
    [SerializeField] private Image manaBar;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI manaText;

    [Header("Abilities")]
    [SerializeField] private AbilityButton[] abilityButtons = new AbilityButton[4];

    [Header("Resources")]
    [SerializeField] private TextMeshProUGUI goldText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private Image experienceBar;

    [Header("Minimap")]
    [SerializeField] private RawImage minimapRender;
    [SerializeField] private Canvas minimapCanvas;

    private Champion playerChampion;
    private ChampionAbility[] playerAbilities;

    void Start()
    {
        BindPlayerChampion();
        SetupAbilityButtons();
        RefreshAll();
    }

    void Update()
    {
        if (playerChampion == null)
        {
            BindPlayerChampion();
            return;
        }

        UpdateResourceDisplay();
        UpdateProgressionDisplay();
        UpdateAbilityCooldowns();
    }

    private void BindPlayerChampion()
    {
        ChampionController controller = FindObjectOfType<ChampionController>();
        playerChampion = controller != null
            ? controller.GetComponent<Champion>()
            : FindObjectOfType<Champion>();

        if (playerChampion == null)
            return;

        playerChampion.OnResourcesChanged -= RefreshResourcesFromEvent;
        playerChampion.OnProgressionChanged -= RefreshProgressionFromEvent;
        playerChampion.OnResourcesChanged += RefreshResourcesFromEvent;
        playerChampion.OnProgressionChanged += RefreshProgressionFromEvent;

        playerAbilities = playerChampion.GetComponents<ChampionAbility>();
        SetupAbilityButtons();
    }

    private void OnDestroy()
    {
        if (playerChampion == null)
            return;

        playerChampion.OnResourcesChanged -= RefreshResourcesFromEvent;
        playerChampion.OnProgressionChanged -= RefreshProgressionFromEvent;
    }

    private void RefreshResourcesFromEvent() => UpdateResourceDisplay();
    private void RefreshProgressionFromEvent() => UpdateProgressionDisplay();

    private void RefreshAll()
    {
        UpdateResourceDisplay();
        UpdateProgressionDisplay();
        UpdateAbilityCooldowns();
    }

    private void UpdateResourceDisplay()
    {
        if (playerChampion == null)
            return;

        if (healthBar != null)
            healthBar.fillAmount = playerChampion.HealthPercent;
        if (healthText != null)
            healthText.text = $"{playerChampion.CurrentHealth:F0} / {playerChampion.MaxHealth:F0}";

        if (manaBar != null)
            manaBar.fillAmount = playerChampion.ManaPercent;
        if (manaText != null)
            manaText.text = $"{playerChampion.CurrentMana:F0} / {playerChampion.MaxMana:F0}";
    }

    private void UpdateProgressionDisplay()
    {
        if (playerChampion == null)
            return;

        if (goldText != null)
            goldText.text = playerChampion.Gold.ToString("N0");
        if (levelText != null)
            levelText.text = playerChampion.Level.ToString();
        if (experienceBar != null)
            experienceBar.fillAmount = playerChampion.ExperiencePercent;
    }

    private void SetupAbilityButtons()
    {
        string[] keys = { "Q", "W", "E", "R" };
        int count = Mathf.Min(abilityButtons.Length, keys.Length);

        for (int i = 0; i < count; i++)
        {
            AbilityButton button = abilityButtons[i];
            if (button == null)
                continue;

            button.SetKeyLabel(keys[i]);
            if (button.ability == null && playerAbilities != null && i < playerAbilities.Length)
                button.ability = playerAbilities[i];
        }
    }

    private void UpdateAbilityCooldowns()
    {
        if (abilityButtons == null)
            return;

        foreach (AbilityButton button in abilityButtons)
        {
            if (button?.ability == null)
                continue;

            button.UpdateCooldown(button.ability.GetCooldownPercent());
            button.SetInteractable(button.ability.CanCast());
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

    public void UpdateCooldown(float readyPercent)
    {
        if (cooldownOverlay != null)
            cooldownOverlay.fillAmount = 1f - Mathf.Clamp01(readyPercent);
    }

    public void SetInteractable(bool interactable)
    {
        if (button != null)
            button.interactable = interactable;
    }
}
