using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks major-objective last hitting team and announces kills through the existing announcer.
/// </summary>
public class AOGObjectiveLifeTrackerRuntime : MonoBehaviour
{
    private AOGNeutralBossAI boss;
    private MinionTeam? lastHittingTeam;
    private bool announced;

    private void Awake()
    {
        boss=GetComponent<AOGNeutralBossAI>();
    }

    private void OnEnable()
    {
        AOGCombatEvents.BasicAttackHit+=OnBasicHit;
        AOGCombatEvents.AbilityHit+=OnAbilityHit;
    }

    private void OnDisable()
    {
        AOGCombatEvents.BasicAttackHit-=OnBasicHit;
        AOGCombatEvents.AbilityHit-=OnAbilityHit;
    }

    private void Update()
    {
        if(announced||boss==null||!boss.IsDead)return;
        announced=true;
        string objective=GetComponent<AOGVoidTitanMarker>()!=null?"VOID TITAN":boss.bossType==AOGNeutralBossType.Dragon?"DRAGON":"MEDUSA";
        string team=lastHittingTeam.HasValue?(lastHittingTeam.Value==MinionTeam.Blue?"BLUE TEAM":"RED TEAM"):"A TEAM";
        Color color=lastHittingTeam.HasValue&&lastHittingTeam.Value==MinionTeam.Red?new Color(1f,0.24f,0.28f):new Color(0.24f,0.70f,1f);
        AOGScoreboardAndAnnouncerRuntime.Instance?.ShowExternalMessage(team+" SLEW THE "+objective,color,3.6f);
        SpawnObjectiveDeathBurst(objective,color);
    }

    private void OnBasicHit(AOGCombatHitEvent hit){Track(hit);}
    private void OnAbilityHit(AOGCombatHitEvent hit){Track(hit);}

    private void Track(AOGCombatHitEvent hit)
    {
        if(boss==null||hit.targetKind!=AOGCombatTargetKind.Boss||hit.target==null||hit.source==null)return;
        AOGNeutralBossAI targetBoss=hit.target.GetComponentInParent<AOGNeutralBossAI>();
        if(targetBoss!=boss)return;
        AOGCharacterStats source=hit.source.GetComponentInParent<AOGCharacterStats>();
        if(source!=null)lastHittingTeam=source.team;
    }

    private void SpawnObjectiveDeathBurst(string objective,Color color)
    {
        float radius=objective=="VOID TITAN"?8f:objective=="DRAGON"?6.5f:5.5f;
        for(int i=0;i<4;i++)
        {
            GameObject ring=AOGAbilityVisuals.CreateRing(objective+"_Death_Shock_"+i,transform.position+Vector3.up*0.08f,radius*(0.48f+i*0.18f),color,0.10f+0.025f*i);
            Destroy(ring,0.8f+i*0.12f);
        }
    }
}

/// <summary>
/// Adds a low-health boss enrage state without replacing AOGNeutralBossAI state ownership.
/// </summary>
public class AOGBossEnrageRuntime : MonoBehaviour
{
    private AOGNeutralBossAI boss;
    private bool enraged;
    private float baseCooldown;
    private float baseMoveSpeed;
    private float nextPulse;

    private void Awake()
    {
        boss=GetComponent<AOGNeutralBossAI>();
        if(boss!=null)
        {
            baseCooldown=boss.attackCooldown;
            baseMoveSpeed=boss.moveSpeed;
        }
    }

    private void Update()
    {
        if(boss==null||boss.IsDead)return;
        float ratio=boss.hp/Mathf.Max(1f,boss.maxHp);
        if(!enraged&&ratio<=0.30f)EnterEnrage();
        if(enraged&&Time.time>=nextPulse)
        {
            nextPulse=Time.time+1.25f;
            Color c=GetComponent<AOGVoidTitanMarker>()!=null?new Color(0.56f,0.16f,1f):boss.bossType==AOGNeutralBossType.Dragon?new Color(1f,0.18f,0.03f):new Color(0.70f,0.18f,0.94f);
            GameObject ring=AOGAbilityVisuals.CreateRing("Boss_Enrage_Pulse",transform.position+Vector3.up*0.05f,4.2f,c,0.07f);
            Destroy(ring,0.42f);
        }
    }

    private void EnterEnrage()
    {
        enraged=true;
        boss.attackCooldown=Mathf.Max(0.65f,baseCooldown*0.72f);
        boss.moveSpeed=baseMoveSpeed*1.12f;
        Color c=GetComponent<AOGVoidTitanMarker>()!=null?new Color(0.56f,0.16f,1f):boss.bossType==AOGNeutralBossType.Dragon?new Color(1f,0.18f,0.03f):new Color(0.70f,0.18f,0.94f);
        AOGScoreboardAndAnnouncerRuntime.Instance?.ShowExternalMessage((GetComponent<AOGVoidTitanMarker>()!=null?"VOID TITAN":boss.bossType.ToString().ToUpperInvariant())+" ENRAGED",c,2.8f);
        for(int i=0;i<3;i++)
        {
            GameObject ring=AOGAbilityVisuals.CreateRing("Boss_Enrage_"+i,transform.position+Vector3.up*0.06f,3.2f+i*1.1f,c,0.12f);
            Destroy(ring,0.7f+i*0.1f);
        }
    }
}

/// <summary>
/// Presentation-only tower debris spawned from TowerHealth.TowerDestroyed before the tower object disappears.
/// </summary>
public class AOGStructureDestructionSpectacleRuntime : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if(FindFirstObjectByType<AOGStructureDestructionSpectacleRuntime>()!=null)return;
        GameObject host=new GameObject("AOG_Structure_Destruction_Spectacle");DontDestroyOnLoad(host);host.AddComponent<AOGStructureDestructionSpectacleRuntime>();
    }

    private void OnEnable()
    {
        TowerHealth.TowerDestroyed+=OnTowerDestroyed;
        AOGStrategicLaneSeal.SealDestroyed+=OnSealDestroyed;
        AOGStrategicLaneSeal.SealReactivated+=OnSealReactivated;
    }

    private void OnDisable()
    {
        TowerHealth.TowerDestroyed-=OnTowerDestroyed;
        AOGStrategicLaneSeal.SealDestroyed-=OnSealDestroyed;
        AOGStrategicLaneSeal.SealReactivated-=OnSealReactivated;
    }

    private void OnTowerDestroyed(TowerHealth tower)
    {
        if(tower==null)return;
        Color color=tower.towerTeam==MinionTeam.Blue?new Color(0.18f,0.62f,1f):new Color(1f,0.18f,0.10f);
        SpawnDebris(tower.transform.position,color,10,1.5f,3.2f);
        for(int i=0;i<3;i++)
        {
            GameObject ring=AOGAbilityVisuals.CreateRing("Tower_Collapse_Shockwave_"+i,tower.transform.position+Vector3.up*0.08f,2.4f+i*1.8f,color,0.14f);
            Destroy(ring,0.65f+i*0.14f);
        }
        Camera.main?.GetComponent<AOGMobaCameraController>()?.AddRandomImpulse(0.46f);
    }

    private void OnSealDestroyed(AOGStrategicLaneSeal seal)
    {
        if(seal==null)return;
        MinionTeam pressureTeam=seal.team==MinionTeam.Blue?MinionTeam.Red:MinionTeam.Blue;
        Color color=pressureTeam==MinionTeam.Blue?new Color(0.22f,0.68f,1f):new Color(1f,0.24f,0.28f);
        AOGScoreboardAndAnnouncerRuntime.Instance?.ShowExternalMessage((pressureTeam==MinionTeam.Blue?"BLUE":"RED")+" LANE PRESSURE ASCENDING — "+seal.lane.ToString().ToUpperInvariant(),color,3.4f);
        SpawnDebris(seal.transform.position,color,7,1.1f,2.4f);
    }

    private void OnSealReactivated(AOGStrategicLaneSeal seal)
    {
        if(seal==null)return;
        Color color=seal.team==MinionTeam.Blue?new Color(0.22f,0.68f,1f):new Color(1f,0.24f,0.28f);
        AOGScoreboardAndAnnouncerRuntime.Instance?.ShowExternalMessage(seal.lane.ToString().ToUpperInvariant()+" SEAL REACTIVATED",color,2.6f);
    }

    private static void SpawnDebris(Vector3 origin,Color color,int count,float horizontal,float vertical)
    {
        for(int i=0;i<count;i++)
        {
            float a=i*Mathf.PI*2f/Mathf.Max(1,count)+Random.Range(-0.25f,0.25f);
            GameObject shard=GameObject.CreatePrimitive(PrimitiveType.Cube);
            shard.name="Structure_Debris_"+i;
            shard.transform.position=origin+Vector3.up*Random.Range(0.6f,2.4f);
            shard.transform.localScale=new Vector3(Random.Range(0.12f,0.35f),Random.Range(0.22f,0.70f),Random.Range(0.12f,0.35f));
            shard.transform.rotation=Random.rotation;
            Renderer renderer=shard.GetComponent<Renderer>();if(renderer!=null)renderer.sharedMaterial=Emissive(Color.Lerp(new Color(0.08f,0.08f,0.10f),color,0.28f),1.4f);
            Collider c=shard.GetComponent<Collider>();if(c!=null)Destroy(c);
            AOGDeathFragmentMotionRuntime motion=shard.AddComponent<AOGDeathFragmentMotionRuntime>();motion.velocity=new Vector3(Mathf.Cos(a)*horizontal,Random.Range(vertical*0.65f,vertical),Mathf.Sin(a)*horizontal);
            Destroy(shard,1.6f);
        }
    }

    private static Material Emissive(Color color,float strength)
    {
        Shader shader=Shader.Find("Universal Render Pipeline/Lit");if(shader==null)shader=Shader.Find("Standard");Material mat=new Material(shader){color=color};if(mat.HasProperty("_EmissionColor")){mat.EnableKeyword("_EMISSION");mat.SetColor("_EmissionColor",color*strength);}return mat;
    }
}

/// <summary>
/// Adds a stronger final Nexus pre-destruction and explosion presentation while the existing
/// AOGNexusCore remains match authority.
/// </summary>
public class AOGNexusFinalSpectacleRuntime : MonoBehaviour
{
    private AOGNexusCore nexus;
    private bool exploded;
    private float nextCriticalPulse;

    private void Awake(){nexus=GetComponent<AOGNexusCore>();}

    private void Update()
    {
        if(nexus==null)return;
        float ratio=nexus.hp/Mathf.Max(1f,nexus.maxHp);
        if(!nexus.IsDestroyed&&ratio<=0.20f&&Time.time>=nextCriticalPulse)
        {
            nextCriticalPulse=Time.time+0.65f;
            Color c=nexus.team==MinionTeam.Blue?new Color(0.24f,0.72f,1f):new Color(1f,0.22f,0.28f);
            GameObject ring=AOGAbilityVisuals.CreateRing("Nexus_Critical_Pulse",transform.position+Vector3.up*0.1f,4.5f,c,0.12f);Destroy(ring,0.5f);
        }

        if(!exploded&&nexus.IsDestroyed)
        {
            exploded=true;
            StartCoroutine(FinalBurst());
        }
    }

    private IEnumerator FinalBurst()
    {
        Color c=nexus.team==MinionTeam.Blue?new Color(0.24f,0.72f,1f):new Color(1f,0.22f,0.28f);
        for(int wave=0;wave<5;wave++)
        {
            GameObject ring=AOGAbilityVisuals.CreateRing("Nexus_Final_Wave_"+wave,transform.position+Vector3.up*0.08f,3.8f+wave*2.3f,c,0.16f);
            Destroy(ring,1f);
            yield return new WaitForSeconds(0.14f);
        }
        Camera.main?.GetComponent<AOGMobaCameraController>()?.AddRandomImpulse(0.75f);
    }
}

public class AOGObjectiveAndStructureSpectacleBootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if(FindFirstObjectByType<AOGObjectiveAndStructureSpectacleBootstrap>()!=null)return;
        GameObject host=new GameObject("AOG_Objective_Structure_Spectacle_Bootstrap");DontDestroyOnLoad(host);host.AddComponent<AOGObjectiveAndStructureSpectacleBootstrap>();
    }

    private float nextScan;
    private void Update()
    {
        if(Time.unscaledTime<nextScan)return;nextScan=Time.unscaledTime+0.75f;
        foreach(AOGNeutralBossAI boss in FindObjectsByType<AOGNeutralBossAI>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {
            if(boss==null)continue;
            if(boss.GetComponent<AOGObjectiveLifeTrackerRuntime>()==null)boss.gameObject.AddComponent<AOGObjectiveLifeTrackerRuntime>();
            if(boss.GetComponent<AOGBossEnrageRuntime>()==null)boss.gameObject.AddComponent<AOGBossEnrageRuntime>();
        }
        foreach(AOGNexusCore nexus in FindObjectsByType<AOGNexusCore>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            if(nexus!=null&&nexus.GetComponent<AOGNexusFinalSpectacleRuntime>()==null)nexus.gameObject.AddComponent<AOGNexusFinalSpectacleRuntime>();
    }
}
