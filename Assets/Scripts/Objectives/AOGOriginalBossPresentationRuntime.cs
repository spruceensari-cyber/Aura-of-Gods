using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[DefaultExecutionOrder(16440)]
public class AOGOriginalBossPresentationRuntime : MonoBehaviour
{
    private readonly HashSet<AOGNeutralBossAI> processed = new HashSet<AOGNeutralBossAI>();
    private readonly Dictionary<string,Material> materials = new Dictionary<string,Material>();
    private float nextRefresh;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindFirstObjectByType<AOGOriginalBossPresentationRuntime>() != null) return;
        GameObject host = new GameObject("AOG_Original_Boss_Presentation_Runtime");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGOriginalBossPresentationRuntime>();
    }

    private IEnumerator Start()
    {
        yield return new WaitForSecondsRealtime(1.0f);
        Refresh();
    }

    private void Update()
    {
        if (Time.unscaledTime < nextRefresh) return;
        nextRefresh = Time.unscaledTime + 1.0f;
        Refresh();
    }

    private void Refresh()
    {
        foreach (AOGNeutralBossAI boss in AOGWorldRegistry.Bosses)
        {
            if (boss == null || processed.Contains(boss)) continue;
            BuildBoss(boss);
            processed.Add(boss);
        }
        processed.RemoveWhere(x => x == null);
    }

    private void BuildBoss(AOGNeutralBossAI boss)
    {
        foreach (Renderer r in boss.GetComponentsInChildren<Renderer>(true))
        {
            if (r == null) continue;
            string n = r.gameObject.name.ToLowerInvariant();
            if (n.Contains("clean_") || n.Contains("boss_authority")) continue;
            r.enabled = false;
        }

        Transform root = new GameObject("Boss_Authority_Visual").transform;
        root.SetParent(boss.transform,false);
        root.localPosition = Vector3.zero;

        if (boss.bossType == AOGNeutralBossType.Dragon) BuildDragon(root);
        else BuildMedusa(root);
    }

    private void BuildDragon(Transform root)
    {
        Material scale = Lit("Dragon_Scale",new Color(0.12f,0.035f,0.025f),0.28f,0.22f);
        Material armor = Lit("Dragon_Armor",new Color(0.24f,0.055f,0.035f),0.38f,0.34f);
        Material fire = Emissive("Dragon_Fire",new Color(1f,0.19f,0.03f),4.8f);

        Part(root,PrimitiveType.Capsule,"Clean_Dragon_Torso",new Vector3(0f,2.8f,0f),new Vector3(2.5f,2.2f,4.2f),scale,Quaternion.Euler(90f,0f,0f));
        Part(root,PrimitiveType.Sphere,"Clean_Dragon_Chest",new Vector3(0f,3.2f,1.8f),new Vector3(2.8f,2.3f,2.4f),armor,Quaternion.identity);
        Part(root,PrimitiveType.Capsule,"Clean_Dragon_Neck",new Vector3(0f,4.0f,3.4f),new Vector3(1.2f,1.8f,1.2f),scale,Quaternion.Euler(58f,0f,0f));
        Part(root,PrimitiveType.Sphere,"Clean_Dragon_Head",new Vector3(0f,4.9f,4.4f),new Vector3(1.65f,1.25f,2.0f),armor,Quaternion.identity);
        Part(root,PrimitiveType.Sphere,"Clean_Dragon_Maw",new Vector3(0f,4.65f,5.65f),new Vector3(1.2f,0.65f,1.4f),fire,Quaternion.identity);

        for(int side=-1;side<=1;side+=2)
        {
            Part(root,PrimitiveType.Cube,"Clean_Dragon_Wing_"+side,new Vector3(side*3.3f,4.2f,0.4f),new Vector3(4.8f,0.22f,2.3f),armor,Quaternion.Euler(18f,side*18f,side*-22f));
            Part(root,PrimitiveType.Capsule,"Clean_Dragon_Leg_F_"+side,new Vector3(side*1.7f,1.2f,2.2f),new Vector3(0.65f,1.5f,0.65f),scale,Quaternion.identity);
            Part(root,PrimitiveType.Capsule,"Clean_Dragon_Leg_B_"+side,new Vector3(side*1.8f,1.2f,-1.7f),new Vector3(0.72f,1.6f,0.72f),scale,Quaternion.identity);
            Part(root,PrimitiveType.Cone,"Clean_Dragon_Horn_"+side,new Vector3(side*0.75f,5.9f,4.3f),new Vector3(0.35f,1.2f,0.35f),fire,Quaternion.Euler(-22f,0f,side*14f));
        }

        for(int i=0;i<5;i++)
            Part(root,PrimitiveType.Capsule,"Clean_Dragon_Tail_"+i,new Vector3(0f,2.4f-i*0.18f,-3.6f-i*1.25f),new Vector3(1.35f-i*0.16f,1.1f,1.35f-i*0.16f),scale,Quaternion.Euler(90f,0f,0f));
    }

    private void BuildMedusa(Transform root)
    {
        Material skin = Lit("Medusa_Skin",new Color(0.18f,0.42f,0.34f),0.38f,0.06f);
        Material armor = Lit("Medusa_Armor",new Color(0.11f,0.055f,0.18f),0.46f,0.34f);
        Material venom = Emissive("Medusa_Venom",new Color(0.42f,0.95f,0.52f),3.8f);
        Material voidMat = Emissive("Medusa_Void",new Color(0.50f,0.16f,0.82f),3.5f);

        Part(root,PrimitiveType.Capsule,"Clean_Medusa_Torso",new Vector3(0f,3.8f,0f),new Vector3(1.45f,2.2f,1.3f),armor,Quaternion.identity);
        Part(root,PrimitiveType.Sphere,"Clean_Medusa_Head",new Vector3(0f,6.2f,0f),new Vector3(1.1f,1.2f,1.05f),skin,Quaternion.identity);
        Part(root,PrimitiveType.Sphere,"Clean_Medusa_Eye",new Vector3(0f,6.25f,0.88f),new Vector3(0.42f,0.28f,0.18f),voidMat,Quaternion.identity);

        for(int i=0;i<10;i++)
        {
            float a=i*Mathf.PI*2f/10f;
            Vector3 p=new Vector3(Mathf.Cos(a)*1.1f,6.95f+Mathf.Sin(a*2f)*0.18f,Mathf.Sin(a)*1.1f);
            Part(root,PrimitiveType.Capsule,"Clean_Medusa_Snake_"+i,p,new Vector3(0.22f,1.15f,0.22f),i%2==0?venom:voidMat,Quaternion.Euler(35f, -a*Mathf.Rad2Deg, 22f));
        }

        for(int i=0;i<7;i++)
        {
            float y=2.5f-i*0.42f;
            float z=-i*0.9f;
            float s=1.45f-i*0.11f;
            Part(root,PrimitiveType.Capsule,"Clean_Medusa_Tail_"+i,new Vector3(Mathf.Sin(i*0.8f)*0.55f,y,z),new Vector3(s,0.72f,s),skin,Quaternion.Euler(90f,0f,0f));
        }

        for(int side=-1;side<=1;side+=2)
        {
            Part(root,PrimitiveType.Capsule,"Clean_Medusa_Arm_"+side,new Vector3(side*1.45f,4.3f,0f),new Vector3(0.34f,1.55f,0.34f),skin,Quaternion.Euler(0f,0f,side*24f));
            Part(root,PrimitiveType.Sphere,"Clean_Medusa_Orb_"+side,new Vector3(side*2.0f,3.15f,0.35f),new Vector3(0.45f,0.45f,0.45f),side<0?venom:voidMat,Quaternion.identity);
        }
    }

    private GameObject Part(Transform parent,PrimitiveType type,string name,Vector3 pos,Vector3 scale,Material material,Quaternion rotation)
    {
        GameObject go;
        if(type==PrimitiveType.Cone)
        {
            go=GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            scale=new Vector3(scale.x,scale.y,scale.z);
        }
        else go=GameObject.CreatePrimitive(type);
        go.name=name;
        go.transform.SetParent(parent,false);
        go.transform.localPosition=pos;
        go.transform.localScale=scale;
        go.transform.localRotation=rotation;
        Renderer r=go.GetComponent<Renderer>(); if(r!=null) r.sharedMaterial=material;
        Collider c=go.GetComponent<Collider>(); if(c!=null) Destroy(c);
        return go;
    }

    private Material Lit(string key,Color color,float smoothness,float metallic)
    {
        if(materials.TryGetValue(key,out Material cached)&&cached!=null)return cached;
        Shader shader=Shader.Find("Universal Render Pipeline/Lit"); if(shader==null)shader=Shader.Find("Standard");
        Material mat=new Material(shader){name=key,color=color,enableInstancing=true};
        if(mat.HasProperty("_BaseColor"))mat.SetColor("_BaseColor",color);
        if(mat.HasProperty("_Smoothness"))mat.SetFloat("_Smoothness",smoothness);
        if(mat.HasProperty("_Metallic"))mat.SetFloat("_Metallic",metallic);
        materials[key]=mat; return mat;
    }

    private Material Emissive(string key,Color color,float strength)
    {
        Material mat=Lit(key,color,0.42f,0.12f);
        if(mat.HasProperty("_EmissionColor")){mat.EnableKeyword("_EMISSION");mat.SetColor("_EmissionColor",color*strength);} return mat;
    }
}
