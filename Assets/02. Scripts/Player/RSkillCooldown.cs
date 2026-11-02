using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerStats))]
public sealed class RSkillCooldown : MonoBehaviour
{
    [SerializeField] private string skillName = "무차별 난사";
    [SerializeField, Min(0.1f)] private float cooldownSeconds = 30f;
    [SerializeField, Min(1)] private int totalShots = 50;
    [SerializeField, Min(0.1f)] private float shotsPerSecond = 10f;
    [SerializeField, Min(0f)] private float damageMultiplier = 1.5f;
    [SerializeField, Min(0.1f)] private float projectileSpeed = 16f;
    [SerializeField] private Projectile2D projectilePrefab;

    private float readyAt;
    private float nextShotAt;
    private float firingUntil;
    private float currentFiringDuration;
    private int shotsRemaining;
    private PlayerStats stats;
    private Vector2? manualAimPoint;
    private int bulletAugmentLevel;
    private int damageAugmentLevel;
    private int cooldownAugmentLevel;

    public float CooldownSeconds => Mathf.Max(0f, cooldownSeconds -
        AugmentCatalog.BarrageCooldownReduction(cooldownAugmentLevel));
    public int TotalShots => totalShots + 10 * bulletAugmentLevel;
    public float DamageMultiplier => damageMultiplier + 0.1f * damageAugmentLevel;
    public string SkillName => skillName;
    public float RemainingSeconds => IsFiring ? 0f : Mathf.Max(0f, readyAt - Time.time);
    public bool IsReady => !IsFiring && Time.time >= readyAt;
    public int ShotsRemaining => shotsRemaining;
    public Vector2? ManualAimPoint => manualAimPoint;
    public float FiringDuration => TotalShots / Mathf.Max(0.1f, shotsPerSecond);
    public bool IsFiring => Time.time < firingUntil;
    public float UsageRemaining01 => IsFiring
        ? Mathf.Clamp01((firingUntil - Time.time) / currentFiringDuration) : 0f;
    public float DamagePerShot
    {
        get
        {
            PlayerStats currentStats = stats != null ? stats : GetComponent<PlayerStats>();
            return currentStats != null ? currentStats.AttackDamage * DamageMultiplier : 0f;
        }
    }

    private void Awake()
    {
        stats = GetComponent<PlayerStats>();
    }

    public void ConfigureProjectile(Projectile2D prefab)
    {
        projectilePrefab = prefab;
    }

    public void ApplyAugmentLevels(int bullets, int damage, int cooldown)
    {
        bulletAugmentLevel = Mathf.Clamp(bullets, 0, 5);
        damageAugmentLevel = Mathf.Clamp(damage, 0, 5);
        float previousCooldown = CooldownSeconds;
        cooldownAugmentLevel = Mathf.Clamp(cooldown, 0, 5);
        if (readyAt > Time.time)
            readyAt = Mathf.Max(Time.time, readyAt - (previousCooldown - CooldownSeconds));
    }

    private void Update()
    {
        if (Time.timeScale <= 0f)
            return;

        UpdateManualAim();
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.rKey.wasPressedThisFrame && IsReady && !IsFiring &&
            projectilePrefab != null && (manualAimPoint.HasValue || FindNearestVisibleDummy() != null))
        {
            shotsRemaining = TotalShots;
            nextShotAt = Time.time;
            currentFiringDuration = FiringDuration;
            firingUntil = Time.time + currentFiringDuration;
            readyAt = firingUntil + CooldownSeconds;
        }

        while (shotsRemaining > 0 && Time.time >= nextShotAt)
        {
            if (manualAimPoint.HasValue)
            {
                Vector2 direction = (manualAimPoint.Value - (Vector2)transform.position).normalized;
                if (direction.sqrMagnitude > 0.001f)
                {
                    Projectile2D projectile = Instantiate(projectilePrefab,
                        transform.position + (Vector3)direction * 0.55f, Quaternion.identity);
                    projectile.InitializeDirection(direction, DamagePerShot, projectileSpeed);
                }
            }
            else
            {
                TrainingDummy2D target = FindNearestVisibleDummy();
                if (target != null)
                {
                    Vector3 direction = (target.transform.position - transform.position).normalized;
                    Projectile2D projectile = Instantiate(projectilePrefab,
                        transform.position + direction * 0.55f, Quaternion.identity);
                    projectile.Initialize(target, DamagePerShot, projectileSpeed);
                }
            }
            shotsRemaining--;
            nextShotAt += 1f / shotsPerSecond;
        }
    }

    private void UpdateManualAim()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.isPressed)
        {
            ApplyAimInput(false, null);
            return;
        }

        Vector2 screen = mouse.position.ReadValue();
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem != null)
        {
            var pointer = new PointerEventData(eventSystem) { position = screen };
            var uiHits = new List<RaycastResult>();
            eventSystem.RaycastAll(pointer, uiHits);
            if (uiHits.Count > 0)
            {
                ApplyAimInput(false, null);
                return;
            }
        }

        Camera camera = Camera.main;
        if (camera == null)
        {
            ApplyAimInput(false, null);
            return;
        }

        float depth = Mathf.Abs(transform.position.z - camera.transform.position.z);
        Vector3 world = camera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, depth));
        ApplyAimInput(true, (Vector2)world);
    }

    public void ApplyAimInput(bool leftButtonHeld, Vector2? worldPoint)
    {
        manualAimPoint = leftButtonHeld ? worldPoint : null;
    }

    public TrainingDummy2D FindNearestVisibleDummy()
    {
        Camera camera = Camera.main;
        if (camera == null)
            return null;

        TrainingDummy2D nearest = null;
        float nearestSqrDistance = float.PositiveInfinity;
        foreach (TrainingDummy2D dummy in FindObjectsByType<TrainingDummy2D>(FindObjectsSortMode.None))
        {
            if (!IsValidTarget(dummy, camera))
                continue;

            float sqrDistance = (dummy.transform.position - transform.position).sqrMagnitude;
            if (sqrDistance >= nearestSqrDistance)
                continue;
            nearest = dummy;
            nearestSqrDistance = sqrDistance;
        }
        return nearest;
    }

    private static bool IsValidTarget(TrainingDummy2D target, Camera camera)
    {
        if (target == null || !target.IsAlive || camera == null)
            return false;
        Vector3 viewport = camera.WorldToViewportPoint(target.transform.position);
        return viewport.z > 0f && viewport.x >= 0f && viewport.x <= 1f &&
            viewport.y >= 0f && viewport.y <= 1f;
    }
}
