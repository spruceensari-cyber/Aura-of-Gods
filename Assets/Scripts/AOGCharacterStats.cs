using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AOGCharacterStats : MonoBehaviour
{
    public MinionTeam team = MinionTeam.Blue;

    [Header("Health")]
    public float maxHp = 600f;
    public float hp = 600f;
    [Min(0f)] public float deathPresentationDuration = 1.6f;
    [Min(1f)] public float baseRespawnTime = 7f;

    [Header("Combat")]
    public float attackDamage = 45f;
    public float attackRange = 4f;
    public float attackCooldown = 1.1f;

    [Header("Movement")]
    public float moveSpeed = 6f;

    private ChampionPresentationController presentation;
    private bool deathStarted;
    private Vector3 fallbackSpawnPosition;
    private Quaternion fallbackSpawnRotation;
    private GameObject lastDamageSource;

    public bool IsDead => hp <= 0f || deathStarted;

    private void Start()
    {
        if (hp <= 0f)
            hp = maxHp;

        presentation = GetComponent<ChampionPresentationController>();
        fallbackSpawnPosition = transform.position;
        fallbackSpawnRotation = transform.rotation;
    }

    public void TakeDamage(float amount)
    {
        TakeDamage(amount, null);
    }

    public void TakeDamage(float amount, GameObject source)
    {
        if (deathStarted || amount <= 0f)
            return;

        if (source != null)
        {
            lastDamageSource = source;
            AOGChampionDamageLedger ledger = GetComponent<AOGChampionDamageLedger>();
            if (ledger == null)
                ledger = gameObject.AddComponent<AOGChampionDamageLedger>();
            ledger.RegisterDamage(source);
        }

        hp = Mathf.Clamp(hp - amount, 0f, maxHp);

        if (hp > 0f)
        {
            presentation?.PlayHitReaction();
            return;
        }

        Die(source);
    }

    private void Die(GameObject killer)
    {
        if (deathStarted)
            return;

        deathStarted = true;
        hp = 0f;
        presentation?.PlayDeath();

        AOGChampionDamageLedger ledger = GetComponent<AOGChampionDamageLedger>();
        List<GameObject> assistants = ledger != null ? ledger.CollectAssistants(killer) : new List<GameObject>();
        AOGCombatEvents.RaiseChampionDeath(new AOGChampionDeathEvent
        {
            victim = this,
            killer = killer != null ? killer : lastDamageSource,
            assistants = assistants
        });

        SetGameplayEnabled(false);
        StartCoroutine(RespawnSequence());
    }

    private IEnumerator RespawnSequence()
    {
        if (deathPresentationDuration > 0f)
            yield return new WaitForSeconds(deathPresentationDuration);

        SetRenderersVisible(false);

        AOGChampionProgression progression = GetComponent<AOGChampionProgression>();
        int level = progression != null ? progression.level : 1;
        float respawnTime = baseRespawnTime + Mathf.Max(0, level - 1) * 0.65f;
        yield return new WaitForSeconds(respawnTime);

        Transform spawn = FindTeamSpawn();
        if (spawn != null)
        {
            transform.position = spawn.position + new Vector3(team == MinionTeam.Blue ? 1.8f : -1.8f, 0.25f, team == MinionTeam.Blue ? 1.8f : -1.8f);
            transform.rotation = spawn.rotation;
        }
        else
        {
            transform.position = fallbackSpawnPosition;
            transform.rotation = fallbackSpawnRotation;
        }

        hp = maxHp;
        deathStarted = false;
        lastDamageSource = null;
        AOGChampionDamageLedger ledger = GetComponent<AOGChampionDamageLedger>();
        ledger?.ClearLedger();
        SetRenderersVisible(true);
        SetGameplayEnabled(true);

        AOGAbilityVisuals.CreateRing("Champion_Respawn", transform.position + Vector3.up * 0.1f, 2.8f, new Color(0.35f, 0.82f, 1f, 1f), 0.14f);

        AOGActiveChampion marker = GetComponent<AOGActiveChampion>();
        bool isHumanPlayer = marker != null && AOGPlayerChampionAuthority.CurrentChampion == marker;
        if (isHumanPlayer)
        {
            Camera camera = Camera.main;
            if (camera != null)
                camera.GetComponent<AOGMobaCameraController>()?.SetTarget(transform, true);
        }
    }

    private void SetGameplayEnabled(bool enabled)
    {
        AOGUnifiedMobaInputDriver unified = GetComponent<AOGUnifiedMobaInputDriver>();
        if (unified != null)
        {
            AOGActiveChampion marker = GetComponent<AOGActiveChampion>();
            bool isHumanPlayer = marker != null && AOGPlayerChampionAuthority.CurrentChampion == marker;
            unified.enabled = enabled && isHumanPlayer;
        }

        AOGPlayerMOBAController legacyMoba = GetComponent<AOGPlayerMOBAController>();
        if (legacyMoba != null)
            legacyMoba.enabled = false;

        foreach (Collider col in GetComponentsInChildren<Collider>(true))
        {
            if (col != null)
                col.enabled = enabled;
        }
    }

    private void SetRenderersVisible(bool visible)
    {
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
        {
            if (renderer != null && !renderer.gameObject.name.ToLowerInvariant().Contains("hp_bar"))
                renderer.enabled = visible;
        }

        AOGWorldHealthBar healthBar = GetComponent<AOGWorldHealthBar>();
        if (healthBar != null)
        {
            if (visible) healthBar.Refresh();
            else healthBar.Hide();
        }
    }

    private Transform FindTeamSpawn()
    {
        string[] names = team == MinionTeam.Blue
            ? new[] { "BlueSpawn", "Blue_Spawn", "BlueBaseSpawn" }
            : new[] { "RedSpawn", "Red_Spawn", "RedBaseSpawn" };

        GameObject[] all = FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (GameObject obj in all)
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
}
