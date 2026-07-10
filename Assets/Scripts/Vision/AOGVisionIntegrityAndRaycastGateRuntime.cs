using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Keeps hidden enemy colliders out of the player's command raycast without disabling physics.
/// Hidden collider GameObjects are moved to Ignore Raycast layer; overlap/damage physics that use
/// explicit all-layer masks remain unaffected.
/// </summary>
public class AOGVisionRaycastGateRuntime : MonoBehaviour
{
    private readonly Dictionary<GameObject,int> originalLayers = new Dictionary<GameObject,int>();
    private AOGCharacterStats stats;
    private float nextCheck;
    private bool lastHidden;

    private void Awake()
    {
        stats = GetComponent<AOGCharacterStats>();
        CacheColliderLayers();
    }

    private void Update()
    {
        if (Time.unscaledTime < nextCheck) return;
        nextCheck = Time.unscaledTime + 0.10f;
        if (stats == null) return;

        MinionTeam viewer = AOGVisionAuthorityRuntime.PlayerTeam;
        bool hidden = stats.team != viewer && !AOGVisionAuthorityRuntime.IsVisibleToTeam(transform.position,viewer);
        if (hidden == lastHidden) return;
        lastHidden = hidden;
        ApplyRaycastState(hidden);
    }

    private void CacheColliderLayers()
    {
        foreach (Collider collider in GetComponentsInChildren<Collider>(true))
        {
            if (collider == null) continue;
            GameObject go = collider.gameObject;
            if (!originalLayers.ContainsKey(go)) originalLayers.Add(go,go.layer);
        }
    }

    private void ApplyRaycastState(bool hidden)
    {
        CacheColliderLayers();
        foreach (KeyValuePair<GameObject,int> pair in originalLayers)
        {
            if (pair.Key == null) continue;
            pair.Key.layer = hidden ? Physics.IgnoreRaycastLayer : pair.Value;
        }
    }

    private void OnDisable()
    {
        foreach (KeyValuePair<GameObject,int> pair in originalLayers)
            if (pair.Key != null) pair.Key.layer = pair.Value;
    }
}

/// <summary>
/// Enemy lane-minion presentation gate. Keeps simulation active while hiding renderer information and
/// command-raycast selection outside vision.
/// </summary>
public class AOGFogMinionPresentationRuntime : MonoBehaviour
{
    private readonly Dictionary<GameObject,int> originalLayers = new Dictionary<GameObject,int>();
    private Minion minion;
    private bool lastVisible = true;
    private float nextCheck;

    private void Awake()
    {
        minion = GetComponent<Minion>();
        CacheLayers();
    }

    private void Update()
    {
        if (Time.unscaledTime < nextCheck) return;
        nextCheck = Time.unscaledTime + 0.14f;
        if (minion == null) return;

        MinionTeam viewer = AOGVisionAuthorityRuntime.PlayerTeam;
        bool visible = minion.team == viewer || AOGVisionAuthorityRuntime.IsVisibleToTeam(transform.position,viewer);
        if (visible == lastVisible) return;
        lastVisible = visible;

        foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
            if (renderer != null) renderer.enabled = visible;

        CacheLayers();
        foreach (KeyValuePair<GameObject,int> pair in originalLayers)
            if (pair.Key != null) pair.Key.layer = visible ? pair.Value : Physics.IgnoreRaycastLayer;
    }

    private void CacheLayers()
    {
        foreach (Collider collider in GetComponentsInChildren<Collider>(true))
        {
            if (collider == null) continue;
            GameObject go = collider.gameObject;
            if (!originalLayers.ContainsKey(go)) originalLayers.Add(go,go.layer);
        }
    }

    private void OnDisable()
    {
        foreach (KeyValuePair<GameObject,int> pair in originalLayers)
            if (pair.Key != null) pair.Key.layer = pair.Value;
    }
}

/// <summary>
/// Central integrity manager for vision source lifecycle and local click gating.
/// </summary>
[DefaultExecutionOrder(-605)]
public class AOGVisionIntegrityBootstrap : MonoBehaviour
{
    private float nextFast;
    private float nextDiscovery;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindFirstObjectByType<AOGVisionIntegrityBootstrap>() != null) return;
        GameObject host = new GameObject("AOG_Vision_Integrity_Bootstrap");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGVisionIntegrityBootstrap>();
    }

    private void Update()
    {
        if (Time.unscaledTime >= nextFast)
        {
            nextFast = Time.unscaledTime + 0.25f;
            SyncWardSources();
            ConfigureHumanCommandMasks();
        }

        if (Time.unscaledTime < nextDiscovery) return;
        nextDiscovery = Time.unscaledTime + 1.0f;
        RefreshSourceLifecycle();
        AttachPresentationGates();
    }

    private static void SyncWardSources()
    {
        foreach (AOGWardRuntime ward in AOGVisionAuthorityRuntime.ActiveWards)
        {
            if (ward == null) continue;
            AOGVisionSourceRuntime source = ward.GetComponent<AOGVisionSourceRuntime>();
            if (source == null) continue;
            source.team = ward.team;
            source.radius = ward.visionRadius;
            if (!source.enabled) source.enabled = true;
        }
    }

    private static void ConfigureHumanCommandMasks()
    {
        AOGActiveChampion player = AOGPlayerChampionAuthority.CurrentChampion;
        if (player == null) return;
        AOGUnifiedMobaInputDriver driver = player.GetComponent<AOGUnifiedMobaInputDriver>();
        if (driver != null)
            driver.commandMask &= ~(1 << Physics.IgnoreRaycastLayer);
    }

    private static void RefreshSourceLifecycle()
    {
        foreach (AOGVisionSourceRuntime source in FindObjectsByType<AOGVisionSourceRuntime>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {
            if (source == null) continue;
            bool operational = true;

            AOGCharacterStats hero = source.GetComponent<AOGCharacterStats>();
            if (hero != null) operational = !hero.IsDead;

            TowerHealth tower = source.GetComponent<TowerHealth>();
            if (tower != null) operational = tower.hp > 0f;

            Minion minion = source.GetComponent<Minion>();
            if (minion != null) operational = minion.hp > 0f && minion.gameObject.activeInHierarchy;

            AOGWardRuntime ward = source.GetComponent<AOGWardRuntime>();
            if (ward != null) operational = ward.Remaining > 0f;

            if (source.enabled != operational) source.enabled = operational;
        }
    }

    private static void AttachPresentationGates()
    {
        foreach (AOGTeamMemberIdentity member in AOGWorldRegistry.TeamMembers)
        {
            if (member == null) continue;
            if (member.GetComponent<AOGVisionRaycastGateRuntime>() == null)
                member.gameObject.AddComponent<AOGVisionRaycastGateRuntime>();
        }

        foreach (Minion minion in FindObjectsByType<Minion>(FindObjectsInactive.Exclude,FindObjectsSortMode.None))
        {
            if (minion == null) continue;
            if (minion.GetComponent<AOGFogMinionPresentationRuntime>() == null)
                minion.gameObject.AddComponent<AOGFogMinionPresentationRuntime>();
        }
    }
}
