using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class AOGReplaceBlueTotems : MonoBehaviour
{
    [Header("New Visual")]
    public GameObject blueTowerPrefab;

    [Header("Placement Settings")]
    public float towerScale = 7f;
    public float yOffset = 0f;
    public float rotationY = 45f;

    [ContextMenu("Replace Old Blue Totems With New Towers")]
    public void ReplaceOldBlueTotemsWithNewTowers()
    {
        if (blueTowerPrefab == null)
        {
            Debug.LogError("Blue Tower Prefab is missing. Drag PF_Blue_Celestial_Tower here.");
            return;
        }

        ClearGeneratedBlueTowers();

        GameObject root = new GameObject("AOG_Generated_Blue_Celestial_Towers");
        root.transform.SetParent(transform);

        MeshRenderer[] renderers = FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);

        int createdCount = 0;

        foreach (MeshRenderer renderer in renderers)
        {
            if (renderer == null)
                continue;

            GameObject obj = renderer.gameObject;
            string objName = obj.name.ToLower();

            bool isBlueTowerLike =
                objName.Contains("blue") &&
                (
                    objName.Contains("tower") ||
                    objName.Contains("outer") ||
                    objName.Contains("inner") ||
                    objName.Contains("mid") ||
                    objName.Contains("top") ||
                    objName.Contains("bot")
                );

            bool isNotGameplaySmallObject =
                !objName.Contains("minion") &&
                !objName.Contains("spawn") &&
                !objName.Contains("target") &&
                !objName.Contains("projectile") &&
                !objName.Contains("base") &&
                !objName.Contains("nexus");

            if (isBlueTowerLike && isNotGameplaySmallObject)
            {
                Vector3 spawnPos = obj.transform.position;
                spawnPos.y += yOffset;

                GameObject newTower = Instantiate(
                    blueTowerPrefab,
                    spawnPos,
                    Quaternion.Euler(0f, rotationY, 0f),
                    root.transform
                );

                newTower.name = "Blue_Celestial_Tower_Visual_" + createdCount;
                newTower.transform.localScale = Vector3.one * towerScale;

                renderer.enabled = false;

                createdCount++;
            }
        }

        Debug.Log("Blue celestial towers created: " + createdCount);
    }

    [ContextMenu("Clear Generated Blue Towers")]
    public void ClearGeneratedBlueTowers()
    {
        Transform oldRoot = transform.Find("AOG_Generated_Blue_Celestial_Towers");

        if (oldRoot != null)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(oldRoot.gameObject);
            else
                Destroy(oldRoot.gameObject);
#else
            Destroy(oldRoot.gameObject);
#endif
        }
    }
}