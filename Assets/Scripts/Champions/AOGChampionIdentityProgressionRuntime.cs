using UnityEngine;

/// <summary>
/// Adds per-champion combat identity without replacing the existing Q/W/E/R kits.
/// The runtime watches progression ranks and applies stable, one-time deltas so
/// leveling abilities changes the champion outside of cooldown text alone.
/// </summary>
[DefaultExecutionOrder(80)]
public class AOGChampionIdentityProgressionRuntime : MonoBehaviour, IChampionBasicAttackModifier
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

    private void Awake()
    {
        champion = GetComponent<AOGActiveChampion>();
        stats = GetComponent<AOGCharacterStats>();
        progression = GetComponent<AOGChampionProgression>();
        presentation = GetComponent<ChampionPresentationController>();
    }

    private void Update()
    {
        if (champion == null || !champion.IsActiveChampion || stats == null || progression == null)
            return;

        ApplyNewRanks();
    }

    private void ApplyNewRanks()
    {
        while (appliedQ < progression.qRank)
        {
            appliedQ++;
            ApplyRankDelta(0);
        }
        while (appliedW < progression.wRank)
        {
            appliedW++;
            ApplyRankDelta(1);
        }
        while (appliedE < progression.eRank)
        {
            appliedE++;
            ApplyRankDelta(2);
        }
        while (appliedR < progression.rRank)
        {
            appliedR++;
            ApplyRankDelta(3);
        }
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

    public void OnBasicAttackHit(Minion target)
    {
        if (target == null || champion == null || stats == null)
            return;

        string id = champion.championId.ToLowerInvariant();
        if (id == "lyra")
        {
            passiveStacks = Mathf.Min(4, passiveStacks + 1);
            if (passiveStacks >= 4)
            {
                passiveStacks = 0;
                target.TakeDamage(stats.attackDamage * 0.42f, gameObject);
                SpawnProc(target.transform.position, 1.1f);
            }
        }
        else if (id == "kaelith")
        {
            if (Time.time >= nextPassiveProc)
            {
                nextPassiveProc = Time.time + 5f;
                target.TakeDamage(stats.attackDamage * 0.30f, gameObject);
                AddMaxHealthTemporaryHeal(22f);
                SpawnProc(target.transform.position, 1.35f);
            }
        }
        else if (id == "vesper")
        {
            passiveStacks = Mathf.Min(3, passiveStacks + 1);
            if (passiveStacks == 3)
            {
                passiveStacks = 0;
                target.TakeDamage(stats.attackDamage * 0.55f, gameObject);
                SpawnProc(target.transform.position, 0.9f);
            }
        }
        else if (id == "auron")
        {
            if (Time.time >= nextPassiveProc)
            {
                nextPassiveProc = Time.time + 6f;
                stats.hp = Mathf.Min(stats.maxHp, stats.hp + 30f);
                SpawnProc(transform.position, 1.6f);
            }
        }
        else if (id == "nyra" || id == "pyrelle" || id == "selene")
        {
            if (Time.time >= nextPassiveProc)
            {
                nextPassiveProc = Time.time + 4.5f;
                target.TakeDamage(stats.attackDamage * 0.24f, gameObject);
                SpawnProc(target.transform.position, 1.0f);
            }
        }
    }

    private void AddMaxHealth(float amount)
    {
        stats.maxHp += amount;
        stats.hp += amount;
    }

    private void AddMaxHealthTemporaryHeal(float amount)
    {
        stats.hp = Mathf.Min(stats.maxHp, stats.hp + amount);
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

/// <summary>
/// Keeps the identity-progression layer attached only to actual selectable champions.
/// </summary>
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

    private void Update()
    {
        foreach (AOGActiveChampion hero in FindObjectsByType<AOGActiveChampion>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (hero != null && hero.GetComponent<AOGChampionIdentityProgressionRuntime>() == null)
                hero.gameObject.AddComponent<AOGChampionIdentityProgressionRuntime>();
        }
    }
}
