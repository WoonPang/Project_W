using System;
using UnityEngine;

public sealed class PlayerStats : MonoBehaviour
{
    [SerializeField, Min(1f)] private float maxHealth = 100f;
    [SerializeField, Min(0f)] private float attackDamage = 10f;
    [SerializeField, Min(0f)] private float moveSpeed = 1f;
    [SerializeField, Min(0.01f)] private float attackSpeed = 1f;
    [Header("Gains per character level")]
    [SerializeField, Min(0f)] private float healthPerLevel = 5f;
    [SerializeField, Min(0f)] private float attackDamagePerLevel = 2.5f;
    [SerializeField, Min(0f)] private float moveSpeedPerLevel = 0.025f;
    [SerializeField, Min(0f)] private float attackSpeedPerLevel = 0.025f;
    [SerializeField, Min(0f)] private float invulnerabilitySeconds = 3f;

    private float invulnerableUntil;
    private bool debugInvincible;

    public float MaxHealth => maxHealth;
    public float CurrentHealth { get; private set; }
    public float AttackDamage => attackDamage;
    public float MoveSpeed => moveSpeed;
    public float AttackSpeed => attackSpeed;
    public bool IsInvulnerable => Time.time < invulnerableUntil;
    public bool IsDebugInvincible => debugInvincible;
    public float InvulnerabilityRemaining => Mathf.Max(0f, invulnerableUntil - Time.time);
    public event Action<float> Damaged;

    private void Awake()
    {
        CurrentHealth = maxHealth;
    }

    public void AdjustHealth(float amount)
    {
        CurrentHealth = Mathf.Clamp(CurrentHealth + amount, 0f, maxHealth);
    }

    public void SetDebugInvincible(bool enabled)
    {
        debugInvincible = enabled;
    }

    public bool ReceiveDamage(float amount)
    {
        if (amount <= 0f || CurrentHealth <= 0f || IsInvulnerable || debugInvincible)
            return false;

        PlayerAutomaticAugments2D skills = GetComponent<PlayerAutomaticAugments2D>();
        if (skills != null && skills.TryBlockHit())
        {
            StartInvulnerability();
            return false;
        }

        AdjustHealth(-amount);
        StartInvulnerability();
        Damaged?.Invoke(amount);
        return true;
    }

    private void StartInvulnerability()
    {
        invulnerableUntil = Time.time + invulnerabilitySeconds;
    }

    public void AddAttackDamage(float amount)
    {
        attackDamage = Mathf.Max(0f, attackDamage + amount);
    }

    public void AddMaxHealth(float amount)
    {
        if (amount <= 0f)
            return;

        maxHealth += amount;
        CurrentHealth += amount;
    }

    public void AddMoveSpeed(float amount)
    {
        moveSpeed = Mathf.Max(0f, moveSpeed + amount);
    }

    public void AddAttackSpeed(float amount)
    {
        attackSpeed = Mathf.Max(0.01f, attackSpeed + amount);
    }

    public void ApplyLevelGrowth()
    {
        AddMaxHealth(healthPerLevel);
        AddAttackDamage(attackDamagePerLevel);
        AddMoveSpeed(moveSpeedPerLevel);
        AddAttackSpeed(attackSpeedPerLevel);
    }
}
