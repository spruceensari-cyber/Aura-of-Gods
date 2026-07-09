using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Locks authored road and lane decoration during play. Legacy editor/upgrader scripts are disabled,
/// physics is removed from decorative road pieces and their transforms are restored every LateUpdate.
/// </summary>
public class AOGSceneRuntimeLock : MonoBehaviour
{
    private readonly Dictionary<Transform, Pose> locked = new();
    private static readonly HashSet<string> DisabledLegacyTypes = new()
    {
        "AOGRoadDetailUpgrade",
        "AOGTerrainHeightShaper",
        "AOGStructureUpgrade",
        "AOGSceneLookSetup",
        "AOGTerrainPainter",
        "AOGRoadBuilder",
        "AOGMapDecorator"
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Ensure();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Ensure();
        AOGSceneRuntimeLock instance = FindObjectOfType<AOGSceneRuntimeLock>();
        if (instance != null) instance.RebuildLock();
    }

    private static void Ensure()
    {
        if (FindObjectOfType<AOGSceneRuntimeLock>() != null) return;
        new GameObject("AOG_Scene_Runtime_Lock").AddComponent<AOGSceneRuntimeLock>();
    }

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        RebuildLock();
    }

    public void RebuildLock()
    {
        locked.Clear();
        DisableLegacyBuilders();

        Transform[] all = FindObjectsByType<Transform>(FindObjectsSortMode.None);
        foreach (Transform t in all)
        {
            if (t == null || !t.gameObject.scene.IsValid()) continue;
            if (!IsRoadDecor(t)) continue;

            locked[t] = new Pose(t.position, t.rotation);

            Rigidbody rb = t.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.detectCollisions = false;
            }

            Collider col = t.GetComponent<Collider>();
            if (col != null) col.enabled = false;

            t.gameObject.isStatic = true;
        }
    }

    private void DisableLegacyBuilders()
    {
        MonoBehaviour[] behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour == null || behaviour == this) continue;
            if (DisabledLegacyTypes.Contains(behaviour.GetType().Name))
                behaviour.enabled = false;
        }
    }

    private static bool IsRoadDecor(Transform t)
    {
        string n = t.name.ToLowerInvariant();
        if (n.Contains("road_detail") || n.Contains("broken_stone") || n.Contains("dark_crack") || n.Contains("small_rune"))
            return true;

        Transform p = t.parent;
        while (p != null)
        {
            string pn = p.name.ToLowerInvariant();
            if (pn.Contains("road_detail") || pn.Contains("lane_decor") || pn.Contains("road_decor"))
                return true;
            p = p.parent;
        }
        return false;
    }

    private void LateUpdate()
    {
        foreach (KeyValuePair<Transform, Pose> pair in locked)
        {
            Transform t = pair.Key;
            if (t == null) continue;
            Pose pose = pair.Value;
            if ((t.position - pose.position).sqrMagnitude > 0.000001f) t.position = pose.position;
            if (Quaternion.Angle(t.rotation, pose.rotation) > 0.01f) t.rotation = pose.rotation;
        }
    }
}
