using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(EnemyStatus2D))]
public sealed class AssassinMeleeEnemy2D : CombatTarget2D, ILevelScaledEnemyHealth
{
    [SerializeField, Min(1f)] private float maxHealth = 10f;
    [SerializeField, Min(0f)] private float attackDamage = 10f;
    [SerializeField, Min(0f)] private float moveSpeed = 1.2f;
    [SerializeField, Min(0f)] private float experienceOnDeath = 10f;
    [SerializeField, Min(0.1f)] private float worldUnitsPerMoveSpeed = 8.5f;
    [SerializeField, Min(0.1f)] private float attackRange = 0.95f;
    [SerializeField, Min(0.1f)] private float stopDistance = 0.7f;
    [SerializeField, Min(0.1f)] private float attackInterval = 1f;
    [SerializeField, Min(0f)] private float contactPauseSeconds = 0.5f;
    [SerializeField, Min(1f)] private float despawnDistance = 30f;
    [SerializeField] private SpriteRenderer bodyRenderer;
    [SerializeField] private SpriteRenderer outlineRenderer;
    [SerializeField] private SpriteRenderer healthFillRenderer;

    private Rigidbody2D body;
    private EnemyStatus2D status;
    private PlayerStats player;
    private PlayerProgress playerProgress;
    private float currentHealth;
    private float nextAttackAt;
    private float stoppedUntil;

    public override bool IsAlive => currentHealth > 0f;
    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    public float AttackDamage => attackDamage;
    public float MoveSpeed => moveSpeed;
    public float ExperienceOnDeath => experienceOnDeath;
    public float ChaseWorldSpeed => moveSpeed * worldUnitsPerMoveSpeed;
    public float ContactPauseSeconds => contactPauseSeconds;
    public bool IsContactPaused => Time.time < stoppedUntil;

    public void Configure(SpriteRenderer enemyBody, SpriteRenderer outline, SpriteRenderer healthFill)
    {
        bodyRenderer = enemyBody;
        outlineRenderer = outline;
        healthFillRenderer = healthFill;
    }

    public void ApplyLevelHealth(int level)
    {
        float targetMaximum = EnemyHealthScaling2D.MaxHealth(EnemyHealthKind.AssassinMelee, level);
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
        if (bodyRenderer != null)
            bodyRenderer.color = status != null && status.IsStunned
                ? new Color(1f, 0.88f, 0.24f)
                : status != null && status.IsSlowed
                    ? new Color(0.56f, 0.91f, 1f)
                    : Color.white;
        if (distance <= attackRange && player.CurrentHealth > 0f &&
            Time.time >= nextAttackAt && (status == null || !status.IsStunned))
        {
            player.ReceiveDamage(attackDamage);
            nextAttackAt = Time.time + attackInterval;
            stoppedUntil = Time.time + contactPauseSeconds;
        }
    }

    private void FixedUpdate()
    {
        if (Time.timeScale <= 0f || !IsAlive || player == null ||
            player.CurrentHealth <= 0f || IsContactPaused)
            return;
        Vector2 difference = (Vector2)player.transform.position - body.position;
        float distance = difference.magnitude;
        if (distance <= 0.001f)
            return;

        float angle = Mathf.Atan2(difference.y, difference.x) * Mathf.Rad2Deg - 90f;
        Quaternion facing = Quaternion.Euler(0f, 0f, angle);
        if (bodyRenderer != null)
            bodyRenderer.transform.localRotation = facing;
        if (outlineRenderer != null)
            outlineRenderer.transform.localRotation = facing;

        if (distance <= stopDistance)
            return;
        float multiplier = status != null ? status.MovementMultiplier : 1f;
        float step = ChaseWorldSpeed * multiplier * Time.fixedDeltaTime;
        body.MovePosition(body.position + difference.normalized *
            Mathf.Min(step, distance - stopDistance));
    }

    public override void TakeDamage(float amount)
    {
        if (!IsAlive || amount <= 0f)
            return;
        currentHealth = Mathf.Max(0f, currentHealth - amount);
        ReportDamage(amount, transform.position + Vector3.up * 0.95f);
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
            new Vector3(-0.47f * (1f - ratio), 0.75f, -0.02f);
        healthFillRenderer.color = Color.Lerp(
            new Color(0.95f, 0.25f, 0.25f), new Color(0.28f, 0.9f, 0.42f), ratio);
    }
}
