using UnityEngine;

public enum Team
{
    Solar,
    Void
}



public class TowerCombat : MonoBehaviour
{
    public Team towerTeam = Team.Void;

    public float range = 12f;
    public float fireRate = 1f;
    public float damage = 20f;

    private float nextFireTime;

    void Update()
    {
        GameObject player = GameObject.Find("Kaelith_Player");
        if (player == null) return;

        PlayerTeam playerTeam = player.GetComponent<PlayerTeam>();

        if (playerTeam != null && playerTeam.team == towerTeam)
        {
            return;
        }

        float distance = Vector3.Distance(transform.position, player.transform.position);

        if (distance <= range && Time.time >= nextFireTime)
        {
            Attack(player);
            nextFireTime = Time.time + fireRate;
        }
    }

    void Attack(GameObject target)
    {
        GameObject bullet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bullet.name = "Tower_Bullet";
        bullet.transform.position = transform.position + Vector3.up * 4f;
        bullet.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        bullet.GetComponent<Renderer>().material.color =
            towerTeam == Team.Void ? Color.red : Color.cyan;

        TowerBullet bulletScript = bullet.AddComponent<TowerBullet>();
        bulletScript.target = target.transform;
        bulletScript.damage = damage;
    }
}

public class TowerBullet : MonoBehaviour
{
    public Transform target;
    public float speed = 18f;
    public float damage = 20f;

    void Update()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 targetPos = target.position + Vector3.up;

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPos,
            speed * Time.deltaTime
        );

        if (Vector3.Distance(transform.position, targetPos) < 0.3f)
        {
            PlayerHealth hp = target.GetComponent<PlayerHealth>();

            if (hp != null)
            {
                hp.TakeDamage(damage);
                Debug.Log("Tower hit Kaelith! HP: " + hp.currentHealth);
            }

            Destroy(gameObject);
        }
    }
}