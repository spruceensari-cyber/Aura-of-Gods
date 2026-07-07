using UnityEngine;

public class TowerBolt : MonoBehaviour
{
    public Minion target;
    public float damage = 30f;
    public float speed = 18f;
    public float lifeTime = 3f;

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

        Vector3 targetPos = target.transform.position + Vector3.up * 1.2f;

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPos,
            speed * Time.deltaTime
        );

        if (Vector3.Distance(transform.position, targetPos) <= 0.25f)
        {
            target.TakeDamage(damage, gameObject);
            Destroy(gameObject);
        }
    }
}