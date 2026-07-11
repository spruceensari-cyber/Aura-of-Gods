using System.Collections;
using UnityEngine;

/// <summary>
/// Repairs targetability and world bars for runtime-created minions, structures and bosses.
/// It does not replace combat authority; AOGUnifiedMobaInputDriver and existing health components remain authoritative.
/// </summary>
[DefaultExecutionOrder(16420)]
public class AOGCombatInteractionRepairRuntime : MonoBehaviour
{
    private float nextRefresh;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindFirstObjectByType<AOGCombatInteractionRepairRuntime>() != null) return;
        GameObject host = new GameObject("AOG_Combat_Interaction_Repair_Runtime");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGCombatInteractionRepairRuntime>();
    }

    private IEnumerator Start()
    {
        yield return new WaitForSecondsRealtime(0.8f);
        RefreshAll();
    }

    private void Update()
    {
        if (Time.unscaledTime < nextRefresh) return;
        nextRefresh = Time.unscaledTime + 0.75f;
        RefreshAll();
    }

    private static void RefreshAll()
    {
        foreach (Minion minion in Minion.Active)
            ConfigureMinion(minion);

        foreach (TowerHealth tower in AOGWorldRegistry.Towers)
            ConfigureTower(tower);

        foreach (AOGNeutralBossAI boss in AOGWorldRegistry.Bosses)
            ConfigureBoss(boss);

        foreach (AOGNexusCore nexus in AOGWorldRegistry.Nexuses)
            ConfigureNexus(nexus);

        AOGActiveChampion active = AOGPlayerChampionAuthority.CurrentChampion;
        if (active != null)
        {
            AOGUnifiedMobaInputDriver driver = active.GetComponent<AOGUnifiedMobaInputDriver>();
            if (driver == null) driver = active.gameObject.AddComponent<AOGUnifiedMobaInputDriver>();
            driver.leftClickAlsoMoves = false;
            driver.commandMask = ~0;
        }
    }

    private static void ConfigureMinion(Minion minion)
    {
        if (minion == null || minion.hp <= 0f) return;

        CapsuleCollider capsule = minion.GetComponent<CapsuleCollider>();
        if (capsule == null) capsule = minion.gameObject.AddComponent<CapsuleCollider>();
        capsule.enabled = true;
        capsule.isTrigger = false;
        capsule.center = new Vector3(0f, minion.role == MinionRole.Cannon ? 0.85f : 1.0f, 0f);
        capsule.height = minion.role == MinionRole.Cannon ? 1.8f : 2.1f;
        capsule.radius = minion.role == MinionRole.Cannon ? 0.86f : 0.56f;

        AOGWorldHealthBar bar = minion.GetComponent<AOGWorldHealthBar>();
        if (bar == null) bar = minion.gameObject.AddComponent<AOGWorldHealthBar>();
        bar.enabled = true;
        bar.barOffset = new Vector3(0f, minion.role == MinionRole.Cannon ? 2.5f : 2.2f, 0f);
        bar.barWidth = minion.role == MinionRole.Cannon ? 1.55f : 1.18f;
        bar.barHeight = 0.12f;
        bar.Refresh();
    }

    private static void ConfigureTower(TowerHealth tower)
    {
        if (tower == null || tower.hp <= 0f) return;
        Collider collider = tower.GetComponent<Collider>();
        if (collider == null)
        {
            BoxCollider box = tower.gameObject.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 2.4f, 0f);
            box.size = new Vector3(3.4f, 5.0f, 3.4f);
        }
        else collider.enabled = true;

        AOGObjectiveWorldBar bar = tower.GetComponent<AOGObjectiveWorldBar>();
        if (bar == null) bar = tower.gameObject.AddComponent<AOGObjectiveWorldBar>();
        bar.enabled = true;
        bar.offset = new Vector3(0f, 7.5f, 0f);
        bar.width = 4.2f;
        bar.height = 0.30f;
    }

    private static void ConfigureBoss(AOGNeutralBossAI boss)
    {
        if (boss == null || boss.IsDead) return;
        CapsuleCollider capsule = boss.GetComponent<CapsuleCollider>();
        if (capsule == null) capsule = boss.gameObject.AddComponent<CapsuleCollider>();
        capsule.enabled = true;
        capsule.isTrigger = false;
        capsule.center = new Vector3(0f, 2.5f, 0f);
        capsule.height = boss.bossType == AOGNeutralBossType.Dragon ? 6.5f : 5.5f;
        capsule.radius = boss.bossType == AOGNeutralBossType.Dragon ? 2.8f : 2.3f;

        AOGObjectiveWorldBar bar = boss.GetComponent<AOGObjectiveWorldBar>();
        if (bar == null) bar = boss.gameObject.AddComponent<AOGObjectiveWorldBar>();
        bar.enabled = true;
        bar.offset = new Vector3(0f, boss.bossType == AOGNeutralBossType.Dragon ? 7.8f : 6.8f, 0f);
        bar.width = 5.6f;
        bar.height = 0.36f;
    }

    private static void ConfigureNexus(AOGNexusCore nexus)
    {
        if (nexus == null || nexus.IsDestroyed) return;
        Collider collider = nexus.GetComponent<Collider>();
        if (collider != null) collider.enabled = true;

        AOGObjectiveWorldBar bar = nexus.GetComponent<AOGObjectiveWorldBar>();
        if (bar == null) bar = nexus.gameObject.AddComponent<AOGObjectiveWorldBar>();
        bar.enabled = true;
        bar.offset = new Vector3(0f, 7.2f, 0f);
        bar.width = 5.2f;
        bar.height = 0.34f;
    }
}
