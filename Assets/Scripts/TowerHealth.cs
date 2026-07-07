using UnityEngine;

public class TowerHealth : MonoBehaviour
{
    public MinionTeam towerTeam;

    [Header("Health")]
    public float maxHp = 800f;
    public float hp = 800f;

    [Header("Death")]
    public bool destroyOnDeath = true;

    void Start()
    {
        if (hp <= 0f)
            hp = maxHp;
    }

    public float maxHealth
    {
        get { return maxHp; }
        set { maxHp = value; }
    }

    public float currentHealth
    {
        get { return hp; }
        set { hp = value; }
    }

    public void TakeDamage(float amount)
    {
        if (hp <= 0f || amount <= 0f)
            return;

        float oldHp = hp;
        hp -= amount;
        hp = Mathf.Clamp(hp, 0f, maxHp);

        float appliedDamage = oldHp - hp;
        Vector3 textPosition = transform.position + Vector3.up * 4f;
        AOGFloatingCombatText.SpawnDamage(textPosition, appliedDamage, new Color(1f, 0.46f, 0.2f, 1f));

        Debug.Log(name + " tower took damage. HP: " + hp + " / " + maxHp);

        if (hp <= 0f)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log(name + " tower destroyed.");

        if (destroyOnDeath)
        {
            Destroy(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
