using UnityEngine;

public class AOGChampionVisualApplier : MonoBehaviour
{
    private const string AuraName = "AOG_Selected_Champion_Aura";
    private const string LightName = "AOG_Selected_Champion_Light";

    [SerializeField] private string championId;
    [SerializeField] private Color accent = Color.white;

    private ParticleSystem aura;
    private Light accentLight;

    public static GameObject FindPlayerObject()
    {
        GameObject tagged = GameObject.FindGameObjectWithTag("Player");
        if (tagged != null)
            return tagged;

        AOGPlayerMOBAController aogController = Object.FindAnyObjectByType<AOGPlayerMOBAController>();
        if (aogController != null)
            return aogController.gameObject;

        ChampionController championController = Object.FindAnyObjectByType<ChampionController>();
        if (championController != null)
            return championController.gameObject;

        AOGCharacterStats stats = Object.FindAnyObjectByType<AOGCharacterStats>();
        if (stats != null)
            return stats.gameObject;

        Champion champion = Object.FindAnyObjectByType<Champion>();
        if (champion != null)
            return champion.gameObject;

        return null;
    }

    public static void ApplyToCurrentPlayer(AOGChampionDefinition champion)
    {
        GameObject player = FindPlayerObject();
        if (player == null || champion == null)
            return;

        AOGCharacterStats stats = player.GetComponent<AOGCharacterStats>();
        if (stats == null)
            stats = player.AddComponent<AOGCharacterStats>();

        stats.maxHp = champion.maxHp;
        stats.hp = Mathf.Clamp(stats.hp <= 0f ? champion.maxHp : stats.hp, 1f, champion.maxHp);
        stats.attackDamage = champion.attackDamage;
        stats.attackRange = champion.attackRange;
        stats.moveSpeed = champion.moveSpeed;

        if (player.GetComponent<AOGPlayerMOBAController>() == null)
            player.AddComponent<AOGPlayerMOBAController>();

        AOGChampionVisualApplier visual = player.GetComponent<AOGChampionVisualApplier>();
        if (visual == null)
            visual = player.AddComponent<AOGChampionVisualApplier>();

        visual.Apply(champion);
    }

    public void Apply(AOGChampionDefinition champion)
    {
        if (champion == null)
            return;

        championId = champion.id;
        accent = champion.accent;

        ApplyRendererAccent();
        BuildAura();
    }

    private void Start()
    {
        AOGChampionDefinition selected = AOGChampionCatalog.GetById(championId);
        if (selected == null)
            selected = AOGChampionCatalog.GetSelectedOrDefault();

        Apply(selected);
    }

    private void ApplyRendererAccent()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || renderer.name.Contains("AOG_"))
                continue;

            Material[] materials = renderer.materials;
            for (int i = 0; i < materials.Length; i++)
            {
                Material mat = materials[i];
                if (mat == null)
                    continue;

                Color current = mat.color;
                mat.color = Color.Lerp(current, accent, 0.18f);

                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", accent * 0.45f);
                }
            }
        }
    }

    private void BuildAura()
    {
        Transform oldAura = transform.Find(AuraName);
        if (oldAura != null)
            Destroy(oldAura.gameObject);

        GameObject auraObject = new GameObject(AuraName);
        auraObject.transform.SetParent(transform);
        auraObject.transform.localPosition = new Vector3(0f, 0.08f, 0f);

        aura = auraObject.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = aura.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.55f, 1.2f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.08f, 0.22f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.22f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(accent.r, accent.g, accent.b, 0.25f), new Color(accent.r, accent.g, accent.b, 0.75f));
        main.maxParticles = 48;

        ParticleSystem.EmissionModule emission = aura.emission;
        emission.rateOverTime = 18f;

        ParticleSystem.ShapeModule shape = aura.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 1.45f;

        ParticleSystem.VelocityOverLifetimeModule velocity = aura.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.y = new ParticleSystem.MinMaxCurve(0.2f, 0.65f);

        Transform oldLight = transform.Find(LightName);
        if (oldLight != null)
            Destroy(oldLight.gameObject);

        GameObject lightObject = new GameObject(LightName);
        lightObject.transform.SetParent(transform);
        lightObject.transform.localPosition = new Vector3(0f, 2.4f, 0f);

        accentLight = lightObject.AddComponent<Light>();
        accentLight.type = LightType.Point;
        accentLight.color = accent;
        accentLight.intensity = 1.2f;
        accentLight.range = 7f;
        accentLight.shadows = LightShadows.None;
    }
}
