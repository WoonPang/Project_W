using UnityEngine;

public sealed class CameraFollow2D : MonoBehaviour
{
    [SerializeField] private Transform target;

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        FollowTarget();
    }

    private void LateUpdate()
    {
        FollowTarget();
    }

    private void FollowTarget()
    {
        if (target == null)
            return;

        Vector3 position = transform.position;
        transform.position = new Vector3(target.position.x, target.position.y, position.z);
    }
}
