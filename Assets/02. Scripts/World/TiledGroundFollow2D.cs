using UnityEngine;

public sealed class TiledGroundFollow2D : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField, Min(0.01f)] private float tileWorldSize = 25.6f;

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        FollowTarget();
    }

    public void ConfigureTileWorldSize(float size)
    {
        tileWorldSize = Mathf.Max(0.01f, size);
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

        // Move by a whole texture repeat so the ash pattern stays fixed in world space.
        float step = Mathf.Max(0.01f, tileWorldSize);
        transform.position = new Vector3(
            Mathf.Round(target.position.x / step) * step,
            Mathf.Round(target.position.y / step) * step,
            transform.position.z);
    }
}
