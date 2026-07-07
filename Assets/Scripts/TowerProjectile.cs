using UnityEngine;

public class TowerProjectile : MonoBehaviour
{
    public Transform target;
    public float speed = 18f;
    public float damage = 35f;
    public bool targetIsMinion = true;

    void Update()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 targetPosition = target.position + Vector3.up * 1f;

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            speed * Time.deltaTime
        );

        float distance = Vector3.Distance(transform.position, targetPosition);

        if (distance <= 0.3f)
        {
            HitTarget();
        }
    }

    void HitTarget()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        if (targetIsMinion)
        {
            Minion minion = target.GetComponent<Minion>();

            if (minion != null)
            {
                minion.TakeDamage(damage, null);
            }
        }
        else
        {
            PlayerHealth playerHealth = target.GetComponent<PlayerHealth>();

            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
            }
        }

        Destroy(gameObject);
    }
}