using UnityEngine;

/// <summary>
/// Per-champion identity progression and passive combat behavior.
/// Passive procs consume unified AOGCombatEvents so they work consistently against
/// champions, minions, neutral monsters and bosses without duplicate target-specific paths.
/// </summary>
[DefaultExecutionOrder(80)]
public class AOGChampionIdentityProgressionRuntime : MonoBehaviour
{
    private AOGActiveChampion champion;
    private AOGCharacterStats stats;
    private AOGChampionProgression progression;
    private ChampionPresentationController presentation;

    private int appliedQ;
    private int appliedW;
    private int appliedE;
    private int appliedR;
    private int passiveStacks;
    private float nextPassiveProc;
    private float temporaryRangeEnd;
    private float temporaryRangeBonus;

    private void Awake()
    {
        champion = GetComponent<AOGActiveChampion>();
        stats = GetComponent<AOGCharacterStats>();
        progression = GetComponent<AOGChampionProgression>();
        presentation = GetComponent<ChampionPresentationController>();
    }

    private void OnEnable()
    {
        AOGCombatEvents.BasicAttackHit += OnBasicAttackHitEvent;
    }

    private void OnDisable()
    {
        AOGCombatEvents.BasicAttackHit -= OnBasicAttackHitEvent;
        RemoveTemporaryRangeBonus();
    }

    private void Update()
    {
        if (champion == null || !champion.IsActiveChampion || stats == null || progression == null)
            return;

        ApplyNewRanks();
        if (temporaryRangeBonus > 0f && Time.time >= temporaryRangeEnd)
            RemoveTemporaryRangeBonus();
    }

    private void ApplyNewRanks()
    {
        while (appliedQ < progression.qRank) { appliedQ++; ApplyRankDelta(0); }
        while (appliedW < progression.wRank) { appliedW++; ApplyRankDelta(1); }
        while (appliedE < progression.eRank) { appliedE++; ApplyRankDelta(2); }
        while (appliedR < progression.rRank) { appliedR++; ApplyRankDelta(3); }
    }

    private void ApplyRankDelta(int slot)
    {
        string id = champion.championId.ToLowerInvariant();
        if (id == "lyra")
        {
            if (slot == 0) stats.attackDamage += 3.5f;
            else if (slot == 1) stats.moveSpeed += 0.10f;
            else if (slot == 2) stats.attackRange += 0.08f;
            else stats.attackDamage += 8f;
        }
        else if (id == "kaelith")
        {
            if (slot == 0) stats.attackDamage += 4.5f;
            else if (slot == 1) AddMaxHealth(34f);
            else if (slot == 2) stats.moveSpeed += 0.08f;
            else AddMaxHealth(80f);
        }
        else if (id == "auron")
        {
            if (slot == 0) stats.attackDamage += 4f;
            else if (slot == 1) AddMaxHealth(55f);
            else if (slot == 2) stats.moveSpeed += 0.06f;
            else AddMaxHealth(110f);
        }
        else if (id == "vesper")
        {
            if (slot == 0) stats.attackDamage += 3.8f;
            else if (slot == 1) stats.moveSpeed += 0.12f;
            else if (slot == 2) stats.attackRange += 0.14f;
            else stats.attackCooldown = Mathf.Max(0.38f, stats.attackCooldown * 0.965f);
        }
        else if (id == "nyra")
        {
            if (slot == 0) stats.attackDamage += 3.2f;
            else if (slot == 1) AddMaxHealth(28f);
            else if (slot == 2) stats.moveSpeed += 0.13f;
            else stats.attackDamage += 7.5f;
        }
        else if (id == "pyrelle")
        {
            if (slot == 0) stats.attackDamage += 4.8f;
            else if (slot == 1) stats.attackRange += 0.10f;
            else if (slot == 2) stats.moveSpeed += 0.08f;
            else stats.attackDamage += 10f;
        }
        else if (id == "selene")
        {
            if (slot == 0) stats.attackDamage += 2.8f;
            else if (slot == 1) AddMaxHealth(38f);
            else if (slot == 2) stats.moveSpeed += 0.09f;
            else AddMaxHealth(72f);
        }

        GameObject ring = AOGAbilityVisuals.CreateRing(
            "Champion_Rank_Growth_" + champion.championId,
            transform.position + Vector3.up * 0.08f,
            1.5f + slot * 0.18f,
            champion.accentColor,
            0.07f);
        Destroy(ring, 0.38f);
    }

    private void OnBasicAttackHitEvent(AOGCombatHitEvent hit)
    {
        if (champion == null || stats == null || !champion.IsActiveChampion)
            return;
        if (!SourceBelongsToThisChampion(hit.source))
            return;
        if (hit.target == null)
            return;

        string id = champion.championId.ToLowerInvariant();
        float targetScale = hit.targetKind == AOGCombatTargetKind.Champion ? 1f :
                            hit.targetKind == AOGCombatTargetKind.Boss ? 0.80f :
                            hit.targetKind == AOGCombatTargetKind.NeutralMonster ? 0.65f : 0.55f;

        if (id == "lyra")
        {
            passiveStacks = Mathf.Min(4, passiveStacks + 1);
            if (passiveStacks >= 4)
            {
                passiveStacks = 0;
                DealBonusDamage(hit, stats.attackDamage * 0.42f * targetScale);
                GrantTemporaryRange(0.85f, 2.5f);
                SpawnProc(hit.target.transform.position, 1.1f);
            }
        }
        else if (id == "kaelith")
        {
            passiveStacks = Mathf.Min(5, passiveStacks + 1);
            if (passiveStacks >= 5 && Time.time >= nextPassiveProc)
            {
                passiveStacks = 0;
                nextPassiveProc = Time.time + 3.5f;
                DealBonusDamage(hit, stats.attackDamage * 0.30f * targetScale);
                float heal = hit.targetKind == AOGCombatTargetKind.Champion ? 34f : 16f;
                stats.hp = Mathf.Min(stats.maxHp, stats.hp + heal);
                SpawnProc(hit.target.transform.position, 1.35f);
            }
        }
        else if (id == "vesper")
        {
            passiveStacks = Mathf.Min(3, passiveStacks + 1);
            if (passiveStacks >= 3)
            {
                passiveStacks = 0;
                DealBonusDamage(hit, stats.attackDamage * 0.55f * targetScale);
                SpawnProc(hit.target.transform.position, 0.9f);
            }
        }
        else if (id == "auron")
        {
            if (Time.time >= nextPassiveProc)
            {
                nextPassiveProc = Time.time + 6f;
                float heal = hit.targetKind == AOGCombatTargetKind.Champion ? 45f : 24f;
                stats.hp = Mathf.Min(stats.maxHp, stats.hp + heal);
                SpawnProc(transform.position, 1.6f);
            }
        }
        else if (id == "nyra")
        {
            passiveStacks = Mathf.Min(6, passiveStacks + 1);
            if (passiveStacks >= 3 && Time.time >= nextPassiveProc)
            {
                passiveStacks -= 3;
                nextPassiveProc = Time.time + 3f;
                float heal = hit.targetKind == AOGCombatTargetKind.Champion ? 26f : 12f;
                stats.hp = Mathf.Min(stats.maxHp, stats.hp + heal);
                stats.moveSpeed += 0.35f;
                Invoke(nameof(RemoveNyraSpeed), 1.2f);
                SpawnProc(hit.target.transform.position, 1.0f);
            }
        }
        else if (id == "pyrelle")
        {
            passiveStacks = Mathf.Min(4, passiveStacks + 1);
            if (passiveStacks >= 4)
            {
                passiveStacks = 0;
                DealBonusDamage(hit, stats.attackDamage * 0.38f * targetScale);
                SpawnProc(hit.target.transform.position, 1.25f);
            }
        }
        else if (id == "selene")
        {
            if (Time.time >= nextPassiveProc)
            {
                nextPassiveProc = Time.time + 4.5f;
                DealBonusDamage(hit, stats.attackDamage * 0.22f * targetScale);
                SpawnProc(hit.target.transform.position, 1.15f);
            }
        }
    }

    private bool SourceBelongsToThisChampion(GameObject source)
    {
        if (source == null) return false;
        if (source == gameObject || source.transform.IsChildOf(transform)) return true;
        AOGCharacterStats parent = source.GetComponentInParent<AOGCharacterStats>();
        return parent != null && parent == stats;
    }

    private void DealBonusDamage(AOGCombatHitEvent hit, float amount)
    {
        if (hit.target == null || amount <= 0f) return;
        if (hit.targetKind == AOGCombatTargetKind.Champion)
        {
            AOGCharacterStats target = hit.target.GetComponentInParent<AOGCharacterStats>();
            target?.TakeDamage(amount, gameObject);
        }
        else if (hit.targetKind == AOGCombatTargetKind.Minion)
        {
            Minion target = hit.target.GetComponentInParent<Minion>();
            if (target != null) target.TakeDamage(amount, gameObject);
        }
        else if (hit.targetKind == AOGCombatTargetKind.NeutralMonster)
        {
            AOGNeutralMonsterRuntime target = hit.target.GetComponentInParent<AOGNeutralMonsterRuntime>();
            target?.TakeDamage(amount, gameObject);
        }
        else if (hit.targetKind == AOGCombatTargetKind.Boss)
        {
            AOGNeutralBossAI target = hit.target.GetComponentInParent<AOGNeutralBossAI>();
            target?.TakeDamage(amount, gameObject);
        }
    }

    private void GrantTemporaryRange(float bonus, float duration)
    {
        RemoveTemporaryRangeBonus();
        temporaryRangeBonus = bonus;
        stats.attackRange += bonus;
        temporaryRangeEnd = Time.time + duration;
    }

    private void RemoveTemporaryRangeBonus()
    {
        if (stats != null && temporaryRangeBonus > 0f)
            stats.attackRange = Mathf.Max(0.5f, stats.attackRange - temporaryRangeBonus);
        temporaryRangeBonus = 0f;
    }

    private void RemoveNyraSpeed()
    {
        if (stats != null)
            stats.moveSpeed = Mathf.Max(1f, stats.moveSpeed - 0.35f);
    }

    private void AddMaxHealth(float amount)
    {
        stats.maxHp += amount;
        stats.hp += amount;
    }

    private void SpawnProc(Vector3 position, float radius)
    {
        presentation?.SpawnImpactVfx(position + Vector3.up * 0.7f);
        GameObject ring = AOGAbilityVisuals.CreateRing(
            champion.displayName + "_Passive_Proc",
            position + Vector3.up * 0.05f,
            radius,
            champion.accentColor,
            0.08f);
        Destroy(ring, 0.32f);
    }
}

public class AOGChampionIdentityProgressionBootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindFirstObjectByType<AOGChampionIdentityProgressionBootstrap>() != null)
            return;
        GameObject host = new GameObject("AOG_Champion_Identity_Progression_Bootstrap");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGChampionIdentityProgressionBootstrap>();
    }

    private float nextScan;
    private void Update()
    {
        if (Time.unscaledTime < nextScan) return;
        nextScan = Time.unscaledTime + 0.75f;
        foreach (AOGActiveChampion hero in FindObjectsByType<AOGActiveChampion>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (hero != null && hero.GetComponent<AOGChampionIdentityProgressionRuntime>() == null)
                hero.gameObject.AddComponent<AOGChampionIdentityProgressionRuntime>();
        }
    }
}
