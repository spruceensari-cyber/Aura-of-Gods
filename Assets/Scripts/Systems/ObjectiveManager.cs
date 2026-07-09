using UnityEngine;
using UnityEngine.Serialization;
using System.Collections;

/// <summary>
/// Aura of Gods neutral objective manager.
/// Bot-side objective: Dragon. Top-side objective: Medusa.
/// Spawns competitive boss AI and keeps serialized migration support for scenes that previously stored the top objective as Baron.
/// </summary>
public class ObjectiveManager : MonoBehaviour
{
    [Header("Respawn Timers")]
    [SerializeField] private float dragonRespawnTime = 300f;
    [FormerlySerializedAs("baronRespawnTime")]
    [SerializeField] private float medusaRespawnTime = 420f;

    [Header("Objective Pits")]
    [SerializeField] private Transform dragonPitPosition;
    [FormerlySerializedAs("baronPitPosition")]
    [SerializeField] private Transform medusaPitPosition;

    [Header("Fallback Map Positions")]
    [SerializeField] private Vector3 fallbackDragonPosition = new Vector3(45f, 0f, -35f);
    [SerializeField] private Vector3 fallbackMedusaPosition = new Vector3(-45f, 0f, 35f);

    private GameObject dragonNPC;
    private GameObject medusaNPC;
    private bool dragonAlive;
    private bool medusaAlive;
    private float dragonRespawnAt = -1f;
    private float medusaRespawnAt = -1f;

    public bool DragonAlive => dragonAlive;
    public bool MedusaAlive => medusaAlive;
    public GameObject DragonObject => dragonNPC;
    public GameObject MedusaObject => medusaNPC;
    public float DragonRespawnRemaining => dragonAlive || dragonRespawnAt < 0f ? 0f : Mathf.Max(0f, dragonRespawnAt - Time.time);
    public float MedusaRespawnRemaining => medusaAlive || medusaRespawnAt < 0f ? 0f : Mathf.Max(0f, medusaRespawnAt - Time.time);

    public event System.Action OnObjectiveStateChanged;

    void Start()
    {
        SpawnDragon();
        SpawnMedusa();
    }

    private void SpawnDragon()
    {
        Vector3 position = dragonPitPosition != null ? dragonPitPosition.position : fallbackDragonPosition;
        GameObject dragon = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        dragon.name = "Dragon_Objective";
        dragon.transform.position = position;
        dragon.transform.localScale = Vector3.one;

        Renderer baseRenderer = dragon.GetComponent<Renderer>();
        if (baseRenderer != null)
            baseRenderer.enabled = false;

        CombatUnit unit = dragon.AddComponent<CombatUnit>();
        unit.Configure(TeamType.Neutral, UnitType.Neutral, 3000f);
        unit.OnDeath += OnDragonKilled;

        AOGDragonBossAI boss = dragon.AddComponent<AOGDragonBossAI>();
        boss.InitializePresentation();

        dragonNPC = dragon;
        dragonAlive = true;
        dragonRespawnAt = -1f;
        OnObjectiveStateChanged?.Invoke();
    }

    private void SpawnMedusa()
    {
        Vector3 position = medusaPitPosition != null ? medusaPitPosition.position : fallbackMedusaPosition;
        GameObject medusa = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        medusa.name = "Medusa_Objective";
        medusa.transform.position = position;
        medusa.transform.localScale = Vector3.one;

        Renderer baseRenderer = medusa.GetComponent<Renderer>();
        if (baseRenderer != null)
            baseRenderer.enabled = false;

        CombatUnit unit = medusa.AddComponent<CombatUnit>();
        unit.Configure(TeamType.Neutral, UnitType.Neutral, 5000f);
        unit.OnDeath += OnMedusaKilled;

        medusa.AddComponent<AOGMedusaBossAI>();

        medusaNPC = medusa;
        medusaAlive = true;
        medusaRespawnAt = -1f;
        OnObjectiveStateChanged?.Invoke();
    }

    public void OnDragonKilled()
    {
        if (!dragonAlive)
            return;

        dragonAlive = false;
        dragonRespawnAt = Time.time + dragonRespawnTime;
        PublishState();
        StartCoroutine(RespawnDragonAfterDelay());
    }

    public void OnMedusaKilled()
    {
        if (!medusaAlive)
            return;

        medusaAlive = false;
        medusaRespawnAt = Time.time + medusaRespawnTime;
        PublishState();
        StartCoroutine(RespawnMedusaAfterDelay());
    }

    public void OnBaronKilled()
    {
        OnMedusaKilled();
    }

    private IEnumerator RespawnDragonAfterDelay()
    {
        if (dragonNPC != null)
            Destroy(dragonNPC);

        yield return new WaitForSeconds(dragonRespawnTime);
        SpawnDragon();
    }

    private IEnumerator RespawnMedusaAfterDelay()
    {
        if (medusaNPC != null)
            Destroy(medusaNPC);

        yield return new WaitForSeconds(medusaRespawnTime);
        SpawnMedusa();
    }

    private void PublishState()
    {
        OnObjectiveStateChanged?.Invoke();

        if (NetworkManager.Instance != null)
            NetworkManager.Instance.OnStateUpdate?.Invoke(new GameStateSnapshot());
    }
}
