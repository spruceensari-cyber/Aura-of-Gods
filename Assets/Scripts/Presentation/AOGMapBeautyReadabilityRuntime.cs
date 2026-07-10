using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Presentation-only map polish. Keeps the existing gameplay map, lanes, colliders,
/// towers, objectives and AI paths intact while improving readability and PC visual quality.
/// </summary>
[DefaultExecutionOrder(16120)]
public class AOGMapBeautyReadabilityRuntime : MonoBehaviour
{
    private bool built;
    private Transform root;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindFirstObjectByType<AOGMapBeautyReadabilityRuntime>() != null) return;
        GameObject host = new GameObject("AOG_Map_Beauty_Readability_Runtime");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGMapBeautyReadabilityRuntime>();
    }

    private IEnumerator Start()
    {
        yield return new WaitForSecondsRealtime(1.4f);
        Apply();
    }

    private void Apply()
    {
        if (built) return;
        GameObject map = GameObject.Find("AOG_Symmetric_Reference_Map");
        if (map == null) return;

        built = true;
        root = new GameObject("12_Map_Beauty_Readability_Pass").transform;
        root.SetParent(map.transform, false);

        ImproveLighting();
        BuildLaneSurfaceAccents();
        BuildObjectivePitRims();
        BuildFactionTerrainGrades();
        BuildReadableWallEdges();
    }

    private void ImproveLighting()
    {
        RenderSettings.ambientLight = new Color(0.18f,0.20f,0.25f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.045f,0.060f,0.078f);
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.0065f;

        foreach (Light light in FindObjectsByType<Light>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {
            if (light == null) continue;
            if (light.type == LightType.Directional)
            {
                light.intensity = Mathf.Max(light.intensity, 1.25f);
                light.color = Color.Lerp(light.color, new Color(0.82f,0.90f,1f), 0.35f);
                light.shadows = LightShadows.Soft;
                light.shadowStrength = 0.72f;
            }
        }
    }

    private void BuildLaneSurfaceAccents()
    {
        Material laneEdge = Lit(new Color(0.18f,0.19f,0.205f),0.34f,0.22f);
        Material midEnergy = Emissive(new Color(0.18f,0.40f,0.78f),1.65f);
        Material topBronze = Lit(new Color(0.22f,0.18f,0.13f),0.30f,0.24f);
        Material botWet = Lit(new Color(0.08f,0.15f,0.14f),0.48f,0.06f);

        BuildPathTrim("Mid_Readability_Spine", new[]{new Vector3(-88,0.07f,-62),new Vector3(-48,0.07f,-36),new Vector3(0,0.07f,0),new Vector3(48,0.07f,36),new Vector3(88,0.07f,62)}, 1.05f, laneEdge, midEnergy);
        BuildPathTrim("Top_Citadel_Trim", new[]{new Vector3(-116,0.07f,28),new Vector3(-88,0.07f,58),new Vector3(-48,0.07f,88),new Vector3(8,0.07f,103),new Vector3(82,0.07f,110)}, 0.85f, topBronze, null);
        BuildPathTrim("Bot_Drowned_Trim", new[]{new Vector3(-82,0.07f,-110),new Vector3(-8,0.07f,-103),new Vector3(48,0.07f,-88),new Vector3(88,0.07f,-58),new Vector3(116,0.07f,-28)}, 0.85f, botWet, null);
    }

    private void BuildObjectivePitRims()
    {
        BuildRim(new Vector3(-36,0.10f,28),7.8f,new Color(1f,0.42f,0.10f),"Dragon_Pit_Readability_Rim");
        BuildRim(new Vector3(36,0.10f,-28),7.8f,new Color(0.62f,0.28f,0.92f),"Medusa_Pit_Readability_Rim");
        BuildRim(new Vector3(0,0.10f,-132),15f,new Color(0.46f,0.16f,0.92f),"Titan_Sanctuary_Readability_Rim");
    }

    private void BuildFactionTerrainGrades()
    {
        Material blueMist = Emissive(new Color(0.06f,0.34f,0.70f),0.75f);
        Material redMist = Emissive(new Color(0.65f,0.05f,0.11f),0.70f);
        CreateFlatAccent("Blue_Base_Terrain_Grade",new Vector3(-102,0.055f,-80),new Vector3(46f,0.04f,36f),blueMist,8f);
        CreateFlatAccent("Red_Base_Terrain_Grade",new Vector3(102,0.055f,80),new Vector3(46f,0.04f,36f),redMist,-172f);
    }

    private void BuildReadableWallEdges()
    {
        Material wallEdge = Emissive(new Color(0.10f,0.20f,0.26f),0.95f);
        Vector3[] points =
        {
            new Vector3(-142,0.15f,114),new Vector3(142,0.15f,114),
            new Vector3(-142,0.15f,-114),new Vector3(142,0.15f,-114),
            new Vector3(-166,0.15f,152),new Vector3(166,0.15f,152),
            new Vector3(-166,0.15f,-152),new Vector3(166,0.15f,-152)
        };
        for(int i=0;i<points.Length;i+=2)
        {
            CreateSegment("Readable_Boundary_Edge_"+i,points[i],points[i+1],0.18f,wallEdge);
        }
    }

    private void BuildPathTrim(string name, Vector3[] points, float width, Material stone, Material energy)
    {
        GameObject group = new GameObject(name);
        group.transform.SetParent(root,false);
        for(int i=0;i<points.Length-1;i++)
        {
            CreateSegment(name+"_Stone_"+i,points[i],points[i+1],width,stone,group.transform);
            if(energy!=null)CreateSegment(name+"_Energy_"+i,points[i]+Vector3.up*0.035f,points[i+1]+Vector3.up*0.035f,0.14f,energy,group.transform);
        }
    }

    private void BuildRim(Vector3 center,float radius,Color color,string name)
    {
        GameObject rim = new GameObject(name);
        rim.transform.SetParent(root,false);
        rim.transform.position = center;
        LineRenderer line = rim.AddComponent<LineRenderer>();
        line.loop = true;
        line.useWorldSpace = false;
        line.positionCount = 96;
        line.startWidth = 0.13f;
        line.endWidth = 0.13f;
        line.sharedMaterial = Emissive(color,2.4f);
        line.shadowCastingMode = ShadowCastingMode.Off;
        for(int i=0;i<96;i++)
        {
            float a=i*Mathf.PI*2f/96f;
            line.SetPosition(i,new Vector3(Mathf.Cos(a)*radius,0f,Mathf.Sin(a)*radius));
        }
    }

    private void CreateFlatAccent(string name,Vector3 position,Vector3 scale,Material material,float yaw)
    {
        GameObject go=GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name=name;
        go.transform.SetParent(root,false);
        go.transform.position=position;
        go.transform.rotation=Quaternion.Euler(0f,yaw,0f);
        go.transform.localScale=scale;
        go.GetComponent<Renderer>().sharedMaterial=material;
        Collider c=go.GetComponent<Collider>(); if(c!=null)Destroy(c);
    }

    private void CreateSegment(string name,Vector3 a,Vector3 b,float width,Material material,Transform parent=null)
    {
        GameObject go=GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name=name;
        go.transform.SetParent(parent!=null?parent:root,false);
        Vector3 delta=b-a;
        go.transform.position=(a+b)*0.5f;
        go.transform.rotation=Quaternion.LookRotation(delta.normalized);
        go.transform.localScale=new Vector3(width,0.055f,delta.magnitude);
        go.GetComponent<Renderer>().sharedMaterial=material;
        Collider c=go.GetComponent<Collider>(); if(c!=null)Destroy(c);
    }

    private static Material Lit(Color color,float smoothness,float metallic)
    {
        Shader shader=Shader.Find("Universal Render Pipeline/Lit");
        if(shader==null)shader=Shader.Find("Standard");
        Material mat=new Material(shader){color=color,enableInstancing=true};
        if(mat.HasProperty("_BaseColor"))mat.SetColor("_BaseColor",color);
        if(mat.HasProperty("_Smoothness"))mat.SetFloat("_Smoothness",smoothness);
        if(mat.HasProperty("_Metallic"))mat.SetFloat("_Metallic",metallic);
        return mat;
    }

    private static Material Emissive(Color color,float strength)
    {
        Material mat=Lit(color,0.38f,0.12f);
        if(mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor",color*strength);
        }
        return mat;
    }
}
