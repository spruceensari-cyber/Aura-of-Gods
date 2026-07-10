using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Adds distinct Celestial and Fallen tower silhouettes without replacing combat components.
/// </summary>
[DefaultExecutionOrder(16020)]
public class AOGTowerFactionIdentityRuntime : MonoBehaviour
{
    private readonly HashSet<TowerHealth> processed=new HashSet<TowerHealth>();
    private float nextScan;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if(FindFirstObjectByType<AOGTowerFactionIdentityRuntime>()!=null)return;
        GameObject host=new GameObject("AOG_Tower_Faction_Identity_Runtime"); DontDestroyOnLoad(host); host.AddComponent<AOGTowerFactionIdentityRuntime>();
    }

    private void Update()
    {
        if(Time.unscaledTime<nextScan)return;
        nextScan=Time.unscaledTime+1f;
        foreach(TowerHealth tower in FindObjectsByType<TowerHealth>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {
            if(tower==null||processed.Contains(tower))continue;
            Build(tower); processed.Add(tower);
        }
    }

    private static void Build(TowerHealth tower)
    {
        Transform root=new GameObject("AOG_Tower_Faction_Accents").transform; root.SetParent(tower.transform,false);
        bool blue=tower.towerTeam==MinionTeam.Blue;
        Color accent=blue?new Color(0.18f,0.62f,1f):new Color(0.95f,0.10f,0.16f);
        Material stone=Lit(blue?new Color(0.18f,0.23f,0.30f):new Color(0.16f,0.055f,0.07f),0.34f,0.32f);
        Material energy=Emissive(accent,3.4f);

        if(blue)
        {
            for(int i=0;i<4;i++)
            {
                float a=(45f+i*90f)*Mathf.Deg2Rad;
                GameObject fin=GameObject.CreatePrimitive(PrimitiveType.Cube); fin.name="Celestial_Fin_"+i; fin.transform.SetParent(root,false); fin.transform.localPosition=new Vector3(Mathf.Cos(a)*1.65f,4.8f,Mathf.Sin(a)*1.65f); fin.transform.localRotation=Quaternion.Euler(0f,-a*Mathf.Rad2Deg,28f); fin.transform.localScale=new Vector3(0.22f,1.6f,0.48f); fin.GetComponent<Renderer>().sharedMaterial=stone; RemoveCollider(fin);
            }
            CreateOrb(root,new Vector3(0f,7.2f,0f),0.62f,energy,"Celestial_Focus_Core");
        }
        else
        {
            for(int i=0;i<3;i++)
            {
                float a=(i*120f)*Mathf.Deg2Rad;
                GameObject horn=GameObject.CreatePrimitive(PrimitiveType.Cube); horn.name="Fallen_Fracture_Spire_"+i; horn.transform.SetParent(root,false); horn.transform.localPosition=new Vector3(Mathf.Cos(a)*1.45f,5.0f,Mathf.Sin(a)*1.45f); horn.transform.localRotation=Quaternion.Euler(18f,-a*Mathf.Rad2Deg,-22f); horn.transform.localScale=new Vector3(0.26f,1.9f,0.36f); horn.GetComponent<Renderer>().sharedMaterial=stone; RemoveCollider(horn);
            }
            CreateOrb(root,new Vector3(0f,6.8f,0f),0.70f,energy,"Fallen_Rift_Core");
        }

        GameObject ring=new GameObject(blue?"Celestial_Target_Ring":"Fallen_Target_Ring"); ring.transform.SetParent(root,false); ring.transform.localPosition=new Vector3(0f,5.7f,0f); LineRenderer line=ring.AddComponent<LineRenderer>(); line.loop=true; line.useWorldSpace=false; line.positionCount=40; line.startWidth=0.055f; line.endWidth=0.055f; line.sharedMaterial=energy; float radius=1.05f; for(int i=0;i<40;i++){float a=i*Mathf.PI*2f/40f; line.SetPosition(i,new Vector3(Mathf.Cos(a)*radius,0f,Mathf.Sin(a)*radius));}
    }

    private static void CreateOrb(Transform root,Vector3 pos,float size,Material material,string name)
    {
        GameObject orb=GameObject.CreatePrimitive(PrimitiveType.Sphere); orb.name=name; orb.transform.SetParent(root,false); orb.transform.localPosition=pos; orb.transform.localScale=Vector3.one*size; orb.GetComponent<Renderer>().sharedMaterial=material; RemoveCollider(orb);
    }

    private static Material Lit(Color color,float smoothness,float metallic)
    {
        Shader shader=Shader.Find("Universal Render Pipeline/Lit");if(shader==null)shader=Shader.Find("Standard");Material mat=new Material(shader){color=color,enableInstancing=true};if(mat.HasProperty("_BaseColor"))mat.SetColor("_BaseColor",color);if(mat.HasProperty("_Smoothness"))mat.SetFloat("_Smoothness",smoothness);if(mat.HasProperty("_Metallic"))mat.SetFloat("_Metallic",metallic);return mat;
    }

    private static Material Emissive(Color color,float strength)
    {
        Material mat=Lit(color,0.42f,0.15f);if(mat.HasProperty("_EmissionColor")){mat.EnableKeyword("_EMISSION");mat.SetColor("_EmissionColor",color*strength);}return mat;
    }

    private static void RemoveCollider(GameObject go){Collider c=go.GetComponent<Collider>();if(c!=null)Object.Destroy(c);}
}
