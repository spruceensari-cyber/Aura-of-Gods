using UnityEngine;

public class AOGSimpleMinionMover : MonoBehaviour
{
    public Transform target;
    public float moveSpeed = 2.5f;
    public float rotateSpeed = 8f;
    public float stopDistance = 1.5f;

    void Update()
    {
        if (target == null)
            return;

        Vector3 targetPos = target.position;
        targetPos.y = transform.position.y;

        Vector3 direction = targetPos - transform.position;

        if (direction.magnitude <= stopDistance)
            return;

        Vector3 moveDir = direction.normalized;

        transform.position += moveDir * moveSpeed * Time.deltaTime;

        if (moveDir != Vector3.zero)
        {
            Quaternion lookRot = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                lookRot,
                rotateSpeed * Time.deltaTime
            );
        }
    }
}