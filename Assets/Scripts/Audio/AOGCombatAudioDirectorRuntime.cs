using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Event-driven combat audio director. Ensures the unified 11-hero roster has fallback combat audio,
/// layers light champion-identity impacts on confirmed hit events, and plays team-aware kill/objective stingers.
/// Existing ChampionAudioController remains per-character SFX authority.
/// </summary>
[DefaultExecutionOrder(17000)]
public class AOGCombatAudioDirectorRuntime : MonoBehaviour
{
    private readonly HashSet<AOGCharacterStats> configuredCharacters = new HashSet<AOGCharacterStats>();
    private readonly Dictionary<AOGNeutralBossAI,bool> bossDeathState = new Dictionary<AOGNeutralBossAI,bool>();
    private readonly Dictionary<TowerHealth,bool> towerDeathState = new Dictionary<TowerHealth,bool>();
    private readonly Dictionary<string,AudioClip> clips = new Dictionary<string,AudioClip>();
    private readonly List<AudioSource> worldChannels = new List<AudioSource>();

    private AudioSource stingerSource;
    private int nextWorldChannel;
    private float nextScan;
    private float nextObjectiveScan;
    private float nextBasicImpactTime;
    private float nextAbilityImpactTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindFirstObjectByType<AOGCombatAudioDirectorRuntime>() != null) return;
        GameObject host = new GameObject("AOG_Combat_Audio_Director_Runtime");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGCombatAudioDirectorRuntime>();
    }

    private void Awake()
    {
        BuildSources();
        BuildClipBank();
    }

    private void OnEnable()
    {
        AOGCombatEvents.BasicAttackHit += OnBasicAttackHit;
        AOGCombatEvents.AbilityHit += OnAbilityHit;
        AOGCombatEvents.ChampionDeath += OnChampionDeath;
    }

    private void OnDisable()
    {
        AOGCombatEvents.BasicAttackHit -= OnBasicAttackHit;
        AOGCombatEvents.AbilityHit -= OnAbilityHit;
        AOGCombatEvents.ChampionDeath -= OnChampionDeath;
    }

    private void Update()
    {
        if (Time.unscaledTime >= nextScan)
        {
            nextScan = Time.unscaledTime + 1.0f;
            EnsureRosterAudio();
        }

        if (Time.unscaledTime >= nextObjectiveScan)
        {
            nextObjectiveScan = Time.unscaledTime + 0.25f;
            PollObjectiveAndTowerStates();
        }
    }

    private void BuildSources()
    {
        GameObject stingerObject = new GameObject("AOG_Global_Stinger_Source");
        stingerObject.transform.SetParent(transform,false);
        stingerSource = stingerObject.AddComponent<AudioSource>();
        stingerSource.playOnAwake = false;
        stingerSource.loop = false;
        stingerSource.spatialBlend = 0f;
        stingerSource.volume = 0.72f;

        for (int i=0;i<6;i++)
        {
            GameObject channelObject = new GameObject("AOG_World_Audio_Channel_"+i);
            channelObject.transform.SetParent(transform,false);
            AudioSource source = channelObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0.82f;
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.minDistance = 2.5f;
            source.maxDistance = 38f;
            source.dopplerLevel = 0f;
            source.volume = 0.62f;
            worldChannels.Add(source);
        }
    }

    private void BuildClipBank()
    {
        clips["basic_melee"] = CreateImpact("basic_melee",0.14f,165f,0.78f,31);
        clips["basic_ranged"] = CreateImpact("basic_ranged",0.11f,540f,0.42f,37);
        clips["moon"] = CreateLayeredSweep("moon",0.26f,860f,430f,0.30f,0.18f);
        clips["spirit"] = CreateLayeredSweep("spirit",0.30f,720f,1240f,0.26f,0.24f);
        clips["fire"] = CreateImpact("fire",0.30f,118f,1.05f,43);
        clips["astral"] = CreateLayeredSweep("astral",0.34f,460f,1160f,0.28f,0.20f);
        clips["eclipse"] = CreateImpact("eclipse",0.24f,92f,0.95f,51);
        clips["solar"] = CreateImpact("solar",0.28f,138f,1.20f,57);
        clips["nature"] = CreateLayeredSweep("nature",0.28f,380f,760f,0.24f,0.18f);
        clips["fang"] = CreateImpact("fang",0.18f,205f,0.92f,63);
        clips["shadow"] = CreateLayeredSweep("shadow",0.22f,300f,105f,0.32f,0.20f);

        clips["ally_kill"] = CreateStinger("ally_kill",0.70f,330f,660f,990f,0.42f);
        clips["enemy_kill"] = CreateStinger("enemy_kill",0.70f,440f,260f,150f,0.44f);
        clips["player_death"] = CreateStinger("player_death",0.90f,330f,190f,96f,0.50f);
        clips["dragon"] = CreateStinger("dragon",1.15f,120f,250f,520f,0.52f);
        clips["medusa"] = CreateStinger("medusa",1.10f,260f,390f,180f,0.48f);
        clips["titan"] = CreateStinger("titan",1.35f,75f,130f,310f,0.60f);
        clips["tower_fall"] = CreateStinger("tower_fall",0.80f,150f,110f,70f,0.46f);
    }

    private void EnsureRosterAudio()
    {
        foreach (AOGCharacterStats stats in AOGWorldRegistry.Characters)
        {
            if (stats == null || configuredCharacters.Contains(stats)) continue;
            ChampionAudioController audio = stats.GetComponent<ChampionAudioController>();
            if (audio == null) audio = stats.gameObject.AddComponent<ChampionAudioController>();
            audio.ConfigureProceduralFallback(ResolveArchetype(stats));
            configuredCharacters.Add(stats);
        }
    }

    private ChampionArchetype ResolveArchetype(AOGCharacterStats stats)
    {
        string id = ResolveChampionId(stats.gameObject);
        if (id == "auron" || id == "seris" || id == "mireva") return ChampionArchetype.Guardian;
        if (id == "lyra" || id == "nyra" || id == "pyrelle" || id == "selene") return ChampionArchetype.ArcaneCaster;
        return ChampionArchetype.Duelist;
    }

    private void OnBasicAttackHit(AOGCombatHitEvent hit)
    {
        if (hit.source == null || hit.target == null || Time.unscaledTime < nextBasicImpactTime) return;
        nextBasicImpactTime = Time.unscaledTime + 0.035f;

        ChampionAudioController audio = hit.source.GetComponentInParent<ChampionAudioController>();
        audio?.PlayAttackImpact();

        AOGCharacterStats sourceStats = hit.source.GetComponentInParent<AOGCharacterStats>();
        bool ranged = sourceStats != null && sourceStats.attackRange > 3.8f;
        PlayWorld(ranged ? "basic_ranged" : "basic_melee",hit.target.transform.position,ranged?0.42f:0.52f,ranged?1.08f:0.94f);
    }

    private void OnAbilityHit(AOGCombatHitEvent hit)
    {
        if (hit.source == null || hit.target == null || Time.unscaledTime < nextAbilityImpactTime) return;
        nextAbilityImpactTime = Time.unscaledTime + 0.045f;

        string id = ResolveChampionId(hit.source);
        string key = IdentityAudioKey(id);
        float volume = Mathf.Clamp(0.42f + hit.damage / 950f,0.42f,0.72f);
        float pitch = hit.damage > 260f ? 0.88f : 1.0f;
        PlayWorld(key,hit.target.transform.position,volume,pitch);
    }

    private void OnChampionDeath(AOGChampionDeathEvent data)
    {
        AOGActiveChampion player = AOGPlayerChampionAuthority.CurrentChampion;
        if (player == null || data.victim == null) return;

        if (data.victim.gameObject == player.gameObject)
        {
            PlayStinger("player_death",0.70f,1f);
            return;
        }

        if (data.victim.team != player.GetComponent<AOGCharacterStats>().team)
            PlayStinger("ally_kill",0.54f,1f);
        else
            PlayStinger("enemy_kill",0.50f,0.96f);
    }

    private void PollObjectiveAndTowerStates()
    {
        foreach (AOGNeutralBossAI boss in AOGWorldRegistry.Bosses)
        {
            if (boss == null) continue;
            bool dead = boss.IsDead;
            bool previous;
            if (!bossDeathState.TryGetValue(boss,out previous))
            {
                bossDeathState[boss] = dead;
                continue;
            }
            if (!previous && dead)
            {
                if (boss.GetComponent<AOGVoidTitanMarker>() != null) PlayStinger("titan",0.78f,0.94f);
                else if (boss.bossType == AOGNeutralBossType.Dragon) PlayStinger("dragon",0.70f,1f);
                else PlayStinger("medusa",0.66f,1f);
            }
            bossDeathState[boss] = dead;
        }

        foreach (TowerHealth tower in AOGWorldRegistry.Towers)
        {
            if (tower == null) continue;
            bool dead = tower.hp <= 0f;
            bool previous;
            if (!towerDeathState.TryGetValue(tower,out previous))
            {
                towerDeathState[tower] = dead;
                continue;
            }
            if (!previous && dead)
            {
                PlayWorld("tower_fall",tower.transform.position,0.70f,0.92f);
                Camera.main?.GetComponent<AOGMobaCameraController>()?.AddRandomImpulse(0.22f);
            }
            towerDeathState[tower] = dead;
        }
    }

    private void PlayWorld(string key,Vector3 position,float volume,float pitch)
    {
        AudioClip clip;
        if (!clips.TryGetValue(key,out clip) || clip == null || worldChannels.Count == 0) return;
        AudioSource channel = worldChannels[nextWorldChannel++ % worldChannels.Count];
        channel.transform.position = position;
        channel.pitch = pitch * Random.Range(0.97f,1.03f);
        channel.PlayOneShot(clip,volume);
    }

    private void PlayStinger(string key,float volume,float pitch)
    {
        AudioClip clip;
        if (!clips.TryGetValue(key,out clip) || clip == null || stingerSource == null) return;
        stingerSource.pitch = pitch;
        stingerSource.Stop();
        stingerSource.PlayOneShot(clip,volume);
    }

    private static string ResolveChampionId(GameObject source)
    {
        if (source == null) return string.Empty;
        AOGActiveChampion active = source.GetComponentInParent<AOGActiveChampion>();
        if (active != null && !string.IsNullOrEmpty(active.championId)) return active.championId.ToLowerInvariant();
        AOGTeamMemberIdentity member = source.GetComponentInParent<AOGTeamMemberIdentity>();
        if (member != null && !string.IsNullOrEmpty(member.championId)) return member.championId.ToLowerInvariant();
        return source.name.ToLowerInvariant();
    }

    private static string IdentityAudioKey(string id)
    {
        if (id.Contains("lyra")) return "moon";
        if (id.Contains("nyra") || id.Contains("seris")) return "spirit";
        if (id.Contains("pyrelle")) return "fire";
        if (id.Contains("selene")) return "astral";
        if (id.Contains("kaelith")) return "eclipse";
        if (id.Contains("auron")) return "solar";
        if (id.Contains("mireva")) return "nature";
        if (id.Contains("dravenor")) return "fang";
        if (id.Contains("nocthyr")) return "shadow";
        return "basic_melee";
    }

    private static AudioClip CreateImpact(string name,float length,float frequency,float weight,int seed)
    {
        const int sampleRate = 44100;
        int sampleCount = Mathf.Max(1,Mathf.RoundToInt(length*sampleRate));
        float[] data = new float[sampleCount];
        System.Random rng = new System.Random(seed);
        float phase = 0f;
        for (int i=0;i<sampleCount;i++)
        {
            float t=i/(float)sampleCount;
            float envelope=Mathf.Exp(-9f*t);
            float f=Mathf.Lerp(frequency*1.45f,frequency*0.62f,t);
            phase+=2f*Mathf.PI*f/sampleRate;
            float click=i<sampleRate*0.012f?(float)(rng.NextDouble()*2.0-1.0)*(1f-i/(sampleRate*0.012f)):0f;
            float body=Mathf.Sin(phase)+0.38f*Mathf.Sin(phase*0.5f)+0.16f*Mathf.Sin(phase*2.02f);
            data[i]=Mathf.Clamp((body*0.38f*weight+click*0.32f)*envelope,-0.95f,0.95f);
        }
        return BuildClip(name,data,sampleRate);
    }

    private static AudioClip CreateLayeredSweep(string name,float length,float start,float end,float volume,float shimmer)
    {
        const int sampleRate=44100;
        int sampleCount=Mathf.Max(1,Mathf.RoundToInt(length*sampleRate));
        float[] data=new float[sampleCount];
        float phase=0f;
        for(int i=0;i<sampleCount;i++)
        {
            float t=i/(float)sampleCount;
            float f=Mathf.Lerp(start,end,t*t*(3f-2f*t));
            phase+=2f*Mathf.PI*f/sampleRate;
            float envelope=Mathf.Sin(Mathf.PI*t)*Mathf.Pow(1f-t,0.28f);
            float body=Mathf.Sin(phase)+0.30f*Mathf.Sin(phase*2.01f)+shimmer*Mathf.Sin(phase*4.03f);
            data[i]=body*envelope*volume;
        }
        return BuildClip(name,data,sampleRate);
    }

    private static AudioClip CreateStinger(string name,float length,float f1,float f2,float f3,float volume)
    {
        const int sampleRate=44100;
        int sampleCount=Mathf.Max(1,Mathf.RoundToInt(length*sampleRate));
        float[] data=new float[sampleCount];
        float p1=0f,p2=0f,p3=0f;
        for(int i=0;i<sampleCount;i++)
        {
            float t=i/(float)sampleCount;
            p1+=2f*Mathf.PI*f1/sampleRate;
            p2+=2f*Mathf.PI*f2/sampleRate;
            p3+=2f*Mathf.PI*f3/sampleRate;
            float attack=Mathf.Clamp01(t/0.08f);
            float decay=Mathf.Pow(1f-t,1.6f);
            float envelope=attack*decay;
            float chord=Mathf.Sin(p1)*0.52f+Mathf.Sin(p2)*0.30f+Mathf.Sin(p3)*0.18f;
            data[i]=Mathf.Clamp(chord*envelope*volume,-0.95f,0.95f);
        }
        return BuildClip(name,data,sampleRate);
    }

    private static AudioClip BuildClip(string name,float[] data,int sampleRate)
    {
        AudioClip clip=AudioClip.Create("AOG_Director_"+name,data.Length,1,sampleRate,false);
        clip.SetData(data,0);
        return clip;
    }
}
