using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class AOGReplaceBlueCylinderTowersOnly : MonoBehaviour
{
    [Header("New Blue Tower Visual")]
    public GameObject blueTowerPrefab;

    [Header("Placement Settings")]
    public float towerScale = 7f;
    public float yOffset = 0f;
    public float rotationY = 45f;

    [ContextMenu("Replace ONLY Old Blue Cylinder Towers")]
    public void ReplaceOnlyOldBlueCylinderTowers()
    {
        if (blueTowerPrefab == null)
        {
            Debug.LogError("Blue Tower Prefab is missing.");
            return;
        }

        ClearGeneratedBlueTowers();

        GameObject root = new GameObject("AOG_Generated_Blue_Celestial_Towers_ONLY");
        root.transform.SetParent(transform);

        MeshFilter[] meshFilters = FindObjectsByType<MeshFilter>(FindObjectsSortMode.None);

        int createdCount = 0;

        foreach (MeshFilter mf in meshFilters)
        {
            if (mf == null || mf.sharedMesh == null)
                continue;

            GameObject obj = mf.gameObject;

            string objName = obj.name.ToLower();
            string meshName = mf.sharedMesh.name.ToLower();

            bool isOldCylinderMesh =
                meshName.Contains("cylinder");

            bool isBlueOldTowerName =
                objName.Contains("blue") &&
                (
                    objName.Contains("mid") ||
                    objName.Contains("outer") ||
                    objName.Contains("inner") ||
                    objName.Contains("tower")
                );

            bool excludeWrongObjects =
                objName.Contains("minion") ||
                objName.Contains("spawn") ||
                objName.Contains("target") ||
                objName.Contains("projectile") ||
                objName.Contains("nexus") ||
                objName.Contains("base") ||
                objName.Contains("generated") ||
                objName.Contains("visual") ||
                objName.Contains("tree") ||
                objName.Contains("rock") ||
                objName.Contains("rune") ||
                objName.Contains("pillar") ||
                objName.Contains("camp") ||
                objName.Contains("boss");

            if (isOldCylinderMesh && isBlueOldTowerName && !excludeWrongObjects)
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

                MeshRenderer oldRenderer = obj.GetComponent<MeshRenderer>();
                if (oldRenderer != null)
                    oldRenderer.enabled = false;

                createdCount++;
            }
        }

        Debug.Log("ONLY old blue cylinder towers replaced: " + createdCount);
    }

    [ContextMenu("Clear Generated Blue Towers ONLY")]
    public void ClearGeneratedBlueTowers()
    {
        Transform oldRoot = transform.Find("AOG_Generated_Blue_Celestial_Towers_ONLY");

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