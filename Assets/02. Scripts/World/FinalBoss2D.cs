using Action = System.Action;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(EnemyStatus2D))]
public sealed class FinalBoss2D : CombatTarget2D, ILevelScaledEnemyHealth
{
    public static event Action Defeated;
    private enum BossAction { Chasing, LaserWarning, LaserFiring, MeteorStorm, SummonWarning }

    [SerializeField, Min(1f)] private float maxHealth = 500f;
    [SerializeField, Min(0f)] private float attackDamage = 20f;
    [SerializeField, Min(0f)] private float moveSpeed = 1.5f;
    [SerializeField, Min(0f)] private float experienceOnDeath = 100f;
    [SerializeField, Min(0.1f)] private float worldUnitsPerMoveSpeed = 5f;
    [SerializeField, Min(0.1f)] private float stopDistance = 5f;
    [SerializeField, Min(0.1f)] private float attackRange = 8f;
    [SerializeField, Min(0.1f)] private float scatterAttackInterval = 2.2f;
    [SerializeField, Range(1, 32)] private int scatterProjectileCount = 24;
    [SerializeField, Min(0.1f)] private float laserChargeSeconds = 1.5f;
    [SerializeField, Min(0.1f)] private float laserFlashSeconds = 0.3f;
    [SerializeField, Min(0.1f)] private float laserLength = 30f;
    [SerializeField, Min(0.01f)] private float laserHitRadius = 0.55f;
    [SerializeField, Range(0f, 1f)] private float laserAimLockFraction = 0.65f;
    [SerializeField, Min(0.1f)] private float meteorChargeSeconds = 0.8f;
    [SerializeField, Min(0.1f)] private float meteorImpactSeconds = 0.3f;
    [SerializeField, Min(1f)] private float meteorPatternSeconds = 5f;
    [SerializeField, Min(0.1f)] private float meteorHitRadius = 1.1f;
    [SerializeField, Min(0.1f)] private float summonChargeSeconds = 1f;
    [SerializeField, Min(0.1f)] private float actionInterval = 1.5f;
    [SerializeField, Min(0f)] private float laserCooldownSeconds = 7f;
    [SerializeField, Min(0f)] private float meteorCooldownSeconds = 15f;
    [SerializeField, Min(0f)] private float minimumSummonPlayerDistance = 3.5f;
    [SerializeField] private SpriteRenderer bodyRenderer;
    [SerializeField] private SpriteRenderer healthFillRenderer;
    [SerializeField] private SpriteRenderer laserBoundary;
    [SerializeField] private SpriteRenderer laserFill;
    [SerializeField] private SpriteRenderer laserBeam;
    [SerializeField] private SpriteRenderer summonGlow;
    [SerializeField] private SpriteRenderer[] meteorBoundaries;
    [SerializeField] private SpriteRenderer[] meteorFills;
    [SerializeField] private SpriteRenderer[] meteorFalling;
    [SerializeField] private SpriteRenderer[] meteorImpacts;
    [SerializeField] private EnemyProjectile2D scatterProjectilePrefab;
    [SerializeField] private CombatTarget2D[] summonPrefabs;

    private Rigidbody2D body;
    private EnemyStatus2D status;
    private PlayerStats player;
    private PlayerProgress playerProgress;
    private Camera worldCamera;
    private Vector2[] meteorPositions;
    private bool[] meteorScheduled;
    private bool[] meteorLanded;
    private int activeMeteorCount;
    private Vector2 laserDirection;
    private float currentHealth;
    private float actionStartedAt;
    private float nextActionAt;
    private float nextLaserAt;
    private float nextMeteorAt;
    private bool summonPending;
    private bool hasSummonedAtHalfHealth;
    private float nextScatterAt;
    private int scatterVolleyCount;
    private BossAction action;

    public override bool IsAlive => currentHealth > 0f;
    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    public float AttackDamage => attackDamage;
    public float MoveSpeed => moveSpeed;
    public float ExperienceOnDeath => experienceOnDeath;
    public bool IsChargingLaser => action == BossAction.LaserWarning;
    public bool IsChargingMeteor => action == BossAction.MeteorStorm;
    public bool IsChargingSummon => action == BossAction.SummonWarning;
    public bool HasSummonedAtHalfHealth => hasSummonedAtHalfHealth;
    public bool IsSummonInvulnerable => action == BossAction.SummonWarning ||
        (summonPending && !MonsterSpawnGate2D.IsPaused);

    public void Configure(SpriteRenderer bossBody, SpriteRenderer healthFill,
        SpriteRenderer beamBoundary, SpriteRenderer beamFill, SpriteRenderer beamFlash,
        SpriteRenderer summonEffect, SpriteRenderer[] meteorWarningCircles,
        SpriteRenderer[] meteorFillCircles, SpriteRenderer[] fallingMeteors,
        SpriteRenderer[] meteorFlashCircles, EnemyProjectile2D scatterProjectile,
        CombatTarget2D[] enemyPrefabs)
    {
        bodyRenderer = bossBody;
        healthFillRenderer = healthFill;
        laserBoundary = beamBoundary;
        laserFill = beamFill;
        laserBeam = beamFlash;
        summonGlow = summonEffect;
        meteorBoundaries = meteorWarningCircles;
        meteorFills = meteorFillCircles;
        meteorFalling = fallingMeteors;
        meteorImpacts = meteorFlashCircles;
        scatterProjectilePrefab = scatterProjectile;
        summonPrefabs = enemyPrefabs;
    }

    public void ApplyLevelHealth(int level)
    {
        float newMaximum = EnemyHealthScaling2D.MaxHealth(EnemyHealthKind.FinalBoss, level);
        currentHealth = EnemyHealthScaling2D.ScaleRemainingHealth(currentHealth, maxHealth,
            newMaximum);
        maxHealth = newMaximum;
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
        meteorPositions = new Vector2[meteorBoundaries.Length];
        meteorScheduled = new bool[meteorBoundaries.Length];
        meteorLanded = new bool[meteorBoundaries.Length];
        ApplyLevelHealth(EnemyHealthScaling2D.CurrentPlayerLevel());
        HideEffects();
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
        if (status != null && status.IsStunned)
        {
            if (action != BossAction.Chasing)
                CancelAction();
            bodyRenderer.color = new Color(1f, 0.86f, 0.25f);
            return;
        }

        float progress;
        switch (action)
        {
            case BossAction.LaserWarning:
                progress = Mathf.Clamp01((Time.time - actionStartedAt) / laserChargeSeconds);
                UpdateLaserWarning(progress);
                if (progress >= 1f)
                    FireLaser();
                break;
            case BossAction.LaserFiring:
                progress = Mathf.Clamp01((Time.time - actionStartedAt) / laserFlashSeconds);
                Color beamColor = laserBeam.color;
                beamColor.a = 0.85f * (1f - progress);
                laserBeam.color = beamColor;
                if (progress >= 1f)
                    EndAction();
                break;
            case BossAction.MeteorStorm:
                UpdateMeteorStorm();
                break;
            case BossAction.SummonWarning:
                progress = Mathf.Clamp01((Time.time - actionStartedAt) / summonChargeSeconds);
                summonGlow.transform.localScale = Vector3.one * Mathf.Lerp(2f, 5.5f, progress);
                summonGlow.color = new Color(0.8f, 0.35f, 1f, 0.15f + progress * 0.4f);
                if (progress >= 1f)
                {
                    if (SummonAllMonsters())
                    {
                        summonPending = false;
                        hasSummonedAtHalfHealth = true;
                        EndAction();
                    }
                    else
                        CancelAction();
                }
                break;
            case BossAction.Chasing:
                if (Time.time >= nextActionAt)
                    TryStartPattern();
                if (action == BossAction.Chasing && Time.time >= nextScatterAt &&
                    Vector2.Distance(transform.position, player.transform.position) <= attackRange)
                    FireScatterAttack();
                break;
        }
        bodyRenderer.color = status != null && status.IsSlowed
            ? new Color(0.56f, 0.91f, 1f)
            : action == BossAction.Chasing
                ? new Color(0.34f, 0.19f, 0.53f)
                : new Color(0.6f, 0.3f, 0.7f);
    }

    private void FixedUpdate()
    {
        if (Time.timeScale <= 0f || !IsAlive || player == null ||
            player.CurrentHealth <= 0f || status != null && status.IsStunned)
            return;
        if (action != BossAction.Chasing)
        {
            body.linearVelocity = Vector2.zero;
            return;
        }
        Vector2 difference = (Vector2)player.transform.position - body.position;
        if (difference.magnitude <= stopDistance)
            return;
        float multiplier = status != null ? status.MovementMultiplier : 1f;
        float step = moveSpeed * worldUnitsPerMoveSpeed * multiplier * Time.fixedDeltaTime;
        body.MovePosition(body.position + difference.normalized *
            Mathf.Min(step, difference.magnitude - stopDistance));
    }

    private void TryStartPattern()
    {
        if (action != BossAction.Chasing || player == null || Time.time < nextActionAt)
            return;
        if (summonPending && !MonsterSpawnGate2D.IsPaused)
        {
            BeginSummonWarning();
            return;
        }
        if (Vector2.Distance(transform.position, player.transform.position) > attackRange)
            return;
        bool laserReady = Time.time >= nextLaserAt;
        bool meteorReady = Time.time >= nextMeteorAt;
        bool far = Vector2.Distance(transform.position, player.transform.position) > stopDistance;
        if ((far && laserReady) || (!far && !meteorReady && laserReady))
        {
            BeginLaserWarning();
            return;
        }
        if (meteorReady)
        {
            BeginMeteorStorm();
            return;
        }
        if (laserReady)
            BeginLaserWarning();
    }

    private void FireScatterAttack()
    {
        if (action != BossAction.Chasing || Time.timeScale <= 0f || !IsAlive ||
            player == null || player.CurrentHealth <= 0f || scatterProjectilePrefab == null)
            return;
        Vector2 aim = (Vector2)player.transform.position - (Vector2)transform.position;
        if (aim.sqrMagnitude <= 0.001f)
            aim = Vector2.down;
        float aimDegrees = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;
        float step = 360f / scatterProjectileCount;
        float offset = scatterVolleyCount++ % 2 == 0 ? 0f : step * 0.5f;
        for (int index = 0; index < scatterProjectileCount; index++)
        {
            float angle = (aimDegrees + index * step + offset) * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            EnemyProjectile2D shot = Instantiate(scatterProjectilePrefab,
                (Vector2)transform.position + direction * 1.7f, Quaternion.identity);
            SpriteRenderer shotRenderer = shot.GetComponent<SpriteRenderer>();
            if (shotRenderer != null)
                shotRenderer.color = new Color(1f, 0.38f, 0.72f);
            shot.Launch(direction, attackDamage);
        }
        nextScatterAt = Time.time + scatterAttackInterval;
    }

    private void BeginLaserWarning()
    {
        body.linearVelocity = Vector2.zero;
        laserDirection = ((Vector2)player.transform.position - (Vector2)transform.position).normalized;
        if (laserDirection.sqrMagnitude <= 0.001f)
            laserDirection = Vector2.down;
        action = BossAction.LaserWarning;
        actionStartedAt = Time.time;
        laserBoundary.enabled = true;
        laserFill.enabled = true;
        UpdateLaserWarning(0f);
    }

    private void UpdateLaserWarning(float progress)
    {
        if (progress < laserAimLockFraction)
        {
            Vector2 towardPlayer = (Vector2)player.transform.position - (Vector2)transform.position;
            if (towardPlayer.sqrMagnitude > 0.001f)
                laserDirection = towardPlayer.normalized;
        }
        SetBeam(laserBoundary, laserLength, 1.2f,
            new Color(1f, 0.1f, 0.15f, 0.28f));
        SetBeam(laserFill, Mathf.Max(0.05f, laserLength * progress), 0.95f,
            new Color(1f, 0.14f, 0.2f, 0.3f + 0.5f * progress));
    }

    private void SetBeam(SpriteRenderer renderer, float length, float width, Color color)
    {
        renderer.transform.localPosition = (Vector3)(laserDirection * length * 0.5f);
        renderer.transform.localRotation = Quaternion.Euler(0f, 0f,
            Mathf.Atan2(laserDirection.y, laserDirection.x) * Mathf.Rad2Deg);
        renderer.transform.localScale = new Vector3(length, width, 1f);
        renderer.color = color;
    }

    private void FireLaser()
    {
        laserBoundary.enabled = false;
        laserFill.enabled = false;
        laserBeam.enabled = true;
        SetBeam(laserBeam, laserLength, laserHitRadius * 2f,
            new Color(1f, 0.56f, 0.32f, 0.85f));
        if (MidBoss2D.IsPointNearSegment(transform.position,
                (Vector2)transform.position + laserDirection * laserLength,
                player.transform.position, laserHitRadius))
            player.ReceiveDamage(attackDamage * 1.5f);
        action = BossAction.LaserFiring;
        actionStartedAt = Time.time;
    }

    private float MeteorInterval => activeMeteorCount > 1
        ? (meteorPatternSeconds - meteorChargeSeconds - meteorImpactSeconds) /
          (activeMeteorCount - 1)
        : 0f;

    private void BeginMeteorStorm()
    {
        body.linearVelocity = Vector2.zero;
        if (worldCamera == null)
            worldCamera = Camera.main;
        if (worldCamera == null || meteorPositions.Length == 0)
        {
            CancelAction();
            return;
        }

        action = BossAction.MeteorStorm;
        actionStartedAt = Time.time;
        activeMeteorCount = meteorPositions.Length;
        for (int index = 0; index < activeMeteorCount; index++)
        {
            meteorScheduled[index] = false;
            meteorLanded[index] = false;
        }
        HideEffects();
        UpdateMeteorStorm();
    }

    private void ScheduleMeteor(int index, float interval)
    {
        float inset = meteorHitRadius + 0.25f;
        float halfHeight = worldCamera.orthographicSize;
        float halfWidth = halfHeight * worldCamera.aspect;
        Vector2 center = worldCamera.transform.position;
        float bestClearance = float.NegativeInfinity;
        Vector2 best = center;
        for (int attempt = 0; attempt < 64; attempt++)
        {
            Vector2 candidate = center + new Vector2(
                Random.Range(-halfWidth + inset, halfWidth - inset),
                Random.Range(-halfHeight + inset, halfHeight - inset));
            float clearance = float.PositiveInfinity;
            for (int previous = index - 1; previous >= 0; previous--)
            {
                if ((index - previous) * interval >=
                    meteorChargeSeconds + meteorImpactSeconds)
                    break;
                clearance = Mathf.Min(clearance,
                    Vector2.Distance(candidate, meteorPositions[previous]));
            }
            if (clearance > bestClearance)
            {
                bestClearance = clearance;
                best = candidate;
            }
            if (clearance >= meteorHitRadius * 2f + 0.25f)
                break;
        }
        meteorPositions[index] = best;
        meteorScheduled[index] = true;
        meteorBoundaries[index].transform.position = best;
        meteorFills[index].transform.position = best;
        meteorImpacts[index].transform.position = best;
    }

    private void UpdateMeteorStorm()
    {
        float elapsed = Time.time - actionStartedAt;
        float interval = MeteorInterval;
        for (int index = 0; index < activeMeteorCount; index++)
        {
            float localTime = elapsed - index * interval;
            if (localTime >= 0f && !meteorScheduled[index])
                ScheduleMeteor(index, interval);
            bool warning = localTime >= 0f && localTime < meteorChargeSeconds;
            meteorBoundaries[index].enabled = warning;
            meteorFills[index].enabled = warning;
            meteorFalling[index].enabled = warning &&
                localTime >= meteorChargeSeconds * 0.65f;
            if (warning)
            {
                float progress = Mathf.Clamp01(localTime / meteorChargeSeconds);
                meteorFills[index].transform.localScale = Vector3.one *
                    Mathf.Max(0.05f, meteorHitRadius * 2f * progress);
                meteorFills[index].color = new Color(1f, 0.2f, 0.1f,
                    0.2f + progress * 0.55f);
                if (meteorFalling[index].enabled)
                    meteorFalling[index].transform.position = meteorPositions[index] +
                        Vector2.up * Mathf.Lerp(3.5f, 0f,
                            (progress - 0.65f) / 0.35f);
            }
            if (localTime >= meteorChargeSeconds && !meteorLanded[index])
            {
                meteorLanded[index] = true;
                if (Vector2.Distance(player.transform.position, meteorPositions[index]) <=
                    meteorHitRadius)
                    player.ReceiveDamage(attackDamage * 1.2f);
            }
            bool impact = localTime >= meteorChargeSeconds &&
                localTime < meteorChargeSeconds + meteorImpactSeconds;
            meteorImpacts[index].enabled = impact;
            if (impact)
                meteorImpacts[index].color = new Color(1f, 0.7f, 0.3f,
                    0.85f * (1f - (localTime - meteorChargeSeconds) / meteorImpactSeconds));
        }
        if (elapsed >= meteorPatternSeconds)
            EndAction();
    }

    private void BeginSummonWarning()
    {
        body.linearVelocity = Vector2.zero;
        action = BossAction.SummonWarning;
        actionStartedAt = Time.time;
        summonGlow.enabled = true;
    }

    private bool SummonAllMonsters()
    {
        if (MonsterSpawnGate2D.IsPaused || summonPrefabs == null)
            return false;
        for (int index = 0; index < summonPrefabs.Length * 2; index++)
        {
            CombatTarget2D prefab = summonPrefabs[index / 2];
            if (prefab != null)
                Instantiate(prefab, FindSummonPosition(index), Quaternion.identity);
        }
        return true;
    }

    private Vector2 FindSummonPosition(int index)
    {
        if (worldCamera == null)
            worldCamera = Camera.main;
        for (int attempt = 0; attempt < 12; attempt++)
        {
            float angle = (index + attempt * 0.37f) * Mathf.PI * 2f / 10f;
            Vector2 candidate = (Vector2)transform.position +
                new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (3.5f + attempt * 0.2f);
            Vector3 viewport = worldCamera.WorldToViewportPoint(candidate);
            if (viewport.z > 0f && viewport.x >= 0.07f && viewport.x <= 0.93f &&
                viewport.y >= 0.07f && viewport.y <= 0.93f &&
                Vector2.Distance(candidate, player.transform.position) >= minimumSummonPlayerDistance)
                return candidate;
        }
        return BasicMeleeEnemySpawner2D.PositionOutsideView(worldCamera,
            player.transform.position, 1.5f, minimumSummonPlayerDistance,
            index % 4, (index + 0.5f) / 10f);
    }

    private void EndAction()
    {
        switch (action)
        {
            case BossAction.LaserFiring: nextLaserAt = Time.time + laserCooldownSeconds; break;
            case BossAction.MeteorStorm: nextMeteorAt = Time.time + meteorCooldownSeconds; break;
        }
        HideEffects();
        action = BossAction.Chasing;
        nextActionAt = Time.time + (summonPending ? 0f : actionInterval);
    }

    private void CancelAction()
    {
        HideEffects();
        action = BossAction.Chasing;
        nextActionAt = Time.time + (summonPending ? 0f : actionInterval);
    }

    private void HideEffects()
    {
        if (laserBoundary != null) laserBoundary.enabled = false;
        if (laserFill != null) laserFill.enabled = false;
        if (laserBeam != null) laserBeam.enabled = false;
        if (summonGlow != null) summonGlow.enabled = false;
        if (meteorBoundaries == null) return;
        for (int index = 0; index < meteorBoundaries.Length; index++)
        {
            meteorBoundaries[index].enabled = false;
            meteorFills[index].enabled = false;
            meteorFalling[index].enabled = false;
            meteorImpacts[index].enabled = false;
        }
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
        ReportDamage(amount, transform.position + Vector3.up * 2f);
        UpdateHealthBar();
        if (IsAlive)
            return;
        HideEffects();
        if (playerProgress == null)
            playerProgress = FindFirstObjectByType<PlayerProgress>();
        if (playerProgress != null)
            playerProgress.RegisterEnemyKill(experienceOnDeath);
        Defeated?.Invoke();
        Destroy(gameObject);
    }

    private void UpdateHealthBar()
    {
        if (healthFillRenderer == null)
            return;
        float ratio = maxHealth > 0f ? Mathf.Clamp01(currentHealth / maxHealth) : 0f;
        healthFillRenderer.transform.localScale = new Vector3(3.2f * ratio, 0.16f, 1f);
        healthFillRenderer.transform.localPosition =
            new Vector3(-1.6f * (1f - ratio), 2.02f, -0.02f);
        healthFillRenderer.color = Color.Lerp(new Color(0.95f, 0.25f, 0.25f),
            new Color(0.48f, 0.92f, 0.65f), ratio);
    }
}
