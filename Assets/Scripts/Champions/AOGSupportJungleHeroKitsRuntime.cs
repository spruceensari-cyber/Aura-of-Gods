using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum AOGSupportJungleHeroType
{
    Seris,
    Mireva,
    Dravenor,
    Nocthyr
}

/// <summary>
/// Champion-specific ability logic for Seris, Mireva, Dravenor and Nocthyr.
/// Existing role movement/targeting AI remains authoritative; this component only layers
/// hero-identity ability decisions at a conservative cadence.
/// </summary>
public class AOGSupportJungleHeroKitRuntime : MonoBehaviour
{
    public AOGSupportJungleHeroType heroType;

    private AOGTeamMemberIdentity identity;
    private AOGCharacterStats stats;
    private ChampionPresentationController presentation;
    private float nextDecision;
    private float nextQ;
    private float nextW;
    private float nextE;
    private float nextR;
    private float empoweredUntil;
    private int huntStacks;

    private void Awake()
    {
        identity = GetComponent<AOGTeamMemberIdentity>();
        stats = GetComponent<AOGCharacterStats>();
        presentation = GetComponent<ChampionPresentationController>();
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
        if (identity == null || stats == null || stats.IsDead)
            return;
        if (AOGMatchDirector.Instance == null || AOGMatchDirector.Instance.State != AOGMatchState.Playing)
            return;
        if (Time.time < nextDecision)
            return;

        nextDecision = Time.time + DecisionInterval();
        switch (heroType)
        {
            case AOGSupportJungleHeroType.Seris: ThinkSeris(); break;
            case AOGSupportJungleHeroType.Mireva: ThinkMireva(); break;
            case AOGSupportJungleHeroType.Dravenor: ThinkDravenor(); break;
            case AOGSupportJungleHeroType.Nocthyr: ThinkNocthyr(); break;
        }
    }

    private void ThinkSeris()
    {
        AOGCharacterStats ally = FindLowestHealthAlly(10f);
        AOGCharacterStats enemy = FindNearestEnemy(8f);

        if (ally != null && ally.hp / Mathf.Max(1f,ally.maxHp) < 0.32f && Time.time >= nextR)
        {
            nextR = Time.time + 52f;
            StartCoroutine(SerisSanctuary(ally));
            return;
        }

        if (ally != null && ally.hp / Mathf.Max(1f,ally.maxHp) < 0.62f && Time.time >= nextW)
        {
            nextW = Time.time + 8.5f;
            ApplyTemporaryShield(ally,180f,4.5f,new Color(0.24f,0.88f,0.95f));
            presentation?.PlayAbility(1);
            return;
        }

        if (enemy != null && Time.time >= nextE && AllyUnderPressure(9f))
        {
            nextE = Time.time + 11f;
            StartCoroutine(SerisPeelPulse(enemy));
            return;
        }

        if (enemy != null && Time.time >= nextQ)
        {
            nextQ = Time.time + 5.5f;
            SerisAetherThread(enemy);
        }
    }

    private void SerisAetherThread(AOGCharacterStats enemy)
    {
        if (enemy == null) return;
        presentation?.PlayAbility(0);
        GameObject beam = AOGAbilityVisuals.CreateBeam("Seris_Aether_Thread",transform.position+Vector3.up*1.25f,enemy.transform.position+Vector3.up*1.0f,new Color(0.24f,0.88f,0.95f),0.16f);
        Destroy(beam,0.32f);
        DealChampionDamage(enemy,72f,"seris_aether_thread");
        StartCoroutine(TemporarySlow(enemy,0.72f,1.4f));
    }

    private IEnumerator SerisPeelPulse(AOGCharacterStats enemy)
    {
        presentation?.PlayAbility(2);
        Vector3 center = transform.position;
        for (int i=0;i<2;i++)
        {
            GameObject ring=AOGAbilityVisuals.CreateRing("Seris_Peel_Pulse_"+i,center+Vector3.up*0.06f,2.6f+i*1.3f,new Color(0.28f,0.82f,1f),0.10f);
            Destroy(ring,0.45f);
        }
        yield return new WaitForSeconds(0.22f);
        foreach(AOGCharacterStats target in FindEnemyChampions(center,4.8f))
        {
            DealChampionDamage(target,54f,"seris_peel_pulse");
            StartCoroutine(TemporarySlow(target,0.55f,1.8f));
        }
    }

    private IEnumerator SerisSanctuary(AOGCharacterStats focus)
    {
        presentation?.PlayAbility(3);
        Vector3 center = focus != null ? focus.transform.position : transform.position;
        GameObject ring=AOGAbilityVisuals.CreateRing("Seris_Aether_Sanctuary",center+Vector3.up*0.05f,5.2f,new Color(0.32f,0.92f,1f),0.18f);
        Destroy(ring,5.5f);
        float end=Time.time+5f;
        while(Time.time<end)
        {
            foreach(AOGCharacterStats ally in FindAllies(center,5.2f))
            {
                ally.hp=Mathf.Min(ally.maxHp,ally.hp+34f);
            }
            yield return new WaitForSeconds(0.75f);
        }
    }

    private void ThinkMireva()
    {
        AOGCharacterStats ally=FindLowestHealthAlly(10f);
        AOGCharacterStats enemy=FindNearestEnemy(8f);

        if (ally != null && ally.hp/Mathf.Max(1f,ally.maxHp)<0.28f && Time.time>=nextR)
        {
            nextR=Time.time+58f;
            StartCoroutine(MirevaVerdantRenewal());
            return;
        }

        if (ally != null && ally.hp/Mathf.Max(1f,ally.maxHp)<0.68f && Time.time>=nextW)
        {
            nextW=Time.time+9f;
            StartCoroutine(MirevaHealingGarden(ally.transform.position));
            return;
        }

        if (enemy != null && Time.time>=nextE)
        {
            nextE=Time.time+12f;
            StartCoroutine(MirevaRootField(enemy.transform.position));
            return;
        }

        if (enemy != null && Time.time>=nextQ)
        {
            nextQ=Time.time+5.8f;
            MirevaThornLance(enemy);
        }
    }

    private void MirevaThornLance(AOGCharacterStats enemy)
    {
        presentation?.PlayAbility(0);
        GameObject beam=AOGAbilityVisuals.CreateBeam("Mireva_Thorn_Lance",transform.position+Vector3.up*1.0f,enemy.transform.position+Vector3.up*0.8f,new Color(0.18f,0.82f,0.40f),0.13f);
        Destroy(beam,0.28f);
        DealChampionDamage(enemy,84f,"mireva_thorn_lance");
    }

    private IEnumerator MirevaHealingGarden(Vector3 center)
    {
        presentation?.PlayAbility(1);
        GameObject ring=AOGAbilityVisuals.CreateRing("Mireva_Healing_Garden",center+Vector3.up*0.05f,4.4f,new Color(0.22f,0.90f,0.46f),0.14f);
        Destroy(ring,5.2f);
        float end=Time.time+4.5f;
        while(Time.time<end)
        {
            foreach(AOGCharacterStats ally in FindAllies(center,4.4f))
                ally.hp=Mathf.Min(ally.maxHp,ally.hp+42f);
            yield return new WaitForSeconds(0.8f);
        }
    }

    private IEnumerator MirevaRootField(Vector3 center)
    {
        presentation?.PlayAbility(2);
        GameObject telegraph=AOGAbilityVisuals.CreateRing("Mireva_Root_Field_Telegraph",center+Vector3.up*0.05f,3.6f,new Color(0.16f,0.74f,0.32f),0.10f);
        yield return new WaitForSeconds(0.55f);
        foreach(AOGCharacterStats enemy in FindEnemyChampions(center,3.6f))
        {
            DealChampionDamage(enemy,62f,"mireva_root_field");
            StartCoroutine(TemporarySlow(enemy,0.18f,1.2f));
        }
        if(telegraph!=null)Destroy(telegraph,0.25f);
    }

    private IEnumerator MirevaVerdantRenewal()
    {
        presentation?.PlayAbility(3);
        for(int pulse=0;pulse<4;pulse++)
        {
            GameObject ring=AOGAbilityVisuals.CreateRing("Mireva_Verdant_Renewal_"+pulse,transform.position+Vector3.up*0.05f,3.8f+pulse*0.9f,new Color(0.24f,0.96f,0.52f),0.13f);
            Destroy(ring,0.6f);
            foreach(AOGCharacterStats ally in FindAllies(transform.position,7f))
            {
                ally.hp=Mathf.Min(ally.maxHp,ally.hp+96f);
                ApplyTemporaryShield(ally,75f,3f,new Color(0.30f,0.92f,0.50f));
            }
            yield return new WaitForSeconds(0.65f);
        }
    }

    private void ThinkDravenor()
    {
        AOGCharacterStats enemy=FindNearestEnemy(12f);
        AOGNeutralMonsterRuntime monster=FindNearestMonster(9f);
        AOGNeutralBossAI boss=FindNearestBoss(10f);

        if (enemy != null && enemy.hp/Mathf.Max(1f,enemy.maxHp)<0.36f && Time.time>=nextR)
        {
            nextR=Time.time+46f;
            StartCoroutine(DravenorApexHunt(enemy));
            return;
        }

        if (enemy != null && FlatDistance(transform.position,enemy.transform.position)>3f && Time.time>=nextE)
        {
            nextE=Time.time+10f;
            StartCoroutine(DravenorFangLeap(enemy));
            return;
        }

        if ((monster!=null||boss!=null) && Time.time>=nextQ)
        {
            nextQ=Time.time+5f;
            if(monster!=null) DravenorExecuteMonster(monster);
            else DravenorExecuteBoss(boss);
            return;
        }

        if ((enemy!=null||monster!=null) && Time.time>=nextW)
        {
            nextW=Time.time+12f;
            StartCoroutine(DravenorTrackingState());
        }
    }

    private void DravenorExecuteMonster(AOGNeutralMonsterRuntime monster)
    {
        if(monster==null||monster.IsDead)return;
        presentation?.PlayAbility(0);
        float missing=1f-monster.hp/Mathf.Max(1f,monster.maxHp);
        float damage=115f+180f*missing;
        monster.TakeDamage(damage,gameObject);
        huntStacks=Mathf.Clamp(huntStacks+1,0,6);
        SpawnFangImpact(monster.transform.position,new Color(0.90f,0.30f,0.10f));
    }

    private void DravenorExecuteBoss(AOGNeutralBossAI boss)
    {
        if(boss==null||boss.IsDead)return;
        presentation?.PlayAbility(0);
        float missing=1f-boss.hp/Mathf.Max(1f,boss.maxHp);
        float damage=90f+130f*missing;
        boss.TakeDamage(damage,gameObject);
        SpawnFangImpact(boss.transform.position,new Color(0.90f,0.30f,0.10f));
    }

    private IEnumerator DravenorTrackingState()
    {
        presentation?.PlayAbility(1);
        float bonus=0.85f+0.08f*huntStacks;
        stats.moveSpeed+=bonus;
        empoweredUntil=Time.time+4.5f;
        GameObject ring=AOGAbilityVisuals.CreateRing("Dravenor_Tracking_State",transform.position+Vector3.up*0.05f,1.8f,new Color(0.92f,0.28f,0.10f),0.08f);
        Destroy(ring,4.5f);
        yield return new WaitForSeconds(4.5f);
        stats.moveSpeed=Mathf.Max(1f,stats.moveSpeed-bonus);
    }

    private IEnumerator DravenorFangLeap(AOGCharacterStats enemy)
    {
        if(enemy==null)yield break;
        presentation?.PlayAbility(2);
        Vector3 start=transform.position;
        Vector3 direction=(enemy.transform.position-start);direction.y=0f;
        Vector3 end=enemy.transform.position-direction.normalized*1.4f;
        float elapsed=0f;
        while(elapsed<0.24f)
        {
            elapsed+=Time.deltaTime;
            transform.position=Vector3.Lerp(start,end,Mathf.Clamp01(elapsed/0.24f));
            yield return null;
        }
        if(enemy!=null&&!enemy.IsDead)
        {
            float damage=118f+(Time.time<empoweredUntil?35f:0f);
            DealChampionDamage(enemy,damage,"dravenor_fang_leap");
            StartCoroutine(TemporarySlow(enemy,0.70f,1.0f));
            SpawnFangImpact(enemy.transform.position,new Color(0.95f,0.34f,0.10f));
        }
    }

    private IEnumerator DravenorApexHunt(AOGCharacterStats enemy)
    {
        if(enemy==null)yield break;
        presentation?.PlayAbility(3);
        GameObject mark=AOGAbilityVisuals.CreateRing("Dravenor_Apex_Hunt_Mark",enemy.transform.position+Vector3.up*0.05f,2.2f,new Color(1f,0.18f,0.06f),0.14f);
        yield return new WaitForSeconds(0.45f);
        if(enemy!=null&&!enemy.IsDead)
        {
            Vector3 behind=enemy.transform.position-enemy.transform.forward*1.3f;
            transform.position=behind;
            float missing=1f-enemy.hp/Mathf.Max(1f,enemy.maxHp);
            DealChampionDamage(enemy,180f+260f*missing,"dravenor_apex_hunt");
        }
        if(mark!=null)Destroy(mark,0.3f);
    }

    private void ThinkNocthyr()
    {
        AOGCharacterStats enemy=FindBestAssassinationTarget(13f);
        if(enemy==null)return;
        float distance=FlatDistance(transform.position,enemy.transform.position);

        if(enemy.hp/Mathf.Max(1f,enemy.maxHp)<0.32f && Time.time>=nextR)
        {
            nextR=Time.time+48f;
            StartCoroutine(NocthyrNightfall(enemy));
            return;
        }

        if(distance>3.5f && Time.time>=nextE)
        {
            nextE=Time.time+9.5f;
            StartCoroutine(NocthyrShadowstep(enemy));
            return;
        }

        if(distance<=4.2f && Time.time>=nextQ)
        {
            nextQ=Time.time+4.8f;
            NocthyrShadeCut(enemy);
            return;
        }

        if(Time.time>=nextW)
        {
            nextW=Time.time+13f;
            StartCoroutine(NocthyrUmbralVeil());
        }
    }

    private void NocthyrShadeCut(AOGCharacterStats enemy)
    {
        presentation?.PlayAbility(0);
        DealChampionDamage(enemy,112f,"nocthyr_shade_cut");
        GameObject slash=AOGAbilityVisuals.CreateBeam("Nocthyr_Shade_Cut",transform.position+Vector3.up*1.0f,enemy.transform.position+Vector3.up*0.8f,new Color(0.26f,0.18f,0.72f),0.22f);
        Destroy(slash,0.22f);
    }

    private IEnumerator NocthyrUmbralVeil()
    {
        presentation?.PlayAbility(1);
        stats.moveSpeed+=1.2f;
        empoweredUntil=Time.time+3.8f;
        GameObject ring=AOGAbilityVisuals.CreateRing("Nocthyr_Umbral_Veil",transform.position+Vector3.up*0.05f,2.1f,new Color(0.18f,0.12f,0.52f),0.07f);
        Destroy(ring,3.8f);
        yield return new WaitForSeconds(3.8f);
        stats.moveSpeed=Mathf.Max(1f,stats.moveSpeed-1.2f);
    }

    private IEnumerator NocthyrShadowstep(AOGCharacterStats enemy)
    {
        if(enemy==null)yield break;
        presentation?.PlayAbility(2);
        Vector3 start=transform.position;
        Vector3 end=enemy.transform.position-enemy.transform.forward*1.2f;
        GameObject beam=AOGAbilityVisuals.CreateBeam("Nocthyr_Shadowstep_Path",start+Vector3.up*0.4f,end+Vector3.up*0.4f,new Color(0.22f,0.16f,0.68f),0.32f);
        yield return new WaitForSeconds(0.12f);
        transform.position=end;
        if(beam!=null)Destroy(beam,0.25f);
        if(enemy!=null&&!enemy.IsDead)
            DealChampionDamage(enemy,95f+(Time.time<empoweredUntil?45f:0f),"nocthyr_shadowstep");
    }

    private IEnumerator NocthyrNightfall(AOGCharacterStats enemy)
    {
        if(enemy==null)yield break;
        presentation?.PlayAbility(3);
        GameObject outer=AOGAbilityVisuals.CreateRing("Nocthyr_Nightfall_Outer",enemy.transform.position+Vector3.up*0.05f,3.8f,new Color(0.20f,0.12f,0.62f),0.18f);
        yield return new WaitForSeconds(0.55f);
        if(enemy!=null&&!enemy.IsDead)
        {
            transform.position=enemy.transform.position-enemy.transform.forward*1.0f;
            float missing=1f-enemy.hp/Mathf.Max(1f,enemy.maxHp);
            DealChampionDamage(enemy,210f+230f*missing,"nocthyr_nightfall");
        }
        if(outer!=null)Destroy(outer,0.35f);
    }

    private void OnChampionDeath(AOGChampionDeathEvent data)
    {
        if(heroType!=AOGSupportJungleHeroType.Nocthyr||data.killer==null)return;
        if(BelongsToThisChampion(data.killer))
        {
            nextQ=Mathf.Min(nextQ,Time.time+0.8f);
            nextE=Mathf.Min(nextE,Time.time+1.2f);
            nextW=Mathf.Min(nextW,Time.time+1.5f);
            GameObject ring=AOGAbilityVisuals.CreateRing("Nocthyr_Kill_Reset",transform.position+Vector3.up*0.05f,2.2f,new Color(0.30f,0.18f,0.82f),0.10f);
            Destroy(ring,0.5f);
        }
    }

    private bool AllyUnderPressure(float radius)
    {
        foreach(AOGCharacterStats ally in FindAllies(transform.position,radius))
            if(ally!=stats && ally.hp/Mathf.Max(1f,ally.maxHp)<0.55f)return true;
        return false;
    }

    private AOGCharacterStats FindLowestHealthAlly(float radius)
    {
        AOGCharacterStats best=null;float bestRatio=1.01f;
        foreach(AOGCharacterStats ally in FindAllies(transform.position,radius))
        {
            float ratio=ally.hp/Mathf.Max(1f,ally.maxHp);
            if(ratio<bestRatio){best=ally;bestRatio=ratio;}
        }
        return best;
    }

    private AOGCharacterStats FindNearestEnemy(float radius)
    {
        AOGCharacterStats best=null;float bestDistance=radius;
        foreach(AOGCharacterStats hero in FindObjectsByType<AOGCharacterStats>(FindObjectsInactive.Exclude,FindObjectsSortMode.None))
        {
            if(hero==null||hero==stats||hero.IsDead||hero.team==stats.team)continue;
            float d=FlatDistance(transform.position,hero.transform.position);
            if(d<bestDistance){best=hero;bestDistance=d;}
        }
        return best;
    }

    private AOGCharacterStats FindBestAssassinationTarget(float radius)
    {
        AOGCharacterStats best=null;float bestScore=float.MaxValue;
        foreach(AOGCharacterStats hero in FindObjectsByType<AOGCharacterStats>(FindObjectsInactive.Exclude,FindObjectsSortMode.None))
        {
            if(hero==null||hero==stats||hero.IsDead||hero.team==stats.team)continue;
            float d=FlatDistance(transform.position,hero.transform.position);if(d>radius)continue;
            float score=hero.hp/Mathf.Max(1f,hero.maxHp)+d/radius*0.35f;
            if(score<bestScore){best=hero;bestScore=score;}
        }
        return best;
    }

    private List<AOGCharacterStats> FindAllies(Vector3 center,float radius)
    {
        List<AOGCharacterStats> result=new List<AOGCharacterStats>();
        foreach(AOGCharacterStats hero in FindObjectsByType<AOGCharacterStats>(FindObjectsInactive.Exclude,FindObjectsSortMode.None))
        {
            if(hero==null||hero.IsDead||hero.team!=stats.team)continue;
            if(FlatDistance(center,hero.transform.position)<=radius)result.Add(hero);
        }
        return result;
    }

    private List<AOGCharacterStats> FindEnemyChampions(Vector3 center,float radius)
    {
        List<AOGCharacterStats> result=new List<AOGCharacterStats>();
        foreach(AOGCharacterStats hero in FindObjectsByType<AOGCharacterStats>(FindObjectsInactive.Exclude,FindObjectsSortMode.None))
        {
            if(hero==null||hero==stats||hero.IsDead||hero.team==stats.team)continue;
            if(FlatDistance(center,hero.transform.position)<=radius)result.Add(hero);
        }
        return result;
    }

    private AOGNeutralMonsterRuntime FindNearestMonster(float radius)
    {
        AOGNeutralMonsterRuntime best=null;float bestDistance=radius;
        foreach(AOGNeutralMonsterRuntime monster in FindObjectsByType<AOGNeutralMonsterRuntime>(FindObjectsInactive.Exclude,FindObjectsSortMode.None))
        {
            if(monster==null||monster.IsDead)continue;
            float d=FlatDistance(transform.position,monster.transform.position);
            if(d<bestDistance){best=monster;bestDistance=d;}
        }
        return best;
    }

    private AOGNeutralBossAI FindNearestBoss(float radius)
    {
        AOGNeutralBossAI best=null;float bestDistance=radius;
        foreach(AOGNeutralBossAI boss in FindObjectsByType<AOGNeutralBossAI>(FindObjectsInactive.Exclude,FindObjectsSortMode.None))
        {
            if(boss==null||boss.IsDead)continue;
            float d=FlatDistance(transform.position,boss.transform.position);
            if(d<bestDistance){best=boss;bestDistance=d;}
        }
        return best;
    }

    private IEnumerator TemporarySlow(AOGCharacterStats target,float multiplier,float duration)
    {
        if(target==null)yield break;
        float delta=target.moveSpeed*(1f-multiplier);
        target.moveSpeed=Mathf.Max(1f,target.moveSpeed-delta);
        yield return new WaitForSeconds(duration);
        if(target!=null)target.moveSpeed+=delta;
    }

    private void ApplyTemporaryShield(AOGCharacterStats ally,float amount,float duration,Color color)
    {
        if(ally==null)return;
        AOGTemporarySupportShieldRuntime shield=ally.GetComponent<AOGTemporarySupportShieldRuntime>();
        if(shield==null)shield=ally.gameObject.AddComponent<AOGTemporarySupportShieldRuntime>();
        shield.Apply(amount,duration,color);
    }

    private void DealChampionDamage(AOGCharacterStats target,float amount,string abilityId)
    {
        if(target==null||target.IsDead||target.team==stats.team)return;
        target.TakeDamage(amount,gameObject);
        AOGCombatEvents.RaiseAbilityHit(new AOGCombatHitEvent
        {
            source=gameObject,
            target=target.gameObject,
            damage=amount,
            basicAttack=false,
            abilityId=abilityId,
            targetKind=AOGCombatTargetKind.Champion
        });
    }

    private void SpawnFangImpact(Vector3 point,Color color)
    {
        for(int i=0;i<2;i++)
        {
            GameObject ring=AOGAbilityVisuals.CreateRing("Fang_Impact_"+i,point+Vector3.up*0.06f,1.2f+i*0.8f,color,0.10f);
            Destroy(ring,0.35f+i*0.08f);
        }
    }

    private bool BelongsToThisChampion(GameObject source)
    {
        if(source==null)return false;
        if(source==gameObject||source.transform.IsChildOf(transform))return true;
        AOGCharacterStats parent=source.GetComponentInParent<AOGCharacterStats>();
        return parent!=null&&parent.gameObject==gameObject;
    }

    private float DecisionInterval()
    {
        if(heroType==AOGSupportJungleHeroType.Seris||heroType==AOGSupportJungleHeroType.Mireva)return 0.42f;
        return 0.34f;
    }

    private static float FlatDistance(Vector3 a,Vector3 b)
    {
        a.y=0f;b.y=0f;return Vector3.Distance(a,b);
    }
}

/// <summary>
/// Temporary absorb buffer implemented as temporary max-HP and current-HP headroom.
/// It preserves existing damage authority and removes only the unused remainder on expiry.
/// </summary>
public class AOGTemporarySupportShieldRuntime : MonoBehaviour
{
    private AOGCharacterStats stats;
    private Coroutine routine;
    private float activeAmount;

    private void Awake(){stats=GetComponent<AOGCharacterStats>();}

    public void Apply(float amount,float duration,Color color)
    {
        if(stats==null||amount<=0f)return;
        if(routine!=null)StopCoroutine(routine);
        if(activeAmount>0f)
        {
            stats.maxHp=Mathf.Max(1f,stats.maxHp-activeAmount);
            stats.hp=Mathf.Min(stats.hp,stats.maxHp);
        }
        activeAmount=amount;
        stats.maxHp+=amount;
        stats.hp+=amount;
        GameObject ring=AOGAbilityVisuals.CreateRing("Support_Shield",transform.position+Vector3.up*0.05f,1.8f,color,0.10f);
        Destroy(ring,duration);
        routine=StartCoroutine(RemoveAfter(duration));
    }

    private IEnumerator RemoveAfter(float duration)
    {
        yield return new WaitForSeconds(duration);
        if(stats!=null&&activeAmount>0f)
        {
            stats.maxHp=Mathf.Max(1f,stats.maxHp-activeAmount);
            stats.hp=Mathf.Min(stats.hp,stats.maxHp);
        }
        activeAmount=0f;
        routine=null;
    }
}

public class AOGSupportJungleHeroKitsBootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if(FindFirstObjectByType<AOGSupportJungleHeroKitsBootstrap>()!=null)return;
        GameObject host=new GameObject("AOG_Support_Jungle_Hero_Kits_Bootstrap");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGSupportJungleHeroKitsBootstrap>();
    }

    private float nextScan;
    private void Update()
    {
        if(Time.unscaledTime<nextScan)return;
        nextScan=Time.unscaledTime+0.7f;

        foreach(AOGTeamMemberIdentity member in FindObjectsByType<AOGTeamMemberIdentity>(FindObjectsInactive.Exclude,FindObjectsSortMode.None))
        {
            if(member==null||string.IsNullOrEmpty(member.championId))continue;
            AOGSupportJungleHeroType? type=Resolve(member.championId);
            if(!type.HasValue)continue;
            AOGSupportJungleHeroKitRuntime kit=member.GetComponent<AOGSupportJungleHeroKitRuntime>();
            if(kit==null)kit=member.gameObject.AddComponent<AOGSupportJungleHeroKitRuntime>();
            kit.heroType=type.Value;
        }
    }

    private static AOGSupportJungleHeroType? Resolve(string id)
    {
        string key=id.ToLowerInvariant();
        if(key=="seris")return AOGSupportJungleHeroType.Seris;
        if(key=="mireva")return AOGSupportJungleHeroType.Mireva;
        if(key=="dravenor")return AOGSupportJungleHeroType.Dravenor;
        if(key=="nocthyr")return AOGSupportJungleHeroType.Nocthyr;
        return null;
    }
}
