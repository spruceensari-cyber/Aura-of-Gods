using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class AOGFinalTowerLayout : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject blueTowerPrefab;
    public GameObject redTowerPrefab;

    [Header("Generated Parents")]
    public string anchorRootName = "AOG_Final_Tower_Anchors";
    public string generatedRootName = "AOG_Final_Tower_Visuals";

    [Header("Rebuild Settings")]
    public float blueScale = 7f;
    public float redScale = 5f;
    public float blueYOffset = 0f;
    public float redYOffset = 0f;

    [ContextMenu("Capture Current Tower Visuals As Anchors")]
    public void CaptureCurrentTowerVisualsAsAnchors()
    {
        ClearAnchors();

        GameObject anchorRoot = new GameObject(anchorRootName);
        anchorRoot.transform.SetParent(transform);

        Transform[] allTransforms = FindObjectsByType<Transform>(FindObjectsSortMode.None);

        int blueCount = 0;
        int redCount = 0;

        foreach (Transform t in allTransforms)
        {
            if (t == null)
                continue;

            string name = t.gameObject.name.ToLower();

            bool isGeneratedRoot =
                name.Contains(anchorRootName.ToLower()) ||
                name.Contains(generatedRootName.ToLower());

            if (isGeneratedRoot)
                continue;

            bool isBlueTower =
                name.Contains("blue_celestial_tower") ||
                name.Contains("pf_blue_celestial_tower");

            bool isRedTower =
                name.Contains("red_fallen") ||
                name.Contains("pf_red_fallen") ||
                name.Contains("red_") && name.Contains("tower_visual");

            if (isBlueTower)
            {
                CreateAnchor(
                    "Blue_Tower_Anchor_" + blueCount,
                    t.position,
                    t.rotation,
                    anchorRoot.transform
                );

                blueCount++;
            }

            if (isRedTower)
            {
                CreateAnchor(
                    "Red_Tower_Anchor_" + redCount,
                    t.position,
                    t.rotation,
                    anchorRoot.transform
                );

                redCount++;
            }
        }

        Debug.Log("Captured blue tower anchors: " + blueCount + " | red tower anchors: " + redCount);
    }

    [ContextMenu("Rebuild Towers From Anchors")]
    public void RebuildTowersFromAnchors()
    {
        ClearGeneratedTowerVisuals();

        Transform anchorRoot = transform.Find(anchorRootName);

        if (anchorRoot == null)
        {
            Debug.LogError("No anchor root found. First run: Capture Current Tower Visuals As Anchors");
            return;
        }

        GameObject generatedRoot = new GameObject(generatedRootName);
        generatedRoot.transform.SetParent(transform);

        int createdCount = 0;

        foreach (Transform anchor in anchorRoot)
        {
            string anchorName = anchor.gameObject.name.ToLower();

            bool isBlue = anchorName.Contains("blue");
            bool isRed = anchorName.Contains("red");

            GameObject prefabToUse = null;
            float scaleToUse = 1f;
            float yOffsetToUse = 0f;

            if (isBlue)
            {
                prefabToUse = blueTowerPrefab;
                scaleToUse = blueScale;
                yOffsetToUse = blueYOffset;
            }
            else if (isRed)
            {
                prefabToUse = redTowerPrefab;
                scaleToUse = redScale;
                yOffsetToUse = redYOffset;
            }

            if (prefabToUse == null)
                continue;

            Vector3 pos = anchor.position;
            pos.y += yOffsetToUse;

            GameObject tower = Instantiate(
                prefabToUse,
                pos,
                anchor.rotation,
                generatedRoot.transform
            );

            tower.name = anchor.gameObject.name.Replace("Anchor", "Visual");
            tower.transform.localScale = Vector3.one * scaleToUse;

            createdCount++;
        }

        Debug.Log("Rebuilt tower visuals from anchors: " + createdCount);
    }

    [ContextMenu("Clear Generated Tower Visuals")]
    public void ClearGeneratedTowerVisuals()
    {
        Transform old = transform.Find(generatedRootName);

        if (old != null)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(old.gameObject);
            else
                Destroy(old.gameObject);
#else
            Destroy(old.gameObject);
#endif
        }
    }

    [ContextMenu("Clear Anchors")]
    public void ClearAnchors()
    {
        Transform old = transform.Find(anchorRootName);

        if (old != null)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(old.gameObject);
            else
                Destroy(old.gameObject);
#else
            Destroy(old.gameObject);
#endif
        }
    }

    private void CreateAnchor(string name, Vector3 position, Quaternion rotation, Transform parent)
    {
        GameObject anchor = new GameObject(name);
        anchor.transform.SetParent(parent);
        anchor.transform.position = position;
        anchor.transform.rotation = rotation;
        anchor.transform.localScale = Vector3.one;
    }
}