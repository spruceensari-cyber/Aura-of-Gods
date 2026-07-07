using System.Collections.Generic;
using UnityEngine;

public class AOGTowerFullCleanup : MonoBehaviour
{
    public float towerHp = 800f;
    public float attackRange = 22f;
    public float attackDamage = 30f;
    public float attackRate = 1.2f;

    [ContextMenu("1 Clean Old Decorative Towers")]
    public void CleanOldDecorativeTowers()
    {
        GameObject[] all = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        HashSet<GameObject> deleteRoots = new HashSet<GameObject>();
        int removedComponents = 0;

        foreach (GameObject obj in all)
        {
            if (obj == null) continue;

            string n = obj.name.ToLower();

            if (IsOldDecorativeTowerName(n))
            {
                GameObject root = FindOldTowerRoot(obj);
                if (root != null)
                    deleteRoots.Add(root);
            }

            if (!IsValidGameplayTowerName(n))
            {
                TowerAttack attack = obj.GetComponent<TowerAttack>();
                if (attack != null)
                {
                    DestroyImmediate(attack);
                    removedComponents++;
                }

                TowerHealth health = obj.GetComponent<TowerHealth>();
                if (health != null)
                {
                    DestroyImmediate(health);
                    removedComponents++;
                }
            }
        }

        int deleted = 0;
        foreach (GameObject root in deleteRoots)
        {
            if (root == null) continue;
            DestroyImmediate(root);
            deleted++;
        }

        Debug.Log("Old decorative towers cleaned. Deleted roots: " + deleted + " | Removed wrong combat components: " + removedComponents);
    }

    [ContextMenu("2 Setup Gameplay Towers")]
    public void SetupGameplayTowers()
    {
        GameObject[] all = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        int blue = 0;
        int red = 0;

        foreach (GameObject obj in all)
        {
            if (obj == null) continue;

            string n = obj.name.ToLower();
            if (!IsValidGameplayTowerName(n))
                continue;

            bool isBlue = n.StartsWith("blue_");
            bool isRed = n.StartsWith("red_");

            TowerHealth health = obj.GetComponent<TowerHealth>();
            if (health == null)
                health = obj.AddComponent<TowerHealth>();

            health.maxHp = towerHp;
            health.hp = towerHp;
            health.towerTeam = isBlue ? MinionTeam.Blue : MinionTeam.Red;
            health.destroyOnDeath = true;

            TowerAttack attack = obj.GetComponent<TowerAttack>();
            if (attack == null)
                attack = obj.AddComponent<TowerAttack>();

            attack.attackRange = attackRange;
            attack.attackDamage = attackDamage;
            attack.attackRate = attackRate;

            foreach (BoxCollider oldBox in obj.GetComponents<BoxCollider>())
                DestroyImmediate(oldBox);

            BoxCollider box = obj.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 2f, 0f);
            box.size = new Vector3(3f, 4f, 3f);
            box.isTrigger = false;

            if (isBlue) blue++;
            if (isRed) red++;
        }

        Debug.Log("Gameplay towers setup complete. Blue: " + blue + " | Red: " + red);
    }

    [ContextMenu("3 Clean And Setup Towers")]
    public void CleanAndSetupTowers()
    {
        CleanOldDecorativeTowers();
        SetupGameplayTowers();
    }

    bool IsValidGameplayTowerName(string n)
    {
        bool team = n.StartsWith("blue_") || n.StartsWith("red_");
        bool lane = n.Contains("_top_tower_") || n.Contains("_mid_tower_") || n.Contains("_bot_tower_");
        return team && lane;
    }

    bool IsOldDecorativeTowerName(string n)
    {
        bool team = n.StartsWith("blue_") || n.StartsWith("red_");
        if (!team) return false;

        bool lane = n.Contains("_top_") || n.Contains("_mid_") || n.Contains("_bot_");
        if (!lane) return false;

        if (IsValidGameplayTowerName(n)) return false;

        return n.Contains("outer") ||
               n.Contains("inner") ||
               n.Contains("guardian") ||
               n.Contains("celestial_tower_visual") ||
               n.Contains("attack_core") ||
               n.Contains("energy_head") ||
               n.Contains("base_platform") ||
               n.Contains("outer_platform") ||
               n.Contains("inner_ring") ||
               n.Contains("small_flame");
    }

    GameObject FindOldTowerRoot(GameObject obj)
    {
        Transform current = obj.transform;
        Transform best = current;

        while (current.parent != null)
        {
            string pn = current.parent.name.ToLower();
            if (IsOldDecorativeTowerName(pn))
            {
                best = current.parent;
                current = current.parent;
            }
            else
            {
                break;
            }
        }

        return best.gameObject;
    }
}
