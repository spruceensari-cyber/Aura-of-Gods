using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum AOGMatchState
{
    Loading,
    ChampionSelect,
    MatchStarting,
    Playing,
    BlueVictory,
    RedVictory
}

[DefaultExecutionOrder(-1000)]
public class AOGMatchDirector : MonoBehaviour
{
    public static AOGMatchDirector Instance { get; private set; }

    public AOGMatchState State { get; private set; } = AOGMatchState.Loading;
    public float MatchTime => State == AOGMatchState.Playing ? Time.unscaledTime - matchStartTime : 0f;
    public AOGNexusCore BlueNexus { get; private set; }
    public AOGNexusCore RedNexus { get; private set; }

    private float matchStartTime;
    private Canvas resultCanvas;
    private int activeSceneHandle = int.MinValue;
    private bool matchStartInProgress;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        EnsureInstance();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureInstance();
        if (Instance != null)
        {
            Instance.PrepareForScene(scene);
            Instance.StartCoroutine(Instance.SetupSceneAfterStartup());
        }
    }

    private static void EnsureInstance()
    {
        if (Instance != null)
            return;

        AOGMatchDirector existing = Object.FindFirstObjectByType<AOGMatchDirector>();
        if (existing != null)
        {
            Instance = existing;
            return;
        }

        GameObject host = new GameObject("AOG_Match_Director");
        Instance = host.AddComponent<AOGMatchDirector>();
        Object.DontDestroyOnLoad(host);
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

    private void Start()
    {
        PrepareForScene(SceneManager.GetActiveScene());
        StartCoroutine(SetupSceneAfterStartup());
    }

    private void PrepareForScene(Scene scene)
    {
        if (!scene.IsValid() || activeSceneHandle == scene.handle)
            return;

        activeSceneHandle = scene.handle;
        matchStartTime = 0f;
        matchStartInProgress = false;
        BlueNexus = null;
        RedNexus = null;

        if (resultCanvas != null)
            Destroy(resultCanvas.gameObject);

        SetState(AOGMatchState.Loading);
    }

    private IEnumerator SetupSceneAfterStartup()
    {
        yield return null;
        yield return null;

        EnsureNexusCores();
        EnsureTowerBars();

        if (State == AOGMatchState.Loading)
        {
            SetState(AOGMatchState.ChampionSelect);
            AOGGameSession.EnsureInstance().BeginChampionSelection();
        }
    }

    public void BeginMatch()
    {
        if (State == AOGMatchState.Playing || State == AOGMatchState.MatchStarting || matchStartInProgress)
            return;

        AOGGameSession session = AOGGameSession.EnsureInstance();
        AOGPlayerChampionAuthority authority = AOGPlayerChampionAuthority.EnsureInstance();
        if (!session.SelectionCommitted || !authority.HasValidPlayer)
        {
            Debug.LogError("AOG: Match start was blocked because no valid selected player champion is registered.");
            return;
        }

        StartCoroutine(BeginMatchRoutine());
    }

    private IEnumerator BeginMatchRoutine()
    {
        matchStartInProgress = true;
        SetState(AOGMatchState.MatchStarting);
        yield return null;

        AOGPlayerChampionAuthority authority = AOGPlayerChampionAuthority.EnsureInstance();
        if (authority.HasValidPlayer)
        {
            matchStartTime = Time.unscaledTime;
            SetState(AOGMatchState.Playing);
        }
        else
        {
            SetState(AOGMatchState.ChampionSelect);
        }

        matchStartInProgress = false;
    }

    public void RegisterNexus(AOGNexusCore nexus)
    {
        if (nexus == null)
            return;

        if (nexus.team == MinionTeam.Blue)
            BlueNexus = nexus;
        else
            RedNexus = nexus;
    }

    public void NotifyNexusDestroyed(AOGNexusCore nexus)
    {
        if (nexus == null || State == AOGMatchState.BlueVictory || State == AOGMatchState.RedVictory)
            return;

        bool blueWon = nexus.team == MinionTeam.Red;
        SetState(blueWon ? AOGMatchState.BlueVictory : AOGMatchState.RedVictory);
        ShowResultOverlay(blueWon);
    }

    public AOGNexusCore GetEnemyNexus(MinionTeam team)
    {
        return team == MinionTeam.Blue ? RedNexus : BlueNexus;
    }

    private void SetState(AOGMatchState newState)
    {
        State = newState;
        AOGGameSession.Instance?.SetMatchState(newState);
    }

    private void EnsureNexusCores()
    {
        AOGNexusCore[] existing = Object.FindObjectsByType<AOGNexusCore>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (AOGNexusCore nexus in existing)
            RegisterNexus(nexus);

        Transform blueSpawn = FindNamedTransform("BlueBaseSpawn", "Blue_Spawn", "BlueSpawn");
        Transform redSpawn = FindNamedTransform("RedBaseSpawn", "Red_Spawn", "RedSpawn");

        if (BlueNexus == null && blueSpawn != null)
            BlueNexus = CreateNexus("Blue_Aether_Nexus", blueSpawn.position + new Vector3(-2f, 0f, -2f), MinionTeam.Blue);

        if (RedNexus == null && redSpawn != null)
            RedNexus = CreateNexus("Red_Aether_Nexus", redSpawn.position + new Vector3(2f, 0f, 2f), MinionTeam.Red);
    }

    private static Transform FindNamedTransform(params string[] names)
    {
        GameObject[] objects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (GameObject obj in objects)
        {
            if (obj == null)
                continue;

            foreach (string candidate in names)
            {
                if (string.Equals(obj.name, candidate, System.StringComparison.OrdinalIgnoreCase))
                    return obj.transform;
            }
        }

        return null;
    }

    private AOGNexusCore CreateNexus(string objectName, Vector3 position, MinionTeam team)
    {
        GameObject root = new GameObject(objectName);
        root.transform.position = position;

        CapsuleCollider collider = root.AddComponent<CapsuleCollider>();
        collider.center = new Vector3(0f, 2.2f, 0f);
        collider.height = 5.2f;
        collider.radius = 2.6f;

        AOGNexusCore nexus = root.AddComponent<AOGNexusCore>();
        nexus.team = team;
        nexus.maxHp = 4000f;
        nexus.hp = nexus.maxHp;

        AOGCombatUnit combatUnit = root.AddComponent<AOGCombatUnit>();
        combatUnit.team = team == MinionTeam.Blue ? AOGTeam.Blue : AOGTeam.Red;
        combatUnit.unitType = AOGUnitType.Nexus;

        AOGObjectiveWorldBar bar = root.AddComponent<AOGObjectiveWorldBar>();
        bar.offset = new Vector3(0f, 6.2f, 0f);
        bar.width = 4.8f;
        bar.height = 0.34f;

        Color teamColor = team == MinionTeam.Blue
            ? new Color(0.15f, 0.58f, 1f, 1f)
            : new Color(1f, 0.18f, 0.24f, 1f);

        BuildNexusArt(root.transform, teamColor);
        RegisterNexus(nexus);
        return nexus;
    }

    private static void BuildNexusArt(Transform root, Color teamColor)
    {
        Material darkMetal = CreateLitMaterial("Nexus_DarkMetal", new Color(0.045f, 0.06f, 0.085f, 1f), 0.72f, 0.55f);
        Material energy = CreateEmissionMaterial("Nexus_Energy", teamColor, 4.5f);

        GameObject baseDisk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        baseDisk.name = "Nexus_Base";
        baseDisk.transform.SetParent(root, false);
        baseDisk.transform.localPosition = new Vector3(0f, 0.35f, 0f);
        baseDisk.transform.localScale = new Vector3(3.6f, 0.35f, 3.6f);
        baseDisk.GetComponent<Renderer>().sharedMaterial = darkMetal;
        Destroy(baseDisk.GetComponent<Collider>());

        GameObject core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        core.name = "Nexus_Core";
        core.transform.SetParent(root, false);
        core.transform.localPosition = new Vector3(0f, 2.7f, 0f);
        core.transform.localScale = new Vector3(1.65f, 2.8f, 1.65f);
        core.GetComponent<Renderer>().sharedMaterial = energy;
        Destroy(core.GetComponent<Collider>());

        for (int i = 0; i < 3; i++)
        {
            GameObject ring = new GameObject("Nexus_Orbit_Ring_" + i);
            ring.transform.SetParent(root, false);
            ring.transform.localPosition = new Vector3(0f, 2.7f, 0f);
            ring.transform.localRotation = Quaternion.Euler(25f + i * 38f, i * 60f, 15f);
            LineRenderer line = ring.AddComponent<LineRenderer>();
            line.loop = true;
            line.useWorldSpace = false;
            line.positionCount = 64;
            line.startWidth = 0.09f;
            line.endWidth = 0.09f;
            line.sharedMaterial = energy;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            float radius = 2.05f + i * 0.28f;
            for (int p = 0; p < line.positionCount; p++)
            {
                float angle = p * Mathf.PI * 2f / line.positionCount;
                line.SetPosition(p, new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
            }
            AOGOrbitAnimator orbit = ring.AddComponent<AOGOrbitAnimator>();
            orbit.localAxis = i % 2 == 0 ? Vector3.up : Vector3.forward;
            orbit.speed = 24f + i * 11f;
        }

        for (int i = 0; i < 6; i++)
        {
            float angle = i * Mathf.PI * 2f / 6f;
            GameObject pylon = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pylon.name = "Nexus_Pylon_" + i;
            pylon.transform.SetParent(root, false);
            pylon.transform.localPosition = new Vector3(Mathf.Cos(angle) * 2.65f, 1.25f, Mathf.Sin(angle) * 2.65f);
            pylon.transform.localRotation = Quaternion.Euler(-15f, -angle * Mathf.Rad2Deg, 18f);
            pylon.transform.localScale = new Vector3(0.32f, 2.6f, 0.5f);
            pylon.GetComponent<Renderer>().sharedMaterial = darkMetal;
            Destroy(pylon.GetComponent<Collider>());
        }
    }

    private void EnsureTowerBars()
    {
        TowerHealth[] towers = Object.FindObjectsByType<TowerHealth>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (TowerHealth tower in towers)
        {
            if (tower == null)
                continue;

            AOGObjectiveWorldBar bar = tower.GetComponent<AOGObjectiveWorldBar>();
            if (bar == null)
                bar = tower.gameObject.AddComponent<AOGObjectiveWorldBar>();

            bar.offset = new Vector3(0f, 6.5f, 0f);
            bar.width = 3.6f;
            bar.height = 0.26f;
        }
    }

    private static Material CreateLitMaterial(string name, Color color, float smoothness, float metallic)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        Material material = new Material(shader) { name = name, color = color };
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
        return material;
    }

    private static Material CreateEmissionMaterial(string name, Color color, float emission)
    {
        Material material = CreateLitMaterial(name, color, 0.45f, 0.15f);
        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * emission);
        }
        return material;
    }

    private void ShowResultOverlay(bool blueWon)
    {
        if (resultCanvas != null)
            Destroy(resultCanvas.gameObject);

        GameObject canvasObject = new GameObject("MatchResultCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        resultCanvas = canvasObject.GetComponent<Canvas>();
        resultCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        resultCanvas.sortingOrder = 5000;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        GameObject shade = new GameObject("Shade", typeof(RectTransform), typeof(Image));
        shade.transform.SetParent(canvasObject.transform, false);
        RectTransform shadeRect = shade.GetComponent<RectTransform>();
        shadeRect.anchorMin = Vector2.zero;
        shadeRect.anchorMax = Vector2.one;
        shadeRect.offsetMin = Vector2.zero;
        shadeRect.offsetMax = Vector2.zero;
        shade.GetComponent<Image>().color = new Color(0.005f, 0.008f, 0.015f, 0.78f);

        GameObject textObject = new GameObject("Result", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(shade.transform, false);
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(1000f, 180f);

        Text label = textObject.GetComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.text = blueWon ? "VICTORY — BLUE NEXUS ASCENDANT" : "DEFEAT — RED NEXUS ASCENDANT";
        label.fontSize = 54;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = blueWon ? new Color(0.42f, 0.78f, 1f) : new Color(1f, 0.34f, 0.38f);
    }
}

public class AOGNexusCore : MonoBehaviour
{
    public MinionTeam team;
    public float maxHp = 4000f;
    public float hp = 4000f;
    public bool IsDestroyed => hp <= 0f;

    private bool destroyed;

    private void Start()
    {
        hp = Mathf.Clamp(hp <= 0f ? maxHp : hp, 0f, maxHp);
        AOGMatchDirector.Instance?.RegisterNexus(this);
    }

    public void TakeDamage(float amount)
    {
        if (destroyed || amount <= 0f)
            return;

        hp = Mathf.Clamp(hp - amount, 0f, maxHp);
        if (hp <= 0f)
            DestroyNexus();
    }

    private void DestroyNexus()
    {
        if (destroyed)
            return;

        destroyed = true;
        StartCoroutine(DestructionSequence());
        AOGMatchDirector.Instance?.NotifyNexusDestroyed(this);
    }

    private IEnumerator DestructionSequence()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        Vector3 originalScale = transform.localScale;

        for (float t = 0f; t < 1.2f; t += Time.deltaTime)
        {
            float pulse = 1f + Mathf.Sin(t * 28f) * 0.08f;
            transform.localScale = originalScale * pulse;
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null) continue;
                foreach (Material material in renderer.materials)
                {
                    if (material.HasProperty("_EmissionColor"))
                        material.SetColor("_EmissionColor", Color.white * Mathf.Lerp(2f, 12f, t / 1.2f));
                }
            }
            yield return null;
        }

        transform.localScale = originalScale * 0.1f;
    }
}

public class AOGObjectiveWorldBar : MonoBehaviour
{
    public Vector3 offset = new Vector3(0f, 5f, 0f);
    public float width = 3.5f;
    public float height = 0.25f;

    private Transform root;
    private Transform fill;
    private TowerHealth tower;
    private AOGNexusCore nexus;
    private AOGNeutralBossAI boss;

    private void Start()
    {
        tower = GetComponent<TowerHealth>();
        nexus = GetComponent<AOGNexusCore>();
        boss = GetComponent<AOGNeutralBossAI>();
        Build();
    }

    private void LateUpdate()
    {
        if (root == null || fill == null)
            return;

        float ratio = GetRatio();
        fill.localScale = new Vector3(ratio, 1f, 1f);
        fill.localPosition = new Vector3(-(1f - ratio) * 0.5f, 0f, -0.03f);

        if (Camera.main != null)
            root.rotation = Camera.main.transform.rotation;
    }

    private float GetRatio()
    {
        if (tower != null) return Mathf.Clamp01(tower.hp / Mathf.Max(1f, tower.maxHp));
        if (nexus != null) return Mathf.Clamp01(nexus.hp / Mathf.Max(1f, nexus.maxHp));
        if (boss != null) return Mathf.Clamp01(boss.hp / Mathf.Max(1f, boss.maxHp));
        return 1f;
    }

    private MinionTeam GetTeam()
    {
        if (tower != null) return tower.towerTeam;
        if (nexus != null) return nexus.team;
        return MinionTeam.Blue;
    }

    private void Build()
    {
        GameObject rootObject = new GameObject("AOG_Objective_HP_Bar");
        rootObject.transform.SetParent(transform, false);
        rootObject.transform.localPosition = offset;
        root = rootObject.transform;

        GameObject border = CreateCube("Border", root, new Vector3(width + 0.16f, height + 0.14f, 0.08f), new Color(0.01f, 0.015f, 0.025f, 1f));
        GameObject bg = CreateCube("Background", border.transform, new Vector3(0.95f, 0.58f, 0.75f), new Color(0.06f, 0.075f, 0.085f, 1f));

        Color color;
        if (boss != null)
            color = new Color(0.82f, 0.34f, 0.94f, 1f);
        else
            color = GetTeam() == MinionTeam.Blue ? new Color(0.18f, 0.62f, 1f, 1f) : new Color(1f, 0.20f, 0.24f, 1f);

        GameObject fillObject = CreateCube("Fill", bg.transform, new Vector3(1f, 0.74f, 0.65f), color);
        fill = fillObject.transform;
    }

    private static GameObject CreateCube(string name, Transform parent, Vector3 scale, Color color)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localScale = scale;
        Collider col = go.GetComponent<Collider>();
        if (col != null) Destroy(col);

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        Material material = new Material(shader) { color = color };
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        go.GetComponent<Renderer>().sharedMaterial = material;
        go.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return go;
    }
}

public class AOGOrbitAnimator : MonoBehaviour
{
    public Vector3 localAxis = Vector3.up;
    public float speed = 25f;

    private void Update()
    {
        transform.Rotate(localAxis, speed * Time.deltaTime, Space.Self);
    }
}
