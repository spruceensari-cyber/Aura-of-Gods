using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Signature Marksman kit for Veyra.
/// Uses orbit beacons to bend projectile routes and create precision firing corridors.
/// </summary>
public class VeyraVectorSaintKit : MonoBehaviour
{
    private Champion champion;
    private ChampionController controller;
    private AOGChampionBlueprint blueprint;
    private StatModifier identityModifier;
    private StatModifier reloadModifier;
    private readonly Dictionary<AbilityKey, ChampionAbility> abilities = new();
    private Vector3? activeBeacon;
    private GameObject beaconVisual;

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
                StartCoroutine(VectorShot(ability));
                break;
            case AbilityKey.W:
                PlaceBeacon(ability.LastTargetPosition);
                break;
            case AbilityKey.E:
                StartCoroutine(SlipstreamReload(ability));
                break;
            case AbilityKey.R:
                StartCoroutine(Heavenline(ability));
                break;
        }
    }

    private IEnumerator VectorShot(ChampionAbility ability)
    {
        Vector3 start = transform.position + Vector3.up * 1.1f;
        Vector3 end = ability.LastTargetPosition;
        Vector3 bend = activeBeacon ?? Vector3.Lerp(start, end, 0.5f) + transform.right * 2.5f;

        yield return TravelBolt(start, bend, 0.16f, 0.22f, new Color(0.10f, 0.82f, 1f));
        yield return TravelBolt(bend, end, 0.12f, 0.16f, new Color(0.48f, 0.24f, 1f));
        DamageLine(bend, end, 1.2f, 44f + champion.AttackDamage * 0.38f);
    }

    private void PlaceBeacon(Vector3 point)
    {
        activeBeacon = point;
        if (beaconVisual != null)
            Destroy(beaconVisual);

        beaconVisual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        beaconVisual.name = "Veyra_Orbit_Beacon";
        beaconVisual.transform.position = point + Vector3.up * 0.35f;
        beaconVisual.transform.localScale = new Vector3(1.8f, 0.05f, 1.8f);
        Collider col = beaconVisual.GetComponent<Collider>();
        if (col != null) Destroy(col);
        beaconVisual.AddComponent<VeyraBeaconSpin>();
    }

    private IEnumerator SlipstreamReload(ChampionAbility ability)
    {
        Vector3 start = transform.position;
        Vector3 direction = transform.forward;
        Vector3 end = start + direction * 4.2f;
        float elapsed = 0f;

        while (elapsed < 0.16f)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(start, end, Mathf.Clamp01(elapsed / 0.16f));
            yield return null;
        }

        if (reloadModifier != null)
            champion.RemoveStatModifier(reloadModifier);

        reloadModifier = new StatModifier { AttackSpeedBonus = 0.55f, MovementSpeedBonus = 0.30f };
        champion.AddStatModifier(reloadModifier);
        yield return new WaitForSeconds(2.8f);

        if (champion != null && reloadModifier != null)
        {
            champion.RemoveStatModifier(reloadModifier);
            reloadModifier = null;
        }
    }

    private IEnumerator Heavenline(ChampionAbility ability)
    {
        Vector3 start = transform.position + Vector3.up * 1.2f;
        Vector3 end = ability.LastTargetPosition;

        if (activeBeacon.HasValue)
        {
            Vector3 beacon = activeBeacon.Value;
            yield return TravelBolt(start, beacon, 0.22f, 0.34f, new Color(0.14f, 0.86f, 1f));
            yield return TravelBolt(beacon, end, 0.18f, 0.30f, new Color(0.68f, 0.28f, 1f));
            DamageLine(beacon, end, 1.8f, 145f + champion.AttackDamage * 0.75f);
        }
        else
        {
            yield return TravelBolt(start, end, 0.32f, 0.46f, new Color(0.18f, 0.82f, 1f));
            DamageLine(start, end, 1.8f, 145f + champion.AttackDamage * 0.75f);
        }
    }

    private IEnumerator TravelBolt(Vector3 from, Vector3 to, float duration, float radius, Color color)
    {
        GameObject bolt = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bolt.name = "Veyra_Vector_Bolt";
        bolt.transform.localScale = Vector3.one * radius;
        Collider col = bolt.GetComponent<Collider>();
        if (col != null) Destroy(col);

        Renderer renderer = bolt.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Standard"));
        mat.color = color;
        renderer.sharedMaterial = mat;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            bolt.transform.position = Vector3.Lerp(from, to, t);
            yield return null;
        }

        Destroy(bolt);
    }

    private void DamageLine(Vector3 from, Vector3 to, float radius, float damage)
    {
        Vector3 direction = to - from;
        float distance = direction.magnitude;
        if (distance < 0.01f)
            return;

        RaycastHit[] hits = Physics.SphereCastAll(from, radius, direction.normalized, distance);
        HashSet<Object> damaged = new();
        foreach (RaycastHit hit in hits)
        {
            Champion target = hit.collider.GetComponentInParent<Champion>();
            if (target != null && target != champion && target.Team != champion.Team && damaged.Add(target))
            {
                target.TakeDamage(damage, DamageType.Physical);
                continue;
            }

            CombatUnit unit = hit.collider.GetComponentInParent<CombatUnit>();
            if (unit != null && unit.UnitTeam != champion.Team && damaged.Add(unit))
                unit.TakeDamage(damage);
        }
    }

    private void OnDestroy()
    {
        foreach (ChampionAbility ability in abilities.Values)
            if (ability != null) ability.OnCastCompleted -= HandleCastCompleted;

        if (champion != null && identityModifier != null)
            champion.RemoveStatModifier(identityModifier);
        if (champion != null && reloadModifier != null)
            champion.RemoveStatModifier(reloadModifier);
        if (beaconVisual != null)
            Destroy(beaconVisual);
    }
}

public class VeyraBeaconSpin : MonoBehaviour
{
    void Update()
    {
        transform.Rotate(Vector3.up, Time.deltaTime * 85f, Space.World);
        float pulse = 1f + Mathf.Sin(Time.time * 4f) * 0.08f;
        transform.localScale = new Vector3(1.8f * pulse, 0.05f, 1.8f * pulse);
    }
}
