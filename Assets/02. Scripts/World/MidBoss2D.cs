using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(EnemyStatus2D))]
public sealed class MidBoss2D : CombatTarget2D, ILevelScaledEnemyHealth
{
    private enum BossAction { Chasing, SlamWarning, DashWarning, DashHold, Dashing, SummonWarning }

    [SerializeField, Min(1f)] private float maxHealth = 150f;
    [SerializeField, Min(0f)] private float attackDamage = 10f;
    [SerializeField, Min(0f)] private float moveSpeed = 1.25f;
    [SerializeField, Min(0f)] private float experienceOnDeath = 50f;
    [SerializeField, Min(0.1f)] private float worldUnitsPerMoveSpeed = 5f;
    [SerializeField, Min(0.1f)] private float stopDistance = 2.1f;
    [SerializeField, Min(0.1f)] private float slamStartRange = 2.3f;
    [SerializeField, Min(0.1f)] private float slamRadius = 4.5f;
    [SerializeField, Min(0.1f)] private float slamWarningSeconds = 0.8f;
    [SerializeField, Min(0.1f)] private float dashWarningSeconds = 1f;
    [SerializeField, Min(0f)] private float dashHoldSeconds = 0.5f;
    [SerializeField, Min(0.1f)] private float dashDistance = 12f;
    [SerializeField, Min(0.1f)] private float patternStartRange = 7f;
    [SerializeField, Min(0.1f)] private float dashSpeed = 16f;
    [SerializeField, Min(0.1f)] private float dashHitRadius = 0.85f;
    [SerializeField, Min(0.1f)] private float summonWarningSeconds = 0.9f;
    [SerializeField, Min(0.1f)] private float actionInterval = 1.5f;
    [SerializeField, Min(0f)] private float slamCooldownSeconds = 5f;
    [SerializeField, Min(0f)] private float dashCooldownSeconds = 10f;
    [SerializeField, Min(0f)] private float minimumSummonPlayerDistance = 3.5f;
    [SerializeField] private SpriteRenderer bodyRenderer;
    [SerializeField] private SpriteRenderer healthFillRenderer;
    [SerializeField] private SpriteRenderer circleBoundary;
    [SerializeField] private SpriteRenderer circleFill;
    [SerializeField] private SpriteRenderer dashBoundary;
    [SerializeField] private SpriteRenderer dashFill;
    [SerializeField] private BasicMeleeEnemy2D meleePrefab;
    [SerializeField] private BasicRangedEnemy2D rangedPrefab;

    private Rigidbody2D body;
    private EnemyStatus2D status;
    private PlayerStats player;
    private PlayerProgress playerProgress;
    private Camera worldCamera;
    private float currentHealth;
    private float actionStartedAt;
    private float nextActionAt;
    private float nextSlamAt;
    private float nextDashAt;
    private bool summonPending;
    private bool hasSummonedAtHalfHealth;
    private float slamFlashUntil;
    private float dashRemaining;
    private Vector2 dashDirection;
    private bool dashHasHit;
    private BossAction action;

    public override bool IsAlive => currentHealth > 0f;
    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    public float AttackDamage => attackDamage;
    public float MoveSpeed => moveSpeed;
    public float ExperienceOnDeath => experienceOnDeath;
    public float SlamDamage => attackDamage * 1.75f;
    public float DashDamage => attackDamage * 1.25f;
    public bool IsChargingSlam => action == BossAction.SlamWarning;
    public bool IsChargingDash => action == BossAction.DashWarning;
    public bool IsWaitingToDash => action == BossAction.DashHold;
    public bool IsDashing => action == BossAction.Dashing;
    public bool IsChargingSummon => action == BossAction.SummonWarning;
    public bool HasSummonedAtHalfHealth => hasSummonedAtHalfHealth;
    public bool IsSummonInvulnerable => action == BossAction.SummonWarning ||
        (summonPending && !MonsterSpawnGate2D.IsPaused);

    public void Configure(SpriteRenderer bossBody, SpriteRenderer healthFill,
        SpriteRenderer slamBoundary, SpriteRenderer slamFill,
        SpriteRenderer chargeBoundary, SpriteRenderer chargeFill,
        BasicMeleeEnemy2D melee, BasicRangedEnemy2D ranged)
    {
        bodyRenderer = bossBody;
        healthFillRenderer = healthFill;
        circleBoundary = slamBoundary;
        circleFill = slamFill;
        dashBoundary = chargeBoundary;
        dashFill = chargeFill;
        meleePrefab = melee;
        rangedPrefab = ranged;
    }

    public void ApplyLevelHealth(int level)
    {
        float targetMaximum = EnemyHealthScaling2D.MaxHealth(EnemyHealthKind.MidBoss, level);
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
        worldCamera = Camera.main;
        currentHealth = maxHealth;
        ApplyLevelHealth(EnemyHealthScaling2D.CurrentPlayerLevel());
        HideWarnings();
        nextActionAt = Time.time + 1f;
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
        if (player == null || player.CurrentHealth <= 0f)
            return;
        UpdateSlamFlash();
        if (status != null && status.IsStunned)
        {
            if (action != BossAction.Chasing)
                CancelAction();
            bodyRenderer.color = new Color(1f, 0.86f, 0.25f);
            return;
        }

        if (action == BossAction.SlamWarning)
        {
            float progress = Mathf.Clamp01((Time.time - actionStartedAt) / slamWarningSeconds);
            UpdateSlamWarning(progress);
            if (progress >= 1f)
                ResolveSlam();
        }
        else if (action == BossAction.DashWarning)
        {
            float progress = Mathf.Clamp01((Time.time - actionStartedAt) / dashWarningSeconds);
            UpdateDashWarning(progress);
            if (progress >= 1f)
            {
                action = BossAction.DashHold;
                actionStartedAt = Time.time;
            }
        }
        else if (action == BossAction.DashHold)
        {
            Color color = dashFill.color;
            color.a = 0.65f + 0.15f * Mathf.Sin((Time.time - actionStartedAt) * 20f);
            dashFill.color = color;
            if (Time.time - actionStartedAt >= dashHoldSeconds)
                StartDash();
        }
        else if (action == BossAction.SummonWarning)
        {
            float progress = Mathf.Clamp01((Time.time - actionStartedAt) / summonWarningSeconds);
            UpdateSummonWarning(progress);
            if (progress >= 1f)
            {
                if (SummonMinions())
                {
                    summonPending = false;
                    hasSummonedAtHalfHealth = true;
                    EndAction();
                }
                else
                    CancelAction();
            }
        }
        else if (action == BossAction.Chasing && Time.time >= nextActionAt)
            TryStartPattern();
        bodyRenderer.color = action == BossAction.Chasing
            ? status != null && status.IsSlowed
                ? new Color(0.56f, 0.91f, 1f) : new Color(0.9f, 0.38f, 0.42f)
            : new Color(1f, 0.55f, 0.42f);
    }

    private void FixedUpdate()
    {
        if (Time.timeScale <= 0f || !IsAlive || player == null ||
            player.CurrentHealth <= 0f || status != null && status.IsStunned)
            return;
        float multiplier = status != null ? status.MovementMultiplier : 1f;
        if (action == BossAction.Dashing)
        {
            Vector2 from = body.position;
            SetRectangle(dashBoundary, dashRemaining, 1.65f,
                new Color(1f, 0.12f, 0.15f, 0.18f));
            float step = Mathf.Min(dashRemaining, dashSpeed * multiplier * Time.fixedDeltaTime);
            Vector2 to = from + dashDirection * step;
            body.MovePosition(to);
            if (!dashHasHit && IsPointNearSegment(from, to,
                    player.transform.position, dashHitRadius))
            {
                dashHasHit = true;
                player.ReceiveDamage(DashDamage);
            }
            dashRemaining -= step;
            if (dashRemaining <= 0.001f)
                EndAction();
            return;
        }
        if (action != BossAction.Chasing)
        {
            body.linearVelocity = Vector2.zero;
            return;
        }
        Vector2 difference = (Vector2)player.transform.position - body.position;
        float distance = difference.magnitude;
        if (distance <= stopDistance || distance <= 0.001f)
            return;
        float moveStep = moveSpeed * worldUnitsPerMoveSpeed * multiplier * Time.fixedDeltaTime;
        body.MovePosition(body.position + difference.normalized *
            Mathf.Min(moveStep, distance - stopDistance));
    }

    private void TryStartPattern()
    {
        if (action != BossAction.Chasing || Time.time < nextActionAt || player == null)
            return;

        if (summonPending && !MonsterSpawnGate2D.IsPaused)
        {
            BeginSummonWarning();
            return;
        }
        float distance = Vector2.Distance(transform.position, player.transform.position);
        if (distance > slamStartRange && distance <= patternStartRange &&
            Time.time >= nextDashAt)
        {
            BeginDashWarning();
            return;
        }
        if (distance <= slamStartRange && Time.time >= nextSlamAt)
        {
            BeginSlam();
            return;
        }
        if (distance <= patternStartRange && Time.time >= nextDashAt)
            BeginDashWarning();
    }

    private void BeginSlam()
    {
        body.linearVelocity = Vector2.zero;
        action = BossAction.SlamWarning;
        actionStartedAt = Time.time;
        slamFlashUntil = 0f;
        circleBoundary.enabled = true;
        circleBoundary.transform.localScale = Vector3.one * (slamRadius * 2f);
        circleBoundary.color = new Color(1f, 0.1f, 0.12f, 0.18f);
        circleFill.enabled = true;
        UpdateSlamWarning(0f);
    }

    private void UpdateSlamWarning(float progress)
    {
        circleFill.transform.localScale = Vector3.one *
            (slamRadius * 2f * Mathf.Max(0.02f, progress));
        circleFill.color = new Color(1f, 0.08f, 0.1f, 0.38f + 0.3f * progress);
    }

    private void ResolveSlam()
    {
        if (Vector2.Distance(transform.position, player.transform.position) <= slamRadius)
            player.ReceiveDamage(SlamDamage);
        circleBoundary.enabled = false;
        circleFill.transform.localScale = Vector3.one * (slamRadius * 2f);
        circleFill.color = new Color(1f, 0.4f, 0.35f, 0.7f);
        slamFlashUntil = Time.time + 0.2f;
        EndAction(false);
    }

    private void UpdateSlamFlash()
    {
        if (slamFlashUntil <= 0f)
            return;
        if (Time.time >= slamFlashUntil)
        {
            slamFlashUntil = 0f;
            circleFill.enabled = false;
            return;
        }
        Color color = circleFill.color;
        color.a = (slamFlashUntil - Time.time) / 0.2f * 0.7f;
        circleFill.color = color;
    }

    private void BeginDashWarning()
    {
        body.linearVelocity = Vector2.zero;
        dashDirection = ((Vector2)player.transform.position - (Vector2)transform.position).normalized;
        if (dashDirection.sqrMagnitude < 0.001f)
            dashDirection = Vector2.down;
        action = BossAction.DashWarning;
        actionStartedAt = Time.time;
        dashBoundary.enabled = true;
        dashFill.enabled = true;
        SetRectangle(dashBoundary, dashDistance, 1.65f,
            new Color(1f, 0.12f, 0.15f, 0.25f));
        UpdateDashWarning(0f);
    }

    private void UpdateDashWarning(float progress)
    {
        Vector2 towardPlayer = (Vector2)player.transform.position - (Vector2)transform.position;
        if (towardPlayer.sqrMagnitude > 0.001f)
            dashDirection = towardPlayer.normalized;
        SetRectangle(dashBoundary, dashDistance, 1.65f,
            new Color(1f, 0.12f, 0.15f, 0.25f));
        SetRectangle(dashFill, Mathf.Max(0.05f, dashDistance * progress), 1.35f,
            new Color(1f, 0.13f, 0.15f, 0.35f + 0.45f * progress));
    }

    private void SetRectangle(SpriteRenderer renderer, float length, float width, Color color)
    {
        renderer.transform.localPosition = (Vector3)(dashDirection * (length * 0.5f));
        renderer.transform.localRotation = Quaternion.Euler(0f, 0f,
            Mathf.Atan2(dashDirection.y, dashDirection.x) * Mathf.Rad2Deg);
        renderer.transform.localScale = new Vector3(length, width, 1f);
        renderer.color = color;
    }

    private void StartDash()
    {
        dashBoundary.enabled = true;
        dashFill.enabled = false;
        dashRemaining = dashDistance;
        dashHasHit = false;
        action = BossAction.Dashing;
    }

    private void BeginSummonWarning()
    {
        body.linearVelocity = Vector2.zero;
        action = BossAction.SummonWarning;
        actionStartedAt = Time.time;
        circleFill.enabled = true;
        UpdateSummonWarning(0f);
    }

    private void UpdateSummonWarning(float progress)
    {
        circleFill.transform.localScale = Vector3.one * Mathf.Lerp(1.5f, 4.5f, progress);
        circleFill.color = new Color(0.85f, 0.35f, 1f, 0.2f + 0.3f * progress);
    }

    private bool SummonMinions()
    {
        if (MonsterSpawnGate2D.IsPaused || meleePrefab == null || rangedPrefab == null)
            return false;
        for (int index = 0; index < 6; index++)
        {
            Vector2 position = FindSummonPosition(index);
            if (index < 3)
                Instantiate(meleePrefab, position, Quaternion.identity);
            else
                Instantiate(rangedPrefab, position, Quaternion.identity);
        }
        return true;
    }

    private Vector2 FindSummonPosition(int index)
    {
        if (worldCamera == null)
            worldCamera = Camera.main;
        for (int attempt = 0; attempt < 12; attempt++)
        {
            float angle = (index + attempt * 0.31f) * Mathf.PI / 3f;
            Vector2 candidate = (Vector2)transform.position +
                new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (2.8f + attempt * 0.15f);
            Vector3 viewport = worldCamera.WorldToViewportPoint(candidate);
            if (viewport.z > 0f && viewport.x >= 0.08f && viewport.x <= 0.92f &&
                viewport.y >= 0.08f && viewport.y <= 0.92f &&
                Vector2.Distance(candidate, player.transform.position) >= minimumSummonPlayerDistance)
                return candidate;
        }
        return BasicMeleeEnemySpawner2D.PositionOutsideView(worldCamera,
            player.transform.position, 1.5f, minimumSummonPlayerDistance,
            index % 4, (index + 0.5f) / 6f);
    }

    private void EndAction(bool hideCircle = true)
    {
        switch (action)
        {
            case BossAction.SlamWarning:
                nextSlamAt = Time.time + slamCooldownSeconds;
                break;
            case BossAction.Dashing:
                nextDashAt = Time.time + dashCooldownSeconds;
                break;
        }
        if (hideCircle)
            circleFill.enabled = false;
        circleBoundary.enabled = false;
        dashBoundary.enabled = false;
        dashFill.enabled = false;
        action = BossAction.Chasing;
        nextActionAt = Time.time + (summonPending ? 0f : actionInterval);
    }

    private void CancelAction()
    {
        slamFlashUntil = 0f;
        HideWarnings();
        action = BossAction.Chasing;
        nextActionAt = Time.time + (summonPending ? 0f : actionInterval);
    }

    private void HideWarnings()
    {
        if (circleBoundary != null)
            circleBoundary.enabled = false;
        if (circleFill != null)
            circleFill.enabled = false;
        if (dashBoundary != null)
            dashBoundary.enabled = false;
        if (dashFill != null)
            dashFill.enabled = false;
    }

    public static bool IsPointNearSegment(Vector2 from, Vector2 to, Vector2 point, float radius)
    {
        Vector2 path = to - from;
        float fraction = path.sqrMagnitude > 0.0001f
            ? Mathf.Clamp01(Vector2.Dot(point - from, path) / path.sqrMagnitude) : 0f;
        return Vector2.Distance(point, from + path * fraction) <= radius;
    }

    public override void TakeDamage(float amount)
    {
        if (!IsAlive || amount <= 0f || IsSummonInvulnerable)
            return;
        bool aboveHalfHealth = currentHealth > maxHealth * 0.5f;
        currentHealth = Mathf.Max(0f, currentHealth - amount);
        if (aboveHalfHealth && currentHealth > 0f &&
            currentHealth <= maxHealth * 0.5f && !hasSummonedAtHalfHealth)
        {
            summonPending = true;
            nextActionAt = Time.time;
        }
        ReportDamage(amount, transform.position + Vector3.up * 1.5f);
        UpdateHealthBar();
        if (IsAlive)
            return;
        HideWarnings();
        if (playerProgress == null)
            playerProgress = FindFirstObjectByType<PlayerProgress>();
        if (playerProgress != null)
            playerProgress.RegisterEnemyKill(experienceOnDeath);
        Destroy(gameObject);
    }

    private void UpdateHealthBar()
    {
        if (healthFillRenderer == null)
            return;
        float ratio = maxHealth > 0f ? Mathf.Clamp01(currentHealth / maxHealth) : 0f;
        healthFillRenderer.transform.localScale = new Vector3(2.26f * ratio, 0.12f, 1f);
        healthFillRenderer.transform.localPosition =
            new Vector3(-1.13f * (1f - ratio), 1.54f, -0.02f);
        healthFillRenderer.color = Color.Lerp(
            new Color(0.95f, 0.25f, 0.25f), new Color(0.35f, 0.92f, 0.47f), ratio);
    }
}
