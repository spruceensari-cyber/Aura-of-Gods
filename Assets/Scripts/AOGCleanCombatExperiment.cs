using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class AOGCleanCombatExperiment : MonoBehaviour
{
    [ContextMenu("Clean Combat Experiment Objects")]
    public void CleanCombatExperimentObjects()
    {
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);

        int deleted = 0;
        int cleaned = 0;

        foreach (GameObject obj in allObjects)
        {
            if (obj == null)
                continue;

            string n = obj.name.ToLower();

            bool isHealthBarObject =
                n.StartsWith("hb_") ||
                n.Contains("health_background") ||
                n.Contains("health_fill");

            if (isHealthBarObject)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    DestroyImmediate(obj);
                else
                    Destroy(obj);
#else
                Destroy(obj);
#endif
                deleted++;
                continue;
            }

            RemoveIfExists<AOGDamageable>(obj, ref cleaned);
            RemoveIfExists<AOGWorldHealthBar>(obj, ref cleaned);
            RemoveIfExists<AOGTowerBeamAttack>(obj, ref cleaned);
            RemoveIfExists<AOGMinionCombatAI>(obj, ref cleaned);
            RemoveIfExists<AOGCombatUnit>(obj, ref cleaned);
        }

        Debug.Log("Cleaned combat experiment. Deleted health bars: " + deleted + " | Removed components: " + cleaned);
    }

    private void RemoveIfExists<T>(GameObject obj, ref int count) where T : Component
    {
        T component = obj.GetComponent<T>();

        if (component != null)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(component);
            else
                Destroy(component);
#else
            Destroy(component);
#endif
            count++;
        }
    }
}