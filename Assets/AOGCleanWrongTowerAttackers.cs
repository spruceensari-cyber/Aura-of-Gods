using UnityEngine;

public class AOGCleanWrongTowerAttackers : MonoBehaviour
{
    [ContextMenu("Clean Wrong Tower Attackers")]
    public void CleanWrongTowerAttackers()
    {
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);

        int removedTowerAttack = 0;
        int removedTowerHealth = 0;

        foreach (GameObject obj in allObjects)
        {
            string n = obj.name.ToLower();

            bool isValidLaneTower =
                (n.StartsWith("blue_") || n.StartsWith("red_")) &&
                (
                    n.Contains("_top_tower_") ||
                    n.Contains("_mid_tower_") ||
                    n.Contains("_bot_tower_")
                );

            if (isValidLaneTower)
                continue;

            TowerAttack towerAttack = obj.GetComponent<TowerAttack>();
            if (towerAttack != null)
            {
                DestroyImmediate(towerAttack);
                removedTowerAttack++;
            }

            TowerHealth towerHealth = obj.GetComponent<TowerHealth>();
            if (towerHealth != null)
            {
                DestroyImmediate(towerHealth);
                removedTowerHealth++;
            }
        }

        Debug.Log("Yanlış kule saldırıcıları temizlendi. TowerAttack: " 
            + removedTowerAttack + " | TowerHealth: " + removedTowerHealth);
    }
}