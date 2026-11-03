using UnityEngine;

[RequireComponent(typeof(PlayerStats))]
public sealed class AutoAttack2D : MonoBehaviour
{
    [SerializeField, Min(0f)] private float projectileSpeed = 12f;
    [SerializeField] private Projectile2D projectilePrefab;

    private PlayerStats stats;
    private float nextAttackTime;

    public void SetProjectilePrefab(Projectile2D prefab)
    {
        projectilePrefab = prefab;
    }

    private void Awake()
    {
        stats = GetComponent<PlayerStats>();
    }

    private void Update()
    {
        if (Time.timeScale <= 0f || projectilePrefab == null || Time.time < nextAttackTime)
            return;

        CombatTarget2D target = FindNearestTarget();
        if (target == null)
            return;

        Vector3 direction = (target.transform.position - transform.position).normalized;
        Projectile2D projectile = Instantiate(
            projectilePrefab, transform.position + direction * 0.5f, Quaternion.identity);
        projectile.Initialize(target, stats.AttackDamage, projectileSpeed);
        nextAttackTime = Time.time + 1f / Mathf.Max(0.01f, stats.AttackSpeed);
    }

    private CombatTarget2D FindNearestTarget()
    {
        Camera camera = Camera.main;
        if (camera == null)
            return null;

        CombatTarget2D nearest = null;
        float nearestSqrDistance = float.PositiveInfinity;

        foreach (CombatTarget2D target in FindObjectsByType<CombatTarget2D>(FindObjectsSortMode.None))
        {
            if (!target.IsAlive)
                continue;

            Vector3 viewport = camera.WorldToViewportPoint(target.transform.position);
            if (viewport.z <= 0f || viewport.x < 0f || viewport.x > 1f ||
                viewport.y < 0f || viewport.y > 1f)
                continue;

            float sqrDistance = (target.transform.position - transform.position).sqrMagnitude;
            if (sqrDistance >= nearestSqrDistance)
                continue;

            nearest = target;
            nearestSqrDistance = sqrDistance;
        }

        return nearest;
    }
}
