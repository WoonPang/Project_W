using UnityEngine;

[RequireComponent(typeof(PlayerStats))]
public sealed class AutoAttack2D : MonoBehaviour
{
    [SerializeField, Min(0f)] private float attackRange = 6f;
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
        CombatTarget2D nearest = null;
        float nearestSqrDistance = attackRange * attackRange;

        foreach (CombatTarget2D dummy in FindObjectsByType<CombatTarget2D>(FindObjectsSortMode.None))
        {
            if (!dummy.IsAlive)
                continue;

            float sqrDistance = (dummy.transform.position - transform.position).sqrMagnitude;
            if (sqrDistance > nearestSqrDistance)
                continue;

            nearest = dummy;
            nearestSqrDistance = sqrDistance;
        }

        return nearest;
    }
}
