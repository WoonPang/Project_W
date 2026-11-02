using UnityEngine;

public sealed class EnemyProjectile2D : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float speed = 8f;
    [SerializeField, Min(0.1f)] private float lifetime = 3f;
    [SerializeField, Min(0.01f)] private float hitRadius = 0.16f;

    private Vector2 direction;
    private float damage;
    private float remainingLifetime;

    public void Launch(Vector2 shotDirection, float shotDamage)
    {
        direction = shotDirection.normalized;
        damage = shotDamage;
        remainingLifetime = lifetime;
    }

    private void Update()
    {
        if (Time.timeScale <= 0f || direction == Vector2.zero)
            return;
        Vector2 start = transform.position;
        float distance = speed * Time.deltaTime;
        RaycastHit2D[] hits = Physics2D.CircleCastAll(start, hitRadius, direction, distance);
        foreach (RaycastHit2D hit in hits)
        {
            PlayerStats player = hit.collider.GetComponentInParent<PlayerStats>();
            if (player == null)
                continue;
            player.ReceiveDamage(damage);
            Destroy(gameObject);
            return;
        }
        transform.position = start + direction * distance;
        remainingLifetime -= Time.deltaTime;
        if (remainingLifetime <= 0f)
            Destroy(gameObject);
    }
}
