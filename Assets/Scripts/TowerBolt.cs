using UnityEngine;

public class TowerBolt : MonoBehaviour
{
    public Transform targetTransform;
    public Minion minionTarget;
    public AOGCharacterStats heroTarget;
    public float damage = 30f;
    public float speed = 18f;
    public float lifeTime = 3f;
    public Color color = Color.red;

    private TrailRenderer trail;

    private void Start()
    {
        Destroy(gameObject, lifeTime);
        trail = GetComponent<TrailRenderer>();
    }

    private void Update()
    {
        if (targetTransform == null || !targetTransform.gameObject.activeInHierarchy)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 targetPos = targetTransform.position + Vector3.up * (heroTarget != null ? 1.35f : 1.05f);
        Vector3 direction = targetPos - transform.position;
        float distance = direction.magnitude;

        if (distance <= Mathf.Max(0.22f, speed * Time.deltaTime))
        {
            ResolveHit(targetPos);
            return;
        }

        transform.position += direction.normalized * speed * Time.deltaTime;
        transform.rotation = Quaternion.LookRotation(direction.normalized);
    }

    private void ResolveHit(Vector3 hitPoint)
    {
        if (minionTarget != null && minionTarget.hp > 0f)
            minionTarget.TakeDamage(damage, gameObject);
        else if (heroTarget != null && !heroTarget.IsDead)
            heroTarget.TakeDamage(damage);

        GameObject ring = AOGAbilityVisuals.CreateRing("Tower_Bolt_Impact", hitPoint, 0.92f, color, 0.09f);
        Destroy(ring, 0.35f);

        AOGMobaCameraController camera = Camera.main != null ? Camera.main.GetComponent<AOGMobaCameraController>() : null;
        if (heroTarget != null)
            camera?.AddRandomImpulse(0.16f);

        Destroy(gameObject);
    }
}
