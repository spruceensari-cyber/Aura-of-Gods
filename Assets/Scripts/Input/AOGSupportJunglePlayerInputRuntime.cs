using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Human-player Q/W/E/R control for Seris, Mireva, Dravenor and Nocthyr.
/// When attached to the authoritative player champion, the bot-decision kit is disabled.
/// </summary>
public class AOGSupportJunglePlayerInputRuntime : MonoBehaviour, IAOGAbilityCooldownProvider
{
    public AOGSupportJungleHeroType heroType;

    private AOGActiveChampion champion;
    private AOGCharacterStats stats;
    private ChampionPresentationController presentation;
    private AOGCombatStatBlock statBlock;
    private float nextQ;
    private float nextW;
    private float nextE;
    private float nextR;
    private float empoweredUntil;
    private int huntStacks;

    public string ChampionDisplayName => heroType == AOGSupportJungleHeroType.Seris ? "SERIS" : heroType == AOGSupportJungleHeroType.Mireva ? "MIREVA" : heroType == AOGSupportJungleHeroType.Dravenor ? "DRAVENOR" : "NOCTHYR";
    public string ChampionRoleName => heroType == AOGSupportJungleHeroType.Seris ? "AETHER VEIL" : heroType == AOGSupportJungleHeroType.Mireva ? "BLOOM WARDEN" : heroType == AOGSupportJungleHeroType.Dravenor ? "FANG STALKER" : "SHADE TRACKER";

    private void Awake()
    {
        champion = GetComponent<AOGActiveChampion>();
        stats = GetComponent<AOGCharacterStats>();
        presentation = GetComponent<ChampionPresentationController>();
        statBlock = GetComponent<AOGCombatStatBlock>();
        AOGSupportJungleHeroKitRuntime aiKit = GetComponent<AOGSupportJungleHeroKitRuntime>();
        if (aiKit != null) aiKit.enabled = false;
    }

    private void OnEnable()
    {
        AOGCombatEvents.ChampionDeath += OnChampionDeath;
    }

    private void OnDisable()
    {
        AOGCombatEvents.ChampionDeath -= OnChampionDeath;
    }

    private void Update()
    {
        if (champion == null || !champion.IsActiveChampion || stats == null || stats.IsDead) return;
        if (AOGPlayerChampionAuthority.CurrentChampion != champion) return;
        if (AOGMatchDirector.Instance == null || AOGMatchDirector.Instance.State != AOGMatchState.Playing) return;

        if (AOGInputBridge.KeyPressedThisFrame(KeyCode.Q)) CastQ();
        if (AOGInputBridge.KeyPressedThisFrame(KeyCode.W)) CastW();
        if (AOGInputBridge.KeyPressedThisFrame(KeyCode.E)) CastE();
        if (AOGInputBridge.KeyPressedThisFrame(KeyCode.R)) CastR();
    }

    public void CastQ()
    {
        if (Time.time < nextQ) return;
        switch (heroType)
        {
            case AOGSupportJungleHeroType.Seris:
            {
                AOGCharacterStats enemy = ResolveEnemyTarget(9f);
                if (enemy == null) return;
                nextQ = Time.time + QCooldown;
                presentation?.PlayAbility(0);
                GameObject beam=AOGAbilityVisuals.CreateBeam("Seris_Player_Aether_Thread",transform.position+Vector3.up*1.2f,enemy.transform.position+Vector3.up*0.9f,new Color(0.24f,0.88f,0.95f),0.16f);
                Destroy(beam,0.32f);
                Deal(enemy,ScaleMagic(82f,0.38f),"seris_player_q");
                StartCoroutine(TemporarySlow(enemy,0.72f,1.4f));
                break;
            }
            case AOGSupportJungleHeroType.Mireva:
            {
                AOGCharacterStats enemy=ResolveEnemyTarget(10f); if(enemy==null)return;
                nextQ=Time.time+QCooldown; presentation?.PlayAbility(0);
                GameObject beam=AOGAbilityVisuals.CreateBeam("Mireva_Player_Thorn_Lance",transform.position+Vector3.up*1.1f,enemy.transform.position+Vector3.up*0.8f,new Color(0.18f,0.82f,0.42f),0.13f);Destroy(beam,0.28f);
                Deal(enemy,ScaleMagic(92f,0.44f),"mireva_player_q");
                break;
            }
            case AOGSupportJungleHeroType.Dravenor:
                CastDravenorQ();
                break;
            default:
            {
                AOGCharacterStats enemy=ResolveEnemyTarget(4.5f);if(enemy==null)return;
                nextQ=Time.time+QCooldown;presentation?.PlayAbility(0);
                Deal(enemy,ScalePhysical(118f,0.72f),"nocthyr_player_q");
                GameObject beam=AOGAbilityVisuals.CreateBeam("Nocthyr_Player_Shade_Cut",transform.position+Vector3.up*1.0f,enemy.transform.position+Vector3.up*0.8f,new Color(0.28f,0.18f,0.76f),0.22f);Destroy(beam,0.24f);
                break;
            }
        }
    }

    public void CastW()
    {
        if (Time.time < nextW) return;
        switch (heroType)
        {
            case AOGSupportJungleHeroType.Seris:
            {
                AOGCharacterStats ally=ResolveAllyTarget(10f)??stats;
                nextW=Time.time+WCooldown;presentation?.PlayAbility(1);
                ApplyShield(ally,180f+ScaleMagic(0f,0.40f),4.5f,new Color(0.24f,0.88f,0.95f));
                break;
            }
            case AOGSupportJungleHeroType.Mireva:
            {
                nextW=Time.time+WCooldown;presentation?.PlayAbility(1);
                StartCoroutine(MirevaGarden(ResolveGroundPoint(7f)));
                break;
            }
            case AOGSupportJungleHeroType.Dravenor:
            {
                nextW=Time.time+WCooldown;presentation?.PlayAbility(1);
                StartCoroutine(TemporarySpeed(0.85f+0.08f*huntStacks,4.5f,new Color(0.92f,0.28f,0.10f),"Dravenor_Player_Tracking"));
                empoweredUntil=Time.time+4.5f;
                break;
            }
            default:
            {
                nextW=Time.time+WCooldown;presentation?.PlayAbility(1);
                StartCoroutine(TemporarySpeed(1.2f,3.8f,new Color(0.24f,0.16f,0.68f),"Nocthyr_Player_Umbral_Veil"));
                empoweredUntil=Time.time+3.8f;
                break;
            }
        }
    }

    public void CastE()
    {
        if (Time.time < nextE) return;
        switch (heroType)
        {
            case AOGSupportJungleHeroType.Seris:
                nextE=Time.time+ECooldown;StartCoroutine(SerisPeelPulse());break;
            case AOGSupportJungleHeroType.Mireva:
                nextE=Time.time+ECooldown;StartCoroutine(MirevaRootField(ResolveGroundPoint(7.5f)));break;
            case AOGSupportJungleHeroType.Dravenor:
            {
                AOGCharacterStats enemy=ResolveEnemyTarget(11f);if(enemy==null)return;
                nextE=Time.time+ECooldown;StartCoroutine(DravenorLeap(enemy));break;
            }
            default:
            {
                AOGCharacterStats enemy=ResolveEnemyTarget(11f);if(enemy==null)return;
                nextE=Time.time+ECooldown;StartCoroutine(NocthyrShadowstep(enemy));break;
            }
        }
    }

    public void CastR()
    {
        if (Time.time < nextR) return;
        switch (heroType)
        {
            case AOGSupportJungleHeroType.Seris:
                nextR=Time.time+RCooldown;StartCoroutine(SerisSanctuary(ResolveGroundPoint(6f)));break;
            case AOGSupportJungleHeroType.Mireva:
                nextR=Time.time+RCooldown;StartCoroutine(MirevaRenewal());break;
            case AOGSupportJungleHeroType.Dravenor:
            {
                AOGCharacterStats enemy=ResolveEnemyTarget(13f);if(enemy==null)return;
                nextR=Time.time+RCooldown;StartCoroutine(DravenorApexHunt(enemy));break;
            }
            default:
            {
                AOGCharacterStats enemy=ResolveEnemyTarget(13f);if(enemy==null)return;
                nextR=Time.time+RCooldown;StartCoroutine(NocthyrNightfall(enemy));break;
            }
        }
    }

    private void CastDravenorQ()
    {
        AOGNeutralMonsterRuntime monster=ResolveMonsterTarget(7f);
        if(monster!=null)
        {
            nextQ=Time.time+QCooldown;presentation?.PlayAbility(0);
            float missing=1f-monster.hp/Mathf.Max(1f,monster.maxHp);
            monster.TakeDamage(125f+200f*missing,gameObject);huntStacks=Mathf.Clamp(huntStacks+1,0,6);FangImpact(monster.transform.position);return;
        }
        AOGNeutralBossAI boss=ResolveBossTarget(8f);
        if(boss!=null)
        {
            nextQ=Time.time+QCooldown;presentation?.PlayAbility(0);
            float missing=1f-boss.hp/Mathf.Max(1f,boss.maxHp);
            boss.TakeDamage(95f+145f*missing,gameObject);FangImpact(boss.transform.position);return;
        }
        AOGCharacterStats enemy=ResolveEnemyTarget(3.5f);
        if(enemy==null)return;
        nextQ=Time.time+QCooldown;presentation?.PlayAbility(0);Deal(enemy,ScalePhysical(108f,0.65f),"dravenor_player_q");FangImpact(enemy.transform.position);
    }

    private IEnumerator SerisPeelPulse()
    {
        presentation?.PlayAbility(2);
        Vector3 center=transform.position;
        for(int i=0;i<2;i++){GameObject ring=AOGAbilityVisuals.CreateRing("Seris_Player_Peel_"+i,center+Vector3.up*0.05f,2.7f+i*1.4f,new Color(0.28f,0.82f,1f),0.10f);Destroy(ring,0.48f);}
        yield return new WaitForSeconds(0.22f);
        foreach(AOGCharacterStats enemy in EnemiesInRadius(center,4.9f)){Deal(enemy,ScaleMagic(60f,0.22f),"seris_player_e");StartCoroutine(TemporarySlow(enemy,0.55f,1.8f));}
    }

    private IEnumerator SerisSanctuary(Vector3 center)
    {
        presentation?.PlayAbility(3);
        GameObject ring=AOGAbilityVisuals.CreateRing("Seris_Player_Sanctuary",center+Vector3.up*0.05f,5.2f,new Color(0.32f,0.92f,1f),0.18f);Destroy(ring,5.5f);
        float end=Time.time+5f;
        while(Time.time<end){foreach(AOGCharacterStats ally in AlliesInRadius(center,5.2f))ally.hp=Mathf.Min(ally.maxHp,ally.hp+34f+ScaleMagic(0f,0.06f));yield return new WaitForSeconds(0.75f);}
    }

    private IEnumerator MirevaGarden(Vector3 center)
    {
        GameObject ring=AOGAbilityVisuals.CreateRing("Mireva_Player_Garden",center+Vector3.up*0.05f,4.5f,new Color(0.22f,0.90f,0.46f),0.14f);Destroy(ring,5.3f);
        float end=Time.time+4.8f;
        while(Time.time<end){foreach(AOGCharacterStats ally in AlliesInRadius(center,4.5f))ally.hp=Mathf.Min(ally.maxHp,ally.hp+42f+ScaleMagic(0f,0.05f));yield return new WaitForSeconds(0.8f);}
    }

    private IEnumerator MirevaRootField(Vector3 center)
    {
        presentation?.PlayAbility(2);
        GameObject telegraph=AOGAbilityVisuals.CreateRing("Mireva_Player_Root_Telegraph",center+Vector3.up*0.05f,3.7f,new Color(0.16f,0.74f,0.32f),0.11f);
        yield return new WaitForSeconds(0.55f);
        foreach(AOGCharacterStats enemy in EnemiesInRadius(center,3.7f)){Deal(enemy,ScaleMagic(70f,0.30f),"mireva_player_e");StartCoroutine(TemporarySlow(enemy,0.18f,1.25f));}
        if(telegraph!=null)Destroy(telegraph,0.3f);
    }

    private IEnumerator MirevaRenewal()
    {
        presentation?.PlayAbility(3);
        for(int pulse=0;pulse<4;pulse++)
        {
            GameObject ring=AOGAbilityVisuals.CreateRing("Mireva_Player_Renewal_"+pulse,transform.position+Vector3.up*0.05f,3.8f+pulse*0.9f,new Color(0.24f,0.96f,0.52f),0.13f);Destroy(ring,0.65f);
            foreach(AOGCharacterStats ally in AlliesInRadius(transform.position,7f)){ally.hp=Mathf.Min(ally.maxHp,ally.hp+96f+ScaleMagic(0f,0.08f));ApplyShield(ally,70f,3f,new Color(0.30f,0.92f,0.50f));}
            yield return new WaitForSeconds(0.65f);
        }
    }

    private IEnumerator DravenorLeap(AOGCharacterStats enemy)
    {
        if(enemy==null)yield break;presentation?.PlayAbility(2);
        Vector3 start=transform.position;Vector3 direction=enemy.transform.position-start;direction.y=0f;Vector3 end=enemy.transform.position-direction.normalized*1.4f;
        float elapsed=0f;while(elapsed<0.24f){elapsed+=Time.deltaTime;transform.position=Vector3.Lerp(start,end,Mathf.Clamp01(elapsed/0.24f));yield return null;}
        if(enemy!=null&&!enemy.IsDead){Deal(enemy,ScalePhysical(125f+(Time.time<empoweredUntil?35f:0f),0.55f),"dravenor_player_e");StartCoroutine(TemporarySlow(enemy,0.70f,1f));FangImpact(enemy.transform.position);}
    }

    private IEnumerator DravenorApexHunt(AOGCharacterStats enemy)
    {
        if(enemy==null)yield break;presentation?.PlayAbility(3);
        GameObject mark=AOGAbilityVisuals.CreateRing("Dravenor_Player_Apex_Mark",enemy.transform.position+Vector3.up*0.05f,2.2f,new Color(1f,0.18f,0.06f),0.14f);
        yield return new WaitForSeconds(0.45f);
        if(enemy!=null&&!enemy.IsDead){transform.position=enemy.transform.position-enemy.transform.forward*1.3f;float missing=1f-enemy.hp/Mathf.Max(1f,enemy.maxHp);Deal(enemy,ScalePhysical(180f+260f*missing,0.80f),"dravenor_player_r");}
        if(mark!=null)Destroy(mark,0.3f);
    }

    private IEnumerator NocthyrShadowstep(AOGCharacterStats enemy)
    {
        if(enemy==null)yield break;presentation?.PlayAbility(2);
        Vector3 start=transform.position;Vector3 end=enemy.transform.position-enemy.transform.forward*1.2f;
        GameObject beam=AOGAbilityVisuals.CreateBeam("Nocthyr_Player_Shadowstep",start+Vector3.up*0.4f,end+Vector3.up*0.4f,new Color(0.22f,0.16f,0.68f),0.32f);
        yield return new WaitForSeconds(0.12f);transform.position=end;if(beam!=null)Destroy(beam,0.25f);
        if(enemy!=null&&!enemy.IsDead)Deal(enemy,ScalePhysical(100f+(Time.time<empoweredUntil?45f:0f),0.62f),"nocthyr_player_e");
    }

    private IEnumerator NocthyrNightfall(AOGCharacterStats enemy)
    {
        if(enemy==null)yield break;presentation?.PlayAbility(3);
        GameObject outer=AOGAbilityVisuals.CreateRing("Nocthyr_Player_Nightfall",enemy.transform.position+Vector3.up*0.05f,3.8f,new Color(0.20f,0.12f,0.62f),0.18f);
        yield return new WaitForSeconds(0.55f);
        if(enemy!=null&&!enemy.IsDead){transform.position=enemy.transform.position-enemy.transform.forward*1.0f;float missing=1f-enemy.hp/Mathf.Max(1f,enemy.maxHp);Deal(enemy,ScalePhysical(210f+230f*missing,0.90f),"nocthyr_player_r");}
        if(outer!=null)Destroy(outer,0.35f);
    }

    private IEnumerator TemporarySlow(AOGCharacterStats target,float multiplier,float duration)
    {
        if(target==null)yield break;float original=target.moveSpeed;target.moveSpeed=Mathf.Max(1f,original*multiplier);yield return new WaitForSeconds(duration);if(target!=null)target.moveSpeed=Mathf.Max(target.moveSpeed,original);
    }

    private IEnumerator TemporarySpeed(float bonus,float duration,Color color,string effectName)
    {
        stats.moveSpeed+=bonus;GameObject ring=AOGAbilityVisuals.CreateRing(effectName,transform.position+Vector3.up*0.05f,1.9f,color,0.08f);Destroy(ring,duration);
        yield return new WaitForSeconds(duration);if(stats!=null)stats.moveSpeed=Mathf.Max(1f,stats.moveSpeed-bonus);
    }

    private void ApplyShield(AOGCharacterStats ally,float amount,float duration,Color color)
    {
        if(ally==null)return;AOGTemporarySupportShieldRuntime shield=ally.GetComponent<AOGTemporarySupportShieldRuntime>();if(shield==null)shield=ally.gameObject.AddComponent<AOGTemporarySupportShieldRuntime>();shield.Apply(amount,duration,color);
    }

    private void Deal(AOGCharacterStats target,float amount,string abilityId)
    {
        if(target==null||target.IsDead||target.team==stats.team)return;target.TakeDamage(amount,gameObject);AOGCombatEvents.RaiseAbilityHit(new AOGCombatHitEvent{source=gameObject,target=target.gameObject,damage=amount,basicAttack=false,abilityId=abilityId,targetKind=AOGCombatTargetKind.Champion});
    }

    private void FangImpact(Vector3 point)
    {
        for(int i=0;i<2;i++){GameObject ring=AOGAbilityVisuals.CreateRing("Player_Fang_Impact_"+i,point+Vector3.up*0.05f,1.2f+i*0.8f,new Color(0.94f,0.30f,0.08f),0.10f);Destroy(ring,0.38f);}
    }

    private AOGCharacterStats ResolveEnemyTarget(float range)
    {
        AOGCharacterStats pointer=EnemyHeroUnderPointer();if(pointer!=null&&FlatDistance(transform.position,pointer.transform.position)<=range)return pointer;
        AOGCharacterStats best=null;float bestD=range;foreach(AOGCharacterStats hero in FindObjectsByType<AOGCharacterStats>(FindObjectsInactive.Exclude,FindObjectsSortMode.None)){if(hero==null||hero==stats||hero.IsDead||hero.team==stats.team)continue;float d=FlatDistance(transform.position,hero.transform.position);if(d<bestD){best=hero;bestD=d;}}return best;
    }

    private AOGCharacterStats ResolveAllyTarget(float range)
    {
        AOGCharacterStats pointer=AllyHeroUnderPointer();if(pointer!=null&&FlatDistance(transform.position,pointer.transform.position)<=range)return pointer;
        AOGCharacterStats best=null;float bestRatio=1.01f;foreach(AOGCharacterStats hero in FindObjectsByType<AOGCharacterStats>(FindObjectsInactive.Exclude,FindObjectsSortMode.None)){if(hero==null||hero.IsDead||hero.team!=stats.team)continue;float d=FlatDistance(transform.position,hero.transform.position);if(d>range)continue;float ratio=hero.hp/Mathf.Max(1f,hero.maxHp);if(ratio<bestRatio){best=hero;bestRatio=ratio;}}return best;
    }

    private AOGCharacterStats EnemyHeroUnderPointer(){return HeroUnderPointer(false);}
    private AOGCharacterStats AllyHeroUnderPointer(){return HeroUnderPointer(true);}

    private AOGCharacterStats HeroUnderPointer(bool allied)
    {
        Camera cam=Camera.main;if(cam==null)return null;Ray ray=cam.ScreenPointToRay(AOGInputBridge.PointerPosition);RaycastHit[] hits=Physics.RaycastAll(ray,1200f,~0,QueryTriggerInteraction.Ignore);System.Array.Sort(hits,(a,b)=>a.distance.CompareTo(b.distance));
        foreach(RaycastHit hit in hits){AOGCharacterStats hero=hit.collider.GetComponentInParent<AOGCharacterStats>();if(hero==null||hero.IsDead)continue;if(allied&&hero.team==stats.team)return hero;if(!allied&&hero!=stats&&hero.team!=stats.team)return hero;}return null;
    }

    private Vector3 ResolveGroundPoint(float fallbackDistance)
    {
        Camera cam=Camera.main;if(cam!=null){Ray ray=cam.ScreenPointToRay(AOGInputBridge.PointerPosition);RaycastHit hit;if(Physics.Raycast(ray,out hit,1200f,~0,QueryTriggerInteraction.Ignore)){Vector3 p=hit.point;p.y=transform.position.y;return p;}}
        return transform.position+transform.forward*fallbackDistance;
    }

    private AOGNeutralMonsterRuntime ResolveMonsterTarget(float range)
    {
        AOGNeutralMonsterRuntime best=null;float bestD=range;foreach(AOGNeutralMonsterRuntime m in FindObjectsByType<AOGNeutralMonsterRuntime>(FindObjectsInactive.Exclude,FindObjectsSortMode.None)){if(m==null||m.IsDead)continue;float d=FlatDistance(transform.position,m.transform.position);if(d<bestD){best=m;bestD=d;}}return best;
    }

    private AOGNeutralBossAI ResolveBossTarget(float range)
    {
        AOGNeutralBossAI best=null;float bestD=range;foreach(AOGNeutralBossAI b in FindObjectsByType<AOGNeutralBossAI>(FindObjectsInactive.Exclude,FindObjectsSortMode.None)){if(b==null||b.IsDead)continue;float d=FlatDistance(transform.position,b.transform.position);if(d<bestD){best=b;bestD=d;}}return best;
    }

    private List<AOGCharacterStats> AlliesInRadius(Vector3 center,float radius)
    {
        List<AOGCharacterStats> result=new List<AOGCharacterStats>();foreach(AOGCharacterStats hero in FindObjectsByType<AOGCharacterStats>(FindObjectsInactive.Exclude,FindObjectsSortMode.None)){if(hero==null||hero.IsDead||hero.team!=stats.team)continue;if(FlatDistance(center,hero.transform.position)<=radius)result.Add(hero);}return result;
    }

    private List<AOGCharacterStats> EnemiesInRadius(Vector3 center,float radius)
    {
        List<AOGCharacterStats> result=new List<AOGCharacterStats>();foreach(AOGCharacterStats hero in FindObjectsByType<AOGCharacterStats>(FindObjectsInactive.Exclude,FindObjectsSortMode.None)){if(hero==null||hero==stats||hero.IsDead||hero.team==stats.team)continue;if(FlatDistance(center,hero.transform.position)<=radius)result.Add(hero);}return result;
    }

    private float ScaleMagic(float baseAmount,float ratio){return baseAmount+(statBlock!=null?statBlock.abilityPower*ratio:0f);}
    private float ScalePhysical(float baseAmount,float ratio){return baseAmount+stats.attackDamage*ratio;}

    private void OnChampionDeath(AOGChampionDeathEvent data)
    {
        if(heroType!=AOGSupportJungleHeroType.Nocthyr||data.killer==null)return;
        if(data.killer==gameObject||data.killer.transform.IsChildOf(transform)){nextQ=Mathf.Min(nextQ,Time.time+0.8f);nextE=Mathf.Min(nextE,Time.time+1.2f);nextW=Mathf.Min(nextW,Time.time+1.5f);GameObject ring=AOGAbilityVisuals.CreateRing("Nocthyr_Player_Kill_Reset",transform.position+Vector3.up*0.05f,2.2f,new Color(0.30f,0.18f,0.82f),0.10f);Destroy(ring,0.5f);}
    }

    private float QCooldown=>heroType==AOGSupportJungleHeroType.Seris?5.5f:heroType==AOGSupportJungleHeroType.Mireva?5.8f:heroType==AOGSupportJungleHeroType.Dravenor?5f:4.8f;
    private float WCooldown=>heroType==AOGSupportJungleHeroType.Seris?8.5f:heroType==AOGSupportJungleHeroType.Mireva?9f:heroType==AOGSupportJungleHeroType.Dravenor?12f:13f;
    private float ECooldown=>heroType==AOGSupportJungleHeroType.Seris?11f:heroType==AOGSupportJungleHeroType.Mireva?12f:heroType==AOGSupportJungleHeroType.Dravenor?10f:9.5f;
    private float RCooldown=>heroType==AOGSupportJungleHeroType.Seris?52f:heroType==AOGSupportJungleHeroType.Mireva?58f:heroType==AOGSupportJungleHeroType.Dravenor?46f:48f;

    public string GetAbilityName(int slot)
    {
        if(heroType==AOGSupportJungleHeroType.Seris)return slot==0?"AETHER THREAD":slot==1?"VEIL SHIELD":slot==2?"RESCUE PULSE":"SANCTUARY";
        if(heroType==AOGSupportJungleHeroType.Mireva)return slot==0?"THORN LANCE":slot==1?"HEALING GARDEN":slot==2?"ROOT FIELD":"VERDANT RENEWAL";
        if(heroType==AOGSupportJungleHeroType.Dravenor)return slot==0?"FANG EXECUTE":slot==1?"TRACKING STATE":slot==2?"FANG LEAP":"APEX HUNT";
        return slot==0?"SHADE CUT":slot==1?"UMBRAL VEIL":slot==2?"SHADOWSTEP":"NIGHTFALL";
    }

    public float GetAbilityCooldownDuration(int slot)=>slot==0?QCooldown:slot==1?WCooldown:slot==2?ECooldown:RCooldown;
    public float GetAbilityCooldownRatio(int slot){float next=slot==0?nextQ:slot==1?nextW:slot==2?nextE:nextR;return Mathf.Clamp01((next-Time.time)/Mathf.Max(0.1f,GetAbilityCooldownDuration(slot)));}

    private static float FlatDistance(Vector3 a,Vector3 b){a.y=0f;b.y=0f;return Vector3.Distance(a,b);}
}

public class AOGSupportJunglePlayerInputBootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if(FindFirstObjectByType<AOGSupportJunglePlayerInputBootstrap>()!=null)return;
        GameObject host=new GameObject("AOG_Support_Jungle_Player_Input_Bootstrap");DontDestroyOnLoad(host);host.AddComponent<AOGSupportJunglePlayerInputBootstrap>();
    }

    private float nextScan;
    private void Update()
    {
        if(Time.unscaledTime<nextScan)return;nextScan=Time.unscaledTime+0.35f;
        AOGActiveChampion player=AOGPlayerChampionAuthority.CurrentChampion;if(player==null||string.IsNullOrEmpty(player.championId))return;
        AOGSupportJungleHeroType? type=Resolve(player.championId);if(!type.HasValue)return;
        AOGSupportJungleHeroKitRuntime aiKit=player.GetComponent<AOGSupportJungleHeroKitRuntime>();if(aiKit!=null)aiKit.enabled=false;
        AOGSupportJunglePlayerInputRuntime input=player.GetComponent<AOGSupportJunglePlayerInputRuntime>();if(input==null)input=player.gameObject.AddComponent<AOGSupportJunglePlayerInputRuntime>();input.heroType=type.Value;
    }

    private static AOGSupportJungleHeroType? Resolve(string id)
    {
        string key=id.ToLowerInvariant();if(key=="seris")return AOGSupportJungleHeroType.Seris;if(key=="mireva")return AOGSupportJungleHeroType.Mireva;if(key=="dravenor")return AOGSupportJungleHeroType.Dravenor;if(key=="nocthyr")return AOGSupportJungleHeroType.Nocthyr;return null;
    }
}
