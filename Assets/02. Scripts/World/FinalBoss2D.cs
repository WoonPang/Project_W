using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D), typeof(EnemyStatus2D))]
public sealed class FinalBoss2D : CombatTarget2D, ILevelScaledEnemyHealth
{
    private enum BossAction { Chasing, LaserWarning, LaserFiring, MeteorWarning, MeteorImpact, SummonWarning }

    [SerializeField, Min(1f)] private float maxHealth = 500f;
    [SerializeField, Min(0f)] private float attackDamage = 20f;
    [SerializeField, Min(0f)] private float moveSpeed = 1.5f;
    [SerializeField, Min(0f)] private float experienceOnDeath = 100f;
    [SerializeField, Min(0.1f)] private float worldUnitsPerMoveSpeed = 5f;
    [SerializeField, Min(0.1f)] private float stopDistance = 5f;
    [SerializeField, Min(0.1f)] private float attackRange = 8f;
    [SerializeField, Min(0.1f)] private float scatterAttackInterval = 2.2f;
    [SerializeField, Range(1, 20)] private int scatterProjectileCount = 10;
    [SerializeField, Range(0f, 180f)] private float scatterArcDegrees = 144f;
    [SerializeField, Min(0.1f)] private float laserChargeSeconds = 1.5f;
    [SerializeField, Min(0.1f)] private float laserFlashSeconds = 0.3f;
    [SerializeField, Min(0.1f)] private float laserLength = 30f;
    [SerializeField, Min(0.01f)] private float laserHitRadius = 0.55f;
    [SerializeField, Range(0f, 1f)] private float laserAimLockFraction = 0.65f;
    [SerializeField, Min(0.1f)] private float meteorChargeSeconds = 1.6f;
    [SerializeField, Min(0.1f)] private float meteorImpactSeconds = 0.3f;
    [SerializeField, Min(0.1f)] private float meteorHitRadius = 1.1f;
    [SerializeField, Min(0.1f)] private float summonChargeSeconds = 1f;
    [SerializeField, Min(0.1f)] private float actionInterval = 1.5f;
    [SerializeField, Min(0f)] private float laserCooldownSeconds = 7f;
    [SerializeField, Min(0f)] private float meteorCooldownSeconds = 15f;
    [SerializeField, Min(0f)] private float summonCooldownSeconds = 30f;
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
    private int activeMeteorCount;
    private Vector2 reservedMeteorSafePosition;
    private Vector2 laserDirection;
    private float currentHealth;
    private float actionStartedAt;
    private float nextActionAt;
    private float nextLaserAt;
    private float nextMeteorAt;
    private float nextSummonAt;
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
    public bool IsChargingMeteor => action == BossAction.MeteorWarning;
    public bool IsChargingSummon => action == BossAction.SummonWarning;

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
            case BossAction.MeteorWarning:
                progress = Mathf.Clamp01((Time.time - actionStartedAt) / meteorChargeSeconds);
                UpdateMeteorWarning(progress);
                if (progress >= 1f)
                    DropMeteors();
                break;
            case BossAction.MeteorImpact:
                progress = Mathf.Clamp01((Time.time - actionStartedAt) / meteorImpactSeconds);
                for (int index = 0; index < activeMeteorCount; index++)
                {
                    Color color = meteorImpacts[index].color;
                    color.a = 0.85f * (1f - progress);
                    meteorImpacts[index].color = color;
                }
                if (progress >= 1f)
                    EndAction();
                break;
            case BossAction.SummonWarning:
                progress = Mathf.Clamp01((Time.time - actionStartedAt) / summonChargeSeconds);
                summonGlow.transform.localScale = Vector3.one * Mathf.Lerp(2f, 5.5f, progress);
                summonGlow.color = new Color(0.8f, 0.35f, 1f, 0.15f + progress * 0.4f);
                if (progress >= 1f)
                {
                    SummonAllMonsters();
                    EndAction();
                }
                break;
            case BossAction.Chasing:
                if (Time.time >= nextActionAt)
                    TryStartRandomPattern();
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
        if (Time.timeScale <= 0f || !IsAlive || action != BossAction.Chasing ||
            player == null || player.CurrentHealth <= 0f || status != null && status.IsStunned)
            return;
        Vector2 difference = (Vector2)player.transform.position - body.position;
        if (difference.magnitude <= stopDistance)
            return;
        float multiplier = status != null ? status.MovementMultiplier : 1f;
        float step = moveSpeed * worldUnitsPerMoveSpeed * multiplier * Time.fixedDeltaTime;
        body.MovePosition(body.position + difference.normalized *
            Mathf.Min(step, difference.magnitude - stopDistance));
    }

    private void TryStartRandomPattern()
    {
        if (action != BossAction.Chasing || player == null || Time.time < nextActionAt ||
            Vector2.Distance(transform.position, player.transform.position) > attackRange)
            return;
        int count = 0;
        int chosen = -1;
        if (Time.time >= nextLaserAt)
        {
            count++;
            chosen = 0;
        }
        if (Time.time >= nextMeteorAt)
        {
            count++;
            if (Random.Range(0, count) == 0)
                chosen = 1;
        }
        if (Time.time >= nextSummonAt)
        {
            count++;
            if (Random.Range(0, count) == 0)
                chosen = 2;
        }
        switch (chosen)
        {
            case 0: BeginLaserWarning(); break;
            case 1: BeginMeteorWarning(); break;
            case 2: BeginSummonWarning(); break;
        }
    }

    private void FireScatterAttack()
    {
        if (scatterProjectilePrefab == null)
            return;
        Vector2 aim = (Vector2)player.transform.position - (Vector2)transform.position;
        if (aim.sqrMagnitude <= 0.001f)
            aim = Vector2.down;
        float aimDegrees = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;
        float step = scatterProjectileCount > 1
            ? scatterArcDegrees / (scatterProjectileCount - 1) : 0f;
        // With ten shots, alternate the slight offset so one lane passes through the aim point.
        float offset = scatterVolleyCount++ % 2 == 0 ? step * 0.5f : -step * 0.5f;
        for (int index = 0; index < scatterProjectileCount; index++)
        {
            float angle = (aimDegrees - scatterArcDegrees * 0.5f +
                index * step + offset) * Mathf.Deg2Rad;
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

    private void BeginMeteorWarning()
    {
        action = BossAction.MeteorWarning;
        actionStartedAt = Time.time;
        if (worldCamera == null)
            worldCamera = Camera.main;
        if (worldCamera == null || meteorPositions.Length == 0)
        {
            activeMeteorCount = 0;
            CancelAction();
            return;
        }

        float spacing = meteorHitRadius * 2f + 0.35f;
        float halfHeight = worldCamera.orthographicSize;
        float halfWidth = halfHeight * worldCamera.aspect;
        float inset = meteorHitRadius + 0.35f;
        int columns = Mathf.Max(1, Mathf.FloorToInt((halfWidth * 2f - inset * 2f) / spacing) + 1);
        int rows = Mathf.Max(1, Mathf.FloorToInt((halfHeight * 2f - inset * 2f) / spacing) + 1);
        Vector2 center = worldCamera.transform.position;
        Vector2 first = center - new Vector2((columns - 1) * spacing,
            (rows - 1) * spacing) * 0.5f;

        int nearest = 0;
        float nearestDistance = float.PositiveInfinity;
        for (int index = 0; index < columns * rows; index++)
        {
            Vector2 cell = first + new Vector2(index % columns, index / columns) * spacing;
            float distance = ((Vector2)player.transform.position - cell).sqrMagnitude;
            if (distance >= nearestDistance)
                continue;
            nearestDistance = distance;
            nearest = index;
        }

        int safeCell = FindAdjacentSafeCell(nearest, columns, rows, first, spacing);
        reservedMeteorSafePosition = first +
            new Vector2(safeCell % columns, safeCell / columns) * spacing;
        List<int> available = new List<int>(columns * rows - 1);
        for (int index = 0; index < columns * rows; index++)
            if (index != safeCell && index != nearest)
                available.Add(index);
        for (int index = available.Count - 1; index > 0; index--)
        {
            int other = Random.Range(0, index + 1);
            (available[index], available[other]) = (available[other], available[index]);
        }
        if (safeCell != nearest)
            available.Insert(0, nearest);
        activeMeteorCount = Mathf.Min(meteorPositions.Length,
            Mathf.Min(available.Count, Mathf.FloorToInt(columns * rows * 0.75f)));
        for (int index = 0; index < activeMeteorCount; index++)
        {
            int cell = available[index];
            meteorPositions[index] = first +
                new Vector2(cell % columns, cell / columns) * spacing;
            meteorBoundaries[index].transform.position = meteorPositions[index];
            meteorFills[index].transform.position = meteorPositions[index];
            meteorImpacts[index].transform.position = meteorPositions[index];
            meteorBoundaries[index].enabled = true;
            meteorFills[index].enabled = true;
            meteorFalling[index].enabled = false;
            meteorImpacts[index].enabled = false;
        }
        UpdateMeteorWarning(0f);
    }

    private int FindAdjacentSafeCell(int nearest, int columns, int rows,
        Vector2 first, float spacing)
    {
        int column = nearest % columns;
        int row = nearest / columns;
        int best = nearest;
        float bestDistance = float.NegativeInfinity;
        foreach (Vector2Int direction in new[] {
                     Vector2Int.left, Vector2Int.right, Vector2Int.up, Vector2Int.down })
        {
            int nextColumn = column + direction.x;
            int nextRow = row + direction.y;
            if (nextColumn < 0 || nextColumn >= columns || nextRow < 0 || nextRow >= rows)
                continue;
            Vector2 cell = first + new Vector2(nextColumn, nextRow) * spacing;
            float distance = (cell - (Vector2)transform.position).sqrMagnitude;
            if (distance <= bestDistance)
                continue;
            bestDistance = distance;
            best = nextRow * columns + nextColumn;
        }
        return best;
    }

    private void UpdateMeteorWarning(float progress)
    {
        for (int index = 0; index < activeMeteorCount; index++)
        {
            meteorFills[index].transform.localScale = Vector3.one *
                Mathf.Max(0.05f, meteorHitRadius * 2f * progress);
            meteorFills[index].color = new Color(1f, 0.2f, 0.1f, 0.2f + progress * 0.55f);
            meteorFalling[index].enabled = progress >= 0.65f;
            if (meteorFalling[index].enabled)
                meteorFalling[index].transform.position = meteorPositions[index] +
                    Vector2.up * Mathf.Lerp(3.5f, 0f, (progress - 0.65f) / 0.35f);
        }
    }

    private void DropMeteors()
    {
        bool hit = false;
        for (int index = 0; index < activeMeteorCount; index++)
        {
            meteorBoundaries[index].enabled = false;
            meteorFills[index].enabled = false;
            meteorFalling[index].enabled = false;
            meteorImpacts[index].enabled = true;
            meteorImpacts[index].color = new Color(1f, 0.7f, 0.3f, 0.85f);
            if (Vector2.Distance(player.transform.position, meteorPositions[index]) <= meteorHitRadius)
                hit = true;
        }
        if (hit)
            player.ReceiveDamage(attackDamage * 1.2f);
        action = BossAction.MeteorImpact;
        actionStartedAt = Time.time;
    }

    private void BeginSummonWarning()
    {
        action = BossAction.SummonWarning;
        actionStartedAt = Time.time;
        summonGlow.enabled = true;
    }

    private void SummonAllMonsters()
    {
        if (MonsterSpawnGate2D.IsPaused || summonPrefabs == null)
            return;
        for (int index = 0; index < summonPrefabs.Length * 2; index++)
        {
            CombatTarget2D prefab = summonPrefabs[index / 2];
            if (prefab != null)
                Instantiate(prefab, FindSummonPosition(index), Quaternion.identity);
        }
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
            case BossAction.MeteorImpact: nextMeteorAt = Time.time + meteorCooldownSeconds; break;
            case BossAction.SummonWarning: nextSummonAt = Time.time + summonCooldownSeconds; break;
        }
        HideEffects();
        action = BossAction.Chasing;
        nextActionAt = Time.time + actionInterval;
    }

    private void CancelAction()
    {
        HideEffects();
        action = BossAction.Chasing;
        nextActionAt = Time.time + actionInterval;
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
        if (!IsAlive || amount <= 0f)
            return;
        currentHealth = Mathf.Max(0f, currentHealth - amount);
        ReportDamage(amount, transform.position + Vector3.up * 2f);
        UpdateHealthBar();
        if (IsAlive)
            return;
        HideEffects();
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
        healthFillRenderer.transform.localScale = new Vector3(3.2f * ratio, 0.16f, 1f);
        healthFillRenderer.transform.localPosition =
            new Vector3(-1.6f * (1f - ratio), 2.02f, -0.02f);
        healthFillRenderer.color = Color.Lerp(new Color(0.95f, 0.25f, 0.25f),
            new Color(0.48f, 0.92f, 0.65f), ratio);
    }
}
