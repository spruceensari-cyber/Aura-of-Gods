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
        hp -= amount;
        hp = Mathf.Clamp(hp, 0f, maxHp);

        Debug.Log(name + " hasar aldı: " + hp + " / " + maxHp);

        if (hp <= 0f)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log(name + " öldü.");
        gameObject.SetActive(false);
    }
}