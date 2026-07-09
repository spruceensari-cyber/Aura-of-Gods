using UnityEngine;

public class LyraProjectile : MonoBehaviour
{
    public Minion target;
    public GameObject owner;
    public float damage = 80f;
    public float speed = 20f;
    public Color color = new Color(1f, 0.05f, 0.65f);
    public float lifeTime = 3f;

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        if (target == null || !target.gameObject.activeInHierarchy || target.hp <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 targetPosition = target.transform.position + Vector3.up * 1.2f;
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);

        if ((transform.position - targetPosition).sqrMagnitude > 0.09f)
            return;

        if (damage > 0f)
            target.TakeDamage(damage, owner != null ? owner : gameObject);

        Destroy(gameObject);
    }
}
