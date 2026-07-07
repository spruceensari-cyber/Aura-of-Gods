using UnityEngine;

public class TowerBolt : MonoBehaviour
{
    public Transform target;
    public float damage = 30f;
    public float speed = 18f;
    public float lifeTime = 3f;
    public float impactDistance = 0.25f;
    public float targetHeight = 1.2f;

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 targetPos = target.position + Vector3.up * targetHeight;

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPos,
            speed * Time.deltaTime
        );

        if (Vector3.Distance(transform.position, targetPos) <= impactDistance)
        {
            HitTarget();
            Destroy(gameObject);
        }
    }

    void HitTarget()
    {
        if (target == null)
            return;

        Minion minion = target.GetComponentInParent<Minion>();

        if (minion != null)
        {
            minion.TakeDamage(damage, gameObject);
            return;
        }

        AOGCharacterStats hero = target.GetComponentInParent<AOGCharacterStats>();

        if (hero != null)
        {
            hero.TakeDamage(damage);
            return;
        }

        PlayerHealth legacyPlayerHealth = target.GetComponentInParent<PlayerHealth>();

        if (legacyPlayerHealth != null)
            legacyPlayerHealth.TakeDamage(damage);
    }
}
