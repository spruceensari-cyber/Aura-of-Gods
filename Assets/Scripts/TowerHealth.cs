using System;
using UnityEngine;

public class TowerHealth : MonoBehaviour
{
    public static event Action<TowerHealth> TowerDestroyed;

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
        if (hp <= 0f)
            return;

        hp -= amount;
        hp = Mathf.Clamp(hp, 0f, maxHp);

        Debug.Log(name + " kule hasar aldı. HP: " + hp + " / " + maxHp);

        if (hp <= 0f)
            Die();
    }

    void Die()
    {
        Debug.Log(name + " kule yok oldu.");
        TowerDestroyed?.Invoke(this);

        if (destroyOnDeath)
            Destroy(gameObject);
        else
            gameObject.SetActive(false);
    }
}
