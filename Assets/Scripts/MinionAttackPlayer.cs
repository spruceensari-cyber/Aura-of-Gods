using UnityEngine;

public class MinionAttackPlayer : MonoBehaviour
{
    public float attackRange = 2.2f;
    public float attackDamage = 8f;
    public float attackCooldown = 1.2f;

    private float nextAttackTime = 0f;
    private Transform player;
    private PlayerHealth playerHealth;

    void Start()
    {
        GameObject playerObj = GameObject.Find("Kaelith_Player");

        if (playerObj != null)
        {
            player = playerObj.transform;
            playerHealth = playerObj.GetComponent<PlayerHealth>();
        }
    }

    void Update()
    {
        if (player == null || playerHealth == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= attackRange && Time.time >= nextAttackTime)
        {
            playerHealth.TakeDamage(attackDamage);
            Debug.Log("Minyon player'a vurdu");

            nextAttackTime = Time.time + attackCooldown;
        }
    }
}