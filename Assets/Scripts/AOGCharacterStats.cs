using UnityEngine;

public class AOGCharacterStats : MonoBehaviour
{
    public MinionTeam team = MinionTeam.Blue;

    [Header("Health")]
    public float maxHp = 600f;
    public float hp = 600f;

    [Header("Combat")]
    public float attackDamage = 45f;
    public float attackRange = 4f;
    public float attackCooldown = 1.1f;

    [Header("Movement")]
    public float moveSpeed = 6f;

    public bool IsDead => hp <= 0f;

    void Start()
    {
        if (hp <= 0f)
            hp = maxHp;
    }

    public void TakeDamage(float amount)
    {
        if (hp <= 0f || amount <= 0f)
            return;

        float oldHp = hp;
        hp -= amount;
        hp = Mathf.Clamp(hp, 0f, maxHp);

        float appliedDamage = oldHp - hp;
        AOGFloatingCombatText.SpawnDamage(transform.position, appliedDamage, new Color(1f, 0.32f, 0.22f, 1f));

        Debug.Log(name + " took damage: " + hp + " / " + maxHp);

        if (hp <= 0f)
        {
            Die();
        }
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

    void Die()
    {
        Debug.Log(name + " died.");
        gameObject.SetActive(false);
    }
}
