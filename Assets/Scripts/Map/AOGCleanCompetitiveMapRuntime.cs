using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Replaces only the legacy visual map presentation with a lightweight competitive three-lane world.
/// Gameplay paths, minion spawner, towers, objectives, AI and combat components remain authoritative.
/// </summary>
[DefaultExecutionOrder(16100)]
public class AOGCleanCompetitiveMapRuntime : MonoBehaviour
{
    public static AOGCleanCompetitiveMapRuntime Instance { get; private set; }

    private const string CleanRootName = "AOG_Clean_Competitive_Map";
    private Transform root;
    private bool built;

    private Material groundMat;
    private Material laneMat;
    private Material laneEdgeMat;
    private Material riverMat;
    private Material stoneMat;
    private Material darkStoneMat;
    private Material foliageMat;
    private Material trunkMat;
    private Material blueEnergyMat;
    private Material redEnergyMat;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindFirstObjectByType<AOGCleanCompetitiveMapRuntime>() != null) return;
        GameObject host = new GameObject("AOG_Clean_Competitive_Map_Runtime");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGCleanCompetitiveMapRuntime>();
    }

    private void Awake()
    {
        Instance = this;
    }

    private IEnumerator Start()
    {
        yield return new WaitForSecondsRealtime(0.9f);
        BuildWhenReady();
    }

    private void BuildWhenReady()
    {
        if (built) return;
        MinionSpawner spawner = FindFirstObjectByType<MinionSpawner>();
        if (spawner == null || spawner.blueBaseSpawn == null || spawner.redBaseSpawn == null)
        {
            StartCoroutine(Retry());
            return;
        }

        built = true;
        DisableLegacyVisualMap();
        DisableCompetingMapPresentationRuntimes();
        BuildMaterials();

        GameObject existing = GameObject.Find(CleanRootName);
        if (existing != null) Destroy(existing);

        GameObject rootObject = new GameObject(CleanRootName);
        root = rootObject.transform;

        BuildGround();
        BuildLane("Mid_Lane",spawner.blueBaseSpawn,spawner.midLaneWaypoints,spawner.redBaseSpawn,13.5f);
        BuildLane("Top_Lane",spawner.blueBaseSpawn,spawner.topLaneWaypoints,spawner.redBaseSpawn,12.5f);
        BuildLane("Bot_Lane",spawner.blueBaseSpawn,spawner.botLaneWaypoints,spawner.redBaseSpawn,12.5f);
        BuildRiver();
        BuildBasePlatforms(spawner.blueBaseSpawn.position,spawner.redBaseSpawn.position);
        BuildTowerPresentation();
        BuildJungleMasses(spawner);
        BuildObjectiveClearings();
        BuildOuterFrame();
        ApplyLightingBudget();
    }

    private IEnumerator Retry()
    {
        yield return new WaitForSecondsRealtime(0.5f);
        BuildWhenReady();
    }

    private void DisableLegacyVisualMap()
    {
        GameObject oldMap = GameObject.Find("AOG_Symmetric_Reference_Map");
        if (oldMap == null) return;

        foreach (Renderer renderer in oldMap.GetComponentsInChildren<Renderer>(true))
            if (renderer != null) renderer.enabled = false;

        foreach (Collider collider in oldMap.GetComponentsInChildren<Collider>(true))
            if (collider != null) collider.enabled = false;
    }

    private static void DisableCompetingMapPresentationRuntimes()
    {
        string[] blockedTypes =
        {
            "AOGPremiumMapVisualRuntime",
            "AOGMapSpatialIdentityRuntime",
            "AOGDistinctJungleFlowRuntime",
            "AOGMapGameplayBeautyRuntime",
            "AOGBenchmarkMapArtDirectionRuntime"
        };

        foreach (MonoBehaviour behaviour in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include,FindObjectsSortMode.None))
        {
            if (behaviour == null) continue;
            string typeName = behaviour.GetType().Name;
            foreach (string blocked in blockedTypes)
            {
                if (typeName == blocked)
                {
                    behaviour.enabled = false;
                    break;
                }
            }
        }
    }

    private void BuildMaterials()
    {
        groundMat = Lit("Clean_Ground",new Color(0.035f,0.105f,0.055f),0.12f,0.02f);
        laneMat = Lit("Clean_Lane_Stone",new Color(0.27f,0.245f,0.205f),0.30f,0.06f);
        laneEdgeMat = Lit("Clean_Lane_Edge",new Color(0.105f,0.095f,0.082f),0.24f,0.13f);
        riverMat = Lit("Clean_Aether_River",new Color(0.015f,0.145f,0.205f),0.72f,0.05f);
        stoneMat = Lit("Clean_Stone",new Color(0.22f,0.215f,0.20f),0.28f,0.10f);
        darkStoneMat = Lit("Clean_Dark_Stone",new Color(0.055f,0.058f,0.065f),0.24f,0.18f);
        foliageMat = Lit("Clean_Foliage",new Color(0.018f,0.12f,0.045f),0.08f,0f);
        trunkMat = Lit("Clean_Trunk",new Color(0.10f,0.055f,0.028f),0.08f,0f);
        blueEnergyMat = Emissive("Clean_Blue_Energy",new Color(0.06f,0.42f,1f),2.8f);
        redEnergyMat = Emissive("Clean_Red_Energy",new Color(0.86f,0.04f,0.07f),2.6f);
    }

    private void BuildGround()
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "Clean_Playable_Ground";
        ground.transform.SetParent(root,false);
        ground.transform.position = new Vector3(0f,-0.55f,0f);
        ground.transform.localScale = new Vector3(340f,1f,270f);
        ground.GetComponent<Renderer>().sharedMaterial = groundMat;
        BoxCollider collider = ground.GetComponent<BoxCollider>();
        collider.size = Vector3.one;
    }

    private void BuildLane(string name,Transform blueBase,Transform[] waypoints,Transform redBase,float width)
    {
        List<Vector3> points = new List<Vector3> { blueBase.position };
        if (waypoints != null)
            foreach (Transform waypoint in waypoints) if (waypoint != null) points.Add(waypoint.position);
        points.Add(redBase.position);

        GameObject group = new GameObject(name);
        group.transform.SetParent(root,false);
        for (int i=0;i<points.Count-1;i++)
        {
            Vector3 a = points[i];
            Vector3 b = points[i+1];
            CreateSegment(group.transform,name+"_Stone_"+i,a,b,width,0.12f,laneMat,0.04f);
            CreateParallelEdge(group.transform,name+"_EdgeL_"+i,a,b,width*0.5f+0.7f,0.55f,laneEdgeMat);
            CreateParallelEdge(group.transform,name+"_EdgeR_"+i,a,b,-width*0.5f-0.7f,0.55f,laneEdgeMat);
        }
    }

    private void BuildRiver()
    {
        GameObject group = new GameObject("Aether_River");
        group.transform.SetParent(root,false);
        Vector3[] points =
        {
            new Vector3(-88f,0.02f,42f),
            new Vector3(-52f,0.02f,28f),
            new Vector3(-15f,0.02f,10f),
            new Vector3(15f,0.02f,-10f),
            new Vector3(52f,0.02f,-28f),
            new Vector3(88f,0.02f,-42f)
        };
        for (int i=0;i<points.Length-1;i++)
            CreateSegment(group.transform,"River_"+i,points[i],points[i+1],17f,0.07f,riverMat,0.015f);
    }

    private void BuildBasePlatforms(Vector3 blue,Vector3 red)
    {
        BuildBase("Blue_Celestial_Base",blue,blueEnergyMat);
        BuildBase("Red_Fallen_Base",red,redEnergyMat);
    }

    private void BuildBase(string name,Vector3 center,Material energy)
    {
        GameObject group = new GameObject(name);
        group.transform.SetParent(root,false);
        CreateDisk(group.transform,"Outer",center+Vector3.up*0.06f,30f,0.18f,stoneMat);
        CreateDisk(group.transform,"Inner",center+Vector3.up*0.18f,19f,0.20f,darkStoneMat);
        CreateDisk(group.transform,"Core_Rune",center+Vector3.up*0.32f,10f,0.08f,energy);
    }

    private void BuildTowerPresentation()
    {
        foreach (TowerHealth tower in AOGWorldRegistry.Towers)
        {
            if (tower == null || tower.GetComponent<AOGSealCombatTargetAdapter>() != null) continue;

            foreach (Renderer renderer in tower.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null) continue;
                string n = renderer.gameObject.name.ToLowerInvariant();
                if (n.Contains("health") || n.Contains("bar") || n.Contains("range")) continue;
                renderer.enabled = false;
            }

            BuildTowerShell(tower.transform,tower.towerTeam);
        }
    }

    private void BuildTowerShell(Transform parent,MinionTeam team)
    {
        Transform shell = new GameObject("Clean_Tower_Shell").transform;
        shell.SetParent(parent,false);
        Material energy = team == MinionTeam.Blue ? blueEnergyMat : redEnergyMat;

        CreatePrimitive(shell,PrimitiveType.Cylinder,"Base",new Vector3(0f,0.55f,0f),new Vector3(2.3f,0.55f,2.3f),stoneMat,false);
        CreatePrimitive(shell,PrimitiveType.Cylinder,"Body",new Vector3(0f,3.0f,0f),new Vector3(1.25f,2.7f,1.25f),darkStoneMat,false);
        CreatePrimitive(shell,PrimitiveType.Cylinder,"Crown",new Vector3(0f,5.7f,0f),new Vector3(1.9f,0.42f,1.9f),stoneMat,false);
        GameObject crystal = CreatePrimitive(shell,PrimitiveType.Sphere,"Energy_Core",new Vector3(0f,7.1f,0f),Vector3.one*1.15f,energy,false);
        crystal.transform.localScale = new Vector3(0.72f,1.45f,0.72f);

        for (int i=0;i<4;i++)
        {
            float a = i*Mathf.PI*0.5f;
            Vector3 p = new Vector3(Mathf.Cos(a)*1.35f,6.5f,Mathf.Sin(a)*1.35f);
            GameObject fin = CreatePrimitive(shell,PrimitiveType.Cube,"Crown_Fin_"+i,p,new Vector3(0.20f,1.3f,0.55f),stoneMat,false);
            fin.transform.localRotation = Quaternion.Euler(0f,-a*Mathf.Rad2Deg,20f);
        }
    }

    private void BuildJungleMasses(MinionSpawner spawner)
    {
        Random.InitState(99173);
        GameObject jungle = new GameObject("Clean_Jungle_Masses");
        jungle.transform.SetParent(root,false);

        Vector3[] centers =
        {
            new Vector3(-78f,0f,28f), new Vector3(-58f,0f,-24f), new Vector3(-34f,0f,58f),
            new Vector3(-25f,0f,-55f), new Vector3(22f,0f,54f), new Vector3(38f,0f,-58f),
            new Vector3(62f,0f,24f), new Vector3(78f,0f,-28f)
        };

        foreach (Vector3 center in centers)
        {
            for (int i=0;i<7;i++)
            {
                float angle = Random.Range(0f,Mathf.PI*2f);
                float radius = Random.Range(1.5f,8f);
                Vector3 p = center+new Vector3(Mathf.Cos(angle)*radius,0f,Mathf.Sin(angle)*radius);
                BuildTree(jungle.transform,p,Random.Range(0.85f,1.25f));
            }
        }

        BuildBrushPatch(jungle.transform,new Vector3(-22f,0f,14f),8f);
        BuildBrushPatch(jungle.transform,new Vector3(22f,0f,-14f),8f);
        BuildBrushPatch(jungle.transform,new Vector3(-48f,0f,-18f),7f);
        BuildBrushPatch(jungle.transform,new Vector3(48f,0f,18f),7f);
    }

    private void BuildTree(Transform parent,Vector3 position,float scale)
    {
        GameObject trunk = CreatePrimitive(parent,PrimitiveType.Cylinder,"Tree_Trunk",position+Vector3.up*(1.4f*scale),new Vector3(0.45f*scale,1.4f*scale,0.45f*scale),trunkMat,false);
        Renderer trunkRenderer = trunk.GetComponent<Renderer>();
        trunkRenderer.shadowCastingMode = ShadowCastingMode.Off;

        GameObject crown = CreatePrimitive(parent,PrimitiveType.Sphere,"Tree_Crown",position+Vector3.up*(3.6f*scale),new Vector3(2.2f*scale,2.5f*scale,2.2f*scale),foliageMat,false);
        crown.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
    }

    private void BuildBrushPatch(Transform parent,Vector3 center,float radius)
    {
        GameObject patch = new GameObject("Brush_Patch");
        patch.transform.SetParent(parent,false);
        patch.transform.position = center;
        for (int i=0;i<9;i++)
        {
            float a = i*Mathf.PI*2f/9f;
            Vector3 p = new Vector3(Mathf.Cos(a)*radius*0.55f,0.65f,Mathf.Sin(a)*radius*0.55f);
            GameObject leaf = CreatePrimitive(patch.transform,PrimitiveType.Sphere,"Brush_Leaf_"+i,p,new Vector3(1.8f,0.75f,1.8f),foliageMat,false);
            leaf.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        }
    }

    private void BuildObjectiveClearings()
    {
        BuildClearing("Dragon_Clearing",new Vector3(-38f,0.03f,36f),15f,new Color(0.32f,0.10f,0.03f));
        BuildClearing("Medusa_Clearing",new Vector3(38f,0.03f,-36f),15f,new Color(0.10f,0.05f,0.20f));
        BuildClearing("Titan_Clearing",new Vector3(0f,0.03f,70f),17f,new Color(0.10f,0.04f,0.22f));
    }

    private void BuildClearing(string name,Vector3 center,float radius,Color tint)
    {
        Material mat = Lit(name+"_Mat",Color.Lerp(darkStoneMat.color,tint,0.45f),0.30f,0.18f);
        CreateDisk(root,name,center,radius,0.10f,mat);
    }

    private void BuildOuterFrame()
    {
        CreateWall(new Vector3(0f,2f,136f),new Vector3(340f,4f,3f));
        CreateWall(new Vector3(0f,2f,-136f),new Vector3(340f,4f,3f));
        CreateWall(new Vector3(171f,2f,0f),new Vector3(3f,4f,270f));
        CreateWall(new Vector3(-171f,2f,0f),new Vector3(3f,4f,270f));
    }

    private void CreateWall(Vector3 position,Vector3 scale)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = "Clean_Outer_Wall";
        wall.transform.SetParent(root,false);
        wall.transform.position = position;
        wall.transform.localScale = scale;
        wall.GetComponent<Renderer>().sharedMaterial = darkStoneMat;
    }

    private void ApplyLightingBudget()
    {
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.18f,0.21f,0.23f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.035f,0.055f,0.065f);
        RenderSettings.fogDensity = 0.0024f;

        foreach (Light light in FindObjectsByType<Light>(FindObjectsInactive.Exclude,FindObjectsSortMode.None))
        {
            if (light == null) continue;
            if (light.type == LightType.Directional)
            {
                light.intensity = Mathf.Clamp(light.intensity,0.85f,1.25f);
                light.shadows = LightShadows.Soft;
            }
            else
            {
                light.shadows = LightShadows.None;
            }
        }
    }

    private void CreateParallelEdge(Transform parent,string name,Vector3 a,Vector3 b,float offset,float width,Material material)
    {
        Vector3 delta = b-a;
        delta.y = 0f;
        Vector3 right = new Vector3(delta.z,0f,-delta.x).normalized;
        CreateSegment(parent,name,a+right*offset,b+right*offset,width,0.09f,material,0.06f);
    }

    private void CreateSegment(Transform parent,string name,Vector3 a,Vector3 b,float width,float height,Material material,float y)
    {
        Vector3 delta = b-a;
        float length = delta.magnitude;
        GameObject segment = GameObject.CreatePrimitive(PrimitiveType.Cube);
        segment.name = name;
        segment.transform.SetParent(parent,false);
        segment.transform.position = (a+b)*0.5f+Vector3.up*y;
        segment.transform.rotation = Quaternion.LookRotation(delta.normalized);
        segment.transform.localScale = new Vector3(width,height,length+0.6f);
        segment.GetComponent<Renderer>().sharedMaterial = material;
        Destroy(segment.GetComponent<Collider>());
    }

    private void CreateDisk(Transform parent,string name,Vector3 center,float radius,float height,Material material)
    {
        GameObject disk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        disk.name = name;
        disk.transform.SetParent(parent,false);
        disk.transform.position = center;
        disk.transform.localScale = new Vector3(radius,height,radius);
        disk.GetComponent<Renderer>().sharedMaterial = material;
        Destroy(disk.GetComponent<Collider>());
    }

    private GameObject CreatePrimitive(Transform parent,PrimitiveType type,string name,Vector3 localPosition,Vector3 localScale,Material material,bool keepCollider)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent,false);
        go.transform.localPosition = localPosition;
        go.transform.localScale = localScale;
        go.GetComponent<Renderer>().sharedMaterial = material;
        if (!keepCollider)
        {
            Collider collider = go.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
        }
        return go;
    }

    private static Material Lit(string name,Color color,float smoothness,float metallic)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        Material mat = new Material(shader) { name = name, color = color, enableInstancing = true };
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor",color);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness",smoothness);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic",metallic);
        return mat;
    }

    private static Material Emissive(string name,Color color,float intensity)
    {
        Material mat = Lit(name,color,0.48f,0.14f);
        if (mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor",color*intensity);
        }
        return mat;
    }
}
