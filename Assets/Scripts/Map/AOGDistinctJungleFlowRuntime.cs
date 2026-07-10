using UnityEngine;

/// <summary>
/// Adds a more distinctive jungle flow on top of the current 3-lane map without replacing
/// lane, camp, objective, tower or movement authority. Paths are visual guidance only and
/// generated landmarks remove colliders so they cannot block champions or minions.
/// </summary>
[DefaultExecutionOrder(15850)]
public class AOGDistinctJungleFlowRuntime : MonoBehaviour
{
    private bool built;
    private Transform root;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindFirstObjectByType<AOGDistinctJungleFlowRuntime>() != null)
            return;

        GameObject host = new GameObject("AOG_Distinct_Jungle_Flow_Runtime");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGDistinctJungleFlowRuntime>();
    }

    private void Update()
    {
        if (built)
            return;

        GameObject map = GameObject.Find("AOG_Symmetric_Reference_Map");
        if (map == null)
            return;

        built = true;
        root = new GameObject("11_Distinct_Jungle_Flow").transform;
        root.SetParent(map.transform, false);

        BuildBlueJungle();
        BuildRedJungle();
        BuildCampPockets();
        BuildRiverThresholds();
    }

    private void BuildBlueJungle()
    {
        Material route = Lit(new Color(0.075f,0.115f,0.105f),0.24f,0.08f);
        Material edge = Emissive(new Color(0.08f,0.44f,0.72f),1.8f);

        Vector3[][] routes =
        {
            new[] { new Vector3(-105,0.03f,-70), new Vector3(-86,0.03f,-48), new Vector3(-70,0.03f,-14), new Vector3(-54,0.03f,14), new Vector3(-38,0.03f,32) },
            new[] { new Vector3(-94,0.03f,-88), new Vector3(-66,0.03f,-74), new Vector3(-42,0.03f,-66), new Vector3(-22,0.03f,-42), new Vector3(-20,0.03f,-22) },
            new[] { new Vector3(-78,0.03f,34), new Vector3(-52,0.03f,54), new Vector3(-20,0.03f,58), new Vector3(-4,0.03f,34) }
        };

        for (int i=0;i<routes.Length;i++)
            BuildRoute("Blue_Jungle_Route_"+i,routes[i],7.2f,route,edge);

        BuildArch(new Vector3(-71,0,-13),22f,true);
        BuildArch(new Vector3(-41,0,-64),-18f,true);
        BuildArch(new Vector3(-20,0,57),72f,true);
    }

    private void BuildRedJungle()
    {
        Material route = Lit(new Color(0.105f,0.070f,0.075f),0.24f,0.08f);
        Material edge = Emissive(new Color(0.72f,0.08f,0.16f),1.7f);

        Vector3[][] routes =
        {
            new[] { new Vector3(104,0.03f,70), new Vector3(86,0.03f,50), new Vector3(72,0.03f,17), new Vector3(52,0.03f,-10), new Vector3(38,0.03f,-32) },
            new[] { new Vector3(92,0.03f,90), new Vector3(68,0.03f,72), new Vector3(44,0.03f,64), new Vector3(26,0.03f,39), new Vector3(18,0.03f,20) },
            new[] { new Vector3(80,0.03f,-31), new Vector3(58,0.03f,-52), new Vector3(24,0.03f,-59), new Vector3(6,0.03f,-38) }
        };

        for (int i=0;i<routes.Length;i++)
            BuildRoute("Red_Jungle_Route_"+i,routes[i],7.2f,route,edge);

        BuildSpirePair(new Vector3(72,0,16),-24f);
        BuildSpirePair(new Vector3(44,0,63),16f);
        BuildSpirePair(new Vector3(24,0,-58),-68f);
    }

    private void BuildCampPockets()
    {
        Vector3[] blue =
        {
            new Vector3(-78,0,34), new Vector3(-70,0,-14), new Vector3(-42,0,-66), new Vector3(-12,0,58), new Vector3(-20,0,-22)
        };
        Vector3[] red =
        {
            new Vector3(78,0,-34), new Vector3(72,0,16), new Vector3(44,0,64), new Vector3(16,0,-60), new Vector3(18,0,20)
        };

        for (int i=0;i<blue.Length;i++)
            BuildPocket("Blue_Camp_Pocket_"+i,blue[i],new Color(0.12f,0.48f,0.76f),i%2==0);
        for (int i=0;i<red.Length;i++)
            BuildPocket("Red_Camp_Pocket_"+i,red[i],new Color(0.72f,0.10f,0.16f),i%2!=0);
    }

    private void BuildRiverThresholds()
    {
        BuildThreshold(new Vector3(-30,0,31),35f,new Color(0.12f,0.58f,0.78f));
        BuildThreshold(new Vector3(30,0,-31),-145f,new Color(0.46f,0.18f,0.76f));
        BuildThreshold(new Vector3(-5,0,18),-15f,new Color(0.18f,0.52f,0.82f));
        BuildThreshold(new Vector3(5,0,-18),165f,new Color(0.62f,0.16f,0.38f));
    }

    private void BuildRoute(string name, Vector3[] points, float width, Material route, Material edge)
    {
        GameObject routeRoot = new GameObject(name);
        routeRoot.transform.SetParent(root,false);

        for (int i=0;i<points.Length-1;i++)
        {
            Vector3 a=points[i];
            Vector3 b=points[i+1];
            Vector3 delta=b-a;
            float length=delta.magnitude;

            GameObject segment=GameObject.CreatePrimitive(PrimitiveType.Cube);
            segment.name=name+"_Segment_"+i;
            segment.transform.SetParent(routeRoot.transform,false);
            segment.transform.position=(a+b)*0.5f;
            segment.transform.rotation=Quaternion.LookRotation(delta.normalized);
            segment.transform.localScale=new Vector3(width,0.07f,length);
            segment.GetComponent<Renderer>().sharedMaterial=route;
            RemoveCollider(segment);

            GameObject centerLine=GameObject.CreatePrimitive(PrimitiveType.Cube);
            centerLine.name=name+"_Aether_Trace_"+i;
            centerLine.transform.SetParent(routeRoot.transform,false);
            centerLine.transform.position=(a+b)*0.5f+Vector3.up*0.045f;
            centerLine.transform.rotation=Quaternion.LookRotation(delta.normalized);
            centerLine.transform.localScale=new Vector3(0.10f,0.035f,length*0.92f);
            centerLine.GetComponent<Renderer>().sharedMaterial=edge;
            RemoveCollider(centerLine);
        }
    }

    private void BuildPocket(string name,Vector3 center,Color accent,bool crescent)
    {
        GameObject pocket=new GameObject(name);
        pocket.transform.SetParent(root,false);
        pocket.transform.position=center;

        Material stone=Lit(new Color(0.055f,0.060f,0.070f),0.28f,0.20f);
        Material glow=Emissive(accent,2.4f);

        for(int i=0;i<7;i++)
        {
            float start=crescent?-110f:70f;
            float arc=220f;
            float angle=(start+(arc/(6f))*i)*Mathf.Deg2Rad;
            float radius=6.4f+(i%2)*0.7f;
            Vector3 p=new Vector3(Mathf.Cos(angle)*radius,0.38f,Mathf.Sin(angle)*radius);

            GameObject shard=GameObject.CreatePrimitive(PrimitiveType.Cube);
            shard.name="Pocket_Stone_"+i;
            shard.transform.SetParent(pocket.transform,false);
            shard.transform.localPosition=p;
            shard.transform.localRotation=Quaternion.Euler(0f,-angle*Mathf.Rad2Deg,12f*(i%2==0?1f:-1f));
            shard.transform.localScale=new Vector3(1.1f,0.75f+(i%3)*0.35f,2.2f);
            shard.GetComponent<Renderer>().sharedMaterial=stone;
            RemoveCollider(shard);
        }

        GameObject core=GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        core.name="Camp_Pocket_Core";
        core.transform.SetParent(pocket.transform,false);
        core.transform.localPosition=new Vector3(0f,0.12f,0f);
        core.transform.localScale=new Vector3(4.2f,0.05f,4.2f);
        core.GetComponent<Renderer>().sharedMaterial=glow;
        RemoveCollider(core);
    }

    private void BuildArch(Vector3 position,float yaw,bool blue)
    {
        Color accent=blue?new Color(0.12f,0.55f,0.95f):new Color(0.8f,0.1f,0.16f);
        GameObject arch=new GameObject("Celestial_Jungle_Arch");
        arch.transform.SetParent(root,false);
        arch.transform.position=position;
        arch.transform.rotation=Quaternion.Euler(0f,yaw,0f);

        CreatePillar(arch.transform,new Vector3(-2.2f,2.4f,0f),5f,accent);
        CreatePillar(arch.transform,new Vector3(2.2f,2.4f,0f),5f,accent);
        CreateLintel(arch.transform,new Vector3(0f,4.8f,0f),new Vector3(5.2f,0.38f,0.55f),accent);
    }

    private void BuildSpirePair(Vector3 position,float yaw)
    {
        GameObject rootObj=new GameObject("Fallen_Jungle_Spire_Pair");
        rootObj.transform.SetParent(root,false);
        rootObj.transform.position=position;
        rootObj.transform.rotation=Quaternion.Euler(0f,yaw,0f);
        CreatePillar(rootObj.transform,new Vector3(-2.0f,2.7f,0f),5.4f,new Color(0.76f,0.08f,0.14f));
        CreatePillar(rootObj.transform,new Vector3(2.0f,2.1f,0.6f),4.2f,new Color(0.58f,0.08f,0.30f));
    }

    private void BuildThreshold(Vector3 position,float yaw,Color accent)
    {
        GameObject marker=new GameObject("River_Jungle_Threshold");
        marker.transform.SetParent(root,false);
        marker.transform.position=position;
        marker.transform.rotation=Quaternion.Euler(0f,yaw,0f);
        CreatePillar(marker.transform,new Vector3(-1.8f,1.8f,0f),3.6f,accent);
        CreatePillar(marker.transform,new Vector3(1.8f,1.8f,0f),3.6f,accent);
    }

    private void CreatePillar(Transform parent,Vector3 localPosition,float height,Color accent)
    {
        GameObject pillar=GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pillar.transform.SetParent(parent,false);
        pillar.transform.localPosition=localPosition;
        pillar.transform.localScale=new Vector3(0.48f,height*0.5f,0.48f);
        pillar.GetComponent<Renderer>().sharedMaterial=Lit(Color.Lerp(new Color(0.05f,0.05f,0.065f),accent,0.20f),0.30f,0.34f);
        RemoveCollider(pillar);

        GameObject cap=GameObject.CreatePrimitive(PrimitiveType.Sphere);
        cap.transform.SetParent(parent,false);
        cap.transform.localPosition=localPosition+Vector3.up*(height*0.5f+0.35f);
        cap.transform.localScale=Vector3.one*0.42f;
        cap.GetComponent<Renderer>().sharedMaterial=Emissive(accent,3f);
        RemoveCollider(cap);
    }

    private void CreateLintel(Transform parent,Vector3 localPosition,Vector3 scale,Color accent)
    {
        GameObject lintel=GameObject.CreatePrimitive(PrimitiveType.Cube);
        lintel.transform.SetParent(parent,false);
        lintel.transform.localPosition=localPosition;
        lintel.transform.localScale=scale;
        lintel.GetComponent<Renderer>().sharedMaterial=Lit(Color.Lerp(new Color(0.06f,0.06f,0.08f),accent,0.18f),0.28f,0.30f);
        RemoveCollider(lintel);
    }

    private static void RemoveCollider(GameObject go)
    {
        Collider c=go.GetComponent<Collider>();
        if(c!=null) Destroy(c);
    }

    private static Material Lit(Color color,float smoothness,float metallic)
    {
        Shader shader=Shader.Find("Universal Render Pipeline/Lit");
        if(shader==null) shader=Shader.Find("Standard");
        Material mat=new Material(shader){color=color,enableInstancing=true};
        if(mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor",color);
        if(mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness",smoothness);
        if(mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic",metallic);
        return mat;
    }

    private static Material Emissive(Color color,float strength)
    {
        Material mat=Lit(color,0.42f,0.12f);
        if(mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor",color*strength);
        }
        return mat;
    }
}
