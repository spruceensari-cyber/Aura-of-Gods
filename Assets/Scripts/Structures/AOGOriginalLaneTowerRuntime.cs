using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Replaces every legacy small/laser tower presentation with one large original lane tower shell.
/// TowerHealth, TowerAttack, colliders and health bars remain authoritative.
/// </summary>
[DefaultExecutionOrder(16520)]
public class AOGOriginalLaneTowerRuntime : MonoBehaviour
{
    private readonly HashSet<TowerHealth> processed = new HashSet<TowerHealth>();
    private static readonly Dictionary<string,Material> materials = new Dictionary<string,Material>();
    private float nextRefresh;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindFirstObjectByType<AOGOriginalLaneTowerRuntime>() != null) return;
        GameObject host = new GameObject("AOG_Original_Lane_Tower_Runtime");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGOriginalLaneTowerRuntime>();
    }

    private IEnumerator Start()
    {
        yield return new WaitForSecondsRealtime(1.2f);
        Refresh();
    }

    private void Update()
    {
        if (Time.unscaledTime < nextRefresh) return;
        nextRefresh = Time.unscaledTime + 0.9f;
        Refresh();
    }

    private void Refresh()
    {
        foreach (TowerHealth tower in AOGWorldRegistry.Towers)
        {
            if (tower == null || processed.Contains(tower)) continue;
            ReplacePresentation(tower);
            processed.Add(tower);
        }
        processed.RemoveWhere(x => x == null);
    }

    private static void ReplacePresentation(TowerHealth tower)
    {
        if (tower.GetComponent<AOGSealCombatTargetAdapter>() != null) return;

        HideLegacyTowerVisuals(tower.transform);
        Transform existing = tower.transform.Find("AOG_Original_Lane_Tower_Visual");
        if (existing == null) BuildTower(tower);

        AOGObjectiveWorldBar bar = tower.GetComponent<AOGObjectiveWorldBar>();
        if (bar == null) bar = tower.gameObject.AddComponent<AOGObjectiveWorldBar>();
        bar.enabled = true;
        bar.offset = new Vector3(0f, 10.4f, 0f);
        bar.width = 4.6f;
        bar.height = 0.32f;

        AOGWorldHealthBar duplicate = tower.GetComponent<AOGWorldHealthBar>();
        if (duplicate != null) duplicate.enabled = false;

        Collider rootCollider = tower.GetComponent<Collider>();
        if (rootCollider == null)
        {
            CapsuleCollider capsule = tower.gameObject.AddComponent<CapsuleCollider>();
            capsule.center = new Vector3(0f, 3.7f, 0f);
            capsule.height = 7.4f;
            capsule.radius = 2.15f;
        }
        else rootCollider.enabled = true;
    }

    private static void HideLegacyTowerVisuals(Transform tower)
    {
        Transform originalRoot = tower.Find("AOG_Original_Lane_Tower_Visual");

        foreach (LineRenderer line in tower.GetComponentsInChildren<LineRenderer>(true))
        {
            if (line == null) continue;
            if (originalRoot != null && line.transform.IsChildOf(originalRoot)) continue;
            string n = line.gameObject.name.ToLowerInvariant();
            if (n.Contains("hp") || n.Contains("health")) continue;
            line.enabled = false;
        }

        foreach (Renderer renderer in tower.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer == null) continue;
            Transform t = renderer.transform;
            if (t == tower) continue;
            if (originalRoot != null && t.IsChildOf(originalRoot)) continue;

            string n = renderer.gameObject.name.ToLowerInvariant();
            bool keep = n.Contains("hp_") || n.Contains("health") || n.Contains("objective") ||
                        n.Contains("target") || n.Contains("telegraph") || n.Contains("projectile") ||
                        n.Contains("bolt") || n.Contains("impact");
            if (!keep) renderer.enabled = false;
        }
    }

    private static void BuildTower(TowerHealth tower)
    {
        bool blue = tower.towerTeam == MinionTeam.Blue;
        Color accent = blue ? new Color(0.08f,0.54f,1f) : new Color(1f,0.10f,0.16f);
        Color stoneColor = blue ? new Color(0.10f,0.13f,0.18f) : new Color(0.17f,0.075f,0.085f);
        Material stone = MaterialFor((blue?"Blue":"Red")+"_Original_Tower_Stone",stoneColor,0.38f,0.36f,false);
        Material trim = MaterialFor((blue?"Blue":"Red")+"_Original_Tower_Trim",new Color(0.32f,0.28f,0.20f),0.58f,0.62f,false);
        Material energy = MaterialFor((blue?"Blue":"Red")+"_Original_Tower_Energy",accent,0.52f,0.10f,true);

        Transform root = new GameObject("AOG_Original_Lane_Tower_Visual").transform;
        root.SetParent(tower.transform,false);

        Part(root,PrimitiveType.Cylinder,"Tower_Foundation",new Vector3(0f,0.35f,0f),new Vector3(3.7f,0.40f,3.7f),stone,Quaternion.identity);
        Part(root,PrimitiveType.Cylinder,"Tower_Step",new Vector3(0f,0.95f,0f),new Vector3(3.05f,0.34f,3.05f),trim,Quaternion.identity);
        Part(root,PrimitiveType.Cylinder,"Tower_Main_Shaft",new Vector3(0f,4.25f,0f),new Vector3(1.55f,3.25f,1.55f),stone,Quaternion.identity);
        Part(root,PrimitiveType.Cylinder,"Tower_Upper_Ring",new Vector3(0f,7.15f,0f),new Vector3(2.35f,0.38f,2.35f),trim,Quaternion.identity);
        Part(root,PrimitiveType.Sphere,"Tower_Energy_Core",new Vector3(0f,8.35f,0f),new Vector3(1.20f,1.55f,1.20f),energy,Quaternion.identity);

        for (int i=0;i<4;i++)
        {
            float a=i*Mathf.PI*0.5f;
            Vector3 p=new Vector3(Mathf.Cos(a)*2.15f,6.45f,Mathf.Sin(a)*2.15f);
            GameObject buttress=Part(root,PrimitiveType.Cube,"Tower_Buttress_"+i,p,new Vector3(0.48f,3.25f,0.72f),stone,Quaternion.Euler(-12f,-a*Mathf.Rad2Deg,0f));
            Renderer r=buttress.GetComponent<Renderer>(); if(r!=null) r.shadowCastingMode=ShadowCastingMode.On;
        }

        for (int i=0;i<6;i++)
        {
            float a=i*Mathf.PI*2f/6f;
            Vector3 p=new Vector3(Mathf.Cos(a)*2.0f,8.25f,Mathf.Sin(a)*2.0f);
            Part(root,PrimitiveType.Cube,"Tower_Crown_"+i,p,new Vector3(0.34f,1.20f,0.58f),trim,Quaternion.Euler(-18f,-a*Mathf.Rad2Deg,20f));
        }
    }

    private static GameObject Part(Transform parent,PrimitiveType type,string name,Vector3 position,Vector3 scale,Material material,Quaternion rotation)
    {
        GameObject go=GameObject.CreatePrimitive(type);
        go.name=name;
        go.transform.SetParent(parent,false);
        go.transform.localPosition=position;
        go.transform.localScale=scale;
        go.transform.localRotation=rotation;
        Renderer renderer=go.GetComponent<Renderer>();
        if(renderer!=null) renderer.sharedMaterial=material;
        Collider collider=go.GetComponent<Collider>();
        if(collider!=null) Destroy(collider);
        return go;
    }

    private static Material MaterialFor(string key,Color color,float smoothness,float metallic,bool emission)
    {
        if(materials.TryGetValue(key,out Material cached)&&cached!=null) return cached;
        Shader shader=Shader.Find("Universal Render Pipeline/Lit");
        if(shader==null) shader=Shader.Find("Standard");
        Material mat=new Material(shader){name=key,color=color,enableInstancing=true};
        if(mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor",color);
        if(mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness",smoothness);
        if(mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic",metallic);
        if(emission && mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor",color*4.0f);
        }
        materials[key]=mat;
        return mat;
    }
}
