using UnityEngine;

public class LyraProjectile : MonoBehaviour
{
    public Minion target;
    public float damage = 80f;
    public float speed = 20f;
    public Color color = Color.magenta;

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

        Vector3 targetPosition = target.transform.position + Vector3.up * 1.2f;

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            speed * Time.deltaTime
        );

        if (Vector3.Distance(transform.position, targetPosition) <= 0.3f)
        {
            if (damage > 0f)
                target.TakeDamage(damage, gameObject);

            Destroy(gameObject);
        }
    }
}