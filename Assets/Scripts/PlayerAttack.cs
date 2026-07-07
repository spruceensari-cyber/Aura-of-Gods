using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    public float attackRange = 4f;
    public float damage = 25f;

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) // sol tık
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Minion minion = hit.collider.GetComponent<Minion>();

                if (minion != null)
                {
                    float distance = Vector3.Distance(transform.position, minion.transform.position);

                    if (distance <= attackRange)
                    {
                        minion.TakeDamage(damage, gameObject);
                    }
                }
            }
        }
    }
}