using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(EnemyStatus2D))]
public sealed class ChargingRangedEnemy2D : CombatTarget2D, ILevelScaledEnemyHealth
{
    [SerializeField, Min(1f)] private float maxHealth = 10f;
    [SerializeField, Min(0f)] private float attackDamage = 15f;
    [SerializeField, Min(0f)] private float moveSpeed = 1f;
    [SerializeField, Min(0f)] private float experienceOnDeath = 10f;
    [SerializeField, Min(0.1f)] private float attackRange = 8f;
    [SerializeField, Min(0.1f)] private float preferredDistance = 5f;
    [SerializeField, Min(0f)] private float screenEdgeInset = 2.5f;
    [SerializeField, Min(0.1f)] private float chargeSeconds = 1.8f;
    [SerializeField, Range(0f, 1f)] private float aimLockFraction = 0.6f;
    [SerializeField, Min(0.1f)] private float attackCooldown = 3f;
    [SerializeField, Min(0.1f)] private float warningSweepSpeed = 20f;
    [SerializeField, Min(0.01f)] private float laserHitRadius = 0.55f;
    [SerializeField, Min(0.1f)] private float worldUnitsPerMoveSpeed = 5f;
    [SerializeField, Min(1f)] private float despawnDistance = 30f;
    [SerializeField] private SpriteRenderer bodyRenderer;
    [SerializeField] private SpriteRenderer healthFillRenderer;
    [SerializeField] private SpriteRenderer warningGlow;
    [SerializeField] private SpriteRenderer warningCore;
    [SerializeField] private SpriteRenderer laserFlash;

    private Rigidbody2D body;
    private EnemyStatus2D status;
    private PlayerStats player;
    private PlayerProgress playerProgress;
    private float currentHealth;
    private float nextAttackAt;
    private float chargeStartedAt;
    private float flashStartedAt = -1f;
    private float firedBeamLength;
    private Vector2 aimDirection;
    private bool charging;
    private bool aimLocked;

    public override bool IsAlive => currentHealth > 0f;
    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    public float AttackDamage => attackDamage;
    public float MoveSpeed => moveSpeed;
    public float ExperienceOnDeath => experienceOnDeath;
    public float ChargeSeconds => chargeSeconds;
    public bool IsCharging => charging;

    public void Configure(SpriteRenderer bodySprite, SpriteRenderer healthFill,
        SpriteRenderer glow, SpriteRenderer core, SpriteRenderer flash)
    {
        bodyRenderer = bodySprite;
        healthFillRenderer = healthFill;
        warningGlow = glow;
        warningCore = core;
        laserFlash = flash;
    }

    public void ApplyLevelHealth(int level)
    {
        float targetMaximum = EnemyHealthScaling2D.MaxHealth(EnemyHealthKind.ChargingRanged, level);
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
        HideLaser();
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

        UpdateFlash();
        if (status != null && status.IsStunned)
        {
            if (charging)
                CancelCharge();
            bodyRenderer.color = new Color(1f, 0.88f, 0.24f);
            return;
        }
        if (charging)
            UpdateCharge();
        else if (distance <= attackRange && IsInsideAttackArea() &&
                 player.CurrentHealth > 0f && Time.time >= nextAttackAt)
            BeginCharge();

        float chargeProgress = charging
            ? Mathf.Clamp01((Time.time - chargeStartedAt) / chargeSeconds) : 0f;
        bodyRenderer.color = charging
            ? Color.Lerp(Color.white, new Color(1f, 0.48f, 0.48f), chargeProgress * 0.8f)
            : status != null && status.IsSlowed
                ? new Color(0.56f, 0.91f, 1f)
                : Color.white;
    }

    private void FixedUpdate()
    {
        if (Time.timeScale <= 0f || !IsAlive || charging || player == null ||
            player.CurrentHealth <= 0f)
            return;
        Vector2 difference = (Vector2)player.transform.position - body.position;
        float distance = difference.magnitude;
        float stoppingDistance = IsInsideAttackArea() ? preferredDistance : 0f;
        if (distance <= stoppingDistance || distance <= 0.001f)
            return;
        float multiplier = status != null ? status.MovementMultiplier : 1f;
        float step = moveSpeed * worldUnitsPerMoveSpeed * multiplier * Time.fixedDeltaTime;
        body.MovePosition(body.position + difference.normalized * Mathf.Min(step, distance - stoppingDistance));
    }

    public bool IsInsideAttackArea()
    {
        Camera camera = Camera.main;
        if (camera == null || !camera.orthographic)
            return false;
        Vector3 viewport = camera.WorldToViewportPoint(transform.position);
        float halfHeight = camera.orthographicSize;
        float halfWidth = halfHeight * camera.aspect;
        float horizontalInset = Mathf.Min(0.45f, screenEdgeInset / (2f * halfWidth));
        float verticalInset = Mathf.Min(0.45f, screenEdgeInset / (2f * halfHeight));
        return viewport.z > 0f &&
            viewport.x >= horizontalInset && viewport.x <= 1f - horizontalInset &&
            viewport.y >= verticalInset && viewport.y <= 1f - verticalInset;
    }

    private void BeginCharge()
    {
        charging = true;
        aimLocked = false;
        chargeStartedAt = Time.time;
        aimDirection = ((Vector2)player.transform.position - (Vector2)transform.position).normalized;
        warningGlow.enabled = true;
        warningCore.enabled = true;
        UpdateWarning(0f);
    }

    private void UpdateCharge()
    {
        float progress = Mathf.Clamp01((Time.time - chargeStartedAt) / chargeSeconds);
        if (!aimLocked)
        {
            Vector2 desired = (Vector2)player.transform.position - (Vector2)transform.position;
            if (desired.sqrMagnitude > 0.001f)
                aimDirection = Vector2.Lerp(aimDirection, desired.normalized,
                    Mathf.Clamp01(Time.deltaTime * 8f)).normalized;
            if (progress >= aimLockFraction)
                aimLocked = true;
        }
        UpdateWarning(progress);
        if (progress < 1f)
            return;
        FireLaser();
    }

    private void UpdateWarning(float progress)
    {
        float length = DistanceToCameraEdge(Camera.main, transform.position, aimDirection);
        float sweepLength = Mathf.Min(length,
            Mathf.Max(0.1f, warningSweepSpeed * (Time.time - chargeStartedAt)));
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 18f);
        SetBeam(warningGlow, length, Mathf.Lerp(0.25f, 0.48f, progress),
            new Color(1f, 0.08f, 0.1f, 0.3f + 0.13f * progress));
        SetBeam(warningCore, sweepLength, Mathf.Lerp(0.045f, 0.11f, progress),
            new Color(1f, 0.12f + 0.18f * progress, 0.12f,
                0.48f + 0.35f * progress + 0.12f * pulse));
    }

    public static float DistanceToCameraEdge(Camera camera, Vector3 origin, Vector2 direction)
    {
        if (camera == null || !camera.orthographic || direction.sqrMagnitude < 0.0001f)
            return 0f;
        Vector3 start = camera.WorldToViewportPoint(origin);
        Vector3 step = camera.WorldToViewportPoint(origin + (Vector3)direction.normalized) - start;
        if (start.z <= 0f)
            return 0f;
        float horizontal = step.x > 0.00001f ? (1f - start.x) / step.x
            : step.x < -0.00001f ? -start.x / step.x : float.PositiveInfinity;
        float vertical = step.y > 0.00001f ? (1f - start.y) / step.y
            : step.y < -0.00001f ? -start.y / step.y : float.PositiveInfinity;
        return Mathf.Max(0f, Mathf.Min(horizontal, vertical)) + 0.05f;
    }

    private void FireLaser()
    {
        charging = false;
        warningGlow.enabled = false;
        warningCore.enabled = false;
        flashStartedAt = Time.time;
        firedBeamLength = DistanceToCameraEdge(Camera.main, transform.position, aimDirection);
        laserFlash.enabled = true;
        SetBeam(laserFlash, firedBeamLength, 0.28f, new Color(1f, 0.38f, 0.2f, 1f));
        Vector2 origin = (Vector2)transform.position + aimDirection * 0.55f;
        if (player.CurrentHealth > 0f && IsPointInLaser(origin, aimDirection,
                player.transform.position, firedBeamLength, laserHitRadius))
            player.ReceiveDamage(attackDamage);
        nextAttackAt = Time.time + attackCooldown;
    }

    public static bool IsPointInLaser(Vector2 origin, Vector2 direction,
        Vector2 point, float length, float hitRadius)
    {
        Vector2 offset = point - origin;
        float along = Vector2.Dot(offset, direction);
        if (along < 0f || along > length)
            return false;
        float perpendicular = Mathf.Abs(direction.x * offset.y - direction.y * offset.x);
        return perpendicular <= hitRadius;
    }

    private void UpdateFlash()
    {
        if (flashStartedAt < 0f)
            return;
        float remaining = Mathf.Clamp01(1f - (Time.time - flashStartedAt) / 0.22f);
        laserFlash.enabled = remaining > 0f;
        if (remaining <= 0f)
        {
            flashStartedAt = -1f;
            return;
        }
        SetBeam(laserFlash, firedBeamLength, 0.16f + 0.25f * remaining,
            new Color(1f, 0.3f + 0.35f * remaining, 0.2f, remaining));
    }

    private void CancelCharge()
    {
        charging = false;
        aimLocked = false;
        warningGlow.enabled = false;
        warningCore.enabled = false;
        nextAttackAt = Time.time + 0.5f;
    }

    private void SetBeam(SpriteRenderer renderer, float length, float width, Color color)
    {
        if (renderer == null)
            return;
        Transform beam = renderer.transform;
        beam.localPosition = (Vector3)(aimDirection * (length * 0.5f));
        beam.localRotation = Quaternion.Euler(0f, 0f,
            Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg);
        beam.localScale = new Vector3(length, width, 1f);
        renderer.color = color;
    }

    private void HideLaser()
    {
        if (warningGlow != null)
            warningGlow.enabled = false;
        if (warningCore != null)
            warningCore.enabled = false;
        if (laserFlash != null)
            laserFlash.enabled = false;
    }

    public override void TakeDamage(float amount)
    {
        if (!IsAlive || amount <= 0f)
            return;
        currentHealth = Mathf.Max(0f, currentHealth - amount);
        ReportDamage(amount, transform.position + Vector3.up * 0.9f);
        UpdateHealthBar();
        if (IsAlive)
            return;
        HideLaser();
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
            new Vector3(-0.47f * (1f - ratio), 0.78f, -0.02f);
        healthFillRenderer.color = Color.Lerp(
            new Color(0.95f, 0.25f, 0.25f), new Color(0.28f, 0.9f, 0.42f), ratio);
    }
}
