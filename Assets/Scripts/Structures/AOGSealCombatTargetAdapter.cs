using UnityEngine;

/// <summary>
/// Adapts strategic seals to the existing TowerHealth targeting path without adding a
/// second player attack system. TowerHealth is used only as a click/damage proxy; the
/// authoritative HP and lifecycle remain on AOGStrategicLaneSeal.
/// </summary>
public class AOGSealCombatTargetAdapter : MonoBehaviour
{
    private const float ProxyHp = 100000000f;
    private AOGStrategicLaneSeal seal;
    private TowerHealth proxy;
    private float lastProxyHp;

    private void Awake()
    {
        seal = GetComponent<AOGStrategicLaneSeal>();
        proxy = GetComponent<TowerHealth>();
        if (proxy == null) proxy = gameObject.AddComponent<TowerHealth>();
        if (seal != null) proxy.towerTeam = seal.team;
        proxy.maxHp = ProxyHp;
        proxy.hp = ProxyHp;
        proxy.destroyOnDeath = false;
        lastProxyHp = ProxyHp;
    }

    private void Update()
    {
        if (seal == null || proxy == null) return;

        if (seal.State != AOGSealState.Active)
        {
            proxy.hp = ProxyHp;
            lastProxyHp = ProxyHp;
            return;
        }

        if (proxy.hp < lastProxyHp)
        {
            float delta = lastProxyHp - proxy.hp;
            seal.TakeDamage(delta);
            proxy.hp = ProxyHp;
            lastProxyHp = ProxyHp;
        }
        else
        {
            lastProxyHp = proxy.hp;
        }
    }
}

public class AOGSealCombatTargetBootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindFirstObjectByType<AOGSealCombatTargetBootstrap>() != null) return;
        GameObject host = new GameObject("AOG_Seal_Combat_Target_Bootstrap");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGSealCombatTargetBootstrap>();
    }

    private float nextScan;
    private void Update()
    {
        if (Time.unscaledTime < nextScan) return;
        nextScan = Time.unscaledTime + 0.6f;
        foreach (AOGStrategicLaneSeal seal in FindObjectsByType<AOGStrategicLaneSeal>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            if (seal != null && seal.GetComponent<AOGSealCombatTargetAdapter>() == null)
                seal.gameObject.AddComponent<AOGSealCombatTargetAdapter>();
    }
}
