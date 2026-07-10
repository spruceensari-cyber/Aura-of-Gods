using System.Collections;
using UnityEngine;

[DefaultExecutionOrder(15700)]
public class AOGMapSpatialIdentityRuntime : MonoBehaviour
{
    private Transform root;
    private bool built;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindFirstObjectByType<AOGMapSpatialIdentityRuntime>() != null) return;
        GameObject host = new GameObject("AOG_Map_Spatial_Identity_Runtime");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGMapSpatialIdentityRuntime>();
    }

    private IEnumerator Start()
    {
        yield return new WaitForSecondsRealtime(1.25f);
        Build();
    }

    private void Build()
    {
        if (built) return;
        GameObject map = GameObject.Find("AOG_Symmetric_Reference_Map");
        if (map == null) return;

        built = true;
        root = new GameObject("AOG_Map_Spatial_Identity_Pass").transform;
        root.SetParent(map.transform,false);

        BuildJungleGates();
        BuildRiverCrystals();
        BuildLaneTrimLandmarks();
        BuildBaseLandmarks();
    }

    private void BuildJungleGates()
    {
        Vector3[] gates =
        {
            new Vector3(-55,0,18), new Vector3(-30,0,-42), new Vector3(-82,0,-18),
            new Vector3(55,0,-18), new Vector3(30,0,42), new Vector3(82,0,18)
        };

        for (int i=0;i<gates.Length;i++)
        {
            bool blueSide = gates[i].x < 0f;
            Color accent = blueSide ? new Color(0.12f,0.52f,1f) : new Color(0.92f,0.10f,0.12f);
            GameObject gate = new GameObject((blueSide?"Celestial":"Fallen")+"_Jungle_Gate_"+i);
            gate.transform.SetParent(root,false);
            gate.transform.position = gates[i];

            CreatePillar(gate.transform,new Vector3(-2.1f,2.2f,0f),accent,4.4f);
            CreatePillar(gate.transform,new Vector3(2.1f,2.2f,0f),accent,4.4f);
            CreateBeam(gate.transform,new Vector3(0f,4.4f,0f),new Vector3(4.8f,0.34f,0.42f),accent*0.65f);
        }
    }

    private void BuildRiverCrystals()
    {
        Vector3[] points =
        {
            new Vector3(-64,0,34), new Vector3(-22,0,30), new Vector3(0,0,0),
            new Vector3(22,0,-30), new Vector3(64,0,-34)
        };

        for (int i=0;i<points.Length;i++)
        {
            GameObject crystal = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            crystal.name = "River_Aether_Crystal_"+i;
            crystal.transform.SetParent(root,false);
            crystal.transform.position = points[i] + Vector3.up*1.8f;
            crystal.transform.rotation = Quaternion.Euler(0f,0f,45f);
            crystal.transform.localScale = new Vector3(0.55f,1.45f,0.55f);
            crystal.GetComponent<Renderer>().sharedMaterial = Emissive(new Color(0.08f,0.62f,0.82f),3.2f);
            Collider c=crystal.GetComponent<Collider>(); if(c!=null)Destroy(c);

            GameObject ring=AOGAbilityVisuals.CreateRing("River_Crystal_Ring_"+i,points[i]+Vector3.up*0.04f,1.8f,new Color(0.08f,0.52f,0.78f),0.045f);
            ring.transform.SetParent(root,true);
        }
    }

    private void BuildLaneTrimLandmarks()
    {
        Vector3[] markers =
        {
            new Vector3(-90,0,-58),new Vector3(-45,0,-32),new Vector3(0,0,0),new Vector3(45,0,32),new Vector3(90,0,58),
            new Vector3(-105,0,38),new Vector3(-54,0,84),new Vector3(8,0,98),new Vector3(70,0,104),
            new Vector3(-70,0,-104),new Vector3(-8,0,-98),new Vector3(54,0,-84),new Vector3(105,0,-38)
        };

        for(int i=0;i<markers.Length;i++)
        {
            Color c = Mathf.Abs(markers[i].x) < 12f ? new Color(0.52f,0.24f,0.86f) : new Color(0.18f,0.46f,0.62f);
            GameObject stone=GameObject.CreatePrimitive(PrimitiveType.Cube);
            stone.name="Lane_Trim_Stone_"+i;
            stone.transform.SetParent(root,false);
            stone.transform.position=markers[i]+Vector3.up*0.25f;
            stone.transform.rotation=Quaternion.Euler(0f,(i*31f)%180f,0f);
            stone.transform.localScale=new Vector3(2.6f,0.38f,0.9f);
            stone.GetComponent<Renderer>().sharedMaterial=Lit(Color.Lerp(new Color(0.10f,0.11f,0.13f),c,0.15f),0.26f,0.16f);
            Collider col=stone.GetComponent<Collider>();if(col!=null)Destroy(col);
        }
    }

    private void BuildBaseLandmarks()
    {
        BuildBaseCrown(new Vector3(-105,0,-78),true);
        BuildBaseCrown(new Vector3(105,0,78),false);
    }

    private void BuildBaseCrown(Vector3 center,bool blue)
    {
        Color accent=blue?new Color(0.12f,0.56f,1f):new Color(1f,0.12f,0.10f);
        string prefix=blue?"Celestial":"Fallen";
        for(int i=0;i<6;i++)
        {
            float a=i*Mathf.PI*2f/6f;
            Vector3 p=center+new Vector3(Mathf.Cos(a)*24f,0f,Mathf.Sin(a)*24f);
            GameObject tower=new GameObject(prefix+"_Base_Landmark_"+i);
            tower.transform.SetParent(root,false);
            tower.transform.position=p;
            CreatePillar(tower.transform,new Vector3(0f,3f,0f),accent,6f);
            GameObject orb=GameObject.CreatePrimitive(PrimitiveType.Sphere);
            orb.transform.SetParent(tower.transform,false);
            orb.transform.localPosition=new Vector3(0f,6.5f,0f);
            orb.transform.localScale=Vector3.one*0.75f;
            orb.GetComponent<Renderer>().sharedMaterial=Emissive(accent,4f);
            Collider c=orb.GetComponent<Collider>();if(c!=null)Destroy(c);
        }
    }

    private static void CreatePillar(Transform parent,Vector3 pos,Color accent,float height)
    {
        GameObject pillar=GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pillar.transform.SetParent(parent,false);
        pillar.transform.localPosition=pos;
        pillar.transform.localScale=new Vector3(0.65f,height*0.5f,0.65f);
        pillar.GetComponent<Renderer>().sharedMaterial=Lit(Color.Lerp(new Color(0.055f,0.06f,0.075f),accent,0.12f),0.22f,0.28f);
        Collider c=pillar.GetComponent<Collider>();if(c!=null)Destroy(c);
    }

    private static void CreateBeam(Transform parent,Vector3 pos,Vector3 scale,Color color)
    {
        GameObject beam=GameObject.CreatePrimitive(PrimitiveType.Cube);
        beam.transform.SetParent(parent,false);
        beam.transform.localPosition=pos;
        beam.transform.localScale=scale;
        beam.GetComponent<Renderer>().sharedMaterial=Emissive(color,2.2f);
        Collider c=beam.GetComponent<Collider>();if(c!=null)Destroy(c);
    }

    private static Material Lit(Color color,float smoothness,float metallic)
    {
        Shader shader=Shader.Find("Universal Render Pipeline/Lit");if(shader==null)shader=Shader.Find("Standard");
        Material mat=new Material(shader){color=color,enableInstancing=true};
        if(mat.HasProperty("_BaseColor"))mat.SetColor("_BaseColor",color);
        if(mat.HasProperty("_Smoothness"))mat.SetFloat("_Smoothness",smoothness);
        if(mat.HasProperty("_Metallic"))mat.SetFloat("_Metallic",metallic);
        return mat;
    }

    private static Material Emissive(Color color,float strength)
    {
        Material mat=Lit(color,0.38f,0.10f);
        if(mat.HasProperty("_EmissionColor")){mat.EnableKeyword("_EMISSION");mat.SetColor("_EmissionColor",color*strength);}
        return mat;
    }
}
