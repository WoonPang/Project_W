using UnityEngine;

public sealed class PlayerStats : MonoBehaviour
{
    [SerializeField, Min(1f)] private float maxHealth = 100f;
    [SerializeField, Min(0f)] private float attackDamage = 10f;
    [SerializeField, Min(0f)] private float moveSpeed = 1f;
    [SerializeField, Min(0.01f)] private float attackSpeed = 1f;

    public float MaxHealth => maxHealth;
    public float CurrentHealth { get; private set; }
    public float AttackDamage => attackDamage;
    public float MoveSpeed => moveSpeed;
    public float AttackSpeed => attackSpeed;

    private void Awake()
    {
        CurrentHealth = maxHealth;
    }

    public void AdjustHealth(float amount)
    {
        CurrentHealth = Mathf.Clamp(CurrentHealth + amount, 0f, maxHealth);
    }

    public bool ReceiveDamage(float amount)
    {
        if (amount <= 0f || CurrentHealth <= 0f)
            return false;

        PlayerAutomaticAugments2D skills = GetComponent<PlayerAutomaticAugments2D>();
        if (skills != null && skills.TryBlockHit())
            return false;

        AdjustHealth(-amount);
        return true;
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
}
