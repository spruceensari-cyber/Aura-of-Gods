using UnityEngine;
using System.Collections;

/// <summary>
/// Professional spell visual effects - blood splashes, dark auras, explosions.
/// Supports all ability types with customizable particle systems.
/// </summary>
public class SpellVisualEffect : MonoBehaviour
{
    [SerializeField] private ParticleSystem impactParticles;
    [SerializeField] private ParticleSystem trailParticles;
    [SerializeField] private Light effectLight;
    [SerializeField] private AudioClip castSound;
    [SerializeField] private AudioClip impactSound;
    [SerializeField] private float effectDuration = 2f;

    private AudioSource audioSource;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    public void PlayCastEffect(Vector3 position, Color tint)
    {
        if (castSound != null && audioSource != null)
            audioSource.PlayOneShot(castSound);

        if (trailParticles != null)
        {
            ParticleSystem trail = Instantiate(trailParticles, position, Quaternion.identity);
            ParticleSystem.MainModule main = trail.main;
            main.startColor = tint;
            Destroy(trail.gameObject, effectDuration);
        }
    }

    public void PlayImpactEffect(Vector3 position, Color tint, float scale = 1f)
    {
        if (impactSound != null && audioSource != null)
            audioSource.PlayOneShot(impactSound);

        if (impactParticles != null)
        {
            ParticleSystem impact = Instantiate(impactParticles, position, Quaternion.identity);
            ParticleSystem.MainModule main = impact.main;
            main.startColor = tint;
            impact.transform.localScale = Vector3.one * scale;
            Destroy(impact.gameObject, effectDuration);
        }

        if (effectLight != null)
        {
            Light flashLight = Instantiate(effectLight, position, Quaternion.identity);
            StartCoroutine(FadeLightOut(flashLight));
        }
    }

    private IEnumerator FadeLightOut(Light light)
    {
        float elapsed = 0f;
        float initialIntensity = light.intensity;

        while (elapsed < 0.5f)
        {
            elapsed += Time.deltaTime;
            light.intensity = Mathf.Lerp(initialIntensity, 0f, elapsed / 0.5f);
            yield return null;
        }

        Destroy(light.gameObject);
    }
}

/// <summary>
/// Gothic dark particle system preset manager.
/// Unity 6 starts newly added ParticleSystems immediately, so each preset stops the
/// system before changing modules and starts it only after configuration is complete.
/// </summary>
public class GothicParticlePresets
{
    public static void CreateBloodExplosion(Vector3 position)
    {
        GameObject blood = new GameObject("BloodExplosion");
        blood.transform.position = position;

        ParticleSystem ps = blood.AddComponent<ParticleSystem>();
        StopForConfiguration(ps);

        ParticleSystem.MainModule main = ps.main;
        main.startColor = new Color(0.8f, 0.1f, 0.1f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
        main.startLifetime = 1.5f;
        main.maxParticles = 50;
        main.emitterVelocityMode = ParticleSystemEmitterVelocityMode.Transform;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 30f;

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.5f;

        ParticleSystem.VelocityOverLifetimeModule velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.x = new ParticleSystem.MinMaxCurve(-5f, 5f);
        velocity.y = new ParticleSystem.MinMaxCurve(2f, 8f);
        velocity.z = new ParticleSystem.MinMaxCurve(-5f, 5f);

        ps.Play(true);
        Object.Destroy(blood, 2f);
    }

    public static void CreateDarkAura(Vector3 position, float radius = 2f)
    {
        GameObject aura = new GameObject("DarkAura");
        aura.transform.position = position;

        ParticleSystem ps = aura.AddComponent<ParticleSystem>();
        StopForConfiguration(ps);

        ParticleSystem.MainModule main = ps.main;
        main.startColor = new Color(0.3f, 0.1f, 0.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
        main.startLifetime = 2f;
        main.maxParticles = 80;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 20f;

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = radius;

        ParticleSystem.VelocityOverLifetimeModule velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.x = new ParticleSystem.MinMaxCurve(-3f, 3f);
        velocity.y = new ParticleSystem.MinMaxCurve(1f, 3f);
        velocity.z = new ParticleSystem.MinMaxCurve(-3f, 3f);

        ps.Play(true);
        Object.Destroy(aura, 3f);
    }

    public static void CreateShadowExplosion(Vector3 position, float radius = 3f)
    {
        GameObject explosion = new GameObject("ShadowExplosion");
        explosion.transform.position = position;

        ParticleSystem ps = explosion.AddComponent<ParticleSystem>();
        StopForConfiguration(ps);

        ParticleSystem.MainModule main = ps.main;
        main.startColor = new Color(0.2f, 0.2f, 0.3f);
        main.startSize = new ParticleSystem.MinMaxCurve(1f, 3f);
        main.startLifetime = 1f;
        main.maxParticles = 120;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 60f;
        emission.rateOverTimeMultiplier = 3f;

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = radius;

        ParticleSystem.VelocityOverLifetimeModule velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.x = new ParticleSystem.MinMaxCurve(-10f, 10f);
        velocity.y = new ParticleSystem.MinMaxCurve(-5f, 15f);
        velocity.z = new ParticleSystem.MinMaxCurve(-10f, 10f);

        ps.Play(true);
        Object.Destroy(explosion, 1.5f);
    }

    private static void StopForConfiguration(ParticleSystem particleSystem)
    {
        if (particleSystem == null)
            return;

        particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        particleSystem.Clear(true);
    }
}
