using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Rebuilds a compact XZ spatial grid for active minions. Minion movement/combat queries then touch
/// only nearby cells instead of scanning every active minion for every unit.
/// </summary>
[DefaultExecutionOrder(-120)]
public class AOGMinionSpatialGridRuntime : MonoBehaviour
{
    private const float CellSize = 6f;
    private static readonly Dictionary<long,List<Minion>> cells = new Dictionary<long,List<Minion>>();
    private static readonly Stack<List<Minion>> listPool = new Stack<List<Minion>>();
    private float nextRebuild;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (FindFirstObjectByType<AOGMinionSpatialGridRuntime>() != null) return;
        GameObject host = new GameObject("AOG_Minion_Spatial_Grid_Runtime");
        DontDestroyOnLoad(host);
        host.AddComponent<AOGMinionSpatialGridRuntime>();
    }

    private void Update()
    {
        if (Time.unscaledTime < nextRebuild) return;
        nextRebuild = Time.unscaledTime + 0.12f;
        Rebuild();
    }

    private static void Rebuild()
    {
        foreach (List<Minion> list in cells.Values)
        {
            list.Clear();
            listPool.Push(list);
        }
        cells.Clear();

        foreach (Minion minion in Minion.Active)
        {
            if (minion == null || minion.hp <= 0f || !minion.gameObject.activeInHierarchy) continue;
            long key = Key(minion.transform.position);
            if (!cells.TryGetValue(key,out List<Minion> list))
            {
                list = listPool.Count > 0 ? listPool.Pop() : new List<Minion>(12);
                cells.Add(key,list);
            }
            list.Add(minion);
        }
    }

    public static void Query(Vector3 position,float radius,List<Minion> results)
    {
        results.Clear();
        int minX = Mathf.FloorToInt((position.x-radius)/CellSize);
        int maxX = Mathf.FloorToInt((position.x+radius)/CellSize);
        int minZ = Mathf.FloorToInt((position.z-radius)/CellSize);
        int maxZ = Mathf.FloorToInt((position.z+radius)/CellSize);
        float radiusSqr = radius*radius;

        for (int x=minX;x<=maxX;x++)
        {
            for (int z=minZ;z<=maxZ;z++)
            {
                if (!cells.TryGetValue(Key(x,z),out List<Minion> list)) continue;
                for (int i=0;i<list.Count;i++)
                {
                    Minion minion = list[i];
                    if (minion == null) continue;
                    Vector3 delta = minion.transform.position-position;
                    delta.y = 0f;
                    if (delta.sqrMagnitude <= radiusSqr) results.Add(minion);
                }
            }
        }
    }

    private static long Key(Vector3 position)
    {
        return Key(Mathf.FloorToInt(position.x/CellSize),Mathf.FloorToInt(position.z/CellSize));
    }

    private static long Key(int x,int z)
    {
        return ((long)x << 32) ^ (uint)z;
    }
}
