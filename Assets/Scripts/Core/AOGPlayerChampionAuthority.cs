using UnityEngine;

public enum AOGRole
{
    Top,
    Jungle,
    Mid,
    ADC,
    Support
}

[DefaultExecutionOrder(-1400)]
public class AOGPlayerChampionAuthority : MonoBehaviour
{
    public static AOGPlayerChampionAuthority Instance { get; private set; }
    public static AOGActiveChampion CurrentChampion => Instance != null ? Instance.currentChampion : null;

    [SerializeField] private AOGActiveChampion currentChampion;
    [SerializeField] private AOGRole currentRole = AOGRole.Mid;

    public AOGRole CurrentRole => currentRole;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        if (Instance != null)
            return;

        AOGPlayerChampionAuthority existing = FindFirstObjectByType<AOGPlayerChampionAuthority>(FindObjectsInactive.Include);
        if (existing != null)
        {
            Instance = existing;
            return;
        }

        GameObject host = new GameObject("AOG_Player_Champion_Authority");
        Instance = host.AddComponent<AOGPlayerChampionAuthority>();
        DontDestroyOnLoad(host);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void RegisterPlayerChampion(AOGActiveChampion selected, AOGRole role)
    {
        if (selected == null)
            return;

        currentRole = role;

        AOGActiveChampion[] candidates = FindObjectsByType<AOGActiveChampion>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (AOGActiveChampion candidate in candidates)
        {
            if (candidate == null)
                continue;

            bool isSelected = candidate == selected;
            candidate.gameObject.SetActive(true);
            candidate.SetActiveChampion(isSelected);

            AOGUnifiedMobaInputDriver input = candidate.GetComponent<AOGUnifiedMobaInputDriver>();
            if (isSelected && input == null)
                input = candidate.gameObject.AddComponent<AOGUnifiedMobaInputDriver>();
            if (input != null)
                input.enabled = isSelected;

            AOGPlayerMOBAController legacy = candidate.GetComponent<AOGPlayerMOBAController>();
            if (legacy != null)
                legacy.enabled = false;

            if (!isSelected)
                candidate.gameObject.SetActive(false);
        }

        selected.gameObject.SetActive(true);
        AOGCharacterStats stats = selected.GetComponent<AOGCharacterStats>();
        if (stats != null)
        {
            stats.team = MinionTeam.Blue;
            stats.hp = Mathf.Max(1f, stats.maxHp);
        }

        MoveToBlueRoleSpawn(selected.transform, role);
        selected.SetActiveChampion(true);
        currentChampion = selected;

        EnsureSinglePlayerCombatDriver(selected);

        Camera camera = Camera.main;
        if (camera != null)
        {
            AOGMobaCameraController controller = camera.GetComponent<AOGMobaCameraController>();
            if (controller != null)
                controller.SetTarget(selected.transform, true);
        }

        FindFirstObjectByType<AOGDynamicChampionHudBinder>()?.Bind(selected);

        AOGPlayerEconomy economy = selected.GetComponent<AOGPlayerEconomy>();
        if (economy != null)
            AOGShopRuntime.Instance?.Bind(economy);
    }

    private void LateUpdate()
    {
        if (currentChampion == null || !currentChampion.gameObject.activeInHierarchy)
            return;

        EnsureSinglePlayerCombatDriver(currentChampion);

        Camera camera = Camera.main;
        if (camera == null)
            return;

        AOGMobaCameraController controller = camera.GetComponent<AOGMobaCameraController>();
        if (controller != null && controller.target != currentChampion.transform)
            controller.SetTarget(currentChampion.transform, false);
    }

    private static void EnsureSinglePlayerCombatDriver(AOGActiveChampion selected)
    {
        if (selected == null)
            return;

        AOGUnifiedMobaInputDriver unified = selected.GetComponent<AOGUnifiedMobaInputDriver>();
        if (unified == null)
            unified = selected.gameObject.AddComponent<AOGUnifiedMobaInputDriver>();
        unified.enabled = true;

        AOGEnemyTargetSelectionRuntime legacyTargeting = selected.GetComponent<AOGEnemyTargetSelectionRuntime>();
        if (legacyTargeting != null)
            legacyTargeting.enabled = false;

        AOGPassiveLaneAutoAttackRuntime passiveAutoAttack = selected.GetComponent<AOGPassiveLaneAutoAttackRuntime>();
        if (passiveAutoAttack != null)
            passiveAutoAttack.enabled = false;

        PlayerAutoAttack oldAuto = selected.GetComponent<PlayerAutoAttack>();
        if (oldAuto != null)
            oldAuto.enabled = false;

        PlayerAttack oldAttack = selected.GetComponent<PlayerAttack>();
        if (oldAttack != null)
            oldAttack.enabled = false;
    }

    private static void MoveToBlueRoleSpawn(Transform champion, AOGRole role)
    {
        Transform spawn = FindNamedTransform("Blue_" + role + "_Spawn");
        if (spawn == null)
            spawn = FindNamedTransform("BluePlayerSpawn", "BlueBaseSpawn", "Blue_Spawn", "BlueSpawn");

        if (spawn == null)
            return;

        Vector3 roleOffset = role switch
        {
            AOGRole.Top => new Vector3(-2.5f, 0.2f, 2.5f),
            AOGRole.Jungle => new Vector3(-1.2f, 0.2f, 2.2f),
            AOGRole.Mid => new Vector3(0f, 0.2f, 2.8f),
            AOGRole.ADC => new Vector3(1.4f, 0.2f, 2.2f),
            _ => new Vector3(2.6f, 0.2f, 2.5f)
        };

        champion.position = spawn.position + roleOffset;
        champion.rotation = spawn.rotation;
    }

    private static Transform FindNamedTransform(params string[] names)
    {
        foreach (GameObject obj in FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (obj == null)
                continue;

            foreach (string name in names)
                if (string.Equals(obj.name, name, System.StringComparison.OrdinalIgnoreCase))
                    return obj.transform;
        }

        return null;
    }
}
