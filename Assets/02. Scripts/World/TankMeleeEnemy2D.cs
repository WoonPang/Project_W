using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(PolygonCollider2D), typeof(EnemyStatus2D))]
public sealed class TankMeleeEnemy2D : CombatTarget2D, ILevelScaledEnemyHealth
{
    [SerializeField, Min(1f)] private float maxHealth = 32.5f;
    [SerializeField, Min(0f)] private float attackDamage = 5f;
    [SerializeField, Min(0f)] private float moveSpeed = 0.75f;
    [SerializeField, Min(0f)] private float experienceOnDeath = 7.5f;
    [SerializeField, Min(0.1f)] private float attackInterval = 1f;
    [SerializeField, Min(0.1f)] private float attackRange = 1.15f;
    [SerializeField, Min(0.1f)] private float stopDistance = 1f;
    [SerializeField, Min(0.1f)] private float worldUnitsPerMoveSpeed = 5f;
    [SerializeField, Min(1f)] private float despawnDistance = 30f;
    [SerializeField] private SpriteRenderer bodyRenderer;
    [SerializeField] private SpriteRenderer healthFillRenderer;

    private Rigidbody2D body;
    private EnemyStatus2D status;
    private PlayerStats player;
    private PlayerProgress playerProgress;
    private float currentHealth;
    private float nextAttackAt;

    public override bool IsAlive => currentHealth > 0f;
    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    public float AttackDamage => attackDamage;
    public float MoveSpeed => moveSpeed;
    public float ExperienceOnDeath => experienceOnDeath;

    public void Configure(SpriteRenderer enemyBody, SpriteRenderer healthFill)
    {
        bodyRenderer = enemyBody;
        healthFillRenderer = healthFill;
    }

    public void ApplyLevelHealth(int level)
    {
        float targetMaximum = EnemyHealthScaling2D.MaxHealth(EnemyHealthKind.TankMelee, level);
        currentHealth = EnemyHealthScaling2D.ScaleRemainingHealth(
            currentHealth, maxHealth, targetMaximum);
        maxHealth = targetMaximum;
        UpdateHealthBar();
    }

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        status = GetComponent<EnemyStatus2D>();
        player = FindFirstObjectByType<PlayerStats>();
        if (player != null)
            playerProgress = player.GetComponent<PlayerProgress>();
        currentHealth = maxHealth;
        ApplyLevelHealth(EnemyHealthScaling2D.CurrentPlayerLevel());
    }

    private void Update()
    {
        if (Time.timeScale <= 0f || !IsAlive)
            return;
        if (player == null)
        {
            player = FindFirstObjectByType<PlayerStats>();
            if (player != null)
                playerProgress = player.GetComponent<PlayerProgress>();
        }
        if (player == null)
            return;

        float distance = Vector2.Distance(transform.position, player.transform.position);
        if (distance > despawnDistance)
        {
            Destroy(gameObject);
            return;
        }
        if (distance <= attackRange && player.CurrentHealth > 0f &&
            Time.time >= nextAttackAt && (status == null || !status.IsStunned))
        {
            player.ReceiveDamage(attackDamage);
            nextAttackAt = Time.time + attackInterval;
        }
        if (bodyRenderer != null)
            bodyRenderer.color = status != null && status.IsStunned
                ? new Color(1f, 0.88f, 0.24f)
                : status != null && status.IsSlowed
                    ? new Color(0.56f, 0.91f, 1f)
                    : Color.white;
    }

    private void FixedUpdate()
    {
        if (Time.timeScale <= 0f || !IsAlive || player == null || player.CurrentHealth <= 0f)
            return;
        Vector2 difference = (Vector2)player.transform.position - body.position;
        float distance = difference.magnitude;
        if (distance <= stopDistance)
            return;
        float multiplier = status != null ? status.MovementMultiplier : 1f;
        float step = moveSpeed * worldUnitsPerMoveSpeed * multiplier * Time.fixedDeltaTime;
        body.MovePosition(body.position + difference.normalized * Mathf.Min(step, distance - stopDistance));
    }

    public override void TakeDamage(float amount)
    {
        if (!IsAlive || amount <= 0f)
            return;
        currentHealth = Mathf.Max(0f, currentHealth - amount);
        ReportDamage(amount, transform.position + Vector3.up * 1.05f);
        UpdateHealthBar();
        if (IsAlive)
            return;
        if (playerProgress == null)
            playerProgress = FindFirstObjectByType<PlayerProgress>();
        if (playerProgress != null)
            playerProgress.AddExperience(experienceOnDeath);
        Destroy(gameObject);
    }

    private void UpdateHealthBar()
    {
        if (healthFillRenderer == null)
            return;
        float ratio = maxHealth > 0f ? Mathf.Clamp01(currentHealth / maxHealth) : 0f;
        healthFillRenderer.transform.localScale = new Vector3(0.94f * ratio, 0.08f, 1f);
        healthFillRenderer.transform.localPosition =
            new Vector3(-0.47f * (1f - ratio), 0.76f, -0.02f);
        healthFillRenderer.color = Color.Lerp(
            new Color(0.95f, 0.25f, 0.25f), new Color(0.28f, 0.9f, 0.42f), ratio);
    }
}
