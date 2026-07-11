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
    private AOGTowerBoltPoolRuntime pool;
    private float expiresAt;
    private bool prepared;

    public void Prepare(AOGTowerBoltPoolRuntime ownerPool,Transform target,Minion minion,AOGCharacterStats hero,float amount,float moveSpeed,Color boltColor,float lifetime)
    {
        pool = ownerPool;
        targetTransform = target;
        minionTarget = minion;
        heroTarget = hero;
        damage = amount;
        speed = moveSpeed;
        color = boltColor;
        lifeTime = lifetime;
        expiresAt = Time.time + lifetime;
        prepared = true;
        if (trail == null) trail = GetComponent<TrailRenderer>();
    }

    private void OnEnable()
    {
        if (trail == null) trail = GetComponent<TrailRenderer>();
        if (!prepared) expiresAt = Time.time + lifeTime;
    }

    private void Update()
    {
        if (Time.time >= expiresAt)
        {
            Release();
            return;
        }

        if (targetTransform == null || !targetTransform.gameObject.activeInHierarchy)
        {
            Release();
            return;
        }

        Vector3 targetPos = targetTransform.position + Vector3.up * (heroTarget != null ? 1.35f : 1.05f);
        Vector3 direction = targetPos - transform.position;
        float distance = direction.magnitude;

        if (distance <= Mathf.Max(0.22f,speed*Time.deltaTime))
        {
            ResolveHit(targetPos);
            return;
        }

        Vector3 normalized = direction.normalized;
        transform.position += normalized*speed*Time.deltaTime;
        transform.rotation = Quaternion.LookRotation(normalized);
    }

    private void ResolveHit(Vector3 hitPoint)
    {
        if (minionTarget != null && minionTarget.hp > 0f)
            minionTarget.TakeDamage(damage,gameObject);
        else if (heroTarget != null && !heroTarget.IsDead)
            heroTarget.TakeDamage(damage);

        GameObject ring = AOGAbilityVisuals.CreateRing("Tower_Bolt_Impact",hitPoint,0.92f,color,0.09f);
        Destroy(ring,0.35f);

        AOGTransientPrimitivePoolRuntime.SpawnSphere(hitPoint,Vector3.one*0.34f,Color.Lerp(color,Color.white,0.28f),4.2f,0.18f);

        AOGMobaCameraController camera = Camera.main != null ? Camera.main.GetComponent<AOGMobaCameraController>() : null;
        if (heroTarget != null) camera?.AddRandomImpulse(0.12f);
        Release();
    }

    private void Release()
    {
        prepared = false;
        targetTransform = null;
        minionTarget = null;
        heroTarget = null;
        if (pool != null)
            pool.Return(gameObject);
        else
            Destroy(gameObject);
    }
}
