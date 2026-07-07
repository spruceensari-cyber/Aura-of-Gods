using UnityEngine;

public class AOGMobaCameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Camera Position")]
    public Vector3 offset = new Vector3(0f, 18f, -12f);

    [Header("Follow Settings")]
    public float followSpeed = 8f;

    [Header("Look Settings")]
    public Vector3 lookOffset = new Vector3(0f, 0f, 2f);

    void LateUpdate()
    {
        if (target == null)
            return;

        Vector3 desiredPosition = target.position + offset;

        transform.position = Vector3.Lerp(
            transform.position,
            desiredPosition,
            followSpeed * Time.deltaTime
        );

        Vector3 lookPoint = target.position + lookOffset;

        transform.rotation = Quaternion.LookRotation(
            lookPoint - transform.position
        );
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}