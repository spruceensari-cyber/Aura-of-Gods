using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    public float currentHealth = 100f;

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float damage)
    {
        if (currentHealth <= 0f || damage <= 0f)
            return;

        float oldHealth = currentHealth;
        currentHealth -= damage;

        if (currentHealth < 0f)
        {
            currentHealth = 0f;
        }

        float appliedDamage = oldHealth - currentHealth;
        AOGFloatingCombatText.SpawnDamage(transform.position, appliedDamage, new Color(1f, 0.3f, 0.22f, 1f));

        Debug.Log("Player HP: " + currentHealth);

        if (currentHealth <= 0f)
        {
            Debug.Log("Player died");
        }
    }
}
