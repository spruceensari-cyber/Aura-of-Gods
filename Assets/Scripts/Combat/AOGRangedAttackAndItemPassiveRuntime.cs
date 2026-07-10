using System.Collections;
using UnityEngine;

/// <summary>
/// Legacy compatibility component. Authoritative ranged travel and impact are now owned by
/// AOGChampionAttackProjectile through AOGUnifiedMobaInputDriver.
/// </summary>
public class AOGRangedAttackPresentationRuntime : MonoBehaviour
{
}

/// <summary>
/// Resolves Aether Market combat passives from unified combat events, allowing items to
/// affect champion, minion, neutral and boss combat without duplicate target-specific code.
/// </summary>
public class AOGItemPassiveCombatRuntime : MonoBehaviour
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

    private void OnEnable()
    {
        AOGCombatEvents.BasicAttackHit += OnBasicAttackHit;
    }

    private void OnDisable()
    {
        AOGCombatEvents.BasicAttackHit -= OnBasicAttackHit;
    }

    private void Update()
    {
        if (stats == null || economy == null || champion == null || !champion.IsActiveChampion || stats.IsDead)
            return;

        if (Has("titanheart") && stats.hp / Mathf.Max(1f,stats.maxHp) < 0.30f && Time.time >= nextTitanGuard)
        {
            nextTitanGuard = Time.time + 18f;
            stats.hp = Mathf.Min(stats.maxHp, stats.hp + stats.maxHp * 0.10f);
            SpawnPassiveRing("Titan_Heart_Guard",2.5f,new Color(0.96f,0.26f,0.32f));
        }

        if (Has("warstride") && Time.time >= nextWarstrideBurst)
        {
            nextWarstrideBurst = Time.time + 12f;
            StartCoroutine(WarstrideBurst());
        }
    }

    private void OnBasicAttackHit(AOGCombatHitEvent hit)
    {
        if (stats == null || economy == null || champion == null || !champion.IsActiveChampion)
            return;
        if (!BelongsToThisChampion(hit.source) || hit.target == null)
            return;

        if (Has("moonblade"))
            stats.hp = Mathf.Min(stats.maxHp,stats.hp+Mathf.Max(4f,stats.attackDamage*0.055f));

        if (Has("starfang"))
            DealBonusDamage(hit,stats.attackDamage*0.12f);

        float healthRatio = TargetHealthRatio(hit);
        if (Has("voidglass") && healthRatio >= 0f && healthRatio < 0.35f)
            DealBonusDamage(hit,stats.attackDamage*0.20f);

        if (Has("eclipse"))
        {
            stats.hp=Mathf.Min(stats.maxHp,stats.hp+8f);
            if (Random.value<0.16f)
                DealBonusDamage(hit,stats.attackDamage*0.26f);
        }

        if (Has("godbreaker") && healthRatio >= 0f && healthRatio < 0.18f)
        {
            DealBonusDamage(hit,stats.attackDamage*0.45f);
            SpawnPassiveRing("Godbreaker_Execute",1.3f,new Color(1f,0.42f,0.12f));
        }
    }

    private IEnumerator WarstrideBurst()
    {
        float bonus=0.55f;
        stats.moveSpeed+=bonus;
        SpawnPassiveRing("Warstride_Burst",1.8f,new Color(0.42f,0.86f,0.48f));
        yield return new WaitForSeconds(2.5f);
        if (stats!=null)
            stats.moveSpeed=Mathf.Max(1f,stats.moveSpeed-bonus);
    }

    private bool BelongsToThisChampion(GameObject source)
    {
        if (source==null) return false;
        if (source==gameObject || source.transform.IsChildOf(transform)) return true;
        AOGCharacterStats parent=source.GetComponentInParent<AOGCharacterStats>();
        return parent!=null && parent==stats;
    }

    private void DealBonusDamage(AOGCombatHitEvent hit,float amount)
    {
        if (hit.target==null || amount<=0f) return;
        switch (hit.targetKind)
        {
            case AOGCombatTargetKind.Champion:
                hit.target.GetComponentInParent<AOGCharacterStats>()?.TakeDamage(amount,gameObject);
                break;
            case AOGCombatTargetKind.Minion:
            {
                Minion minion=hit.target.GetComponentInParent<Minion>();
                if (minion!=null) minion.TakeDamage(amount,gameObject);
                break;
            }
            case AOGCombatTargetKind.NeutralMonster:
                hit.target.GetComponentInParent<AOGNeutralMonsterRuntime>()?.TakeDamage(amount,gameObject);
                break;
            case AOGCombatTargetKind.Boss:
                hit.target.GetComponentInParent<AOGNeutralBossAI>()?.TakeDamage(amount,gameObject);
                break;
        }
    }

    private static float TargetHealthRatio(AOGCombatHitEvent hit)
    {
        if (hit.target==null) return -1f;
        if (hit.targetKind==AOGCombatTargetKind.Champion)
        {
            AOGCharacterStats hero=hit.target.GetComponentInParent<AOGCharacterStats>();
            return hero!=null?hero.hp/Mathf.Max(1f,hero.maxHp):-1f;
        }
        if (hit.targetKind==AOGCombatTargetKind.Minion)
        {
            Minion minion=hit.target.GetComponentInParent<Minion>();
            return minion!=null?minion.hp/Mathf.Max(1f,minion.maxHp):-1f;
        }
        if (hit.targetKind==AOGCombatTargetKind.NeutralMonster)
        {
            AOGNeutralMonsterRuntime monster=hit.target.GetComponentInParent<AOGNeutralMonsterRuntime>();
            return monster!=null?monster.hp/Mathf.Max(1f,monster.maxHp):-1f;
        }
        if (hit.targetKind==AOGCombatTargetKind.Boss)
        {
            AOGNeutralBossAI boss=hit.target.GetComponentInParent<AOGNeutralBossAI>();
            return boss!=null?boss.hp/Mathf.Max(1f,boss.maxHp):-1f;
        }
        return -1f;
    }

    private bool Has(string id)
    {
        foreach (AOGItemDefinition item in economy.inventory)
            if (item!=null && item.id==id) return true;
        return false;
    }

    private void SpawnPassiveRing(string name,float radius,Color color)
    {
        GameObject ring=AOGAbilityVisuals.CreateRing(name,transform.position+Vector3.up*0.06f,radius,color,0.08f);
        Destroy(ring,0.45f);
    }
}

public class AOGCombatEnhancementBootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindFirstObjectByType<AOGCombatEnhancementBootstrap>()!=null)
            return;
        GameObject host=new GameObject("AOG_Combat_Enhancement_Bootstrap");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGCombatEnhancementBootstrap>();
    }

    private float nextScan;
    private void Update()
    {
        if (Time.unscaledTime<nextScan) return;
        nextScan=Time.unscaledTime+0.75f;

        foreach (AOGActiveChampion hero in FindObjectsByType<AOGActiveChampion>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {
            if (hero==null) continue;
            if (hero.GetComponent<AOGItemPassiveCombatRuntime>()==null)
                hero.gameObject.AddComponent<AOGItemPassiveCombatRuntime>();
        }
    }
}
