using UnityEngine;

/// <summary>
Champion death animation with dark gothic theme
/// </summary>
public class DeathEffect : MonoBehaviour
{
    [SerializeField] private float dissolveDuration = 1.5f;
    private Renderer[] renderers;
    private float elapsedTime;
    private bool isDying;
    
    void Start()
    {
        renderers = GetComponentsInChildren<Renderer>();
    }
    
    public void PlayDeathEffect()
    {
        isDying = true;
        elapsedTime = 0f;
        
        // Create dark vortex effect at death position
        GothicParticlePresets.CreateShadowExplosion(transform.position, 2f);
        GothicParticlePresets.CreateDarkAura(transform.position, 3f);
        GothicParticlePresets.CreateBloodExplosion(transform.position);
        
        // Play death sound
        AudioSource audio = GetComponent<AudioSource>();
        if (audio == null)
            audio = gameObject.AddComponent<AudioSource>();
        
        // Fade out effect
        StartCoroutine(System.Collections.Coroutines.FadeOutCoroutine(this, dissolveDuration));
    }
}

/// <summary>
Gothic-themed aura effect for champions
/// </summary>
public class ChampionAuraEffect : MonoBehaviour
{
    [SerializeField] private Color auraColor = new Color(0.3f, 0.1f, 0.5f);
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float minScale = 0.9f;
    [SerializeField] private float maxScale = 1.1f;
    
    private GameObject auraObject;
    private ParticleSystem auraParticles;
    private float time;
    
    void Start()
    {
        CreateAura();
    }
    
    private void CreateAura()
    {
        auraObject = new GameObject("ChampionAura");
        auraObject.transform.SetParent(transform);
        auraObject.transform.localPosition = Vector3.zero;
        
        auraParticles = auraObject.AddComponent<ParticleSystem>();
        var main = auraParticles.main;
        main.startColor = auraColor;
        main.startSize = new ParticleSystem.MinMaxCurve(0.5f, 1f);
        main.startLifetime = 1f;
        main.maxParticles = 30;
        main.loop = true;
        
        var emission = auraParticles.emission;
        emission.rateOverTime = 15f;
        
        var shape = auraParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 1.5f;
    }
    
    void Update()
    {
        time += Time.deltaTime * pulseSpeed;
        float scale = Mathf.Lerp(minScale, maxScale, (Mathf.Sin(time) + 1f) / 2f);
        auraObject.transform.localScale = Vector3.one * scale;
    }
}
