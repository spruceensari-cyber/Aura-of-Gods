using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
Modern dark themed UI system - sleek neon accents, gothic colors
/// </summary>
public class ModernDarkUITheme : MonoBehaviour
{
    [SerializeField] private Color darkBG = new Color(0.08f, 0.08f, 0.12f);
    [SerializeField] private Color neonBlue = new Color(0.2f, 0.8f, 1f);
    [SerializeField] private Color neonPurple = new Color(0.7f, 0.2f, 1f);
    [SerializeField] private Color accentGold = new Color(1f, 0.8f, 0f);
    [SerializeField] private Color textPrimary = new Color(0.95f, 0.95f, 0.95f);
    [SerializeField] private Color textSecondary = new Color(0.7f, 0.7f, 0.7f);
    
    private Canvas mainCanvas;
    
    void Start()
    {
        ApplyTheme();
    }
    
    private void ApplyTheme()
    {
        mainCanvas = FindObjectOfType<Canvas>();
        if (mainCanvas == null) return;
        
        // Apply theme to all UI elements
        Image[] images = mainCanvas.GetComponentsInChildren<Image>();
        foreach (Image img in images)
        {
            if (img.tag == "UIBackground")
                img.color = darkBG;
            else if (img.tag == "UIAccent")
                img.color = neonBlue;
        }
        
        TextMeshProUGUI[] texts = mainCanvas.GetComponentsInChildren<TextMeshProUGUI>();
        foreach (TextMeshProUGUI text in texts)
        {
            if (text.tag == "UITitle")
                text.color = neonPurple;
            else
                text.color = textPrimary;
        }
    }
    
    public void StyleButton(Button button)
    {
        Image img = button.GetComponent<Image>();
        if (img != null)
        {
            img.color = darkBG;
            
            // Add neon outline effect
            Outline outline = button.GetComponent<Outline>();
            if (outline == null)
                outline = button.gameObject.AddComponent<Outline>();
            
            outline.effectColor = neonBlue;
            outline.effectDistance = new Vector2(2, 2);
        }
        
        // Add hover effect
        ColorBlock colors = button.colors;
        colors.normalColor = darkBG;
        colors.highlightedColor = neonBlue;
        colors.pressedColor = neonPurple;
        colors.selectedColor = neonBlue;
        button.colors = colors;
    }
    
    public void StylePanel(Image panel)
    {
        panel.color = darkBG;
        
        Outline outline = panel.GetComponent<Outline>();
        if (outline == null)
            outline = panel.gameObject.AddComponent<Outline>();
        
        outline.effectColor = neonPurple;
        outline.effectDistance = new Vector2(1, 1);
    }
}

/// <summary>
Neon glow effect for UI elements
/// </summary>
public class NeonGlowEffect : MonoBehaviour
{
    [SerializeField] private Color glowColor = new Color(0.2f, 0.8f, 1f);
    [SerializeField] private float glowIntensity = 1.5f;
    [SerializeField] private float pulseSpeed = 2f;
    
    private Image image;
    private float time;
    
    void Start()
    {
        image = GetComponent<Image>();
    }
    
    void Update()
    {
        time += Time.deltaTime * pulseSpeed;
        
        // Pulsing glow effect
        float glow = 1f + (Mathf.Sin(time) * 0.5f * glowIntensity);
        Color c = glowColor;
        c.a = glow;
        
        if (image != null)
            image.color = Color.Lerp(image.color, c, 0.1f);
    }
}
