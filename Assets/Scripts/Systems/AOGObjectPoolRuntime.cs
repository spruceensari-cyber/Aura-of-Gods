using System.Collections.Generic;
using UnityEngine;

public class AOGPooledObject : MonoBehaviour
{
    internal string PoolKey;
    internal AOGObjectPoolRuntime Owner;

    public void Release()
    {
        Owner?.Release(gameObject);
    }
}

/// <summary>
/// Lightweight keyed object pool for projectiles, temporary VFX and UI markers.
/// </summary>
public class AOGObjectPoolRuntime : MonoBehaviour
{
    public static AOGObjectPoolRuntime Instance { get; private set; }

    private readonly Dictionary<string, Queue<GameObject>> available = new();
    private readonly Dictionary<GameObject, string> active = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindObjectOfType<AOGObjectPoolRuntime>() != null)
            return;

        GameObject obj = new GameObject("AOG_Object_Pool_Runtime");
        obj.AddComponent<AOGObjectPoolRuntime>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public GameObject Get(string key, System.Func<GameObject> factory, Vector3 position, Quaternion rotation)
    {
        if (string.IsNullOrWhiteSpace(key) || factory == null)
            return null;

        if (!available.TryGetValue(key, out Queue<GameObject> queue))
        {
            queue = new Queue<GameObject>();
            available[key] = queue;
        }

        GameObject obj = null;
        while (queue.Count > 0 && obj == null)
            obj = queue.Dequeue();

        if (obj == null)
        {
            obj = factory();
            AOGPooledObject pooled = obj.GetComponent<AOGPooledObject>();
            if (pooled == null)
                pooled = obj.AddComponent<AOGPooledObject>();
            pooled.PoolKey = key;
            pooled.Owner = this;
        }

        obj.transform.SetPositionAndRotation(position, rotation);
        obj.SetActive(true);
        active[obj] = key;
        return obj;
    }

    public void Release(GameObject obj)
    {
        if (obj == null)
            return;

        string key = null;
        if (!active.TryGetValue(obj, out key))
        {
            AOGPooledObject pooled = obj.GetComponent<AOGPooledObject>();
            key = pooled != null ? pooled.PoolKey : null;
        }

        if (string.IsNullOrWhiteSpace(key))
        {
            Destroy(obj);
            return;
        }

        if (!available.TryGetValue(key, out Queue<GameObject> queue))
        {
            queue = new Queue<GameObject>();
            available[key] = queue;
        }

        active.Remove(obj);
        obj.SetActive(false);
        obj.transform.SetParent(transform, false);
        queue.Enqueue(obj);
    }

    public void Prewarm(string key, int count, System.Func<GameObject> factory)
    {
        for (int i = 0; i < Mathf.Max(0, count); i++)
        {
            GameObject obj = Get(key, factory, Vector3.zero, Quaternion.identity);
            Release(obj);
        }
    }
}
