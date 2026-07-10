using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Adds lightweight silhouette accents to current champion models without replacing imported meshes.
/// Each champion receives a different readable shape language and restrained emissive accents.
/// </summary>
[DefaultExecutionOrder(15920)]
public class AOGHeroSilhouetteIdentityRuntime : MonoBehaviour
{
    private readonly HashSet<AOGActiveChampion> processed = new HashSet<AOGActiveChampion>();
    private float nextScan;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindFirstObjectByType<AOGHeroSilhouetteIdentityRuntime>() != null) return;
        GameObject host = new GameObject("AOG_Hero_Silhouette_Identity_Runtime");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGHeroSilhouetteIdentityRuntime>();
    }

    private void Update()
    {
        if (Time.unscaledTime < nextScan) return;
        nextScan = Time.unscaledTime + 1f;

        foreach (AOGActiveChampion champion in FindObjectsByType<AOGActiveChampion>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (champion == null || processed.Contains(champion)) continue;
            BuildIdentity(champion);
            processed.Add(champion);
        }
    }

    private void BuildIdentity(AOGActiveChampion champion)
    {
        Transform root = new GameObject("AOG_Hero_Identity_Accents").transform;
        root.SetParent(champion.transform,false);

        string id = champion.championId != null ? champion.championId.ToLowerInvariant() : string.Empty;
        Color accent = champion.accentColor;

        if (id.Contains("lyra")) BuildLyra(root,accent);
        else if (id.Contains("nyra")) BuildNyra(root,accent);
        else if (id.Contains("pyrelle")) BuildPyrelle(root,accent);
        else if (id.Contains("selene")) BuildSelene(root,accent);
        else if (id.Contains("kaelith")) BuildKaelith(root,accent);
        else if (id.Contains("auron")) BuildAuron(root,accent);
        else if (id.Contains("vesper")) BuildVesper(root,accent);
        else BuildGeneric(root,accent);
    }

    private static void BuildLyra(Transform root, Color accent)
    {
        CreateCrescentPair(root,new Vector3(0f,1.45f,-0.28f),1.05f,accent);
        CreateRibbon(root,new Vector3(0f,1.0f,-0.48f),new Vector3(0.18f,1.55f,0.08f),accent);
    }

    private static void BuildNyra(Transform root, Color accent)
    {
        for(int i=0;i<4;i++)
        {
            float a=(i*90f+35f)*Mathf.Deg2Rad;
            GameObject wisp=CreateSphere(root,"Spirit_Fragment_"+i,new Vector3(Mathf.Cos(a)*0.82f,1.3f+i*0.18f,Mathf.Sin(a)*0.82f),0.18f,accent);
            wisp.AddComponent<AOGOrbitAccentRuntime>().angularSpeed=24f+i*7f;
        }
        CreateRibbon(root,new Vector3(0f,1.15f,-0.60f),new Vector3(0.12f,1.8f,0.06f),accent);
        CreateRibbon(root,new Vector3(0.25f,1.05f,-0.52f),new Vector3(0.10f,1.55f,0.05f),Color.Lerp(accent,Color.white,0.35f));
    }

    private static void BuildPyrelle(Transform root, Color accent)
    {
        for(int i=0;i<5;i++)
        {
            float a=i*Mathf.PI*2f/5f;
            CreateShard(root,"Heat_Crown_"+i,new Vector3(Mathf.Cos(a)*0.50f,2.72f,Mathf.Sin(a)*0.50f),new Vector3(0.10f,0.48f,0.10f),accent,Quaternion.Euler(0f,-a*Mathf.Rad2Deg,18f));
        }
        CreateSphere(root,"Ember_Core",new Vector3(0f,1.45f,0.24f),0.20f,Color.Lerp(accent,Color.white,0.28f));
    }

    private static void BuildSelene(Transform root, Color accent)
    {
        Transform ringRoot=new GameObject("Constellation_Mechanism").transform;
        ringRoot.SetParent(root,false); ringRoot.localPosition=new Vector3(0f,1.65f,0f);
        for(int i=0;i<6;i++)
        {
            float a=i*Mathf.PI*2f/6f;
            CreateSphere(ringRoot,"Star_Node_"+i,new Vector3(Mathf.Cos(a)*0.92f,0f,Mathf.Sin(a)*0.92f),0.10f,accent);
        }
        AOGOrbitAccentRuntime orbit=ringRoot.gameObject.AddComponent<AOGOrbitAccentRuntime>(); orbit.angularSpeed=16f;
    }

    private static void BuildKaelith(Transform root, Color accent)
    {
        CreateEclipseRing(root,new Vector3(0f,1.55f,-0.42f),0.95f,accent);
        CreateShard(root,"Eclipse_Fracture_L",new Vector3(-0.62f,1.22f,-0.20f),new Vector3(0.12f,0.82f,0.18f),accent,Quaternion.Euler(0f,0f,34f));
        CreateShard(root,"Eclipse_Fracture_R",new Vector3(0.62f,1.22f,-0.20f),new Vector3(0.12f,0.82f,0.18f),accent,Quaternion.Euler(0f,0f,-34f));
    }

    private static void BuildAuron(Transform root, Color accent)
    {
        GameObject shield=GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        shield.name="Solar_Guard_Backplate"; shield.transform.SetParent(root,false); shield.transform.localPosition=new Vector3(-0.62f,1.2f,0.10f); shield.transform.localRotation=Quaternion.Euler(90f,0f,0f); shield.transform.localScale=new Vector3(0.62f,0.10f,0.62f); shield.GetComponent<Renderer>().sharedMaterial=Emissive(accent,1.8f); RemoveCollider(shield);
        CreateSphere(root,"Solar_Core",new Vector3(0f,1.60f,-0.30f),0.18f,accent);
    }

    private static void BuildVesper(Transform root, Color accent)
    {
        CreateShard(root,"Bow_Fin_L",new Vector3(-0.62f,1.30f,-0.25f),new Vector3(0.08f,0.95f,0.12f),accent,Quaternion.Euler(0f,0f,28f));
        CreateShard(root,"Bow_Fin_R",new Vector3(0.62f,1.30f,-0.25f),new Vector3(0.08f,0.95f,0.12f),accent,Quaternion.Euler(0f,0f,-28f));
    }

    private static void BuildGeneric(Transform root, Color accent)
    {
        CreateSphere(root,"Identity_Core",new Vector3(0f,1.7f,-0.35f),0.16f,accent);
    }

    private static void CreateCrescentPair(Transform root,Vector3 position,float radius,Color accent)
    {
        CreateEclipseRing(root,position,radius,accent);
        GameObject cut=GameObject.CreatePrimitive(PrimitiveType.Sphere); cut.name="Moon_Cutout_Accent"; cut.transform.SetParent(root,false); cut.transform.localPosition=position+new Vector3(0.32f,0f,0f); cut.transform.localScale=Vector3.one*0.95f; cut.GetComponent<Renderer>().sharedMaterial=Lit(new Color(0.02f,0.02f,0.035f),0.2f,0.1f); RemoveCollider(cut);
    }

    private static void CreateEclipseRing(Transform root,Vector3 position,float radius,Color accent)
    {
        GameObject ring=new GameObject("Identity_Energy_Ring"); ring.transform.SetParent(root,false); ring.transform.localPosition=position;
        LineRenderer line=ring.AddComponent<LineRenderer>(); line.loop=true; line.useWorldSpace=false; line.positionCount=48; line.startWidth=0.055f; line.endWidth=0.055f; line.sharedMaterial=Emissive(accent,3f);
        for(int i=0;i<48;i++){float a=i*Mathf.PI*2f/48f; line.SetPosition(i,new Vector3(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius,0f));}
    }

    private static void CreateRibbon(Transform root,Vector3 position,Vector3 scale,Color accent)
    {
        CreateShard(root,"Spectral_Ribbon",position,scale,accent,Quaternion.Euler(18f,0f,16f));
    }

    private static void CreateShard(Transform root,string name,Vector3 position,Vector3 scale,Color accent,Quaternion rotation)
    {
        GameObject shard=GameObject.CreatePrimitive(PrimitiveType.Cube); shard.name=name; shard.transform.SetParent(root,false); shard.transform.localPosition=position; shard.transform.localRotation=rotation; shard.transform.localScale=scale; shard.GetComponent<Renderer>().sharedMaterial=Emissive(accent,2.2f); RemoveCollider(shard);
    }

    private static GameObject CreateSphere(Transform root,string name,Vector3 position,float size,Color accent)
    {
        GameObject sphere=GameObject.CreatePrimitive(PrimitiveType.Sphere); sphere.name=name; sphere.transform.SetParent(root,false); sphere.transform.localPosition=position; sphere.transform.localScale=Vector3.one*size; sphere.GetComponent<Renderer>().sharedMaterial=Emissive(accent,3.5f); RemoveCollider(sphere); return sphere;
    }

    private static Material Lit(Color color,float smoothness,float metallic)
    {
        Shader shader=Shader.Find("Universal Render Pipeline/Lit"); if(shader==null) shader=Shader.Find("Standard"); Material mat=new Material(shader){color=color,enableInstancing=true}; if(mat.HasProperty("_BaseColor"))mat.SetColor("_BaseColor",color); if(mat.HasProperty("_Smoothness"))mat.SetFloat("_Smoothness",smoothness); if(mat.HasProperty("_Metallic"))mat.SetFloat("_Metallic",metallic); return mat;
    }

    private static Material Emissive(Color color,float strength)
    {
        Material mat=Lit(color,0.45f,0.18f); if(mat.HasProperty("_EmissionColor")){mat.EnableKeyword("_EMISSION");mat.SetColor("_EmissionColor",color*strength);} return mat;
    }

    private static void RemoveCollider(GameObject go){Collider c=go.GetComponent<Collider>(); if(c!=null)Object.Destroy(c);}
}

public class AOGOrbitAccentRuntime : MonoBehaviour
{
    public float angularSpeed=24f;
    private void Update(){transform.Rotate(Vector3.up,angularSpeed*Time.deltaTime,Space.Self);}
}
