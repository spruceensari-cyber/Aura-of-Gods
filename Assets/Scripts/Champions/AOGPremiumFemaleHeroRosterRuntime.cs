using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public enum AOGPremiumMageType
{
    NyraSpiritVixen,
    PyrelleFlameSovereign,
    SeleneAstralOracle
}

public class AOGPremiumMageSkillSet : MonoBehaviour, IAOGAbilityCooldownProvider
{
    public AOGPremiumMageType mageType;

    private AOGCharacterStats stats;
    private ChampionPresentationController presentation;
    private float nextQ;
    private float nextW;
    private float nextE;
    private float nextR;

    public string ChampionDisplayName => mageType == AOGPremiumMageType.NyraSpiritVixen ? "NYRA" : mageType == AOGPremiumMageType.PyrelleFlameSovereign ? "PYRELLE" : "SELENE";
    public string ChampionRoleName => mageType == AOGPremiumMageType.NyraSpiritVixen ? "SPIRIT VIXEN" : mageType == AOGPremiumMageType.PyrelleFlameSovereign ? "FLAME SOVEREIGN" : "ASTRAL ORACLE";

    private void Awake()
    {
        stats = GetComponent<AOGCharacterStats>();
        presentation = GetComponent<ChampionPresentationController>();
    }

    private void Update()
    {
        AOGActiveChampion active = GetComponent<AOGActiveChampion>();
        if (active != null && !active.IsActiveChampion)
            return;

        if (AOGInputBridge.KeyPressedThisFrame(KeyCode.Q)) CastQ();
        if (AOGInputBridge.KeyPressedThisFrame(KeyCode.W)) CastW();
        if (AOGInputBridge.KeyPressedThisFrame(KeyCode.E)) CastE();
        if (AOGInputBridge.KeyPressedThisFrame(KeyCode.R)) CastR();
    }

    public void CastQ()
    {
        if (Time.time < nextQ) return;
        nextQ = Time.time + 5f;
        presentation?.PlayAbility(0);

        if (mageType == AOGPremiumMageType.NyraSpiritVixen)
            FireProjectile(145f, 19f, 18f, new Color(0.92f, 0.28f, 0.72f), AOGSkillProjectile.Shape.Orb, 1);
        else if (mageType == AOGPremiumMageType.PyrelleFlameSovereign)
            FireProjectile(170f, 17f, 16f, new Color(1f, 0.24f, 0.035f), AOGSkillProjectile.Shape.Orb, 0);
        else
            FireProjectile(135f, 22f, 20f, new Color(0.42f, 0.74f, 1f), AOGSkillProjectile.Shape.Lance, 2);
    }

    public void CastW()
    {
        if (Time.time < nextW) return;
        nextW = Time.time + 10f;
        presentation?.PlayAbility(1);

        if (mageType == AOGPremiumMageType.NyraSpiritVixen)
        {
            DamageRadius(transform.position, 4.2f, 95f);
            GameObject ring = AOGAbilityVisuals.CreateRing("Nyra_Charm_Pulse", transform.position, 4.2f, new Color(1f, 0.34f, 0.74f), 0.12f);
            Destroy(ring, 0.7f);
        }
        else if (mageType == AOGPremiumMageType.PyrelleFlameSovereign)
        {
            Vector3 start = transform.position + transform.forward * 1.2f + Vector3.up * 0.1f;
            Vector3 end = transform.position + transform.forward * 11f + Vector3.up * 0.1f;
            GameObject beam = AOGAbilityVisuals.CreateBeam("Pyrelle_Inferno_Line", start, end, new Color(1f, 0.18f, 0.02f), 0.65f);
            Destroy(beam, 0.55f);
            DamageLine(start, end, 150f, 1.3f);
        }
        else
        {
            AOGPersistentDamageZone zone = new GameObject("Selene_Moon_Field").AddComponent<AOGPersistentDamageZone>();
            zone.transform.position = transform.position + transform.forward * 4f;
            zone.owner = gameObject;
            zone.team = stats.team;
            zone.radius = 4.5f;
            zone.duration = 4f;
            zone.damagePerTick = 32f;
            zone.tickRate = 0.65f;
            zone.color = new Color(0.22f, 0.42f, 0.88f, 0.62f);
            zone.BuildVisual();
        }
    }

    public void CastE()
    {
        if (Time.time < nextE) return;
        nextE = Time.time + 8f;
        presentation?.PlayAbility(2);
        StartCoroutine(DashRoutine());
    }

    public void CastR()
    {
        if (Time.time < nextR) return;
        nextR = Time.time + 48f;
        presentation?.PlayAbility(3);

        float radius = mageType == AOGPremiumMageType.PyrelleFlameSovereign ? 8f : 9f;
        float damage = mageType == AOGPremiumMageType.PyrelleFlameSovereign ? 430f : mageType == AOGPremiumMageType.NyraSpiritVixen ? 360f : 390f;
        Color color = AccentColor();
        DamageRadius(transform.position, radius, damage);
        GameObject ring = AOGAbilityVisuals.CreateRing(ChampionDisplayName + "_Ultimate", transform.position, radius, color, 0.22f);
        Destroy(ring, 1.0f);
        presentation?.SpawnAbilityImpactVfx(transform.position + Vector3.up * 0.8f, 3);
        Camera.main?.GetComponent<AOGMobaCameraController>()?.AddRandomImpulse(0.46f);
    }

    private IEnumerator DashRoutine()
    {
        float distance = mageType == AOGPremiumMageType.NyraSpiritVixen ? 7.5f : mageType == AOGPremiumMageType.PyrelleFlameSovereign ? 6.5f : 8f;
        Vector3 start = transform.position;
        Vector3 end = start + transform.forward * distance;
        GameObject trail = AOGAbilityVisuals.CreateBeam(ChampionDisplayName + "_Dash_Trail", start + Vector3.up * 0.3f, end + Vector3.up * 0.3f, AccentColor(), 0.42f);

        float elapsed = 0f;
        while (elapsed < 0.24f)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / 0.24f);
            transform.position = Vector3.Lerp(start, end, 1f - Mathf.Pow(1f - t, 3f));
            yield return null;
        }

        transform.position = end;
        DamageRadius(end, 2.4f, mageType == AOGPremiumMageType.PyrelleFlameSovereign ? 125f : 90f);
        if (trail != null) Destroy(trail, 0.35f);
    }

    private void FireProjectile(float damage, float speed, float range, Color color, AOGSkillProjectile.Shape shape, int pierce)
    {
        GameObject projectile = new GameObject(ChampionDisplayName + "_Q_Projectile");
        projectile.transform.position = transform.position + Vector3.up * 1.35f + transform.forward * 0.9f;
        AOGSkillProjectile skill = projectile.AddComponent<AOGSkillProjectile>();
        skill.owner = gameObject;
        skill.team = stats.team;
        skill.direction = transform.forward;
        skill.speed = speed;
        skill.range = range;
        skill.damage = damage;
        skill.radius = 0.38f;
        skill.color = color;
        skill.pierceCount = pierce;
        skill.BuildVisual(shape);
    }

    private void DamageRadius(Vector3 center, float radius, float damage)
    {
        foreach (Collider hit in Physics.OverlapSphere(center, radius, ~0, QueryTriggerInteraction.Ignore))
            DamageCollider(hit, damage);
    }

    private void DamageLine(Vector3 start, Vector3 end, float damage, float radius)
    {
        Vector3 direction = end - start;
        RaycastHit[] hits = Physics.SphereCastAll(start, radius, direction.normalized, direction.magnitude, ~0, QueryTriggerInteraction.Ignore);
        foreach (RaycastHit hit in hits)
            DamageCollider(hit.collider, damage);
    }

    private void DamageCollider(Collider hit, float damage)
    {
        if (hit == null || stats == null) return;
        Minion minion = hit.GetComponentInParent<Minion>();
        if (minion != null && minion.team != stats.team) { minion.TakeDamage(damage, gameObject); return; }
        AOGNeutralBossAI boss = hit.GetComponentInParent<AOGNeutralBossAI>();
        if (boss != null) { boss.TakeDamage(damage, gameObject); return; }
        TowerHealth tower = hit.GetComponentInParent<TowerHealth>();
        if (tower != null && tower.towerTeam != stats.team) tower.TakeDamage(damage * 0.35f);
    }

    private Color AccentColor()
    {
        return mageType == AOGPremiumMageType.NyraSpiritVixen
            ? new Color(0.94f, 0.28f, 0.74f)
            : mageType == AOGPremiumMageType.PyrelleFlameSovereign
                ? new Color(1f, 0.22f, 0.035f)
                : new Color(0.36f, 0.68f, 1f);
    }

    public string GetAbilityName(int slot)
    {
        if (mageType == AOGPremiumMageType.NyraSpiritVixen)
            return slot == 0 ? "SPIRIT ORB" : slot == 1 ? "HEART ECHO" : slot == 2 ? "VIXEN STEP" : "NINEFOLD REQUIEM";
        if (mageType == AOGPremiumMageType.PyrelleFlameSovereign)
            return slot == 0 ? "FLAME ORB" : slot == 1 ? "INFERNO LINE" : slot == 2 ? "PHOENIX DASH" : "SOLAR CATASTROPHE";
        return slot == 0 ? "STAR LANCE" : slot == 1 ? "MOON FIELD" : slot == 2 ? "ASTRAL SHIFT" : "CELESTIAL JUDGMENT";
    }

    public float GetAbilityCooldownDuration(int slot) => slot == 0 ? 5f : slot == 1 ? 10f : slot == 2 ? 8f : 48f;

    public float GetAbilityCooldownRatio(int slot)
    {
        float next = slot == 0 ? nextQ : slot == 1 ? nextW : slot == 2 ? nextE : nextR;
        return Mathf.Clamp01((next - Time.time) / GetAbilityCooldownDuration(slot));
    }
}

[DefaultExecutionOrder(-810)]
public class AOGPremiumFemaleHeroRosterRuntime : MonoBehaviour
{
    private bool built;
    private Font font;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        GameObject host = new GameObject("AOG_Premium_Female_Hero_Roster");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGPremiumFemaleHeroRosterRuntime>();
    }

    private void Awake()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }

    private void Update()
    {
        if (built)
            return;

        GameObject selectCanvas = GameObject.Find("ChampionSelectCanvas");
        if (selectCanvas == null)
            return;

        AOGActiveChampion nyra = CreateHero(AOGPremiumMageType.NyraSpiritVixen, "Nyra_Player", "nyra", "NYRA", "SPIRIT VIXEN", new Color(0.94f, 0.28f, 0.74f), new Vector3(-7f, 0.2f, 4f));
        AOGActiveChampion pyrelle = CreateHero(AOGPremiumMageType.PyrelleFlameSovereign, "Pyrelle_Player", "pyrelle", "PYRELLE", "FLAME SOVEREIGN", new Color(1f, 0.22f, 0.035f), new Vector3(0f, 0.2f, 5f));
        AOGActiveChampion selene = CreateHero(AOGPremiumMageType.SeleneAstralOracle, "Selene_Player", "selene", "SELENE", "ASTRAL ORACLE", new Color(0.36f, 0.68f, 1f), new Vector3(7f, 0.2f, 4f));

        AddHeroButton(selectCanvas.transform, nyra, new Vector2(-480f, -330f));
        AddHeroButton(selectCanvas.transform, pyrelle, new Vector2(0f, -330f));
        AddHeroButton(selectCanvas.transform, selene, new Vector2(480f, -330f));
        built = true;
    }

    private AOGActiveChampion CreateHero(AOGPremiumMageType type, string objectName, string id, string display, string role, Color accent, Vector3 offset)
    {
        GameObject go = new GameObject(objectName);
        Transform spawn = FindBlueSpawn();
        go.transform.position = (spawn != null ? spawn.position : Vector3.zero) + offset;
        BuildMageVisual(go.transform, type, accent);

        AOGCharacterStats stats = go.AddComponent<AOGCharacterStats>();
        stats.team = MinionTeam.Blue;
        stats.maxHp = type == AOGPremiumMageType.PyrelleFlameSovereign ? 860f : 790f;
        stats.hp = stats.maxHp;
        stats.moveSpeed = type == AOGPremiumMageType.NyraSpiritVixen ? 6.9f : 6.3f;
        stats.attackDamage = type == AOGPremiumMageType.PyrelleFlameSovereign ? 64f : 57f;
        stats.attackRange = 6.2f;
        stats.attackCooldown = type == AOGPremiumMageType.NyraSpiritVixen ? 0.82f : 0.92f;

        go.AddComponent<ChampionAudioController>();
        go.AddComponent<ChampionPresentationController>();
        go.AddComponent<AOGPlayerMOBAController>().enabled = false;
        go.AddComponent<AOGPlayerEconomy>();
        go.AddComponent<AOGChampionProgression>();
        go.AddComponent<AOGAutoAttackPresentationRuntime>();

        CapsuleCollider collider = go.AddComponent<CapsuleCollider>();
        collider.center = new Vector3(0f, 1.1f, 0f);
        collider.height = 2.4f;
        collider.radius = 0.62f;
        Rigidbody body = go.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;

        AOGActiveChampion marker = go.AddComponent<AOGActiveChampion>();
        marker.championId = id;
        marker.displayName = display;
        marker.roleName = role;
        marker.accentColor = accent;
        marker.SetActiveChampion(false);

        AOGPremiumMageSkillSet skillSet = go.AddComponent<AOGPremiumMageSkillSet>();
        skillSet.mageType = type;
        return marker;
    }

    private void AddHeroButton(Transform canvas, AOGActiveChampion hero, Vector2 position)
    {
        GameObject buttonObject = new GameObject("Select_" + hero.championId, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(canvas, false);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(360f, 64f);
        buttonObject.GetComponent<Image>().color = new Color(hero.accentColor.r * 0.42f, hero.accentColor.g * 0.42f, hero.accentColor.b * 0.42f, 0.98f);

        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(buttonObject.transform, false);
        RectTransform tr = textObject.GetComponent<RectTransform>();
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.offsetMin = tr.offsetMax = Vector2.zero;
        Text text = textObject.GetComponent<Text>();
        text.font = font;
        text.fontSize = 20;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.text = hero.displayName + "  •  " + hero.roleName;
        text.raycastTarget = false;

        buttonObject.GetComponent<Button>().onClick.AddListener(() => ActivatePremiumHero(hero));
    }

    private void ActivatePremiumHero(AOGActiveChampion selected)
    {
        foreach (AOGActiveChampion champion in FindObjectsByType<AOGActiveChampion>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (champion == null) continue;
            bool isSelected = champion == selected;
            champion.gameObject.SetActive(true);
            champion.SetActiveChampion(isSelected);
            if (!isSelected) champion.gameObject.SetActive(false);
        }

        selected.gameObject.SetActive(true);
        selected.SetActiveChampion(true);
        FindFirstObjectByType<AOGDynamicChampionHudBinder>()?.Bind(selected);
        AOGMatchDirector.Instance?.BeginMatch();

        GameObject canvas = GameObject.Find("ChampionSelectCanvas");
        if (canvas != null) Destroy(canvas);
    }

    private void BuildMageVisual(Transform parent, AOGPremiumMageType type, Color accent)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        Color clothColor = type == AOGPremiumMageType.NyraSpiritVixen
            ? new Color(0.18f, 0.035f, 0.14f)
            : type == AOGPremiumMageType.PyrelleFlameSovereign
                ? new Color(0.22f, 0.035f, 0.02f)
                : new Color(0.035f, 0.07f, 0.18f);

        Material cloth = new Material(shader) { color = clothColor };
        Material skin = new Material(shader) { color = new Color(0.92f, 0.72f, 0.64f) };
        Material hair = new Material(shader) { color = type == AOGPremiumMageType.PyrelleFlameSovereign ? new Color(0.42f, 0.035f, 0.02f) : new Color(0.08f, 0.05f, 0.12f) };
        Material energy = new Material(shader) { color = accent };
        if (energy.HasProperty("_EmissionColor"))
        {
            energy.EnableKeyword("_EMISSION");
            energy.SetColor("_EmissionColor", accent * 4.5f);
        }

        GameObject root = new GameObject(type + "_Premium_Visual");
        root.transform.SetParent(parent, false);
        Create(PrimitiveType.Capsule, "Body", root.transform, new Vector3(0f, 1.25f, 0f), new Vector3(0.58f, 0.95f, 0.46f), cloth);
        Create(PrimitiveType.Sphere, "Head", root.transform, new Vector3(0f, 2.5f, 0f), new Vector3(0.48f, 0.56f, 0.46f), skin);
        Create(PrimitiveType.Sphere, "Hair", root.transform, new Vector3(0f, 2.62f, -0.08f), new Vector3(0.55f, 0.62f, 0.50f), hair);
        Create(PrimitiveType.Cylinder, "Skirt", root.transform, new Vector3(0f, 0.65f, 0f), new Vector3(0.62f, 0.70f, 0.62f), cloth);
        Create(PrimitiveType.Sphere, "Spell_Core", root.transform, new Vector3(0.72f, 1.5f, 0.35f), Vector3.one * 0.28f, energy);

        if (type == AOGPremiumMageType.NyraSpiritVixen)
        {
            for (int i = 0; i < 5; i++)
            {
                float angle = Mathf.Lerp(-70f, 70f, i / 4f) * Mathf.Deg2Rad;
                Transform tail = Create(PrimitiveType.Capsule, "Spirit_Tail_" + i, root.transform, new Vector3(Mathf.Sin(angle) * 0.9f, 1.1f, -0.8f - Mathf.Cos(angle) * 0.35f), new Vector3(0.20f, 0.85f, 0.20f), energy).transform;
                tail.localRotation = Quaternion.Euler(55f, -Mathf.Sin(angle) * 35f, Mathf.Sin(angle) * 20f);
            }
        }
        else if (type == AOGPremiumMageType.PyrelleFlameSovereign)
        {
            Create(PrimitiveType.Cube, "Flame_Crown", root.transform, new Vector3(0f, 3.08f, 0f), new Vector3(0.7f, 0.12f, 0.7f), energy);
        }
        else
        {
            GameObject halo = AOGAbilityVisuals.CreateRing("Astral_Halo", parent.position + new Vector3(0f, 3.1f, 0f), 0.75f, accent, 0.06f);
            halo.transform.SetParent(root.transform, true);
        }
    }

    private static GameObject Create(PrimitiveType type, string name, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = material;
        Collider collider = go.GetComponent<Collider>();
        if (collider != null) Destroy(collider);
        return go;
    }

    private static Transform FindBlueSpawn()
    {
        GameObject spawn = GameObject.Find("BlueSpawn") ?? GameObject.Find("Blue_Spawn") ?? GameObject.Find("BlueBaseSpawn");
        return spawn != null ? spawn.transform : null;
    }
}
