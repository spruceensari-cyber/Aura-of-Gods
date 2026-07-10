using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum AOGTemporaryBuffType
{
    AetherFlow,
    InfernalFury,
    DragonResonance,
    MedusaInsight,
    TitanDominion
}

public class AOGJungleBuffRuntime : MonoBehaviour
{
    private class ActiveBuff
    {
        public AOGTemporaryBuffType type;
        public float endTime;
        public float damageDelta;
        public float speedDelta;
        public float originalCooldown;
    }

    private readonly Dictionary<AOGTemporaryBuffType,ActiveBuff> active = new Dictionary<AOGTemporaryBuffType,ActiveBuff>();
    private AOGCharacterStats stats;

    public IReadOnlyDictionary<AOGTemporaryBuffType,float> Remaining
    {
        get
        {
            Dictionary<AOGTemporaryBuffType,float> result = new Dictionary<AOGTemporaryBuffType,float>();
            foreach (var pair in active) result[pair.Key] = Mathf.Max(0f,pair.Value.endTime-Time.time);
            return result;
        }
    }

    private void Awake() { stats = GetComponent<AOGCharacterStats>(); }

    private void Update()
    {
        List<AOGTemporaryBuffType> expired = new List<AOGTemporaryBuffType>();
        foreach (var pair in active)
            if (Time.time >= pair.Value.endTime) expired.Add(pair.Key);
        foreach (AOGTemporaryBuffType type in expired) Remove(type);
    }

    public void Apply(AOGTemporaryBuffType type,float duration)
    {
        if (stats == null) return;
        if (active.ContainsKey(type)) Remove(type);

        ActiveBuff buff = new ActiveBuff { type=type, endTime=Time.time+duration, originalCooldown=stats.attackCooldown };
        switch (type)
        {
            case AOGTemporaryBuffType.AetherFlow:
                stats.attackCooldown = Mathf.Max(0.28f,stats.attackCooldown*0.90f);
                buff.speedDelta = 0.25f; stats.moveSpeed += buff.speedDelta;
                break;
            case AOGTemporaryBuffType.InfernalFury:
                buff.damageDelta = 18f; stats.attackDamage += buff.damageDelta;
                break;
            case AOGTemporaryBuffType.DragonResonance:
                buff.damageDelta = 24f; stats.attackDamage += buff.damageDelta;
                buff.speedDelta = 0.22f; stats.moveSpeed += buff.speedDelta;
                break;
            case AOGTemporaryBuffType.MedusaInsight:
                stats.attackCooldown = Mathf.Max(0.28f,stats.attackCooldown*0.88f);
                buff.speedDelta = 0.32f; stats.moveSpeed += buff.speedDelta;
                break;
            case AOGTemporaryBuffType.TitanDominion:
                buff.damageDelta = 36f; stats.attackDamage += buff.damageDelta;
                buff.speedDelta = 0.45f; stats.moveSpeed += buff.speedDelta;
                stats.attackCooldown = Mathf.Max(0.28f,stats.attackCooldown*0.84f);
                break;
        }
        active[type] = buff;
        Color c = BuffColor(type);
        GameObject ring = AOGAbilityVisuals.CreateRing(type+"_Applied",transform.position+Vector3.up*0.05f,1.8f,c,0.08f);
        Destroy(ring,0.5f);
    }

    private void Remove(AOGTemporaryBuffType type)
    {
        if (!active.TryGetValue(type,out ActiveBuff buff) || stats == null) return;
        stats.attackDamage -= buff.damageDelta;
        stats.moveSpeed = Mathf.Max(1f,stats.moveSpeed-buff.speedDelta);
        if (type==AOGTemporaryBuffType.AetherFlow || type==AOGTemporaryBuffType.MedusaInsight || type==AOGTemporaryBuffType.TitanDominion)
            stats.attackCooldown = Mathf.Max(stats.attackCooldown,buff.originalCooldown);
        active.Remove(type);
    }

    public static Color BuffColor(AOGTemporaryBuffType type)
    {
        switch (type)
        {
            case AOGTemporaryBuffType.AetherFlow: return new Color(0.20f,0.72f,1f);
            case AOGTemporaryBuffType.InfernalFury: return new Color(1f,0.24f,0.08f);
            case AOGTemporaryBuffType.DragonResonance: return new Color(1f,0.48f,0.10f);
            case AOGTemporaryBuffType.MedusaInsight: return new Color(0.62f,0.26f,0.94f);
            default: return new Color(0.42f,0.18f,0.88f);
        }
    }
}

public class AOGNeutralCampBuffRewardRuntime : MonoBehaviour
{
    private AOGNeutralMonsterRuntime monster;
    private void Awake()
    {
        monster = GetComponent<AOGNeutralMonsterRuntime>();
        if (monster != null) monster.Died += OnDied;
    }
    private void OnDestroy() { if (monster != null) monster.Died -= OnDied; }

    private void OnDied(AOGNeutralMonsterRuntime dead,GameObject killer)
    {
        if (killer == null || dead == null) return;
        AOGTemporaryBuffType? type = dead.monsterType == AOGNeutralMonsterType.AetherSentinel ? AOGTemporaryBuffType.AetherFlow :
                                     dead.monsterType == AOGNeutralMonsterType.InfernalBrute ? AOGTemporaryBuffType.InfernalFury : (AOGTemporaryBuffType?)null;
        if (!type.HasValue) return;
        AOGJungleBuffRuntime buff = killer.GetComponentInParent<AOGJungleBuffRuntime>();
        if (buff == null)
        {
            AOGCharacterStats stats = killer.GetComponentInParent<AOGCharacterStats>();
            if (stats != null) buff = stats.gameObject.AddComponent<AOGJungleBuffRuntime>();
        }
        buff?.Apply(type.Value,90f);
    }
}

public class AOGObjectiveRewardTrackerRuntime : MonoBehaviour
{
    private AOGNeutralBossAI boss;
    private MinionTeam? lastHittingTeam;
    private bool rewarded;

    private void Awake() { boss = GetComponent<AOGNeutralBossAI>(); }
    private void OnEnable() { AOGCombatEvents.BasicAttackHit += OnHit; }
    private void OnDisable() { AOGCombatEvents.BasicAttackHit -= OnHit; }

    private void Update()
    {
        if (rewarded || boss == null || !boss.IsDead || !lastHittingTeam.HasValue) return;
        rewarded = true;
        AOGTemporaryBuffType reward = boss.bossType == AOGNeutralBossType.Dragon ? AOGTemporaryBuffType.DragonResonance : AOGTemporaryBuffType.MedusaInsight;
        ApplyTeamReward(lastHittingTeam.Value,reward,120f);
    }

    private void OnHit(AOGCombatHitEvent hit)
    {
        if (boss == null || hit.targetKind != AOGCombatTargetKind.Boss || hit.target == null || hit.source == null) return;
        AOGNeutralBossAI target = hit.target.GetComponentInParent<AOGNeutralBossAI>();
        if (target != boss) return;
        AOGCharacterStats sourceStats = hit.source.GetComponentInParent<AOGCharacterStats>();
        if (sourceStats != null) lastHittingTeam = sourceStats.team;
    }

    public static void ApplyTeamReward(MinionTeam team,AOGTemporaryBuffType type,float duration)
    {
        foreach (AOGTeamMemberIdentity member in FindObjectsByType<AOGTeamMemberIdentity>(FindObjectsInactive.Exclude,FindObjectsSortMode.None))
        {
            if (member == null || member.team != team) continue;
            AOGJungleBuffRuntime buff = member.GetComponent<AOGJungleBuffRuntime>();
            if (buff == null) buff = member.gameObject.AddComponent<AOGJungleBuffRuntime>();
            buff.Apply(type,duration);
        }
    }
}

public class AOGPlayerBuffHudRuntime : MonoBehaviour
{
    private Canvas canvas;
    private Text label;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindFirstObjectByType<AOGPlayerBuffHudRuntime>() != null) return;
        GameObject host = new GameObject("AOG_Player_Buff_HUD");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGPlayerBuffHudRuntime>();
    }

    private void Awake()
    {
        GameObject co = new GameObject("BuffHudCanvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));
        co.transform.SetParent(transform,false);
        canvas = co.GetComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceOverlay; canvas.sortingOrder=2600;
        CanvasScaler scaler=co.GetComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution=new Vector2(1920,1080);
        GameObject text=new GameObject("ActiveBuffs",typeof(RectTransform),typeof(Text)); text.transform.SetParent(co.transform,false);
        RectTransform r=text.GetComponent<RectTransform>(); r.anchorMin=r.anchorMax=new Vector2(0.5f,0f); r.pivot=new Vector2(0.5f,0f); r.anchoredPosition=new Vector2(0f,224f); r.sizeDelta=new Vector2(700f,28f);
        label=text.GetComponent<Text>(); label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); label.fontSize=13; label.fontStyle=FontStyle.Bold; label.alignment=TextAnchor.MiddleCenter; label.color=new Color(0.78f,0.88f,1f); label.raycastTarget=false;
    }

    private void Update()
    {
        AOGActiveChampion player=AOGPlayerChampionAuthority.CurrentChampion;
        AOGJungleBuffRuntime buffs=player!=null?player.GetComponent<AOGJungleBuffRuntime>():null;
        if (buffs==null) { label.text=string.Empty; return; }
        List<string> parts=new List<string>();
        foreach (var pair in buffs.Remaining)
            if (pair.Value>0f) parts.Add(pair.Key.ToString().ToUpperInvariant()+" "+Mathf.CeilToInt(pair.Value)+"s");
        label.text=string.Join("   •   ",parts);
    }
}

public class AOGJungleBuffBootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindFirstObjectByType<AOGJungleBuffBootstrap>()!=null) return;
        GameObject host=new GameObject("AOG_Jungle_Buff_Bootstrap"); DontDestroyOnLoad(host); host.AddComponent<AOGJungleBuffBootstrap>();
    }

    private float nextScan;
    private void Update()
    {
        if (Time.unscaledTime<nextScan) return; nextScan=Time.unscaledTime+0.8f;
        foreach (AOGNeutralMonsterRuntime monster in FindObjectsByType<AOGNeutralMonsterRuntime>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            if (monster!=null && monster.GetComponent<AOGNeutralCampBuffRewardRuntime>()==null) monster.gameObject.AddComponent<AOGNeutralCampBuffRewardRuntime>();
        foreach (AOGNeutralBossAI boss in FindObjectsByType<AOGNeutralBossAI>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            if (boss!=null && boss.GetComponent<AOGObjectiveRewardTrackerRuntime>()==null) boss.gameObject.AddComponent<AOGObjectiveRewardTrackerRuntime>();
    }
}
