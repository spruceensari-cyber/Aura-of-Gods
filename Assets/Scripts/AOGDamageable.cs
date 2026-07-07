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
        if (hp <= 0f || amount <= 0f)
            return;

        float oldHp = hp;
        hp -= amount;
        hp = Mathf.Clamp(hp, 0f, maxHp);

        float appliedDamage = oldHp - hp;
        AOGFloatingCombatText.SpawnDamage(transform.position, appliedDamage, new Color(1f, 0.62f, 0.22f, 1f));

        if (hp <= 0f)
            Die();
    }

    public void Heal(float amount)
    {
        if (amount <= 0f || hp <= 0f)
            return;

        float oldHp = hp;
        hp += amount;
        hp = Mathf.Clamp(hp, 0f, maxHp);

        float appliedHeal = hp - oldHp;
        AOGFloatingCombatText.SpawnHeal(transform.position, appliedHeal);
    }

    public void Die()
    {
        hp = 0f;
        Destroy(gameObject);
    }
}
