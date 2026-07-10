using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Competitive attack-move command layer for the authoritative human champion.
/// It reuses AOGUnifiedMobaInputDriver's existing target/chase/attack authority and only writes
/// command state through cached reflection handles because the current driver keeps those fields private.
/// No second damage or attack timing system is introduced.
/// </summary>
[DefaultExecutionOrder(-55)]
public class AOGAttackMoveCommandRuntime : MonoBehaviour
{
    public float acquireRadius = 8.5f;
    public float championThreatRadius = 7.25f;
    public float destinationTolerance = 0.55f;
    public float reacquireInterval = 0.08f;

    private AOGUnifiedMobaInputDriver driver;
    private AOGCharacterStats stats;
    private AOGActiveChampion champion;
    private Camera gameplayCamera;

    private bool armed;
    private bool attackMoveActive;
    private Vector3 attackMovePoint;
    private float nextAcquire;
    private bool moveIssued;

    private FieldInfo targetHeroField;
    private FieldInfo targetMinionField;
    private FieldInfo targetNeutralField;
    private FieldInfo targetTowerField;
    private FieldInfo targetNexusField;
    private FieldInfo targetBossField;
    private FieldInfo hasMoveTargetField;
    private MethodInfo setMoveTargetMethod;
    private MethodInfo clearTargetsMethod;

    private void Awake()
    {
        driver = GetComponent<AOGUnifiedMobaInputDriver>();
        stats = GetComponent<AOGCharacterStats>();
        champion = GetComponent<AOGActiveChampion>();
        gameplayCamera = Camera.main;
        CacheDriverAccessors();
    }

    private void Update()
    {
        if (!IsAuthoritativePlayer()) return;
        if (gameplayCamera == null || !gameplayCamera.isActiveAndEnabled) gameplayCamera = Camera.main;

        GuardVisionLostTargets();

        if (AOGInputBridge.RightPressedThisFrame())
        {
            armed = false;
            attackMoveActive = false;
            moveIssued = false;
            return;
        }

        if (AOGInputBridge.KeyPressedThisFrame(KeyCode.A))
        {
            armed = true;
            attackMoveActive = false;
            moveIssued = false;
            ShowArmedPulse();
        }

        if (armed && AOGInputBridge.LeftPressedThisFrame())
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;
            if (!ResolvePointerGround(out attackMovePoint)) return;
            armed = false;
            attackMoveActive = true;
            moveIssued = false;
            nextAcquire = 0f;
            ShowCommandPoint(attackMovePoint);
        }

        if (!attackMoveActive) return;

        if (FlatDistance(transform.position,attackMovePoint) <= destinationTolerance && !HasCombatTarget())
        {
            attackMoveActive = false;
            moveIssued = false;
            return;
        }

        if (Time.time < nextAcquire) return;
        nextAcquire = Time.time + reacquireInterval;

        if (HasCombatTarget()) return;

        Object target = AcquireBestTarget();
        if (target != null)
        {
            AssignTarget(target);
            moveIssued = false;
        }
        else if (!moveIssued)
        {
            IssueMove(attackMovePoint);
            moveIssued = true;
        }
    }

    private bool IsAuthoritativePlayer()
    {
        if (driver == null || stats == null || champion == null || stats.IsDead) return false;
        if (!driver.enabled || !champion.IsActiveChampion) return false;
        return AOGPlayerChampionAuthority.CurrentChampion == champion;
    }

    private void CacheDriverAccessors()
    {
        if (driver == null) return;
        BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        System.Type type = typeof(AOGUnifiedMobaInputDriver);
        targetHeroField = type.GetField("targetHero",flags);
        targetMinionField = type.GetField("targetMinion",flags);
        targetNeutralField = type.GetField("targetNeutralMonster",flags);
        targetTowerField = type.GetField("targetTower",flags);
        targetNexusField = type.GetField("targetNexus",flags);
        targetBossField = type.GetField("targetBoss",flags);
        hasMoveTargetField = type.GetField("hasMoveTarget",flags);
        setMoveTargetMethod = type.GetMethod("SetMoveTarget",flags);
        clearTargetsMethod = type.GetMethod("ClearTargets",flags);
    }

    private Object AcquireBestTarget()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position,acquireRadius,~0,QueryTriggerInteraction.Ignore);
        AOGCharacterStats bestHero = null;
        float bestHeroDistance = championThreatRadius;
        Minion bestMinion = null;
        float bestMinionScore = float.PositiveInfinity;
        AOGNeutralMonsterRuntime bestNeutral = null;
        float bestNeutralDistance = acquireRadius;

        foreach (Collider hit in hits)
        {
            if (hit == null) continue;

            AOGCharacterStats hero = hit.GetComponentInParent<AOGCharacterStats>();
            if (hero != null && hero != stats && !hero.IsDead && hero.team != stats.team)
            {
                if (!AOGVisionAuthorityRuntime.IsVisibleToTeam(hero.transform.position,stats.team)) continue;
                float d = FlatDistance(transform.position,hero.transform.position);
                if (d < bestHeroDistance) { bestHero = hero; bestHeroDistance = d; }
                continue;
            }

            Minion minion = hit.GetComponentInParent<Minion>();
            if (minion != null && minion.hp > 0f && minion.team != stats.team)
            {
                if (!AOGVisionAuthorityRuntime.IsVisibleToTeam(minion.transform.position,stats.team)) continue;
                float distance = FlatDistance(transform.position,minion.transform.position);
                float hpRatio = minion.hp / Mathf.Max(1f,minion.maxHp);
                float destinationBias = FlatDistance(minion.transform.position,attackMovePoint) * 0.035f;
                float score = distance * 0.65f + hpRatio * 2.2f + destinationBias;
                if (score < bestMinionScore) { bestMinion = minion; bestMinionScore = score; }
                continue;
            }

            AOGNeutralMonsterRuntime neutral = hit.GetComponentInParent<AOGNeutralMonsterRuntime>();
            if (neutral != null && !neutral.IsDead && AOGVisionAuthorityRuntime.IsVisibleToTeam(neutral.transform.position,stats.team))
            {
                float d = FlatDistance(transform.position,neutral.transform.position);
                if (d < bestNeutralDistance) { bestNeutral = neutral; bestNeutralDistance = d; }
            }
        }

        if (bestHero != null) return bestHero;
        if (bestMinion != null) return bestMinion;
        if (bestNeutral != null && FlatDistance(bestNeutral.transform.position,attackMovePoint) <= acquireRadius * 0.75f) return bestNeutral;

        TowerHealth bestTower = null;
        float bestTowerDistance = acquireRadius;
        foreach (TowerHealth tower in AOGWorldRegistry.Towers)
        {
            if (tower == null || tower.hp <= 0f || tower.towerTeam == stats.team) continue;
            float d = FlatDistance(transform.position,tower.transform.position);
            if (d < bestTowerDistance) { bestTower = tower; bestTowerDistance = d; }
        }
        return bestTower;
    }

    private void AssignTarget(Object target)
    {
        if (driver == null || target == null) return;
        clearTargetsMethod?.Invoke(driver,null);
        hasMoveTargetField?.SetValue(driver,false);

        if (target is AOGCharacterStats hero) targetHeroField?.SetValue(driver,hero);
        else if (target is Minion minion) targetMinionField?.SetValue(driver,minion);
        else if (target is AOGNeutralMonsterRuntime neutral) targetNeutralField?.SetValue(driver,neutral);
        else if (target is TowerHealth tower) targetTowerField?.SetValue(driver,tower);
        else if (target is AOGNexusCore nexus) targetNexusField?.SetValue(driver,nexus);
        else if (target is AOGNeutralBossAI boss) targetBossField?.SetValue(driver,boss);
    }

    private void IssueMove(Vector3 point)
    {
        if (driver == null || setMoveTargetMethod == null) return;
        setMoveTargetMethod.Invoke(driver,new object[] { point });
    }

    private bool HasCombatTarget()
    {
        if (driver == null) return false;
        return targetHeroField?.GetValue(driver) != null ||
               targetMinionField?.GetValue(driver) != null ||
               targetNeutralField?.GetValue(driver) != null ||
               targetTowerField?.GetValue(driver) != null ||
               targetNexusField?.GetValue(driver) != null ||
               targetBossField?.GetValue(driver) != null;
    }

    private void GuardVisionLostTargets()
    {
        if (driver == null || stats == null) return;

        AOGCharacterStats hero = targetHeroField?.GetValue(driver) as AOGCharacterStats;
        if (hero != null && !AOGVisionAuthorityRuntime.IsVisibleToTeam(hero.transform.position,stats.team))
        {
            clearTargetsMethod?.Invoke(driver,null);
            moveIssued = false;
            return;
        }

        Minion minion = targetMinionField?.GetValue(driver) as Minion;
        if (minion != null && minion.team != stats.team && !AOGVisionAuthorityRuntime.IsVisibleToTeam(minion.transform.position,stats.team))
        {
            clearTargetsMethod?.Invoke(driver,null);
            moveIssued = false;
        }
    }

    private bool ResolvePointerGround(out Vector3 point)
    {
        point = transform.position;
        if (gameplayCamera == null) return false;
        Ray ray = gameplayCamera.ScreenPointToRay(AOGInputBridge.PointerPosition);
        if (Physics.Raycast(ray,out RaycastHit hit,1200f,driver != null ? driver.commandMask : ~0,QueryTriggerInteraction.Ignore))
        {
            point = hit.point;
            point.y = transform.position.y;
            return true;
        }

        Plane ground = new Plane(Vector3.up,transform.position);
        if (!ground.Raycast(ray,out float enter)) return false;
        point = ray.GetPoint(enter);
        point.y = transform.position.y;
        return true;
    }

    private void ShowArmedPulse()
    {
        Color color = champion != null ? champion.accentColor : new Color(0.34f,0.78f,1f);
        GameObject ring = AOGAbilityVisuals.CreateRing("AttackMove_Armed",transform.position+Vector3.up*0.05f,1.7f,color,0.08f);
        Destroy(ring,0.35f);
    }

    private void ShowCommandPoint(Vector3 point)
    {
        Color color = champion != null ? champion.accentColor : new Color(0.34f,0.78f,1f);
        GameObject ring = AOGAbilityVisuals.CreateRing("AttackMove_Command",point+Vector3.up*0.05f,1.1f,color,0.07f);
        Destroy(ring,0.55f);
    }

    private static float FlatDistance(Vector3 a,Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a,b);
    }
}

[DefaultExecutionOrder(-600)]
public class AOGAttackMoveBootstrap : MonoBehaviour
{
    private float nextAttach;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindFirstObjectByType<AOGAttackMoveBootstrap>() != null) return;
        GameObject host = new GameObject("AOG_Attack_Move_Bootstrap");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGAttackMoveBootstrap>();
    }

    private void Update()
    {
        if (Time.unscaledTime < nextAttach) return;
        nextAttach = Time.unscaledTime + 0.5f;
        AOGActiveChampion player = AOGPlayerChampionAuthority.CurrentChampion;
        if (player == null) return;
        if (player.GetComponent<AOGAttackMoveCommandRuntime>() == null)
            player.gameObject.AddComponent<AOGAttackMoveCommandRuntime>();
    }
}
