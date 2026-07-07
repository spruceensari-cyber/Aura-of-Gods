using UnityEngine;
using System.Collections;

public class AOGSimpleCombatAnimator : MonoBehaviour
{
    [Header("Attack Visual")]
    public float attackForwardDistance = 0.45f;
    public float attackDuration = 0.12f;
    public float returnDuration = 0.12f;
    public float attackTiltAngle = 8f;

    [Header("Movement Visual")]
    public float moveBobAmount = 0.04f;
    public float moveBobSpeed = 9f;

    private Vector3 originalLocalPosition;
    private Quaternion originalLocalRotation;
    private bool isAttacking;
    private float bobTimer;

    void Start()
    {
        originalLocalPosition = transform.localPosition;
        originalLocalRotation = transform.localRotation;
    }

    void Update()
    {
        if (isAttacking)
            return;

        bool isMoving = IsObjectMoving();

        if (isMoving)
        {
            bobTimer += Time.deltaTime * moveBobSpeed;
            float bob = Mathf.Sin(bobTimer) * moveBobAmount;

            transform.localPosition = originalLocalPosition + new Vector3(0f, bob, 0f);
        }
        else
        {
            transform.localPosition = Vector3.Lerp(
                transform.localPosition,
                originalLocalPosition,
                Time.deltaTime * 8f
            );
        }
    }

    Vector3 lastPosition;

    bool IsObjectMoving()
    {
        float moved = Vector3.Distance(transform.position, lastPosition);
        lastPosition = transform.position;

        return moved > 0.002f;
    }

    public void PlayAttack()
    {
        if (!gameObject.activeInHierarchy)
            return;

        StopAllCoroutines();
        StartCoroutine(AttackRoutine());
    }

    IEnumerator AttackRoutine()
    {
        isAttacking = true;

        Vector3 startPos = originalLocalPosition;
        Quaternion startRot = originalLocalRotation;

        Vector3 attackPos = originalLocalPosition + transform.InverseTransformDirection(transform.forward) * attackForwardDistance;
        Quaternion attackRot = originalLocalRotation * Quaternion.Euler(attackTiltAngle, 0f, 0f);

        float t = 0f;

        while (t < attackDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / attackDuration);

            transform.localPosition = Vector3.Lerp(startPos, attackPos, p);
            transform.localRotation = Quaternion.Slerp(startRot, attackRot, p);

            yield return null;
        }

        t = 0f;

        while (t < returnDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / returnDuration);

            transform.localPosition = Vector3.Lerp(attackPos, originalLocalPosition, p);
            transform.localRotation = Quaternion.Slerp(attackRot, originalLocalRotation, p);

            yield return null;
        }

        transform.localPosition = originalLocalPosition;
        transform.localRotation = originalLocalRotation;

        isAttacking = false;
    }
}