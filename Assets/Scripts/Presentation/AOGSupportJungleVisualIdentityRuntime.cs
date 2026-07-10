using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Adds champion-specific silhouette/model accents for Seris, Mireva, Dravenor and Nocthyr.
/// Existing imported/procedural character models remain intact; unsupported generic fallback pieces
/// are selectively hidden for melee jungle champions and replaced with stronger role silhouettes.
/// </summary>
[DefaultExecutionOrder(15940)]
public class AOGSupportJungleVisualIdentityRuntime : MonoBehaviour
{
    private readonly HashSet<AOGTeamMemberIdentity> processed = new HashSet<AOGTeamMemberIdentity>();
    private float nextScan;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindFirstObjectByType<AOGSupportJungleVisualIdentityRuntime>() != null) return;
        GameObject host = new GameObject("AOG_Support_Jungle_Visual_Identity_Runtime");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGSupportJungleVisualIdentityRuntime>();
    }

    private void Update()
    {
        if (Time.unscaledTime < nextScan) return;
        nextScan = Time.unscaledTime + 0.8f;

        foreach (AOGTeamMemberIdentity member in FindObjectsByType<AOGTeamMemberIdentity>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {
            if (member == null || processed.Contains(member) || string.IsNullOrEmpty(member.championId)) continue;
            string id = member.championId.ToLowerInvariant();
            if (id != "seris" && id != "mireva" && id != "dravenor" && id != "nocthyr") continue;
            Build(member,id);
            processed.Add(member);
        }
    }

    private void Build(AOGTeamMemberIdentity member,string id)
    {
        if (member.transform.Find("AOG_Support_Jungle_Visual_Identity") != null) return;

        Transform root = new GameObject("AOG_Support_Jungle_Visual_Identity").transform;
        root.SetParent(member.transform,false);

        Color accent = Accent(id);
        Material energy = Emissive(accent,3.8f);
        Material dark = Lit(DarkColor(id),0.42f,0.28f);
        Material secondary = Lit(Color.Lerp(DarkColor(id),accent,0.26f),0.34f,0.16f);

        if (id == "seris") BuildSeris(root,energy,dark,secondary);
        else if (id == "mireva") BuildMireva(root,energy,dark,secondary);
        else if (id == "dravenor")
        {
            DisableGenericMageFallback(member.transform);
            BuildDravenor(root,energy,dark,secondary);
        }
        else
        {
            DisableGenericMageFallback(member.transform);
            BuildNocthyr(root,energy,dark,secondary);
        }
    }

    private static void BuildSeris(Transform root,Material energy,Material dark,Material secondary)
    {
        // High, protective silhouette: veil fins, staff focus, orbiting rescue glyphs.
        CreateShard(root,"Veil_Fin_L",new Vector3(-0.62f,1.62f,-0.28f),new Vector3(0.10f,1.45f,0.30f),secondary,Quaternion.Euler(-12f,0f,-24f));
        CreateShard(root,"Veil_Fin_R",new Vector3(0.62f,1.62f,-0.28f),new Vector3(0.10f,1.45f,0.30f),secondary,Quaternion.Euler(-12f,0f,24f));

        GameObject staff=GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        staff.name="Aether_Rescue_Staff";staff.transform.SetParent(root,false);staff.transform.localPosition=new Vector3(0.82f,1.18f,0.10f);staff.transform.localRotation=Quaternion.Euler(8f,0f,-10f);staff.transform.localScale=new Vector3(0.07f,1.25f,0.07f);staff.GetComponent<Renderer>().sharedMaterial=dark;RemoveCollider(staff);
        CreateOrb(root,"Staff_Focus",new Vector3(1.02f,2.42f,0.10f),0.28f,energy);

        Transform glyphRoot=new GameObject("Seris_Rescue_Glyphs").transform;glyphRoot.SetParent(root,false);glyphRoot.localPosition=new Vector3(0f,1.55f,0f);
        for(int i=0;i<3;i++)
        {
            float a=i*Mathf.PI*2f/3f;
            CreateDisc(glyphRoot,"Shield_Glyph_"+i,new Vector3(Mathf.Cos(a)*0.86f,0.22f*i,Mathf.Sin(a)*0.86f),0.22f,energy);
        }
        AOGSupportJungleVisualMotionRuntime motion=glyphRoot.gameObject.AddComponent<AOGSupportJungleVisualMotionRuntime>();motion.rotationSpeed=24f;motion.bobAmplitude=0.06f;motion.bobSpeed=1.8f;
    }

    private static void BuildMireva(Transform root,Material energy,Material dark,Material secondary)
    {
        // Organic support silhouette: bloom crown, root mantle and floating seed cores.
        for(int i=0;i<6;i++)
        {
            float a=i*Mathf.PI*2f/6f;
            Vector3 p=new Vector3(Mathf.Cos(a)*0.44f,2.62f+0.06f*(i%2),Mathf.Sin(a)*0.44f);
            CreateShard(root,"Bloom_Petal_"+i,p,new Vector3(0.12f,0.52f,0.20f),i%2==0?energy:secondary,Quaternion.Euler(-10f,a*Mathf.Rad2Deg,20f));
        }

        for(int i=0;i<4;i++)
        {
            float side=i<2?-1f:1f;
            float z=(i%2==0?-0.20f:-0.48f);
            GameObject rootBranch=GameObject.CreatePrimitive(PrimitiveType.Capsule);
            rootBranch.name="Warden_Root_"+i;rootBranch.transform.SetParent(root,false);rootBranch.transform.localPosition=new Vector3(side*(0.42f+0.15f*(i%2)),0.92f,z);rootBranch.transform.localRotation=Quaternion.Euler(24f,side*22f,side*(22f+10f*(i%2)));rootBranch.transform.localScale=new Vector3(0.10f,0.72f+0.16f*(i%2),0.10f);rootBranch.GetComponent<Renderer>().sharedMaterial=secondary;RemoveCollider(rootBranch);
        }

        Transform seeds=new GameObject("Living_Seed_Orbit").transform;seeds.SetParent(root,false);seeds.localPosition=new Vector3(0f,1.42f,0f);
        for(int i=0;i<3;i++)
        {
            float a=(i*120f+30f)*Mathf.Deg2Rad;
            CreateOrb(seeds,"Living_Seed_"+i,new Vector3(Mathf.Cos(a)*0.76f,0.15f*i,Mathf.Sin(a)*0.76f),0.16f,energy);
        }
        AOGSupportJungleVisualMotionRuntime motion=seeds.gameObject.AddComponent<AOGSupportJungleVisualMotionRuntime>();motion.rotationSpeed=-18f;motion.bobAmplitude=0.08f;motion.bobSpeed=1.4f;
    }

    private static void BuildDravenor(Transform root,Material energy,Material dark,Material secondary)
    {
        // Low-forward predator silhouette: fang shoulders, forearm claws and spinal hunt trophies.
        CreateShard(root,"Fang_Pauldron_L",new Vector3(-0.78f,1.72f,0.04f),new Vector3(0.20f,0.72f,0.30f),secondary,Quaternion.Euler(-18f,0f,-42f));
        CreateShard(root,"Fang_Pauldron_R",new Vector3(0.78f,1.72f,0.04f),new Vector3(0.20f,0.72f,0.30f),secondary,Quaternion.Euler(-18f,0f,42f));

        for(int side=-1;side<=1;side+=2)
        {
            for(int claw=0;claw<3;claw++)
            {
                CreateShard(root,"Hunt_Claw_"+side+"_"+claw,new Vector3(side*(0.66f+0.07f*claw),0.82f,0.36f+0.08f*claw),new Vector3(0.055f,0.58f+0.08f*claw,0.055f),energy,Quaternion.Euler(58f,side*8f,side*(18f+claw*5f)));
            }
        }

        for(int i=0;i<4;i++)
        {
            CreateShard(root,"Hunt_Trophy_"+i,new Vector3(0f,1.05f+0.34f*i,-0.46f),new Vector3(0.11f,0.38f,0.13f),i==3?energy:dark,Quaternion.Euler(-26f,0f,(i%2==0?8f:-8f)));
        }

        GameObject hunterCore=CreateOrb(root,"Predator_Core",new Vector3(0f,1.48f,0.38f),0.20f,energy);
        AOGSupportJungleVisualMotionRuntime pulse=hunterCore.AddComponent<AOGSupportJungleVisualMotionRuntime>();pulse.pulseAmplitude=0.12f;pulse.pulseSpeed=3.0f;
    }

    private static void BuildNocthyr(Transform root,Material energy,Material dark,Material secondary)
    {
        // Asymmetric assassin silhouette: one long void blade, one hook, floating shadow shards.
        CreateShard(root,"Nightfall_Blade",new Vector3(0.78f,1.16f,0.16f),new Vector3(0.13f,1.55f,0.16f),energy,Quaternion.Euler(8f,0f,-19f));
        CreateShard(root,"Shade_Hook",new Vector3(-0.62f,1.04f,0.24f),new Vector3(0.10f,0.86f,0.14f),secondary,Quaternion.Euler(14f,0f,34f));

        Transform shardOrbit=new GameObject("Nocthyr_Shadow_Shard_Orbit").transform;shardOrbit.SetParent(root,false);shardOrbit.localPosition=new Vector3(0f,1.58f,-0.10f);
        for(int i=0;i<5;i++)
        {
            float a=i*Mathf.PI*2f/5f;
            Vector3 p=new Vector3(Mathf.Cos(a)*0.90f,(i%2)*0.28f,Mathf.Sin(a)*0.64f);
            CreateShard(shardOrbit,"Detached_Shadow_"+i,p,new Vector3(0.08f,0.42f+0.10f*(i%2),0.10f),i%2==0?energy:dark,Quaternion.Euler(i*17f,i*51f,i*29f));
        }
        AOGSupportJungleVisualMotionRuntime motion=shardOrbit.gameObject.AddComponent<AOGSupportJungleVisualMotionRuntime>();motion.rotationSpeed=31f;motion.bobAmplitude=0.09f;motion.bobSpeed=2.3f;

        CreateShard(root,"Asymmetric_Back_Fin",new Vector3(-0.38f,1.84f,-0.44f),new Vector3(0.16f,1.24f,0.24f),dark,Quaternion.Euler(-24f,0f,-21f));
    }

    private static void DisableGenericMageFallback(Transform root)
    {
        foreach(Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if(child.name=="Mage_Focus"||child.name=="Moon_Mantle")child.gameObject.SetActive(false);
        }
    }

    private static GameObject CreateOrb(Transform root,string name,Vector3 position,float size,Material material)
    {
        GameObject go=GameObject.CreatePrimitive(PrimitiveType.Sphere);go.name=name;go.transform.SetParent(root,false);go.transform.localPosition=position;go.transform.localScale=Vector3.one*size;go.GetComponent<Renderer>().sharedMaterial=material;RemoveCollider(go);return go;
    }

    private static void CreateDisc(Transform root,string name,Vector3 position,float size,Material material)
    {
        GameObject go=GameObject.CreatePrimitive(PrimitiveType.Cylinder);go.name=name;go.transform.SetParent(root,false);go.transform.localPosition=position;go.transform.localRotation=Quaternion.Euler(90f,0f,0f);go.transform.localScale=new Vector3(size,0.035f,size);go.GetComponent<Renderer>().sharedMaterial=material;RemoveCollider(go);
    }

    private static void CreateShard(Transform root,string name,Vector3 position,Vector3 scale,Material material,Quaternion rotation)
    {
        GameObject go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(root,false);go.transform.localPosition=position;go.transform.localRotation=rotation;go.transform.localScale=scale;go.GetComponent<Renderer>().sharedMaterial=material;RemoveCollider(go);
    }

    private static Color Accent(string id)
    {
        if(id=="seris")return new Color(0.24f,0.88f,0.95f);
        if(id=="mireva")return new Color(0.18f,0.82f,0.42f);
        if(id=="dravenor")return new Color(0.92f,0.28f,0.08f);
        return new Color(0.28f,0.18f,0.72f);
    }

    private static Color DarkColor(string id)
    {
        if(id=="seris")return new Color(0.035f,0.075f,0.095f);
        if(id=="mireva")return new Color(0.045f,0.095f,0.060f);
        if(id=="dravenor")return new Color(0.11f,0.040f,0.025f);
        return new Color(0.025f,0.020f,0.070f);
    }

    private static Material Lit(Color color,float smoothness,float metallic)
    {
        Shader shader=Shader.Find("Universal Render Pipeline/Lit");if(shader==null)shader=Shader.Find("Standard");Material mat=new Material(shader){color=color,enableInstancing=true};if(mat.HasProperty("_BaseColor"))mat.SetColor("_BaseColor",color);if(mat.HasProperty("_Smoothness"))mat.SetFloat("_Smoothness",smoothness);if(mat.HasProperty("_Metallic"))mat.SetFloat("_Metallic",metallic);return mat;
    }

    private static Material Emissive(Color color,float strength)
    {
        Material mat=Lit(color,0.46f,0.14f);if(mat.HasProperty("_EmissionColor")){mat.EnableKeyword("_EMISSION");mat.SetColor("_EmissionColor",color*strength);}return mat;
    }

    private static void RemoveCollider(GameObject go){Collider c=go.GetComponent<Collider>();if(c!=null)Object.Destroy(c);}
}

public class AOGSupportJungleVisualMotionRuntime : MonoBehaviour
{
    public float rotationSpeed;
    public float bobAmplitude;
    public float bobSpeed=1.5f;
    public float pulseAmplitude;
    public float pulseSpeed=2f;

    private Vector3 basePosition;
    private Vector3 baseScale;

    private void Awake(){basePosition=transform.localPosition;baseScale=transform.localScale;}

    private void Update()
    {
        if(Mathf.Abs(rotationSpeed)>0.01f)transform.Rotate(Vector3.up,rotationSpeed*Time.deltaTime,Space.Self);
        Vector3 p=basePosition;if(bobAmplitude>0f)p.y+=Mathf.Sin(Time.time*bobSpeed)*bobAmplitude;transform.localPosition=p;
        if(pulseAmplitude>0f)transform.localScale=baseScale*(1f+Mathf.Sin(Time.time*pulseSpeed)*pulseAmplitude);
    }
}
