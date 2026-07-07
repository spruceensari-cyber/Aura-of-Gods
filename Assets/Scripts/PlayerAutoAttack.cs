using UnityEngine;
using System.Collections;

public class PlayerAutoAttack : MonoBehaviour
{
    public float attackRange = 4f;
    public float attackDamage = 35f;
    public float attackCooldown = 1f;

    public Transform spear;
    public Vector3 spearAttackLocalPosition = new Vector3(0.35f, 0.3f, 1.2f);

    private Vector3 spearIdleLocalPosition;
    private float nextAttackTime = 0f;
    private Minion currentTarget;
    private bool isAttacking = false;

    void Start()
    {
        if (spear != null)
        {
            spearIdleLocalPosition = spear.localPosition;
        }
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(1))
        {
            SelectTargetWithRightClick();
        }

        if (currentTarget == null) return;
        if (isAttacking) return;

        float distance = Vector3.Distance(transform.position, currentTarget.transform.position);

        if (distance <= attackRange && Time.time >= nextAttackTime)
        {
            StartCoroutine(SpearAttack());
        }
    }

    void SelectTargetWithRightClick()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 1000f))
        {
            Minion minion = hit.collider.GetComponent<Minion>();

            if (minion == null)
            {
                minion = hit.collider.GetComponentInParent<Minion>();
            }

            if (minion != null)
            {
                currentTarget = minion;
                Debug.Log("Hedef seçildi: " + minion.name);
            }
            else
            {
                currentTarget = null;
                Debug.Log("Minyon seçilmedi.");
            }
        }
    }

    IEnumerator SpearAttack()
    {
        if (currentTarget == null) yield break;

        isAttacking = true;
        nextAttackTime = Time.time + attackCooldown;

        Vector3 lookPos = currentTarget.transform.position - transform.position;
        lookPos.y = 0f;

        if (lookPos != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(lookPos);
        }

        float duration = 0.12f;
        float timer = 0f;

        if (spear != null)
        {
            while (timer < duration)
            {
                timer += Time.deltaTime;
                spear.localPosition = Vector3.Lerp(
                    spearIdleLocalPosition,
                    spearAttackLocalPosition,
                    timer / duration
                );

                yield return null;
            }
        }

        if (currentTarget != null)
        {
            float distance = Vector3.Distance(transform.position, currentTarget.transform.position);

            if (distance <= attackRange)
            {
                currentTarget.TakeDamage(attackDamage, gameObject);
                Debug.Log("Mızrak darbesi vurdu: " + currentTarget.name);
            }
        }

        timer = 0f;

        if (spear != null)
        {
            while (timer < duration)
            {
                timer += Time.deltaTime;
                spear.localPosition = Vector3.Lerp(
                    spearAttackLocalPosition,
                    spearIdleLocalPosition,
                    timer / duration
                );

                yield return null;
            }

            spear.localPosition = spearIdleLocalPosition;
        }

        isAttacking = false;
    }
}