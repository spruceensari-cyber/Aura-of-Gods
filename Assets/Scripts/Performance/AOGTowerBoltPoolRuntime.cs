using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Shared tower projectile pool. Reuses projectile spheres, trails and materials across all towers.
/// </summary>
public class AOGTowerBoltPoolRuntime : MonoBehaviour
{
    private static AOGTowerBoltPoolRuntime instance;
    private readonly Queue<GameObject> pool = new Queue<GameObject>();
    private readonly Dictionary<string,Material> materials = new Dictionary<string,Material>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install() { Ensure(); }

    private static AOGTowerBoltPoolRuntime Ensure()
    {
        if (instance != null) return instance;
        instance = FindFirstObjectByType<AOGTowerBoltPoolRuntime>();
        if (instance != null) return instance;
        GameObject host = new GameObject("AOG_Tower_Bolt_Pool");
        DontDestroyOnLoad(host);
        instance = host.AddComponent<AOGTowerBoltPoolRuntime>();
        return instance;
    }

    public static GameObject Spawn(Vector3 position,Transform target,Minion minion,AOGCharacterStats hero,float damage,float speed,Color color,float size)
    {
        return Ensure().SpawnInternal(position,target,minion,hero,damage,speed,color,size);
    }

    private GameObject SpawnInternal(Vector3 position,Transform target,Minion minion,AOGCharacterStats hero,float damage,float speed,Color color,float size)
    {
        GameObject projectile = pool.Count > 0 ? pool.Dequeue() : CreateProjectile();
        projectile.name = "Pooled_Tower_Energy_Bolt";
        projectile.transform.SetParent(transform,false);
        projectile.transform.position = position;
        projectile.transform.rotation = Quaternion.identity;
        projectile.transform.localScale = Vector3.one*size;

        Material material = GetMaterial(color);
        Renderer renderer = projectile.GetComponent<Renderer>();
        if (renderer != null) renderer.sharedMaterial = material;

        TrailRenderer trail = projectile.GetComponent<TrailRenderer>();
        if (trail != null)
        {
            trail.Clear();
            trail.time = 0.34f;
            trail.startWidth = size*1.35f;
            trail.endWidth = 0f;
            trail.sharedMaterial = material;
            trail.startColor = color;
            trail.endColor = new Color(color.r,color.g,color.b,0f);
            trail.enabled = true;
        }

        TowerBolt bolt = projectile.GetComponent<TowerBolt>();
        bolt.Prepare(this,target,minion,hero,damage,speed,color,3.2f);
        projectile.SetActive(true);
        return projectile;
    }

    private GameObject CreateProjectile()
    {
        GameObject projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        projectile.transform.SetParent(transform,false);
        Collider collider = projectile.GetComponent<Collider>();
        if (collider != null) Destroy(collider);

        Renderer renderer = projectile.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        TrailRenderer trail = projectile.AddComponent<TrailRenderer>();
        trail.minVertexDistance = 0.08f;
        trail.numCornerVertices = 2;
        trail.numCapVertices = 2;
        projectile.AddComponent<TowerBolt>();
        projectile.SetActive(false);
        return projectile;
    }

    internal void Return(GameObject projectile)
    {
        if (projectile == null) return;
        TrailRenderer trail = projectile.GetComponent<TrailRenderer>();
        if (trail != null)
        {
            trail.Clear();
            trail.enabled = false;
        }
        projectile.SetActive(false);
        projectile.transform.SetParent(transform,false);
        pool.Enqueue(projectile);
    }

    private Material GetMaterial(Color color)
    {
        string key = Mathf.RoundToInt(color.r*31f)+"_"+Mathf.RoundToInt(color.g*31f)+"_"+Mathf.RoundToInt(color.b*31f);
        if (materials.TryGetValue(key,out Material cached) && cached != null) return cached;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        Material material = new Material(shader) { name = "TowerBolt_"+key, color = color, enableInstancing = true };
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor",color);
        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor",color*5f);
        }
        materials[key] = material;
        return material;
    }
}
