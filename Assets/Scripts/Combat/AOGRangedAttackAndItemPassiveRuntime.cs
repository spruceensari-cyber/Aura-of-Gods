using System.Collections;
using UnityEngine;

/// <summary>
/// Gives ranged champions visible projectile travel on confirmed basic-attack hits.
/// Damage remains owned by AOGUnifiedMobaInputDriver; this component is presentation-only
/// so it cannot double-apply combat damage.
/// </summary>
public class AOGRangedAttackPresentationRuntime : MonoBehaviour, IChampionBasicAttackModifier
{
    private AOGCharacterStats stats;
    private AOGActiveChampion champion;

    private void Awake()
    {
        stats = GetComponent<AOGCharacterStats>();
        champion = GetComponent<AOGActiveChampion>();
    }

    public void OnBasicAttackHit(Minion target)
    {
        if (target == null || stats == null || stats.attackRange < 4.2f)
            return;

        StartCoroutine(TravelVisual(target.transform));
    }

    private IEnumerator TravelVisual(Transform target)
    {
        if (target == null)
            yield break;

        GameObject projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        projectile.name = (champion != null ? champion.displayName : "Champion") + "_Basic_Projectile_Visual";
        projectile.transform.position = transform.position + Vector3.up * 1.35f + transform.forward * 0.55f;
        projectile.transform.localScale = Vector3.one * 0.22f;

        Collider collider = projectile.GetComponent<Collider>();
        if (collider != null) Destroy(collider);

        Renderer renderer = projectile.GetComponent<Renderer>();
        Color accent = champion != null ? champion.accentColor : new Color(0.25f,0.72f,1f);
        if (renderer != null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            Material material = new Material(shader) { color = accent };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", accent);
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", accent * 4f);
            }
            renderer.sharedMaterial = material;
        }

        LineRenderer trail = projectile.AddComponent<LineRenderer>();
        trail.positionCount = 2;
        trail.startWidth = 0.12f;
        trail.endWidth = 0.025f;
        trail.startColor = accent;
        trail.endColor = new Color(accent.r,accent.g,accent.b,0f);
        Shader trailShader = Shader.Find("Universal Render Pipeline/Unlit");
        if (trailShader == null) trailShader = Shader.Find("Unlit/Color");
        trail.material = new Material(trailShader) { color = accent };

        float speed = 24f;
        float deadline = Time.time + 0.8f;
        Vector3 previous = projectile.transform.position;
        while (target != null && Time.time < deadline)
        {
            Vector3 destination = target.position + Vector3.up * 0.8f;
            projectile.transform.position = Vector3.MoveTowards(projectile.transform.position, destination, speed * Time.deltaTime);
            trail.SetPosition(0, previous);
            trail.SetPosition(1, projectile.transform.position);
            previous = projectile.transform.position;
            if (Vector3.Distance(projectile.transform.position, destination) < 0.15f)
                break;
            yield return null;
        }

        Destroy(projectile);
    }
}

/// <summary>
/// Adds gameplay passives to the existing Aether Market items without changing purchase UI.
/// Passives are resolved from inventory IDs, keeping the original shop data authoritative.
/// </summary>
public class AOGItemPassiveCombatRuntime : MonoBehaviour, IChampionBasicAttackModifier
{
    private AOGCharacterStats stats;
    private AOGPlayerEconomy economy;
    private AOGActiveChampion champion;
    private float nextTitanGuard;
    private float nextWarstrideBurst;

    private void Awake()
    {
        stats = GetComponent<AOGCharacterStats>();
        economy = GetComponent<AOGPlayerEconomy>();
        champion = GetComponent<AOGActiveChampion>();
    }

    private void Update()
    {
        if (stats == null || economy == null || champion == null || !champion.IsActiveChampion || stats.IsDead)
            return;

        if (Has("titanheart") && stats.hp / Mathf.Max(1f,stats.maxHp) < 0.30f && Time.time >= nextTitanGuard)
        {
            nextTitanGuard = Time.time + 18f;
            stats.hp = Mathf.Min(stats.maxHp, stats.hp + stats.maxHp * 0.10f);
            SpawnPassiveRing("Titan_Heart_Guard", 2.5f, new Color(0.96f,0.26f,0.32f));
        }

        if (Has("warstride") && Time.time >= nextWarstrideBurst)
        {
            nextWarstrideBurst = Time.time + 12f;
            StartCoroutine(WarstrideBurst());
        }
    }

    public void OnBasicAttackHit(Minion target)
    {
        if (target == null || stats == null || economy == null)
            return;

        if (Has("moonblade"))
            stats.hp = Mathf.Min(stats.maxHp, stats.hp + Mathf.Max(4f, stats.attackDamage * 0.055f));

        if (Has("starfang"))
        {
            float bonus = stats.attackDamage * 0.12f;
            target.TakeDamage(bonus, gameObject);
        }

        if (Has("voidglass") && target.hp / Mathf.Max(1f,target.maxHp) < 0.35f)
            target.TakeDamage(stats.attackDamage * 0.20f, gameObject);

        if (Has("eclipse"))
        {
            stats.hp = Mathf.Min(stats.maxHp, stats.hp + 8f);
            if (Random.value < 0.16f)
                target.TakeDamage(stats.attackDamage * 0.26f, gameObject);
        }

        if (Has("godbreaker") && target.hp / Mathf.Max(1f,target.maxHp) < 0.18f)
        {
            target.TakeDamage(stats.attackDamage * 0.45f, gameObject);
            SpawnPassiveRing("Godbreaker_Execute", 1.3f, new Color(1f,0.42f,0.12f));
        }
    }

    private IEnumerator WarstrideBurst()
    {
        float bonus = 0.55f;
        stats.moveSpeed += bonus;
        SpawnPassiveRing("Warstride_Burst", 1.8f, new Color(0.42f,0.86f,0.48f));
        yield return new WaitForSeconds(2.5f);
        if (stats != null)
            stats.moveSpeed = Mathf.Max(1f, stats.moveSpeed - bonus);
    }

    private bool Has(string id)
    {
        foreach (AOGItemDefinition item in economy.inventory)
            if (item != null && item.id == id) return true;
        return false;
    }

    private void SpawnPassiveRing(string name, float radius, Color color)
    {
        GameObject ring = AOGAbilityVisuals.CreateRing(name, transform.position + Vector3.up * 0.06f, radius, color, 0.08f);
        Destroy(ring, 0.45f);
    }
}

public class AOGCombatEnhancementBootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindFirstObjectByType<AOGCombatEnhancementBootstrap>() != null)
            return;
        GameObject host = new GameObject("AOG_Combat_Enhancement_Bootstrap");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGCombatEnhancementBootstrap>();
    }

    private void Update()
    {
        foreach (AOGActiveChampion hero in FindObjectsByType<AOGActiveChampion>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (hero == null) continue;
            if (hero.GetComponent<AOGRangedAttackPresentationRuntime>() == null)
                hero.gameObject.AddComponent<AOGRangedAttackPresentationRuntime>();
            if (hero.GetComponent<AOGItemPassiveCombatRuntime>() == null)
                hero.gameObject.AddComponent<AOGItemPassiveCombatRuntime>();
        }
    }
}
