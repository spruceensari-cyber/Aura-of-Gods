using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Final spatial readability/art layer. It does not replace map geometry or navigation.
/// All generated overlays are collider-free and tuned for desktop tactical readability.
/// </summary>
[DefaultExecutionOrder(16150)]
public class AOGFinalMapArtPassRuntime : MonoBehaviour
{
    private readonly Dictionary<string,Material> materials=new Dictionary<string,Material>();
    private Transform root;
    private bool built;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if(FindFirstObjectByType<AOGFinalMapArtPassRuntime>()!=null)return;
        GameObject host=new GameObject("AOG_Final_Map_Art_Pass");DontDestroyOnLoad(host);host.AddComponent<AOGFinalMapArtPassRuntime>();
    }

    private IEnumerator Start()
    {
        yield return new WaitForSecondsRealtime(1.4f);
        Build();
    }

    private void Build()
    {
        if(built)return;
        GameObject map=GameObject.Find("AOG_Symmetric_Reference_Map");
        if(map==null)return;
        built=true;
        root=new GameObject("12_Final_Map_Art_Pass").transform;root.SetParent(map.transform,false);

        ImproveLightingAndFog();
        BuildLaneInlays();
        BuildRiverEnergyChannel();
        BuildObjectiveRims();
        BuildFactionBaseSilhouettes();
        TuneWorldMaterials();
    }

    private void ImproveLightingAndFog()
    {
        RenderSettings.fog=true;
        RenderSettings.fogMode=FogMode.ExponentialSquared;
        RenderSettings.fogDensity=Mathf.Min(RenderSettings.fogDensity>0f?RenderSettings.fogDensity:0.0035f,0.0045f);
        RenderSettings.fogColor=Color.Lerp(RenderSettings.fogColor,new Color(0.055f,0.075f,0.095f),0.35f);
        RenderSettings.ambientMode=AmbientMode.Trilight;
        RenderSettings.ambientSkyColor=new Color(0.19f,0.25f,0.32f);
        RenderSettings.ambientEquatorColor=new Color(0.10f,0.14f,0.17f);
        RenderSettings.ambientGroundColor=new Color(0.035f,0.045f,0.055f);
        RenderSettings.ambientIntensity=1.08f;

        foreach(Light light in FindObjectsByType<Light>(FindObjectsInactive.Exclude,FindObjectsSortMode.None))
        {
            if(light==null)continue;
            if(light.type==LightType.Directional)
            {
                light.intensity=Mathf.Clamp(light.intensity,1.05f,1.42f);
                light.shadows=LightShadows.Soft;
                light.shadowStrength=0.76f;
            }
        }
    }

    private void BuildLaneInlays()
    {
        MinionSpawner spawner=FindFirstObjectByType<MinionSpawner>();
        if(spawner==null)return;
        BuildLane("Top",spawner.topLaneWaypoints,new Color(0.16f,0.40f,0.50f),0.85f);
        BuildLane("Mid",spawner.midLaneWaypoints,new Color(0.42f,0.20f,0.70f),1.05f);
        BuildLane("Bot",spawner.botLaneWaypoints,new Color(0.12f,0.44f,0.36f),0.85f);
    }

    private void BuildLane(string laneName,Transform[] waypoints,Color accent,float widthMultiplier)
    {
        if(waypoints==null||waypoints.Length<2)return;
        Material stone=Mat("FinalLaneStone_"+laneName,new Color(0.115f,0.125f,0.135f),0.22f,0.08f);
        Material glow=Emission("FinalLaneGlow_"+laneName,accent,1.6f);

        for(int i=0;i<waypoints.Length-1;i++)
        {
            if(waypoints[i]==null||waypoints[i+1]==null)continue;
            Vector3 a=waypoints[i].position+Vector3.up*0.035f;
            Vector3 b=waypoints[i+1].position+Vector3.up*0.035f;
            Vector3 delta=b-a;
            float length=delta.magnitude;
            if(length<0.5f)continue;

            GameObject trim=GameObject.CreatePrimitive(PrimitiveType.Cube);
            trim.name=laneName+"_Final_Inlay_"+i;
            trim.transform.SetParent(root,false);
            trim.transform.position=(a+b)*0.5f;
            trim.transform.rotation=Quaternion.LookRotation(delta.normalized);
            trim.transform.localScale=new Vector3(2.8f*widthMultiplier,0.045f,length*0.94f);
            trim.GetComponent<Renderer>().sharedMaterial=stone;
            RemoveCollider(trim);

            GameObject center=GameObject.CreatePrimitive(PrimitiveType.Cube);
            center.name=laneName+"_Aether_Line_"+i;
            center.transform.SetParent(root,false);
            center.transform.position=(a+b)*0.5f+Vector3.up*0.025f;
            center.transform.rotation=Quaternion.LookRotation(delta.normalized);
            center.transform.localScale=new Vector3(0.08f*widthMultiplier,0.028f,length*0.90f);
            center.GetComponent<Renderer>().sharedMaterial=glow;
            RemoveCollider(center);
        }
    }

    private void BuildRiverEnergyChannel()
    {
        Material water=Mat("AetherRiverSurface",new Color(0.055f,0.18f,0.24f),0.78f,0.08f);
        Material energy=Emission("AetherRiverPulse",new Color(0.08f,0.56f,0.78f),2.4f);
        Vector3[] points={new Vector3(-84f,0.02f,48f),new Vector3(-54f,0.02f,34f),new Vector3(-22f,0.02f,18f),new Vector3(10f,0.02f,-4f),new Vector3(40f,0.02f,-22f),new Vector3(74f,0.02f,-42f)};
        for(int i=0;i<points.Length-1;i++)
        {
            Vector3 a=points[i],b=points[i+1],delta=b-a;
            float length=delta.magnitude;
            GameObject surface=GameObject.CreatePrimitive(PrimitiveType.Cube);surface.name="Aether_River_Surface_"+i;surface.transform.SetParent(root,false);surface.transform.position=(a+b)*0.5f;surface.transform.rotation=Quaternion.LookRotation(delta.normalized);surface.transform.localScale=new Vector3(8.5f,0.035f,length);surface.GetComponent<Renderer>().sharedMaterial=water;RemoveCollider(surface);
            GameObject pulse=GameObject.CreatePrimitive(PrimitiveType.Cube);pulse.name="Aether_River_Pulse_"+i;pulse.transform.SetParent(root,false);pulse.transform.position=(a+b)*0.5f+Vector3.up*0.04f;pulse.transform.rotation=Quaternion.LookRotation(delta.normalized);pulse.transform.localScale=new Vector3(0.12f,0.02f,length*0.92f);pulse.GetComponent<Renderer>().sharedMaterial=energy;RemoveCollider(pulse);
        }
    }

    private void BuildObjectiveRims()
    {
        foreach(AOGNeutralBossAI boss in FindObjectsByType<AOGNeutralBossAI>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {
            if(boss==null)continue;
            bool titan=boss.GetComponent<AOGVoidTitanMarker>()!=null;
            Color accent=titan?new Color(0.54f,0.16f,0.92f):boss.bossType==AOGNeutralBossType.Dragon?new Color(0.96f,0.30f,0.06f):new Color(0.54f,0.22f,0.82f);
            BuildObjectiveRim(boss.transform.position,titan?11f:boss.bossType==AOGNeutralBossType.Dragon?8f:7f,accent,titan?8:6);
        }
    }

    private void BuildObjectiveRim(Vector3 center,float radius,Color accent,int pylons)
    {
        Material stone=Mat("ObjectiveRimStone_"+accent.ToString(),new Color(0.08f,0.085f,0.105f),0.28f,0.24f);
        Material energy=Emission("ObjectiveRimEnergy_"+accent.ToString(),accent,2.8f);
        for(int i=0;i<pylons;i++)
        {
            float a=i*Mathf.PI*2f/pylons;
            Vector3 p=center+new Vector3(Mathf.Cos(a)*radius,0f,Mathf.Sin(a)*radius);
            GameObject pylon=GameObject.CreatePrimitive(PrimitiveType.Cube);pylon.name="Objective_Rim_Pylon_"+i;pylon.transform.SetParent(root,false);pylon.transform.position=p+Vector3.up*1.3f;pylon.transform.rotation=Quaternion.Euler(0f,-a*Mathf.Rad2Deg,14f*(i%2==0?1f:-1f));pylon.transform.localScale=new Vector3(0.45f,2.6f,0.75f);pylon.GetComponent<Renderer>().sharedMaterial=stone;RemoveCollider(pylon);
            GameObject core=GameObject.CreatePrimitive(PrimitiveType.Sphere);core.name="Objective_Rim_Core_"+i;core.transform.SetParent(root,false);core.transform.position=p+Vector3.up*2.85f;core.transform.localScale=Vector3.one*0.30f;core.GetComponent<Renderer>().sharedMaterial=energy;RemoveCollider(core);
        }
    }

    private void BuildFactionBaseSilhouettes()
    {
        foreach(AOGNexusCore nexus in FindObjectsByType<AOGNexusCore>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {
            if(nexus==null)continue;
            bool blue=nexus.team==MinionTeam.Blue;
            Color accent=blue?new Color(0.18f,0.62f,1f):new Color(1f,0.16f,0.22f);
            Material stone=Mat(blue?"CelestialBaseStone":"FallenBaseStone",blue?new Color(0.24f,0.28f,0.34f):new Color(0.16f,0.06f,0.08f),0.32f,0.28f);
            for(int i=0;i<5;i++)
            {
                float a=i*Mathf.PI*2f/5f;
                Vector3 p=nexus.transform.position+new Vector3(Mathf.Cos(a)*9f,0f,Mathf.Sin(a)*9f);
                GameObject spire=GameObject.CreatePrimitive(PrimitiveType.Cube);spire.name=(blue?"Celestial":"Fallen")+"_Base_Spire_"+i;spire.transform.SetParent(root,false);spire.transform.position=p+Vector3.up*(blue?2.4f:2.8f);spire.transform.rotation=Quaternion.Euler(blue?0f:18f,-a*Mathf.Rad2Deg,blue?0f:(i%2==0?18f:-18f));spire.transform.localScale=blue?new Vector3(0.65f,4.8f,0.95f):new Vector3(0.52f,5.6f,0.78f);spire.GetComponent<Renderer>().sharedMaterial=stone;RemoveCollider(spire);
                GameObject mark=GameObject.CreatePrimitive(PrimitiveType.Sphere);mark.name="Base_Energy_Mark";mark.transform.SetParent(root,false);mark.transform.position=p+Vector3.up*(blue?5.2f:5.9f);mark.transform.localScale=Vector3.one*(blue?0.42f:0.34f);mark.GetComponent<Renderer>().sharedMaterial=Emission("BaseMark_"+nexus.team,accent,3.8f);RemoveCollider(mark);
            }
        }
    }

    private void TuneWorldMaterials()
    {
        foreach(Renderer renderer in FindObjectsByType<Renderer>(FindObjectsInactive.Exclude,FindObjectsSortMode.None))
        {
            if(renderer==null||renderer is ParticleSystemRenderer)continue;
            string n=renderer.name.ToLowerInvariant();
            if(n.Contains("ground")||n.Contains("lane")||n.Contains("terrain"))
            {
                foreach(Material material in renderer.materials)
                {
                    if(material==null)continue;
                    if(material.HasProperty("_Smoothness"))material.SetFloat("_Smoothness",Mathf.Clamp(material.GetFloat("_Smoothness"),0.10f,0.34f));
                    if(material.HasProperty("_Metallic"))material.SetFloat("_Metallic",Mathf.Min(material.GetFloat("_Metallic"),0.12f));
                }
            }
            renderer.receiveShadows=true;
        }
    }

    private Material Mat(string key,Color color,float smoothness,float metallic)
    {
        if(materials.TryGetValue(key,out Material existing)&&existing!=null)return existing;
        Shader shader=Shader.Find("Universal Render Pipeline/Lit");if(shader==null)shader=Shader.Find("Standard");Material mat=new Material(shader){name=key,color=color,enableInstancing=true};if(mat.HasProperty("_BaseColor"))mat.SetColor("_BaseColor",color);if(mat.HasProperty("_Smoothness"))mat.SetFloat("_Smoothness",smoothness);if(mat.HasProperty("_Metallic"))mat.SetFloat("_Metallic",metallic);materials[key]=mat;return mat;
    }

    private Material Emission(string key,Color color,float strength)
    {
        Material mat=Mat(key,color,0.42f,0.12f);if(mat.HasProperty("_EmissionColor")){mat.EnableKeyword("_EMISSION");mat.SetColor("_EmissionColor",color*strength);}return mat;
    }

    private static void RemoveCollider(GameObject go){Collider c=go.GetComponent<Collider>();if(c!=null)Destroy(c);}
}
