using UnityEngine;

public class AOGDamageable : MonoBehaviour
{
    public MinionTeam team = MinionTeam.Blue;

    [Header("Compatibility Health")]
    public float maxHp = 100f;
    public float hp = 100f;

    public float currentHp
    {
        get { return hp; }
        set { hp = value; }
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

    public bool IsDead
    {
        get { return hp <= 0f; }
    }

    void Start()
    {
        if (hp <= 0f)
            hp = maxHp;
    }

    public void TakeDamage(float amount)
    {
        TakeDamage(amount, null);
    }

    public void TakeDamage(float amount, GameObject attacker)
    {
        if (hp <= 0f)
            return;

        hp -= amount;
        hp = Mathf.Clamp(hp, 0f, maxHp);

        if (hp <= 0f)
            Die();
    }

    public void Heal(float amount)
    {
        hp += amount;
        hp = Mathf.Clamp(hp, 0f, maxHp);
    }

    public void Die()
    {
        hp = 0f;
        Destroy(gameObject);
    }
}