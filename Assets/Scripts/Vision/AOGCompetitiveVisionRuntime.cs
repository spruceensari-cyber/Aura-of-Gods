using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Lightweight team vision source. Champions, allied structures, lane minions and wards contribute
/// reveal radius to their owning team. The authority is information-only and never disables AI,
/// damage, physics or movement authority.
/// </summary>
public class AOGVisionSourceRuntime : MonoBehaviour
{
    public MinionTeam team = MinionTeam.Blue;
    public float radius = 12f;
    public bool revealNeutralObjectives = true;
    public bool persistentWhileDisabled;

    private void OnEnable() { AOGVisionAuthorityRuntime.Register(this); }
    private void OnDisable() { if (!persistentWhileDisabled) AOGVisionAuthorityRuntime.Unregister(this); }
    private void OnDestroy() { AOGVisionAuthorityRuntime.Unregister(this); }
}

public static class AOGVisionAuthorityRuntime
{
    private static readonly HashSet<AOGVisionSourceRuntime> sources = new HashSet<AOGVisionSourceRuntime>();
    private static readonly HashSet<AOGWardRuntime> wards = new HashSet<AOGWardRuntime>();

    public static IEnumerable<AOGVisionSourceRuntime> Sources => sources;
    public static IEnumerable<AOGWardRuntime> ActiveWards => wards;

    internal static void Register(AOGVisionSourceRuntime source)
    {
        if (source != null) sources.Add(source);
    }

    internal static void Unregister(AOGVisionSourceRuntime source)
    {
        if (source != null) sources.Remove(source);
    }

    internal static void RegisterWard(AOGWardRuntime ward)
    {
        if (ward != null) wards.Add(ward);
    }

    internal static void UnregisterWard(AOGWardRuntime ward)
    {
        if (ward != null) wards.Remove(ward);
    }

    public static bool IsVisibleToTeam(Vector3 worldPoint, MinionTeam viewerTeam)
    {
        foreach (AOGVisionSourceRuntime source in sources)
        {
            if (source == null || !source.isActiveAndEnabled || source.team != viewerTeam) continue;
            Vector3 delta = source.transform.position - worldPoint;
            delta.y = 0f;
            if (delta.sqrMagnitude <= source.radius * source.radius)
                return true;
        }
        return false;
    }

    public static bool IsVisibleToTeam(Component target, MinionTeam viewerTeam)
    {
        if (target == null) return false;

        AOGCharacterStats hero = target.GetComponentInParent<AOGCharacterStats>();
        if (hero != null)
        {
            if (hero.team == viewerTeam) return true;
            return IsVisibleToTeam(hero.transform.position,viewerTeam);
        }

        Minion minion = target.GetComponentInParent<Minion>();
        if (minion != null)
        {
            if (minion.team == viewerTeam) return true;
            return IsVisibleToTeam(minion.transform.position,viewerTeam);
        }

        TowerHealth tower = target.GetComponentInParent<TowerHealth>();
        if (tower != null && tower.towerTeam == viewerTeam) return true;

        return IsVisibleToTeam(target.transform.position,viewerTeam);
    }

    public static MinionTeam PlayerTeam
    {
        get
        {
            AOGActiveChampion player = AOGPlayerChampionAuthority.CurrentChampion;
            AOGCharacterStats stats = player != null ? player.GetComponent<AOGCharacterStats>() : null;
            return stats != null ? stats.team : MinionTeam.Blue;
        }
    }
}

/// <summary>
/// Local-player presentation gate for enemy champions. Hides enemy renderers and world health bars
/// while out of team vision without disabling colliders, AI, movement or combat simulation.
/// </summary>
public class AOGFogTargetPresentationRuntime : MonoBehaviour
{
    private AOGCharacterStats stats;
    private AOGWorldHealthBar worldBar;
    private bool lastVisible = true;
    private float nextRefresh;

    private void Awake()
    {
        stats = GetComponent<AOGCharacterStats>();
        worldBar = GetComponent<AOGWorldHealthBar>();
    }

    private void Update()
    {
        if (Time.unscaledTime < nextRefresh) return;
        nextRefresh = Time.unscaledTime + 0.10f;

        if (stats == null) return;
        MinionTeam viewer = AOGVisionAuthorityRuntime.PlayerTeam;
        bool visible = stats.team == viewer || AOGVisionAuthorityRuntime.IsVisibleToTeam(transform.position,viewer);
        if (visible == lastVisible) return;
        lastVisible = visible;
        ApplyVisibility(visible);
    }

    private void ApplyVisibility(bool visible)
    {
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
        {
            if (renderer == null) continue;
            string lower = renderer.gameObject.name.ToLowerInvariant();
            if (lower.Contains("vision_ring") || lower.Contains("ward_range")) continue;
            renderer.enabled = visible;
        }

        if (worldBar == null) worldBar = GetComponent<AOGWorldHealthBar>();
        if (worldBar != null)
        {
            if (visible) worldBar.Refresh();
            else worldBar.Hide();
        }
    }
}

/// <summary>
/// Timed team ward. Provides vision only and intentionally does not act as a combat unit.
/// </summary>
public class AOGWardRuntime : MonoBehaviour
{
    public MinionTeam team = MinionTeam.Blue;
    public float lifetime = 90f;
    public float visionRadius = 14f;
    private float expiresAt;
    private AOGVisionSourceRuntime source;

    public float Remaining => Mathf.Max(0f,expiresAt-Time.time);

    private void Awake()
    {
        expiresAt = Time.time + lifetime;
        source = GetComponent<AOGVisionSourceRuntime>();
        if (source == null) source = gameObject.AddComponent<AOGVisionSourceRuntime>();
        source.team = team;
        source.radius = visionRadius;
    }

    private void OnEnable()
    {
        AOGVisionAuthorityRuntime.RegisterWard(this);
        if (source != null)
        {
            source.team = team;
            source.radius = visionRadius;
        }
    }

    private void OnDisable() { AOGVisionAuthorityRuntime.UnregisterWard(this); }
    private void OnDestroy() { AOGVisionAuthorityRuntime.UnregisterWard(this); }

    private void Update()
    {
        if (Time.time >= expiresAt) Destroy(gameObject);
    }

    public static AOGWardRuntime Spawn(Vector3 point, MinionTeam team)
    {
        GameObject root = new GameObject(team + "_Aether_Ward");
        root.transform.position = point + Vector3.up * 0.08f;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        Color accent = team == MinionTeam.Blue ? new Color(0.18f,0.66f,1f) : new Color(1f,0.24f,0.34f);
        Material material = new Material(shader) { color = accent * 0.65f };
        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor",accent * 4f);
        }

        GameObject stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        stem.name = "Ward_Stem";
        stem.transform.SetParent(root.transform,false);
        stem.transform.localPosition = new Vector3(0f,0.45f,0f);
        stem.transform.localScale = new Vector3(0.12f,0.45f,0.12f);
        stem.GetComponent<Renderer>().sharedMaterial = material;
        Destroy(stem.GetComponent<Collider>());

        GameObject eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        eye.name = "Ward_Eye";
        eye.transform.SetParent(root.transform,false);
        eye.transform.localPosition = new Vector3(0f,1.05f,0f);
        eye.transform.localScale = Vector3.one * 0.32f;
        eye.GetComponent<Renderer>().sharedMaterial = material;
        Destroy(eye.GetComponent<Collider>());

        Light light = eye.AddComponent<Light>();
        light.type = LightType.Point;
        light.range = 4f;
        light.intensity = 1.1f;
        light.color = accent;

        AOGWardRuntime ward = root.AddComponent<AOGWardRuntime>();
        ward.team = team;
        ward.lifetime = 90f;
        ward.visionRadius = 14f;
        AOGAbilityVisuals.CreateRing("Ward_Placed",point+Vector3.up*0.05f,2.1f,accent,0.08f);
        return ward;
    }
}

/// <summary>
/// Human trinket input. Key 4 places a timed ward at the mouse ground point within placement range.
/// </summary>
public class AOGWardPlacementRuntime : MonoBehaviour
{
    public float placementRange = 8f;
    public float cooldown = 105f;
    private AOGCharacterStats stats;
    private AOGActiveChampion active;
    private float nextReady;

    public float CooldownRemaining => Mathf.Max(0f,nextReady-Time.time);

    private void Awake()
    {
        stats = GetComponent<AOGCharacterStats>();
        active = GetComponent<AOGActiveChampion>();
    }

    private void Update()
    {
        if (stats == null || stats.IsDead || active == null || AOGPlayerChampionAuthority.CurrentChampion != active) return;
        if (AOGMatchDirector.Instance == null || AOGMatchDirector.Instance.State != AOGMatchState.Playing) return;
        if (!AOGInputBridge.KeyPressedThisFrame(KeyCode.Alpha4) || Time.time < nextReady) return;
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

        Camera cam = Camera.main;
        if (cam == null) return;
        Ray ray = cam.ScreenPointToRay(AOGInputBridge.PointerPosition);
        Vector3 point;
        if (Physics.Raycast(ray,out RaycastHit hit,1200f,~0,QueryTriggerInteraction.Ignore)) point = hit.point;
        else
        {
            Plane ground = new Plane(Vector3.up,transform.position);
            if (!ground.Raycast(ray,out float enter)) return;
            point = ray.GetPoint(enter);
        }

        Vector3 flat = point-transform.position;
        flat.y = 0f;
        if (flat.magnitude > placementRange)
            point = transform.position + flat.normalized * placementRange;
        point.y = transform.position.y;

        AOGWardRuntime.Spawn(point,stats.team);
        nextReady = Time.time + cooldown;
        AOGScoreboardAndAnnouncerRuntime.Instance?.ShowExternalMessage("AETHER WARD DEPLOYED",new Color(0.40f,0.82f,1f),1.2f);
    }
}

/// <summary>
/// Minimap fog texture driven by the same team vision authority. It darkens unknown map cells while
/// leaving visible cells readable. Markers remain separate UI children and can be filtered independently.
/// </summary>
public class AOGMinimapFogOverlayRuntime : MonoBehaviour
{
    private const int TextureSize = 64;
    private const float HalfWidth = 170f;
    private const float HalfDepth = 156f;
    private Texture2D texture;
    private RawImage image;
    private RectTransform minimap;
    private float nextUpdate;

    private void Update()
    {
        if (minimap == null)
        {
            AOGCompetitiveMobaHUDRuntime hud = FindFirstObjectByType<AOGCompetitiveMobaHUDRuntime>();
            if (hud == null) return;
            foreach (RectTransform rect in hud.GetComponentsInChildren<RectTransform>(true))
                if (rect.name == "Minimap") { minimap = rect; break; }
            if (minimap == null) return;
            BuildOverlay();
        }

        if (Time.unscaledTime < nextUpdate) return;
        nextUpdate = Time.unscaledTime + 0.25f;
        RefreshTexture();
    }

    private void BuildOverlay()
    {
        GameObject go = new GameObject("FogOfWarOverlay",typeof(RectTransform),typeof(RawImage));
        go.transform.SetParent(minimap,false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        go.transform.SetAsFirstSibling();

        texture = new Texture2D(TextureSize,TextureSize,TextureFormat.RGBA32,false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        image = go.GetComponent<RawImage>();
        image.texture = texture;
        image.raycastTarget = false;
    }

    private void RefreshTexture()
    {
        if (texture == null) return;
        MinionTeam team = AOGVisionAuthorityRuntime.PlayerTeam;
        Color visible = new Color(0.01f,0.02f,0.04f,0.08f);
        Color hidden = new Color(0.005f,0.01f,0.02f,0.78f);
        Color[] pixels = new Color[TextureSize*TextureSize];

        for (int y=0;y<TextureSize;y++)
        {
            float nz = y/(TextureSize-1f)*2f-1f;
            for (int x=0;x<TextureSize;x++)
            {
                float nx = x/(TextureSize-1f)*2f-1f;
                Vector3 point = new Vector3(nx*HalfWidth,0f,nz*HalfDepth);
                pixels[y*TextureSize+x] = AOGVisionAuthorityRuntime.IsVisibleToTeam(point,team) ? visible : hidden;
            }
        }

        texture.SetPixels(pixels);
        texture.Apply(false,false);
    }
}

[DefaultExecutionOrder(-610)]
public class AOGCompetitiveVisionBootstrap : MonoBehaviour
{
    private float nextAttach;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindFirstObjectByType<AOGCompetitiveVisionBootstrap>() != null) return;
        GameObject host = new GameObject("AOG_Competitive_Vision_Bootstrap");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGCompetitiveVisionBootstrap>();
        host.AddComponent<AOGMinimapFogOverlayRuntime>();
    }

    private void Update()
    {
        if (Time.unscaledTime < nextAttach) return;
        nextAttach = Time.unscaledTime + 1.5f;

        foreach (AOGTeamMemberIdentity member in AOGWorldRegistry.TeamMembers)
        {
            if (member == null) continue;
            EnsureSource(member.gameObject,member.team,member.role == AOGRole.Support ? 14f : 13f);
            if (member.GetComponent<AOGFogTargetPresentationRuntime>() == null)
                member.gameObject.AddComponent<AOGFogTargetPresentationRuntime>();
            if (member.isHumanPlayer && member.GetComponent<AOGWardPlacementRuntime>() == null)
                member.gameObject.AddComponent<AOGWardPlacementRuntime>();
        }

        foreach (TowerHealth tower in AOGWorldRegistry.Towers)
            if (tower != null && tower.hp > 0f) EnsureSource(tower.gameObject,tower.towerTeam,16f);

        foreach (AOGStrategicLaneSeal seal in AOGWorldRegistry.Seals)
            if (seal != null) EnsureSource(seal.gameObject,seal.team,12f);

        foreach (Minion minion in FindObjectsByType<Minion>(FindObjectsInactive.Exclude,FindObjectsSortMode.None))
            if (minion != null && minion.hp > 0f) EnsureSource(minion.gameObject,minion.team,8.5f);
    }

    private static void EnsureSource(GameObject target,MinionTeam team,float radius)
    {
        if (target == null) return;
        AOGVisionSourceRuntime source = target.GetComponent<AOGVisionSourceRuntime>();
        if (source == null) source = target.AddComponent<AOGVisionSourceRuntime>();
        source.team = team;
        source.radius = radius;
    }
}
