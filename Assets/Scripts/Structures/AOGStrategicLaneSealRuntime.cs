using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum AOGSealLane
{
    Top,
    Mid,
    Bot
}

public enum AOGSealState
{
    Active,
    Destroyed,
    Respawning
}

public class AOGStrategicLaneSeal : MonoBehaviour
{
    public MinionTeam team;
    public AOGSealLane lane;
    public float maxHp = 1800f;
    public float hp = 1800f;
    public float respawnDuration = 150f;

    public AOGSealState State { get; private set; } = AOGSealState.Active;
    public float RespawnRemaining => State == AOGSealState.Respawning ? Mathf.Max(0f, respawnCompleteTime - Time.time) : 0f;

    public static event Action<AOGStrategicLaneSeal> SealDestroyed;
    public static event Action<AOGStrategicLaneSeal> SealReactivated;

    private float respawnCompleteTime;
    private Renderer[] renderers;
    private Collider[] colliders;
    private Vector3 initialScale;

    private void Awake()
    {
        initialScale = transform.localScale;
        renderers = GetComponentsInChildren<Renderer>(true);
        colliders = GetComponentsInChildren<Collider>(true);
        hp = Mathf.Clamp(hp <= 0f ? maxHp : hp, 0f, maxHp);
    }

    public void TakeDamage(float amount, GameObject source = null)
    {
        if (State != AOGSealState.Active || amount <= 0f)
            return;

        hp = Mathf.Clamp(hp - amount, 0f, maxHp);
        FlashHit();
        if (hp <= 0f)
            StartCoroutine(DestroyAndRespawn());
    }

    private IEnumerator DestroyAndRespawn()
    {
        if (State != AOGSealState.Active)
            yield break;

        State = AOGSealState.Destroyed;
        SealDestroyed?.Invoke(this);

        Vector3 startScale = transform.localScale;
        for (float t = 0f; t < 0.85f; t += Time.deltaTime)
        {
            float k = Mathf.Clamp01(t / 0.85f);
            transform.localScale = Vector3.Lerp(startScale, startScale * 0.12f, k * k);
            transform.Rotate(Vector3.up, 150f * Time.deltaTime, Space.World);
            yield return null;
        }

        SetVisible(false);
        State = AOGSealState.Respawning;
        respawnCompleteTime = Time.time + respawnDuration;
        yield return new WaitForSeconds(respawnDuration);

        transform.localScale = initialScale;
        hp = maxHp;
        SetVisible(true);
        State = AOGSealState.Active;
        respawnCompleteTime = 0f;
        AOGAbilityVisuals.CreateRing("Seal_Reactivated", transform.position + Vector3.up * 0.05f, 3.4f, TeamColor(), 0.12f);
        SealReactivated?.Invoke(this);
    }

    private void SetVisible(bool visible)
    {
        foreach (Renderer renderer in renderers)
            if (renderer != null) renderer.enabled = visible;
        foreach (Collider collider in colliders)
            if (collider != null) collider.enabled = visible;
    }

    private void FlashHit()
    {
        AOGAbilityVisuals.CreateRing("Seal_Hit", transform.position + Vector3.up * 0.05f, 2.2f, TeamColor(), 0.05f);
    }

    private Color TeamColor()
    {
        return team == MinionTeam.Blue ? new Color(0.20f,0.64f,1f) : new Color(1f,0.20f,0.26f);
    }
}

[DefaultExecutionOrder(-680)]
public class AOGStrategicLaneSealSystemRuntime : MonoBehaviour
{
    public static AOGStrategicLaneSealSystemRuntime Instance { get; private set; }
    private static readonly List<AOGStrategicLaneSeal> seals = new List<AOGStrategicLaneSeal>();
    private bool built;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindFirstObjectByType<AOGStrategicLaneSealSystemRuntime>() != null)
            return;
        GameObject host = new GameObject("AOG_Strategic_Lane_Seal_System");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGStrategicLaneSealSystemRuntime>();
    }

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        if (built) return;
        if (AOGMatchDirector.Instance == null) return;
        MinionSpawner spawner = FindFirstObjectByType<MinionSpawner>();
        if (spawner == null || spawner.blueBaseSpawn == null || spawner.redBaseSpawn == null) return;
        BuildSeals(spawner);
        built = true;
    }

    public static bool IsSealDown(MinionTeam sealOwner, AOGSealLane lane)
    {
        foreach (AOGStrategicLaneSeal seal in seals)
            if (seal != null && seal.team == sealOwner && seal.lane == lane && seal.State != AOGSealState.Active)
                return true;
        return false;
    }

    private void BuildSeals(MinionSpawner spawner)
    {
        seals.Clear();
        BuildTeamSeals(MinionTeam.Blue, spawner.blueBaseSpawn.position, spawner, 1f);
        BuildTeamSeals(MinionTeam.Red, spawner.redBaseSpawn.position, spawner, -1f);
    }

    private void BuildTeamSeals(MinionTeam team, Vector3 basePosition, MinionSpawner spawner, float direction)
    {
        CreateSeal(team, AOGSealLane.Top, ResolveLaneSealPosition(basePosition, spawner.topLaneWaypoints, team, direction));
        CreateSeal(team, AOGSealLane.Mid, ResolveLaneSealPosition(basePosition, spawner.midLaneWaypoints, team, direction));
        CreateSeal(team, AOGSealLane.Bot, ResolveLaneSealPosition(basePosition, spawner.botLaneWaypoints, team, direction));
    }

    private static Vector3 ResolveLaneSealPosition(Vector3 basePosition, Transform[] lane, MinionTeam team, float direction)
    {
        Vector3 target = basePosition + new Vector3(0f,0f,8f * direction);
        if (lane != null && lane.Length > 0)
        {
            Transform anchor = team == MinionTeam.Blue ? lane[0] : lane[lane.Length - 1];
            if (anchor != null)
                target = Vector3.Lerp(basePosition, anchor.position, 0.32f);
        }
        target.y = basePosition.y + 0.2f;
        return target;
    }

    private void CreateSeal(MinionTeam team, AOGSealLane lane, Vector3 position)
    {
        GameObject root = new GameObject(team + "_" + lane + "_" + (team == MinionTeam.Blue ? "AetherSeal" : "CorruptionSeal"));
        root.transform.position = position;

        CapsuleCollider collider = root.AddComponent<CapsuleCollider>();
        collider.center = new Vector3(0f,1.5f,0f);
        collider.height = 3.6f;
        collider.radius = 1.5f;

        AOGStrategicLaneSeal seal = root.AddComponent<AOGStrategicLaneSeal>();
        seal.team = team;
        seal.lane = lane;
        seal.maxHp = 1800f;
        seal.hp = seal.maxHp;
        seal.respawnDuration = 150f;

        BuildSealArt(root.transform, team, lane);
        AOGObjectiveWorldBar bar = root.AddComponent<AOGObjectiveWorldBar>();
        bar.offset = new Vector3(0f,4.8f,0f);
        bar.width = 3.1f;
        bar.height = 0.22f;
        seals.Add(seal);
    }

    private static void BuildSealArt(Transform root, MinionTeam team, AOGSealLane lane)
    {
        Color accent = team == MinionTeam.Blue ? new Color(0.16f,0.62f,1f) : new Color(1f,0.16f,0.24f);
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        Material dark = new Material(shader) { color = new Color(0.035f,0.05f,0.075f) };
        Material energy = new Material(shader) { color = accent };
        if (energy.HasProperty("_EmissionColor"))
        {
            energy.EnableKeyword("_EMISSION");
            energy.SetColor("_EmissionColor", accent * 4f);
        }

        GameObject baseDisk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        baseDisk.transform.SetParent(root,false);
        baseDisk.transform.localPosition = new Vector3(0f,0.25f,0f);
        baseDisk.transform.localScale = new Vector3(1.8f,0.25f,1.8f);
        baseDisk.GetComponent<Renderer>().sharedMaterial = dark;
        Destroy(baseDisk.GetComponent<Collider>());

        GameObject core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        core.transform.SetParent(root,false);
        core.transform.localPosition = new Vector3(0f,1.75f,0f);
        core.transform.localScale = lane == AOGSealLane.Mid ? new Vector3(0.95f,2.2f,0.95f) : new Vector3(0.8f,1.9f,0.8f);
        core.GetComponent<Renderer>().sharedMaterial = energy;
        Destroy(core.GetComponent<Collider>());

        for (int i=0;i<4;i++)
        {
            float angle = i * Mathf.PI * 0.5f;
            GameObject pylon = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pylon.transform.SetParent(root,false);
            pylon.transform.localPosition = new Vector3(Mathf.Cos(angle)*1.15f,1.0f,Mathf.Sin(angle)*1.15f);
            pylon.transform.localRotation = Quaternion.Euler(0f,-angle*Mathf.Rad2Deg,18f);
            pylon.transform.localScale = new Vector3(0.22f,1.7f,0.35f);
            pylon.GetComponent<Renderer>().sharedMaterial = dark;
            Destroy(pylon.GetComponent<Collider>());
        }

        AOGAbilityVisuals.CreateRing(team + "_" + lane + "_SealAura", root.position + Vector3.up*0.05f, 2.4f, accent, 0.08f);
    }
}

public class AOGEliteMinionRuntime : MonoBehaviour
{
    private Minion minion;
    private Vector3 baseScale;

    private void Awake()
    {
        minion = GetComponent<Minion>();
        baseScale = transform.localScale;
    }

    private void Start()
    {
        if (minion == null) return;
        minion.maxHp *= 2.15f;
        minion.hp = minion.maxHp;
        minion.damage *= 1.75f;
        minion.speed *= 0.92f;
        transform.localScale = baseScale * 1.18f;
        Color accent = minion.team == MinionTeam.Blue ? new Color(0.22f,0.76f,1f) : new Color(1f,0.24f,0.30f);
        AOGAbilityVisuals.CreateRing("Elite_Minon_Aura", transform.position + Vector3.up*0.05f, 1.15f, accent, 0.055f);
    }
}
