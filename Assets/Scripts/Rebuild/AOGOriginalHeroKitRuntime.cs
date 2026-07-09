using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum AOGOriginalHeroId
{
    SorynPrismHuntress,
    CaelixRiftVanguard,
    VaelithChronoOracle
}

/// <summary>
/// Clean three-hero vertical-slice kit layer. Reconfigures Q/W/E/R consistently and adds hero-specific motion/effects.
/// </summary>
public class AOGOriginalHeroKitRuntime : MonoBehaviour
{
    public AOGOriginalHeroId HeroId { get; private set; }

    private Champion champion;
    private ChampionController controller;
    private readonly Dictionary<AbilityKey, ChampionAbility> abilities = new();
    private StatModifier identityModifier;

    public void Initialize(AOGOriginalHeroId id)
    {
        HeroId = id;
        champion = GetComponent<Champion>();
        controller = GetComponent<ChampionController>();
        if (champion == null) return;

        ApplyIdentityStats();
        ConfigureAbilities();
        gameObject.name = HeroId switch
        {
            AOGOriginalHeroId.SorynPrismHuntress => "Player_Soryn_Prism_Huntress",
            AOGOriginalHeroId.CaelixRiftVanguard => "Hero_Caelix_Rift_Vanguard",
            _ => "Hero_Vaelith_Chrono_Oracle"
        };
    }

    private void ApplyIdentityStats()
    {
        if (identityModifier != null) champion.RemoveStatModifier(identityModifier);
        identityModifier = HeroId switch
        {
            AOGOriginalHeroId.SorynPrismHuntress => new StatModifier
            {
                AttackDamageBonus = 10f,
                AttackSpeedBonus = 0.18f,
                MovementSpeedBonus = 0.35f
            },
            AOGOriginalHeroId.CaelixRiftVanguard => new StatModifier
            {
                AttackDamageBonus = 6f,
                ArmorBonus = 22f,
                SpellBlockBonus = 18f,
                MovementSpeedBonus = -0.2f
            },
            _ => new StatModifier
            {
                AbilityPowerBonus = 28f,
                MovementSpeedBonus = 0.15f,
                SpellBlockBonus = 6f
            }
        };
        champion.AddStatModifier(identityModifier);
    }

    private void ConfigureAbilities()
    {
        abilities.Clear();
        Dictionary<AbilityKey, ChampionAbility> byKey = new();
        foreach (ChampionAbility ability in GetComponents<ChampionAbility>())
            byKey[ability.Key] = ability;

        switch (HeroId)
        {
            case AOGOriginalHeroId.SorynPrismHuntress:
                Set(byKey, AbilityKey.Q, AbilityType.Linear, "Prism Bolt", "Piercing precision shot.", 35f, 5.5f, 0.08f, 72f, 0.35f, 13f, 1.2f);
                Set(byKey, AbilityKey.W, AbilityType.AOE, "Refraction Mine", "Delayed control zone.", 50f, 12f, 0.12f, 48f, 0.25f, 10f, 3.2f);
                Set(byKey, AbilityKey.E, AbilityType.Instant, "Vector Shift", "Short evasive phase dash.", 40f, 9f, 0.04f, 24f, 0.15f, 5.5f, 2.2f);
                Set(byKey, AbilityKey.R, AbilityType.Linear, "Spectrum Break", "Long-range prism barrage.", 100f, 75f, 0.45f, 220f, 0.65f, 18f, 2.0f);
                break;

            case AOGOriginalHeroId.CaelixRiftVanguard:
                Set(byKey, AbilityKey.Q, AbilityType.Linear, "Gravemarch", "Armored rush that cracks the lane.", 40f, 7f, 0.12f, 68f, 0.25f, 8f, 1.8f);
                Set(byKey, AbilityKey.W, AbilityType.Instant, "Aegis Collapse", "Defensive pulse around Caelix.", 45f, 11f, 0.05f, 44f, 0.20f, 1f, 4f);
                Set(byKey, AbilityKey.E, AbilityType.AOE, "Fault Cage", "Rift zone that punishes clustered enemies.", 55f, 14f, 0.22f, 82f, 0.30f, 8f, 4.2f);
                Set(byKey, AbilityKey.R, AbilityType.AOE, "Event Horizon", "Massive frontline collapse zone.", 100f, 90f, 0.65f, 210f, 0.55f, 9f, 6f);
                break;

            default:
                Set(byKey, AbilityKey.Q, AbilityType.Linear, "Second Hand", "Chronal lance that marks a path.", 35f, 6f, 0.10f, 76f, 0.40f, 12f, 1.4f);
                Set(byKey, AbilityKey.W, AbilityType.AOE, "Hourglass Field", "Temporal slowing field.", 55f, 13f, 0.18f, 54f, 0.30f, 9f, 3.8f);
                Set(byKey, AbilityKey.E, AbilityType.Instant, "Borrowed Moment", "Instant self-centered temporal burst.", 45f, 10f, 0.04f, 58f, 0.35f, 1f, 3.4f);
                Set(byKey, AbilityKey.R, AbilityType.AOE, "Zero Hour", "Large chronal detonation.", 100f, 88f, 0.70f, 230f, 0.70f, 10f, 6.5f);
                break;
        }

        controller?.RefreshAbilities();
    }

    private void Set(
        Dictionary<AbilityKey, ChampionAbility> byKey,
        AbilityKey key,
        AbilityType type,
        string name,
        string description,
        float mana,
        float cooldown,
        float castTime,
        float damage,
        float apRatio,
        float range,
        float radius)
    {
        if (!byKey.TryGetValue(key, out ChampionAbility ability) || ability == null)
            ability = gameObject.AddComponent<ChampionAbility>();

        ability.ConfigureRuntime(key, type, name, description, mana, cooldown, castTime, damage, apRatio, range, radius);
        ability.OnCastCompleted -= OnCastCompleted;
        ability.OnCastCompleted += OnCastCompleted;
        abilities[key] = ability;
    }

    private void OnCastCompleted(ChampionAbility ability)
    {
        if (ability == null) return;

        switch (HeroId)
        {
            case AOGOriginalHeroId.SorynPrismHuntress:
                ResolveSoryn(ability);
                break;
            case AOGOriginalHeroId.CaelixRiftVanguard:
                ResolveCaelix(ability);
                break;
            case AOGOriginalHeroId.VaelithChronoOracle:
                ResolveVaelith(ability);
                break;
        }
    }

    private void ResolveSoryn(ChampionAbility ability)
    {
        if (ability.Key == AbilityKey.E)
            StartCoroutine(DashTowards(ability.LastTargetPosition, 5.5f, 0.16f));
        else if (ability.Key == AbilityKey.W)
            StartCoroutine(DelayedPulse(ability.LastTargetPosition, ability.AOERadius, new Color(0.12f, 0.82f, 1f, 0.75f), 0.45f));
        else if (ability.Key == AbilityKey.R)
            StartCoroutine(TriplePulseLine(transform.position, ability.LastTargetPosition, new Color(0.55f, 0.22f, 1f, 0.75f)));
    }

    private void ResolveCaelix(ChampionAbility ability)
    {
        if (ability.Key == AbilityKey.Q)
            StartCoroutine(DashTowards(ability.LastTargetPosition, 6.5f, 0.24f));
        else if (ability.Key == AbilityKey.W)
            StartCoroutine(TemporaryGuard());
        else
            SpawnPulse(ability.LastTargetPosition, ability.AOERadius, new Color(1f, 0.18f, 0.05f, 0.7f));
    }

    private void ResolveVaelith(ChampionAbility ability)
    {
        Color chrono = new Color(0.22f, 0.65f, 1f, 0.72f);
        if (ability.Key == AbilityKey.W)
            StartCoroutine(DelayedPulse(ability.LastTargetPosition, ability.AOERadius, chrono, 0.65f));
        else if (ability.Key == AbilityKey.R)
            StartCoroutine(ExpandingChronoField(ability.LastTargetPosition, ability.AOERadius, chrono));
        else
            SpawnPulse(ability.Key == AbilityKey.E ? transform.position : ability.LastTargetPosition, ability.AOERadius, chrono);
    }

    private IEnumerator DashTowards(Vector3 target, float maxDistance, float duration)
    {
        Vector3 start = transform.position;
        Vector3 delta = target - start;
        delta.y = 0f;
        Vector3 end = start + Vector3.ClampMagnitude(delta, maxDistance);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            transform.position = Vector3.Lerp(start, end, eased);
            yield return null;
        }
        transform.position = end;
    }

    private IEnumerator TemporaryGuard()
    {
        StatModifier guard = new StatModifier { ArmorBonus = 30f, SpellBlockBonus = 24f };
        champion.AddStatModifier(guard);
        SpawnPulse(transform.position, 4f, new Color(1f, 0.3f, 0.08f, 0.65f));
        yield return new WaitForSeconds(2.5f);
        if (champion != null) champion.RemoveStatModifier(guard);
    }

    private IEnumerator DelayedPulse(Vector3 center, float radius, Color color, float delay)
    {
        SpawnPulse(center, radius * 0.55f, color * 0.5f);
        yield return new WaitForSeconds(delay);
        SpawnPulse(center, radius, color);
    }

    private IEnumerator TriplePulseLine(Vector3 from, Vector3 to, Color color)
    {
        for (int i = 1; i <= 3; i++)
        {
            SpawnPulse(Vector3.Lerp(from, to, i / 3f), 2.3f, color);
            yield return new WaitForSeconds(0.10f);
        }
    }

    private IEnumerator ExpandingChronoField(Vector3 center, float radius, Color color)
    {
        for (int i = 1; i <= 4; i++)
        {
            SpawnPulse(center, radius * (i / 4f), color);
            yield return new WaitForSeconds(0.12f);
        }
    }

    private void SpawnPulse(Vector3 position, float radius, Color color)
    {
        GameObject obj = new GameObject("AOG_Original_Hero_Pulse");
        LineRenderer lr = obj.AddComponent<LineRenderer>();
        lr.loop = true;
        lr.useWorldSpace = true;
        lr.positionCount = 48;
        lr.widthMultiplier = 0.07f;
        lr.material = CreateMaterial(color);

        for (int i = 0; i < lr.positionCount; i++)
        {
            float a = i / (float)lr.positionCount * Mathf.PI * 2f;
            lr.SetPosition(i, position + new Vector3(Mathf.Cos(a) * radius, 0.08f, Mathf.Sin(a) * radius));
        }
        Destroy(obj, 0.45f);
    }

    private static Material CreateMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
        Material mat = new Material(shader) { color = color };
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        return mat;
    }

    private void OnDestroy()
    {
        if (champion != null && identityModifier != null)
            champion.RemoveStatModifier(identityModifier);

        foreach (ChampionAbility ability in abilities.Values)
        {
            if (ability != null) ability.OnCastCompleted -= OnCastCompleted;
        }
    }
}
