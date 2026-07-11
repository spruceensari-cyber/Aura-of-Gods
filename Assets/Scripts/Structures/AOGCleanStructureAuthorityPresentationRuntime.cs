using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Ensures that only authoritative TowerHealth and AOGNexusCore objects receive the clean-build
/// structure presentation. Existing combat/health components remain untouched.
/// </summary>
[DefaultExecutionOrder(16180)]
public class AOGCleanStructureAuthorityPresentationRuntime : MonoBehaviour
{
    private readonly HashSet<TowerHealth> processedTowers = new HashSet<TowerHealth>();
    private readonly HashSet<AOGNexusCore> processedNexuses = new HashSet<AOGNexusCore>();
    private static readonly Dictionary<string,Material> materialCache = new Dictionary<string,Material>();
    private float nextRefresh;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindFirstObjectByType<AOGCleanStructureAuthorityPresentationRuntime>() != null) return;
        GameObject host = new GameObject("AOG_Clean_Structure_Authority_Presentation");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGCleanStructureAuthorityPresentationRuntime>();
    }

    private void Update()
    {
        if (Time.unscaledTime < nextRefresh) return;
        nextRefresh = Time.unscaledTime + 1.25f;

        foreach (TowerHealth tower in AOGWorldRegistry.Towers)
        {
            if (tower == null || processedTowers.Contains(tower)) continue;
            ConfigureTower(tower);
            processedTowers.Add(tower);
        }

        foreach (AOGNexusCore nexus in AOGWorldRegistry.Nexuses)
        {
            if (nexus == null || processedNexuses.Contains(nexus)) continue;
            ConfigureNexus(nexus);
            processedNexuses.Add(nexus);
        }

        processedTowers.RemoveWhere(t => t == null);
        processedNexuses.RemoveWhere(n => n == null);
    }

    private static void ConfigureTower(TowerHealth tower)
    {
        if (tower.GetComponent<AOGSealCombatTargetAdapter>() != null) return;

        AOGObjectiveWorldBar objectiveBar = tower.GetComponent<AOGObjectiveWorldBar>();
        if (objectiveBar != null)
        {
            objectiveBar.offset = new Vector3(0f,7.45f,0f);
            objectiveBar.width = 4.0f;
            objectiveBar.height = 0.28f;
        }

        AOGWorldHealthBar duplicateBar = tower.GetComponent<AOGWorldHealthBar>();
        if (duplicateBar != null) duplicateBar.enabled = false;

        if (tower.transform.Find("Clean_Tower_Shell") == null)
            BuildFallbackTowerShell(tower.transform,tower.towerTeam);

        BoxCollider box = tower.GetComponent<BoxCollider>();
        CapsuleCollider capsule = tower.GetComponent<CapsuleCollider>();
        if (box == null && capsule == null)
        {
            box = tower.gameObject.AddComponent<BoxCollider>();
            box.center = new Vector3(0f,2.2f,0f);
            box.size = new Vector3(3.3f,4.6f,3.3f);
            box.isTrigger = false;
        }
    }

    private static void ConfigureNexus(AOGNexusCore nexus)
    {
        AOGObjectiveWorldBar bar = nexus.GetComponent<AOGObjectiveWorldBar>();
        if (bar != null)
        {
            bar.offset = new Vector3(0f,7.2f,0f);
            bar.width = 5.1f;
            bar.height = 0.34f;
        }

        if (nexus.transform.Find("Clean_Nexus_Authority_Frame") != null) return;

        Color accent = nexus.team == MinionTeam.Blue
            ? new Color(0.12f,0.58f,1f)
            : new Color(1f,0.12f,0.18f);
        Material stone = GetMaterial(
            nexus.team+"_Nexus_Frame",
            nexus.team == MinionTeam.Blue ? new Color(0.085f,0.11f,0.16f) : new Color(0.15f,0.045f,0.055f),
            0.34f,
            0.30f,
            false);
        Material energy = GetMaterial(nexus.team+"_Nexus_Energy",accent,0.48f,0.12f,true);

        Transform frame = new GameObject("Clean_Nexus_Authority_Frame").transform;
        frame.SetParent(nexus.transform,false);

        CreatePart(frame,PrimitiveType.Cylinder,"Defense_Plinth",new Vector3(0f,0.25f,0f),new Vector3(4.4f,0.24f,4.4f),stone);
        for (int i=0;i<6;i++)
        {
            float a = i*Mathf.PI*2f/6f;
            Vector3 p = new Vector3(Mathf.Cos(a)*3.4f,1.35f,Mathf.Sin(a)*3.4f);
            GameObject pylon = CreatePart(frame,PrimitiveType.Cube,"Defense_Pylon_"+i,p,new Vector3(0.32f,1.65f,0.58f),stone);
            pylon.transform.localRotation = Quaternion.Euler(-12f,-a*Mathf.Rad2Deg,16f);
        }

        GameObject halo = new GameObject("Nexus_Authority_Halo");
        halo.transform.SetParent(frame,false);
        halo.transform.localPosition = new Vector3(0f,3.1f,0f);
        LineRenderer line = halo.AddComponent<LineRenderer>();
        line.loop = true;
        line.useWorldSpace = false;
        line.positionCount = 40;
        line.startWidth = 0.075f;
        line.endWidth = 0.075f;
        line.sharedMaterial = energy;
        line.shadowCastingMode = ShadowCastingMode.Off;
        for (int i=0;i<line.positionCount;i++)
        {
            float a = i*Mathf.PI*2f/line.positionCount;
            line.SetPosition(i,new Vector3(Mathf.Cos(a)*3.15f,Mathf.Sin(a*2f)*0.10f,Mathf.Sin(a)*3.15f));
        }
    }

    private static void BuildFallbackTowerShell(Transform parent,MinionTeam team)
    {
        Color accent = team == MinionTeam.Blue ? new Color(0.14f,0.60f,1f) : new Color(1f,0.12f,0.18f);
        Material stone = GetMaterial(team+"_Tower_Stone",new Color(0.075f,0.08f,0.10f),0.30f,0.26f,false);
        Material energy = GetMaterial(team+"_Tower_Energy",accent,0.42f,0.10f,true);

        Transform shell = new GameObject("Clean_Tower_Shell").transform;
        shell.SetParent(parent,false);
        CreatePart(shell,PrimitiveType.Cylinder,"Tower_Base",new Vector3(0f,0.45f,0f),new Vector3(2.35f,0.45f,2.35f),stone);
        CreatePart(shell,PrimitiveType.Cylinder,"Tower_Shaft",new Vector3(0f,3.0f,0f),new Vector3(1.15f,2.55f,1.15f),stone);
        CreatePart(shell,PrimitiveType.Cylinder,"Tower_Crown",new Vector3(0f,5.8f,0f),new Vector3(1.85f,0.40f,1.85f),stone);
        GameObject core = CreatePart(shell,PrimitiveType.Sphere,"Tower_Core",new Vector3(0f,7.0f,0f),new Vector3(0.78f,1.35f,0.78f),energy);
        core.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
    }

    private static GameObject CreatePart(Transform parent,PrimitiveType type,string name,Vector3 position,Vector3 scale,Material material)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent,false);
        go.transform.localPosition = position;
        go.transform.localScale = scale;
        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer != null) renderer.sharedMaterial = material;
        Collider collider = go.GetComponent<Collider>();
        if (collider != null) Destroy(collider);
        return go;
    }

    private static Material GetMaterial(string key,Color color,float smoothness,float metallic,bool emission)
    {
        if (materialCache.TryGetValue(key,out Material cached) && cached != null) return cached;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        Material material = new Material(shader) { name = key, color = color, enableInstancing = true };
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor",color);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness",smoothness);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic",metallic);
        if (emission && material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor",color*3.6f);
        }
        materialCache[key] = material;
        return material;
    }
}
