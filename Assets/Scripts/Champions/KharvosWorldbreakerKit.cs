using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Signature Vanguard kit for Kharvos.
/// Creates temporary route pressure with fracture lines, walls and collapsing stone rings.
/// </summary>
public class KharvosWorldbreakerKit : MonoBehaviour
{
    private Champion champion;
    private ChampionController controller;
    private AOGChampionBlueprint blueprint;
    private StatModifier identityModifier;
    private StatModifier guardModifier;
    private readonly Dictionary<AbilityKey, ChampionAbility> abilities = new();

    public void Initialize(AOGChampionBlueprint data)
    {
        blueprint = data;
        champion = GetComponent<Champion>();
        controller = GetComponent<ChampionController>();

        if (champion == null || blueprint == null)
            return;

        identityModifier = blueprint.CreateStatModifier();
        champion.AddStatModifier(identityModifier);
        ConfigureAbilities();
    }

    private void ConfigureAbilities()
    {
        ChampionAbility[] existing = GetComponents<ChampionAbility>();
        Dictionary<AbilityKey, ChampionAbility> byKey = new();
        foreach (ChampionAbility ability in existing)
            byKey[ability.Key] = ability;

        foreach (AOGAbilityBlueprint spec in blueprint.abilities)
        {
            if (!byKey.TryGetValue(spec.key, out ChampionAbility ability) || ability == null)
                ability = gameObject.AddComponent<ChampionAbility>();

            ability.ConfigureRuntime(spec.key, spec.type, spec.name, spec.description, spec.manaCost,
                spec.cooldown, spec.castTime, spec.baseDamage, spec.apRatio, spec.range, spec.radius);

            ability.OnCastCompleted -= HandleCastCompleted;
            ability.OnCastCompleted += HandleCastCompleted;
            abilities[spec.key] = ability;
        }

        controller?.RefreshAbilities();
    }

    private void HandleCastCompleted(ChampionAbility ability)
    {
        if (ability == null)
            return;

        switch (ability.Key)
        {
            case AbilityKey.Q:
                StartCoroutine(FaultlineRush(ability));
                break;
            case AbilityKey.W:
                StartCoroutine(GravestoneRampart(ability));
                break;
            case AbilityKey.E:
                StartCoroutine(SeismicGuard(ability));
                break;
            case AbilityKey.R:
                StartCoroutine(Worldbreak(ability));
                break;
        }
    }

    private IEnumerator FaultlineRush(ChampionAbility ability)
    {
        Vector3 start = transform.position;
        Vector3 end = ability.LastTargetPosition;
        end.y = transform.position.y;
        Vector3 direction = end - start;
        if (direction.magnitude > ability.Range)
            end = start + direction.normalized * ability.Range;

        float elapsed = 0f;
        float duration = 0.28f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            transform.position = Vector3.Lerp(start, end, t);
            yield return null;
        }

        int segments = 5;
        for (int i = 0; i < segments; i++)
        {
            Vector3 point = Vector3.Lerp(start, end, i / (float)(segments - 1));
            SpawnStoneSpike(point, 0.9f, 1.4f, 1.6f);
            DamageArea(point, 1.6f, 18f + champion.AttackDamage * 0.18f);
            yield return new WaitForSeconds(0.06f);
        }
    }

    private IEnumerator GravestoneRampart(ChampionAbility ability)
    {
        Vector3 center = ability.LastTargetPosition;
        Vector3 forward = (center - transform.position).normalized;
        Vector3 right = new Vector3(forward.z, 0f, -forward.x);
        List<GameObject> wallPieces = new();

        for (int i = -2; i <= 2; i++)
        {
            Vector3 point = center + right * i * 1.4f;
            wallPieces.Add(SpawnStoneSpike(point, 1.2f, 3.8f, 1.2f));
        }

        yield return new WaitForSeconds(4.5f);
        foreach (GameObject piece in wallPieces)
        {
            if (piece != null)
                Destroy(piece);
        }
    }

    private IEnumerator SeismicGuard(ChampionAbility ability)
    {
        if (guardModifier != null)
            champion.RemoveStatModifier(guardModifier);

        guardModifier = new StatModifier
        {
            ArmorBonus = 28f,
            SpellBlockBonus = 24f,
            MovementSpeedBonus = 0.35f
        };
        champion.AddStatModifier(guardModifier);

        PulseRing(transform.position, ability.AOERadius, 0.45f);
        DamageArea(transform.position, ability.AOERadius, 26f + champion.AbilityPower * 0.15f);

        yield return new WaitForSeconds(3f);
        if (champion != null && guardModifier != null)
        {
            champion.RemoveStatModifier(guardModifier);
            guardModifier = null;
        }
    }

    private IEnumerator Worldbreak(ChampionAbility ability)
    {
        Vector3 center = ability.LastTargetPosition;
        int stones = 12;
        float startRadius = ability.AOERadius;

        for (int ring = 0; ring < 3; ring++)
        {
            float radius = Mathf.Lerp(startRadius, 2.2f, ring / 2f);
            for (int i = 0; i < stones; i++)
            {
                float angle = i * (360f / stones) * Mathf.Deg2Rad;
                Vector3 point = center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
                SpawnStoneSpike(point, 0.8f, 2.3f + ring * 0.6f, 1.0f);
            }

            PulseRing(center, radius, 0.5f);
            DamageArea(center, radius + 1.2f, 42f + champion.AttackDamage * 0.22f);
            yield return new WaitForSeconds(0.32f);
        }

        DamageArea(center, 3.2f, 85f + champion.AttackDamage * 0.45f);
    }

    private GameObject SpawnStoneSpike(Vector3 position, float width, float height, float depth)
    {
        GameObject stone = GameObject.CreatePrimitive(PrimitiveType.Cube);
        stone.name = "Kharvos_Stone";
        stone.transform.position = position + Vector3.up * (height * 0.5f);
        stone.transform.localScale = new Vector3(width, height, depth);
        stone.transform.rotation = Quaternion.Euler(Random.Range(-5f, 5f), Random.Range(0f, 180f), Random.Range(-7f, 7f));
        return stone;
    }

    private void DamageArea(Vector3 center, float radius, float damage)
    {
        HashSet<Object> hitObjects = new();
        Collider[] hits = Physics.OverlapSphere(center, radius);
        foreach (Collider hit in hits)
        {
            Champion target = hit.GetComponentInParent<Champion>();
            if (target != null && target != champion && target.Team != champion.Team && hitObjects.Add(target))
            {
                target.TakeDamage(damage, DamageType.Physical);
                continue;
            }

            CombatUnit unit = hit.GetComponentInParent<CombatUnit>();
            if (unit != null && unit.UnitTeam != champion.Team && hitObjects.Add(unit))
                unit.TakeDamage(damage);
        }
    }

    private void PulseRing(Vector3 center, float radius, float lifetime)
    {
        GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.name = "Kharvos_Seismic_Ring";
        ring.transform.position = center + Vector3.up * 0.08f;
        ring.transform.localScale = new Vector3(radius, 0.03f, radius);
        Collider collider = ring.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);
        StartCoroutine(DestroyAfter(ring, lifetime));
    }

    private IEnumerator DestroyAfter(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (obj != null)
            Destroy(obj);
    }

    private void OnDestroy()
    {
        foreach (ChampionAbility ability in abilities.Values)
        {
            if (ability != null)
                ability.OnCastCompleted -= HandleCastCompleted;
        }

        if (champion != null && identityModifier != null)
            champion.RemoveStatModifier(identityModifier);
        if (champion != null && guardModifier != null)
            champion.RemoveStatModifier(guardModifier);
    }
}
