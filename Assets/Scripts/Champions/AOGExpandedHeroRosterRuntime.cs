using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public enum AOGExtraHeroType { Auron, Vesper }

public class AOGExtraHeroSkillSet : MonoBehaviour, IAOGAbilityCooldownProvider
{
    public AOGExtraHeroType heroType;
    private float nextQ, nextW, nextE, nextR;
    private AOGCharacterStats stats;
    private ChampionPresentationController presentation;

    public string ChampionDisplayName => heroType == AOGExtraHeroType.Auron ? "AURON" : "VESPER";
    public string ChampionRoleName => heroType == AOGExtraHeroType.Auron ? "SOLAR VANGUARD" : "VOID ARCHER";

    private void Awake() { stats = GetComponent<AOGCharacterStats>(); presentation = GetComponent<ChampionPresentationController>(); }
    private void Update()
    {
        AOGActiveChampion active = GetComponent<AOGActiveChampion>();
        if (active != null && !active.IsActiveChampion) return;
        if (AOGInputBridge.KeyPressedThisFrame(KeyCode.Q)) CastQ();
        if (AOGInputBridge.KeyPressedThisFrame(KeyCode.W)) CastW();
        if (AOGInputBridge.KeyPressedThisFrame(KeyCode.E)) CastE();
        if (AOGInputBridge.KeyPressedThisFrame(KeyCode.R)) CastR();
    }

    public void CastQ()
    {
        if (Time.time < nextQ) return; nextQ = Time.time + 5f; presentation?.PlayAbility(0);
        if (heroType == AOGExtraHeroType.Auron)
        {
            DamageRadius(transform.position + transform.forward * 2.2f, 2.8f, 145f);
            GameObject ring = AOGAbilityVisuals.CreateRing("Auron_Q", transform.position + transform.forward * 2.2f, 2.8f, new Color(1f,0.62f,0.15f), 0.14f); Destroy(ring,0.45f);
        }
        else
        {
            FireProjectile(155f, 19f, 18f, new Color(0.18f,0.86f,0.92f));
        }
    }

    public void CastW()
    {
        if (Time.time < nextW) return; nextW = Time.time + 10f; presentation?.PlayAbility(1);
        if (stats == null) return;
        if (heroType == AOGExtraHeroType.Auron)
        {
            float old = stats.maxHp; stats.maxHp += 180f; stats.hp += stats.maxHp - old; Invoke(nameof(RemoveAuronShield), 4f);
            GameObject ring = AOGAbilityVisuals.CreateRing("Auron_W", transform.position, 3.2f, new Color(1f,0.72f,0.22f), 0.11f); Destroy(ring,0.8f);
        }
        else
        {
            stats.moveSpeed += 2f; Invoke(nameof(RemoveVesperSpeed), 3f);
            AOGAbilityVisuals.CreateBeam("Vesper_W", transform.position + Vector3.up, transform.position + transform.forward * 5f + Vector3.up, new Color(0.18f,0.86f,0.92f), 0.18f);
        }
    }

    public void CastE()
    {
        if (Time.time < nextE) return; nextE = Time.time + 8f; presentation?.PlayAbility(2);
        Vector3 start = transform.position; Vector3 end = start + transform.forward * (heroType == AOGExtraHeroType.Auron ? 5.5f : 7.5f);
        transform.position = end; DamageRadius(end, 2.2f, heroType == AOGExtraHeroType.Auron ? 110f : 85f);
        GameObject beam = AOGAbilityVisuals.CreateBeam("Hero_Dash", start + Vector3.up*0.2f, end + Vector3.up*0.2f, heroType == AOGExtraHeroType.Auron ? new Color(1f,0.62f,0.15f) : new Color(0.18f,0.86f,0.92f), 0.35f); Destroy(beam,0.35f);
    }

    public void CastR()
    {
        if (Time.time < nextR) return; nextR = Time.time + 48f; presentation?.PlayAbility(3);
        float radius = heroType == AOGExtraHeroType.Auron ? 7f : 9f;
        DamageRadius(transform.position, radius, heroType == AOGExtraHeroType.Auron ? 420f : 350f);
        GameObject ring = AOGAbilityVisuals.CreateRing("Hero_Ultimate", transform.position, radius, heroType == AOGExtraHeroType.Auron ? new Color(1f,0.55f,0.08f) : new Color(0.12f,0.78f,0.95f), 0.22f); Destroy(ring,1f);
        Camera.main?.GetComponent<AOGMobaCameraController>()?.AddRandomImpulse(0.45f);
    }

    private void FireProjectile(float damage, float speed, float range, Color color)
    {
        GameObject go = new GameObject("ExtraHeroProjectile"); go.transform.position = transform.position + Vector3.up*1.3f + transform.forward;
        AOGSkillProjectile p = go.AddComponent<AOGSkillProjectile>(); p.owner = gameObject; p.team = stats.team; p.direction = transform.forward; p.speed = speed; p.range = range; p.damage = damage; p.color = color; p.radius = 0.35f; p.BuildVisual(AOGSkillProjectile.Shape.Lance);
    }

    private void DamageRadius(Vector3 center, float radius, float damage)
    {
        foreach (Collider c in Physics.OverlapSphere(center, radius, ~0, QueryTriggerInteraction.Ignore))
        {
            Minion m = c.GetComponentInParent<Minion>(); if (m != null && m.team != stats.team) { m.TakeDamage(damage, gameObject); continue; }
            AOGNeutralBossAI b = c.GetComponentInParent<AOGNeutralBossAI>(); if (b != null) { b.TakeDamage(damage, gameObject); continue; }
            TowerHealth t = c.GetComponentInParent<TowerHealth>(); if (t != null && t.towerTeam != stats.team) t.TakeDamage(damage * 0.35f);
        }
    }

    private void RemoveAuronShield(){ if(stats!=null){ stats.maxHp = Mathf.Max(1f, stats.maxHp-180f); stats.hp = Mathf.Min(stats.hp, stats.maxHp); } }
    private void RemoveVesperSpeed(){ if(stats!=null) stats.moveSpeed = Mathf.Max(1f, stats.moveSpeed-2f); }

    public string GetAbilityName(int slot)
    {
        if (heroType == AOGExtraHeroType.Auron) return slot==0?"SUNBREAKER":slot==1?"DAWN AEGIS":slot==2?"SOLAR CHARGE":"HEAVENFALL";
        return slot==0?"VOID ARROW":slot==1?"PHASE HUNT":slot==2?"BLINK SHOT":"STARLESS NIGHT";
    }
    public float GetAbilityCooldownDuration(int slot)=>slot==0?5f:slot==1?10f:slot==2?8f:48f;
    public float GetAbilityCooldownRatio(int slot){ float next=slot==0?nextQ:slot==1?nextW:slot==2?nextE:nextR; return Mathf.Clamp01((next-Time.time)/GetAbilityCooldownDuration(slot)); }
}

[DefaultExecutionOrder(-820)]
public class AOGExpandedHeroRosterRuntime : MonoBehaviour
{
    private bool built;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install(){ GameObject host=new GameObject("AOG_Expanded_Hero_Roster"); DontDestroyOnLoad(host); host.AddComponent<AOGExpandedHeroRosterRuntime>(); }

    private void Update()
    {
        if (built) return;
        GameObject selectCanvas = GameObject.Find("ChampionSelectCanvas");
        AOGChampionSelectionRuntime selector = FindFirstObjectByType<AOGChampionSelectionRuntime>();
        if (selectCanvas == null || selector == null) return;

        AOGActiveChampion auron = CreateHero("Auron_Player", "auron", "AURON", "SOLAR VANGUARD", AOGExtraHeroType.Auron, new Color(1f,0.56f,0.12f), new Vector3(4f,0.2f,2f));
        AOGActiveChampion vesper = CreateHero("Vesper_Player", "vesper", "VESPER", "VOID ARCHER", AOGExtraHeroType.Vesper, new Color(0.12f,0.78f,0.95f), new Vector3(-4f,0.2f,2f));
        AddSelectButton(selectCanvas.transform, selector, auron, new Vector2(-520f,-410f));
        AddSelectButton(selectCanvas.transform, selector, vesper, new Vector2(520f,-410f));
        built = true;
    }

    private AOGActiveChampion CreateHero(string objectName,string id,string display,string role,AOGExtraHeroType type,Color accent,Vector3 offset)
    {
        GameObject go=new GameObject(objectName); Transform spawn=FindSpawn(); go.transform.position=(spawn!=null?spawn.position:Vector3.zero)+offset;
        BuildVisual(go.transform,type,accent);
        AOGCharacterStats stats=go.AddComponent<AOGCharacterStats>(); stats.team=MinionTeam.Blue; stats.maxHp=type==AOGExtraHeroType.Auron?1250f:780f; stats.hp=stats.maxHp; stats.moveSpeed=type==AOGExtraHeroType.Auron?5.6f:6.7f; stats.attackDamage=type==AOGExtraHeroType.Auron?72f:58f; stats.attackRange=type==AOGExtraHeroType.Auron?2.5f:6.2f; stats.attackCooldown=type==AOGExtraHeroType.Auron?1.05f:0.86f;
        go.AddComponent<ChampionAudioController>(); go.AddComponent<ChampionPresentationController>(); go.AddComponent<AOGPlayerMOBAController>().enabled=false; go.AddComponent<AOGPlayerEconomy>(); go.AddComponent<AOGChampionProgression>(); go.AddComponent<AOGAutoAttackPresentationRuntime>();
        CapsuleCollider col=go.AddComponent<CapsuleCollider>(); col.center=new Vector3(0f,1.1f,0f); col.height=2.4f; col.radius=0.65f; Rigidbody rb=go.AddComponent<Rigidbody>(); rb.isKinematic=true; rb.useGravity=false;
        AOGActiveChampion marker=go.AddComponent<AOGActiveChampion>(); marker.championId=id; marker.displayName=display; marker.roleName=role; marker.accentColor=accent; marker.SetActiveChampion(false);
        AOGExtraHeroSkillSet kit=go.AddComponent<AOGExtraHeroSkillSet>(); kit.heroType=type;
        return marker;
    }

    private void AddSelectButton(Transform canvas,AOGChampionSelectionRuntime selector,AOGActiveChampion hero,Vector2 pos)
    {
        GameObject go=new GameObject("Select_"+hero.championId,typeof(RectTransform),typeof(Image),typeof(Button)); go.transform.SetParent(canvas,false);
        RectTransform r=go.GetComponent<RectTransform>(); r.anchorMin=r.anchorMax=new Vector2(0.5f,0.5f); r.anchoredPosition=pos; r.sizeDelta=new Vector2(320f,58f); go.GetComponent<Image>().color=new Color(hero.accentColor.r*0.45f,hero.accentColor.g*0.45f,hero.accentColor.b*0.45f,0.98f);
        GameObject tgo=new GameObject("Text",typeof(RectTransform),typeof(Text)); tgo.transform.SetParent(go.transform,false); RectTransform tr=tgo.GetComponent<RectTransform>(); tr.anchorMin=Vector2.zero; tr.anchorMax=Vector2.one; tr.offsetMin=tr.offsetMax=Vector2.zero; Text t=tgo.GetComponent<Text>(); t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); t.text="ENTER AS "+hero.displayName; t.fontSize=20; t.alignment=TextAnchor.MiddleCenter; t.color=Color.white;
        MethodInfo method=typeof(AOGChampionSelectionRuntime).GetMethod("SelectChampion",BindingFlags.Instance|BindingFlags.NonPublic); go.GetComponent<Button>().onClick.AddListener(()=>method?.Invoke(selector,new object[]{hero}));
    }

    private static Transform FindSpawn(){ GameObject g=GameObject.Find("BlueSpawn")??GameObject.Find("Blue_Spawn")??GameObject.Find("BlueBaseSpawn"); return g!=null?g.transform:null; }

    private static void BuildVisual(Transform parent,AOGExtraHeroType type,Color accent)
    {
        Shader shader=Shader.Find("Universal Render Pipeline/Lit"); if(shader==null)shader=Shader.Find("Standard"); Material armor=new Material(shader){color=type==AOGExtraHeroType.Auron?new Color(0.20f,0.12f,0.05f):new Color(0.035f,0.09f,0.13f)}; Material energy=new Material(shader){color=accent}; if(energy.HasProperty("_EmissionColor")){energy.EnableKeyword("_EMISSION");energy.SetColor("_EmissionColor",accent*4f);} 
        GameObject root=new GameObject(type+"_Visual"); root.transform.SetParent(parent,false);
        Create(PrimitiveType.Capsule,"Body",root.transform,new Vector3(0,1.2f,0),type==AOGExtraHeroType.Auron?new Vector3(0.9f,1.15f,0.72f):new Vector3(0.62f,1.0f,0.50f),armor);
        Create(PrimitiveType.Sphere,"Head",root.transform,new Vector3(0,2.55f,0),new Vector3(0.55f,0.62f,0.55f),armor);
        if(type==AOGExtraHeroType.Auron){ Create(PrimitiveType.Cube,"Blade",root.transform,new Vector3(0.9f,1.2f,0.15f),new Vector3(0.16f,1.25f,0.20f),energy); Create(PrimitiveType.Cylinder,"Shield",root.transform,new Vector3(-0.85f,1.25f,0.15f),new Vector3(0.58f,0.12f,0.58f),armor).transform.localRotation=Quaternion.Euler(90,0,0); }
        else { Transform bow=Create(PrimitiveType.Cube,"Bow",root.transform,new Vector3(0.75f,1.35f,0.2f),new Vector3(0.10f,1.15f,0.10f),energy).transform; bow.localRotation=Quaternion.Euler(0,0,-18f); Create(PrimitiveType.Sphere,"Core",root.transform,new Vector3(0,2.0f,0.35f),Vector3.one*0.22f,energy); }
    }

    private static GameObject Create(PrimitiveType type,string name,Transform parent,Vector3 pos,Vector3 scale,Material mat){ GameObject g=GameObject.CreatePrimitive(type); g.name=name; g.transform.SetParent(parent,false); g.transform.localPosition=pos; g.transform.localScale=scale; g.GetComponent<Renderer>().sharedMaterial=mat; Object.Destroy(g.GetComponent<Collider>()); return g; }
}
