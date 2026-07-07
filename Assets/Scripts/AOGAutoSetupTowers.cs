using UnityEngine;

public class AOGAutoSetupTowers : MonoBehaviour
{
    [Header("Tower Stats")]
    public float towerHp = 800f;
    public float attackRange = 12f;
    public float attackDamage = 30f;
    public float attackRate = 1.2f;

    [Header("Tower Visual Size")]
    public float towerScale = 3.5f;

    [Header("Tower Health Bar")]
    public Vector3 healthBarOffset = new Vector3(0f, 2.8f, 0f);
    public float healthBarWidth = 1.2f;
    public float healthBarHeight = 0.08f;

    [Header("Tower Collider")]
    public Vector3 colliderCenter = new Vector3(0f, 1.8f, 0f);
    public Vector3 colliderSize = new Vector3(2f, 3.5f, 2f);

    [ContextMenu("Setup All Towers")]
    public void SetupAllTowers()
    {
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);

        int blueCount = 0;
        int redCount = 0;

        foreach (GameObject obj in allObjects)
        {
            string n = obj.name.ToLower();

            bool isBlue = n.StartsWith("blue_");
            bool isRed = n.StartsWith("red_");

            bool isLaneTower =
                n.Contains("_top_tower_") ||
                n.Contains("_mid_tower_") ||
                n.Contains("_bot_tower_");

            if (!isLaneTower || (!isBlue && !isRed))
                continue;

            // TOWER SCALE
            obj.transform.localScale = new Vector3(towerScale, towerScale, towerScale);

            // HEALTH
            TowerHealth health = obj.GetComponent<TowerHealth>();
            if (health == null)
                health = obj.AddComponent<TowerHealth>();

            health.maxHp = towerHp;
            health.hp = towerHp;
            health.towerTeam = isBlue ? MinionTeam.Blue : MinionTeam.Red;
            health.destroyOnDeath = true;

            // ATTACK
            TowerAttack attack = obj.GetComponent<TowerAttack>();
            if (attack == null)
                attack = obj.AddComponent<TowerAttack>();

            attack.attackRange = attackRange;
            attack.attackDamage = attackDamage;
            attack.attackRate = attackRate;

            // WORLD HEALTH BAR
            AOGWorldHealthBar bar = obj.GetComponent<AOGWorldHealthBar>();
            if (bar == null)
                bar = obj.AddComponent<AOGWorldHealthBar>();

            bar.barOffset = healthBarOffset;
            bar.barWidth = healthBarWidth;
            bar.barHeight = healthBarHeight;

            // Remove old BoxColliders
            BoxCollider[] oldBoxes = obj.GetComponents<BoxCollider>();
            foreach (BoxCollider oldBox in oldBoxes)
            {
                DestroyImmediate(oldBox);
            }

            // Add clean smaller collider
            BoxCollider box = obj.AddComponent<BoxCollider>();
            box.center = colliderCenter;
            box.size = colliderSize;
            box.isTrigger = false;

            if (isBlue)
                blueCount++;

            if (isRed)
                redCount++;
        }

        Debug.Log("Kule setup tamamlandı. Blue: " + blueCount + " | Red: " + redCount);
    }
}