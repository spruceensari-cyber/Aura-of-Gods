using UnityEngine;

public class AOGHeroIdentityPassiveRuntime : MonoBehaviour
{
    public string passiveName;
    [TextArea] public string passiveDescription;

    private AOGCharacterStats stats;
    private AOGActiveChampion identity;
    private bool applied;
    private float baseDamage;
    private float pyrelleBonus;

    private void Awake()
    {
        stats = GetComponent<AOGCharacterStats>();
        identity = GetComponent<AOGActiveChampion>();
    }

    private void Start()
    {
        ApplyIdentityOnce();
    }

    private void Update()
    {
        if (!applied)
            ApplyIdentityOnce();

        if (stats == null || identity == null)
            return;

        if (identity.championId == "pyrelle")
        {
            float desiredBonus = stats.hp / Mathf.Max(1f, stats.maxHp) < 0.5f ? 22f : 0f;
            if (!Mathf.Approximately(desiredBonus, pyrelleBonus))
            {
                stats.attackDamage += desiredBonus - pyrelleBonus;
                pyrelleBonus = desiredBonus;
            }
        }
    }

    private void ApplyIdentityOnce()
    {
        if (applied || stats == null || identity == null)
            return;

        applied = true;
        baseDamage = stats.attackDamage;

        switch (identity.championId)
        {
            case "lyra":
                passiveName = "Moon Precision";
                passiveDescription = "Longer attack reach and faster precision attacks.";
                stats.attackRange += 0.8f;
                stats.attackCooldown = Mathf.Max(0.45f, stats.attackCooldown * 0.93f);
                break;

            case "kaelith":
                passiveName = "Eclipse Hunger";
                passiveDescription = "Higher melee damage and slightly faster pursuit.";
                stats.attackDamage += 15f;
                stats.moveSpeed += 0.25f;
                break;

            case "auron":
                passiveName = "Solar Bulwark";
                passiveDescription = "Heavy frontline durability with deliberate melee pressure.";
                AddMaxHealth(260f);
                stats.attackRange = Mathf.Min(stats.attackRange, 2.8f);
                break;

            case "vesper":
                passiveName = "Void Tempo";
                passiveDescription = "Extended range and rapid sustained attacks.";
                stats.attackRange += 1.0f;
                stats.attackCooldown = Mathf.Max(0.42f, stats.attackCooldown * 0.88f);
                break;

            case "nyra":
                passiveName = "Spirit Momentum";
                passiveDescription = "Exceptional movement and fluid spell weaving.";
                stats.moveSpeed += 0.65f;
                stats.attackCooldown = Mathf.Max(0.44f, stats.attackCooldown * 0.92f);
                break;

            case "pyrelle":
                passiveName = "Burning Crown";
                passiveDescription = "Gains bonus attack power while below half health.";
                stats.attackDamage += 10f;
                break;

            case "selene":
                passiveName = "Astral Distance";
                passiveDescription = "Superior spellcaster attack range and safe positioning.";
                stats.attackRange += 1.25f;
                stats.moveSpeed += 0.15f;
                break;

            default:
                passiveName = "Aether Affinity";
                passiveDescription = "Balanced combat growth through Aether resonance.";
                break;
        }
    }

    private void AddMaxHealth(float amount)
    {
        float oldMax = stats.maxHp;
        stats.maxHp += amount;
        stats.hp += stats.maxHp - oldMax;
    }
}

public class AOGHeroIdentityPassiveBootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        GameObject host = new GameObject("AOG_Hero_Identity_Passive_Bootstrap");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGHeroIdentityPassiveBootstrap>();
    }

    private void Update()
    {
        foreach (AOGActiveChampion champion in FindObjectsByType<AOGActiveChampion>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (champion != null && champion.GetComponent<AOGHeroIdentityPassiveRuntime>() == null)
                champion.gameObject.AddComponent<AOGHeroIdentityPassiveRuntime>();
        }
    }
}
