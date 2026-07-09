using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AOGDamageReactionRuntime : MonoBehaviour
{
    public enum Kind { Minion, Tower }
    public Kind kind;

    private Minion minion;
    private TowerHealth tower;
    private float lastHp;
    private bool deathPlayed;
    private Vector3 baseScale;

    private void Awake()
    {
        minion = GetComponent<Minion>();
        tower = GetComponent<TowerHealth>();
        lastHp = CurrentHp();
        baseScale = transform.localScale;
    }

    private void Update()
    {
        float hp = CurrentHp();
        if (hp < lastHp - 0.1f)
            StartCoroutine(HitPulse());

        if (!deathPlayed && lastHp > 0f && hp <= 0f)
        {
            deathPlayed = true;
            if (kind == Kind.Minion) StartCoroutine(MinionDeathDissolve());
            else StartCoroutine(TowerDeathPulse());
        }
        lastHp = hp;
    }

    private float CurrentHp()
    {
        if (minion != null) return minion.hp;
        if (tower != null) return tower.hp;
        return 0f;
    }

    private IEnumerator HitPulse()
    {
        if (kind == Kind.Minion)
            GetComponent<AOGMinionProceduralAnimator>()?.PlayHit();

        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        var originals = new List<Color>();
        var materials = new List<Material>();
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null) continue;
            foreach (Material material in renderer.materials)
            {
                if (material == null) continue;
                materials.Add(material);
                originals.Add(material.color);
                material.color = Color.Lerp(material.color, Color.white, kind == Kind.Tower ? 0.45f : 0.70f);
            }
        }

        float duration = kind == Kind.Tower ? 0.11f : 0.08f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float pulse = 1f + Mathf.Sin(elapsed / duration * Mathf.PI) * (kind == Kind.Tower ? 0.025f : 0.06f);
            transform.localScale = baseScale * pulse;
            yield return null;
        }

        transform.localScale = baseScale;
        for (int i = 0; i < materials.Count && i < originals.Count; i++)
            if (materials[i] != null) materials[i].color = originals[i];
    }

    private IEnumerator MinionDeathDissolve()
    {
        AOGMinionProceduralAnimator animator = GetComponent<AOGMinionProceduralAnimator>();
        animator?.PlayDeath();
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        float elapsed = 0f;
        while (elapsed < 0.65f)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / 0.65f);
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null) continue;
                foreach (Material material in renderer.materials)
                {
                    if (material == null) continue;
                    Color c = material.color;
                    material.color = Color.Lerp(c, new Color(c.r * 0.3f, c.g * 0.3f, c.b * 0.3f, c.a), Time.deltaTime * 7f);
                    if (material.HasProperty("_EmissionColor"))
                        material.SetColor("_EmissionColor", Color.Lerp(material.GetColor("_EmissionColor"), Color.black, Time.deltaTime * 6f));
                }
            }
            yield return null;
        }
    }

    private IEnumerator TowerDeathPulse()
    {
        Color c = tower != null && tower.towerTeam == MinionTeam.Blue ? new Color(0.15f,0.55f,1f) : new Color(1f,0.16f,0.08f);
        for (int i = 0; i < 3; i++)
        {
            GameObject ring = AOGAbilityVisuals.CreateRing("Tower_Death_Shockwave", transform.position + Vector3.up * 0.1f, 2.5f + i * 1.8f, c, 0.16f);
            Destroy(ring, 0.75f);
            yield return new WaitForSeconds(0.12f);
        }
    }
}

public class AOGImpactAndDeathPolishBootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        GameObject host = new GameObject("AOG_Impact_And_Death_Polish");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGImpactAndDeathPolishBootstrap>();
    }

    private void Update()
    {
        foreach (Minion minion in Minion.Active)
        {
            if (minion != null && minion.GetComponent<AOGDamageReactionRuntime>() == null)
            {
                AOGDamageReactionRuntime r = minion.gameObject.AddComponent<AOGDamageReactionRuntime>();
                r.kind = AOGDamageReactionRuntime.Kind.Minion;
            }
        }

        foreach (TowerHealth tower in FindObjectsByType<TowerHealth>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (tower != null && tower.GetComponent<AOGDamageReactionRuntime>() == null)
            {
                AOGDamageReactionRuntime r = tower.gameObject.AddComponent<AOGDamageReactionRuntime>();
                r.kind = AOGDamageReactionRuntime.Kind.Tower;
            }
        }
    }
}
