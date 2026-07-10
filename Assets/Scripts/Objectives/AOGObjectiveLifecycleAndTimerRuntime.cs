using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public struct AOGObjectiveLifecycleSnapshot
{
    public string key;
    public string displayName;
    public bool discovered;
    public bool active;
    public bool respawning;
    public float remaining;
    public Color accent;
}

/// <summary>
/// Provides real respawn lifecycle for Dragon, Medusa and Void Titan and exposes objective state
/// to HUD/observer systems. Existing boss AI remains combat authority.
/// </summary>
[DefaultExecutionOrder(-520)]
public class AOGObjectiveLifecycleRuntime : MonoBehaviour
{
    private class Record
    {
        public string key;
        public string displayName;
        public AOGNeutralBossAI boss;
        public Vector3 homePosition;
        public Quaternion homeRotation;
        public Vector3 homeScale;
        public float respawnDuration;
        public float respawnAt;
        public bool scheduled;
        public Color accent;
    }

    private static AOGObjectiveLifecycleRuntime instance;
    private readonly Dictionary<AOGNeutralBossAI,Record> records = new Dictionary<AOGNeutralBossAI,Record>();
    private readonly Dictionary<string,Record> byKey = new Dictionary<string,Record>();
    private float nextDiscovery;
    private float nextTick;

    public static AOGObjectiveLifecycleRuntime Instance => instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindFirstObjectByType<AOGObjectiveLifecycleRuntime>() != null) return;
        GameObject host = new GameObject("AOG_Objective_Lifecycle_Runtime");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGObjectiveLifecycleRuntime>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    private void Update()
    {
        if (Time.unscaledTime >= nextDiscovery)
        {
            nextDiscovery = Time.unscaledTime + 0.5f;
            DiscoverActiveBosses();
        }

        if (Time.unscaledTime < nextTick) return;
        nextTick = Time.unscaledTime + 0.10f;
        TickRecords();
    }

    private void DiscoverActiveBosses()
    {
        foreach (AOGNeutralBossAI boss in AOGWorldRegistry.Bosses)
        {
            if (boss == null || records.ContainsKey(boss)) continue;
            RegisterBoss(boss);
        }
    }

    private void RegisterBoss(AOGNeutralBossAI boss)
    {
        bool titan = boss.GetComponent<AOGVoidTitanMarker>() != null;
        string key = titan ? "TITAN" : boss.bossType == AOGNeutralBossType.Dragon ? "DRAGON" : "MEDUSA";
        Record record = new Record
        {
            key = key,
            displayName = titan ? "VOID TITAN" : key,
            boss = boss,
            homePosition = boss.transform.position,
            homeRotation = boss.transform.rotation,
            homeScale = boss.transform.localScale,
            respawnDuration = titan ? 240f : boss.bossType == AOGNeutralBossType.Dragon ? 180f : 165f,
            accent = titan ? new Color(0.50f,0.18f,0.94f) : boss.bossType == AOGNeutralBossType.Dragon ? new Color(1f,0.42f,0.10f) : new Color(0.66f,0.30f,0.92f)
        };
        records[boss] = record;
        byKey[key] = record;
    }

    private void TickRecords()
    {
        List<AOGNeutralBossAI> stale = null;
        foreach (KeyValuePair<AOGNeutralBossAI,Record> pair in records)
        {
            AOGNeutralBossAI boss = pair.Key;
            Record record = pair.Value;
            if (boss == null)
            {
                if (stale == null) stale = new List<AOGNeutralBossAI>();
                stale.Add(boss);
                continue;
            }

            if (!record.scheduled && boss.IsDead)
            {
                record.scheduled = true;
                record.respawnAt = Time.unscaledTime + record.respawnDuration;
            }

            if (record.scheduled && Time.unscaledTime >= record.respawnAt)
                Respawn(record);
        }

        if (stale != null)
        {
            foreach (AOGNeutralBossAI boss in stale) records.Remove(boss);
        }
    }

    private void Respawn(Record record)
    {
        AOGNeutralBossAI boss = record.boss;
        if (boss == null) return;

        boss.transform.position = record.homePosition;
        boss.transform.rotation = record.homeRotation;
        boss.transform.localScale = record.homeScale;
        boss.hp = boss.maxHp;

        AOGObjectiveRewardTrackerRuntime reward = boss.GetComponent<AOGObjectiveRewardTrackerRuntime>();
        reward?.ResetForRespawn();
        AOGAdvancedBossPatternsRuntime pattern = boss.GetComponent<AOGAdvancedBossPatternsRuntime>();
        pattern?.ResetForRespawn();
        AOGVoidTitanCombatRuntime titan = boss.GetComponent<AOGVoidTitanCombatRuntime>();
        titan?.ResetForRespawn();

        boss.gameObject.SetActive(true);
        record.scheduled = false;
        record.respawnAt = 0f;

        GameObject ring = AOGAbilityVisuals.CreateRing(record.key + "_Respawn", record.homePosition + Vector3.up * 0.06f, record.key == "TITAN" ? 7f : 4.8f, record.accent, 0.15f);
        Destroy(ring,1.2f);
        AOGScoreboardAndAnnouncerRuntime.Instance?.ShowExternalMessage(record.displayName + " HAS RETURNED",record.accent,2.4f);
    }

    public bool TryGetSnapshot(string key,out AOGObjectiveLifecycleSnapshot snapshot)
    {
        Record record;
        if (!byKey.TryGetValue(key,out record) || record == null || record.boss == null)
        {
            snapshot = new AOGObjectiveLifecycleSnapshot
            {
                key=key,
                displayName=key=="TITAN"?"VOID TITAN":key,
                discovered=false,
                active=false,
                respawning=false,
                remaining=0f,
                accent=key=="DRAGON"?new Color(1f,0.42f,0.10f):key=="MEDUSA"?new Color(0.66f,0.30f,0.92f):new Color(0.50f,0.18f,0.94f)
            };
            return false;
        }

        snapshot = new AOGObjectiveLifecycleSnapshot
        {
            key=record.key,
            displayName=record.displayName,
            discovered=true,
            active=!record.boss.IsDead && record.boss.gameObject.activeInHierarchy,
            respawning=record.scheduled,
            remaining=record.scheduled?Mathf.Max(0f,record.respawnAt-Time.unscaledTime):0f,
            accent=record.accent
        };
        return true;
    }
}

/// <summary>
/// Adds real objective state/timers into the existing ObjectivePanel instead of creating a second HUD.
/// </summary>
[DefaultExecutionOrder(2350)]
public class AOGObjectiveTimerHudRuntime : MonoBehaviour
{
    private RectTransform panel;
    private Text dragon;
    private Text medusa;
    private Text titan;
    private float nextBind;
    private float nextRefresh;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindFirstObjectByType<AOGObjectiveTimerHudRuntime>() != null) return;
        GameObject host = new GameObject("AOG_Objective_Timer_HUD_Runtime");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGObjectiveTimerHudRuntime>();
    }

    private void Update()
    {
        if (panel == null && Time.unscaledTime >= nextBind)
        {
            nextBind = Time.unscaledTime + 0.5f;
            TryBind();
        }
        if (panel == null || Time.unscaledTime < nextRefresh) return;
        nextRefresh = Time.unscaledTime + 0.10f;
        Refresh();
    }

    private void TryBind()
    {
        AOGCompetitiveMobaHUDRuntime hud = FindFirstObjectByType<AOGCompetitiveMobaHUDRuntime>();
        if (hud == null) return;
        panel = FindRect(hud.transform,"ObjectivePanel");
        if (panel == null) return;

        Text objective1 = FindText(panel,"Objective1"); if (objective1 != null) objective1.gameObject.SetActive(false);
        Text objective2 = FindText(panel,"Objective2"); if (objective2 != null) objective2.gameObject.SetActive(false);
        Text hint = FindText(panel,"Hint"); if (hint != null) hint.gameObject.SetActive(false);

        dragon = CreateLine("DragonTimer",panel,new Vector2(0f,20f),new Color(1f,0.66f,0.28f));
        medusa = CreateLine("MedusaTimer",panel,new Vector2(0f,-12f),new Color(0.72f,0.48f,1f));
        titan = CreateLine("TitanTimer",panel,new Vector2(0f,-44f),new Color(0.62f,0.42f,1f));
    }

    private void Refresh()
    {
        RefreshLine(dragon,"DRAGON");
        RefreshLine(medusa,"MEDUSA");
        RefreshLine(titan,"TITAN");
    }

    private void RefreshLine(Text label,string key)
    {
        if (label == null) return;
        AOGObjectiveLifecycleSnapshot state = default;
        bool found = AOGObjectiveLifecycleRuntime.Instance != null && AOGObjectiveLifecycleRuntime.Instance.TryGetSnapshot(key,out state);
        if (!found)
        {
            label.text = key == "TITAN" ? "◇ VOID TITAN   LOCKED" : "◇ " + key + "   UNSEEN";
            label.color = new Color(0.46f,0.54f,0.62f);
            return;
        }

        label.color = state.accent;
        if (state.active)
            label.text = "◆ " + state.displayName + "   ACTIVE";
        else if (state.respawning)
            label.text = "◇ " + state.displayName + "   " + Format(state.remaining);
        else
            label.text = "◇ " + state.displayName + "   DOWN";
    }

    private static string Format(float seconds)
    {
        int total = Mathf.Max(0,Mathf.CeilToInt(seconds));
        return (total/60).ToString("00") + ":" + (total%60).ToString("00");
    }

    private static Text CreateLine(string name,Transform parent,Vector2 position,Color color)
    {
        GameObject go = new GameObject(name,typeof(RectTransform),typeof(Text));
        go.transform.SetParent(parent,false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f,0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(220f,26f);
        Text text = go.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 13;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleLeft;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }

    private static RectTransform FindRect(Transform root,string name)
    {
        foreach (RectTransform rect in root.GetComponentsInChildren<RectTransform>(true)) if (rect.name==name) return rect;
        return null;
    }

    private static Text FindText(Transform root,string name)
    {
        foreach (Text text in root.GetComponentsInChildren<Text>(true)) if (text.name==name) return text;
        return null;
    }
}
