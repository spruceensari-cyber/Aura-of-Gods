using System.Collections;
using UnityEngine;

public class AOGAdvancedBossPatternsRuntime : MonoBehaviour
{
    private AOGNeutralBossAI boss;
    private float nextSpecial;
    private bool casting;

    private void Awake()
    {
        boss=GetComponent<AOGNeutralBossAI>();
        nextSpecial=Time.time+5f;
    }

    private void Update()
    {
        if (boss==null || boss.IsDead || casting) return;
        if (AOGMatchDirector.Instance==null || AOGMatchDirector.Instance.State!=AOGMatchState.Playing) return;
        if (Time.time<nextSpecial) return;

        AOGCharacterStats target=FindNearestChampion(boss.detectionRange+3f);
        if (target==null) { nextSpecial=Time.time+2f; return; }

        nextSpecial=Time.time+(boss.bossType==AOGNeutralBossType.Dragon?7.5f:6.5f);
        StartCoroutine(boss.bossType==AOGNeutralBossType.Dragon?DragonWingShock(target):MedusaVenomPattern(target));
    }

    private IEnumerator DragonWingShock(AOGCharacterStats target)
    {
        casting=true;
        Color c=new Color(1f,0.38f,0.08f);
        GameObject telegraph=AOGAbilityVisuals.CreateRing("Dragon_WingShock_Telegraph",transform.position+Vector3.up*0.06f,5.8f,c,0.12f);
        yield return new WaitForSeconds(0.85f);
        if (telegraph!=null) Destroy(telegraph);

        foreach (Collider hit in Physics.OverlapSphere(transform.position,5.8f,~0,QueryTriggerInteraction.Ignore))
        {
            AOGCharacterStats hero=hit.GetComponentInParent<AOGCharacterStats>();
            if (hero==null || hero.IsDead) continue;
            hero.TakeDamage(boss.attackDamage*0.72f,gameObject);
            Vector3 push=hero.transform.position-transform.position; push.y=0f;
            if (push.sqrMagnitude>0.01f) hero.transform.position+=push.normalized*1.2f;
        }
        Camera.main?.GetComponent<AOGMobaCameraController>()?.AddRandomImpulse(0.42f);
        casting=false;
    }

    private IEnumerator MedusaVenomPattern(AOGCharacterStats target)
    {
        casting=true;
        Vector3 point=target.transform.position;
        Color c=new Color(0.48f,0.16f,0.82f);
        GameObject field=AOGAbilityVisuals.CreateRing("Medusa_Serpent_Field",point+Vector3.up*0.05f,3.4f,c,0.10f);
        yield return new WaitForSeconds(0.65f);

        float end=Time.time+4.5f;
        while (Time.time<end)
        {
            foreach (Collider hit in Physics.OverlapSphere(point,3.4f,~0,QueryTriggerInteraction.Ignore))
            {
                AOGCharacterStats hero=hit.GetComponentInParent<AOGCharacterStats>();
                if (hero!=null && !hero.IsDead) hero.TakeDamage(boss.attackDamage*0.16f,gameObject);
            }
            yield return new WaitForSeconds(0.75f);
        }
        if (field!=null) Destroy(field);
        casting=false;
    }

    private AOGCharacterStats FindNearestChampion(float radius)
    {
        AOGCharacterStats best=null; float bestDistance=radius;
        foreach (AOGCharacterStats hero in FindObjectsByType<AOGCharacterStats>(FindObjectsInactive.Exclude,FindObjectsSortMode.None))
        {
            if (hero==null || hero.IsDead) continue;
            Vector3 a=transform.position;a.y=0f; Vector3 b=hero.transform.position;b.y=0f;
            float d=Vector3.Distance(a,b);
            if (d<bestDistance) { best=hero; bestDistance=d; }
        }
        return best;
    }
}

public class AOGVoidTitanMarker : MonoBehaviour { }

public class AOGVoidTitanCombatRuntime : MonoBehaviour
{
    private AOGNeutralBossAI boss;
    private float nextAttack;
    private int patternIndex;
    private bool casting;

    private void Awake() { boss=GetComponent<AOGNeutralBossAI>(); }

    private void Update()
    {
        if (boss==null || boss.IsDead || casting) return;
        if (AOGMatchDirector.Instance==null || AOGMatchDirector.Instance.State!=AOGMatchState.Playing) return;
        AOGCharacterStats target=FindTarget(15f);
        if (target==null || Time.time<nextAttack) return;
        nextAttack=Time.time+4.2f;
        int pattern=patternIndex++%3;
        if (pattern==0) StartCoroutine(Slam(target));
        else if (pattern==1) StartCoroutine(VoidLine(target));
        else StartCoroutine(PullField());
    }

    private IEnumerator Slam(AOGCharacterStats target)
    {
        casting=true;
        Vector3 point=target.transform.position;
        GameObject ring=AOGAbilityVisuals.CreateRing("Titan_Slam_Telegraph",point+Vector3.up*0.05f,3.8f,new Color(0.40f,0.16f,0.82f),0.13f);
        yield return new WaitForSeconds(1.05f);
        foreach (Collider hit in Physics.OverlapSphere(point,3.8f,~0,QueryTriggerInteraction.Ignore))
        {
            AOGCharacterStats hero=hit.GetComponentInParent<AOGCharacterStats>();
            if (hero!=null && !hero.IsDead) hero.TakeDamage(155f,gameObject);
        }
        if (ring!=null) Destroy(ring);
        Camera.main?.GetComponent<AOGMobaCameraController>()?.AddRandomImpulse(0.55f);
        casting=false;
    }

    private IEnumerator VoidLine(AOGCharacterStats target)
    {
        casting=true;
        Vector3 dir=target.transform.position-transform.position; dir.y=0f; if (dir.sqrMagnitude<0.01f) dir=transform.forward; dir.Normalize();
        Vector3 center=transform.position+dir*5f;
        GameObject telegraph=AOGAbilityVisuals.CreateRing("Titan_VoidLine_Origin",transform.position+Vector3.up*0.05f,1.5f,new Color(0.30f,0.10f,0.72f),0.08f);
        yield return new WaitForSeconds(0.8f);
        foreach (Collider hit in Physics.OverlapBox(center,new Vector3(1.35f,2f,5f),Quaternion.LookRotation(dir),~0,QueryTriggerInteraction.Ignore))
        {
            AOGCharacterStats hero=hit.GetComponentInParent<AOGCharacterStats>();
            if (hero!=null && !hero.IsDead) hero.TakeDamage(125f,gameObject);
        }
        if (telegraph!=null) Destroy(telegraph);
        casting=false;
    }

    private IEnumerator PullField()
    {
        casting=true;
        GameObject ring=AOGAbilityVisuals.CreateRing("Titan_Pull_Field",transform.position+Vector3.up*0.05f,7f,new Color(0.52f,0.20f,0.96f),0.11f);
        float end=Time.time+2.8f;
        while (Time.time<end)
        {
            foreach (AOGCharacterStats hero in FindObjectsByType<AOGCharacterStats>(FindObjectsInactive.Exclude,FindObjectsSortMode.None))
            {
                if (hero==null || hero.IsDead) continue;
                Vector3 delta=transform.position-hero.transform.position; delta.y=0f;
                if (delta.magnitude<=7f && delta.sqrMagnitude>0.04f)
                    hero.transform.position+=delta.normalized*1.6f*Time.deltaTime;
            }
            yield return null;
        }
        if (ring!=null) Destroy(ring);
        casting=false;
    }

    private AOGCharacterStats FindTarget(float radius)
    {
        AOGCharacterStats best=null; float bestDistance=radius;
        foreach (AOGCharacterStats hero in FindObjectsByType<AOGCharacterStats>(FindObjectsInactive.Exclude,FindObjectsSortMode.None))
        {
            if (hero==null || hero.IsDead) continue;
            Vector3 a=transform.position;a.y=0f; Vector3 b=hero.transform.position;b.y=0f;
            float d=Vector3.Distance(a,b);
            if (d<bestDistance) { best=hero;bestDistance=d; }
        }
        return best;
    }
}

public class AOGVoidTitanSpawnerRuntime : MonoBehaviour
{
    public float spawnAtMatchSeconds=600f;
    private bool spawned;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindFirstObjectByType<AOGVoidTitanSpawnerRuntime>()!=null) return;
        GameObject host=new GameObject("AOG_Void_Titan_Spawner"); DontDestroyOnLoad(host); host.AddComponent<AOGVoidTitanSpawnerRuntime>();
    }

    private void Update()
    {
        if (spawned || AOGMatchDirector.Instance==null || AOGMatchDirector.Instance.State!=AOGMatchState.Playing) return;
        if (AOGMatchDirector.Instance.MatchTime<spawnAtMatchSeconds) return;
        spawned=true; SpawnTitan();
    }

    private void SpawnTitan()
    {
        MinionSpawner spawner=FindFirstObjectByType<MinionSpawner>();
        Vector3 center=Vector3.zero;
        if (spawner!=null && spawner.blueBaseSpawn!=null && spawner.redBaseSpawn!=null)
            center=(spawner.blueBaseSpawn.position+spawner.redBaseSpawn.position)*0.5f;
        Vector3 position=center+new Vector3(-12f,0.2f,18f);

        GameObject root=new GameObject("Void_Titan_Late_Objective"); root.transform.position=position;
        CapsuleCollider collider=root.AddComponent<CapsuleCollider>(); collider.center=new Vector3(0f,2.5f,0f); collider.height=5.5f; collider.radius=2.2f;
        AOGNeutralBossAI boss=root.AddComponent<AOGNeutralBossAI>(); boss.bossType=AOGNeutralBossType.Medusa; boss.maxHp=9500f; boss.hp=boss.maxHp; boss.detectionRange=0f; boss.attackDamage=0f; boss.moveSpeed=0f; boss.attackCooldown=999f;
        root.AddComponent<AOGVoidTitanMarker>(); root.AddComponent<AOGVoidTitanCombatRuntime>(); root.AddComponent<AOGObjectiveRewardTrackerRuntime>();
        BuildTitanVisual(root.transform);
        AOGObjectiveWorldBar bar=root.AddComponent<AOGObjectiveWorldBar>(); bar.offset=new Vector3(0f,7.2f,0f); bar.width=5.4f; bar.height=0.38f;
        AOGAbilityVisuals.CreateRing("Void_Titan_Arrival",position+Vector3.up*0.05f,7.5f,new Color(0.42f,0.14f,0.88f),0.15f);
    }

    private static void BuildTitanVisual(Transform root)
    {
        Shader shader=Shader.Find("Universal Render Pipeline/Lit"); if (shader==null) shader=Shader.Find("Standard");
        Material stone=new Material(shader){color=new Color(0.055f,0.045f,0.09f)};
        Material energy=new Material(shader){color=new Color(0.42f,0.16f,0.86f)};
        if (energy.HasProperty("_EmissionColor")){energy.EnableKeyword("_EMISSION");energy.SetColor("_EmissionColor",new Color(0.42f,0.16f,0.86f)*5f);}
        Primitive(PrimitiveType.Capsule,"Titan_Body",root,new Vector3(0,2.8f,0),new Vector3(2.4f,3.2f,2.4f),stone);
        Primitive(PrimitiveType.Sphere,"Titan_Core",root,new Vector3(0,3.2f,0.8f),Vector3.one*0.9f,energy);
        Primitive(PrimitiveType.Cube,"Titan_Shoulder_L",root,new Vector3(-2.0f,4.0f,0),new Vector3(2.2f,1.0f,1.8f),stone);
        Primitive(PrimitiveType.Cube,"Titan_Shoulder_R",root,new Vector3(2.0f,4.0f,0),new Vector3(2.2f,1.0f,1.8f),stone);
        Primitive(PrimitiveType.Cube,"Titan_Arm_L",root,new Vector3(-2.1f,2.3f,0),new Vector3(0.9f,3.2f,0.9f),stone);
        Primitive(PrimitiveType.Cube,"Titan_Arm_R",root,new Vector3(2.1f,2.3f,0),new Vector3(0.9f,3.2f,0.9f),stone);
    }

    private static void Primitive(PrimitiveType type,string name,Transform parent,Vector3 pos,Vector3 scale,Material material)
    {
        GameObject go=GameObject.CreatePrimitive(type); go.name=name; go.transform.SetParent(parent,false); go.transform.localPosition=pos; go.transform.localScale=scale; go.GetComponent<Renderer>().sharedMaterial=material; Destroy(go.GetComponent<Collider>());
    }
}

public class AOGAdvancedBossPatternBootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindFirstObjectByType<AOGAdvancedBossPatternBootstrap>()!=null) return;
        GameObject host=new GameObject("AOG_Advanced_Boss_Pattern_Bootstrap"); DontDestroyOnLoad(host); host.AddComponent<AOGAdvancedBossPatternBootstrap>();
    }
    private float nextScan;
    private void Update()
    {
        if (Time.unscaledTime<nextScan) return; nextScan=Time.unscaledTime+0.8f;
        foreach (AOGNeutralBossAI boss in FindObjectsByType<AOGNeutralBossAI>(FindObjectsInactive.Exclude,FindObjectsSortMode.None))
        {
            if (boss==null || boss.GetComponent<AOGVoidTitanMarker>()!=null) continue;
            if (boss.GetComponent<AOGAdvancedBossPatternsRuntime>()==null) boss.gameObject.AddComponent<AOGAdvancedBossPatternsRuntime>();
        }
    }
}
