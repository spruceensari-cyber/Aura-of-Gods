using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Signature gameplay layer for Nyxara, Rift Dancer.
/// Adds movement-history echoes, reactive rhythm and layered spatial detonations on top of the generic ability runtime.
/// </summary>
public class NyxaraRiftDancerKit : MonoBehaviour
{
    private Champion champion;
    private ChampionController controller;
    private AOGChampionBlueprint blueprint;
    private readonly List<Vector3> movementHistory = new();
    private readonly Dictionary<AbilityKey, ChampionAbility> abilities = new();
    private StatModifier identityModifier;
    private StatModifier rhythmModifier;
    private float historyTimer;
    private Vector3 lastRecordedPosition;
    private int passiveStacks;

    public string ChampionId => blueprint?.id ?? "nyxara_rift_dancer";
    public int PassiveStacks => passiveStacks;

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
        SubscribeCombatEvents();
        RecordHistoryPoint(transform.position, true);
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

            ability.ConfigureRuntime(
                spec.key,
                spec.type,
                spec.name,
                spec.description,
                spec.manaCost,
                spec.cooldown,
                spec.castTime,
                spec.baseDamage,
                spec.apRatio,
                spec.range,
                spec.radius);

            ability.OnCastStarted -= HandleCastStarted;
            ability.OnCastCompleted -= HandleCastCompleted;
            ability.OnCastStarted += HandleCastStarted;
            ability.OnCastCompleted += HandleCastCompleted;
            abilities[spec.key] = ability;
        }

        controller?.RefreshAbilities();
    }

    private void SubscribeCombatEvents()
    {
        if (controller == null)
            return;

        controller.OnBasicAttackResolved -= HandleBasicAttackResolved;
        controller.OnBasicAttackResolved += HandleBasicAttackResolved;
    }

    private void OnDestroy()
    {
        if (controller != null)
            controller.OnBasicAttackResolved -= HandleBasicAttackResolved;

        foreach (ChampionAbility ability in abilities.Values)
        {
            if (ability == null)
                continue;
            ability.OnCastStarted -= HandleCastStarted;
            ability.OnCastCompleted -= HandleCastCompleted;
        }

        if (champion != null && identityModifier != null)
            champion.RemoveStatModifier(identityModifier);
    }

    private void Update()
    {
        if (champion == null || !champion.IsAlive)
            return;

        historyTimer += Time.deltaTime;
        if (historyTimer >= 0.45f)
        {
            historyTimer = 0f;
            RecordHistoryPoint(transform.position, false);
        }
    }

    private void RecordHistoryPoint(Vector3 point, bool force)
    {
        if (!force && Vector3.Distance(lastRecordedPosition, point) < 1.1f)
            return;

        movementHistory.Add(point);
        lastRecordedPosition = point;
        while (movementHistory.Count > 8)
            movementHistory.RemoveAt(0);
    }

    private void HandleBasicAttackResolved()
    {
        passiveStacks = Mathf.Min(3, passiveStacks + 1);
        if (passiveStacks < 3)
            return;

        passiveStacks = 0;
        CreatePulse(transform.position, 2.4f, new Color(0.46f, 0.18f, 0.95f, 0.42f), 0.35f);
        DealEchoDamage(transform.position, 2.4f, 22f + champion.AbilityPower * 0.18f);
    }

    private void HandleCastStarted(ChampionAbility ability)
    {
        if (ability == null)
            return;

        if (ability.Key == AbilityKey.Q)
            RecordHistoryPoint(transform.position, true);

        if (ability.Key == AbilityKey.W)
            StartCoroutine(ActivateCombatRhythm());
    }

    private void HandleCastCompleted(ChampionAbility ability)
    {
        if (ability == null)
            return;

        switch (ability.Key)
        {
            case AbilityKey.Q:
                StartCoroutine(ResolveSeveringStep(ability));
                break;
            case AbilityKey.W:
                ResolveVeilParry();
                break;
            case AbilityKey.E:
                StartCoroutine(ResolveMemoryWell(ability));
                break;
            case AbilityKey.R:
                StartCoroutine(ResolveTwoFuturesCollide(ability));
                break;
        }
    }

    private IEnumerator ResolveSeveringStep(ChampionAbility ability)
    {
        Vector3 origin = transform.position;
        Vector3 destination = ability.LastTargetPosition;
        destination.y = transform.position.y;

        Vector3 delta = destination - origin;
        if (delta.magnitude > ability.Range)
            destination = origin + delta.normalized * ability.Range;

        float duration = 0.13f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            transform.position = Vector3.Lerp(origin, destination, t);
            yield return null;
        }

        transform.position = destination;
        RecordHistoryPoint(destination, true);
        CreatePulse(destination, 2.0f, new Color(0.18f, 0.68f, 1f, 0.38f), 0.25f);

        yield return new WaitForSeconds(0.32f);
        CreatePulse(origin, 2.8f, new Color(0.68f, 0.20f, 1f, 0.44f), 0.4f);
        DealEchoDamage(origin, 2.8f, 38f + champion.AbilityPower * 0.28f);
    }

    private void ResolveVeilParry()
    {
        CreatePulse(transform.position, 4.2f, new Color(0.22f, 0.78f, 0.95f, 0.34f), 0.48f);
        passiveStacks = Mathf.Min(3, passiveStacks + 1);
    }

    private IEnumerator ActivateCombatRhythm()
    {
        if (champion == null)
            yield break;

        if (rhythmModifier != null)
            champion.RemoveStatModifier(rhythmModifier);

        rhythmModifier = new StatModifier
        {
            MovementSpeedBonus = 0.65f,
            AttackSpeedBonus = 0.35f
        };
        champion.AddStatModifier(rhythmModifier);

        yield return new WaitForSeconds(2.6f);

        if (champion != null && rhythmModifier != null)
        {
            champion.RemoveStatModifier(rhythmModifier);
            rhythmModifier = null;
        }
    }

    private IEnumerator ResolveMemoryWell(ChampionAbility ability)
    {
        Vector3 center = ability.LastTargetPosition;
        CreatePulse(center, ability.AOERadius, new Color(0.30f, 0.12f, 0.72f, 0.46f), 0.7f);
        RecordHistoryPoint(center, true);

        float elapsed = 0f;
        while (elapsed < 2.4f)
        {
            elapsed += 0.6f;
            DealEchoDamage(center, ability.AOERadius, 18f + champion.AbilityPower * 0.10f);
            yield return new WaitForSeconds(0.6f);
        }
    }

    private IEnumerator ResolveTwoFuturesCollide(ChampionAbility ability)
    {
        List<Vector3> detonationPoints = new();
        int start = Mathf.Max(0, movementHistory.Count - 4);
        for (int i = start; i < movementHistory.Count; i++)
            detonationPoints.Add(movementHistory[i]);
        detonationPoints.Add(ability.LastTargetPosition);

        foreach (Vector3 point in detonationPoints)
        {
            CreatePulse(point, 3.4f, new Color(0.74f, 0.28f, 1f, 0.52f), 0.5f);
            DealEchoDamage(point, 3.4f, 48f + champion.AbilityPower * 0.22f);
            yield return new WaitForSeconds(0.16f);
        }

        CreatePulse(ability.LastTargetPosition, ability.AOERadius, new Color(0.12f, 0.82f, 1f, 0.58f), 0.9f);
        DealEchoDamage(ability.LastTargetPosition, ability.AOERadius, 92f + champion.AbilityPower * 0.38f);
    }

    private void DealEchoDamage(Vector3 center, float radius, float damage)
    {
        HashSet<Object> damaged = new();
        Collider[] hits = Physics.OverlapSphere(center, radius);
        foreach (Collider hit in hits)
        {
            Champion targetChampion = hit.GetComponentInParent<Champion>();
            if (targetChampion != null && targetChampion != champion && targetChampion.Team != champion.Team && damaged.Add(targetChampion))
            {
                targetChampion.TakeDamage(damage, DamageType.Magical);
                continue;
            }

            CombatUnit unit = hit.GetComponentInParent<CombatUnit>();
            if (unit != null && unit.UnitTeam != champion.Team && damaged.Add(unit))
                unit.TakeDamage(damage);
        }
    }

    private void CreatePulse(Vector3 position, float radius, Color color, float lifetime)
    {
        GameObject pulse = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pulse.name = "Nyxara_Echo_Pulse";
        pulse.transform.position = position + Vector3.up * 0.08f;
        pulse.transform.localScale = new Vector3(0.08f, 0.02f, 0.08f);

        Collider collider = pulse.GetComponent<Collider>();
        if (collider != null)
            Destroy(collider);

        Renderer renderer = pulse.GetComponent<Renderer>();
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
        Material material = new Material(shader);
        material.color = color;
        renderer.sharedMaterial = material;

        StartCoroutine(AnimatePulse(pulse.transform, radius, lifetime));
    }

    private IEnumerator AnimatePulse(Transform pulse, float radius, float lifetime)
    {
        float elapsed = 0f;
        while (pulse != null && elapsed < lifetime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / lifetime);
            float scale = Mathf.Lerp(0.08f, radius, 1f - Mathf.Pow(1f - t, 3f));
            pulse.localScale = new Vector3(scale, 0.02f, scale);
            yield return null;
        }

        if (pulse != null)
            Destroy(pulse.gameObject);
    }
}
