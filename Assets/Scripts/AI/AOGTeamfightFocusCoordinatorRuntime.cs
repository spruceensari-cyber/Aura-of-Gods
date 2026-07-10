using System.Reflection;
using UnityEngine;

/// <summary>
/// Team-level focus coordinator. Selects one visible priority enemy per team and lets role bots
/// converge on the same fight only when local numbers and health permit. Support keeps peel priority.
/// </summary>
[DefaultExecutionOrder(75)]
public class AOGTeamfightFocusCoordinatorRuntime : MonoBehaviour
{
    public static AOGCharacterStats BlueFocus { get; private set; }
    public static AOGCharacterStats RedFocus { get; private set; }

    private float nextThink;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindFirstObjectByType<AOGTeamfightFocusCoordinatorRuntime>() != null) return;
        GameObject host = new GameObject("AOG_Teamfight_Focus_Coordinator");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGTeamfightFocusCoordinatorRuntime>();
    }

    private void Update()
    {
        if (AOGMatchDirector.Instance == null || AOGMatchDirector.Instance.State != AOGMatchState.Playing) return;
        if (Time.time < nextThink) return;
        nextThink = Time.time + 0.30f;

        BlueFocus = SelectFocus(MinionTeam.Blue);
        RedFocus = SelectFocus(MinionTeam.Red);
    }

    public static AOGCharacterStats GetFocus(MinionTeam team)
    {
        return team == MinionTeam.Blue ? BlueFocus : RedFocus;
    }

    private static AOGCharacterStats SelectFocus(MinionTeam team)
    {
        AOGCharacterStats best = null;
        float bestScore = float.MaxValue;

        foreach (AOGCharacterStats enemy in AOGWorldRegistry.Characters)
        {
            if (enemy == null || enemy.IsDead || enemy.team == team) continue;
            if (!AOGVisionAuthorityRuntime.IsVisibleToTeam(enemy.transform.position,team)) continue;

            int nearbyAllies = CountTeamNear(enemy.transform.position,team,13f);
            if (nearbyAllies < 2) continue;

            AOGTeamMemberIdentity identity = enemy.GetComponent<AOGTeamMemberIdentity>();
            float hpRatio = enemy.hp / Mathf.Max(1f,enemy.maxHp);
            float roleWeight = 0f;
            if (identity != null)
            {
                if (identity.role == AOGRole.ADC) roleWeight = -3.2f;
                else if (identity.role == AOGRole.Mid) roleWeight = -2.5f;
                else if (identity.role == AOGRole.Support) roleWeight = -0.6f;
                else if (identity.role == AOGRole.Jungle) roleWeight = -1.3f;
                else if (identity.role == AOGRole.Top) roleWeight = 1.2f;
            }

            float score = hpRatio * 7.5f + roleWeight - nearbyAllies * 0.35f;
            if (score < bestScore)
            {
                best = enemy;
                bestScore = score;
            }
        }

        return best;
    }

    private static int CountTeamNear(Vector3 point,MinionTeam team,float radius)
    {
        int count = 0;
        foreach (AOGCharacterStats hero in AOGWorldRegistry.Characters)
        {
            if (hero == null || hero.IsDead || hero.team != team) continue;
            if (FlatDistance(point,hero.transform.position) <= radius) count++;
        }
        return count;
    }

    private static float FlatDistance(Vector3 a,Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a,b);
    }
}

/// <summary>
/// Per-bot execution layer for coordinated focus and peel. It only writes target references in the
/// existing lane/jungle AI; movement, cooldowns, windup and damage remain owned by those systems.
/// </summary>
[DefaultExecutionOrder(76)]
public class AOGTeamfightRoleExecutionRuntime : MonoBehaviour
{
    public float focusJoinRange = 13f;
    public float peelRadius = 7.5f;
    public float safeHpRatio = 0.42f;

    private AOGTeamMemberIdentity identity;
    private AOGCharacterStats stats;
    private AOGBotChampionAI laneAi;
    private AOGJungleChampionAIRuntime jungleAi;
    private FieldInfo laneTargetField;
    private FieldInfo jungleTargetHeroField;
    private float nextThink;

    private void Awake()
    {
        identity = GetComponent<AOGTeamMemberIdentity>();
        stats = GetComponent<AOGCharacterStats>();
        laneAi = GetComponent<AOGBotChampionAI>();
        jungleAi = GetComponent<AOGJungleChampionAIRuntime>();

        BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        laneTargetField = typeof(AOGBotChampionAI).GetField("currentTarget",flags);
        jungleTargetHeroField = typeof(AOGJungleChampionAIRuntime).GetField("targetHero",flags);
    }

    private void Update()
    {
        if (identity == null || identity.isHumanPlayer || stats == null || stats.IsDead) return;
        if (AOGMatchDirector.Instance == null || AOGMatchDirector.Instance.State != AOGMatchState.Playing) return;
        if (Time.time < nextThink) return;
        nextThink = Time.time + 0.18f;

        laneAi = laneAi != null ? laneAi : GetComponent<AOGBotChampionAI>();
        jungleAi = jungleAi != null ? jungleAi : GetComponent<AOGJungleChampionAIRuntime>();

        float hpRatio = stats.hp / Mathf.Max(1f,stats.maxHp);
        if (hpRatio < safeHpRatio) return;

        if (identity.role == AOGRole.Support)
        {
            AOGCharacterStats peel = FindPeelTarget();
            if (peel != null)
            {
                AssignHeroTarget(peel);
                return;
            }
        }

        AOGCharacterStats focus = AOGTeamfightFocusCoordinatorRuntime.GetFocus(stats.team);
        if (!CanJoinFocus(focus)) return;
        AssignHeroTarget(focus);
    }

    private bool CanJoinFocus(AOGCharacterStats focus)
    {
        if (focus == null || focus.IsDead || focus.team == stats.team) return false;
        if (!AOGVisionAuthorityRuntime.IsVisibleToTeam(focus.transform.position,stats.team)) return false;
        if (FlatDistance(transform.position,focus.transform.position) > focusJoinRange) return false;

        int allies = 0;
        int visibleEnemies = 0;
        foreach (AOGCharacterStats hero in AOGWorldRegistry.Characters)
        {
            if (hero == null || hero.IsDead) continue;
            if (FlatDistance(transform.position,hero.transform.position) > 12f) continue;
            if (hero.team == stats.team) allies++;
            else if (AOGVisionAuthorityRuntime.IsVisibleToTeam(hero.transform.position,stats.team)) visibleEnemies++;
        }

        return allies >= visibleEnemies;
    }

    private AOGCharacterStats FindPeelTarget()
    {
        AOGCharacterStats carry = null;
        foreach (AOGTeamMemberIdentity member in AOGWorldRegistry.TeamMembers)
        {
            if (member == null || member.team != stats.team || member.role != AOGRole.ADC) continue;
            AOGCharacterStats candidate = member.GetComponent<AOGCharacterStats>();
            if (candidate != null && !candidate.IsDead)
            {
                carry = candidate;
                break;
            }
        }
        if (carry == null) return null;

        AOGCharacterStats best = null;
        float bestDistance = peelRadius;
        foreach (AOGCharacterStats enemy in AOGWorldRegistry.Characters)
        {
            if (enemy == null || enemy.IsDead || enemy.team == stats.team) continue;
            if (!AOGVisionAuthorityRuntime.IsVisibleToTeam(enemy.transform.position,stats.team)) continue;
            float distance = FlatDistance(carry.transform.position,enemy.transform.position);
            if (distance < bestDistance)
            {
                best = enemy;
                bestDistance = distance;
            }
        }
        return best;
    }

    private void AssignHeroTarget(AOGCharacterStats target)
    {
        if (target == null) return;
        if (jungleAi != null && jungleAi.enabled && jungleTargetHeroField != null)
            jungleTargetHeroField.SetValue(jungleAi,target);
        else if (laneAi != null && laneAi.enabled && laneTargetField != null)
            laneTargetField.SetValue(laneAi,target.transform);
    }

    private static float FlatDistance(Vector3 a,Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a,b);
    }
}

[DefaultExecutionOrder(-565)]
public class AOGTeamfightRoleExecutionBootstrap : MonoBehaviour
{
    private float nextAttach;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindFirstObjectByType<AOGTeamfightRoleExecutionBootstrap>() != null) return;
        GameObject host = new GameObject("AOG_Teamfight_Role_Execution_Bootstrap");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGTeamfightRoleExecutionBootstrap>();
    }

    private void Update()
    {
        if (Time.unscaledTime < nextAttach) return;
        nextAttach = Time.unscaledTime + 0.8f;

        foreach (AOGTeamMemberIdentity member in AOGWorldRegistry.TeamMembers)
        {
            if (member == null || member.isHumanPlayer) continue;
            if (member.GetComponent<AOGTeamfightRoleExecutionRuntime>() == null)
                member.gameObject.AddComponent<AOGTeamfightRoleExecutionRuntime>();
        }
    }
}
