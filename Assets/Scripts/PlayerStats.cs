using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public float maxHealth = 100f;
    public float currentHealth;

    public int level = 1;
    public int currentXP = 0;
    public int xpToNextLevel = 100;

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void AddXP(int amount)
    {
        currentXP += amount;

        if (currentXP >= xpToNextLevel)
        {
            currentXP -= xpToNextLevel;
            level++;
            xpToNextLevel += 50;

            Debug.Log("Level Up! Yeni Level: " + level);
        }
    }
}