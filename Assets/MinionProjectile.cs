using UnityEngine;

public class MinionProjectile : MonoBehaviour
{
    public Transform target;
    public float damage = 10f;
    public float speed = 12f;

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

        if (distance <= 0.25f)
        {
            Minion targetMinion = target.GetComponent<Minion>();

            if (targetMinion != null)
            {
                targetMinion.TakeDamage(damage);
            }

            Destroy(gameObject);
        }
    }
}