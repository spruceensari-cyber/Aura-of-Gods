using UnityEngine;
using System.Collections;

/// <summary>
/// Champion death animation with dark gothic theme.
/// </summary>
public class DeathEffect : MonoBehaviour
{
    [SerializeField] private float dissolveDuration = 1.5f;
    private Renderer[] renderers;
    private float elapsedTime;
    private bool isDying;

    private void Start()
    {
        renderers = GetComponentsInChildren<Renderer>();
    }

    public void PlayDeathEffect()
    {
        if (isDying)
            return;

        isDying = true;
        elapsedTime = 0f;

        GothicParticlePresets.CreateShadowExplosion(transform.position, 2f);
        GothicParticlePresets.CreateDarkAura(transform.position, 3f);
        GothicParticlePresets.CreateBloodExplosion(transform.position);

        AudioSource audio = GetComponent<AudioSource>();
        if (audio == null)
            audio = gameObject.AddComponent<AudioSource>();

        StartCoroutine(FadeOutCoroutine());
    }

    private IEnumerator FadeOutCoroutine()
    {
        float duration = Mathf.Max(0.05f, dissolveDuration);
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            elapsedTime = t;
            float alpha = 1f - Mathf.Clamp01(t / duration);

            foreach (Renderer targetRenderer in renderers)
            {
                if (targetRenderer == null)
                    continue;

                foreach (Material material in targetRenderer.materials)
                {
                    Color color = material.color;
                    color.a = alpha;
                    material.color = color;
                }
            }

            yield return null;
        }

        gameObject.SetActive(false);
    }
}

/// <summary>
/// Gothic-themed aura effect for champions.
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

    private void Start()
    {
        CreateAura();
    }

    private void CreateAura()
    {
        auraObject = new GameObject("ChampionAura");
        auraObject.transform.SetParent(transform);
        auraObject.transform.localPosition = Vector3.zero;

        auraParticles = auraObject.AddComponent<ParticleSystem>();
        auraParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        auraParticles.Clear(true);

        ParticleSystem.MainModule main = auraParticles.main;
        main.startColor = auraColor;
        main.startSize = new ParticleSystem.MinMaxCurve(0.5f, 1f);
        main.startLifetime = 1f;
        main.maxParticles = 30;
        main.loop = true;

        ParticleSystem.EmissionModule emission = auraParticles.emission;
        emission.rateOverTime = 15f;

        ParticleSystem.ShapeModule shape = auraParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 1.5f;

        auraParticles.Play(true);
    }

    private void Update()
    {
        if (auraObject == null)
            return;

        time += Time.deltaTime * pulseSpeed;
        float scale = Mathf.Lerp(minScale, maxScale, (Mathf.Sin(time) + 1f) / 2f);
        auraObject.transform.localScale = Vector3.one * scale;
    }
}
