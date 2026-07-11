using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Small shared pool for short-lived cube/sphere combat accents. Avoids per-hit primitive allocation,
/// collider creation and material churn. Ring VFX stay under their existing system.
/// </summary>
public class AOGTransientPrimitivePoolRuntime : MonoBehaviour
{
    private static AOGTransientPrimitivePoolRuntime instance;
    private readonly Queue<GameObject> cubePool = new Queue<GameObject>();
    private readonly Queue<GameObject> spherePool = new Queue<GameObject>();
    private readonly Dictionary<string,Material> materials = new Dictionary<string,Material>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        Ensure();
    }

    private static AOGTransientPrimitivePoolRuntime Ensure()
    {
        if (instance != null) return instance;
        instance = FindFirstObjectByType<AOGTransientPrimitivePoolRuntime>();
        if (instance != null) return instance;

        GameObject host = new GameObject("AOG_Transient_Primitive_Pool");
        DontDestroyOnLoad(host);
        instance = host.AddComponent<AOGTransientPrimitivePoolRuntime>();
        return instance;
    }

    public static GameObject SpawnCube(Vector3 position,Quaternion rotation,Vector3 scale,Color color,float emission,float lifetime)
    {
        return Ensure().Spawn(PrimitiveType.Cube,position,rotation,scale,color,emission,lifetime);
    }

    public static GameObject SpawnSphere(Vector3 position,Vector3 scale,Color color,float emission,float lifetime)
    {
        return Ensure().Spawn(PrimitiveType.Sphere,position,Quaternion.identity,scale,color,emission,lifetime);
    }

    private GameObject Spawn(PrimitiveType type,Vector3 position,Quaternion rotation,Vector3 scale,Color color,float emission,float lifetime)
    {
        Queue<GameObject> queue = type == PrimitiveType.Cube ? cubePool : spherePool;
        GameObject go = queue.Count > 0 ? queue.Dequeue() : Create(type);
        go.name = type == PrimitiveType.Cube ? "Pooled_Impact_Slash" : "Pooled_Impact_Orb";
        go.transform.SetParent(transform,false);
        go.transform.position = position;
        go.transform.rotation = rotation;
        go.transform.localScale = scale;

        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer != null) renderer.sharedMaterial = GetMaterial(color,emission);
        go.SetActive(true);
        StartCoroutine(ReturnAfter(go,type,Mathf.Max(0.04f,lifetime)));
        return go;
    }

    private GameObject Create(PrimitiveType type)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.transform.SetParent(transform,false);
        Collider collider = go.GetComponent<Collider>();
        if (collider != null) Destroy(collider);
        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }
        go.SetActive(false);
        return go;
    }

    private IEnumerator ReturnAfter(GameObject go,PrimitiveType type,float lifetime)
    {
        yield return new WaitForSeconds(lifetime);
        if (go == null) yield break;
        go.SetActive(false);
        go.transform.SetParent(transform,false);
        go.transform.localScale = Vector3.one;
        go.transform.localRotation = Quaternion.identity;
        if (type == PrimitiveType.Cube) cubePool.Enqueue(go);
        else spherePool.Enqueue(go);
    }

    private Material GetMaterial(Color color,float emission)
    {
        int r = Mathf.RoundToInt(color.r*31f);
        int g = Mathf.RoundToInt(color.g*31f);
        int b = Mathf.RoundToInt(color.b*31f);
        int e = Mathf.RoundToInt(emission*4f);
        string key = r + "_" + g + "_" + b + "_" + e;
        if (materials.TryGetValue(key,out Material cached) && cached != null) return cached;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        Material mat = new Material(shader) { name = "PooledVFX_" + key, color = color, enableInstancing = true };
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor",color);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness",0.35f);
        if (mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor",color*emission);
        }
        materials[key] = mat;
        return mat;
    }
}
