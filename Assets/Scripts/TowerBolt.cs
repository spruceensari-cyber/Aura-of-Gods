using UnityEngine;

public class TowerBolt : MonoBehaviour
{
    public Minion minionTarget;
    public Champion championTarget;
    public float damage = 30f;
    public float speed = 18f;
    public float lifeTime = 3f;
    public DamageType championDamageType = DamageType.Magical;

    private float expiresAt;
    private AOGPooledObject pooled;

    void OnEnable()
    {
        expiresAt = Time.time + lifeTime;
        pooled = GetComponent<AOGPooledObject>();
    }

    void Update()
    {
        if (Time.time >= expiresAt)
        {
            Release();
            return;
        }

        Transform targetTransform = null;
        if (minionTarget != null)
            targetTransform = minionTarget.transform;
        else if (championTarget != null && championTarget.IsAlive)
            targetTransform = championTarget.transform;

        if (targetTransform == null)
        {
            Release();
            return;
        }

        Vector3 targetPos = targetTransform.position + Vector3.up * 1.2f;
        transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetPos) <= 0.25f)
        {
            if (minionTarget != null)
                minionTarget.TakeDamage(damage, gameObject);
            else if (championTarget != null)
                championTarget.TakeDamage(damage, championDamageType);

            AOGAudioDirectorRuntime.Instance?.PlayCue(AOGAudioCue.AbilityImpact, targetPos);
            Release();
        }
    }

    private void Release()
    {
        minionTarget = null;
        championTarget = null;
        if (pooled != null)
            pooled.Release();
        else
            Destroy(gameObject);
    }
}
