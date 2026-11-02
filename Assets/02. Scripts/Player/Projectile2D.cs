using UnityEngine;

public sealed class Projectile2D : MonoBehaviour
{
    private TrainingDummy2D target;
    private float damage;
    private float speed;
    private bool isDirectional;
    private Vector2 direction;
    private float directedLifetimeRemaining;

    public bool IsDirectional => isDirectional;

    public void Initialize(TrainingDummy2D newTarget, float newDamage, float newSpeed)
    {
        target = newTarget;
        damage = newDamage;
        speed = newSpeed;
    }

    public void InitializeDirection(Vector2 newDirection, float newDamage, float newSpeed)
    {
        target = null;
        direction = newDirection.normalized;
        damage = newDamage;
        speed = newSpeed;
        directedLifetimeRemaining = 2.5f;
        isDirectional = true;
    }

    private void Update()
    {
        if (Time.timeScale <= 0f)
            return;

        if (isDirectional)
        {
            UpdateDirectionalShot();
            return;
        }

        if (target == null || !target.IsAlive)
        {
            Destroy(gameObject);
            return;
        }

        transform.position = Vector3.MoveTowards(
            transform.position, target.transform.position, speed * Time.deltaTime);

        if ((transform.position - target.transform.position).sqrMagnitude > 0.02f)
            return;

        target.TakeDamage(damage);
        Destroy(gameObject);
    }

    private void UpdateDirectionalShot()
    {
        float step = speed * Time.deltaTime;
        RaycastHit2D[] hits = Physics2D.RaycastAll(transform.position, direction, step);
        TrainingDummy2D closestHit = null;
        float closestDistance = float.PositiveInfinity;
        foreach (RaycastHit2D hit in hits)
        {
            TrainingDummy2D dummy = hit.collider.GetComponentInParent<TrainingDummy2D>();
            if (dummy == null || !dummy.IsAlive || hit.distance >= closestDistance)
                continue;
            closestHit = dummy;
            closestDistance = hit.distance;
        }

        if (closestHit != null)
        {
            closestHit.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        transform.position += (Vector3)(direction * step);
        directedLifetimeRemaining -= Time.deltaTime;
        if (directedLifetimeRemaining <= 0f)
            Destroy(gameObject);
    }
}
