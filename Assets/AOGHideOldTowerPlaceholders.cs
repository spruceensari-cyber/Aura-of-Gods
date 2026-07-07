using UnityEngine;

public class AOGHideOldTowerPlaceholders : MonoBehaviour
{
    [ContextMenu("Hide Old Tower Cylinder Visuals")]
    public void HideOldTowerCylinderVisuals()
    {
        MeshFilter[] meshFilters = FindObjectsByType<MeshFilter>(FindObjectsSortMode.None);
        int hiddenCount = 0;

        foreach (MeshFilter mf in meshFilters)
        {
            if (mf == null || mf.sharedMesh == null)
                continue;

            string meshName = mf.sharedMesh.name.ToLower();
            string objectName = mf.gameObject.name.ToLower();

            bool isCylinder = meshName.Contains("cylinder");

            bool isOldTower =
                objectName.Contains("tower") ||
                objectName.Contains("outer") ||
                objectName.Contains("inner") ||
                objectName.Contains("red") ||
                objectName.Contains("blue") ||
                objectName.Contains("nexus");

            bool hasTowerLogic =
                mf.GetComponent("TowerHealth") != null ||
                mf.GetComponent("TowerAttack") != null;

            if (isCylinder && (isOldTower || hasTowerLogic))
            {
                MeshRenderer renderer = mf.GetComponent<MeshRenderer>();

                if (renderer != null)
                {
                    renderer.enabled = false;
                    hiddenCount++;
                }
            }
        }

        Debug.Log("Old tower cylinder visuals hidden: " + hiddenCount);
    }

    [ContextMenu("Show Old Tower Cylinder Visuals")]
    public void ShowOldTowerCylinderVisuals()
    {
        MeshFilter[] meshFilters = FindObjectsByType<MeshFilter>(FindObjectsSortMode.None);
        int shownCount = 0;

        foreach (MeshFilter mf in meshFilters)
        {
            if (mf == null || mf.sharedMesh == null)
                continue;

            string meshName = mf.sharedMesh.name.ToLower();
            string objectName = mf.gameObject.name.ToLower();

            bool isCylinder = meshName.Contains("cylinder");

            bool isOldTower =
                objectName.Contains("tower") ||
                objectName.Contains("outer") ||
                objectName.Contains("inner") ||
                objectName.Contains("red") ||
                objectName.Contains("blue") ||
                objectName.Contains("nexus");

            bool hasTowerLogic =
                mf.GetComponent("TowerHealth") != null ||
                mf.GetComponent("TowerAttack") != null;

            if (isCylinder && (isOldTower || hasTowerLogic))
            {
                MeshRenderer renderer = mf.GetComponent<MeshRenderer>();

                if (renderer != null)
                {
                    renderer.enabled = true;
                    shownCount++;
                }
            }
        }

        Debug.Log("Old tower cylinder visuals shown: " + shownCount);
    }
}