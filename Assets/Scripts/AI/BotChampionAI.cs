using UnityEngine;

/// <summary>
/// Role-aware combat bot foundation with health-state decisions, real ability casting and basic farming pressure.
/// </summary>
public class BotChampionAI : MonoBehaviour
{
    private Champion champion;
    private float decisionTimer;
    private float nextAbilityThinkTime;

    [SerializeField] private float decisionInterval = 0.65f;
    [SerializeField] private float aggroRange = 15f;
    [SerializeField] private float retreatHealthPercent = 0.30f;
    [SerializeField] private float abilityThinkInterval = 0.45f;

    private Champion nearestEnemy;
    private AIState currentState = AIState.Idle;

    private enum AIState
    {
        Idle,
        Farming,
        Engaging,
        Retreating,
        Teamfighting
    }

    void Start()
    {
        champion = GetComponent<Champion>();
        decisionTimer = decisionInterval;
    }

    void Update()
    {
        if (champion == null || !champion.IsAlive)
            return;

        decisionTimer -= Time.deltaTime;
        if (decisionTimer <= 0f)
        {
            MakeDecision();
            decisionTimer = decisionInterval;
        }

        ExecuteState();
    }

    private void MakeDecision()
    {
        nearestEnemy = FindNearestEnemy();

        if (champion.HealthPercent < retreatHealthPercent)
        {
            currentState = AIState.Retreating;
            return;
        }

        if (nearestEnemy != null)
        {
            currentState = AIState.Engaging;
            return;
        }

        currentState = AIState.Farming;
    }

    private void ExecuteState()
    {
        switch (currentState)
        {
            case AIState.Engaging:
                EngageEnemy();
                break;
            case AIState.Retreating:
                Retreat();
                break;
            case AIState.Farming:
                FarmMinions();
                break;
        }
    }

    private void EngageEnemy()
    {
        if (nearestEnemy == null || !nearestEnemy.IsAlive)
            return;

        Vector3 toEnemy = nearestEnemy.transform.position - transform.position;
        float distance = toEnemy.magnitude;
        Vector3 direction = distance > 0.01f ? toEnemy / distance : Vector3.zero;

        ChampionAbility[] abilities = GetComponents<ChampionAbility>();
        float preferredRange = 3.5f;
        foreach (ChampionAbility ability in abilities)
        {
            if (ability != null)
                preferredRange = Mathf.Max(preferredRange, Mathf.Min(ability.Range * 0.75f, 9f));
        }

        if (distance > preferredRange)
            transform.position += direction * champion.MovementSpeed * 0.72f * Time.deltaTime;
        else if (distance < 2.2f)
            transform.position -= direction * champion.MovementSpeed * 0.35f * Time.deltaTime;

        Face(nearestEnemy.transform.position);

        if (Time.time >= nextAbilityThinkTime)
        {
            TryCastBestAvailableAbility(abilities, distance);
            nextAbilityThinkTime = Time.time + abilityThinkInterval;
        }
    }

    private void TryCastBestAvailableAbility(ChampionAbility[] abilities, float distance)
    {
        ChampionAbility best = null;
        float bestScore = float.MinValue;

        foreach (ChampionAbility ability in abilities)
        {
            if (ability == null || !ability.CanCast())
                continue;
            if (distance > ability.Range + ability.AOERadius)
                continue;

            float readinessBias = ability.Key == AbilityKey.R ? 3.5f : 1f;
            float rangeFit = 1f - Mathf.Clamp01(Mathf.Abs(distance - Mathf.Max(1f, ability.Range * 0.65f)) / Mathf.Max(1f, ability.Range));
            float score = readinessBias + rangeFit + Random.Range(0f, 0.8f);

            if (score > bestScore)
            {
                bestScore = score;
                best = ability;
            }
        }

        if (best == null)
            return;

        Champion targetChampion = best.Type == AbilityType.SingleTarget ? nearestEnemy : null;
        best.Cast(nearestEnemy.transform.position, targetChampion);
    }

    private void Retreat()
    {
        if (nearestEnemy == null)
        {
            currentState = AIState.Farming;
            return;
        }

        Vector3 away = transform.position - nearestEnemy.transform.position;
        away.y = 0f;
        if (away.sqrMagnitude > 0.01f)
            transform.position += away.normalized * champion.MovementSpeed * Time.deltaTime;
    }

    private void FarmMinions()
    {
        CombatUnit[] units = FindObjectsByType<CombatUnit>(FindObjectsSortMode.None);
        CombatUnit nearestMinion = null;
        float minDistance = Mathf.Infinity;

        foreach (CombatUnit unit in units)
        {
            if (unit == null || !unit.IsAlive || unit.UnitType != UnitType.Minion || unit.UnitTeam == champion.Team)
                continue;

            float distance = Vector3.Distance(transform.position, unit.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearestMinion = unit;
            }
        }

        if (nearestMinion == null || minDistance >= 24f)
            return;

        Vector3 direction = nearestMinion.transform.position - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.01f)
            transform.position += direction.normalized * champion.MovementSpeed * 0.55f * Time.deltaTime;
    }

    private Champion FindNearestEnemy()
    {
        Champion[] allChampions = FindObjectsByType<Champion>(FindObjectsSortMode.None);
        Champion nearest = null;
        float minDistance = Mathf.Infinity;

        foreach (Champion other in allChampions)
        {
            if (other == null || other == champion || !other.IsAlive || other.Team == champion.Team)
                continue;

            float distance = Vector3.Distance(transform.position, other.transform.position);
            if (distance < minDistance && distance < aggroRange)
            {
                minDistance = distance;
                nearest = other;
            }
        }

        return nearest;
    }

    private void Face(Vector3 point)
    {
        Vector3 direction = point - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.01f)
            return;

        Quaternion target = Quaternion.LookRotation(direction.normalized);
        transform.rotation = Quaternion.Slerp(transform.rotation, target, Time.deltaTime * 8f);
    }
}
