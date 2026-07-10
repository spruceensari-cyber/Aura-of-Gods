using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AOGPremiumMageMechanicalIdentityRuntime : MonoBehaviour, IAOGAbilityCooldownProvider
{
    public AOGPremiumMageType mageType;

    private AOGCharacterStats stats;
    private ChampionPresentationController presentation;
    private AOGActiveChampion active;
    private float nextQ;
    private float nextW;
    private float nextE;
    private float nextR;

    private int nyraFragments;
    private int nyraDashCharges = 2;
    private float nyraChargeRechargeAt;

    private float pyrelleHeat;
    private bool pyrelleOverheated;

    private readonly List<Transform> seleneAnchors = new List<Transform>();
    private float seleneAlignmentUntil;

    public string ChampionDisplayName => mageType == AOGPremiumMageType.NyraSpiritVixen ? "NYRA" : mageType == AOGPremiumMageType.PyrelleFlameSovereign ? "PYRELLE" : "SELENE";
    public string ChampionRoleName => mageType == AOGPremiumMageType.NyraSpiritVixen ? "SPIRIT VIXEN" : mageType == AOGPremiumMageType.PyrelleFlameSovereign ? "FLAME SOVEREIGN" : "ASTRAL ORACLE";

    private void Awake()
    {
        stats = GetComponent<AOGCharacterStats>();
        presentation = GetComponent<ChampionPresentationController>();
        active = GetComponent<AOGActiveChampion>();
    }

    private void Update()
    {
        if (active != null && !active.IsActiveChampion)
            return;

        if (mageType == AOGPremiumMageType.NyraSpiritVixen)
            UpdateNyraCharges();

        if (AOGInputBridge.KeyPressedThisFrame(KeyCode.Q)) CastQ();
        if (AOGInputBridge.KeyPressedThisFrame(KeyCode.W)) CastW();
        if (AOGInputBridge.KeyPressedThisFrame(KeyCode.E)) CastE();
        if (AOGInputBridge.KeyPressedThisFrame(KeyCode.R)) CastR();
    }

    public void CastQ()
    {
        if (Time.time < nextQ) return;
        nextQ = Time.time + QCooldown;
        presentation?.PlayAbility(0);

        if (mageType == AOGPremiumMageType.NyraSpiritVixen)
            CastNyraSpiritArc();
        else if (mageType == AOGPremiumMageType.PyrelleFlameSovereign)
            CastPyrelleFlameOrb();
        else
            CastSeleneStarLance();
    }

    public void CastW()
    {
        if (Time.time < nextW) return;
        nextW = Time.time + WCooldown;
        presentation?.PlayAbility(1);

        if (mageType == AOGPremiumMageType.NyraSpiritVixen)
            CastNyraHeartEcho();
        else if (mageType == AOGPremiumMageType.PyrelleFlameSovereign)
            CastPyrelleInfernoLine();
        else
            CastSeleneMoonField();
    }

    public void CastE()
    {
        if (mageType == AOGPremiumMageType.NyraSpiritVixen)
        {
            if (nyraDashCharges <= 0) return;
            nyraDashCharges--;
            if (nyraDashCharges == 1) nyraChargeRechargeAt = Time.time + 8f;
            StartCoroutine(NyraVixenStep());
            return;
        }

        if (Time.time < nextE) return;
        nextE = Time.time + ECooldown;
        presentation?.PlayAbility(2);

        if (mageType == AOGPremiumMageType.PyrelleFlameSovereign)
            StartCoroutine(PyrellePhoenixDash());
        else
            StartCoroutine(SeleneAstralShift());
    }

    public void CastR()
    {
        if (Time.time < nextR) return;
        nextR = Time.time + RCooldown;
        presentation?.PlayAbility(3);

        if (mageType == AOGPremiumMageType.NyraSpiritVixen)
            StartCoroutine(NyraNinefoldRequiem());
        else if (mageType == AOGPremiumMageType.PyrelleFlameSovereign)
            StartCoroutine(PyrelleSolarCatastrophe());
        else
            StartCoroutine(SeleneCelestialJudgment());
    }

    private void CastNyraSpiritArc()
    {
        Vector3 origin = transform.position + Vector3.up * 1.25f + transform.forward * 0.8f;
        AOGPremiumSkillProjectile projectile = SpawnProjectile("Nyra_Spirit_Arc_Out", origin, transform.forward, 18f, 17f, 135f, 0.42f, new Color(0.96f,0.30f,0.78f), AOGPremiumSkillProjectile.VisualKind.Spirit);
        projectile.onHit = target =>
        {
            GainNyraFragment(1);
            if (target != null)
                StartCoroutine(NyraReturnArc(target.transform.position));
        };
    }

    private IEnumerator NyraReturnArc(Vector3 hitPoint)
    {
        yield return new WaitForSeconds(0.18f);
        Vector3 origin = hitPoint + Vector3.up * 0.9f;
        Vector3 direction = (transform.position + Vector3.up * 1.0f - origin).normalized;
        AOGPremiumSkillProjectile returnOrb = SpawnProjectile("Nyra_Spirit_Arc_Return", origin, direction, 20f, 18f, 105f, 0.48f, new Color(1f,0.55f,0.90f), AOGPremiumSkillProjectile.VisualKind.Wisp);
        returnOrb.onHit = target =>
        {
            GainNyraFragment(1);
            if (stats != null)
                stats.hp = Mathf.Min(stats.maxHp, stats.hp + 28f + nyraFragments * 2f);
        };
    }

    private void CastNyraHeartEcho()
    {
        AOGCharacterStats target = FindNearestEnemyChampion(8f);
        if (target == null)
        {
            nextW = Time.time + 1.2f;
            return;
        }

        StartCoroutine(NyraHeartEchoRoutine(target));
    }

    private IEnumerator NyraHeartEchoRoutine(AOGCharacterStats target)
    {
        GameObject tether = AOGAbilityVisuals.CreateBeam("Nyra_Heart_Echo_Tether", transform.position + Vector3.up * 1.1f, target.transform.position + Vector3.up * 1.1f, new Color(1f,0.34f,0.76f), 0.16f);
        float end = Time.time + 1.4f;
        while (Time.time < end && target != null && !target.IsDead)
        {
            if (FlatDistance(transform.position,target.transform.position) > 9f)
                break;
            yield return null;
        }

        if (tether != null) Destroy(tether);
        if (target != null && !target.IsDead && FlatDistance(transform.position,target.transform.position) <= 9f)
        {
            target.TakeDamage(120f,gameObject);
            GainNyraFragment(2);
            GameObject ring=AOGAbilityVisuals.CreateRing("Nyra_Heart_Echo_Break",target.transform.position+Vector3.up*0.05f,2.1f,new Color(1f,0.28f,0.72f),0.10f);
            Destroy(ring,0.45f);
        }
    }

    private IEnumerator NyraVixenStep()
    {
        presentation?.PlayAbility(2);
        Vector3 start=transform.position;
        Vector3 end=start+transform.forward*5.5f;
        float elapsed=0f;
        while(elapsed<0.16f)
        {
            elapsed+=Time.deltaTime;
            transform.position=Vector3.Lerp(start,end,Mathf.Clamp01(elapsed/0.16f));
            yield return null;
        }
        transform.position=end;
        GameObject ring=AOGAbilityVisuals.CreateRing("Nyra_Vixen_Step",end+Vector3.up*0.05f,1.4f,new Color(0.95f,0.32f,0.78f),0.07f);
        Destroy(ring,0.3f);
    }

    private IEnumerator NyraNinefoldRequiem()
    {
        int strikes = 5 + Mathf.Min(4, nyraFragments);
        nyraFragments = 0;
        for (int i=0;i<strikes;i++)
        {
            AOGCharacterStats target = FindNearestEnemyChampion(11f);
            Vector3 point = target != null ? target.transform.position : transform.position + transform.forward * 4f;
            point += Random.insideUnitSphere * 1.2f; point.y = transform.position.y;
            GameObject telegraph=AOGAbilityVisuals.CreateRing("Nyra_Requiem_Mark_"+i,point+Vector3.up*0.05f,1.6f,new Color(1f,0.34f,0.80f),0.06f);
            yield return new WaitForSeconds(0.22f);
            DamageRadius(point,1.6f,72f);
            if (telegraph!=null) Destroy(telegraph);
            yield return new WaitForSeconds(0.10f);
        }
    }

    private void GainNyraFragment(int amount)
    {
        nyraFragments=Mathf.Clamp(nyraFragments+amount,0,9);
        if (nyraFragments>=6 && stats!=null)
            stats.hp=Mathf.Min(stats.maxHp,stats.hp+18f);
    }

    private void UpdateNyraCharges()
    {
        if (nyraDashCharges >= 2) return;
        if (Time.time < nyraChargeRechargeAt) return;
        nyraDashCharges++;
        if (nyraDashCharges < 2) nyraChargeRechargeAt=Time.time+8f;
    }

    private void CastPyrelleFlameOrb()
    {
        float damage=pyrelleOverheated?235f:165f;
        float radius=pyrelleOverheated?0.62f:0.48f;
        AOGPremiumSkillProjectile orb=SpawnProjectile("Pyrelle_Flame_Orb",transform.position+Vector3.up*1.3f+transform.forward*0.8f,transform.forward,15f,16f,damage,radius,new Color(1f,0.20f,0.03f),AOGPremiumSkillProjectile.VisualKind.Fireball);
        bool empowered=pyrelleOverheated;
        pyrelleOverheated=false;
        pyrelleHeat=0f;
        orb.onHit=target=>
        {
            if (empowered && target!=null)
            {
                AOGPersistentDamageZone zone=new GameObject("Pyrelle_Overheat_Burn").AddComponent<AOGPersistentDamageZone>();
                zone.transform.position=target.transform.position;
                zone.owner=gameObject; zone.team=stats.team; zone.radius=2.6f; zone.duration=3f; zone.damagePerTick=28f; zone.tickRate=0.7f; zone.color=new Color(1f,0.18f,0.02f,0.55f); zone.BuildVisual();
            }
        };
        AddHeat(30f);
    }

    private void CastPyrelleInfernoLine()
    {
        Vector3 start=transform.position+transform.forward*1.0f+Vector3.up*0.08f;
        Vector3 end=transform.position+transform.forward*13f+Vector3.up*0.08f;
        AOGPersistentDamageZone zone=new GameObject("Pyrelle_Inferno_Path").AddComponent<AOGPersistentDamageZone>();
        zone.transform.position=(start+end)*0.5f;
        zone.transform.rotation=Quaternion.LookRotation(end-start);
        zone.owner=gameObject; zone.team=stats.team; zone.radius=4.8f; zone.duration=5f; zone.damagePerTick=35f; zone.tickRate=0.65f; zone.color=new Color(1f,0.16f,0.02f,0.58f); zone.BuildVisual();
        GameObject beam=AOGAbilityVisuals.CreateBeam("Pyrelle_Inferno_Line",start,end,new Color(1f,0.22f,0.02f),0.8f);
        Destroy(beam,0.7f);
        AddHeat(35f);
    }

    private IEnumerator PyrellePhoenixDash()
    {
        Vector3 start=transform.position;
        Vector3 end=start+transform.forward*6.2f;
        float elapsed=0f;
        while(elapsed<0.32f)
        {
            elapsed+=Time.deltaTime;
            transform.position=Vector3.Lerp(start,end,Mathf.Clamp01(elapsed/0.32f));
            if (Mathf.FloorToInt(elapsed*20f)%3==0)
            {
                GameObject ember=AOGAbilityVisuals.CreateRing("Phoenix_Ember",transform.position+Vector3.up*0.04f,1.1f,new Color(1f,0.24f,0.02f),0.05f);
                Destroy(ember,0.35f);
            }
            yield return null;
        }
        transform.position=end;
        DamageRadius(end,2.5f,125f);
        AddHeat(40f);
    }

    private IEnumerator PyrelleSolarCatastrophe()
    {
        Vector3 point=transform.position+transform.forward*6f;
        GameObject telegraph=AOGAbilityVisuals.CreateRing("Pyrelle_Solar_Catastrophe_Telegraph",point+Vector3.up*0.05f,7.5f,new Color(1f,0.20f,0.02f),0.18f);
        yield return new WaitForSeconds(1.4f);
        DamageRadius(point,7.5f,pyrelleOverheated?560f:430f);
        GameObject inner=AOGAbilityVisuals.CreateRing("Pyrelle_Solar_Catastrophe_Impact",point+Vector3.up*0.08f,5.2f,new Color(1f,0.55f,0.06f),0.24f);
        Camera.main?.GetComponent<AOGMobaCameraController>()?.AddRandomImpulse(0.55f);
        if(telegraph!=null)Destroy(telegraph);
        Destroy(inner,0.9f);
        pyrelleHeat=0f; pyrelleOverheated=false;
    }

    private void AddHeat(float amount)
    {
        pyrelleHeat=Mathf.Clamp(pyrelleHeat+amount,0f,100f);
        if(pyrelleHeat>=100f) pyrelleOverheated=true;
    }

    private void CastSeleneStarLance()
    {
        AOGPremiumSkillProjectile lance=SpawnProjectile("Selene_Star_Lance",transform.position+Vector3.up*1.35f+transform.forward*0.8f,transform.forward,24f,21f,145f,0.30f,new Color(0.32f,0.72f,1f),AOGPremiumSkillProjectile.VisualKind.StarLance);
        lance.onHit=target=>
        {
            if(Time.time<seleneAlignmentUntil && target!=null)
                DealDirect(target,58f);
        };
    }

    private void CastSeleneMoonField()
    {
        Vector3 point=transform.position+transform.forward*5f;
        AOGPersistentDamageZone zone=new GameObject("Selene_Moon_Field_Unique").AddComponent<AOGPersistentDamageZone>();
        zone.transform.position=point; zone.owner=gameObject; zone.team=stats.team; zone.radius=4.6f; zone.duration=6f; zone.damagePerTick=24f; zone.tickRate=0.75f; zone.color=new Color(0.20f,0.42f,0.92f,0.55f); zone.BuildVisual();

        GameObject anchor=new GameObject("Selene_Astral_Anchor");
        anchor.transform.position=point+Vector3.up*0.15f;
        seleneAnchors.Add(anchor.transform);
        Destroy(anchor,6f);
        StartCoroutine(RemoveAnchorLater(anchor.transform,6f));
        seleneAlignmentUntil=Time.time+6f;
    }

    private IEnumerator SeleneAstralShift()
    {
        Transform targetAnchor=null;
        float best=float.MaxValue;
        foreach(Transform anchor in seleneAnchors)
        {
            if(anchor==null)continue;
            float d=FlatDistance(transform.position,anchor.position);
            if(d<best){best=d;targetAnchor=anchor;}
        }

        Vector3 end=targetAnchor!=null?targetAnchor.position:transform.position+transform.forward*5f;
        Vector3 start=transform.position;
        GameObject beam=AOGAbilityVisuals.CreateBeam("Selene_Astral_Shift",start+Vector3.up*0.5f,end+Vector3.up*0.5f,new Color(0.34f,0.72f,1f),0.34f);
        float elapsed=0f;
        while(elapsed<0.22f)
        {
            elapsed+=Time.deltaTime;
            transform.position=Vector3.Lerp(start,end,Mathf.Clamp01(elapsed/0.22f));
            yield return null;
        }
        transform.position=end;
        if(beam!=null)Destroy(beam,0.35f);
    }

    private IEnumerator SeleneCelestialJudgment()
    {
        Vector3 center=transform.position+transform.forward*5f;
        GameObject outer=AOGAbilityVisuals.CreateRing("Selene_Judgment_Alignment",center+Vector3.up*0.05f,8.2f,new Color(0.26f,0.58f,1f),0.12f);
        yield return new WaitForSeconds(0.65f);
        GameObject inner=AOGAbilityVisuals.CreateRing("Selene_Judgment_Convergence",center+Vector3.up*0.08f,5.0f,new Color(0.70f,0.86f,1f),0.15f);
        yield return new WaitForSeconds(0.65f);
        DamageRadius(center,8.2f,260f);
        yield return new WaitForSeconds(0.35f);
        DamageRadius(center,5.0f,220f);
        Camera.main?.GetComponent<AOGMobaCameraController>()?.AddRandomImpulse(0.45f);
        if(outer!=null)Destroy(outer);
        if(inner!=null)Destroy(inner);
    }

    private IEnumerator RemoveAnchorLater(Transform anchor,float delay)
    {
        yield return new WaitForSeconds(delay);
        seleneAnchors.Remove(anchor);
    }

    private AOGPremiumSkillProjectile SpawnProjectile(string name,Vector3 origin,Vector3 direction,float speed,float range,float damage,float radius,Color color,AOGPremiumSkillProjectile.VisualKind kind)
    {
        GameObject go=new GameObject(name);
        go.transform.position=origin;
        AOGPremiumSkillProjectile projectile=go.AddComponent<AOGPremiumSkillProjectile>();
        projectile.owner=gameObject; projectile.team=stats.team; projectile.direction=direction; projectile.speed=speed; projectile.range=range; projectile.damage=damage; projectile.radius=radius; projectile.color=color; projectile.visualKind=kind;
        projectile.BuildVisual();
        return projectile;
    }

    private void DamageRadius(Vector3 center,float radius,float damage)
    {
        HashSet<Object> hitTargets=new HashSet<Object>();
        foreach(Collider hit in Physics.OverlapSphere(center,radius,~0,QueryTriggerInteraction.Ignore))
        {
            AOGCharacterStats hero=hit.GetComponentInParent<AOGCharacterStats>();
            if(hero!=null && hero!=stats && hero.team!=stats.team && hitTargets.Add(hero))
            {
                hero.TakeDamage(damage,gameObject);
                AOGCombatEvents.RaiseAbilityHit(new AOGCombatHitEvent{source=gameObject,target=hero.gameObject,damage=damage,basicAttack=false,abilityId=ChampionDisplayName+"_area",targetKind=AOGCombatTargetKind.Champion});
                continue;
            }
            Minion minion=hit.GetComponentInParent<Minion>();
            if(minion!=null && minion.team!=stats.team && hitTargets.Add(minion)){minion.TakeDamage(damage,gameObject);continue;}
            AOGNeutralMonsterRuntime monster=hit.GetComponentInParent<AOGNeutralMonsterRuntime>();
            if(monster!=null && hitTargets.Add(monster)){monster.TakeDamage(damage,gameObject);continue;}
            AOGNeutralBossAI boss=hit.GetComponentInParent<AOGNeutralBossAI>();
            if(boss!=null && hitTargets.Add(boss)){boss.TakeDamage(damage,gameObject);continue;}
        }
    }

    private void DealDirect(GameObject target,float damage)
    {
        if(target==null)return;
        AOGCharacterStats hero=target.GetComponentInParent<AOGCharacterStats>(); if(hero!=null&&hero.team!=stats.team){hero.TakeDamage(damage,gameObject);return;}
        Minion minion=target.GetComponentInParent<Minion>(); if(minion!=null&&minion.team!=stats.team){minion.TakeDamage(damage,gameObject);return;}
        AOGNeutralMonsterRuntime monster=target.GetComponentInParent<AOGNeutralMonsterRuntime>(); if(monster!=null){monster.TakeDamage(damage,gameObject);return;}
        AOGNeutralBossAI boss=target.GetComponentInParent<AOGNeutralBossAI>(); if(boss!=null)boss.TakeDamage(damage,gameObject);
    }

    private AOGCharacterStats FindNearestEnemyChampion(float radius)
    {
        AOGCharacterStats best=null; float bestDistance=radius;
        foreach(AOGCharacterStats hero in FindObjectsByType<AOGCharacterStats>(FindObjectsInactive.Exclude,FindObjectsSortMode.None))
        {
            if(hero==null||hero==stats||hero.IsDead||hero.team==stats.team)continue;
            float d=FlatDistance(transform.position,hero.transform.position);
            if(d<bestDistance){best=hero;bestDistance=d;}
        }
        return best;
    }

    private float QCooldown => mageType==AOGPremiumMageType.NyraSpiritVixen?5f:mageType==AOGPremiumMageType.PyrelleFlameSovereign?6f:4.5f;
    private float WCooldown => mageType==AOGPremiumMageType.NyraSpiritVixen?9f:mageType==AOGPremiumMageType.PyrelleFlameSovereign?11f:10f;
    private float ECooldown => mageType==AOGPremiumMageType.PyrelleFlameSovereign?10f:8f;
    private float RCooldown => mageType==AOGPremiumMageType.NyraSpiritVixen?44f:mageType==AOGPremiumMageType.PyrelleFlameSovereign?52f:48f;

    public string GetAbilityName(int slot)
    {
        if(mageType==AOGPremiumMageType.NyraSpiritVixen)return slot==0?"SPIRIT ARC":slot==1?"HEART ECHO":slot==2?"VIXEN STEP": "NINEFOLD REQUIEM";
        if(mageType==AOGPremiumMageType.PyrelleFlameSovereign)return slot==0?"CROWN FLARE":slot==1?"INFERNO PATH":slot==2?"PHOENIX PASSAGE":"SOLAR CATASTROPHE";
        return slot==0?"STAR LANCE":slot==1?"MOON FIELD":slot==2?"ASTRAL SHIFT":"CELESTIAL JUDGMENT";
    }

    public float GetAbilityCooldownDuration(int slot)=>slot==0?QCooldown:slot==1?WCooldown:slot==2?ECooldown:RCooldown;
    public float GetAbilityCooldownRatio(int slot)
    {
        if(mageType==AOGPremiumMageType.NyraSpiritVixen&&slot==2)return nyraDashCharges>0?0f:Mathf.Clamp01((nyraChargeRechargeAt-Time.time)/8f);
        float next=slot==0?nextQ:slot==1?nextW:slot==2?nextE:nextR;
        return Mathf.Clamp01((next-Time.time)/GetAbilityCooldownDuration(slot));
    }

    private static float FlatDistance(Vector3 a,Vector3 b){a.y=0f;b.y=0f;return Vector3.Distance(a,b);}
}

public class AOGPremiumSkillProjectile : MonoBehaviour
{
    public enum VisualKind{Spirit,Wisp,Fireball,StarLance}
    public GameObject owner;
    public MinionTeam team;
    public Vector3 direction;
    public float speed=18f;
    public float range=16f;
    public float damage=120f;
    public float radius=0.4f;
    public Color color=Color.white;
    public VisualKind visualKind;
    public System.Action<GameObject> onHit;

    private Vector3 start;
    private bool resolved;

    private void Start(){start=transform.position;}
    private void Update()
    {
        if(resolved)return;
        float step=speed*Time.deltaTime;
        RaycastHit[] hits=Physics.SphereCastAll(transform.position,radius,direction.normalized,step,~0,QueryTriggerInteraction.Ignore);
        foreach(RaycastHit hit in hits)
        {
            if(hit.collider==null||owner==null||hit.collider.transform.IsChildOf(owner.transform))continue;
            if(Apply(hit.collider))
            {
                resolved=true;
                onHit?.Invoke(ResolveTargetObject(hit.collider));
                GameObject ring=AOGAbilityVisuals.CreateRing(name+"_Impact",hit.point+Vector3.up*0.06f,1.0f,color,0.07f);
                Destroy(ring,0.3f);
                Destroy(gameObject);
                return;
            }
        }
        transform.position+=direction.normalized*step;
        if(Vector3.Distance(start,transform.position)>=range)Destroy(gameObject);
    }

    public void BuildVisual()
    {
        PrimitiveType type=visualKind==VisualKind.StarLance?PrimitiveType.Capsule:PrimitiveType.Sphere;
        GameObject visual=GameObject.CreatePrimitive(type);
        visual.name=visualKind+"_Visual";
        visual.transform.SetParent(transform,false);
        visual.transform.localPosition=Vector3.zero;
        if(type==PrimitiveType.Capsule)visual.transform.localRotation=Quaternion.Euler(90f,0f,0f);
        visual.transform.localScale=visualKind==VisualKind.Fireball?Vector3.one*0.72f:visualKind==VisualKind.StarLance?new Vector3(0.18f,1.25f,0.18f):Vector3.one*0.48f;
        Collider c=visual.GetComponent<Collider>();if(c!=null)Destroy(c);
        Shader shader=Shader.Find("Universal Render Pipeline/Lit");if(shader==null)shader=Shader.Find("Standard");
        Material mat=new Material(shader){color=color};
        if(mat.HasProperty("_EmissionColor")){mat.EnableKeyword("_EMISSION");mat.SetColor("_EmissionColor",color*5f);}
        visual.GetComponent<Renderer>().sharedMaterial=mat;
        TrailRenderer trail=visual.AddComponent<TrailRenderer>();trail.time=visualKind==VisualKind.StarLance?0.18f:0.34f;trail.startWidth=visualKind==VisualKind.Fireball?0.72f:0.42f;trail.endWidth=0f;trail.sharedMaterial=mat;trail.startColor=color;trail.endColor=new Color(color.r,color.g,color.b,0f);
    }

    private bool Apply(Collider collider)
    {
        AOGCharacterStats hero=collider.GetComponentInParent<AOGCharacterStats>();
        if(hero!=null&&hero.team!=team){hero.TakeDamage(damage,owner);AOGCombatEvents.RaiseAbilityHit(new AOGCombatHitEvent{source=owner,target=hero.gameObject,damage=damage,basicAttack=false,abilityId=name,targetKind=AOGCombatTargetKind.Champion});return true;}
        Minion minion=collider.GetComponentInParent<Minion>();if(minion!=null&&minion.team!=team){minion.TakeDamage(damage,owner);return true;}
        AOGNeutralMonsterRuntime monster=collider.GetComponentInParent<AOGNeutralMonsterRuntime>();if(monster!=null){monster.TakeDamage(damage,owner);return true;}
        AOGNeutralBossAI boss=collider.GetComponentInParent<AOGNeutralBossAI>();if(boss!=null){boss.TakeDamage(damage,owner);return true;}
        return false;
    }

    private static GameObject ResolveTargetObject(Collider collider)
    {
        AOGCharacterStats hero=collider.GetComponentInParent<AOGCharacterStats>();if(hero!=null)return hero.gameObject;
        Minion minion=collider.GetComponentInParent<Minion>();if(minion!=null)return minion.gameObject;
        AOGNeutralMonsterRuntime monster=collider.GetComponentInParent<AOGNeutralMonsterRuntime>();if(monster!=null)return monster.gameObject;
        AOGNeutralBossAI boss=collider.GetComponentInParent<AOGNeutralBossAI>();return boss!=null?boss.gameObject:null;
    }
}

public class AOGPremiumMageMechanicalIdentityBootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if(FindFirstObjectByType<AOGPremiumMageMechanicalIdentityBootstrap>()!=null)return;
        GameObject host=new GameObject("AOG_Premium_Mage_Mechanical_Identity_Bootstrap");DontDestroyOnLoad(host);host.AddComponent<AOGPremiumMageMechanicalIdentityBootstrap>();
    }

    private float nextScan;
    private void Update()
    {
        if(Time.unscaledTime<nextScan)return;nextScan=Time.unscaledTime+0.5f;
        foreach(AOGPremiumMageSkillSet legacy in FindObjectsByType<AOGPremiumMageSkillSet>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {
            if(legacy==null)continue;
            AOGPremiumMageMechanicalIdentityRuntime unique=legacy.GetComponent<AOGPremiumMageMechanicalIdentityRuntime>();
            if(unique==null){unique=legacy.gameObject.AddComponent<AOGPremiumMageMechanicalIdentityRuntime>();unique.mageType=legacy.mageType;}
            legacy.enabled=false;
        }
    }
}
